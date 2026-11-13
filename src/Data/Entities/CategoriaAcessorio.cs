using System.ComponentModel.DataAnnotations;

namespace LojaApi.src.Models;

public class CategoriaAcessorio
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(60)]
    public string Nome { get; set; } = "";

    // Chave estável usada no campo Categoria do produto (ex: "leitor_codigo_barras") —
    // gerada a partir do nome na criação, não muda depois pra não quebrar produtos já salvos.
    [Required, MaxLength(60)]
    public string Chave { get; set; } = "";

    public int Ordem { get; set; } = 0;
    public bool Ativa { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}