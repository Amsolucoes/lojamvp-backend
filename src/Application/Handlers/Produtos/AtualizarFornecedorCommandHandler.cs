using LojaApi.Shared.Application;
using static LojaApi.Application.Produtos.ProdutosApoio;

namespace LojaApi.Application.Produtos;

// Atualiza um fornecedor da loja.
public class AtualizarFornecedorCommandHandler(IProdutosRepository repositorio) : ICommandHandler<AtualizarFornecedorCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarFornecedorCommand command)
    {
        var (lojaId, id, req) = command;
        var fornecedor = await repositorio.ObterFornecedorAsync(id, lojaId);
        if (fornecedor is null) return Resultado.NaoEncontrado();

        if (string.IsNullOrWhiteSpace(req.Nome))
            return Resultado.Invalido("Nome é obrigatório.");

        fornecedor.Nome = req.Nome.Trim();
        fornecedor.CnpjCpf = req.CnpjCpf;
        fornecedor.Telefone = req.Telefone;
        fornecedor.Email = req.Email;
        FornecedorApoio.AplicarEndereco(fornecedor, req);
        fornecedor.Observacoes = req.Observacoes;
        fornecedor.Ativo = req.Ativo;

        await repositorio.SalvarAsync();

        var qtdProdutos = await repositorio.ContarProdutosAtivosDoFornecedorAsync(id);
        return Resultado.Ok(ParaDto(fornecedor, qtdProdutos));
    }
}
