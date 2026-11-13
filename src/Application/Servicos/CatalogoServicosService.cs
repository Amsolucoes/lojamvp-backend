using LojaApi.Data;
using LojaApi.Models;
using LojaApi.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Application.Servicos;

public record SalvarServicoRequest(string Nome, string Categoria, decimal Preco, int DuracaoMin, bool Ativo);

// Catálogo de serviços da loja (o que ela oferece na agenda e no caixa).
public class CatalogoServicosService(AppDbContext db)
{
    private static object Resposta(Servico s) => new { s.Id, s.Nome, s.Categoria, s.Preco, s.DuracaoMin, s.Ativo, s.CriadoEm };

    private static string CategoriaOuPadrao(string? categoria) => string.IsNullOrWhiteSpace(categoria) ? "Geral" : categoria.Trim();

    public async Task<Resultado<object>> ListarAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var servicos = await db.Servicos
            .Where(s => s.LojaId == lojaId)
            .OrderBy(s => s.Categoria).ThenBy(s => s.Nome)
            .Select(s => new { s.Id, s.Nome, s.Categoria, s.Preco, s.DuracaoMin, s.Ativo, s.CriadoEm })
            .ToListAsync();

        return Resultado.Ok(servicos);
    }

    public async Task<Resultado<object>> CriarAsync(Guid? lojaId, SalvarServicoRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");
        if (string.IsNullOrWhiteSpace(req.Nome)) return Resultado.Invalido("Nome do serviço é obrigatório.");

        var servico = new Servico
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Categoria = CategoriaOuPadrao(req.Categoria),
            Preco = req.Preco,
            DuracaoMin = req.DuracaoMin,
            Ativo = req.Ativo,
        };
        db.Servicos.Add(servico);
        await db.SaveChangesAsync();

        return Resultado.Ok(Resposta(servico));
    }

    public async Task<Resultado<object>> AtualizarAsync(Guid? lojaId, Guid id, SalvarServicoRequest req)
    {
        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == id && s.LojaId == lojaId);
        if (servico is null) return Resultado.NaoEncontrado();

        servico.Nome = req.Nome.Trim();
        servico.Categoria = CategoriaOuPadrao(req.Categoria);
        servico.Preco = req.Preco;
        servico.DuracaoMin = req.DuracaoMin;
        servico.Ativo = req.Ativo;
        await db.SaveChangesAsync();

        return Resultado.Ok(Resposta(servico));
    }

    // Serviço com agendamentos não pode sumir (o histórico perderia o vínculo): a loja desativa em vez de excluir.
    public async Task<Resultado<object>> ExcluirAsync(Guid? lojaId, Guid id)
    {
        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == id && s.LojaId == lojaId);
        if (servico is null) return Resultado.NaoEncontrado();

        var qtdAgendamentos = await db.Agendamentos.CountAsync(a => a.ServicoId == id);
        if (qtdAgendamentos > 0)
            return Resultado.Invalido($"Não é possível excluir: este serviço tem {qtdAgendamentos} agendamento(s) vinculado(s). Você pode desativá-lo em vez de excluir.");

        db.Servicos.Remove(servico);
        await db.SaveChangesAsync();
        return Resultado.Ok(new { mensagem = "Serviço excluído." });
    }
}
