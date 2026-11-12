using LojaApi.Data;
using LojaApi.Domain.Chacara;
using LojaApi.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Application.Chacara;

public record SalvarFaixaRequest(int PessoasAte, decimal ValorDiariaSemana, decimal ValorDiariaFimSemana, decimal ValorPacote2DiasFimSemana);

// Tabela de preços por faixa de quantidade de pessoas.
public class FaixasPrecoChacaraService(AppDbContext db)
{
    public async Task<Resultado<object>> ListarAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var lista = await db.FaixasPrecoChacara
            .Where(f => f.LojaId == lojaId)
            .OrderBy(f => f.PessoasAte)
            .ToListAsync();
        return Resultado.Ok(lista);
    }

    public async Task<Resultado<object>> CriarAsync(Guid? lojaId, SalvarFaixaRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (req.PessoasAte <= 0)
            return Resultado.Invalido("Informe um limite de pessoas válido.");

        var jaExiste = await db.FaixasPrecoChacara.AnyAsync(f => f.LojaId == lojaId && f.PessoasAte == req.PessoasAte);
        if (jaExiste) return Resultado.Conflito("Já existe uma faixa com esse limite de pessoas.");

        var faixa = FaixaPrecoChacara.Criar(lojaId.Value, req.PessoasAte, req.ValorDiariaSemana, req.ValorDiariaFimSemana, req.ValorPacote2DiasFimSemana);
        db.FaixasPrecoChacara.Add(faixa);
        await db.SaveChangesAsync();
        return Resultado.Ok(faixa);
    }

    public async Task<Resultado<object>> AtualizarAsync(Guid? lojaId, int id, SalvarFaixaRequest req)
    {
        var faixa = await db.FaixasPrecoChacara.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);
        if (faixa is null) return Resultado.NaoEncontrado();

        var jaExiste = await db.FaixasPrecoChacara.AnyAsync(f => f.LojaId == lojaId && f.PessoasAte == req.PessoasAte && f.Id != id);
        if (jaExiste) return Resultado.Conflito("Já existe uma faixa com esse limite de pessoas.");

        faixa.Atualizar(req.PessoasAte, req.ValorDiariaSemana, req.ValorDiariaFimSemana, req.ValorPacote2DiasFimSemana);
        await db.SaveChangesAsync();
        return Resultado.Ok(faixa);
    }

    public async Task<Resultado<object>> ExcluirAsync(Guid? lojaId, int id)
    {
        var faixa = await db.FaixasPrecoChacara.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);
        if (faixa is null) return Resultado.NaoEncontrado();

        db.FaixasPrecoChacara.Remove(faixa);
        await db.SaveChangesAsync();
        return Resultado.Ok(new { mensagem = "Faixa excluída." });
    }
}
