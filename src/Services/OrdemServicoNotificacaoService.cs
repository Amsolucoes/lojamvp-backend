using LojaApi.Data;
using LojaApi.src.Models.OrdemServico;
using Microsoft.EntityFrameworkCore;
using Resend;

namespace LojaApi.src.Services;

public class OrdemServicoNotificacaoService(AppDbContext db, IResend resend, ILogger<OrdemServicoNotificacaoService> logger)
{
    public record ResultadoEnvio(bool Enviado, string? Erro);

    public async Task<ResultadoEnvio> EnviarPorEmailAsync(Guid orcamentoId, Guid lojaId)
    {
        var orcamento = await db.OrcamentosServico
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == orcamentoId && o.LojaId == lojaId);
        if (orcamento is null) return new ResultadoEnvio(false, "Orçamento não encontrado.");

        var cliente = await db.Clientes.FindAsync(orcamento.ClienteId);
        if (cliente is null || string.IsNullOrWhiteSpace(cliente.Email))
            return new ResultadoEnvio(false, "Cliente não tem e-mail cadastrado.");

        var loja = await db.Lojas.FindAsync(lojaId);
        if (loja is null) return new ResultadoEnvio(false, "Loja não encontrada.");

        var linhas = string.Join("", orcamento.Itens.Select(i =>
            $"<tr><td style='padding:6px 10px;border-bottom:1px solid #eee'>{i.Descricao} ({i.Quantidade}x)</td>" +
            $"<td style='padding:6px 10px;border-bottom:1px solid #eee;text-align:right'>R$ {i.ValorTotal:N2}</td></tr>"));

        var veiculo = string.Join(" · ", new[] { orcamento.VeiculoDescricao, orcamento.Placa }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var logoHtml = string.IsNullOrWhiteSpace(loja.LogoUrl)
            ? $"<h2 style='color:#2f7d4f'>{loja.Nome}</h2>"
            : $"<img src='{loja.LogoUrl}' alt='{loja.Nome}' style='max-height:60px;max-width:220px;margin-bottom:8px' />";

        var contatoLinhas = string.Join("", new[]
        {
            !string.IsNullOrWhiteSpace(loja.Endereco) ? $"<div>{loja.Endereco}</div>" : "",
            !string.IsNullOrWhiteSpace(loja.Telefone) ? $"<div>Tel/WhatsApp: {FormatarTelefone(loja.Telefone)}</div>" : "",
        });

        var html = $@"
            <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                <div style='text-align:center;margin-bottom:12px'>{logoHtml}</div>
                <h3 style='color:#2f7d4f;margin-bottom:4px'>Orçamento</h3>
                <p>Olá, {cliente.Nome}! Segue o orçamento{(string.IsNullOrEmpty(veiculo) ? "" : $" referente ao veículo <strong>{veiculo}</strong>")}.</p>
                <table style='width:100%;border-collapse:collapse;margin-top:12px'>{linhas}</table>
                <table style='width:100%;border-collapse:collapse;margin-top:8px'>
                    <tr><td style='padding:6px 10px;font-weight:600'>Total</td><td style='padding:6px 10px;text-align:right;font-weight:600'>R$ {orcamento.ValorTotal:N2}</td></tr>
                </table>
                {(string.IsNullOrWhiteSpace(orcamento.Observacoes) ? "" : $"<p style='margin-top:16px;font-size:13px;color:#666'><strong>Observações:</strong> {orcamento.Observacoes}</p>")}
                <div style='margin-top:20px;padding-top:12px;border-top:1px solid #eee;font-size:13px;color:#888;text-align:center'>
                    <div style='font-weight:600;color:#555'>{loja.Nome}</div>
                    {contatoLinhas}
                </div>
            </div>";

        try
        {
            var msg = new EmailMessage
            {
                From = "AldevSoftware <ordemservico@aldevsoftware.com.br>",
                Subject = $"Orçamento — {loja.Nome}" + (string.IsNullOrEmpty(veiculo) ? "" : $" ({veiculo})"),
                HtmlBody = html,
            };
            msg.To.Add(cliente.Email);
            await resend.EmailSendAsync(msg);
            logger.LogInformation("Orçamento {OrcamentoId} enviado por e-mail para {Email}.", orcamento.Id, cliente.Email);
            return new ResultadoEnvio(true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar orçamento {OrcamentoId} por e-mail.", orcamento.Id);
            return new ResultadoEnvio(false, "Falha ao enviar o e-mail. Tente novamente.");
        }
    }

    // Formata telefone só para exibição — não altera o valor salvo no banco.
    // Aceita 10 dígitos (fixo, com DDD) ou 11 dígitos (celular, com DDD).
    private static string FormatarTelefone(string telefone)
    {
        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        return digitos.Length switch
        {
            11 => $"({digitos[..2]}) {digitos[2..7]}-{digitos[7..]}",
            10 => $"({digitos[..2]}) {digitos[2..6]}-{digitos[6..]}",
            _ => telefone, // formato inesperado — mostra como veio, sem quebrar
        };
    }
}