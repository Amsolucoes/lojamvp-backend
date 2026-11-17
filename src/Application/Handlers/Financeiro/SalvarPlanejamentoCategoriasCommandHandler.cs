using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Salva os tetos de gasto (pagar) e de receita (receber) por categoria. Categorias que não são da loja são ignoradas;
// um valor negativo recusa tudo (nada é gravado).
public class SalvarPlanejamentoCategoriasCommandHandler(IPlanejamentoRepository repositorio) : ICommandHandler<SalvarPlanejamentoCategoriasCommand>
{
    public async Task<Resultado<object>> HandleAsync(SalvarPlanejamentoCategoriasCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        var idsValidos = await repositorio.ListarIdsDeCategoriasDaLojaAsync(lojaId);
        var existentes = await repositorio.ListarTetosAsync(lojaId);

        foreach (var item in req.Itens.Where(i => idsValidos.Contains(i.CategoriaId)))
        {
            if (!PlanejamentoRegras.ValorDoTetoValido(item.Valor)) return Resultado.Invalido("O valor planejado não pode ser negativo.");
            var tipo = PlanejamentoRegras.TipoDoTeto(item.Tipo);
            var atual = existentes.FirstOrDefault(p => p.CategoriaId == item.CategoriaId && p.Tipo == tipo);
            if (atual is null)
            {
                repositorio.Adicionar(new PlanejamentoCategoria { LojaId = lojaId, CategoriaId = item.CategoriaId, Tipo = tipo, Ativo = item.Ativo, Valor = item.Valor });
            }
            else
            {
                atual.Ativo = item.Ativo;
                atual.Valor = item.Valor;
            }
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { ok = true });
    }
}
