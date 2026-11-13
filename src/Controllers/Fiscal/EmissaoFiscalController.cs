using System.Security.Claims;
using LojaApi.Application.Fiscal;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Fiscal;

[ApiController]
[Route("api/fiscal")]
[Authorize]
public class EmissaoFiscalController(FiscalService servico, ILojaAtual lojaAtual) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Configuração fiscal da loja ──────────────────────────────────────
    [HttpGet("configuracao")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> BuscarConfiguracao() =>
        (await servico.BuscarConfiguracaoAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPut("configuracao")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> SalvarConfiguracao([FromBody] SalvarConfiguracaoFiscalRequest req) =>
        (await servico.SalvarConfiguracaoAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    // ── Emissão ──────────────────────────────────────────────────────────
    [HttpPost("vendas/{vendaId:guid}/emitir")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Emitir(Guid vendaId) =>
        (await servico.EmitirAsync(await lojaAtual.ObterIdAsync(User), UsuarioId, vendaId)).ParaResposta(this);

    [HttpGet("vendas/{vendaId:guid}/emissoes")]
    public async Task<IActionResult> ListarEmissoes(Guid vendaId) =>
        (await servico.ListarEmissoesAsync(await lojaAtual.ObterIdAsync(User), vendaId)).ParaResposta(this);

    [HttpPost("emissoes/{emissaoId:guid}/cancelar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Cancelar(Guid emissaoId, [FromBody] CancelarEmissaoRequest req) =>
        (await servico.CancelarAsync(await lojaAtual.ObterIdAsync(User), emissaoId, req)).ParaResposta(this);
}
