using LojaApi.Modules.Chacara.Application;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Modules.Chacara.Api;

[ApiController]
[Route("api/chacara/fotos")]
[Authorize]
public class FotoChacaraController(FotosChacaraService servico, ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() =>
        (await servico.ListarAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPost]
    public async Task<IActionResult> Adicionar([FromBody] AdicionarFotoRequest req) =>
        (await servico.AdicionarAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id) =>
        (await servico.RemoverAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    [HttpPut("ordem")]
    public async Task<IActionResult> Reordenar([FromBody] ReordenarFotosRequest req) =>
        (await servico.ReordenarAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

}
