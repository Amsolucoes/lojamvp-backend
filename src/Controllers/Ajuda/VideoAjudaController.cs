using LojaApi.Application.Ajuda;
using LojaApi.Shared.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Ajuda;

[ApiController]
[Route("api/videos-ajuda")]
public class VideoAjudaController(VideosAjudaService servico) : ControllerBase
{
    // Público: consumido pela Central de Ajuda dentro da loja
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar() => (await servico.ListarAtivosAsync()).ParaResposta(this);

    // Gestão (superadmin)
    [HttpGet("todos")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ListarTodos() => (await servico.ListarTodosAsync()).ParaResposta(this);

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarVideoRequest req) => (await servico.CriarAsync(req)).ParaResposta(this);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarVideoRequest req) => (await servico.AtualizarAsync(id, req)).ParaResposta(this);

    [HttpPatch("{id:guid}/ativo")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> AlternarAtivo(Guid id) => (await servico.AlternarAtivoAsync(id)).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Excluir(Guid id) => (await servico.ExcluirAsync(id)).ParaResposta(this);
}
