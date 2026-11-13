using LojaApi.Data;
using LojaApi.Domain.Servicos;
using LojaApi.Models;
using LojaApi.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Application.Servicos;

public record AgendarPublicoRequest(Guid ServicoId, string NomeCliente, string Telefone, DateTime DataHora);

// Página pública de agendamento online (sem login): o cliente final escolhe serviço e horário pelo link da loja.
public class AgendamentoOnlineService(AppDbContext db)
{
    private Task<Loja?> BuscarLojaAsync(string slug) => db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);

    private static bool Desligada(Loja? loja) => loja is null || !loja.AgendamentoOnlineAtivo;
    private static bool Pausada(Loja loja) => loja.PausaAte.HasValue && loja.PausaAte.Value > DateTime.UtcNow;

    // Dados da loja + serviços ativos (pelo slug)
    public async Task<Resultado<object>> DadosLojaAsync(string slug)
    {
        var loja = await BuscarLojaAsync(slug);
        if (Desligada(loja)) return Resultado.NaoEncontrado("Página de agendamento não encontrada.");

        var pausado = Pausada(loja!);

        var servicos = pausado
            ? new List<object>()
            : await db.Servicos
                .Where(s => s.LojaId == loja!.Id && s.Ativo)
                .OrderBy(s => s.Categoria).ThenBy(s => s.Nome)
                .Select(s => (object)new { s.Id, s.Nome, s.Categoria, s.Preco, s.DuracaoMin })
                .ToListAsync();

        return Resultado.Ok(new
        {
            nome = loja!.Nome,
            logoUrl = loja.LogoUrl,
            corPrimaria = loja.CorPrimaria,
            confirmacao = AgendamentoRegras.ConfirmacaoOnline(loja.AgendamentoOnlineConfirmacao),
            servicos,
            pausado,
            pausaAte = pausado ? loja.PausaAte : null,
            pausaMensagem = pausado ? (loja.PausaMensagem ?? "Estamos temporariamente fechados. Voltamos em breve!") : null,
        });
    }

    // Horários livres de um dia para um serviço
    public async Task<Resultado<object>> HorariosAsync(string slug, DateTime data, Guid servicoId)
    {
        var loja = await BuscarLojaAsync(slug);
        if (Desligada(loja)) return Resultado.NaoEncontrado("Página não encontrada.");
        if (Pausada(loja!)) return Resultado.Invalido(loja!.PausaMensagem ?? "Agendamentos temporariamente pausados.");

        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == servicoId && s.LojaId == loja!.Id);
        if (servico is null) return Resultado.Invalido("Serviço não encontrado.");

        var dia = DateTime.SpecifyKind(data.Date, DateTimeKind.Unspecified);
        var ocupados = await OcupacoesDoDiaAsync(loja!.Id, dia);

        var agoraLocal = DateTime.UtcNow.AddHours(-3); // aprox. horário Brasília p/ não oferecer horário passado
        var slots = AgendamentoRegras.HorariosLivres(dia, loja.AgendaHoraInicio, loja.AgendaHoraFim, servico.DuracaoMin, agoraLocal, ocupados);

        return Resultado.Ok(new { horarios = slots });
    }

    // Agendamentos que ocupam horário no dia (agendado, concluído, pendente — não os cancelados)
    private async Task<List<Ocupacao>> OcupacoesDoDiaAsync(Guid lojaId, DateTime dia)
    {
        var fim = dia.AddDays(1);
        var ocupados = await db.Agendamentos
            .Where(a => a.LojaId == lojaId && a.DataHora >= dia && a.DataHora < fim && a.Status != StatusAgendamento.Cancelado)
            .Select(a => new { a.DataHora, a.DuracaoMin })
            .ToListAsync();
        return ocupados.Select(o => new Ocupacao(o.DataHora, o.DuracaoMin)).ToList();
    }

    // Cria o agendamento do cliente final (reaproveita o cliente pelo telefone, ou cadastra um novo)
    public async Task<Resultado<object>> AgendarAsync(string slug, AgendarPublicoRequest req)
    {
        var loja = await BuscarLojaAsync(slug);
        if (Desligada(loja)) return Resultado.NaoEncontrado("Página não encontrada.");
        if (Pausada(loja!)) return Resultado.Invalido(loja!.PausaMensagem ?? "Agendamentos temporariamente pausados.");

        if (string.IsNullOrWhiteSpace(req.NomeCliente) || string.IsNullOrWhiteSpace(req.Telefone))
            return Resultado.Invalido("Informe nome e telefone.");

        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == req.ServicoId && s.LojaId == loja!.Id);
        if (servico is null) return Resultado.Invalido("Serviço não encontrado.");

        var telefone = AgendamentoRegras.SoDigitos(req.Telefone);
        var clientesLoja = await db.Clientes.Where(c => c.LojaId == loja!.Id).ToListAsync();
        var cliente = clientesLoja.FirstOrDefault(c => AgendamentoRegras.SoDigitos(c.Telefone) == telefone);

        if (cliente is null)
        {
            cliente = new Cliente
            {
                LojaId = loja!.Id,
                Nome = req.NomeCliente.Trim(),
                Telefone = telefone,  // sempre só dígitos
            };
            db.Clientes.Add(cliente);
        }

        // Hora local, sem UTC (padrão da agenda)
        var dataHora = DateTime.SpecifyKind(req.DataHora, DateTimeKind.Unspecified);

        // Revalida que o horário ainda está livre (evita corrida)
        var ocupados = await OcupacoesDoDiaAsync(loja!.Id, dataHora.Date);
        if (AgendamentoRegras.Conflita(dataHora, dataHora.AddMinutes(servico.DuracaoMin), ocupados))
            return Resultado.Conflito("Este horário acabou de ser reservado. Escolha outro.");

        var status = AgendamentoRegras.StatusDoAgendamentoOnline(AgendamentoRegras.ConfirmacaoOnline(loja.AgendamentoOnlineConfirmacao));

        db.Agendamentos.Add(new Agendamento
        {
            LojaId = loja.Id,
            ServicoId = servico.Id,
            NomeServico = servico.Nome,
            ClienteId = cliente.Id,
            NomeCliente = cliente.Nome,
            Preco = servico.Preco,
            DataHora = dataHora,
            DuracaoMin = servico.DuracaoMin,
            Status = status,
            Origem = "online",
        });
        await db.SaveChangesAsync();

        return Resultado.Ok(new { mensagem = AgendamentoRegras.MensagemDoAgendamentoOnline(status), status });
    }

    // O cliente já está cadastrado? (pelo telefone; devolve só o primeiro nome)
    public async Task<Resultado<object>> VerificarClienteAsync(string slug, string? telefone)
    {
        var loja = await BuscarLojaAsync(slug);
        if (Desligada(loja)) return Resultado.NaoEncontrado();

        var tel = AgendamentoRegras.SoDigitos(telefone);
        if (tel.Length < 8) return Resultado.Ok(new { existe = false });

        var clientesDaLoja = await db.Clientes
            .Where(c => c.LojaId == loja!.Id && c.Telefone != null)
            .Select(c => new { c.Nome, c.Telefone })
            .ToListAsync();

        var cliente = clientesDaLoja.FirstOrDefault(c => AgendamentoRegras.SoDigitos(c.Telefone) == tel);
        if (cliente is null) return Resultado.Ok(new { existe = false });

        return Resultado.Ok(new { existe = true, primeiroNome = AgendamentoRegras.PrimeiroNome(cliente.Nome) });
    }
}
