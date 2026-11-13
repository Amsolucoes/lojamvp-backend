using LojaApi.Domain.Carteira;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Integracao;

// Cadastro de clientes da loja.
[Collection(ColecaoApi.Nome)]
public class ClientesApiTests(ApiFixture api)
{
    private static object Cliente(string nome = "Maria Silva", string telefone = "67999990000", string? cpf = null, string? email = null, string? observacoes = null) =>
        new { nome, telefone, cpf, email, endereco = (string?)null, observacoes, dataNascimento = (DateTime?)null };

    private async Task<LojaTeste> NovaAsync() => await api.NovaLojaAsync(moduloReservasAtivo: false);

    private Task<Venda> VendaAsync(LojaTeste loja, Guid clienteId, decimal total) => api.NoBancoAsync(async db =>
    {
        var v = new Venda { LojaId = loja.Id, ClienteId = clienteId, Total = total, TotalFinal = total, FormaPagamento = "pix" };
        db.Vendas.Add(v);
        await db.SaveChangesAsync();
        return v;
    });

    [FatoIntegracao]
    public async Task Exige_login()
    {
        var loja = await NovaAsync();
        Assert.Equal(401, (await loja.Site.Get("/api/clientes")).Codigo);
    }

    [FatoIntegracao]
    public async Task Criar_devolve_201_com_o_cadastro_zerado_e_o_endereco_para_buscar()
    {
        var loja = await NovaAsync();

        var r = await loja.Painel.Post("/api/clientes", Cliente(cpf: "11122233344", email: "m@x.com", observacoes: "VIP"));

        Assert.Equal(201, r.Codigo);
        var c = r.Json;
        Assert.Equal(("Maria Silva", "67999990000", "11122233344", "m@x.com", "VIP"),
            (c.GetProperty("nome").GetString(), c.GetProperty("telefone").GetString(), c.GetProperty("cpf").GetString(), c.GetProperty("email").GetString(), c.GetProperty("observacoes").GetString()));
        Assert.Equal((0, 0m, 0m), (c.GetProperty("totalCompras").GetInt32(), c.GetProperty("totalGasto").GetDecimal(), c.GetProperty("saldoCarteira").GetDecimal()));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, c.GetProperty("limiteCarteira").ValueKind);
        var id = c.GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/clientes/{id}", r.Local);
        Assert.Equal(loja.Id, (await api.NoBancoAsync(db => db.Clientes.AsNoTracking().FirstAsync(x => x.Id == id))).LojaId);
    }

    [FatoIntegracao]
    public async Task CPF_repetido_na_mesma_loja_e_recusado_mas_vale_em_outra_loja()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        (await a.Painel.Post("/api/clientes", Cliente(cpf: "11122233344"))).DeveSerSucesso();

        var repetido = await a.Painel.Post("/api/clientes", Cliente("Outra", "67911110000", "11122233344"));
        Assert.Equal((409, "CPF já cadastrado."), (repetido.Codigo, repetido.Erro));

        Assert.Equal(201, (await b.Painel.Post("/api/clientes", Cliente(cpf: "11122233344"))).Codigo);
        Assert.Equal(201, (await a.Painel.Post("/api/clientes", Cliente("Sem CPF 1", "67900000001"))).Codigo);   // vazio não conflita
        Assert.Equal(201, (await a.Painel.Post("/api/clientes", Cliente("Sem CPF 2", "67900000002", cpf: " "))).Codigo);
    }

    [FatoIntegracao]
    public async Task Listar_vem_em_ordem_de_nome_so_da_loja_e_filtra_por_nome_telefone_ou_cpf()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        await a.Painel.Post("/api/clientes", Cliente("Zeca Souza", "67911110000", "11111111111"));
        await a.Painel.Post("/api/clientes", Cliente("Ana Lima", "67922220000", "22222222222"));
        await a.Painel.Post("/api/clientes", Cliente("Bruno Dias", "67933330000"));
        await b.Painel.Post("/api/clientes", Cliente("Cliente da B", "67944440000"));

        async Task<List<string?>> Nomes(string query) =>
            (await a.Painel.Get("/api/clientes" + query)).Json.EnumerateArray().Select(x => x.GetProperty("nome").GetString()).ToList();

        Assert.Equal(["Ana Lima", "Bruno Dias", "Zeca Souza"], await Nomes(""));
        Assert.Equal(["Zeca Souza"], await Nomes("?busca=ZECA"));          // nome, sem diferenciar maiúsculas
        Assert.Equal(["Ana Lima"], await Nomes("?busca=2222"));            // telefone ou CPF
        Assert.Equal(["Zeca Souza"], await Nomes("?busca=11111"));
        Assert.Empty(await Nomes("?busca=nao-existe"));
        Assert.Equal(3, (await Nomes("?busca=%20")).Count);                // só espaços = sem filtro
    }

    [FatoIntegracao]
    public async Task Listar_e_buscar_trazem_total_de_compras_gasto_e_saldo_da_carteira()
    {
        var loja = await NovaAsync();
        var id = (await loja.Painel.Post("/api/clientes", Cliente())).Json.GetProperty("id").GetGuid();
        var v1 = await VendaAsync(loja, id, 100);
        await VendaAsync(loja, id, 50);
        await api.NoBancoAsync(async db =>
        {
            db.MovimentosCarteira.Add(MovimentoCarteira.DeCompra(loja.Id, id, v1.Id, 30m, DateTime.UtcNow));
            await db.SaveChangesAsync();
        });

        var lista = (await loja.Painel.Get("/api/clientes")).Json[0];
        var busca = (await loja.Painel.Get($"/api/clientes/{id}")).Json;

        foreach (var c in new[] { lista, busca })
            Assert.Equal((2, 150m, 30m), (c.GetProperty("totalCompras").GetInt32(), c.GetProperty("totalGasto").GetDecimal(), c.GetProperty("saldoCarteira").GetDecimal()));
    }

    [FatoIntegracao]
    public async Task Buscar_dá_404_para_inexistente_e_para_cliente_de_outra_loja()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var idA = (await a.Painel.Post("/api/clientes", Cliente())).Json.GetProperty("id").GetGuid();

        Assert.Equal(200, (await a.Painel.Get($"/api/clientes/{idA}")).Codigo);
        Assert.Equal(404, (await b.Painel.Get($"/api/clientes/{idA}")).Codigo);
        Assert.Equal(404, (await a.Painel.Get($"/api/clientes/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Atualizar_troca_os_dados_e_devolve_o_total_de_compras()
    {
        var loja = await NovaAsync();
        var id = (await loja.Painel.Post("/api/clientes", Cliente(cpf: "11122233344"))).Json.GetProperty("id").GetGuid();
        await VendaAsync(loja, id, 80);

        var r = await loja.Painel.Put($"/api/clientes/{id}", Cliente("Maria Nova", "67988880000", "11122233344", "novo@x.com"));   // mesmo CPF dele mesmo: ok

        Assert.Equal(200, r.Codigo);
        Assert.Equal(("Maria Nova", "67988880000", "novo@x.com", 1, 80m),
            (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("telefone").GetString(), r.Json.GetProperty("email").GetString(),
             r.Json.GetProperty("totalCompras").GetInt32(), r.Json.GetProperty("totalGasto").GetDecimal()));
    }

    [FatoIntegracao]
    public async Task Atualizar_recusa_CPF_de_outro_cliente_da_loja_e_404_para_de_outra_loja()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        await a.Painel.Post("/api/clientes", Cliente("Primeiro", "67911110000", "11111111111"));
        var segundo = (await a.Painel.Post("/api/clientes", Cliente("Segundo", "67922220000", "22222222222"))).Json.GetProperty("id").GetGuid();

        var conflito = await a.Painel.Put($"/api/clientes/{segundo}", Cliente("Segundo", "67922220000", "11111111111"));
        Assert.Equal((409, "CPF já cadastrado."), (conflito.Codigo, conflito.Erro));

        Assert.Equal(404, (await b.Painel.Put($"/api/clientes/{segundo}", Cliente("Invasor", "67900000000"))).Codigo);
        Assert.Equal("Segundo", (await api.NoBancoAsync(db => db.Clientes.AsNoTracking().FirstAsync(x => x.Id == segundo))).Nome);
    }

    [FatoIntegracao]
    public async Task Excluir_exige_admin_remove_e_da_404_na_segunda_vez()
    {
        var loja = await NovaAsync();
        var id = (await loja.Painel.Post("/api/clientes", Cliente())).Json.GetProperty("id").GetGuid();

        var operador = await api.OperadorAsync(loja);
        Assert.Equal(403, (await operador.Delete($"/api/clientes/{id}")).Codigo);
        Assert.Equal(1, await api.NoBancoAsync(db => db.Clientes.CountAsync(c => c.Id == id)));

        var admin = await api.ComPapelAsync(loja, "admin");
        Assert.Equal(204, (await admin.Delete($"/api/clientes/{id}")).Codigo);
        Assert.Equal(404, (await admin.Delete($"/api/clientes/{id}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Cliente_com_saldo_na_carteira_nao_pode_ser_excluido()
    {
        var loja = await NovaAsync();
        var id = (await loja.Painel.Post("/api/clientes", Cliente())).Json.GetProperty("id").GetGuid();
        var venda = await VendaAsync(loja, id, 30);
        await api.NoBancoAsync(async db => { db.MovimentosCarteira.Add(MovimentoCarteira.DeCompra(loja.Id, id, venda.Id, 30m, DateTime.UtcNow)); await db.SaveChangesAsync(); });

        var r = await loja.Painel.Delete($"/api/clientes/{id}");

        Assert.Equal((400, "Este cliente tem saldo a pagar na carteira. Receba o pagamento antes de excluir o cadastro."), (r.Codigo, r.Erro));
        Assert.Equal(1, await api.NoBancoAsync(db => db.Clientes.CountAsync(c => c.Id == id)));
    }

    [FatoIntegracao]
    public async Task Outra_loja_nao_exclui_cliente_alheio()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var id = (await a.Painel.Post("/api/clientes", Cliente())).Json.GetProperty("id").GetGuid();

        Assert.Equal(404, (await b.Painel.Delete($"/api/clientes/{id}")).Codigo);
        Assert.Equal(1, await api.NoBancoAsync(db => db.Clientes.CountAsync(c => c.Id == id)));
    }
}
