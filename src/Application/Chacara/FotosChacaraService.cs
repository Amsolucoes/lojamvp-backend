using LojaApi.Data;
using LojaApi.Domain.Chacara;
using LojaApi.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Application.Chacara;

public record AdicionarFotoRequest(string Url);
public record ReordenarFotosRequest(List<int> Ids); // ids na nova ordem desejada

// Galeria de fotos do site público da chácara.
public class FotosChacaraService(AppDbContext db)
{
    public async Task<Resultado<object>> ListarAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var fotos = await db.FotosChacara
            .Where(f => f.LojaId == lojaId)
            .OrderBy(f => f.Ordem)
            .ToListAsync();
        return Resultado.Ok(fotos);
    }

    public async Task<Resultado<object>> AdicionarAsync(Guid? lojaId, AdicionarFotoRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (string.IsNullOrWhiteSpace(req.Url))
            return Resultado.Invalido("URL inválida.");

        var total = await db.FotosChacara.CountAsync(f => f.LojaId == lojaId);
        if (total >= FotoChacara.LimitePorLoja)
            return Resultado.Invalido($"Limite de {FotoChacara.LimitePorLoja} fotos atingido.");

        var proximaOrdem = await db.FotosChacara
            .Where(f => f.LojaId == lojaId)
            .Select(f => (int?)f.Ordem)
            .MaxAsync() ?? -1;

        var foto = FotoChacara.Criar(lojaId.Value, req.Url, proximaOrdem + 1);
        db.FotosChacara.Add(foto);
        await db.SaveChangesAsync();
        return Resultado.Ok(foto);
    }

    public async Task<Resultado<object>> RemoverAsync(Guid? lojaId, int id)
    {
        var foto = await db.FotosChacara.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);
        if (foto is null) return Resultado.NaoEncontrado();

        db.FotosChacara.Remove(foto);
        await db.SaveChangesAsync();
        return Resultado.Ok(new { mensagem = "Foto removida." });
    }

    public async Task<Resultado<object>> ReordenarAsync(Guid? lojaId, ReordenarFotosRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var fotos = await db.FotosChacara.Where(f => f.LojaId == lojaId).ToListAsync();
        for (int i = 0; i < req.Ids.Count; i++)
        {
            var foto = fotos.FirstOrDefault(f => f.Id == req.Ids[i]);
            if (foto != null) foto.Ordem = i;
        }
        await db.SaveChangesAsync();
        return Resultado.Ok(new { mensagem = "Ordem atualizada." });
    }
}
