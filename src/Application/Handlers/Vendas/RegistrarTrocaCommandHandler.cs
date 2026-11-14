using LojaApi.Domain.Vendas;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Registra uma troca: devolve itens (voltando ao estoque se marcado), baixa os novos e, se sobrar, gera crédito para o cliente.
public class RegistrarTrocaCommandHandler(IVendasRepository repositorio) : ICommandHandler<RegistrarTrocaCommand>
{
    public async Task<Resultado<object>> HandleAsync(RegistrarTrocaCommand command)
    {
        var (lojaId, req) = command;

        var cliente = await repositorio.ObterClienteAsync(req.ClienteId, lojaId);
        if (cliente is null) return Resultado.Invalido("Cliente não encontrado.");

        if (!req.Devolvidos.Any() || !req.Novos.Any())
            return Resultado.Invalido("A troca precisa de ao menos um produto devolvido e um novo.");

        // Os devolvidos não podem ser de outra loja (produto já excluído continua aceito, como antes)
        if (lojaId.HasValue)
        {
            foreach (var item in req.Devolvidos)
            {
                var produto = await repositorio.ObterProdutoAsync(item.ProdutoId, null);
                if (produto != null && produto.LojaId != lojaId)
                    return Resultado.Invalido($"Produto '{item.NomeProduto}' não encontrado.");
            }
        }

        // Valida estoque dos novos
        foreach (var item in req.Novos)
        {
            if (item.VariacaoId.HasValue)
            {
                var variacao = await repositorio.ObterVariacaoAsync(item.VariacaoId.Value, lojaId);
                if (variacao is null || variacao.Estoque < item.Quantidade)
                    return Resultado.Invalido($"Estoque insuficiente para '{item.NomeProduto}'.");
            }
            else
            {
                var produto = await repositorio.ObterProdutoAsync(item.ProdutoId, lojaId);
                if (produto is null || produto.Estoque < item.Quantidade)
                    return Resultado.Invalido($"Estoque insuficiente para '{item.NomeProduto}'.");
            }
        }

        decimal totalDevolvido = req.Devolvidos.Sum(i => i.Quantidade * i.PrecoUnitario);
        decimal totalNovo = req.Novos.Sum(i => i.Quantidade * i.PrecoUnitario);
        decimal diferenca = VendasRegras.DiferencaDaTroca(totalDevolvido, totalNovo);
        decimal creditoGerado = VendasRegras.CreditoDaTroca(diferenca);

        var troca = new Troca
        {
            ClienteId = req.ClienteId,
            TotalDevolvido = totalDevolvido,
            TotalNovo = totalNovo,
            Diferenca = diferenca,
            CreditoGerado = creditoGerado,
            FormaPagamento = diferenca > 0 ? req.FormaPagamento : null,
            LojaId = lojaId,
        };
        repositorio.Adicionar(troca);

        // Devolvidos — volta estoque se marcado
        foreach (var item in req.Devolvidos)
        {
            repositorio.Adicionar(new ItemTroca
            {
                TrocaId = troca.Id,
                ProdutoId = item.ProdutoId,
                NomeProduto = item.NomeProduto,
                VariacaoId = item.VariacaoId,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                Tipo = "devolvido",
                VoltaEstoque = item.VoltaEstoque,
            });

            if (item.VoltaEstoque)
            {
                if (item.VariacaoId.HasValue)
                {
                    var variacao = await repositorio.ObterVariacaoAsync(item.VariacaoId.Value, lojaId);
                    if (variacao != null) { variacao.Estoque += item.Quantidade; variacao.AtualizadoEm = DateTime.UtcNow; }
                }
                else
                {
                    var produto = await repositorio.ObterProdutoAsync(item.ProdutoId, lojaId);
                    if (produto != null) { produto.Estoque += item.Quantidade; produto.AtualizadoEm = DateTime.UtcNow; }
                }

                repositorio.Adicionar(new MovimentoEstoque
                {
                    ProdutoId = item.ProdutoId,
                    Tipo = "entrada",
                    Quantidade = item.Quantidade,
                    Observacao = $"Troca - devolução {item.NomeProduto}",
                    LojaId = lojaId,
                });
            }
        }

        // Novos — baixa estoque
        foreach (var item in req.Novos)
        {
            repositorio.Adicionar(new ItemTroca
            {
                TrocaId = troca.Id,
                ProdutoId = item.ProdutoId,
                NomeProduto = item.NomeProduto,
                VariacaoId = item.VariacaoId,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                Tipo = "novo",
            });

            if (item.VariacaoId.HasValue)
            {
                var variacao = await repositorio.ObterVariacaoAsync(item.VariacaoId.Value, lojaId);
                if (variacao != null) { variacao.Estoque -= item.Quantidade; variacao.AtualizadoEm = DateTime.UtcNow; }
            }
            else
            {
                var produto = await repositorio.ObterProdutoAsync(item.ProdutoId, lojaId);
                if (produto != null) { produto.Estoque -= item.Quantidade; produto.AtualizadoEm = DateTime.UtcNow; }
            }

            repositorio.Adicionar(new MovimentoEstoque
            {
                ProdutoId = item.ProdutoId,
                Tipo = "saida",
                Quantidade = item.Quantidade,
                Observacao = $"Troca - saída {item.NomeProduto}",
                LojaId = lojaId,
            });
        }

        // Crédito gerado vai pro cliente
        if (creditoGerado > 0)
            cliente.CreditoLoja += creditoGerado;

        await repositorio.SalvarAsync();

        return Resultado.Ok(new
        {
            id = troca.Id,
            totalDevolvido,
            totalNovo,
            diferenca,
            creditoGerado,
            creditoTotalCliente = cliente.CreditoLoja,
            mensagem = VendasRegras.MensagemDaTroca(diferenca, creditoGerado)
        });
    }
}
