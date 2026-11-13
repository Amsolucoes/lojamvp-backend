using LojaApi.Models;
using System.ComponentModel.DataAnnotations;

namespace LojaApi.src.Models;

// Guarda o "de-para": código do produto no fornecedor -> produto na loja.
// Usado pra próximas notas do mesmo fornecedor casarem automaticamente.
public class NfProdutoMapeamento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    [MaxLength(14)]
    public string CnpjFornecedor { get; set; } = "";
    [MaxLength(60)]
    public string CodigoFornecedor { get; set; } = ""; // cProd do XML
    public Guid ProdutoId { get; set; }
    public Produto? Produto { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}