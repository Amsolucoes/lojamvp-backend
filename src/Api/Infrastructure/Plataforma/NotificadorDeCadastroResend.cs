using LojaApi.Application.Plataforma;
using LojaApi.Models;

namespace LojaApi.Infrastructure.Plataforma;

// E-mails de novo cadastro pelo Resend: aviso interno para a equipe e boas-vindas para o cliente.
// Falha ao enviar é registrada em log e não derruba o cadastro.
public class NotificadorDeCadastroResend(Resend.IResend resend, ILogger<NotificadorDeCadastroResend> logger) : INotificadorDeCadastro
{
    public Task NotificarAsync(Loja loja, Usuario usuario, PerfilLoja? perfil) =>
        Task.WhenAll(EnviarAvisoNovoCadastroAsync(loja, usuario, perfil), EnviarBoasVindasAsync(loja, usuario, perfil));

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
            var documento = !string.IsNullOrWhiteSpace(loja.Cnpj) ? loja.Cnpj
                : !string.IsNullOrWhiteSpace(loja.Cpf) ? loja.Cpf
                : "não informado no cadastro";
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
                        {Linha("CNPJ/CPF", H(documento))}
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
}
