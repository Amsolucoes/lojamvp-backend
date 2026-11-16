using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Cria um parcelamento: gera todas as parcelas de uma vez (uma por mês), no mesmo grupo. Com "já pago", só a primeira nasce paga.
public class CriarLancamentoParceladoCommandHandler(ILancamentosRepository repositorio) : ICommandHandler<CriarLancamentoParceladoCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarLancamentoParceladoCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        var totalParcelas = LancamentosRegras.TotalDeParcelas(req.PrimeiroVencimento, req.DataFim, req.TotalParcelas);
        if (!LancamentosRegras.TotalDeParcelasValido(totalParcelas))
            return Resultado.Invalido("Número de parcelas deve ficar entre 2 e 120. Ajuste a quantidade ou a data fim.");

        var grupoId = Guid.NewGuid();
        var lista = new List<LancamentoFinanceiro>();

        for (int i = 0; i < totalParcelas; i++)
        {
            lista.Add(new LancamentoFinanceiro
            {
                LojaId = lojaId,
                ContaBancariaId = req.ContaBancariaId,
                Tipo = req.Tipo,
                Modo = "parcelada",
                Descricao = req.Descricao.Trim(),
                CategoriaId = req.CategoriaId,
                Observacao = req.Observacao,
                Valor = req.ValorParcela,
                Vencimento = DateTime.SpecifyKind(req.PrimeiroVencimento.Date, DateTimeKind.Utc).AddMonths(i).AddHours(12),
                GrupoParcelamentoId = grupoId,
                NumeroParcela = i + 1,
                TotalParcelas = totalParcelas,
                Status = (req.JaPago && i == 0) ? "pago" : "pendente",
                PagoEm = (req.JaPago && i == 0) ? DateTime.UtcNow : null,
            });
        }

        repositorio.Adicionar(lista);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { grupoParcelamentoId = grupoId, totalGerado = totalParcelas });
    }
}
