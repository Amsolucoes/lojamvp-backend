using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Cria um lançamento avulso (uma conta a pagar ou a receber só), já paga ou pendente.
public class CriarLancamentoAvulsoCommandHandler(ILancamentosRepository repositorio) : ICommandHandler<CriarLancamentoAvulsoCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarLancamentoAvulsoCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        var lancamento = new LancamentoFinanceiro
        {
            LojaId = lojaId,
            ContaBancariaId = req.ContaBancariaId,
            Tipo = req.Tipo,
            Modo = "avulsa",
            Descricao = req.Descricao.Trim(),
            CategoriaId = req.CategoriaId,
            Observacao = req.Observacao,
            Valor = req.Valor,
            Vencimento = LancamentosRegras.AoMeioDia(req.Vencimento),
            Status = req.JaPago ? "pago" : "pendente",
            PagoEm = req.JaPago ? DateTime.UtcNow : null,
        };
        repositorio.Adicionar(lancamento);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { lancamento.Id });
    }
}
