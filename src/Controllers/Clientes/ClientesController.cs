using LojaApi.Application.Clientes;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Clientes;

[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController(ClientesService servico, ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca) =>
        (await servico.ListarAsync(await lojaAtual.ObterIdAsync(User), busca)).ParaResposta(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id) =>
        (await servico.BuscarAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarClienteRequest req)
    {
        var resultado = await servico.CriarAsync(await lojaAtual.ObterIdAsync(User), req);
        return resultado.Sucesso
            ? CreatedAtAction(nameof(Buscar), new { id = resultado.Valor!.Id }, resultado.Valor)
            : resultado.ParaResposta(this);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarClienteRequest req) =>
        (await servico.AtualizarAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var resultado = await servico.ExcluirAsync(await lojaAtual.ObterIdAsync(User), id);
        return resultado.Sucesso ? NoContent() : resultado.ParaResposta(this);
    }
}
