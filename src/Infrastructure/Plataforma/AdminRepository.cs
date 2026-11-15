using LojaApi.Application.Plataforma;
using LojaApi.Data;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Plataforma;

// Acesso aos dados do painel do superadmin (EF Core). Só lê e grava; as regras ficam nos handlers.
public class AdminRepository(AppDbContext db) : IAdminRepository
{
    // ── Lojas ──

    public Task<List<Loja>> ListarLojasReaisComPagamentosAsync() => db.Lojas.Include(l => l.Pagamentos).Where(l => !l.EhTeste).ToListAsync();

    public Task<List<Pagamento>> ListarUltimosPagamentosPagosAsync(int quantidade) =>
        db.Pagamentos
            .Include(p => p.Loja)
            .Where(p => p.Status == "pago" && !p.Loja!.EhTeste)
            .OrderByDescending(p => p.PagoEm)
            .Take(quantidade)
            .ToListAsync();

    public Task<List<Loja>> ListarLojasAsync(StatusLoja? status, string? busca)
    {
        var q = db.Lojas.Include(l => l.Pagamentos).Include(l => l.Usuarios).ThenInclude(ul => ul.Usuario).AsQueryable();

        if (status.HasValue)
            q = q.Where(l => l.Status == status.Value);

        if (!string.IsNullOrEmpty(busca))
            q = q.Where(l => l.Nome.ToLower().Contains(busca.ToLower()) ||
                              l.Email.Contains(busca) ||
                              (l.Cnpj != null && l.Cnpj.Contains(busca)));

        return q.OrderByDescending(l => l.CriadoEm).ToListAsync();
    }

    public Task<Loja?> ObterLojaCompletaAsync(Guid id) =>
        db.Lojas.Include(l => l.Pagamentos).Include(l => l.Usuarios).ThenInclude(u => u.Usuario).FirstOrDefaultAsync(l => l.Id == id);

    public Task<bool> LojaExisteAsync(Guid id) => db.Lojas.AnyAsync(l => l.Id == id);

    public Task<bool> CnpjDeLojaExisteAsync(string cnpj) => db.Lojas.AnyAsync(l => l.Cnpj == cnpj);

    public Task<UsuarioLoja?> ObterVinculoAdminAsync(Guid lojaId, bool apenasAtivo) =>
        db.UsuariosLoja.Include(ul => ul.Usuario).FirstOrDefaultAsync(ul => ul.LojaId == lojaId && ul.Role == "admin" && (!apenasAtivo || ul.Ativo));

    public Task<List<Loja>> ListarLojasNaoCanceladasAsync() => db.Lojas.Where(l => l.Status != StatusLoja.Cancelado).ToListAsync();

    // ── Faturas e comunicados ──

    public Task<List<Pagamento>> ListarPagamentosDaLojaAsync(Guid lojaId) =>
        db.Pagamentos.Include(p => p.Loja).Where(p => p.LojaId == lojaId).OrderByDescending(p => p.Vencimento).ToListAsync();

    public Task<List<ComunicadoDto>> ListarComunicadosAsync() =>
        db.ComunicadosEnviados
            .OrderByDescending(c => c.EnviadoEm)
            .Select(c => new ComunicadoDto(
                c.Id, c.Assunto, c.CorpoHtml, c.TodasLojas, c.TotalEnviados, c.TotalFalhas,
                c.DestinatariosSucesso.Split(',', StringSplitOptions.RemoveEmptyEntries),
                c.DestinatariosFalha.Split(',', StringSplitOptions.RemoveEmptyEntries),
                c.EnviadoEm))
            .ToListAsync();

    // ── Backup e exclusão ──

    // AsNoTracking em tudo: sem isso, o EF faz "relationship fixup" automático entre entidades carregadas na mesma instância de
    // contexto, criando um grafo muito mais profundo do que os Include() pedem e estourando o limite de profundidade do serializador JSON.
    public async Task<DadosDeBackup> ObterDadosDeBackupAsync(Guid lojaId)
    {
        var produtos = await db.Produtos.Where(p => p.LojaId == lojaId).Include(p => p.Variacoes).AsNoTracking().ToListAsync();
        var clientes = await db.Clientes.Where(c => c.LojaId == lojaId).AsNoTracking().ToListAsync();
        var vendas = await db.Vendas.Where(v => v.LojaId == lojaId).Include(v => v.Itens).AsNoTracking().ToListAsync();
        var trocas = await db.Trocas.Where(t => t.LojaId == lojaId).Include(t => t.Itens).AsNoTracking().ToListAsync();
        var movimentos = await db.Movimentos.Where(m => m.LojaId == lojaId).AsNoTracking().ToListAsync();
        var categorias = await db.CategoriasLoja.Where(c => c.LojaId == lojaId).AsNoTracking().ToListAsync();
        var pagamentos = await db.Pagamentos.Where(p => p.LojaId == lojaId).AsNoTracking().ToListAsync();
        return new DadosDeBackup(produtos, clientes, vendas, trocas, movimentos, categorias, pagamentos);
    }

