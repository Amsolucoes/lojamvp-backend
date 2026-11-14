using LojaApi.Application.Acessorios;
using LojaApi.src.Models;

namespace LojaApi.Tests.Suporte.Falsos;

// Repositório da loja de acessórios em memória: permite testar os HANDLERS (regras) sem banco.
// "Salvamentos" conta as gravações pedidas pelo handler (zero = nada foi gravado).
public sealed class RepositorioAcessoriosFalso : IAcessoriosRepository
{
    public List<ProdutoAcessorio> Produtos { get; } = [];
    public List<PedidoAcessorio> Pedidos { get; } = [];
    public List<CategoriaAcessorio> Categorias { get; } = [];
    public List<AvaliacaoAcessorio> Avaliacoes { get; } = [];
    public HashSet<Guid> ProdutosComPedido { get; } = [];
    public HashSet<string> ChavesEmUso { get; } = [];
    public int Salvamentos { get; private set; }

    public Task<List<PedidoAcessorio>> ListarPedidosVencidosAsync(DateTime agora) => Task.FromResult(
        Pedidos.Where(p => p.Status == "aguardando_pagamento" && p.ExpiraEm != null && p.ExpiraEm < agora).ToList());
    public Task<PedidoAcessorio?> ObterPedidoAsync(Guid id) => Task.FromResult(Pedidos.FirstOrDefault(p => p.Id == id));
    public Task<PedidoAcessorio?> ObterPedidoComItensAsync(Guid id) => Task.FromResult(Pedidos.FirstOrDefault(p => p.Id == id));
    public Task<PedidoAcessorio?> ObterPedidoPelaChaveAsync(Guid id) => Task.FromResult(Pedidos.FirstOrDefault(p => p.Id == id));
    public Task<List<PedidoAcessorioResumoDto>> ListarPedidosResumoAsync(string? status) => Task.FromResult(
        Pedidos.Where(p => string.IsNullOrEmpty(status) || p.Status == status).OrderByDescending(p => p.CriadoEm)
            .Select(p => new PedidoAcessorioResumoDto(p.Id, p.ClienteNome, p.ClienteEmail, p.ClienteTelefone, p.ClienteCpfCnpj, p.Cep, p.Endereco, p.Numero, p.Complemento,
                p.Bairro, p.Cidade, p.Uf, p.Subtotal, p.ValorFrete, p.Total, p.Status, p.CodigoRastreio, p.CriadoEm, p.PagoEm, p.EnviadoEm,
                p.Itens.Select(i => new PedidoAcessorioItemResumoDto(i.NomeProduto, i.Quantidade, i.PrecoUnitario, i.Subtotal)).ToList())).ToList());
    public Task<List<PedidoAcessorio>> ListarPedidosComItensAsync(string? status) => Task.FromResult(
        Pedidos.Where(p => string.IsNullOrEmpty(status) || p.Status == status).OrderByDescending(p => p.CriadoEm).ToList());
    public Task<PedidoAcessorio?> ObterUltimoPedidoPagoComProdutoAsync(string emailEmMinusculas, Guid produtoId) => Task.FromResult(
        Pedidos.Where(p => p.Status == "pago" && p.ClienteEmail.ToLower() == emailEmMinusculas && p.Itens.Any(i => i.ProdutoId == produtoId)).OrderByDescending(p => p.PagoEm).FirstOrDefault());
    public void Adicionar(PedidoAcessorio pedido) => Pedidos.Add(pedido);
    public void Remover(PedidoAcessorio pedido) => Pedidos.Remove(pedido);

