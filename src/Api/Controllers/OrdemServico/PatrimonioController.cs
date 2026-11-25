using LojaApi.Application.OrdemServico;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.OrdemServico;

// Patrimônio da oficina (ferramentas e equipamentos): cadastro, contagens e PDFs.
[ApiController]
[Route("api/ordemservico/patrimonio")]
[Authorize]
public class PatrimonioController(ILojaAtual lojaAtual) : ControllerBase
{
    // ══════════════ Itens de patrimônio (cadastro) ══════════════

    [HttpGet("itens")]
    public async Task<IActionResult> ListarItens([FromServices] IQueryHandler<ListarItensPatrimonioQuery> handler, [FromQuery] bool incluirInativos = false) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), incluirInativos))).ParaResposta(this);

    [HttpPost("itens")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> CriarItem([FromBody] SalvarItemPatrimonioRequest req, [FromServices] ICommandHandler<CriarItemPatrimonioCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPut("itens/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AtualizarItem(Guid id, [FromBody] SalvarItemPatrimonioRequest req, [FromServices] ICommandHandler<AtualizarItemPatrimonioCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpDelete("itens/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> ExcluirItem(Guid id, [FromServices] ICommandHandler<ExcluirItemPatrimonioCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    // ══════════════ Contagens ══════════════

    [HttpGet("contagens")]
    public async Task<IActionResult> ListarContagens([FromServices] IQueryHandler<ListarContagensPatrimonioQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpGet("contagens/{id:guid}")]
    public async Task<IActionResult> BuscarContagem(Guid id, [FromServices] IQueryHandler<BuscarContagemPatrimonioQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPost("contagens")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> CriarContagem([FromBody] CriarContagemRequest req, [FromServices] ICommandHandler<CriarContagemPatrimonioCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpDelete("contagens/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> ExcluirContagem(Guid id, [FromServices] ICommandHandler<ExcluirContagemPatrimonioCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    // ══════════════ PDF ══════════════

    // PDF do cadastro atual (lista de itens + valor total de patrimônio)
    [HttpGet("relatorio-pdf")]
    public async Task<IActionResult> RelatorioPdf([FromServices] IQueryHandler<RelatorioPatrimonioPdfQuery> handler)
    {
        var resultado = await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)));
        return resultado.Sucesso ? ComoArquivo((ArquivoPdfDto)resultado.Valor!) : resultado.ParaResposta(this);
    }

    // PDF de uma contagem específica, com comparativo esperado x contado
    [HttpGet("contagens/{id:guid}/pdf")]
    public async Task<IActionResult> ContagemPdf(Guid id, [FromServices] IQueryHandler<ContagemPatrimonioPdfQuery> handler)
    {
        var resultado = await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id));
        return resultado.Sucesso ? ComoArquivo((ArquivoPdfDto)resultado.Valor!) : resultado.ParaResposta(this);
    }

    private FileContentResult ComoArquivo(ArquivoPdfDto arquivo) => File(arquivo.Pdf, "application/pdf", arquivo.NomeArquivo);
}
