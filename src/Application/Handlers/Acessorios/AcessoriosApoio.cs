using LojaApi.Domain.Acessorios;
using LojaApi.src.Models;

namespace LojaApi.Application.Acessorios;

// Dados do cliente e itens que chegam para criar um pedido (Pix ou cartão)
public record DadosDoPedido(
    string ClienteNome, string ClienteEmail, string ClienteTelefone, string? ClienteCpfCnpj,
    string Cep, string Endereco, string? Numero, string? Complemento, string? Bairro, string Cidade, string Uf,
    List<ItemPedidoRequest> Itens);

// O que vários handlers da loja de acessórios compartilham: liberar pedidos vencidos, devolver estoque e montar o pedido.
public static class AcessoriosApoio
{
    // Libera de volta o estoque de pedidos que passaram do prazo sem pagar.
    // Chamado nos endpoints públicos, evitando precisar de um job em segundo plano.
    public static async Task LiberarPedidosExpiradosAsync(IAcessoriosRepository repositorio)
    {
        var agora = DateTime.UtcNow;
        var expirados = await repositorio.ListarPedidosVencidosAsync(agora);

        if (expirados.Count == 0) return;

        foreach (var pedido in expirados)
        {
            pedido.Status = StatusPedidoAcessorio.Cancelado;
            await DevolverEstoqueAsync(repositorio, pedido);
        }
        await repositorio.SalvarAsync();
    }

    public static async Task DevolverEstoqueAsync(IAcessoriosRepository repositorio, PedidoAcessorio pedido)
    {
        foreach (var item in pedido.Itens)
        {
            var produto = await repositorio.ObterProdutoAsync(item.ProdutoId);
            if (produto != null) produto.Estoque += item.Quantidade;
        }
    }

    // Monta o pedido e já reserva o estoque dos produtos (evita concorrência de dois pedidos pro mesmo item).
    // Nada é gravado aqui: se algum item for inválido volta só a mensagem de erro.
    public static async Task<(PedidoAcessorio? Pedido, string? Erro)> MontarPedidoAsync(IAcessoriosRepository repositorio, DadosDoPedido dados)
    {
        var pedido = new PedidoAcessorio
        {
            ClienteNome = dados.ClienteNome.Trim(),
            ClienteEmail = dados.ClienteEmail.Trim(),
            ClienteTelefone = dados.ClienteTelefone.Trim(),
            ClienteCpfCnpj = dados.ClienteCpfCnpj?.Trim(),
            Cep = dados.Cep.Trim(),
            Endereco = dados.Endereco.Trim(),
            Numero = dados.Numero,
            Complemento = dados.Complemento,
            Bairro = dados.Bairro,
            Cidade = dados.Cidade.Trim(),
            Uf = dados.Uf.Trim().ToUpper(),
            ExpiraEm = DateTime.UtcNow.Add(AcessoriosRegras.PrazoPagamento),
        };

        decimal subtotal = 0;
        foreach (var item in dados.Itens)
        {
            var produto = await repositorio.ObterProdutoAtivoAsync(item.ProdutoId);
            if (produto is null) return (null, "Produto não encontrado.");
            if (produto.Estoque < item.Quantidade)
                return (null, $"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.Estoque}.");

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

            // Reserva o estoque já na criação do pedido
            produto.Estoque -= item.Quantidade;
        }

        var valorFrete = AcessoriosRegras.CalcularFrete(subtotal, pedido.Uf);
        pedido.Subtotal = subtotal;
        pedido.ValorFrete = valorFrete;
        pedido.Total = subtotal + valorFrete;

        return (pedido, null);
    }

    public static string DescricaoDoPedido(PedidoAcessorio pedido) => $"Pedido AlDevSoftware #{pedido.Id.ToString()[..8]}";
}
