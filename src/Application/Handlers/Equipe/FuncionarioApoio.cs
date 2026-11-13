using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// O que os handlers de criar e atualizar funcionário compartilham: resposta, endereço e o salário fixo no Financeiro.
public static class FuncionarioApoio
{
    public const string LojaNaoEncontrada = "Loja não encontrada.";
    public const string ContaNaoEncontrada = "Conta bancária não encontrada.";

    public static ProfissionalDto Dto(Profissional p) => new(
        p.Id, p.Nome, p.Ativo, p.ComissaoPadraoPercentual, p.DiaPagamentoPadrao, p.TipoRemuneracao, p.SalarioFixo, p.ValorDiaria,
        p.Telefone, p.Cep, p.Endereco, p.Numero, p.Bairro, p.Cidade, p.Uf, p.ComissaoBaseCalculo, p.ComissaoVendasAtiva, p.ComissaoVendasPercentual);

    public static void AplicarEndereco(Profissional p, SalvarFuncionarioRequest req)
    {
        p.Cep = EquipeRegras.Limpar(req.Cep, 9);
        p.Endereco = EquipeRegras.Limpar(req.Endereco, 200);
        p.Numero = EquipeRegras.Limpar(req.Numero, 20);
        p.Bairro = EquipeRegras.Limpar(req.Bairro, 100);
        p.Cidade = EquipeRegras.Limpar(req.Cidade, 100);
        p.Uf = EquipeRegras.Uf(req.Uf);
    }

    // Categoria do Financeiro onde caem os salários (criada na primeira vez)
    public static async Task<Guid> ObterOuCriarCategoriaDeSalarioAsync(IEquipeRepository repositorio, Guid lojaId)
    {
        var categoria = await repositorio.ObterCategoriaFinanceiraAsync(lojaId, EquipeRegras.CategoriaSalarios);
        if (categoria != null) return categoria.Id;

        categoria = new CategoriaFinanceira { LojaId = lojaId, Nome = EquipeRegras.CategoriaSalarios, Tipo = "pagar", Icone = EquipeRegras.IconeSalarios };
        repositorio.Adicionar(categoria);
        await repositorio.SalvarAsync();
        return categoria.Id;
    }

    public static LancamentoFixo NovoSalarioFixo(Guid lojaId, Profissional profissional, SalvarFuncionarioRequest req, Guid categoriaId) => new()
    {
        LojaId = lojaId,
        ContaBancariaId = req.ContaBancariaId!.Value,
        Tipo = "pagar",
        Descricao = $"Salário — {profissional.Nome}",
        CategoriaId = categoriaId,
        Valor = req.SalarioFixo!.Value,
        DiaVencimento = EquipeRegras.DiaDoLancamentoFixo(req.DiaPagamentoPadrao),
    };

    public static DateTime MesAtualUtc() { var agora = DateTime.UtcNow; return new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc); }

    // O salário fixo precisa cair numa conta bancária desta mesma loja
    public static async Task<string?> ValidarContaDoSalarioAsync(IEquipeRepository repositorio, Guid lojaId, SalvarFuncionarioRequest req) =>
        req.TipoRemuneracao == TipoRemuneracao.SalarioFixo && req.ContaBancariaId.HasValue &&
        !await repositorio.ContaBancariaDaLojaExisteAsync(req.ContaBancariaId.Value, lojaId)
            ? ContaNaoEncontrada : null;
}
