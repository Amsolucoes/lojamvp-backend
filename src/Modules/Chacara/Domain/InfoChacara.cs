using LojaApi.Models;

namespace LojaApi.Modules.Chacara.Domain;

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

    // Dados mínimos do locador para o contrato sair completo
    public bool LocadorCompleto() =>
        !string.IsNullOrWhiteSpace(LocadorNome)
        && (LocadorCpf ?? "").Count(char.IsDigit) == 11
        && !string.IsNullOrWhiteSpace(LocadorEndereco);

    private static string? Limpar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    // Atualiza os dados cadastrais da chácara (descrição, endereço, horários padrão, locador do contrato...).
    // Campos opcionais em branco viram null; horários em branco voltam ao padrão (08:00 / 22:00).
    public void AtualizarDados(
        string? descricao, string? endereco, string? comodidades, string? comodidadesExtras, string? mapaEmbedUrl,
        string? horaEntrada, string? horaSaida, string? avisosUso,
        string? locadorNome, string? locadorRg, string? locadorCpf, string? locadorEndereco, string? locadorTelefone, string? cidadeAssinatura)
    {
        Descricao = descricao?.Trim() ?? "";
        Endereco = endereco?.Trim() ?? "";
        Comodidades = comodidades ?? "";
        ComodidadesExtras = Limpar(comodidadesExtras);
        MapaEmbedUrl = Limpar(mapaEmbedUrl);
        HoraEntrada = string.IsNullOrWhiteSpace(horaEntrada) ? "08:00" : horaEntrada.Trim();
        HoraSaida = string.IsNullOrWhiteSpace(horaSaida) ? "22:00" : horaSaida.Trim();
        AvisosUso = Limpar(avisosUso);
        LocadorNome = Limpar(locadorNome);
        LocadorRg = Limpar(locadorRg);
        LocadorCpf = Limpar(locadorCpf);
        LocadorEndereco = Limpar(locadorEndereco);
        LocadorTelefone = Limpar(locadorTelefone);
        CidadeAssinatura = Limpar(cidadeAssinatura);
    }

    // Guarda as credenciais do Mercado Pago da própria chácara (o token já vem cifrado)
    public void DefinirPagamentoOnline(string tokenCifrado, string publicKey)
    {
        MpAccessTokenCifrado = tokenCifrado;
        MpPublicKey = publicKey.Trim();
    }

    public void RemoverPagamentoOnline()
    {
        MpAccessTokenCifrado = null;
        MpPublicKey = null;
    }
}