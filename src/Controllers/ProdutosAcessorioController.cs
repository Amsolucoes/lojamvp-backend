using LojaApi.Data;
using LojaApi.Services;
using LojaApi.src.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/loja-acessorios/produtos")]
public class ProdutosAcessorioController(AppDbContext db, MercadoPagoService mpService) : ControllerBase
{
    // Tabela simples de frete fixo por região, enquanto não integra Correios/Melhor Envio.
    // Ajuste os valores como preferir — pode até deixar tudo igual por enquanto.
    private static readonly Dictionary<string, decimal> FRETE_POR_UF = new()
    {
        ["MS"] = 15m, // sua região — mais barato
        ["SP"] = 25m,
        ["RJ"] = 25m,
        ["MG"] = 25m,
        ["PR"] = 25m,
        ["SC"] = 25m,
        ["RS"] = 25m,
        ["GO"] = 25m,
        ["DF"] = 25m,
        ["MT"] = 25m,
    };
    private const decimal FRETE_PADRAO = 35m; // demais estados
    private static readonly TimeSpan PRAZO_PAGAMENTO = TimeSpan.FromMinutes(30);

    // Libera de volta o estoque de pedidos que passaram do prazo sem pagar.
    // Chamado nos endpoints públicos, evitando precisar de um job em segundo plano.
    private async Task LiberarPedidosExpiradosAsync()
    {
        var agora = DateTime.UtcNow;
        var expirados = await db.PedidosAcessorio
            .Include(p => p.Itens)
            .Where(p => p.Status == "aguardando_pagamento" && p.ExpiraEm != null && p.ExpiraEm < agora)
            .ToListAsync();

        if (expirados.Count == 0) return;

        foreach (var pedido in expirados)
        {
            pedido.Status = "cancelado";
            foreach (var item in pedido.Itens)
            {
                var produto = await db.ProdutosAcessorio.FindAsync(item.ProdutoId);
                if (produto != null) produto.Estoque += item.Quantidade;
            }
        }
        await db.SaveChangesAsync();
    }

    [HttpGet("frete")]
    [AllowAnonymous]
    public IActionResult CalcularFrete([FromQuery] string uf)
    {
        var valor = FRETE_POR_UF.TryGetValue(uf.ToUpper(), out var v) ? v : FRETE_PADRAO;
        return Ok(new { valorFrete = valor });
    }

    // ── Criar pedido + gerar cobrança Pix ──────────────────────────
    public record ItemPedidoRequest(Guid ProdutoId, int Quantidade);
    public record CriarPedidoRequest(
        string ClienteNome, string ClienteEmail, string ClienteTelefone, string? ClienteCpfCnpj,
        string Cep, string Endereco, string? Numero, string? Complemento, string? Bairro, string Cidade, string Uf,
        List<ItemPedidoRequest> Itens
    );

