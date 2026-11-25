using System.Security.Cryptography;
using System.Text;

namespace LojaApi.Services;

// Cifra segredos de clientes (ex: Access Token do Mercado Pago) antes de gravar no banco.
// AES-GCM; a chave deriva de Jwt:Secret (mesma variável de ambiente já configurada no servidor).
public static class SegredoService
{
    private static byte[] Chave(IConfiguration config)
        => SHA256.HashData(Encoding.UTF8.GetBytes("segredo-loja:" + config["Jwt:Secret"]));

    public static string Cifrar(string texto, IConfiguration config)
    {
        var chave = Chave(config);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var dados = Encoding.UTF8.GetBytes(texto);
        var cifrado = new byte[dados.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(chave, 16);
        aes.Encrypt(nonce, dados, cifrado, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cifrado).ToArray());
    }

    public static string? Decifrar(string? valor, IConfiguration config)
    {
        if (string.IsNullOrEmpty(valor)) return null;
        try
        {
            var bruto = Convert.FromBase64String(valor);
            var nonce = bruto[..12];
            var tag = bruto[12..28];
            var cifrado = bruto[28..];
            var dados = new byte[cifrado.Length];
            using var aes = new AesGcm(Chave(config), 16);
            aes.Decrypt(nonce, cifrado, tag, dados);
            return Encoding.UTF8.GetString(dados);
        }
        catch
        {
            return null;
        }
    }
}
