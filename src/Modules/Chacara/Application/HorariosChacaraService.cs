using LojaApi.Data;
using LojaApi.Modules.Chacara.Domain;
using LojaApi.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Modules.Chacara.Application;

public record SalvarHorarioRequest(string Tipo, string Hora, decimal Ajuste, int Ordem);

// Horários de entrada/saída oferecidos ao cliente, cada um com um ajuste (acréscimo ou desconto) na diária.
public class HorariosChacaraService(AppDbContext db)
{
    public async Task<Resultado<object>> ListarAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var lista = await db.HorariosChacara
            .Where(h => h.LojaId == lojaId)
            .OrderBy(h => h.Tipo).ThenBy(h => h.Ordem).ThenBy(h => h.Hora)
            .ToListAsync();
        return Resultado.Ok(lista);
    }

    public async Task<Resultado<object>> CriarAsync(Guid? lojaId, SalvarHorarioRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var erro = HorarioChacara.Validar(req.Tipo, req.Hora);
        if (erro != null) return Resultado.Invalido(erro);

        var jaExiste = await db.HorariosChacara.AnyAsync(h => h.LojaId == lojaId && h.Tipo == req.Tipo && h.Hora == req.Hora);
        if (jaExiste) return Resultado.Conflito("Já existe esse horário cadastrado para esse tipo.");

        var horario = HorarioChacara.Criar(lojaId.Value, req.Tipo, req.Hora, req.Ajuste, req.Ordem);
        db.HorariosChacara.Add(horario);
        await db.SaveChangesAsync();
        return Resultado.Ok(horario);
    }

    public async Task<Resultado<object>> AtualizarAsync(Guid? lojaId, int id, SalvarHorarioRequest req)
    {
        var horario = await db.HorariosChacara.FirstOrDefaultAsync(h => h.Id == id && h.LojaId == lojaId);
        if (horario is null) return Resultado.NaoEncontrado();

        var erro = HorarioChacara.Validar(req.Tipo, req.Hora);
        if (erro != null) return Resultado.Invalido(erro);

        var jaExiste = await db.HorariosChacara.AnyAsync(h => h.LojaId == lojaId && h.Tipo == req.Tipo && h.Hora == req.Hora && h.Id != id);
        if (jaExiste) return Resultado.Conflito("Já existe esse horário cadastrado para esse tipo.");

        horario.Atualizar(req.Tipo, req.Hora, req.Ajuste, req.Ordem);
        await db.SaveChangesAsync();
        return Resultado.Ok(horario);
    }

    public async Task<Resultado<object>> ExcluirAsync(Guid? lojaId, int id)
    {
        var horario = await db.HorariosChacara.FirstOrDefaultAsync(h => h.Id == id && h.LojaId == lojaId);
        if (horario is null) return Resultado.NaoEncontrado();

        db.HorariosChacara.Remove(horario);
        await db.SaveChangesAsync();
        return Resultado.Ok(new { mensagem = "Horário excluído." });
    }
}
