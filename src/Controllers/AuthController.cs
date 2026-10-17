using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;
using LojaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokenService, TenantService tenantService, Resend.IResend resend, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var usuario = await db.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == req.Email.ToLower() && u.Ativo);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(req.Senha, usuario.SenhaHash))
            return Unauthorized(new { erro = "E-mail ou senha incorretos." });

        if (usuario.Role != "superadmin")
        {
            var vinculo = await db.UsuariosLoja
                .Include(ul => ul.Loja)
                .FirstOrDefaultAsync(ul => ul.UsuarioId == usuario.Id && ul.Ativo);

            if (vinculo != null)
                await tenantService.VerificarAcessoAsync(vinculo.LojaId);
        }

        usuario.UltimoLoginEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var token = tokenService.GerarToken(usuario);
        return Ok(new LoginResponse(token, usuario.Nome, usuario.Email, usuario.Role));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var nome = User.FindFirstValue(ClaimTypes.Name);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var ehAcessoSuporte = User.FindFirstValue("acesso_suporte") == "true";

        // Atualiza o último acesso ao retomar sessão — mas NÃO conta acesso de suporte (superadmin logado como o cliente)
        if (!ehAcessoSuporte && Guid.TryParse(id, out var usuarioId))
        {
            var usuario = await db.Usuarios.FindAsync(usuarioId);
            if (usuario != null)
            {
                usuario.UltimoLoginEm = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        }

        return Ok(new { id, nome, email, role });
    }

    [HttpGet("gerar-hash/{senha}")]
    [AllowAnonymous]
    public IActionResult GerarHash(string senha)
    {
        return Ok(new { hash = BCrypt.Net.BCrypt.HashPassword(senha) });
    }

    // ── Auto-cadastro (signup público) ────────────────────────────
    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<IActionResult> Signup([FromBody] SignupRequest req)
    {
        // Validações
        if (string.IsNullOrWhiteSpace(req.NomeLoja) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Senha))
            return BadRequest(new { erro = "Preencha todos os campos obrigatórios." });

        if (req.Senha.Length < 6)
            return BadRequest(new { erro = "A senha deve ter pelo menos 6 caracteres." });

        if (await db.Usuarios.AnyAsync(u => u.Email.ToLower() == req.Email.ToLower()))
            return Conflict(new { erro = "Este e-mail já está cadastrado." });

        if (await db.Lojas.AnyAsync(l => l.Email.ToLower() == req.Email.ToLower()))
            return Conflict(new { erro = "Este e-mail já está cadastrado." });

        // Descobre se o perfil escolhido é "financeiro puro" — nesse caso o preço é fixo
        // e NÃO entra na promoção das 10 primeiras lojas (que é voltada pro plano Loja)
        string? tipoPlanoDoPerfil = null;
        if (!string.IsNullOrEmpty(req.PerfilId) && Guid.TryParse(req.PerfilId, out var perfilGuidCheck))
        {
            tipoPlanoDoPerfil = await db.PerfisLoja
                .Where(p => p.Id == perfilGuidCheck)
                .Select(p => p.TipoPlanoAplica)
                .FirstOrDefaultAsync();
        }
        bool ehFinanceiroPuro = tipoPlanoDoPerfil == "financeiro";

        // Conta lojas existentes para a promoção das 10 primeiras (não conta financeiro puro, que tem preço fixo à parte)
        var totalLojas = await db.Lojas.CountAsync(l => !l.EhTeste && l.TipoPlano != "financeiro");
        bool ehPromocional = !ehFinanceiroPuro && totalLojas < 10;

        // Módulos extras escolhidos no cadastro (ex: financeiro, nf, turmas) — só valem
        // pra quem NÃO é financeiro puro (esse já vem com o próprio módulo financeiro incluso)
        var chavesModulosValidas = new HashSet<string> { "financeiro", "nf", "servicos" };
        var modulosExtrasValidos = (req.ModulosExtras ?? new List<string>())
            .Where(m => chavesModulosValidas.Contains(m))
            .Distinct()
            .ToList();

        decimal valorModulosExtras = 0;
        if (!ehFinanceiroPuro && modulosExtrasValidos.Count > 0)
        {
            var precosModulos = await db.ModulosPreco
                .Where(m => modulosExtrasValidos.Contains(m.Chave) && m.DisponivelParaAtivar)
                .ToListAsync();
            valorModulosExtras = precosModulos.Sum(m => m.Valor);
            // Remove da lista qualquer módulo que não existe/não está disponível, pra não gravar sujeira
            modulosExtrasValidos = precosModulos.Select(m => m.Chave).ToList();
        }

        var mensalidadeBase = ehFinanceiroPuro ? 39.90m : (ehPromocional ? 89.90m : 119.90m);

        var loja = new Loja
        {
            Nome = req.NomeLoja,
            Email = req.Email,
            Telefone = req.Telefone,
            CorPrimaria = "#c38228",
            MensalidadeDia = DateTime.UtcNow.Day,
            MensalidadeValor = mensalidadeBase + valorModulosExtras,
            Promocional = ehPromocional,
            ValorPromocional = ehPromocional ? 89.90m : null,
            ValorPosPromocional = ehPromocional ? 119.90m + valorModulosExtras : null,
            MesesPromocional = ehPromocional ? 3 : 0,
            Status = StatusLoja.Trial,
            TrialAte = DateTime.UtcNow.AddDays(7),
            SchemaNome = TenantService.GerarSchemaNome(req.NomeLoja),
        };
        db.Lojas.Add(loja);

        // Usuário admin da loja
        var usuario = new Usuario
        {
            Nome = req.NomeResponsavel,
            Email = req.Email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.Senha),
            Role = "admin",
        };
        db.Usuarios.Add(usuario);

        db.UsuariosLoja.Add(new UsuarioLoja
        {
            LojaId = loja.Id,
            UsuarioId = usuario.Id,
            Role = "admin",
        });

        // Primeira fatura (vence ao fim do trial)
        db.Pagamentos.Add(new Pagamento
        {
            LojaId = loja.Id,
            Valor = loja.MensalidadeValor,
            Status = "pendente",
            Vencimento = loja.TrialAte,
        });

        await db.SaveChangesAsync();

        // Aplica o perfil escolhido (categorias, serviços, tipo de plano)
        if (!string.IsNullOrEmpty(req.PerfilId) && Guid.TryParse(req.PerfilId, out var perfilGuid))
        {
            var perfil = await db.PerfisLoja
                .Include(p => p.Categorias)
                .Include(p => p.Servicos)
                .FirstOrDefaultAsync(p => p.Id == perfilGuid);
            if (perfil != null)
            {
                // Categorias de produto
                foreach (var cat in perfil.Categorias.OrderBy(c => c.Ordem))
                    db.CategoriasLoja.Add(new CategoriaLoja
                    {
                        LojaId = loja.Id,
                        Nome = cat.Nome,
                        Ordem = cat.Ordem,
                        TipoTamanho = cat.TipoTamanho,
                    });

                // Serviços pré-definidos
                foreach (var s in perfil.Servicos.OrderBy(s => s.Ordem))
                    db.Servicos.Add(new Servico
                    {
                        LojaId = loja.Id,
                        Nome = s.Nome,
                        Categoria = s.Categoria,
                        Preco = s.Preco,
                        DuracaoMin = s.DuracaoMin,
                        Ativo = true,
                    });

                // Tipo de plano e módulos conforme o perfil
                loja.TipoPlano = perfil.TipoPlanoAplica;
                var modulosDoPerfil = new List<string>();
                if (perfil.TipoPlanoAplica == "servicos" || perfil.TipoPlanoAplica == "loja_modulos")
                    modulosDoPerfil.Add("servicos");
                else if (perfil.Nome == "Corretora")
                    modulosDoPerfil.Add("corretora");
                else if (perfil.Nome.StartsWith("Pilates"))
                    modulosDoPerfil.Add("turmas");

                // Junta com os módulos extras escolhidos no cadastro (ex: Loja + Financeiro + NF),
                // sem duplicar caso o perfil já traga o mesmo módulo
                var todosModulos = modulosDoPerfil.Concat(modulosExtrasValidos).Distinct().ToList();
                loja.ModulosAtivos = string.Join(",", todosModulos);

                await db.SaveChangesAsync();
            }
        }
        else if (modulosExtrasValidos.Count > 0)
        {
            // Sem perfil escolhido ("Começar do zero"), mas com módulos extras marcados
            loja.ModulosAtivos = string.Join(",", modulosExtrasValidos);
            await db.SaveChangesAsync();
        }

        // E-mails de novo cadastro (aviso interno + boas-vindas). Precisam ser aguardados: o DbContext é
        // escopado à requisição e já estaria descartado quando um envio "solto" fosse ler o perfil.
        try
        {
            PerfilLoja? perfilEscolhido = null;
            if (!string.IsNullOrEmpty(req.PerfilId) && Guid.TryParse(req.PerfilId, out var perfilEmailGuid))
            {
                perfilEscolhido = await db.PerfisLoja.AsNoTracking()
                    .Include(p => p.Categorias)
                    .Include(p => p.Servicos)
                    .FirstOrDefaultAsync(p => p.Id == perfilEmailGuid);
            }
            await Task.WhenAll(
                EnviarAvisoNovoCadastroAsync(loja, usuario, perfilEscolhido),
                EnviarBoasVindasAsync(loja, usuario, perfilEscolhido));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao preparar e-mails de novo cadastro.");
        }

        // Gera token e já loga
        var token = tokenService.GerarToken(usuario);
        return Ok(new LoginResponse(token, usuario.Nome, usuario.Email, usuario.Role));
    }

    private static string H(string? texto) => System.Net.WebUtility.HtmlEncode(texto ?? "");

    private static string NomeModulo(string chave) => chave switch
    {
        "financeiro" => "Financeiro (contas a pagar e receber, cartões, balanço)",
        "nf" => "Emissão de notas fiscais",
        "servicos" => "Serviços e agenda",
        "turmas" => "Turmas e alunos",
        "corretora" => "Corretora (apólices, comissões e funil)",
        _ => chave,
    };

    private async Task EnviarAvisoNovoCadastroAsync(Loja loja, Usuario usuario, PerfilLoja? perfil)
    {
        try
        {
            string Linha(string rotulo, string valor, bool ultima = false) =>
                $"<tr><td style='padding:6px 10px;{(ultima ? "" : "border-bottom:1px solid #eee;")}color:#888'>{rotulo}</td><td style='padding:6px 10px;{(ultima ? "" : "border-bottom:1px solid #eee")}'>{valor}</td></tr>";

            var modulos = string.IsNullOrWhiteSpace(loja.ModulosAtivos) ? "—" : H(loja.ModulosAtivos);
            var mensalidade = "R$ " + loja.MensalidadeValor.ToString("N2") + (loja.Promocional ? " (promocional)" : "");
            var trialAte = loja.TrialAte.ToString("dd/MM/yyyy");
            var html = $@"
                <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                    <h2 style='color:#c38228'>🎉 Nova loja cadastrada!</h2>
                    <table style='width:100%;border-collapse:collapse;margin-top:12px'>
                        {Linha("Nome da loja", "<strong>" + H(loja.Nome) + "</strong>")}
                        {Linha("Perfil escolhido", H(perfil?.Nome ?? "Começar do zero"))}
                        {Linha("Tipo de plano", H(loja.TipoPlano))}
                        {Linha("Módulos", modulos)}
                        {Linha("Mensalidade", mensalidade)}
                        {Linha("Responsável", H(usuario.Nome))}
                        {Linha("E-mail", H(loja.Email))}
                        {Linha("Telefone", H(string.IsNullOrWhiteSpace(loja.Telefone) ? "—" : loja.Telefone))}
                        {Linha("CNPJ/CPF", H(string.IsNullOrWhiteSpace(loja.CnpjCpf) ? "não informado no cadastro" : loja.CnpjCpf))}
                        {Linha("Trial até", trialAte, true)}
                    </table>
                </div>";

            var msg = new Resend.EmailMessage
            {
                From = "AldevSoftware <financeiro@aldevsoftware.com.br>",
                Subject = $"🎉 Novo cadastro: {loja.Nome}",
                HtmlBody = html,
            };
            msg.To.Add("andre.ivarras@gmail.com");
            await resend.EmailSendAsync(msg);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar aviso de novo cadastro.");
        }
    }

    private async Task EnviarBoasVindasAsync(Loja loja, Usuario usuario, PerfilLoja? perfil)
    {
        try
        {
            var primeiroNome = H(usuario.Nome.Split(' ')[0]);

            // Resumo do que o perfil escolhido traz
            var blocoPerfil = "";
            if (perfil != null)
            {
                var categorias = perfil.Categorias.OrderBy(c => c.Ordem).Select(c => c.Nome).Take(8).ToList();
                var servicos = perfil.Servicos.OrderBy(sv => sv.Ordem).Select(sv => sv.Nome).Take(8).ToList();
                var itens = new System.Text.StringBuilder();
                if (!string.IsNullOrWhiteSpace(perfil.Descricao))
                    itens.Append($"<p style='margin:8px 0'>{H(perfil.Descricao)}</p>");
                if (categorias.Count > 0)
                    itens.Append($"<li>Categorias de produto já criadas: {H(string.Join(", ", categorias))}</li>");
                if (servicos.Count > 0)
                    itens.Append($"<li>Serviços já cadastrados (ajuste preços e duração): {H(string.Join(", ", servicos))}</li>");
                blocoPerfil = $@"
                    <div style='background:#faf5ec;border-radius:8px;padding:12px 16px;margin-top:16px'>
                        <strong>Seu perfil: {H(perfil.Nome)}</strong>
                        {itens}
                    </div>";
            }

            var modulos = (loja.ModulosAtivos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            var blocoModulos = modulos.Count == 0 ? "" : $@"
                    <p style='margin-top:16px'>Módulos ativos na sua conta:</p>
                    <ul style='padding-left:20px;line-height:1.8'>{string.Concat(modulos.Select(m => $"<li>{H(NomeModulo(m))}</li>"))}</ul>";

            var html = $@"
                <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                    <h2 style='color:#c38228'>Bem-vindo(a), {primeiroNome}! 🎉</h2>
                    <p>Sua loja <strong>{H(loja.Nome)}</strong> já está pronta pra usar. Você tem <strong>7 dias grátis</strong> (até {loja.TrialAte:dd/MM/yyyy}) pra testar tudo sem compromisso.</p>
                    {blocoPerfil}
                    {blocoModulos}
                    <p style='margin-top:16px'>Alguns pontos de partida:</p>
                    <ul style='padding-left:20px;line-height:1.8'>
                        <li>Cadastre seus primeiros produtos ou serviços</li>
                        <li>Configure suas contas e categorias no Financeiro</li>
                        <li>Convide sua equipe, se precisar</li>
                        <li>Instale o sistema no celular: abra o endereço no navegador e use “Adicionar à tela inicial”</li>
                    </ul>
                    <p style='margin-top:16px'>Tem alguma dúvida de como usar o sistema? Acesse nossa <a href='https://aldevsoftware.com.br/#duvidas' style='color:#c38228'>Central de Dúvidas</a> — tem passo a passo de como cadastrar produto com grade, configurar cartão de crédito e outras coisas do dia a dia.</p>
                    <p style='margin-top:20px'>
                        <a href='https://app.aldevsoftware.com.br' style='background:#c38228;color:#fff;padding:10px 20px;border-radius:8px;text-decoration:none;display:inline-block'>Acessar meu sistema →</a>
                    </p>
                    <p style='margin-top:20px;font-size:13px;color:#888'>Qualquer dúvida, é só responder este e-mail ou chamar no WhatsApp.</p>
                </div>";

            var msg = new Resend.EmailMessage
            {
                From = "AldevSoftware <financeiro@aldevsoftware.com.br>",
                Subject = $"Bem-vindo(a) ao AldevSoftware, {primeiroNome}!",
                HtmlBody = html,
            };
            msg.To.Add(loja.Email);
            await resend.EmailSendAsync(msg);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar e-mail de boas-vindas para {Email}.", loja.Email);
        }
    }

    // ── Trocar senha (usuário logado) ─────────────────────────────
    [HttpPost("trocar-senha")]
    [Authorize]
    public async Task<IActionResult> TrocarSenha([FromBody] TrocarSenhaRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await db.Usuarios.FindAsync(userId);
        if (usuario is null) return Unauthorized();

        if (!BCrypt.Net.BCrypt.Verify(req.SenhaAtual, usuario.SenhaHash))
            return BadRequest(new { erro = "Senha atual incorreta." });

        if (req.NovaSenha.Length < 8)
            return BadRequest(new { erro = "A nova senha deve ter pelo menos 8 caracteres." });

        usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.NovaSenha);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Senha alterada com sucesso." });
    }
}