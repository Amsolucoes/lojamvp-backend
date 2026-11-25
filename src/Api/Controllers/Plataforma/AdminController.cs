using System.Security.Claims;
using LojaApi.Application.Plataforma;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Plataforma;

// Painel do superadmin: dashboard, lojas, pagamentos manuais, bloqueios, comunicados, acesso como suporte, backup e exclusão.
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "superadmin")]
public class AdminController : ControllerBase
{
    private Guid AdminId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromServices] IQueryHandler<DashboardAdminQuery> handler) =>
        (await handler.HandleAsync(new())).ParaResposta(this);

    [HttpGet("lojas")]
    public async Task<IActionResult> Listar([FromServices] IQueryHandler<ListarLojasAdminQuery> handler, [FromQuery] string? status, [FromQuery] string? busca) =>
        (await handler.HandleAsync(new(status, busca))).ParaResposta(this);

    [HttpGet("lojas/{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id, [FromServices] IQueryHandler<BuscarLojaAdminQuery> handler) =>
        (await handler.HandleAsync(new(id))).ParaResposta(this);

    [HttpPost("lojas")]
    public async Task<IActionResult> Criar([FromBody] CriarLojaRequest req, [FromServices] ICommandHandler<CriarLojaAdminCommand> handler)
    {
        var resultado = await handler.HandleAsync(new(req));
        return resultado.Sucesso
            ? CreatedAtAction(nameof(Buscar), new { id = ((LojaDto)resultado.Valor!).Id }, resultado.Valor)
            : resultado.ParaResposta(this);
    }

    // Backup completo da loja (JSON)
    [HttpGet("lojas/{id:guid}/backup")]
    public async Task<IActionResult> Backup(Guid id, [FromServices] IQueryHandler<BackupDaLojaQuery> handler)
    {
        var resultado = await handler.HandleAsync(new(id));
        if (!resultado.Sucesso) return resultado.ParaResposta(this);
        var arquivo = (ArquivoDto)resultado.Valor!;
        return File(arquivo.Conteudo, arquivo.TipoConteudo, arquivo.NomeArquivo);
    }

    // Deletar loja (e todos os dados)
    [HttpDelete("lojas/{id:guid}")]
    public async Task<IActionResult> Deletar(Guid id, [FromServices] ICommandHandler<ExcluirLojaCommand> handler) =>
        (await handler.HandleAsync(new(id))).ParaResposta(this);

    // Deletar apenas produtos, vendas, trocas e ajustes de caixa (mantém clientes, usuários e a loja)
    [HttpDelete("lojas/{id:guid}/produtos-e-vendas")]
    public async Task<IActionResult> DeletarProdutosEVendas(Guid id, [FromServices] ICommandHandler<ExcluirProdutosEVendasCommand> handler) =>
        (await handler.HandleAsync(new(id))).ParaResposta(this);

    // Acessar loja como suporte (gera token do admin da loja)
    [HttpPost("lojas/{id:guid}/acessar")]
    public async Task<IActionResult> AcessarComoSuporte(Guid id, [FromServices] ICommandHandler<AcessarComoSuporteCommand> handler) =>
        (await handler.HandleAsync(new(id))).ParaResposta(this);

    [HttpPut("lojas/{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarLojaRequest req, [FromServices] ICommandHandler<AtualizarLojaAdminCommand> handler) =>
        (await handler.HandleAsync(new(id, req))).ParaResposta(this);

    // Alterar status (bloquear/desbloquear)
    [HttpPatch("lojas/{id:guid}/status")]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusLojaRequest req, [FromServices] ICommandHandler<AlterarStatusLojaCommand> handler) =>
        (await handler.HandleAsync(new(id, req))).ParaResposta(this);

    // Registrar pagamento manual
    [HttpPost("pagamentos")]
    public async Task<IActionResult> RegistrarPagamento([FromBody] RegistrarPagamentoManualRequest req, [FromServices] ICommandHandler<RegistrarPagamentoManualCommand> handler) =>
        (await handler.HandleAsync(new(AdminId, req))).ParaResposta(this);

    // Faturas de todas as lojas, com filtros (status, loja, busca, ano/mês de vencimento, forma) e página
    [HttpGet("pagamentos")]
    public async Task<IActionResult> ListarPagamentos([FromServices] IQueryHandler<ListarPagamentosAdminQuery> handler,
        [FromQuery] string? status, [FromQuery] Guid? lojaId, [FromQuery] string? busca, [FromQuery] int? ano, [FromQuery] int? mes,
        [FromQuery] string? forma, [FromQuery] bool incluirTeste = false, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 20) =>
        (await handler.HandleAsync(new(status, lojaId, busca, ano, mes, forma, incluirTeste, pagina, tamanho))).ParaResposta(this);

    [HttpGet("lojas/{id:guid}/pagamentos")]
    public async Task<IActionResult> Pagamentos(Guid id, [FromServices] IQueryHandler<ListarPagamentosDaLojaQuery> handler) =>
        (await handler.HandleAsync(new(id))).ParaResposta(this);

    [HttpPatch("lojas/{id}/plano")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> AtualizarPlano(Guid id, [FromBody] AtualizarPlanoRequest req, [FromServices] ICommandHandler<AtualizarPlanoLojaCommand> handler) =>
        (await handler.HandleAsync(new(id, req))).ParaResposta(this);

    // Trocar e-mail (login do usuário e/ou contato da loja)
    [HttpPatch("lojas/{id:guid}/email")]
    public async Task<IActionResult> TrocarEmail(Guid id, [FromBody] TrocarEmailRequest req, [FromServices] ICommandHandler<TrocarEmailLojaCommand> handler) =>
        (await handler.HandleAsync(new(id, req))).ParaResposta(this);

    // Atualizar valor da mensalidade (e opcionalmente a assinatura)
    [HttpPatch("lojas/{id:guid}/valor")]
    public async Task<IActionResult> AtualizarValor(Guid id, [FromBody] AtualizarValorRequest req, [FromServices] ICommandHandler<AtualizarValorLojaCommand> handler) =>
        (await handler.HandleAsync(new(id, req))).ParaResposta(this);

    // Disparar verificação de bloqueio manualmente (teste/admin)
    [HttpPost("verificar-bloqueios")]
    public async Task<IActionResult> VerificarBloqueios([FromServices] ICommandHandler<VerificarBloqueiosCommand> handler) =>
        (await handler.HandleAsync(new())).ParaResposta(this);

    [HttpGet("comunicados")]
    public async Task<IActionResult> ListarComunicados([FromServices] IQueryHandler<ListarComunicadosQuery> handler) =>
        (await handler.HandleAsync(new())).ParaResposta(this);

    // Enviar comunicado por e-mail para lojas (novidades, avisos, etc.)
    [HttpPost("comunicados/enviar")]
    public async Task<IActionResult> EnviarComunicado([FromBody] EnviarComunicadoRequest req, [FromServices] ICommandHandler<EnviarComunicadoCommand> handler) =>
        (await handler.HandleAsync(new(req))).ParaResposta(this);

    [HttpPatch("lojas/{id:guid}/trial")]
    public async Task<IActionResult> AlterarTrial(Guid id, [FromBody] AlterarTrialRequest req, [FromServices] ICommandHandler<AlterarTrialCommand> handler) =>
        (await handler.HandleAsync(new(id, req))).ParaResposta(this);
}
