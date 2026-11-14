namespace LojaApi.Domain.Acessorios;

// Situações possíveis de um pedido de acessório (gravadas como texto no banco).
public static class StatusPedidoAcessorio
{
    public const string AguardandoPagamento = "aguardando_pagamento";
    public const string Pago = "pago";
    public const string Enviado = "enviado";
    public const string Entregue = "entregue";
    public const string Cancelado = "cancelado";

    public static readonly string[] Validos = [AguardandoPagamento, Pago, Enviado, Entregue, Cancelado];
}

// Regras puras da loja de acessórios (sem banco): frete, prazo do pedido e chave das categorias.
public static class AcessoriosRegras
{
    // Tabela simples de frete fixo por região, enquanto não integra Correios/Melhor Envio.
    private static readonly Dictionary<string, decimal> FretePorUf = new()
    {
        ["MS"] = 15m, // região da loja — mais barato
        ["SP"] = 25m,
        ["RJ"] = 25m,
        ["MG"] = 25m,
        ["PR"] = 25m,
        ["SC"] = 25m,
        ["RS"] = 25m,
        ["GO"] = 25m,
        ["DF"] = 25m,
        ["MT"] = 25m,
    };

    public const decimal FretePadrao = 35m; // demais estados

    // Frete grátis acima de X de subtotal — null mantém desativado. Para ativar, devolva um valor (ex.: 200m).
    public static decimal? FreteGratisAcimaDe => null;

    // Quanto tempo o cliente tem para pagar antes de o estoque reservado voltar
    public static readonly TimeSpan PrazoPagamento = TimeSpan.FromMinutes(30);

    // Produto criado há menos de X dias aparece como "novo" na vitrine
    public const int DiasProdutoNovo = 14;

    // Frete da região, sem olhar o subtotal (a UF já vem em maiúsculas)
    public static decimal FreteDaUf(string uf) => FretePorUf.TryGetValue(uf, out var valor) ? valor : FretePadrao;

    public static decimal CalcularFrete(decimal subtotal, string uf)
    {
        if (FreteGratisAcimaDe.HasValue && subtotal >= FreteGratisAcimaDe.Value) return 0m;
        return FreteDaUf(uf);
    }

    public static DateTime LimiteProdutoNovo(DateTime agora) => agora.AddDays(-DiasProdutoNovo);

    // "Câmeras & Áudio" -> "cameras_audio": minúsculo, sem acento, símbolos viram "_" (sem repetir nem nas pontas)
    public static string GerarChaveCategoria(string nome)
    {
        var normalizado = nome.Trim().ToLowerInvariant();
        var semAcento = new string(normalizado
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray());
        var chave = new string(semAcento.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        while (chave.Contains("__")) chave = chave.Replace("__", "_");
        return chave.Trim('_');
    }
}
