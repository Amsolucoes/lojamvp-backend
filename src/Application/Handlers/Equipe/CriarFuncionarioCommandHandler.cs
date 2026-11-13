using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;
using static LojaApi.Application.Equipe.FuncionarioApoio;

namespace LojaApi.Application.Equipe;

// Cadastra um funcionário (comissão, salário fixo ou diária); o salário fixo vira um lançamento fixo no Financeiro.
public class CriarFuncionarioCommandHandler(IEquipeRepository repositorio, ILancamentoFixoFinanceiro financeiro) : ICommandHandler<CriarFuncionarioCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarFuncionarioCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);
        if (string.IsNullOrWhiteSpace(req.Nome)) return Resultado.Invalido("Nome é obrigatório.");

        var erro = EquipeRegras.ValidarRemuneracao(req.TipoRemuneracao, req.SalarioFixo, req.ValorDiaria, req.ComissaoVendasAtiva, req.ComissaoVendasPercentual)
                   ?? await ValidarContaDoSalarioAsync(repositorio, lojaId.Value, req);
        if (erro is not null) return Resultado.Invalido(erro);

        var profissional = new Profissional
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            // Não zera em salario_fixo/diaria: o funcionário pode ter salário fixo/diária E ainda ganhar comissão
            // sobre Ordem de Serviço/Agendamento.
            ComissaoPadraoPercentual = req.ComissaoPadraoPercentual,
            DiaPagamentoPadrao = EquipeRegras.DiaValido(req.DiaPagamentoPadrao),
            Ativo = req.Ativo,
            TipoRemuneracao = req.TipoRemuneracao,
            SalarioFixo = EquipeRegras.SalarioAplicavel(req.TipoRemuneracao, req.SalarioFixo),
            ValorDiaria = EquipeRegras.DiariaAplicavel(req.TipoRemuneracao, req.ValorDiaria),
            Telefone = req.Telefone,
            ComissaoBaseCalculo = EquipeRegras.BaseDeCalculo(req.ComissaoBaseCalculo),
            ComissaoVendasAtiva = req.ComissaoVendasAtiva,
            ComissaoVendasPercentual = EquipeRegras.PercentualVendasAplicavel(req.ComissaoVendasAtiva, req.ComissaoVendasPercentual),
        };
        AplicarEndereco(profissional, req);
        repositorio.Adicionar(profissional);
        await repositorio.SalvarAsync();

        if (req.TipoRemuneracao == TipoRemuneracao.SalarioFixo && req.ContaBancariaId.HasValue)
        {
            var categoriaId = await ObterOuCriarCategoriaDeSalarioAsync(repositorio, lojaId.Value);
            var fixo = NovoSalarioFixo(lojaId.Value, profissional, req, categoriaId);
            repositorio.Adicionar(fixo);
            await repositorio.SalvarAsync();

            await financeiro.GerarLoteAsync(fixo, MesAtualUtc());
            await repositorio.SalvarAsync();

            profissional.LancamentoFixoId = fixo.Id;
            await repositorio.SalvarAsync();
        }

        return Resultado.Ok(Dto(profissional));
    }
}
