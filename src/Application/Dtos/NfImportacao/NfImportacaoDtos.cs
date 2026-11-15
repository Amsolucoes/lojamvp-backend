namespace LojaApi.Application.NfImportacao;

// O que NfImportacao DEVOLVE (os nomes das propriedades viram o JSON da API)

public record ItemNfPreview(
    string CodigoFornecedor, string? Gtin, string Descricao,
    string NomeBase, string? Cor, string? Tamanho,
    decimal Quantidade, decimal ValorUnitario, decimal ValorTotal,
    string StatusMatch, // "gtin" | "mapeamento" | "nome_exato" | "sugestao" | "novo"
    Guid? ProdutoSugeridoId, string? ProdutoSugeridoNome,
    bool VariacaoJaExiste, int? EstoqueVariacaoAtual,
    string CategoriaSugerida, bool CategoriaJaExiste
);

public record NfPreviewResponse(string CnpjFornecedor, string NomeFornecedor, string NumeroNf, string ChaveAcesso, List<ItemNfPreview> Itens);

public record ImportacaoConcluidaDto(string Mensagem, int ProdutosNovos, int ProdutosAtualizados, int CategoriasCriadas);
public record NfLancadaDto(string Mensagem, int ProdutosAtualizados);
public record MensagemDto(string Mensagem);

public record NfManualItemDto(Guid ProdutoId, string NomeProduto, Guid? VariacaoId, string? VariacaoLabel, decimal Quantidade, decimal PrecoCusto);

public record NfManualDetalheDto(Guid Id, Guid? FornecedorId, string NumeroNf, DateTime? DataEmissao, decimal? ValorTotal, List<NfManualItemDto> Itens);

public record NfHistoricoDto(
    Guid Id, string NumeroNf, string NomeFornecedor, Guid? FornecedorId, string Origem, DateTime? DataEmissao,
    decimal? ValorTotal, decimal? ValorCustoTotal, decimal? ValorVendaTotal, decimal? QuantidadeTotal,
    int QtdItens, DateTime ImportadoEm, bool Desfeita
);
