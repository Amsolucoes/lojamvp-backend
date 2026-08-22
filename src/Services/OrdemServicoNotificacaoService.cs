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

        var html = $@"
            <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                <h2 style='color:#2f7d4f'>Orçamento — {loja.Nome}</h2>
                <p>Olá, {cliente.Nome}! Segue o orçamento{(string.IsNullOrEmpty(veiculo) ? "" : $" referente ao veículo <strong>{veiculo}</strong>")}.</p>
                <table style='width:100%;border-collapse:collapse;margin-top:12px'>{linhas}</table>
                <table style='width:100%;border-collapse:collapse;margin-top:8px'>
                    <tr><td style='padding:6px 10px;font-weight:600'>Total</td><td style='padding:6px 10px;text-align:right;font-weight:600'>R$ {orcamento.ValorTotal:N2}</td></tr>
                </table>
                {(string.IsNullOrWhiteSpace(orcamento.Observacoes) ? "" : $"<p style='margin-top:16px;font-size:13px;color:#666'><strong>Observações:</strong> {orcamento.Observacoes}</p>")}
                <p style='margin-top:20px;font-size:13px;color:#888'>Qualquer dúvida, entre em contato com {loja.Nome}.</p>
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
}