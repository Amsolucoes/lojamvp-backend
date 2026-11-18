namespace LojaApi.Application.Financeiro;

// Consultas dos relatórios do Financeiro e do Planejamento: só leem (cada uma tem o seu handler em Application/Handlers/Financeiro).
// LojaId nulo = usuário sem vínculo com loja.

public record ResumoMensalQuery(Guid? LojaId, int Ano, int Mes);
public record BalancoPorCategoriaQuery(Guid? LojaId, int Ano, int Mes);
public record BalancoDetalheQuery(Guid? LojaId, int Ano, int Mes, string Categoria, bool Desmembrar);
public record BalancoComparativoQuery(Guid? LojaId, int Ano, int Mes, bool Desmembrar);
public record ResumoAnualQuery(Guid? LojaId, int Ano);
public record AlertasVencimentoQuery(Guid? LojaId, int Dias);
public record CartoesResumoQuery(Guid? LojaId);
public record PlanejamentoQuery(Guid? LojaId, int Ano, int Mes);
