using LojaApi.Application.Chacara;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Chacara;

[ApiController]
[Route("api/chacara")]
[Authorize]
public class ChacaraDashboardController(DashboardChacaraService servico, ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] int mesesAFrente = 6, [FromQuery] int? ano = null) =>
        (await servico.ObterAsync(await lojaAtual.ObterIdAsync(User), mesesAFrente, ano)).ParaResposta(this);

}
