using LojaApi.Modules.Carteira.Application;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Modules.Carteira.Api;

// Carteira do cliente: comprou agora, paga depois (normalmente no fim do mês).
[ApiController]
[Authorize]
public class CarteiraController(CarteiraService servico, ILojaAtual lojaAtual) : ControllerBase
{
    // Clientes que estão devendo (para cobrar)
    [HttpGet("api/carteira/devedores")]
    public async Task<IActionResult> Devedores() =>
        (await servico.DevedoresAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    // Saldo, limite, compras em aberto e extrato de um cliente
    [HttpGet("api/clientes/{clienteId:guid}/carteira")]
    public async Task<IActionResult> Obter(Guid clienteId) =>
        (await servico.ObterAsync(await lojaAtual.ObterIdAsync(User), clienteId)).ParaResposta(this);

    [HttpPut("api/clientes/{clienteId:guid}/carteira/limite")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> DefinirLimite(Guid clienteId, [FromBody] DefinirLimiteCarteiraRequest req) =>
        (await servico.DefinirLimiteAsync(await lojaAtual.ObterIdAsync(User), clienteId, req)).ParaResposta(this);

    [HttpPost("api/clientes/{clienteId:guid}/carteira/pagamentos")]
    public async Task<IActionResult> Receber(Guid clienteId, [FromBody] ReceberCarteiraRequest req) =>
        (await servico.ReceberAsync(await lojaAtual.ObterIdAsync(User), clienteId, req)).ParaResposta(this);

    [HttpDelete("api/clientes/{clienteId:guid}/carteira/pagamentos/{movimentoId:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> DesfazerRecebimento(Guid clienteId, Guid movimentoId) =>
        (await servico.DesfazerRecebimentoAsync(await lojaAtual.ObterIdAsync(User), clienteId, movimentoId)).ParaResposta(this);
}