    // Remove tudo que tem loja_id (qualquer módulo, inclusive os criados depois desta rotina) em passes sucessivos: o que ainda é
    // referenciado por outra tabela (FK sem cascade) é repetido no passe seguinte, depois que quem o referencia já foi apagado.
    // Tudo numa transação: se algo ficar preso, nada é apagado e o erro devolve a tabela e o motivo.
    public async Task<(bool Ok, string? Erro)> ExcluirLojaComTodosOsDadosAsync(Guid id)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var usuarioIds = await db.UsuariosLoja.Where(ul => ul.LojaId == id).Select(ul => ul.UsuarioId).ToListAsync();

        var tabelas = await db.Database
            .SqlQueryRaw<string>("SELECT DISTINCT table_name AS \"Value\" FROM information_schema.columns WHERE table_schema = 'public' AND column_name = 'loja_id' AND table_name <> 'lojas'")
            .ToListAsync();

        var pendentes = new List<string>(tabelas);
        var ultimosErros = new Dictionary<string, string>();

        for (var passe = 0; passe < 12 && pendentes.Count > 0; passe++)
        {
            var progresso = false;
            foreach (var tabela in pendentes.ToList())
            {
                await tx.CreateSavepointAsync("sp_del");
                try
                {
                    await db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{tabela}\" WHERE loja_id = {{0}}", id);
                    await tx.ReleaseSavepointAsync("sp_del");
                    pendentes.Remove(tabela);
                    ultimosErros.Remove(tabela);
                    progresso = true;
                }
                catch (Exception ex)
                {
                    await tx.RollbackToSavepointAsync("sp_del");
                    ultimosErros[tabela] = (ex.InnerException ?? ex).Message;
                }
            }
            if (!progresso) break;
        }

        if (pendentes.Count > 0)
        {
            await tx.RollbackAsync();
            var detalhe = string.Join(" | ", pendentes.Select(t => $"{t}: {ultimosErros.GetValueOrDefault(t)}"));
            return (false, $"Não foi possível excluir: {detalhe}");
        }

        try
        {
            // Usuários que só pertenciam a esta loja
            await tx.CreateSavepointAsync("sp_usr");
            await db.Usuarios
                .Where(u => usuarioIds.Contains(u.Id) && !db.UsuariosLoja.Any(ul => ul.UsuarioId == u.Id))
                .ExecuteDeleteAsync();
            await tx.ReleaseSavepointAsync("sp_usr");
        }
        catch
        {
            await tx.RollbackToSavepointAsync("sp_usr"); // usuário preso a outro registro: não impede a exclusão da loja
        }

        try
        {
            await db.Lojas.Where(l => l.Id == id).ExecuteDeleteAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, $"Não foi possível excluir a loja: {(ex.InnerException ?? ex).Message}");
        }

        await tx.CommitAsync();
        return (true, null);
    }

    public async Task ExcluirProdutosEVendasAsync(Guid id)
    {
        var produtos = await db.Produtos.Where(p => p.LojaId == id).Select(p => p.Id).ToListAsync();

        // Variações, movimentos de estoque e mapeamentos de NF importada dos produtos.
        // (produto_variacoes, movimentos e nf_produto_mapeamentos já são ON DELETE CASCADE no banco, mas deletamos explícito
        // aqui pra manter o mesmo padrão da exclusão da loja inteira)
        await db.ProdutoVariacoes.Where(v => produtos.Contains(v.ProdutoId)).ExecuteDeleteAsync();
        await db.Movimentos.Where(m => m.LojaId == id).ExecuteDeleteAsync();
        await db.NfProdutoMapeamentos.Where(m => produtos.Contains(m.ProdutoId)).ExecuteDeleteAsync();

        // Ajustes de caixa (reforço/sangria) e o espelho no Financeiro, se algum ajuste tiver sido feito com conta bancária
        // vinculada (AjusteContaBancariaId não é FK de verdade no banco, então precisa limpar manual)
        var ajustesContaVinculados = await db.MovimentosCaixa
            .Where(m => m.LojaId == id && m.AjusteContaBancariaId.HasValue)
            .Select(m => m.AjusteContaBancariaId!.Value)
            .ToListAsync();
        await db.AjustesContaBancaria.Where(a => ajustesContaVinculados.Contains(a.Id)).ExecuteDeleteAsync();
        await db.MovimentosCaixa.Where(m => m.LojaId == id).ExecuteDeleteAsync();

        // Itens de troca e trocas
        var trocas = await db.Trocas.Where(t => t.LojaId == id).Select(t => t.Id).ToListAsync();
        await db.ItensTroca.Where(i => trocas.Contains(i.TrocaId)).ExecuteDeleteAsync();
        await db.Trocas.Where(t => t.LojaId == id).ExecuteDeleteAsync();

        // Itens de venda e vendas
        var vendas = await db.Vendas.Where(v => v.LojaId == id).Select(v => v.Id).ToListAsync();
        await db.ItensVenda.Where(i => vendas.Contains(i.VendaId)).ExecuteDeleteAsync();
        await db.Vendas.Where(v => v.LojaId == id).ExecuteDeleteAsync();

        // Produtos. (itens_orcamento_servico.produto_id e agendamentos.venda_id são ON DELETE SET NULL no banco: ficam nulos
        // e mantêm a descrição já salva. Clientes, usuários e a loja permanecem.)
        await db.Produtos.Where(p => p.LojaId == id).ExecuteDeleteAsync();
    }
}
