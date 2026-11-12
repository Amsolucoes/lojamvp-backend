using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.Modules.Carteira.Domain;

// Tipos de lançamento da carteira do cliente (gravados como texto no banco).
public static class TipoMovimentoCarteira
{
    public const string Compra = "compra";       // venda lançada na carteira: aumenta o que o cliente deve
    public const string Pagamento = "pagamento"; // cliente pagou: diminui o que ele deve
}

// Um lançamento na carteira ("fiado") do cliente. O saldo devedor NÃO é guardado: é sempre a soma das compras
// menos a soma dos pagamentos, para nunca ficar fora de sincronia.
public class MovimentoCarteira
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid ClienteId { get; set; }

    [Required, MaxLength(10)]
    public string Tipo { get; set; } = TipoMovimentoCarteira.Compra;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Valor { get; set; }

    public Guid? VendaId { get; set; }            // preenchido nas compras
    [MaxLength(20)]
    public string? FormaPagamento { get; set; }   // preenchido nos pagamentos: dinheiro | pix | credito | debito
    [MaxLength(300)]
    public string? Observacao { get; set; }
    public Guid? MovimentoCaixaId { get; set; }   // entrada criada no caixa quando o cliente pagou

    public DateTime Data { get; set; }            // dia da compra ou do recebimento
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public bool EhCompra() => Tipo == TipoMovimentoCarteira.Compra;

    public static MovimentoCarteira DeCompra(Guid lojaId, Guid clienteId, Guid vendaId, decimal valor, DateTime data) => new()
    {
        LojaId = lojaId, ClienteId = clienteId, Tipo = TipoMovimentoCarteira.Compra,
        VendaId = vendaId, Valor = valor, Data = data,
    };

    public static MovimentoCarteira DePagamento(Guid lojaId, Guid clienteId, decimal valor, string forma, string? observacao,
        DateTime data, Guid movimentoCaixaId) => new()
    {
        LojaId = lojaId, ClienteId = clienteId, Tipo = TipoMovimentoCarteira.Pagamento, Valor = valor,
        FormaPagamento = forma, Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
        Data = data, MovimentoCaixaId = movimentoCaixaId,
    };
}
