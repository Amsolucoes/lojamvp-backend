using LojaApi.Application.Servicos;
using LojaApi.Shared.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Servicos;

// Página pública de agendamento online (o cliente final não faz login)
[ApiController]
[Route("api/publico")]
[AllowAnonymous]
public class PublicoController(AgendamentoOnlineService servico) : ControllerBase
{
    // Dados da loja + serviços (pelo slug)
    [HttpGet("{slug}")]
    public async Task<IActionResult> DadosLoja(string slug) =>
        (await servico.DadosLojaAsync(slug)).ParaResposta(this);

    // Horários livres de um dia para um serviço
    [HttpGet("{slug}/horarios")]
    public async Task<IActionResult> Horarios(string slug, [FromQuery] DateTime data, [FromQuery] Guid servicoId) =>
        (await servico.HorariosAsync(slug, data, servicoId)).ParaResposta(this);

    [HttpPost("{slug}/agendar")]
    public async Task<IActionResult> Agendar(string slug, [FromBody] AgendarPublicoRequest req) =>
        (await servico.AgendarAsync(slug, req)).ParaResposta(this);

    // O cliente já existe? (pelo telefone)
    [HttpGet("{slug}/cliente")]
    public async Task<IActionResult> VerificarCliente(string slug, [FromQuery] string telefone) =>
        (await servico.VerificarClienteAsync(slug, telefone)).ParaResposta(this);
}
