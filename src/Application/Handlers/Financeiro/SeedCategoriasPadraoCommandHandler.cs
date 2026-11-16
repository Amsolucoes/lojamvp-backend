using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Cria as 10 categorias padrão (aluguel, água, luz...), só para quem ainda não tem nenhuma.
public class SeedCategoriasPadraoCommandHandler(IFinanceiroRepository repositorio) : ICommandHandler<SeedCategoriasPadraoCommand>
{
    public async Task<Resultado<object>> HandleAsync(SeedCategoriasPadraoCommand command)
    {
        if (command.LojaId is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        if (await repositorio.LojaTemCategoriasAsync(lojaId))
            return Resultado.Invalido("Você já tem categorias cadastradas.");

        foreach (var (nome, tipo, icone) in FinanceiroRegras.CategoriasPadrao)
            repositorio.Adicionar(new CategoriaFinanceira { LojaId = lojaId, Nome = nome, Tipo = tipo, Icone = icone });

        await repositorio.SalvarAsync();
        return Resultado.Ok(new MensagemDto("Categorias padrão criadas."));
    }
}
