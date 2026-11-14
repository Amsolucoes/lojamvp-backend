using LojaApi.Models;
using LojaApi.Shared.Application;
using static LojaApi.Application.Produtos.ProdutosApoio;

namespace LojaApi.Application.Produtos;

// Cadastra um fornecedor da loja (o controller responde 201).
public class CriarFornecedorCommandHandler(IProdutosRepository repositorio) : ICommandHandler<CriarFornecedorCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarFornecedorCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (string.IsNullOrWhiteSpace(req.Nome))
            return Resultado.Invalido("Nome é obrigatório.");

        var fornecedor = new Fornecedor
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            CnpjCpf = req.CnpjCpf,
            Telefone = req.Telefone,
            Email = req.Email,
            Observacoes = req.Observacoes,
            Ativo = req.Ativo,
        };
        FornecedorApoio.AplicarEndereco(fornecedor, req);
        repositorio.Adicionar(fornecedor);
        await repositorio.SalvarAsync();

        return Resultado.Ok(ParaDto(fornecedor, 0));
    }
}
