namespace LojaApi.Application.Financeiro;

// O que a API do Financeiro (Planejamento e troca de categoria) RECEBE (os nomes das propriedades são o JSON do corpo)

public record PlanejamentoConfigRequest(decimal? ReceitaPlanejada, decimal PercentualPoupanca);
public record PlanejamentoCategoriaItem(Guid CategoriaId, bool Ativo, decimal Valor, string? Tipo = "pagar");
public record PlanejamentoCategoriasRequest(List<PlanejamentoCategoriaItem> Itens);
public record MetaPlanejamentoRequest(string Nome, string? Icone, decimal ValorAlvo, decimal ValorGuardado, DateTime? DataAlvo);
public record AporteMetaRequest(decimal Valor);
public record TrocarCategoriaRequest(Guid? CategoriaId, bool TodasParcelas = false);
