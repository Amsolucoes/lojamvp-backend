using System.Net.Http.Json;

namespace LojaApi.Tests.Suporte;

// Faz login com outro papel (superadmin, operador...). O dono da loja de teste é "admin"; aqui o papel dele é trocado no banco
// e um login novo gera o token com o papel certo.
public static class CenariosPapeis
{
    public static async Task<ClienteApi> ComPapelAsync(this ApiFixture api, LojaTeste loja, string papel)
    {
        var email = $"dono-{loja.Slug[2..]}@teste.com";
        await api.NoBancoAsync(db => Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteUpdateAsync(
            db.Usuarios.Where(u => u.Email == email), s => s.SetProperty(u => u.Role, papel)));

        var http = api.Fabrica.CreateClient();
        var login = await http.PostAsJsonAsync("/api/auth/login", new { email, senha = "123456" });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("token").GetString()!;
        http.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return new ClienteApi(http);
    }

    public static Task<ClienteApi> SuperadminAsync(this ApiFixture api, LojaTeste loja) => api.ComPapelAsync(loja, "superadmin");
    public static Task<ClienteApi> OperadorAsync(this ApiFixture api, LojaTeste loja) => api.ComPapelAsync(loja, "operador");
}
