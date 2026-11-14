using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Renomeia uma categoria. A chave NÃO muda de propósito: trocar mudaria a categoria de produtos já salvos silenciosamente.
public class AtualizarCategoriaAcessorioCommandHandler(IAcessoriosRepository repositorio) : ICommandHandler<AtualizarCategoriaAcessorioCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarCategoriaAcessorioCommand command)
    {
        var (id, req) = command;
        var categoria = await repositorio.ObterCategoriaAsync(id);
        if (categoria is null) return Resultado.NaoEncontrado();

        if (string.IsNullOrWhiteSpace(req.Nome))
            return Resultado.Invalido("Digite o nome da categoria.");

        // A Chave NÃO muda aqui de propósito — trocar mudaria a categoria de produtos já
        // salvos silenciosamente. Só o nome de exibição é editável.
        categoria.Nome = req.Nome.Trim();
        await repositorio.SalvarAsync();

        return Resultado.Ok(categoria);
    }
}
