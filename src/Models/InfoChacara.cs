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
    public string? MapaEmbedUrl { get; set; }                   // URL de embed do Google Maps (Compartilhar > Incorporar mapa), sobrepõe o cálculo automático via endereço
    public string HoraEntrada { get; set; } = "08:00";
    public string HoraSaida { get; set; } = "22:00";
    public string? AvisosUso { get; set; }                       // regras/proibições — um item por linha, mostrado antes de confirmar
    // Dados do locador (você), usados no contrato de locação
    public string? LocadorNome { get; set; }
    public string? LocadorRg { get; set; }
    public string? LocadorCpf { get; set; }
    public string? LocadorEndereco { get; set; }
    public string? LocadorTelefone { get; set; }
    public string? CidadeAssinatura { get; set; } // ex: "Campo Grande – MS", usado no fecho do contrato

    // Pagamento online (Mercado Pago) da própria loja. O token fica cifrado e nunca é devolvido pela API.
    [System.Text.Json.Serialization.JsonIgnore]
    public string? MpAccessTokenCifrado { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string? MpPublicKey { get; set; }
    // true = esta loja recebe na conta da plataforma (credencial do servidor); novas lojas começam em false
    [System.Text.Json.Serialization.JsonIgnore]
    public bool MpContaPlataforma { get; set; }

    public Loja? Loja { get; set; }
}