using LojaApi.Modules.Chacara.Application;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Modules.Chacara.Api;

[ApiController]
[Route("api/chacara/faixas-preco")]
[Authorize]
public class FaixaPrecoChacaraController(FaixasPrecoChacaraService servico, ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() =>
        (await servico.ListarAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarFaixaRequest req) =>
        (await servico.CriarAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarFaixaRequest req) =>
        (await servico.AtualizarAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id) =>
        (await servico.ExcluirAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

}
