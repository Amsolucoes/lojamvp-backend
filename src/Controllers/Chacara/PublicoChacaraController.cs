using LojaApi.Application.Chacara;
using LojaApi.Shared.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Chacara;

// Site público da chácara: o que o cliente vê e faz sem estar logado.
[ApiController]
[Route("api/publico/{slug}/chacara")]
[AllowAnonymous]
public class PublicoChacaraController(SiteChacaraService site, PagamentoReservaChacaraService pagamento) : ControllerBase
{
    [HttpGet("disponibilidade")]
    public async Task<IActionResult> Disponibilidade(string slug, [FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim) =>
        (await site.DisponibilidadeAsync(slug, dataInicio, dataFim)).ParaResposta(this);

    [HttpGet("datas-ocupadas")]
    public async Task<IActionResult> DatasOcupadas(string slug, [FromQuery] int meses = 3) =>
        (await site.DatasOcupadasAsync(slug, meses)).ParaResposta(this);

    [HttpGet("valor")]
    public async Task<IActionResult> Valor(
        string slug, [FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim, [FromQuery] int pessoas,
        [FromQuery] string? horaEntrada = null, [FromQuery] string? horaSaida = null) =>
        (await site.ValorAsync(slug, dataInicio, dataFim, pessoas, horaEntrada, horaSaida)).ParaResposta(this);

    [HttpPost("reservar")]
    public async Task<IActionResult> Reservar(string slug, [FromBody] ReservarPublicoRequest req) =>
        (await site.ReservarAsync(slug, req)).ParaResposta(this);

    [HttpGet("avaliacao/{reservaId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> BuscarParaAvaliar(string slug, int reservaId) =>
        (await site.BuscarParaAvaliarAsync(slug, reservaId)).ParaResposta(this);

    [HttpPost("avaliacao/{reservaId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> EnviarAvaliacao(string slug, int reservaId, [FromBody] EnviarAvaliacaoChacaraRequest req) =>
        (await site.EnviarAvaliacaoAsync(slug, reservaId, req)).ParaResposta(this);

    [HttpPost("reservas/{id:int}/pagamento/escolher")]
    [AllowAnonymous]
    public async Task<IActionResult> EscolherPagamento(string slug, int id, [FromBody] EscolherPagamentoRequest req) =>
        (await pagamento.EscolherAsync(slug, id, req)).ParaResposta(this);

    [HttpPost("reservas/{id:int}/pagamento/pix")]
    [AllowAnonymous]
    public async Task<IActionResult> PagarPix(string slug, int id) =>
        (await pagamento.PagarPixAsync(slug, id)).ParaResposta(this);

    [HttpPost("reservas/{id:int}/pagamento/cartao")]
    [AllowAnonymous]
    public async Task<IActionResult> PagarCartao(string slug, int id, [FromBody] PagarCartaoRequest req) =>
        (await pagamento.PagarCartaoAsync(slug, id, req)).ParaResposta(this);

    [HttpGet("reservas/{id:int}/pagamento/status")]
    [AllowAnonymous]
    public async Task<IActionResult> StatusPagamento(string slug, int id) =>
        (await pagamento.StatusAsync(slug, id)).ParaResposta(this);

    [HttpPost("reservas/{id:int}/cancelar")]
    [AllowAnonymous]
    public async Task<IActionResult> CancelarPeloCliente(string slug, int id) =>
        (await pagamento.CancelarPeloClienteAsync(slug, id)).ParaResposta(this);

    [HttpGet("dados")]
    public async Task<IActionResult> Dados(string slug) =>
        (await site.DadosAsync(slug)).ParaResposta(this);
}
