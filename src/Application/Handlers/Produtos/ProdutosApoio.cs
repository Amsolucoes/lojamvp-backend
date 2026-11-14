using LojaApi.Domain.Produtos;
using LojaApi.Models;

namespace LojaApi.Application.Produtos;

// O que vários handlers de Produtos compartilham.
public static class ProdutosApoio
{
    // Mesmo desenho do que a API sempre devolveu: o estoque mostrado soma as variações ativas
    public static ProdutoDto ParaDto(Produto p) => new(
        p.Id, p.Nome, p.Descricao, p.Categoria,
        p.PrecoCusto, p.PrecoVenda,
        ProdutosRegras.EstoqueExibido(p.Estoque, p.Variacoes.Where(v => v.Ativo).Select(v => v.Estoque)),
        p.EstoqueMinimo,
        p.CodigoBarras, p.Ativo,
        p.CriadoEm, p.AtualizadoEm,
        p.TipoVenda, p.UnidadeMedida,
        p.Variacoes.Where(v => v.Ativo).Select(ParaDto).ToList(),
        p.MarcaId,
        p.Marca?.Nome,
        p.FornecedorId,
        p.Fornecedor?.Nome
    );

    public static ProdutoVariacaoDto ParaDto(ProdutoVariacao v) => new(
        v.Id, v.Tamanho, v.Cor, v.OutroCampo, v.CodigoBarras, v.Estoque, v.EstoqueMinimo, v.Ativo);

    public static FornecedorDto ParaDto(Fornecedor f, int qtdProdutos) => new(
        f.Id, f.Nome, f.CnpjCpf, f.Telefone, f.Email,
        f.Endereco, f.Observacoes, f.Ativo, f.CriadoEm, qtdProdutos,
        f.Cep, f.Rua, f.Numero, f.Bairro, f.Cidade, f.Uf);

    // A marca e o fornecedor do produto, quando informados, têm que ser da loja do usuário
    public static async Task<string?> ValidarMarcaEFornecedorAsync(IProdutosRepository repositorio, Guid? lojaId, Guid? marcaId, Guid? fornecedorId)
    {
        if (!lojaId.HasValue) return null;

        if (marcaId.HasValue && await repositorio.ObterMarcaAsync(marcaId.Value, lojaId) is null)
            return "Marca não encontrada.";
        if (fornecedorId.HasValue && await repositorio.ObterFornecedorAsync(fornecedorId.Value, lojaId) is null)
            return "Fornecedor não encontrado.";

        return null;
    }
}
