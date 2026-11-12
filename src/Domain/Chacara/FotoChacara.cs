using LojaApi.Models;

namespace LojaApi.Domain.Chacara;

public class FotoChacara
{
    public const int LimitePorLoja = 30;

    public int Id { get; set; }
    public Guid LojaId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Ordem { get; set; }

    public Loja? Loja { get; set; }

    public static FotoChacara Criar(Guid lojaId, string url, int ordem) => new()
    {
        LojaId = lojaId, Url = url.Trim(), Ordem = ordem,
    };
}