    public Task<ProdutoAcessorio?> ObterProdutoAsync(Guid id) => Task.FromResult(Produtos.FirstOrDefault(p => p.Id == id));
    public Task<ProdutoAcessorio?> ObterProdutoAtivoAsync(Guid id) => Task.FromResult(Produtos.FirstOrDefault(p => p.Id == id && p.Ativo));
    public Task<List<ProdutoAcessorio>> ListarProdutosAsync() => Task.FromResult(Produtos.OrderBy(p => p.Ordem).ThenBy(p => p.Nome).ToList());
    public Task<List<ProdutoVitrineDto>> ListarVitrineAsync(string? categoria, DateTime limiteNovo) => Task.FromResult(
        Produtos.Where(p => p.Ativo && (string.IsNullOrEmpty(categoria) || p.Categoria == categoria)).OrderBy(p => p.Ordem).ThenBy(p => p.Nome)
            .Select(p => new ProdutoVitrineDto(p.Id, p.Nome, p.Descricao, p.Preco, p.PrecoPromocional, p.Categoria, p.ImagensUrls, p.Estoque > 0, p.Destaque,
                p.CriadoEm >= limiteNovo, 0, 0)).ToList());
    public Task<bool> ProdutoTemPedidosAsync(Guid id) => Task.FromResult(ProdutosComPedido.Contains(id));
    public void Adicionar(ProdutoAcessorio produto) => Produtos.Add(produto);
    public void Remover(ProdutoAcessorio produto) => Produtos.Remove(produto);

    public Task<List<CategoriaAcessorio>> ListarCategoriasAtivasAsync() => Task.FromResult(Categorias.Where(c => c.Ativa).OrderBy(c => c.Ordem).ThenBy(c => c.Nome).ToList());
    public Task<CategoriaAcessorio?> ObterCategoriaAsync(Guid id) => Task.FromResult(Categorias.FirstOrDefault(c => c.Id == id));
    public Task<bool> CategoriaComChaveExisteAsync(string chave) => Task.FromResult(Categorias.Any(c => c.Chave == chave));
    public Task<int> MaiorOrdemDeCategoriaAsync() => Task.FromResult(Categorias.Count == 0 ? -1 : Categorias.Max(c => c.Ordem));
    public Task<bool> CategoriaEmUsoAsync(string chave) => Task.FromResult(ChavesEmUso.Contains(chave));
    public void Adicionar(CategoriaAcessorio categoria) => Categorias.Add(categoria);
    public void Remover(CategoriaAcessorio categoria) => Categorias.Remove(categoria);

    public Task<bool> JaAvaliouAsync(Guid pedidoId, Guid produtoId) => Task.FromResult(Avaliacoes.Any(a => a.PedidoId == pedidoId && a.ProdutoId == produtoId));
    public Task<List<AvaliacaoPublicaDto>> ListarAvaliacoesAsync(Guid produtoId) => Task.FromResult(
        Avaliacoes.Where(a => a.ProdutoId == produtoId).OrderByDescending(a => a.CriadoEm).Select(a => new AvaliacaoPublicaDto(a.Id, a.ClienteNome, a.Nota, a.Comentario, a.CriadoEm)).ToList());
    public void Adicionar(AvaliacaoAcessorio avaliacao) => Avaliacoes.Add(avaliacao);

    public Task SalvarAsync() { Salvamentos++; return Task.CompletedTask; }
}

// Pagamento online "de mentira": o teste diz o que o Mercado Pago responde.
public sealed class PagamentoOnlineAcessorioFalso : IPagamentoOnlineAcessorio
{
    public ResultadoPagamentoAcessorioDto RespostaPix { get; set; } = new("pix-1", "pending", "qr", "qr64", null);
    public ResultadoPagamentoAcessorioDto RespostaCartao { get; set; } = new("cartao-1", "approved", null, null, null);
    public string? StatusConsultado { get; set; }
    public List<decimal> PixCobrados { get; } = [];
    public List<(decimal Valor, int Parcelas)> CartoesCobrados { get; } = [];

    public Task<ResultadoPagamentoAcessorioDto> CriarPix(decimal valor, string descricao, string emailPagador, string cpfPagador, string nomePagador, Guid pagamentoId)
    {
        PixCobrados.Add(valor);
        return Task.FromResult(RespostaPix);
    }

    public Task<ResultadoPagamentoAcessorioDto> CriarCartao(decimal valor, string descricao, string cardToken, int parcelas,
        string emailPagador, string cpfPagador, string nomePagador, Guid pagamentoId)
    {
        CartoesCobrados.Add((valor, parcelas));
        return Task.FromResult(RespostaCartao);
    }

    public Task<string?> VerificarStatus(string mpPaymentId) => Task.FromResult(StatusConsultado);
}
