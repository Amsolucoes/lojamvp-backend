namespace LojaApi.Application.Acessorios;

// O que a loja de acessórios DEVOLVE (os nomes das propriedades viram o JSON da API)

// Resposta do Mercado Pago para o Pix/cartão
public record ResultadoPagamentoAcessorioDto(string? MpPaymentId, string? Status, string? QrCode, string? QrCodeBase64, string? Erro)
{
    public bool Sucesso => Erro == null;
}

// Lista de pedidos do superadmin (resumo, sem os dados do Mercado Pago)
public record PedidoAcessorioResumoDto(
    Guid Id, string ClienteNome, string ClienteEmail, string ClienteTelefone, string? ClienteCpfCnpj,
    string Cep, string Endereco, string? Numero, string? Complemento, string? Bairro, string Cidade, string Uf,
    decimal Subtotal, decimal ValorFrete, decimal Total, string Status, string? CodigoRastreio,
    DateTime CriadoEm, DateTime? PagoEm, DateTime? EnviadoEm, List<PedidoAcessorioItemResumoDto> Itens);

public record PedidoAcessorioItemResumoDto(string NomeProduto, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

// Vitrine pública
public record ProdutoVitrineDto(
    Guid Id, string Nome, string? Descricao, decimal Preco, decimal? PrecoPromocional, string Categoria, string? ImagensUrls,
    bool Disponivel, bool Destaque, bool Novo, double MediaAvaliacoes, int TotalAvaliacoes);

public record AvaliacaoPublicaDto(Guid Id, string ClienteNome, int Nota, string? Comentario, DateTime CriadoEm);
