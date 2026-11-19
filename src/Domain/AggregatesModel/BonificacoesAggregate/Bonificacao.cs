using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.Domain.Bonificacoes;

// Um brinde que a loja deu a um cliente (por exemplo, uma pulseira numa compra). Fica registrado no cliente e dá baixa no estoque.
// O custo do produto e os nomes são guardados no momento do brinde, para o histórico e o relatório não mudarem se o cadastro mudar
// (e para continuarem existindo se o cliente ou o produto forem excluídos depois).
public class Bonificacao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    public Guid? ClienteId { get; set; }          // vira nulo se o cliente for excluído (o histórico e o custo ficam)
    [Required, MaxLength(150)]
    public string NomeCliente { get; set; } = "";

    public Guid? ProdutoId { get; set; }          // vira nulo se o produto for excluído
    public Guid? VariacaoId { get; set; }         // tamanho/cor, quando o produto tem grade
    [Required, MaxLength(200)]
    public string NomeProduto { get; set; } = ""; // já com a variação ("Pulseira - M / Azul")

    [Column(TypeName = "decimal(10,3)")]
    public decimal Quantidade { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal CustoUnitario { get; set; }    // preço de custo do produto na hora do brinde

    [MaxLength(300)]
    public string? Observacao { get; set; }

    public Guid? VendaId { get; set; }            // preenchido quando o brinde foi dado dentro de uma venda
    public Guid? UsuarioId { get; set; }          // quem deu o brinde

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? EstornadaEm { get; set; }    // brinde desfeito (o estoque voltou)

    public bool Estornada() => EstornadaEm.HasValue;

    // Quanto o brinde custou para a loja
    public decimal CustoTotal() => BonificacoesRegras.CustoTotal(Quantidade, CustoUnitario);
}
