using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Cria um lançamento fixo (aluguel, internet...) e gera 60 meses de ocorrências a partir do mês atual (ou da data de início).
public class CriarFixoCommandHandler(ILancamentosRepository repositorio, IGeradorDeLancamentosFixos fixos) : ICommandHandler<CriarFixoCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarFixoCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        var fixo = new LancamentoFixo
        {
            LojaId = lojaId,
            ContaBancariaId = req.ContaBancariaId,
            Tipo = req.Tipo,
            Descricao = req.Descricao.Trim(),
            CategoriaId = req.CategoriaId,
            Observacao = req.Observacao,
            Valor = req.Valor,
            DiaVencimento = LancamentosRegras.DiaDoFixo(req.DiaVencimento),
            DataInicio = req.DataInicio.HasValue
                ? DateTime.SpecifyKind(req.DataInicio.Value.Date, DateTimeKind.Utc)
                : null,
        };
        repositorio.Adicionar(fixo);
        await repositorio.SalvarAsync();

        await fixos.GerarLoteAsync(fixo, LancamentosApoio.MesAtual());
        await repositorio.SalvarAsync();

        if (req.JaPago)
        {
            var primeiraOcorrencia = await repositorio.ObterPrimeiraOcorrenciaDoFixoAsync(fixo.Id);
            if (primeiraOcorrencia != null)
            {
                primeiraOcorrencia.Status = "pago";
                primeiraOcorrencia.PagoEm = DateTime.UtcNow;
                await repositorio.SalvarAsync();
            }
        }

        return Resultado.Ok(new { fixo.Id, geradoAte = fixo.GeradoAte });
    }
}
