using LojaApi.Data;
using LojaApi.Modules.Chacara.Domain;
using LojaApi.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Modules.Chacara.Application;

public record SalvarPeriodoRequest(string Nome, DateTime DataInicio, DateTime DataFim, decimal ValorTotal);

// Períodos especiais (Natal, Ano Novo...) com pacote fechado por um valor total.
public class PeriodosEspeciaisChacaraService(AppDbContext db)
{
    public async Task<Resultado<object>> ListarAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var lista = await db.PeriodosEspeciaisChacara
            .Where(p => p.LojaId == lojaId)
            .OrderBy(p => p.DataInicio)
            .ToListAsync();

        var confirmadas = await db.Reservas
            .Where(r => r.LojaId == lojaId && r.Status == "confirmada")
            .Select(r => new { r.DataInicio, r.DataFim })
            .ToListAsync();

        var resultado = lista.Select(p => new
        {
            p.Id,
            p.Nome,
            p.DataInicio,
            p.DataFim,
            p.ValorTotal,
            // Confirmado: existe reserva confirmada com as MESMAS datas deste período (foi essa opção que o cliente escolheu)
            confirmado = confirmadas.Any(r => r.DataInicio.Date == p.DataInicio.Date && r.DataFim.Date == p.DataFim.Date),
            // Ocupado: existe reserva confirmada que se sobrepõe às datas deste período (mesmo que não seja exatamente igual —
            // ex: cliente reservou "3 dias" e isso ocupa parte do intervalo do "2 dias" e do "4 dias")
            ocupado = confirmadas.Any(r => r.DataInicio <= p.DataFim && r.DataFim >= p.DataInicio),
        });
        return Resultado.Ok(resultado);
    }

    public async Task<Resultado<object>> CriarAsync(Guid? lojaId, SalvarPeriodoRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (string.IsNullOrWhiteSpace(req.Nome))
            return Resultado.Invalido("Informe um nome para o período.");

        var erro = PeriodoEspecialChacara.ValidarDatas(req.DataInicio, req.DataFim);
        if (erro != null) return Resultado.Invalido(erro);

        var periodo = PeriodoEspecialChacara.Criar(lojaId.Value, req.Nome, req.DataInicio, req.DataFim, req.ValorTotal);
        db.PeriodosEspeciaisChacara.Add(periodo);
        await db.SaveChangesAsync();
        return Resultado.Ok(periodo);
    }

    public async Task<Resultado<object>> AtualizarAsync(Guid? lojaId, int id, SalvarPeriodoRequest req)
    {
        var periodo = await db.PeriodosEspeciaisChacara.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (periodo is null) return Resultado.NaoEncontrado();

        var erro = PeriodoEspecialChacara.ValidarDatas(req.DataInicio, req.DataFim);
        if (erro != null) return Resultado.Invalido(erro);

        periodo.Atualizar(req.Nome, req.DataInicio, req.DataFim, req.ValorTotal);
        await db.SaveChangesAsync();
        return Resultado.Ok(periodo);
    }

    public async Task<Resultado<object>> ExcluirAsync(Guid? lojaId, int id)
    {
        var periodo = await db.PeriodosEspeciaisChacara.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (periodo is null) return Resultado.NaoEncontrado();

        db.PeriodosEspeciaisChacara.Remove(periodo);
        await db.SaveChangesAsync();
        return Resultado.Ok(new { mensagem = "Período excluído." });
    }
}
