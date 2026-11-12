using LojaApi.Data;
using LojaApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using NSubstitute;
using Resend;

namespace LojaApi.Tests.Integracao;

[CollectionDefinition(Nome)]
public sealed class ColecaoApi : ICollectionFixture<ApiFixture>
{
    public const string Nome = "api";
}

// Uma única API em memória + um banco novo para toda a execução dos testes de integração.
// Cada teste cria a sua própria loja (NovaLojaAsync), então os testes não interferem uns nos outros.
public sealed class ApiFixture : IAsyncLifetime
{
    public const string VariavelDoBanco = "LOJA_TESTES_DB";
    public const string PerfilChacara = "20000000-0000-0000-0000-00000000000a";
    public const string PerfilLoja = "20000000-0000-0000-0000-000000000002"; // loja com caixa, produtos e vendas

    // Ex.: "Host=localhost;Port=5432;Username=postgres;Password=..." (sem Database: o banco é criado e apagado pelos testes)
    private static readonly string? Servidor = Environment.GetEnvironmentVariable(VariavelDoBanco);
    public static bool Disponivel => !string.IsNullOrWhiteSpace(Servidor);

    private WebApplicationFactory<Program>? _fabrica;
    private string? _nomeBanco;

    // E-mails ficam "de mentira": nada é enviado de verdade, e dá para conferir o que a API tentou enviar.
    public IResend Resend { get; } = Substitute.For<IResend>();

    // Mercado Pago de mentira (nenhuma chamada à internet); o teste controla o que ele responde.
    public MercadoPagoFalso MercadoPago { get; } = new();

    internal WebApplicationFactory<Program> Fabrica => _fabrica ?? throw new InvalidOperationException("API de teste não iniciada.");

    public Task InitializeAsync()
    {
        if (!Disponivel) return Task.CompletedTask;

        _nomeBanco = "loja_teste_" + Guid.NewGuid().ToString("N")[..12];
        // A API lê a connection string logo na inicialização; variável de ambiente é a forma que vale em qualquer versão.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"{Servidor!.TrimEnd(';')};Database={_nomeBanco}");

        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing"); // evita Swagger e configurações de desenvolvimento
            b.ConfigureLogging(l => l.ClearProviders()); // sem o ruído de SQL no resultado dos testes
            b.ConfigureTestServices(s =>
            {
                s.RemoveAll<IHostedService>(); // rotinas de fundo (cobrança, bloqueio, avisos) não rodam durante os testes
                s.RemoveAll<IResend>();
                s.AddSingleton(Resend);
                s.RemoveAll<MercadoPagoService>();
                s.AddSingleton<MercadoPagoService>(MercadoPago);
            });
        });
        _ = _fabrica.Server; // inicia a API (roda as migrations no banco novo)
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_fabrica is not null) await _fabrica.DisposeAsync();
        if (_nomeBanco is null) return;

        NpgsqlConnection.ClearAllPools();
        await using var conexao = new NpgsqlConnection($"{Servidor!.TrimEnd(';')};Database=postgres");
        await conexao.OpenAsync();
        await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_nomeBanco}\" WITH (FORCE)", conexao);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Atalhos ──────────────────────────────────────────────────────────────

    // Roda algo direto no banco (preparar um cenário ou conferir o que ficou gravado).
    public async Task<T> NoBancoAsync<T>(Func<AppDbContext, Task<T>> acao)
    {
        using var escopo = Fabrica.Services.CreateScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public async Task NoBancoAsync(Func<AppDbContext, Task> acao) =>
        await NoBancoAsync<object?>(async db => { await acao(db); return null; });

    public async Task<T> ServicoAsync<TServico, T>(Func<TServico, Task<T>> acao) where TServico : notnull
    {
        using var escopo = Fabrica.Services.CreateScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<TServico>());
    }

    // Cria uma loja de chácara nova (cadastro + login), já com o módulo de reservas ativo e um endereço (slug) próprio.
    public async Task<LojaTeste> NovaLojaAsync(bool moduloReservasAtivo = true, string perfilId = PerfilChacara)
    {
        var sufixo = Guid.NewGuid().ToString("N")[..10];
        var nome = $"Chácara {sufixo}";
        var email = $"dono-{sufixo}@teste.com";

        var anonimo = Fabrica.CreateClient();
        var cadastro = await anonimo.PostAsJsonAsync("/api/auth/signup", new
        {
            nomeLoja = nome,
            perfilId,
            nomeResponsavel = "Dono",
            email,
            senha = "123456",
            telefone = "67999990000",
        });
        cadastro.EnsureSuccessStatusCode();
        var token = (await cadastro.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("token").GetString()!;

        var slug = $"t-{sufixo}";
        var lojaId = await NoBancoAsync(async db =>
        {
            var loja = await db.Lojas.FirstAsync(l => l.Nome == nome);
            loja.Slug = slug;
            if (moduloReservasAtivo) loja.ModulosAtivos = "chacara_reservas";
            await db.SaveChangesAsync();
            return loja.Id;
        });

        var logado = Fabrica.CreateClient();
        logado.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return new LojaTeste(lojaId, slug, new ClienteApi(logado), new ClienteApi(anonimo));
    }
}
