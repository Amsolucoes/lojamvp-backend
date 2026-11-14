namespace LojaApi.Application.OrdemServico;

// O que OrdemServico DEVOLVE (os nomes das propriedades viram o JSON da API)

// Checklist
public record ChecklistItemListaDto(Guid Id, string Nome, int Ordem, bool Ativo);
public record ChecklistCategoriaDto(Guid Id, string Nome, int Ordem, bool Ativa, List<ChecklistItemListaDto> Itens);

// Lista de orçamentos
public record OrcamentoListaDto(
    Guid Id, Guid ClienteId, string? VeiculoDescricao, string? Placa, string Status, decimal ValorTotal,
    DateTime CriadoEm, DateTime? AprovadoEm, DateTime? ConcluidoEm, int QtdMecanicos, List<string> NomesMecanicos);

// Detalhe de um orçamento
public record ItemOrcamentoDto(Guid Id, string Tipo, Guid? ProdutoId, Guid? ServicoId, string Descricao, int Quantidade, decimal ValorUnitario, decimal ValorTotal);
public record MecanicoOrcamentoDto(Guid Id, Guid ProfissionalId, string NomeProfissional, decimal ComissaoPercentual);
public record ChecklistRespostaDto(Guid Id, Guid ChecklistItemId, string NomeItem, string Estado, string? Observacao);

public record OrcamentoDetalheDto(
    Guid Id, Guid ClienteId, string? VeiculoDescricao, string? Placa, string Status, string? Observacoes, decimal ValorTotal,
    DateTime CriadoEm, DateTime? AprovadoEm, DateTime? ConcluidoEm,
    List<ItemOrcamentoDto> Itens, List<MecanicoOrcamentoDto> Mecanicos, List<ChecklistRespostaDto> Checklist);

// Histórico e resumo por cliente
public record ItemDoClienteDto(string Descricao, int Quantidade, decimal ValorTotal);
public record OrcamentoDoClienteDto(
    Guid Id, string? VeiculoDescricao, string? Placa, string Status, decimal ValorTotal, DateTime CriadoEm, DateTime? ConcluidoEm, List<ItemDoClienteDto> Itens);
public record ResumoClienteOrdemDto(Guid ClienteId, int Qtd, decimal Total, bool TemAndamento);

// Patrimônio
public record ItemPatrimonioListaDto(Guid Id, string Nome, string? Categoria, int QuantidadeEsperada, decimal ValorUnitario, string? Observacao, bool Ativo, decimal ValorTotal);
public record ContagemListaDto(Guid Id, DateTime DataContagem, string? Responsavel, string? Observacao, int QtdItens, int QtdDivergentes);
public record ItemContagemDetalheDto(
    Guid Id, Guid ItemPatrimonioId, string NomeItem, string? Categoria, int QuantidadeEsperadaNoMomento, int QuantidadeContada, int Diferenca, decimal ValorUnitario, string? Observacao);
public record ContagemDetalheDto(Guid Id, DateTime DataContagem, string? Responsavel, string? Observacao, List<ItemContagemDetalheDto> Itens);

// Arquivo PDF pronto para baixar
public record ArquivoPdfDto(byte[] Pdf, string NomeArquivo);

// Resultado do envio do orçamento por e-mail
public record ResultadoEnvioOrcamentoDto(bool Enviado, string? Erro);
