using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Cria uma categoria. Nome repetido (sem diferenciar maiúscula nem acento, e de tipo compatível) devolve a que já existe em vez de duplicar.
public class CriarCategoriaFinanceiraCommandHandler(IFinanceiroRepository repositorio) : ICommandHandler<CriarCategoriaFinanceiraCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarCategoriaFinanceiraCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var nomeNormalizado = FinanceiroRegras.NormalizarNomeCategoria(req.Nome);
        var existentes = await repositorio.ListarCategoriasDaLojaAsync(lojaId.Value);
        var igual = existentes.FirstOrDefault(c => FinanceiroRegras.CategoriaEquivalente(c.Nome, c.Tipo, nomeNormalizado, req.Tipo));
        if (igual is not null) return Resultado.Ok(new CategoriaFinanceiraDto(igual.Id, igual.Nome, igual.Tipo, igual.Icone));

        var cat = new CategoriaFinanceira { LojaId = lojaId.Value, Nome = req.Nome.Trim(), Tipo = req.Tipo, Icone = req.Icone };
        repositorio.Adicionar(cat);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new CategoriaFinanceiraDto(cat.Id, cat.Nome, cat.Tipo, cat.Icone));
    }
}
