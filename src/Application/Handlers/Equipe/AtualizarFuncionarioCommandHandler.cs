using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;
using static LojaApi.Application.Equipe.FuncionarioApoio;

namespace LojaApi.Application.Equipe;

// Edita um funcionário e acompanha o lançamento fixo do salário (cria, atualiza ou desativa).
public class AtualizarFuncionarioCommandHandler(IEquipeRepository repositorio, ILancamentoFixoFinanceiro financeiro) : ICommandHandler<AtualizarFuncionarioCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarFuncionarioCommand command)
    {
        var (lojaId, id, req) = command;
        var profissional = await repositorio.ObterProfissionalAsync(id, lojaId);
        if (profissional is null) return Resultado.NaoEncontrado();

        var erro = EquipeRegras.ValidarRemuneracao(req.TipoRemuneracao, req.SalarioFixo, req.ValorDiaria, req.ComissaoVendasAtiva, req.ComissaoVendasPercentual)
                   ?? await ValidarContaDoSalarioAsync(repositorio, lojaId!.Value, req);
        if (erro is not null) return Resultado.Invalido(erro);

        var tipoAnterior = profissional.TipoRemuneracao;

        profissional.Nome = req.Nome.Trim();
        profissional.ComissaoPadraoPercentual = req.ComissaoPadraoPercentual;
        profissional.DiaPagamentoPadrao = EquipeRegras.DiaValido(req.DiaPagamentoPadrao);
        profissional.Ativo = req.Ativo;
        profissional.TipoRemuneracao = req.TipoRemuneracao;
        profissional.SalarioFixo = EquipeRegras.SalarioAplicavel(req.TipoRemuneracao, req.SalarioFixo);
        profissional.ValorDiaria = EquipeRegras.DiariaAplicavel(req.TipoRemuneracao, req.ValorDiaria);
        profissional.Telefone = req.Telefone;
        AplicarEndereco(profissional, req);
        profissional.ComissaoBaseCalculo = EquipeRegras.BaseDeCalculo(req.ComissaoBaseCalculo);
        profissional.ComissaoVendasAtiva = req.ComissaoVendasAtiva;
        profissional.ComissaoVendasPercentual = EquipeRegras.PercentualVendasAplicavel(req.ComissaoVendasAtiva, req.ComissaoVendasPercentual);

        // Saiu do salário fixo — desativa o lançamento fixo antigo, se existir
        if (tipoAnterior == TipoRemuneracao.SalarioFixo && req.TipoRemuneracao != TipoRemuneracao.SalarioFixo && profissional.LancamentoFixoId.HasValue)
        {
            var fixoAntigo = await repositorio.ObterLancamentoFixoAsync(profissional.LancamentoFixoId.Value);
            if (fixoAntigo != null) fixoAntigo.Ativa = false;
            profissional.LancamentoFixoId = null;
        }
        // Continua ou entrou em salário fixo — cria ou atualiza o lançamento fixo
        else if (req.TipoRemuneracao == TipoRemuneracao.SalarioFixo && req.ContaBancariaId.HasValue)
        {
            if (profissional.LancamentoFixoId.HasValue)
            {
                var fixo = await repositorio.ObterLancamentoFixoAsync(profissional.LancamentoFixoId.Value);
                if (fixo != null)
                {
                    fixo.ContaBancariaId = req.ContaBancariaId.Value;
                    fixo.Valor = req.SalarioFixo!.Value;
                    fixo.DiaVencimento = EquipeRegras.DiaValido(req.DiaPagamentoPadrao) ?? fixo.DiaVencimento;
                    fixo.Ativa = true;

                    await financeiro.LimparFuturosAsync(fixo.Id);
                    await financeiro.GerarLoteAsync(fixo, MesAtualUtc());
                }
            }
            else
            {
                var categoriaId = await ObterOuCriarCategoriaDeSalarioAsync(repositorio, lojaId!.Value);
                var fixo = NovoSalarioFixo(lojaId.Value, profissional, req, categoriaId);
                repositorio.Adicionar(fixo);
                await repositorio.SalvarAsync();

                await financeiro.GerarLoteAsync(fixo, MesAtualUtc());

                profissional.LancamentoFixoId = fixo.Id;
            }
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(Dto(profissional));
    }
}
