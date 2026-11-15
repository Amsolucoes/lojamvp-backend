using System.Text.Json;
using System.Text.Json.Serialization;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Backup completo da loja em JSON (catálogo, clientes, vendas, trocas, movimentos, categorias e faturas) para baixar.
public class BackupDaLojaQueryHandler(IPlataformaRepository plataforma, IAdminRepository admin) : IQueryHandler<BackupDaLojaQuery>
{
    public async Task<Resultado<object>> HandleAsync(BackupDaLojaQuery query)
    {
        var loja = await plataforma.ObterLojaAsync(query.Id);
        if (loja is null) return Resultado.NaoEncontrado();

        var d = await admin.ObterDadosDeBackupAsync(query.Id);

        var backup = new
        {
            geradoEm = DateTime.UtcNow,
            loja = new { loja.Id, loja.Nome, loja.Email, loja.Cnpj, loja.Cpf, loja.Telefone, loja.Endereco, loja.Status, loja.CriadoEm },
            categorias = d.Categorias,
            produtos = d.Produtos,
            clientes = d.Clientes,
            vendas = d.Vendas,
            trocas = d.Trocas,
            movimentos = d.Movimentos,
            pagamentos = d.Pagamentos,
            totais = new
            {
                produtos = d.Produtos.Count,
                clientes = d.Clientes.Count,
                vendas = d.Vendas.Count,
                trocas = d.Trocas.Count,
            }
        };

        var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        });

        var nomeArquivo = $"backup-{loja.Nome.Replace(" ", "-").ToLower()}-{DateTime.UtcNow:yyyy-MM-dd}.json";
        return Resultado.Ok(new ArquivoDto(System.Text.Encoding.UTF8.GetBytes(json), "application/json", nomeArquivo));
    }
}
