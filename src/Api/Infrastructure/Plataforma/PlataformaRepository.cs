using LojaApi.Application.Plataforma;
using LojaApi.Data;
using LojaApi.Models;
using LojaApi.src.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Plataforma;

// Acesso aos dados da Plataforma (EF Core). Só lê e grava; as regras ficam nos handlers.
public class PlataformaRepository(AppDbContext db) : IPlataformaRepository
{
    // ── Usuários ──

    public Task<Usuario?> ObterUsuarioAtivoPorEmailAsync(string email) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.Ativo);

    public async Task<Usuario?> ObterUsuarioAsync(Guid id) => await db.Usuarios.FindAsync(id);

    public Task<bool> EmailDeUsuarioExisteAsync(string email, Guid? ignorarUsuarioId) =>
        db.Usuarios.AnyAsync(u => u.Email.ToLower() == email.ToLower() && (ignorarUsuarioId == null || u.Id != ignorarUsuarioId));

    public Task<UsuarioLoja?> ObterVinculoAtivoAsync(Guid usuarioId) =>
        db.UsuariosLoja.Include(ul => ul.Loja).FirstOrDefaultAsync(ul => ul.UsuarioId == usuarioId && ul.Ativo);

    public void Adicionar(Usuario usuario) => db.Usuarios.Add(usuario);
    public void Adicionar(UsuarioLoja vinculo) => db.UsuariosLoja.Add(vinculo);

    // ── Lojas ──

    public async Task<Loja?> ObterLojaAsync(Guid id) => await db.Lojas.FindAsync(id);

    public async Task<Loja?> ObterMinhaLojaComPagamentosAsync(Guid usuarioId)
    {
        var vinculo = await db.UsuariosLoja
            .Include(ul => ul.Loja).ThenInclude(l => l.Pagamentos)
            .FirstOrDefaultAsync(ul => ul.UsuarioId == usuarioId && ul.Ativo);
        return vinculo?.Loja;
    }

    public Task<bool> EmailDeLojaExisteAsync(string email, Guid? ignorarLojaId = null) =>
        db.Lojas.AnyAsync(l => l.Email.ToLower() == email.ToLower() && (ignorarLojaId == null || l.Id != ignorarLojaId));

    public Task<bool> SlugEmUsoAsync(string slug, Guid ignorarLojaId) => db.Lojas.AnyAsync(l => l.Slug == slug && l.Id != ignorarLojaId);

    public Task<int> ContarLojasDaPromocaoAsync() => db.Lojas.CountAsync(l => !l.EhTeste && l.TipoPlano != "financeiro");

    public void Adicionar(Loja loja) => db.Lojas.Add(loja);
    public void Adicionar(Pagamento pagamento) => db.Pagamentos.Add(pagamento);

    // ── Faturas (mensalidade) ──

    public async Task<Pagamento?> ObterPagamentoAsync(Guid id) => await db.Pagamentos.FindAsync(id);

    public Task<Pagamento?> ObterPagamentoComLojaAsync(Guid id) =>
        db.Pagamentos.Include(p => p.Loja).FirstOrDefaultAsync(p => p.Id == id);

    public Task<Pagamento?> ObterPagamentoDaCobrancaAsync(string mpPaymentId) =>
        db.Pagamentos.Include(p => p.Loja).FirstOrDefaultAsync(p => p.MpPaymentId == mpPaymentId || p.CobrancaMpPaymentId == mpPaymentId);

    public Task<bool> PagamentoDoMpJaPagoAsync(string mpPaymentId) =>
        db.Pagamentos.AnyAsync(p => p.MpPaymentId == mpPaymentId && p.Status == "pago");

    public Task<Pagamento?> ObterFaturaEmAbertoAsync(Guid lojaId) =>
        db.Pagamentos.FirstOrDefaultAsync(p => p.LojaId == lojaId && (p.Status == "pendente" || p.Status == "atrasado"));

    public Task<int> ContarFaturasPagasAsync(Guid lojaId) => db.Pagamentos.CountAsync(p => p.LojaId == lojaId && p.Status == "pago");

    public Task<Loja?> ObterLojaPorAssinaturaAsync(string preapprovalId) => db.Lojas.FirstOrDefaultAsync(l => l.MpPreapprovalId == preapprovalId);

    // ── Perfis ──

    public Task<List<PerfilLoja>> ListarPerfisAtivosAsync() =>
        db.PerfisLoja
            .Include(p => p.Categorias.OrderBy(c => c.Ordem))
            .Include(p => p.CamposExtras.OrderBy(c => c.Ordem))
            .Where(p => p.Ativo)
            .ToListAsync();

    public Task<string?> ObterTipoPlanoDoPerfilAsync(Guid perfilId) =>
        db.PerfisLoja.Where(p => p.Id == perfilId).Select(p => p.TipoPlanoAplica).FirstOrDefaultAsync();

    public Task<PerfilLoja?> ObterPerfilComCategoriasECamposAsync(Guid id) =>
        db.PerfisLoja.Include(p => p.Categorias).Include(p => p.CamposExtras).FirstOrDefaultAsync(p => p.Id == id);

    public Task<PerfilLoja?> ObterPerfilComCategoriasEServicosAsync(Guid id) =>
        db.PerfisLoja.Include(p => p.Categorias).Include(p => p.Servicos).FirstOrDefaultAsync(p => p.Id == id);

    public Task<PerfilLoja?> ObterPerfilParaEmailAsync(Guid id) =>
        db.PerfisLoja.AsNoTracking().Include(p => p.Categorias).Include(p => p.Servicos).FirstOrDefaultAsync(p => p.Id == id);

    // ── Categorias e campos da loja ──

    public Task<bool> LojaTemCategoriasAsync(Guid lojaId) => db.CategoriasLoja.AnyAsync(c => c.LojaId == lojaId);

    public async Task RemoverCategoriasECamposDaLojaAsync(Guid lojaId)
    {
        db.CategoriasLoja.RemoveRange(await db.CategoriasLoja.Where(c => c.LojaId == lojaId).ToListAsync());
        db.CamposExtrasLoja.RemoveRange(await db.CamposExtrasLoja.Where(c => c.LojaId == lojaId).ToListAsync());
    }

    public Task<List<CategoriaLojaDto>> ListarCategoriasAtivasAsync(Guid lojaId) =>
        db.CategoriasLoja
            .Where(c => c.LojaId == lojaId && c.Ativo)
            .OrderBy(c => c.Ordem)
            .Select(c => new CategoriaLojaDto(c.Id, c.Nome, c.Ativo, c.Ordem, c.TipoTamanho))
            .ToListAsync();

    public Task<List<CampoExtraLojaDto>> ListarCamposExtrasAtivosAsync(Guid lojaId) =>
        db.CamposExtrasLoja
            .Where(c => c.LojaId == lojaId && c.Ativo)
            .OrderBy(c => c.Ordem)
            .Select(c => new CampoExtraLojaDto(c.Id, c.Chave, c.Label, c.Tipo, c.Opcoes, c.Obrigatorio, c.Ativo, c.Ordem))
            .ToListAsync();

    public void Adicionar(CategoriaLoja categoria) => db.CategoriasLoja.Add(categoria);
    public void Adicionar(CampoExtraLoja campo) => db.CamposExtrasLoja.Add(campo);
    public void Adicionar(Servico servico) => db.Servicos.Add(servico);

    // ── Preços dos módulos ──

    public Task<List<ModuloPreco>> ListarModulosPrecoAsync() => db.ModulosPreco.OrderBy(m => m.Nome).ToListAsync();

    public async Task<ModuloPreco?> ObterModuloPrecoAsync(Guid id) => await db.ModulosPreco.FindAsync(id);

    public Task<ModuloPreco?> ObterModuloPrecoPorChaveAsync(string chave) => db.ModulosPreco.FirstOrDefaultAsync(m => m.Chave == chave);

    public Task<List<ModuloPreco>> ListarModulosDisponiveisAsync(IEnumerable<string> chaves)
    {
        var lista = chaves.ToList();
        return db.ModulosPreco.Where(m => lista.Contains(m.Chave) && m.DisponivelParaAtivar).ToListAsync();
    }

    // ── Push ──

    public Task<bool> AssinaturaPushExisteAsync(Guid usuarioId, string endpoint) =>
        db.AssinaturasPush.AnyAsync(a => a.UsuarioId == usuarioId && a.Endpoint == endpoint);

    public Task<AssinaturaPush?> ObterAssinaturaPushPorEndpointAsync(string endpoint) =>
        db.AssinaturasPush.FirstOrDefaultAsync(a => a.Endpoint == endpoint);

    public Task<AssinaturaPush?> ObterAssinaturaPushDoUsuarioAsync(Guid usuarioId, string endpoint) =>
        db.AssinaturasPush.FirstOrDefaultAsync(a => a.Endpoint == endpoint && a.UsuarioId == usuarioId);

    public Task<List<AssinaturaPush>> ListarAssinaturasPushDoUsuarioAsync(Guid usuarioId) =>
        db.AssinaturasPush.Where(a => a.UsuarioId == usuarioId).ToListAsync();

    public void Adicionar(AssinaturaPush assinatura) => db.AssinaturasPush.Add(assinatura);
    public void Remover(AssinaturaPush assinatura) => db.AssinaturasPush.Remove(assinatura);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
