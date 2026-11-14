using LojaApi.Domain.Produtos;
using LojaApi.Models;

namespace LojaApi.Application.Produtos;

// O que criar e atualizar fornecedor compartilham.
public static class FornecedorApoio
{
    // Grava o endereço detalhado e mantém o texto completo em Endereco (compatível com dados e telas antigas)
    public static void AplicarEndereco(Fornecedor f, SalvarFornecedorRequest req)
    {
        var e = ProdutosRegras.MontarEndereco(req.Cep, req.Rua, req.Numero, req.Bairro, req.Cidade, req.Uf, req.Endereco);
        f.Cep = e.Cep;
        f.Rua = e.Rua;
        f.Numero = e.Numero;
        f.Bairro = e.Bairro;
        f.Cidade = e.Cidade;
        f.Uf = e.Uf;
        f.Endereco = e.Texto;
    }
}
