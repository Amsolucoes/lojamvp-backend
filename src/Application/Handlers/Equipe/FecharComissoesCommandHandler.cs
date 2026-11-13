using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Fecha e paga as comissões (+ diárias) de um profissional num período; cria a conta a pagar se vier conta bancária.
public class FecharComissoesCommandHandler(IEquipeRepository repositorio) : ICommandHandler<FecharComissoesCommand>
{
    private const string LojaNaoEncontrada = "Loja não encontrada.";

    public async Task<Resultado<object>> HandleAsync(FecharComissoesCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);

        var profissional = await repositorio.ObterProfissionalAsync(req.ProfissionalId, lojaId);
        if (profissional is null) return Resultado.NaoEncontrado("Profissional não encontrado.");

        var erro = EquipeRegras.ValidarFechamento(req.QtdDiarias, profissional.ValorDiaria);
        if (erro is not null) return Resultado.Invalido(erro);

        // A conta bancária onde cai o pagamento tem que ser desta mesma loja
        if (req.ContaBancariaId.HasValue && !await repositorio.ContaBancariaDaLojaExisteAsync(req.ContaBancariaId.Value, lojaId.Value))
            return Resultado.Invalido("Conta bancária não encontrada.");

        var inicio = EquipeRegras.InicioDoPeriodo(req.PeriodoInicio);
        var fim = EquipeRegras.FimDoPeriodoExclusivo(req.PeriodoFim);

        var comissoesPendentes = await repositorio.ListarComissoesPendentesDoPeriodoAsync(lojaId.Value, req.ProfissionalId, inicio, fim);

        if (comissoesPendentes.Count == 0 && req.QtdDiarias == 0)
            return Resultado.Invalido("Nenhuma comissão pendente encontrada nesse período, e nenhuma diária informada.");

        var valorComissoes = comissoesPendentes.Sum(c => c.ValorComissao);
        var valorDiarias = EquipeRegras.ValorDasDiarias(req.QtdDiarias, profissional.ValorDiaria);
        var valorTotal = valorComissoes + valorDiarias;

        var fechamento = new FechamentoComissao
        {
            LojaId = lojaId.Value,
            ProfissionalId = profissional.Id,
            PeriodoInicio = inicio,
            PeriodoFim = EquipeRegras.UltimoDiaDoPeriodo(req.PeriodoFim),
            ValorTotal = valorTotal,
            QtdAtendimentos = comissoesPendentes.Count,
            QtdDiarias = req.QtdDiarias,
            ValorDiarias = valorDiarias,
        };
        repositorio.Adicionar(fechamento);
        await repositorio.SalvarAsync();

        foreach (var c in comissoesPendentes)
        {
            c.Status = StatusComissao.Pago;
            c.PagoEm = DateTime.UtcNow;
            c.FechamentoId = fechamento.Id;
        }

        // Integração opcional com o Financeiro: só cria a conta a pagar se veio conta bancária escolhida
        if (req.ContaBancariaId.HasValue)
        {
            var categoriaId = await ObterOuCriarCategoriaDeComissaoAsync(lojaId.Value);
            var lancamento = new LancamentoFinanceiro
            {
                LojaId = lojaId.Value,
                ContaBancariaId = req.ContaBancariaId.Value,
                Tipo = "pagar",
                Modo = "avulsa",
                Descricao = EquipeRegras.DescricaoDoFechamento(profissional.Nome, req.QtdDiarias, req.PeriodoInicio, req.PeriodoFim),
                CategoriaId = categoriaId,
                Valor = valorTotal,
                Vencimento = EquipeRegras.VencimentoDoFechamento(req.Vencimento, req.PeriodoFim),
                Status = "pendente",
            };
            repositorio.Adicionar(lancamento);
            await repositorio.SalvarAsync();

            fechamento.LancamentoFinanceiroId = lancamento.Id;
        }

        await repositorio.SalvarAsync();

        return Resultado.Ok(new
        {
            fechamento.Id,
            fechamento.ValorTotal,
            fechamento.QtdAtendimentos,
            fechamento.QtdDiarias,
            fechamento.ValorDiarias,
            profissionalNome = profissional.Nome,
        });
    }

    // Categoria do Financeiro onde caem as comissões pagas (criada na primeira vez)
    private async Task<Guid> ObterOuCriarCategoriaDeComissaoAsync(Guid lojaId)
    {
        var categoria = await repositorio.ObterCategoriaFinanceiraAsync(lojaId, EquipeRegras.CategoriaComissoes);
        if (categoria != null) return categoria.Id;

        categoria = new CategoriaFinanceira { LojaId = lojaId, Nome = EquipeRegras.CategoriaComissoes, Tipo = "pagar", Icone = EquipeRegras.IconeComissoes };
        repositorio.Adicionar(categoria);
        await repositorio.SalvarAsync();
        return categoria.Id;
    }
}
