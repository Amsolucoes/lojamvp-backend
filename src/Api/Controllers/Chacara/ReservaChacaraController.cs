using LojaApi.Application.Chacara;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Chacara;

// Reservas da chácara, do ponto de vista do dono (painel administrativo).
[ApiController]
[Route("api/chacara/reservas")]
[Authorize]
public class ReservaChacaraController(ReservasChacaraService servico, ILojaAtual lojaAtual) : ControllerBase
{
    private Task<Guid?> LojaId() => lojaAtual.ObterIdAsync(User);

    [HttpGet]
    public async Task<IActionResult> Listar() =>
        (await servico.ListarAsync(await LojaId())).ParaResposta(this);

    [HttpGet("perdidas")]
    public async Task<IActionResult> ListarPerdidas(
        [FromQuery] int pagina = 1, [FromQuery] int porPagina = 10,
        [FromQuery] int? mes = null, [FromQuery] int? ano = null,
        [FromQuery] DateTime? de = null, [FromQuery] DateTime? ate = null) =>
        (await servico.ListarPerdidasAsync(await LojaId(), pagina, porPagina, mes, ano, de, ate)).ParaResposta(this);

    [HttpPatch("{id:int}/marcar-expirada")]
    public async Task<IActionResult> MarcarExpirada(int id) =>
        (await servico.MarcarExpiradaAsync(await LojaId(), id)).ParaResposta(this);

    [HttpPatch("{id:int}/reativar")]
    public async Task<IActionResult> Reativar(int id) =>
        (await servico.ReativarAsync(await LojaId(), id)).ParaResposta(this);

    [HttpPatch("{id:int}/confirmar")]
    public async Task<IActionResult> Confirmar(int id, [FromBody] ConfirmarComPagamentoRequest? req) =>
        (await servico.ConfirmarAsync(await LojaId(), id, req)).ParaResposta(this);

    [HttpPatch("{id:int}/prejuizo")]
    public async Task<IActionResult> RegistrarPrejuizo(int id, [FromBody] RegistrarPrejuizoRequest req) =>
        (await servico.RegistrarPrejuizoAsync(await LojaId(), id, req)).ParaResposta(this);

    [HttpPatch("{id:int}/avaliar-cliente")]
    public async Task<IActionResult> AvaliarCliente(int id, [FromBody] AvaliarClienteRequest req) =>
        (await servico.AvaliarClienteAsync(await LojaId(), id, req)).ParaResposta(this);

    [HttpPatch("{id:int}/manter-negociacao")]
    public async Task<IActionResult> ManterNegociacao(int id) =>
        (await servico.ManterNegociacaoAsync(await LojaId(), id)).ParaResposta(this);

    [HttpPatch("{id:int}/desfazer-negociacao")]
    public async Task<IActionResult> DesfazerNegociacao(int id) =>
        (await servico.DesfazerNegociacaoAsync(await LojaId(), id)).ParaResposta(this);

    [HttpPatch("{id:int}/registrar-pagamento")]
    public async Task<IActionResult> RegistrarPagamento(int id, [FromBody] RegistrarPagamentoRequest req) =>
        (await servico.RegistrarPagamentoAsync(await LojaId(), id, req)).ParaResposta(this);

    [HttpPost]
    public async Task<IActionResult> CriarManual([FromBody] CriarReservaManualRequest req) =>
        (await servico.CriarManualAsync(await LojaId(), req)).ParaResposta(this);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarReservaRequest req) =>
        (await servico.EditarAsync(await LojaId(), id, req)).ParaResposta(this);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id) =>
        (await servico.ExcluirAsync(await LojaId(), id)).ParaResposta(this);

    // Devolve o PDF do contrato (para o dono enviar pelo WhatsApp ou imprimir), sem enviar e-mail.
    [HttpGet("{id:int}/contrato-pdf")]
    public async Task<IActionResult> BaixarContratoPdf(int id)
    {
        var resultado = await servico.GerarContratoPdfAsync(await LojaId(), id);
        return resultado.Sucesso
            ? File(resultado.Valor!.Pdf, "application/pdf", resultado.Valor.NomeArquivo)
            : resultado.ParaResposta(this);
    }

    [HttpPost("{id:int}/enviar-contrato")]
    public async Task<IActionResult> EnviarContrato(int id) =>
        (await servico.EnviarContratoAsync(await LojaId(), id)).ParaResposta(this);
}
