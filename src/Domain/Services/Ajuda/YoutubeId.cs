namespace LojaApi.Domain.Ajuda;

public static class YoutubeId
{
    // Aceita o ID puro, link "watch?v=", "youtu.be/" ou "shorts/". O que não for reconhecido volta do jeito que veio.
    public static string Extrair(string entrada)
    {
        if (!entrada.Contains("youtube.com") && !entrada.Contains("youtu.be"))
            return entrada; // já é o ID puro

        try
        {
            var uri = new Uri(entrada);

            if (entrada.Contains("youtu.be"))
                return uri.AbsolutePath.Trim('/');

            if (entrada.Contains("/shorts/"))
            {
                var partes = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                return partes.Length > 0 ? partes[^1] : entrada;
            }

            var queryParams = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var par in queryParams)
            {
                var kv = par.Split('=', 2);
                if (kv.Length == 2 && kv[0] == "v")
                    return kv[1];
            }

            return entrada;
        }
        catch
        {
            return entrada;
        }
    }
}
