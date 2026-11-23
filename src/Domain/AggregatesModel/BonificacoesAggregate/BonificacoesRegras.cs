namespace LojaApi.Domain.Bonificacoes;

// Regras puras da bonificação (sem banco): validação da quantidade, custo e os textos gravados no histórico de estoque.
public static class BonificacoesRegras
{
    public const int TamanhoMaximoDaObservacao = 300;

    // Brinde dado no caixa a quem não quis se cadastrar
    public const string NomeSemCadastro = "Sem cadastro";

    // Quantidade do brinde: maior que zero; em produto com variação (tamanho/cor) o estoque é inteiro, então a quantidade também
    public static string? ValidarQuantidade(decimal quantidade, bool ehVariacao)
    {
        if (quantidade <= 0) return "Informe uma quantidade maior que zero.";
        if (ehVariacao && quantidade != decimal.Truncate(quantidade)) return "Para produto com variação, a quantidade deve ser um número inteiro.";
        return null;
    }

    // "Pulseira" ou, com variação, "Pulseira (M / Azul)" (o mesmo nome que a venda grava nos itens)
    public static string NomeDoProduto(string nomeProduto, string? tamanho, string? cor)
    {
        var rotulo = string.Join(" / ", new[] { tamanho, cor }.Where(t => !string.IsNullOrWhiteSpace(t)));
        return rotulo == "" ? nomeProduto : $"{nomeProduto} ({rotulo})";
    }

    public static decimal CustoTotal(decimal quantidade, decimal custoUnitario) => Math.Round(quantidade * custoUnitario, 2, MidpointRounding.AwayFromZero);

    // Data de filtro vinda da tela: sem fuso (só "2025-01-10") vale como UTC, que é o que o banco aceita
    public static DateTime EmUtc(DateTime data) => data.Kind switch
    {
        DateTimeKind.Utc => data,
        DateTimeKind.Local => data.ToUniversalTime(),
        _ => DateTime.SpecifyKind(data, DateTimeKind.Utc),
    };

    // Observação digitada, sem espaços nas pontas; em branco vira nula
    public static string? LimparObservacao(string? observacao)
    {
        var texto = observacao?.Trim();
        if (string.IsNullOrEmpty(texto)) return null;
        return texto.Length > TamanhoMaximoDaObservacao ? texto[..TamanhoMaximoDaObservacao] : texto;
    }

    // Histórico de estoque do brinde avulso: "Bonificação - Maria Souza: aniversário" (cabe nos 300 caracteres do movimento)
    public static string ObservacaoDoMovimento(string nomeCliente, string? observacao)
    {
        var texto = $"Bonificação - {nomeCliente}" + (string.IsNullOrWhiteSpace(observacao) ? "" : $": {observacao.Trim()}");
        return texto.Length > TamanhoMaximoDaObservacao ? texto[..TamanhoMaximoDaObservacao] : texto;
    }

    // Histórico de estoque do brinde dado dentro de uma venda: "Bonificação na venda #1a2b3c4d - Pulseira"
    public static string ObservacaoDoMovimentoNaVenda(Guid vendaId, string nomeProduto)
    {
        var texto = $"Bonificação na venda #{vendaId.ToString()[..8]} - {nomeProduto}";
        return texto.Length > TamanhoMaximoDaObservacao ? texto[..TamanhoMaximoDaObservacao] : texto;
    }

    // Histórico de estoque quando o brinde é desfeito
    public static string ObservacaoDoEstorno(string nomeCliente, string nomeProduto)
    {
        var texto = $"Estorno da bonificação - {nomeCliente} - {nomeProduto}";
        return texto.Length > TamanhoMaximoDaObservacao ? texto[..TamanhoMaximoDaObservacao] : texto;
    }

    // O brinde dado numa venda só se desfaz excluindo a venda (que também devolve o estoque)
    public static string? MotivoParaNaoEstornar(bool estornada, Guid? vendaId)
    {
        if (estornada) return "Esta bonificação já foi estornada.";
        if (vendaId.HasValue) return "Esta bonificação foi dada dentro de uma venda. Para desfazer, exclua a venda.";
        return null;
    }
}
