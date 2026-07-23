using LojaApi.Models;

namespace LojaApi.src.Models;

public class InfoChacara
{
    public int Id { get; set; }
    public Guid LojaId { get; set; }

    public string Descricao { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;

    public string Comodidades { get; set; } = string.Empty;     // csv de chaves fixas, ex: "piscina,wifi,churrasqueira"
    public string? ComodidadesExtras { get; set; }              // texto livre, item por linha

    public Loja? Loja { get; set; }
}