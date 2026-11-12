using LojaApi.Modules.Chacara.Application;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Modules.Chacara.Api;

[ApiController]
[Route("api/chacara/configuracao-preco")]
[Authorize]
public class ConfiguracaoPrecoChacaraController(ConfiguracaoPrecoChacaraService servico, ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obter() =>
        (await servico.ObterAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPut]
    public async Task<IActionResult> Atualizar([FromBody] AtualizarConfiguracaoPrecoRequest req) =>
        (await servico.AtualizarAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

}
