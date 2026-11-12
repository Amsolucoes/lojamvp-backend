using LojaApi.Data;
using LojaApi.Domain.Chacara;
using LojaApi.Services;
using LojaApi.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Application.Chacara;

public record AtualizarInfoRequest(
    string Descricao, string Endereco, string Comodidades, string? ComodidadesExtras, string? MapaEmbedUrl,
    string? HoraEntrada, string? HoraSaida, string? AvisosUso,
    string? LocadorNome, string? LocadorRg, string? LocadorCpf, string? LocadorEndereco, string? LocadorTelefone, string? CidadeAssinatura
);

public record SalvarPagamentoRequest(string AccessToken, string PublicKey);

// Dados cadastrais da chácara e a conta do Mercado Pago em que ela recebe os pagamentos.
public class InfoChacaraService(AppDbContext db, MercadoPagoService mp, IConfiguration config)
{
    public async Task<Resultado<object>> ObterAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var info = await ObterOuCriarAsync(lojaId.Value);
        return Resultado.Ok(info);
    }

    public async Task<Resultado<object>> AtualizarAsync(Guid? lojaId, AtualizarInfoRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var info = await db.InfosChacara.FirstOrDefaultAsync(i => i.LojaId == lojaId);
        if (info is null)
        {
            info = new InfoChacara { LojaId = lojaId.Value };
            db.InfosChacara.Add(info);
        }

        info.AtualizarDados(
            req.Descricao, req.Endereco, req.Comodidades, req.ComodidadesExtras, req.MapaEmbedUrl,
            req.HoraEntrada, req.HoraSaida, req.AvisosUso,
            req.LocadorNome, req.LocadorRg, req.LocadorCpf, req.LocadorEndereco, req.LocadorTelefone, req.CidadeAssinatura);
        await db.SaveChangesAsync();
        return Resultado.Ok(info);
    }

    // ── Pagamento online (Mercado Pago) da própria loja ───────────

    public async Task<Resultado<object>> ObterPagamentoAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var info = await ObterOuCriarAsync(lojaId.Value);
        var token = SegredoService.Decifrar(info.MpAccessTokenCifrado, config);

        return Resultado.Ok(new
        {
            configurado = token != null,
            contaPlataforma = info.MpContaPlataforma && token == null,
            tokenFinal = token != null && token.Length > 4 ? token[^4..] : null,
            publicKey = info.MpPublicKey,
        });
    }

    public async Task<Resultado<object>> SalvarPagamentoAsync(Guid? lojaId, SalvarPagamentoRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (string.IsNullOrWhiteSpace(req.AccessToken) || string.IsNullOrWhiteSpace(req.PublicKey))
            return Resultado.Invalido("Informe o Access Token e a Public Key do Mercado Pago.");

        var token = req.AccessToken.Trim();
        var (ok, email) = await mp.ValidarToken(token);
        if (!ok)
            return Resultado.Invalido("O Mercado Pago não aceitou esse Access Token. Confira se copiou o de produção.");

        var info = await ObterOuCriarAsync(lojaId.Value);
        info.DefinirPagamentoOnline(SegredoService.Cifrar(token, config), req.PublicKey);
        await db.SaveChangesAsync();

        return Resultado.Ok(new { configurado = true, contaMercadoPago = email });
    }

    public async Task<Resultado<object>> RemoverPagamentoAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var info = await ObterOuCriarAsync(lojaId.Value);
        info.RemoverPagamentoOnline();
        await db.SaveChangesAsync();
        return Resultado.Ok(new { configurado = false });
    }

    private async Task<InfoChacara> ObterOuCriarAsync(Guid lojaId)
    {
        var info = await db.InfosChacara.FirstOrDefaultAsync(i => i.LojaId == lojaId);
        if (info is null)
        {
            info = new InfoChacara { LojaId = lojaId };
            db.InfosChacara.Add(info);
            await db.SaveChangesAsync();
        }
        return info;
    }
}
