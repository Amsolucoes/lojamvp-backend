namespace LojaApi.Application.Plataforma;

// Consultas do painel do superadmin (cada uma tem o seu handler em Application/Handlers/Plataforma).

public record DashboardAdminQuery();
public record ListarLojasAdminQuery(string? Status, string? Busca);
public record BuscarLojaAdminQuery(Guid Id);
public record ListarPagamentosDaLojaQuery(Guid Id);
public record ListarComunicadosQuery();
public record BackupDaLojaQuery(Guid Id);
