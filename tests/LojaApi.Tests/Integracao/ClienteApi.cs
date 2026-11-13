using System.Net;
using System.Text.Json;

namespace LojaApi.Tests.Integracao;

// Resposta da API já lida, para os testes poderem conferir status e corpo sem cerimônia.
public sealed record Resposta(HttpStatusCode Status, string Corpo, string? TipoConteudo, byte[] Bytes, string? Local = null)
{
    public int Codigo => (int)Status;
    public bool Sucesso => Codigo is >= 200 and < 300;

    public JsonElement Json => JsonDocument.Parse(Corpo).RootElement.Clone();

    // Mensagem de erro no formato { "erro": "..." } que a API devolve nas regras de negócio.
    public string Erro => Json.GetProperty("erro").GetString()!;

    public int Id => Json.GetProperty("id").GetInt32();
}

public sealed class ClienteApi(HttpClient http)
{
    public Task<Resposta> Get(string url) => Enviar(HttpMethod.Get, url, null);
    public Task<Resposta> Post(string url, object? corpo = null) => Enviar(HttpMethod.Post, url, corpo);
    public Task<Resposta> Put(string url, object? corpo = null) => Enviar(HttpMethod.Put, url, corpo);
    public Task<Resposta> Patch(string url, object? corpo = null) => Enviar(HttpMethod.Patch, url, corpo);
    public Task<Resposta> Delete(string url) => Enviar(HttpMethod.Delete, url, null);

    private async Task<Resposta> Enviar(HttpMethod metodo, string url, object? corpo)
    {
        using var req = new HttpRequestMessage(metodo, url);
        if (corpo is not null) req.Content = JsonContent.Create(corpo);
        using var res = await http.SendAsync(req);
        var bytes = await res.Content.ReadAsByteArrayAsync();
        return new Resposta(res.StatusCode, System.Text.Encoding.UTF8.GetString(bytes), res.Content.Headers.ContentType?.MediaType, bytes, res.Headers.Location?.ToString());
    }
}

// Uma loja de chácara criada para o teste: Painel = dono logado; Site = visitante (sem login).
public sealed record LojaTeste(Guid Id, string Slug, ClienteApi Painel, ClienteApi Site)
{
    public string Publico => $"/api/publico/{Slug}/chacara";
}
