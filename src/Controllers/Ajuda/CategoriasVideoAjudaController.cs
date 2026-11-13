using LojaApi.Application.Ajuda;
using LojaApi.Shared.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Ajuda;

[ApiController]
[Route("api/categorias-video-ajuda")]
public class CategoriasVideoAjudaController(CategoriasAjudaService servico) : ControllerBase
{
    // Público: usado no formulário de vídeo e na Central de Ajuda
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar() => (await servico.ListarAtivasAsync()).ParaResposta(this);

    [HttpGet("todas")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ListarTodas() => (await servico.ListarTodasAsync()).ParaResposta(this);

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarCategoriaRequest req) => (await servico.CriarAsync(req)).ParaResposta(this);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarCategoriaRequest req) => (await servico.AtualizarAsync(id, req)).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Excluir(Guid id) => (await servico.ExcluirAsync(id)).ParaResposta(this);
}
