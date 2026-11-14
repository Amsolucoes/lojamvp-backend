using LojaApi.Application.Produtos;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Produtos;

// Fornecedores (de quem a loja compra), com endereço detalhado.
[ApiController]
[Route("api/fornecedores")]
[Authorize]
public class FornecedoresController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] IQueryHandler<ListarFornecedoresQuery> handler, [FromQuery] string? busca) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), busca))).ParaResposta(this);

    // ── Lista simplificada (id + nome), usada no seletor do cadastro de produto ──
    [HttpGet("ativos")]
    public async Task<IActionResult> ListarAtivos([FromServices] IQueryHandler<ListarFornecedoresAtivosQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id, [FromServices] IQueryHandler<BuscarFornecedorQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarFornecedorRequest req, [FromServices] ICommandHandler<CriarFornecedorCommand> handler)
    {
        var resultado = await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req));
        return resultado.Sucesso
            ? CreatedAtAction(nameof(Buscar), new { id = ((FornecedorDto)resultado.Valor!).Id }, resultado.Valor)
            : resultado.ParaResposta(this);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarFornecedorRequest req, [FromServices] ICommandHandler<AtualizarFornecedorCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Excluir(Guid id, [FromServices] ICommandHandler<ExcluirFornecedorCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);
}
