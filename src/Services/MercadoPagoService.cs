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
        string nomePagador, Guid pagamentoId,
        DateTime? expiraEm = null)
    {
        var body = new
        {
            transaction_amount = valor,
            description = descricao,
            payment_method_id = "pix",
            date_of_expiration = expiraEm.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(expiraEm.Value, DateTimeKind.Utc), TimeSpan.Zero).ToString("yyyy-MM-ddTHH:mm:ss.fffK")
                : null,
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

    // ── Verificar uma cobrança recorrente (authorized_payment) ────
    public async Task<(string? preapprovalId, string? status)> VerificarCobrancaAssinatura(string authorizedPaymentId)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{BASE}/authorized_payments/{authorizedPaymentId}");
            req.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode) return (null, null);

            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var preapprovalId = root.TryGetProperty("preapproval_id", out var pre) ? pre.GetString() : null;
            var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;

            return (preapprovalId, status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao verificar cobrança de assinatura {Id}", authorizedPaymentId);
            return (null, null);
        }
    }

    // ── Criar assinatura recorrente (preapproval) ─────────────────
    public async Task<MpAssinaturaResult> CriarAssinatura(
        decimal valorMensal, string motivo,
        string cardToken, string emailPagador,
        DateTime dataInicio, string backUrl)
    {
        var body = new
        {
            reason = motivo,
            payer_email = emailPagador,
            card_token_id = cardToken,
            back_url = backUrl,
            status = "authorized",
            auto_recurring = new
            {
                frequency = 1,
                frequency_type = "months",
                transaction_amount = valorMensal,
                currency_id = "BRL",
                start_date = dataInicio.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
            },
        };

        try
        {
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            logger.LogInformation("MP Assinatura Request: {Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage(HttpMethod.Post, $"{BASE}/preapproval");
            req.Headers.Add("Authorization", $"Bearer {AccessToken}");
            req.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
            req.Content = content;

            var res = await _http.SendAsync(req);
            var respBody = await res.Content.ReadAsStringAsync();
            logger.LogInformation("MP Assinatura Response: {Status} {Body}", res.StatusCode, respBody);

            if (!res.IsSuccessStatusCode)
                return new MpAssinaturaResult { Erro = $"Erro MP: {res.StatusCode} - {respBody}" };

            var doc = JsonDocument.Parse(respBody);
            var root = doc.RootElement;

            return new MpAssinaturaResult
            {
                PreapprovalId = root.GetProperty("id").GetString(),
                Status = root.TryGetProperty("status", out var st) ? st.GetString() : null,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar assinatura MP");
            return new MpAssinaturaResult { Erro = ex.Message };
        }
    }

    // ── Verificar status de uma assinatura ────────────────────────
    public async Task<string?> VerificarAssinatura(string preapprovalId)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{BASE}/preapproval/{preapprovalId}");
            req.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("status").GetString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao verificar assinatura {Id}", preapprovalId);
            return null;
        }
    }

    // ── Cancelar assinatura ───────────────────────────────────────
    public async Task<bool> CancelarAssinatura(string preapprovalId)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { status = "cancelled" });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage(HttpMethod.Put, $"{BASE}/preapproval/{preapprovalId}");
            req.Headers.Add("Authorization", $"Bearer {AccessToken}");
            req.Content = content;
            var res = await _http.SendAsync(req);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao cancelar assinatura {Id}", preapprovalId);
            return false;
        }
    }

    // ── Atualizar valor de uma assinatura ─────────────────────────
    public async Task<bool> AtualizarValorAssinatura(string preapprovalId, decimal novoValor)
    {
        try
        {
            var body = new
            {
                auto_recurring = new
                {
                    transaction_amount = novoValor,
                    currency_id = "BRL",
                },
            };
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage(HttpMethod.Put, $"{BASE}/preapproval/{preapprovalId}");
            req.Headers.Add("Authorization", $"Bearer {AccessToken}");
            req.Content = content;

            var res = await _http.SendAsync(req);
            var respBody = await res.Content.ReadAsStringAsync();
            logger.LogInformation("MP Atualizar assinatura: {Status} {Body}", res.StatusCode, respBody);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao atualizar valor da assinatura {Id}", preapprovalId);
            return false;
        }
    }

    // ── POST genérico ─────────────────────────────────────────────
    private async Task<MpPaymentResult> PostPayment(object body)
    {
        try
        {
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            logger.LogInformation("MP Request Body: {Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var req = new HttpRequestMessage(HttpMethod.Post, $"{BASE}/v1/payments");
            req.Headers.Add("Authorization", $"Bearer {AccessToken}");
            req.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
            req.Content = content;

            var res = await _http.SendAsync(req);
            var body2 = await res.Content.ReadAsStringAsync();

            logger.LogInformation("MP Response: {Status} {Body}", res.StatusCode, body2);

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

public class MpAssinaturaResult
{
    public string? PreapprovalId { get; set; }
    public string? Status { get; set; }
    public string? Erro { get; set; }
    public bool Sucesso => Erro == null;
}