    [HttpPost("pedidos")]
    [AllowAnonymous]
    public async Task<IActionResult> CriarPedido([FromBody] CriarPedidoRequest req)
    {
        await LiberarPedidosExpiradosAsync();

        if (req.Itens.Count == 0)
            return BadRequest(new { erro = "O pedido precisa ter ao menos um item." });

        if (string.IsNullOrWhiteSpace(req.ClienteCpfCnpj))
            return BadRequest(new { erro = "CPF é obrigatório para gerar o Pix." });

        var pedido = new PedidoAcessorio
        {
            ClienteNome = req.ClienteNome.Trim(),
            ClienteEmail = req.ClienteEmail.Trim(),
            ClienteTelefone = req.ClienteTelefone.Trim(),
            ClienteCpfCnpj = req.ClienteCpfCnpj?.Trim(),
            Cep = req.Cep.Trim(),
            Endereco = req.Endereco.Trim(),
            Numero = req.Numero,
            Complemento = req.Complemento,
            Bairro = req.Bairro,
            Cidade = req.Cidade.Trim(),
            Uf = req.Uf.Trim().ToUpper(),
            ExpiraEm = DateTime.UtcNow.Add(PRAZO_PAGAMENTO),
        };

        decimal subtotal = 0;
        foreach (var item in req.Itens)
        {
            var produto = await db.ProdutosAcessorio.FirstOrDefaultAsync(p => p.Id == item.ProdutoId && p.Ativo);
            if (produto is null) return BadRequest(new { erro = $"Produto não encontrado." });
            if (produto.Estoque < item.Quantidade)
                return BadRequest(new { erro = $"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.Estoque}." });

            var precoUnitario = produto.PrecoPromocional ?? produto.Preco;
            var itemSubtotal = precoUnitario * item.Quantidade;
            subtotal += itemSubtotal;

            pedido.Itens.Add(new ItemPedidoAcessorio
            {
                ProdutoId = produto.Id,
                NomeProduto = produto.Nome,
                Quantidade = item.Quantidade,
                PrecoUnitario = precoUnitario,
                Subtotal = itemSubtotal,
            });

            // Reserva o estoque já na criação do pedido (evita concorrência de dois pedidos pro mesmo item)
            produto.Estoque -= item.Quantidade;
        }

        var valorFrete = FRETE_POR_UF.TryGetValue(pedido.Uf, out var vf) ? vf : FRETE_PADRAO;
        pedido.Subtotal = subtotal;
        pedido.ValorFrete = valorFrete;
        pedido.Total = subtotal + valorFrete;

        db.PedidosAcessorio.Add(pedido);
        await db.SaveChangesAsync();

        var resultadoPix = await mpService.CriarPix(
            valor: pedido.Total,
            descricao: $"Pedido AlDevSoftware #{pedido.Id.ToString()[..8]}",
            emailPagador: pedido.ClienteEmail,
            cpfPagador: pedido.ClienteCpfCnpj!,
            nomePagador: pedido.ClienteNome,
            pagamentoId: pedido.Id
        );

        if (!resultadoPix.Sucesso)
        {
            // Desfaz a reserva de estoque se o Pix falhar ao gerar
            foreach (var item in pedido.Itens)
            {
                var produto = await db.ProdutosAcessorio.FindAsync(item.ProdutoId);
                if (produto != null) produto.Estoque += item.Quantidade;
            }
            db.PedidosAcessorio.Remove(pedido);
            await db.SaveChangesAsync();
            return BadRequest(new { erro = "Não foi possível gerar o pagamento. Tente novamente." });
        }

        pedido.MpPaymentId = resultadoPix.MpPaymentId;
        pedido.MpStatus = resultadoPix.Status;
        await db.SaveChangesAsync();

        return Ok(new
        {
            pedido.Id,
            pedido.Total,
            qrCode = resultadoPix.QrCode,
            qrCodeBase64 = resultadoPix.QrCodeBase64,
        });
    }

