using System.Text.Json;
using LojaApi.Domain.NfImportacao;
using LojaApi.Shared.Application;
using LojaApi.src.Models;

namespace LojaApi.Application.NfImportacao;

// Lança uma nota fiscal à mão (sem XML), vinculada a um fornecedor: soma estoque, registra movimentos e guarda o detalhe.
public class LancarNfManualCommandHandler(INfImportacaoRepository repositorio) : ICommandHandler<LancarNfManualCommand>
{
    public async Task<Resultado<object>> HandleAsync(LancarNfManualCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        if (string.IsNullOrWhiteSpace(req.NumeroNf)) return Resultado.Invalido("Informe o número da nota fiscal.");
        if (req.Itens.Count == 0) return Resultado.Invalido("Adicione ao menos um item.");

        var fornecedor = await repositorio.ObterFornecedorAsync(req.FornecedorId, lojaId);
        if (fornecedor is null) return Resultado.Invalido("Fornecedor não encontrado.");

        var numeroNf = req.NumeroNf.Trim();

        // Trava contra lançar a mesma NF (número) duas vezes pro mesmo fornecedor
        if (await repositorio.NotaDoFornecedorExisteAsync(lojaId, fornecedor.Id, numeroNf, null))
            return Resultado.Conflito(NfImportacaoRegras.MensagemNotaDuplicada(numeroNf, fornecedor.Nome));

        var (aplicados, erro) = await NfImportacaoApoio.AplicarItensManuaisAsync(repositorio, lojaId, numeroNf, req.Itens);
        if (erro != null) return Resultado.Invalido(erro);

        repositorio.Adicionar(new NfImportada
        {
            LojaId = lojaId,
            ChaveAcesso = null,
            NumeroNf = numeroNf,
            NomeFornecedor = fornecedor.Nome,
            FornecedorId = fornecedor.Id,
            Origem = NfImportacaoRegras.OrigemManual,
            DataEmissao = NfImportacaoRegras.DataEmissaoUtc(req.DataEmissao),
            ValorTotal = req.ValorTotal ?? aplicados!.CustoTotal,
            ValorCustoTotal = aplicados!.CustoTotal,
            ValorVendaTotal = aplicados.VendaTotal,
            QuantidadeTotal = aplicados.QuantidadeTotal,
            QtdItens = req.Itens.Count,
            ItensJson = JsonSerializer.Serialize(aplicados.Detalhes),
        });
        await repositorio.SalvarAsync();

        return Resultado.Ok(new NfLancadaDto("Nota fiscal lançada.", req.Itens.Count));
    }
}
