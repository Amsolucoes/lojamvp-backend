using LojaApi.Domain.Bonificacoes;

namespace LojaApi.Application.Bonificacoes;

// O que os handlers de Bonificações compartilham
internal static class BonificacoesApoio
{
    public static BonificacaoDto ParaDto(Bonificacao b) => new(b.Id, b.ClienteId, b.NomeCliente, b.ProdutoId, b.VariacaoId, b.NomeProduto, b.Quantidade,
        b.CustoUnitario, b.CustoTotal(), b.Observacao, b.VendaId, b.UsuarioId, b.CriadoEm, b.EstornadaEm);
}
