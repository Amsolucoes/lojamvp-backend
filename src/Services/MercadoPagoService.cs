using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LojaApi.Services;

public class MercadoPagoService(IConfiguration config, ILogger<MercadoPagoService> logger)
{
    private readonly HttpClient _http = new();
    private string AccessToken => config["MercadoPago:AccessToken"]!;
    private const string BASE = "https://api.mercadopago.com";

    // ── Criar pagamento Pix ───────────────────────────────────────
    public async Task<MpPaymentResult> CriarPix(
        decimal valor, string descricao,
        string emailPagador, string cpfPagador,
        string nomePagador, Guid pagamentoId)
    {
        var body = new
        {
            transaction_amount = valor,
            description = descricao,
            payment_method_id = "pix",
            //external_reference = pagamentoId.ToString(),
            payer = new
            {
                email = emailPagador,
                //first_name      = nomePagador.Split(' ').First(),
                //last_name       = nomePagador.Split(' ').Skip(1).LastOrDefault() ?? "",
                identification = new { type = "CPF", number = cpfPagador.Replace(".", "").Replace("-", "") },
            },
        };

        return await PostPayment(body);
    }

    // ── Criar boleto ──────────────────────────────────────────────
    public async Task<MpPaymentResult> CriarBoleto(
        decimal valor, string descricao,
        string emailPagador, string cpfPagador,
        string nomePagador, Guid pagamentoId)
    {
        var body = new
        {
            transaction_amount = valor,
            description = descricao,
            payment_method_id = "bolbradesco",
            external_reference = pagamentoId.ToString(),
            payer = new
            {
                email = emailPagador,
                first_name = nomePagador.Split(' ').First(),
                last_name = nomePagador.Split(' ').Skip(1).LastOrDefault() ?? "",
                identification = new { type = "CPF", number = cpfPagador.Replace(".", "").Replace("-", "") },
                address = new { zip_code = "01310100", street_name = "Av. Paulista", street_number = "1000" },
            },
        };

        return await PostPayment(body);
    }

    // ── Criar pagamento cartão ────────────────────────────────────
    public async Task<MpPaymentResult> CriarCartao(
        decimal valor, string descricao,
        string cardToken, int parcelas,
        string emailPagador, string cpfPagador,
        string nomePagador, Guid pagamentoId)
    {
        var body = new
        {
            transaction_amount = valor,
            description = descricao,
            token = cardToken,
            installments = parcelas,
            payment_method_id = (string?)null, // MP detecta pelo token
            external_reference = pagamentoId.ToString(),
            payer = new
            {
                email = emailPagador,
                identification = new { type = "CPF", number = cpfPagador.Replace(".", "").Replace("-", "") },
            },
        };

        return await PostPayment(body);
    }

    // ── Verificar status de um pagamento ──────────────────────────
    public async Task<string?> VerificarStatus(string mpPaymentId)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{BASE}/v1/payments/{mpPaymentId}");
            req.Headers.Add("Authorization", $"Bearer {AccessToken}");

            var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("status").GetString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao verificar status do pagamento {Id}", mpPaymentId);
            return null;
        }
    }

    // ── POST genérico ─────────────────────────────────────────────
    private async Task<MpPaymentResult> PostPayment(object body)
    {
        logger.LogWarning(">>> ENTROU NO PostPayment");

        try
        {
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            logger.LogWarning("MP Request Body: {Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var req = new HttpRequestMessage(HttpMethod.Post, $"{BASE}/v1/payments");
            req.Headers.Add("Authorization", $"Bearer {AccessToken}");
            req.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
            req.Content = content;

            var res = await _http.SendAsync(req);
            var body2 = await res.Content.ReadAsStringAsync();

            logger.LogWarning("MP Response: {Status} {Body}", res.StatusCode, body2);

            if (!res.IsSuccessStatusCode)
                return new MpPaymentResult { Erro = $"Erro MP: {res.StatusCode} - {body2}" };

            var doc = JsonDocument.Parse(body2);
            var root = doc.RootElement;

            var result = new MpPaymentResult
            {
                MpPaymentId = root.GetProperty("id").GetInt64().ToString(),
                Status = root.GetProperty("status").GetString() ?? "",
            };

            // Pix
            if (root.TryGetProperty("point_of_interaction", out var poi) &&
                poi.TryGetProperty("transaction_data", out var td))
            {
                result.QrCode = td.TryGetProperty("qr_code", out var qr) ? qr.GetString() : null;
                result.QrCodeBase64 = td.TryGetProperty("qr_code_base64", out var qrb) ? qrb.GetString() : null;
            }

            // Boleto
            if (root.TryGetProperty("transaction_details", out var td2))
            {
                result.BoletoUrl = td2.TryGetProperty("external_resource_url", out var url) ? url.GetString() : null;
                result.BoletoBarcode = td2.TryGetProperty("barcode_content", out var bc) ? bc.GetString() : null;
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar pagamento no MP");
            return new MpPaymentResult { Erro = ex.Message };
        }
    }
}

public class MpPaymentResult
{
    public string? MpPaymentId { get; set; }
    public string? Status { get; set; }
    public string? QrCode { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? BoletoUrl { get; set; }
    public string? BoletoBarcode { get; set; }
    public string? Erro { get; set; }
    public bool Sucesso => Erro == null;
}