    // ── Cliente consulta status do pagamento (polling na tela de checkout) ──
    [HttpGet("pedidos/{id:guid}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> ConsultarStatus(Guid id)
    {
        var pedido = await db.PedidosAcessorio.FirstOrDefaultAsync(p => p.Id == id);
        if (pedido is null) return NotFound();

        if (pedido.Status == "aguardando_pagamento" && pedido.MpPaymentId != null)
        {
            var statusAtual = await mpService.VerificarStatus(pedido.MpPaymentId);
            if (statusAtual == "approved" && pedido.Status != "pago")
            {
                pedido.Status = "pago";
                pedido.PagoEm = DateTime.UtcNow;
                pedido.MpStatus = statusAtual;
                pedido.ExpiraEm = null;
                await db.SaveChangesAsync();
            }
        }

        return Ok(new { pedido.Status });
    }

    // ── Gestão de pedidos (superadmin) ─────────────────────────────
    [HttpGet("pedidos")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ListarPedidos([FromQuery] string? status)
    {
        var q = db.PedidosAcessorio.Include(p => p.Itens).AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(p => p.Status == status);

        var lista = await q.OrderByDescending(p => p.CriadoEm).ToListAsync();
        return Ok(lista);
    }

    public record AtualizarStatusPedidoRequest(string Status, string? CodigoRastreio);

    [HttpPatch("pedidos/{id:guid}/status")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> AtualizarStatusPedido(Guid id, [FromBody] AtualizarStatusPedidoRequest req)
    {
        var pedido = await db.PedidosAcessorio.FindAsync(id);
        if (pedido is null) return NotFound();

        var statusValidos = new[] { "aguardando_pagamento", "pago", "enviado", "entregue", "cancelado" };
        if (!statusValidos.Contains(req.Status))
            return BadRequest(new { erro = "Status inválido." });

        pedido.Status = req.Status;
        if (req.CodigoRastreio != null) pedido.CodigoRastreio = req.CodigoRastreio;
        if (req.Status == "enviado") pedido.EnviadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(pedido);
    }

    // ── Categorias (gestão superadmin) ─────────────────────────────
    [HttpGet("categorias")]
    public async Task<IActionResult> ListarCategorias()
    {
        var lista = await db.CategoriasAcessorio
            .Where(c => c.Ativa)
            .OrderBy(c => c.Ordem).ThenBy(c => c.Nome)
            .ToListAsync();
        return Ok(lista);
    }

    public record SalvarCategoriaAcessorioRequest(string Nome);

    private static string GerarChaveCategoria(string nome)
    {
        var normalizado = nome.Trim().ToLowerInvariant();
        var semAcento = new string(normalizado
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray());
        var chave = new string(semAcento.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        while (chave.Contains("__")) chave = chave.Replace("__", "_");
        return chave.Trim('_');
    }

    [HttpPost("categorias")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> CriarCategoria([FromBody] SalvarCategoriaAcessorioRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Digite o nome da categoria." });

        var chave = GerarChaveCategoria(req.Nome);
        if (chave.Length == 0)
            return BadRequest(new { erro = "Nome inválido." });

        var jaExiste = await db.CategoriasAcessorio.AnyAsync(c => c.Chave == chave);
        if (jaExiste)
            return Conflict(new { erro = "Já existe uma categoria com esse nome." });

        var maiorOrdem = await db.CategoriasAcessorio.MaxAsync(c => (int?)c.Ordem) ?? -1;

        var categoria = new CategoriaAcessorio
        {
            Nome = req.Nome.Trim(),
            Chave = chave,
            Ordem = maiorOrdem + 1,
        };
        db.CategoriasAcessorio.Add(categoria);
        await db.SaveChangesAsync();

        return Ok(categoria);
    }

    [HttpPut("categorias/{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> AtualizarCategoria(Guid id, [FromBody] SalvarCategoriaAcessorioRequest req)
    {
        var categoria = await db.CategoriasAcessorio.FindAsync(id);
        if (categoria is null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Digite o nome da categoria." });

        // A Chave NÃO muda aqui de propósito — trocar mudaria a categoria de produtos já
        // salvos silenciosamente. Só o nome de exibição é editável.
        categoria.Nome = req.Nome.Trim();
        await db.SaveChangesAsync();

        return Ok(categoria);
    }

    [HttpDelete("categorias/{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ExcluirCategoria(Guid id)
    {
        var categoria = await db.CategoriasAcessorio.FindAsync(id);
        if (categoria is null) return NotFound();

        var emUso = await db.ProdutosAcessorio.AnyAsync(p => p.Categoria == categoria.Chave);
        if (emUso)
        {
            categoria.Ativa = false;
            await db.SaveChangesAsync();
            return Ok(new { mensagem = "Categoria em uso por produto(s) — foi desativada em vez de excluída." });
        }

        db.CategoriasAcessorio.Remove(categoria);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Categoria excluída." });
    }

    // ── Catálogo público ─────────────────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar([FromQuery] string? categoria)
    {
        await LiberarPedidosExpiradosAsync();

        var q = db.ProdutosAcessorio.Where(p => p.Ativo);
        if (!string.IsNullOrEmpty(categoria)) q = q.Where(p => p.Categoria == categoria);

        var limiteNovo = DateTime.UtcNow.AddDays(-14);
        var lista = await q.OrderBy(p => p.Ordem).ThenBy(p => p.Nome)
            .Select(p => new
            {
                p.Id,
                p.Nome,
                p.Descricao,
                p.Preco,
                p.PrecoPromocional,
                p.Categoria,
                p.ImagensUrls,
                disponivel = p.Estoque > 0,
                destaque = p.Destaque,
                novo = p.CriadoEm >= limiteNovo,
            })
            .ToListAsync();

        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Buscar(Guid id)
    {
        await LiberarPedidosExpiradosAsync();

        var p = await db.ProdutosAcessorio.FirstOrDefaultAsync(x => x.Id == id && x.Ativo);
        if (p is null) return NotFound();

        var limiteNovo = DateTime.UtcNow.AddDays(-14);
        return Ok(new
        {
            p.Id,
            p.Nome,
            p.Descricao,
            p.Preco,
            p.PrecoPromocional,
            p.Categoria,
            p.ImagensUrls,
            p.PesoKg,
            disponivel = p.Estoque > 0,
            estoque = p.Estoque, // opcional exibir quantidade exata; remova se preferir só "disponível/indisponível"
            destaque = p.Destaque,
            novo = p.CriadoEm >= limiteNovo,
        });
    }

    // ── Gestão (superadmin) ──────────────────────────────────────
    [HttpGet("todos")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ListarTodos()
    {
        var lista = await db.ProdutosAcessorio.OrderBy(p => p.Ordem).ThenBy(p => p.Nome).ToListAsync();
        return Ok(lista);
    }

    public record SalvarProdutoRequest(
        string Nome, string? Descricao, decimal Preco, decimal? PrecoPromocional,
        int Estoque, string Categoria, string? ImagensUrls, decimal? PesoKg, bool Ativo, bool Destaque, int Ordem
    );

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarProdutoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nome) || req.Preco <= 0)
            return BadRequest(new { erro = "Preencha nome e preço válido." });

        var produto = new ProdutoAcessorio
        {
            Nome = req.Nome.Trim(),
            Descricao = req.Descricao,
            Preco = req.Preco,
            PrecoPromocional = req.PrecoPromocional,
            Estoque = req.Estoque,
            Categoria = req.Categoria,
            ImagensUrls = req.ImagensUrls,
            PesoKg = req.PesoKg,
            Ativo = req.Ativo,
            Destaque = req.Destaque,
            Ordem = req.Ordem,
        };
        db.ProdutosAcessorio.Add(produto);
        await db.SaveChangesAsync();

        return Ok(produto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarProdutoRequest req)
    {
        var produto = await db.ProdutosAcessorio.FindAsync(id);
        if (produto is null) return NotFound();

        produto.Nome = req.Nome.Trim();
        produto.Descricao = req.Descricao;
        produto.Preco = req.Preco;
        produto.PrecoPromocional = req.PrecoPromocional;
        produto.Estoque = req.Estoque;
        produto.Categoria = req.Categoria;
        produto.ImagensUrls = req.ImagensUrls;
        produto.PesoKg = req.PesoKg;
        produto.Ativo = req.Ativo;
        produto.Destaque = req.Destaque;
        produto.Ordem = req.Ordem;
        produto.AtualizadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(produto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var produto = await db.ProdutosAcessorio.FindAsync(id);
        if (produto is null) return NotFound();

        var temPedidos = await db.ItensPedidoAcessorio.AnyAsync(i => i.ProdutoId == id);
        if (temPedidos)
        {
            produto.Ativo = false;
            await db.SaveChangesAsync();
            return Ok(new { mensagem = "Produto já usado em pedidos — foi desativado em vez de excluído." });
        }

        db.ProdutosAcessorio.Remove(produto);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Produto excluído." });
    }
}