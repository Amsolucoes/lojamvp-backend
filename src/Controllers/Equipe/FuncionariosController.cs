using LojaApi.Application.Equipe;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Equipe;

// Funcionários/profissionais: cadastro, remuneração e comissão por serviço
[ApiController]
[Route("api/funcionarios")]
[Authorize]
public class FuncionariosController(FuncionariosService servico, ILojaAtual lojaAtual) : ControllerBase
{
    // Lista com comissão padrão e exceções por serviço
    [HttpGet]
    public async Task<IActionResult> Listar() =>
        (await servico.ListarAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarFuncionarioRequest req) =>
        (await servico.CriarAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarFuncionarioRequest req) =>
        (await servico.AtualizarAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    [HttpPatch("{id:guid}/ativo")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AlternarAtivo(Guid id) =>
        (await servico.AlternarAtivoAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Excluir(Guid id) =>
        (await servico.ExcluirAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    // Comissão por serviço (exceção ao padrão)
    [HttpPost("{id:guid}/comissoes-servico")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> DefinirComissaoServico(Guid id, [FromBody] SalvarComissaoServicoRequest req) =>
        (await servico.DefinirComissaoServicoAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    [HttpDelete("comissoes-servico/{comissaoId:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> RemoverComissaoServico(Guid comissaoId) =>
        (await servico.RemoverComissaoServicoAsync(await lojaAtual.ObterIdAsync(User), comissaoId)).ParaResposta(this);

    // Lista simplificada (id + nome), usada em seletores de outras telas
    [HttpGet("ativos")]
    public async Task<IActionResult> ListarAtivos() =>
        (await servico.ListarAtivosAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);
}
