using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.src.Models;
using LojaApi.Services;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/loja-acessorios")]
public class PedidosAcessorioController(AppDbContext db, MercadoPagoService mpService) : ControllerBase
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

    // ── Criar pedido + processar pagamento com cartão (via Payment Brick) ──
    public record CriarPedidoCartaoRequest(
        string ClienteNome, string ClienteEmail, string ClienteTelefone, string? ClienteCpfCnpj,
        string Cep, string Endereco, string? Numero, string? Complemento, string? Bairro, string Cidade, string Uf,
        List<ItemPedidoRequest> Itens,
        string Token, string PaymentMethodId, int Installments, string IssuerId
    );

    [HttpPost("pedidos/cartao")]
    [AllowAnonymous]
    public async Task<IActionResult> CriarPedidoCartao([FromBody] CriarPedidoCartaoRequest req)
    {
        await LiberarPedidosExpiradosAsync();

        if (req.Itens.Count == 0)
            return BadRequest(new { erro = "O pedido precisa ter ao menos um item." });

        if (string.IsNullOrWhiteSpace(req.ClienteCpfCnpj))
            return BadRequest(new { erro = "CPF é obrigatório." });

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
            if (produto is null) return BadRequest(new { erro = "Produto não encontrado." });
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

            produto.Estoque -= item.Quantidade;
        }

        var valorFrete = FRETE_POR_UF.TryGetValue(pedido.Uf, out var vf) ? vf : FRETE_PADRAO;
        pedido.Subtotal = subtotal;
        pedido.ValorFrete = valorFrete;
        pedido.Total = subtotal + valorFrete;

        db.PedidosAcessorio.Add(pedido);
        await db.SaveChangesAsync();

        var resultado = await mpService.CriarCartao(
            valor: pedido.Total,
            descricao: $"Pedido AlDevSoftware #{pedido.Id.ToString()[..8]}",
            cardToken: req.Token,
            parcelas: req.Installments,
            emailPagador: pedido.ClienteEmail,
            cpfPagador: pedido.ClienteCpfCnpj!,
            nomePagador: pedido.ClienteNome,
            pagamentoId: pedido.Id
        );

        if (!resultado.Sucesso || resultado.Status == "rejected")
        {
            // Desfaz a reserva de estoque se o pagamento falhar/for recusado
            foreach (var item in pedido.Itens)
            {
                var produto = await db.ProdutosAcessorio.FindAsync(item.ProdutoId);
                if (produto != null) produto.Estoque += item.Quantidade;
            }
            db.PedidosAcessorio.Remove(pedido);
            await db.SaveChangesAsync();
            return BadRequest(new { erro = resultado.Erro ?? "Pagamento recusado pela operadora do cartão. Confira os dados e tente novamente." });
        }

        pedido.MpPaymentId = resultado.MpPaymentId;
        pedido.MpStatus = resultado.Status;

        // Cartão costuma aprovar na hora — já marca como pago se vier "approved"
        if (resultado.Status == "approved")
        {
            pedido.Status = "pago";
            pedido.PagoEm = DateTime.UtcNow;
            pedido.ExpiraEm = null;
        }

        await db.SaveChangesAsync();

        return Ok(new { pedido.Id, pedido.Status, pedido.Total });
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
        var q = db.PedidosAcessorio.AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(p => p.Status == status);

        var lista = await q.OrderByDescending(p => p.CriadoEm)
            .Select(p => new
            {
                p.Id,
                p.ClienteNome,
                p.ClienteEmail,
                p.ClienteTelefone,
                p.ClienteCpfCnpj,
                p.Cep,
                p.Endereco,
                p.Numero,
                p.Complemento,
                p.Bairro,
                p.Cidade,
                p.Uf,
                p.Subtotal,
                p.ValorFrete,
                p.Total,
                p.Status,
                p.CodigoRastreio,
                p.CriadoEm,
                p.PagoEm,
                p.EnviadoEm,
                Itens = p.Itens.Select(i => new
                {
                    i.NomeProduto,
                    i.Quantidade,
                    i.PrecoUnitario,
                    i.Subtotal,
                }),
            })
            .ToListAsync();

        return Ok(lista);
    }

    public record AtualizarStatusPedidoRequest(string Status, string? CodigoRastreio);

    [HttpPatch("pedidos/{id:guid}/status")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> AtualizarStatusPedido(Guid id, [FromBody] AtualizarStatusPedidoRequest req)
    {
        var pedido = await db.PedidosAcessorio.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id);
        if (pedido is null) return NotFound();

        var statusValidos = new[] { "aguardando_pagamento", "pago", "enviado", "entregue", "cancelado" };
        if (!statusValidos.Contains(req.Status))
            return BadRequest(new { erro = "Status inválido." });

        // Ao cancelar (e ele ainda não estava cancelado), devolve o estoque na hora —
        // sem isso, ficaria preso até a expiração automática de 30min.
        if (req.Status == "cancelado" && pedido.Status != "cancelado")
        {
            foreach (var item in pedido.Itens)
            {
                var produto = await db.ProdutosAcessorio.FindAsync(item.ProdutoId);
                if (produto != null) produto.Estoque += item.Quantidade;
            }
        }

        pedido.Status = req.Status;
        if (req.CodigoRastreio != null) pedido.CodigoRastreio = req.CodigoRastreio;
        if (req.Status == "enviado") pedido.EnviadoEm = DateTime.UtcNow;
        pedido.ExpiraEm = null; // já não precisa mais do prazo, seja pago ou cancelado

        await db.SaveChangesAsync();

        return Ok(new
        {
            pedido.Id,
            pedido.ClienteNome,
            pedido.Status,
            pedido.CodigoRastreio,
            pedido.PagoEm,
            pedido.EnviadoEm,
        });
    }
}