using LojaApi.Data;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Application.Chacara;

public record CredencialMp(bool Ok, string? Token, string? PublicKey);

// Credencial do Mercado Pago da loja da chácara: a própria (cifrada no banco) ou, só para a loja da plataforma,
// a do servidor. Uma loja sem nenhuma das duas NÃO cai na conta da plataforma: o pagamento online fica indisponível.
public class CredencialMercadoPagoChacaraService(AppDbContext db, IProtetorDeSegredos segredos)
{
    public async Task<CredencialMp> ObterAsync(Guid lojaId)
    {
        var info = await db.InfosChacara.AsNoTracking().FirstOrDefaultAsync(i => i.LojaId == lojaId);
        var token = segredos.Decifrar(info?.MpAccessTokenCifrado);
        if (token != null) return new CredencialMp(true, token, info!.MpPublicKey);
        if (info?.MpContaPlataforma == true) return new CredencialMp(true, null, null);
        return new CredencialMp(false, null, null);
    }
}
