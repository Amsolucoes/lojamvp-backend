using LojaApi.Models;

namespace LojaApi.src.Models;

public class FotoChacara
{
    public int Id { get; set; }
    public Guid LojaId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Ordem { get; set; }

    public Loja? Loja { get; set; }
}