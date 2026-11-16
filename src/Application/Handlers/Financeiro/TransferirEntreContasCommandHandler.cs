using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Transfere valor entre duas contas da loja. Com "registrar", grava uma saída e uma entrada no histórico (mesmo grupo);
// sem registrar, mexe direto no saldo inicial das duas contas, sem deixar rastro.
public class TransferirEntreContasCommandHandler(IFinanceiroRepository repositorio) : ICommandHandler<TransferirEntreContasCommand>
{
    public async Task<Resultado<object>> HandleAsync(TransferirEntreContasCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        if (req.ContaOrigemId == req.ContaDestinoId)
            return Resultado.Invalido("Escolha duas contas diferentes.");
        if (req.Valor <= 0)
            return Resultado.Invalido("Valor deve ser maior que zero.");

        var origem = await repositorio.ObterContaAsync(req.ContaOrigemId, lojaId);
        var destino = await repositorio.ObterContaAsync(req.ContaDestinoId, lojaId);
        if (origem is null || destino is null) return Resultado.NaoEncontrado("Conta não encontrada.");

        if (req.Registrar)
        {
            var grupoId = Guid.NewGuid();
            repositorio.Adicionar(new AjusteContaBancaria
            {
                LojaId = lojaId,
                ContaBancariaId = origem.Id,
                Tipo = "saida",
                Valor = req.Valor,
                Observacao = FinanceiroRegras.ObservacaoDaTransferencia("para", destino.Nome, req.Observacao),
                TransferenciaGrupoId = grupoId,
            });
            repositorio.Adicionar(new AjusteContaBancaria
            {
                LojaId = lojaId,
                ContaBancariaId = destino.Id,
                Tipo = "entrada",
                Valor = req.Valor,
                Observacao = FinanceiroRegras.ObservacaoDaTransferencia("de", origem.Nome, req.Observacao),
                TransferenciaGrupoId = grupoId,
            });
        }
        else
        {
            origem.SaldoInicial -= req.Valor;
            destino.SaldoInicial += req.Valor;
        }

        await repositorio.SalvarAsync();

        var saldos = await repositorio.CalcularSaldosAsync(lojaId, [origem, destino]);
        return Resultado.Ok(new SaldosDaTransferenciaDto(saldos[origem.Id], saldos[destino.Id]));
    }
}
