using LojaApi.Application.Servicos;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Servicos;

// Planos de assinatura e clientes assinantes
[ApiController]
[Route("api/planos")]
[Authorize]
public class PlanosController(PlanosAssinaturaService servico, ILojaAtual lojaAtual) : ControllerBase
{
    // ── Planos ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Listar() =>
        (await servico.ListarAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarPlanoRequest req) =>
        (await servico.CriarAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarPlanoRequest req) =>
        (await servico.AtualizarAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    // Ativar/desativar plano
    [HttpPatch("{id:guid}/ativo")]
    public async Task<IActionResult> AlternarAtivo(Guid id) =>
        (await servico.AlternarAtivoAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id) =>
        (await servico.ExcluirAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    // ── Assinaturas (cliente vinculado a um plano) ───────────────────────────

    [HttpGet("assinantes")]
    public async Task<IActionResult> Assinantes() =>
        (await servico.AssinantesAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPost("assinantes")]
    public async Task<IActionResult> VincularCliente([FromBody] VincularPlanoRequest req) =>
        (await servico.VincularClienteAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    // Trocar plano ou dia de vencimento
    [HttpPut("assinantes/{id:guid}")]
    public async Task<IActionResult> AtualizarAssinatura(Guid id, [FromBody] AtualizarAssinaturaRequest req) =>
        (await servico.AtualizarAssinaturaAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    [HttpPatch("assinantes/{id:guid}/cancelar")]
    public async Task<IActionResult> CancelarAssinatura(Guid id) =>
        (await servico.CancelarAssinaturaAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    // ── Mensalidades ─────────────────────────────────────────────────────────

    // Quitar todos os meses pendentes de uma assinatura
    [HttpPost("assinantes/{id:guid}/quitar-atraso")]
    public async Task<IActionResult> QuitarAtraso(Guid id) =>
        (await servico.QuitarAtrasoAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    // Pagar meses escolhidos (com valor pago editável, ex.: juros)
    [HttpPost("assinantes/{id:guid}/pagar")]
    public async Task<IActionResult> PagarMeses(Guid id, [FromBody] PagarMesesRequest req) =>
        (await servico.PagarMesesAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    // Marcar a mensalidade do mês como paga/pendente (manual)
    [HttpPost("assinantes/{id:guid}/pagamento")]
    public async Task<IActionResult> MarcarPagamento(Guid id, [FromBody] MarcarPagamentoPlanoRequest req) =>
        (await servico.MarcarPagamentoAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    [HttpGet("assinantes/{id:guid}/historico")]
    public async Task<IActionResult> HistoricoPagamentos(Guid id) =>
        (await servico.HistoricoPagamentosAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    [HttpGet("assinantes/{id:guid}/consumos")]
    public async Task<IActionResult> Consumos(Guid id) =>
        (await servico.ConsumosAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    // Plano de um cliente (usado no Caixa e na Agenda); ?data=yyyy-MM-dd é a data de referência das regras de uso
    [HttpGet("cliente/{clienteId:guid}")]
    public async Task<IActionResult> AssinaturaDoCliente(Guid clienteId, [FromQuery] DateTime? data) =>
        (await servico.AssinaturaDoClienteAsync(await lojaAtual.ObterIdAsync(User), clienteId, data)).ParaResposta(this);
}
