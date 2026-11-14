namespace LojaApi.Application.Acessorios;

// O que a loja de acessórios RECEBE no corpo da requisição

public record ItemPedidoRequest(Guid ProdutoId, int Quantidade);

public record CriarPedidoRequest(
    string ClienteNome, string ClienteEmail, string ClienteTelefone, string? ClienteCpfCnpj,
    string Cep, string Endereco, string? Numero, string? Complemento, string? Bairro, string Cidade, string Uf,
    List<ItemPedidoRequest> Itens
);

public record CriarPedidoCartaoRequest(
    string ClienteNome, string ClienteEmail, string ClienteTelefone, string? ClienteCpfCnpj,
    string Cep, string Endereco, string? Numero, string? Complemento, string? Bairro, string Cidade, string Uf,
    List<ItemPedidoRequest> Itens,
    string Token, string PaymentMethodId, int Installments, string IssuerId
);

public record AtualizarStatusPedidoRequest(string Status, string? CodigoRastreio);

public record CriarAvaliacaoRequest(Guid ProdutoId, string Email, int Nota, string? Comentario);

public record SalvarCategoriaAcessorioRequest(string Nome);

public record SalvarProdutoAcessorioRequest(
    string Nome, string? Descricao, decimal Preco, decimal? PrecoPromocional,
    int Estoque, string Categoria, string? ImagensUrls, decimal? PesoKg, bool Ativo, bool Destaque, int Ordem
);
