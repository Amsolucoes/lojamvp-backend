using LojaApi.Application.OrdemServico;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.OrdemServico;

// Oficina: checklist de inspeção, orçamentos e ordens de serviço.
[ApiController]
[Route("api/ordemservico")]
[Authorize]
public class OrdemServicoController(ILojaAtual lojaAtual) : ControllerBase
{
    // ══════════════ Checklist — categorias e itens ══════════════

    [HttpGet("checklist-categorias")]
    public async Task<IActionResult> ListarChecklistCategorias([FromServices] IQueryHandler<ListarChecklistCategoriasQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpPost("checklist-categorias")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> CriarChecklistCategoria([FromBody] SalvarChecklistCategoriaRequest req, [FromServices] ICommandHandler<CriarChecklistCategoriaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPut("checklist-categorias/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AtualizarChecklistCategoria(Guid id, [FromBody] SalvarChecklistCategoriaRequest req, [FromServices] ICommandHandler<AtualizarChecklistCategoriaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpDelete("checklist-categorias/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> ExcluirChecklistCategoria(Guid id, [FromServices] ICommandHandler<ExcluirChecklistCategoriaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPost("checklist-itens")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> CriarChecklistItem([FromBody] SalvarChecklistItemRequest req, [FromServices] ICommandHandler<CriarChecklistItemCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPut("checklist-itens/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AtualizarChecklistItem(Guid id, [FromBody] SalvarChecklistItemRequest req, [FromServices] ICommandHandler<AtualizarChecklistItemCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpDelete("checklist-itens/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> ExcluirChecklistItem(Guid id, [FromServices] ICommandHandler<ExcluirChecklistItemCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    // ══════════════ Orçamento / Ordem de Serviço ══════════════

    [HttpGet("orcamentos")]
    public async Task<IActionResult> Listar([FromQuery] string? status, [FromQuery] string? placa, [FromServices] IQueryHandler<ListarOrcamentosQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), status, placa))).ParaResposta(this);

    [HttpGet("orcamentos/{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id, [FromServices] IQueryHandler<BuscarOrcamentoQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    // Histórico de ordens de serviço de um cliente (usado na tela de Clientes)
    [HttpGet("orcamentos/cliente/{clienteId:guid}")]
    public async Task<IActionResult> ListarPorCliente(Guid clienteId, [FromServices] IQueryHandler<ListarOrcamentosDoClienteQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), clienteId))).ParaResposta(this);

    // Resumo (qtd + total) de ordens por cliente — usado nos cards da lista de Clientes, pra sinalizar quem já teve OS
    [HttpGet("resumo-clientes")]
    public async Task<IActionResult> ResumoClientes([FromServices] IQueryHandler<ResumoClientesOrdemQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpPost("orcamentos")]
    public async Task<IActionResult> Criar([FromBody] CriarOrcamentoRequest req, [FromServices] ICommandHandler<CriarOrcamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    // Editar (só pendente ou em_andamento — antes de concluir)
    [HttpPut("orcamentos/{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] CriarOrcamentoRequest req, [FromServices] ICommandHandler<AtualizarOrcamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    // Aprovar (pendente → em_andamento)
    [HttpPatch("orcamentos/{id:guid}/aprovar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Aprovar(Guid id, [FromServices] ICommandHandler<AprovarOrcamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    // Reprovar / Cancelar
    [HttpPatch("orcamentos/{id:guid}/cancelar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Cancelar(Guid id, [FromServices] ICommandHandler<CancelarOrcamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    // Desfazer conclusão — reverte estoque, financeiro e comissão
    [HttpPatch("orcamentos/{id:guid}/desfazer-conclusao")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> DesfazerConclusao(Guid id, [FromServices] ICommandHandler<DesfazerConclusaoOrcamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    // Reabrir uma ordem cancelada — volta pra "em andamento"
    [HttpPatch("orcamentos/{id:guid}/reabrir")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Reabrir(Guid id, [FromServices] ICommandHandler<ReabrirOrcamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    // Concluir: gera Financeiro (a receber) + comissão por mecânico + baixa estoque
    [HttpPatch("orcamentos/{id:guid}/concluir")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Concluir(Guid id, [FromBody] ConcluirOrcamentoRequest req, [FromServices] ICommandHandler<ConcluirOrcamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    // Enviar orçamento por e-mail pro cliente
    [HttpPost("orcamentos/{id:guid}/enviar-email")]
    public async Task<IActionResult> EnviarEmail(Guid id, [FromServices] ICommandHandler<EnviarOrcamentoPorEmailCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    // Baixar orçamento em PDF (A4)
    [HttpGet("orcamentos/{id:guid}/pdf")]
    public async Task<IActionResult> BaixarPdf(Guid id, [FromServices] IQueryHandler<BaixarOrcamentoPdfQuery> handler)
    {
        var resultado = await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id));
        return resultado.Sucesso ? ComoArquivo((ArquivoPdfDto)resultado.Valor!) : resultado.ParaResposta(this);
    }

    // Ajustar manualmente a data/hora de entrada (aprovação) e saída (conclusão)
    [HttpPatch("orcamentos/{id:guid}/datas")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AjustarDatas(Guid id, [FromBody] AjustarDatasRequest req, [FromServices] ICommandHandler<AjustarDatasOrcamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    // Excluir (só se ainda pendente)
    [HttpDelete("orcamentos/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Excluir(Guid id, [FromServices] ICommandHandler<ExcluirOrcamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    private FileContentResult ComoArquivo(ArquivoPdfDto arquivo) => File(arquivo.Pdf, "application/pdf", arquivo.NomeArquivo);
}
