using LojaApi.Domain.Plataforma;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// O assinante liga/desliga um módulo sozinho: ligar soma o valor na mensalidade, desligar tira (sem ficar negativa).
// Depois de desligar, só dá para religar passados 30 dias.
public class AlternarModuloCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<AlternarModuloCommand>
{
    public async Task<Resultado<object>> HandleAsync(AlternarModuloCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.NaoEncontrado("Loja não encontrada.");

        var loja = await repositorio.ObterLojaAsync(lojaId.Value);
        if (loja is null) return Resultado.NaoEncontrado();

        var moduloPreco = await repositorio.ObterModuloPrecoPorChaveAsync(req.Chave);
        if (moduloPreco is null || !moduloPreco.DisponivelParaAtivar)
            return Resultado.Invalido("Módulo indisponível.");

        // Bloqueia a REATIVAÇÃO logo depois de uma desativação
        if (req.Ativar && loja.ModulosAlteradoEm.TryGetValue(req.Chave, out var ultimaDesativacao))
        {
            var diasRestantes = PlataformaRegras.DiasParaReativarModulo(ultimaDesativacao, DateTime.UtcNow);
            if (diasRestantes > 0)
            {
                var mensagem = $"Este módulo foi desativado recentemente. Aguarde {diasRestantes} dia(s) para reativar.";
                return Resultado.InvalidoComCorpo(mensagem, new { erro = mensagem, cooldown = true, diasRestantes });
            }
        }

        var lista = PlataformaRegras.SepararModulos(loja.ModulosAtivos).ToList();

        if (req.Ativar)
        {
            if (!lista.Contains(req.Chave)) lista.Add(req.Chave);
            loja.MensalidadeValor += moduloPreco.Valor;
        }
        else if (lista.Contains(req.Chave))
        {
            lista.Remove(req.Chave);
            loja.MensalidadeValor = Math.Max(0, loja.MensalidadeValor - moduloPreco.Valor);
        }

        loja.ModulosAtivos = string.Join(",", lista);

        // A data só é gravada na desativação (o cooldown não pune a ativação)
        if (!req.Ativar)
        {
            var alterados = loja.ModulosAlteradoEm;
            alterados[req.Chave] = DateTime.UtcNow;
            loja.ModulosAlteradoEm = alterados;
        }

        loja.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new ModulosDaLojaDto(loja.ModulosAtivos, loja.MensalidadeValor));
    }
}
