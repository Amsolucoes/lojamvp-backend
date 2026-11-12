using LojaApi.Modules.Chacara.Application;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Modules.Chacara.Api;

[ApiController]
[Route("api/chacara/info")]
[Authorize]
public class InfoChacaraController(InfoChacaraService servico, ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obter() =>
        (await servico.ObterAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPut]
    public async Task<IActionResult> Atualizar([FromBody] AtualizarInfoRequest req) =>
        (await servico.AtualizarAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    [HttpGet("pagamento")]
    public async Task<IActionResult> ObterPagamento() =>
        (await servico.ObterPagamentoAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPut("pagamento")]
    public async Task<IActionResult> SalvarPagamento([FromBody] SalvarPagamentoRequest req) =>
        (await servico.SalvarPagamentoAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    [HttpDelete("pagamento")]
    public async Task<IActionResult> RemoverPagamento() =>
        (await servico.RemoverPagamentoAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

}
