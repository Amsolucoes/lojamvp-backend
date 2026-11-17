using LojaApi.Domain.Plataforma;
using LojaApi.Shared.Application;
using Microsoft.Extensions.Logging;

namespace LojaApi.Application.Plataforma;

// O assinante liga/desliga um módulo sozinho: ligar soma o valor na mensalidade, desligar tira (sem ficar negativa).
// Depois de desligar, só dá para religar passados 30 dias. Ligar um módulo que já está ligado não cobra de novo.
// Se a loja tem assinatura recorrente ativa, o novo valor também vai para o Mercado Pago (se falhar, o módulo muda e fica o aviso no log).
public class AlternarModuloCommandHandler(IPlataformaRepository repositorio, IGatewayDePagamentoPlataforma gateway, ILogger<AlternarModuloCommandHandler> logger) : ICommandHandler<AlternarModuloCommand>
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

        var valorAnterior = loja.MensalidadeValor;
        if (req.Ativar)
        {
            if (!lista.Contains(req.Chave))
            {
                lista.Add(req.Chave);
                loja.MensalidadeValor += moduloPreco.Valor;
            }
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
        await PlataformaApoio.SincronizarFaturaEmAbertoAsync(repositorio, loja);
        await repositorio.SalvarAsync();

        if (loja.MensalidadeValor != valorAnterior && !string.IsNullOrEmpty(loja.MpPreapprovalId) && loja.AssinaturaStatus == "authorized")
        {
            try
            {
                if (!await gateway.AtualizarValorAssinaturaAsync(loja.MpPreapprovalId, loja.MensalidadeValor))
                    logger.LogWarning("Módulo {Chave} da loja {LojaId} alterado, mas a assinatura {Assinatura} não foi atualizada para {Valor} no Mercado Pago.", req.Chave, loja.Id, loja.MpPreapprovalId, loja.MensalidadeValor);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Erro ao atualizar a assinatura {Assinatura} da loja {LojaId} no Mercado Pago após alterar o módulo {Chave}.", loja.MpPreapprovalId, loja.Id, req.Chave);
            }
        }

        return Resultado.Ok(new ModulosDaLojaDto(loja.ModulosAtivos, loja.MensalidadeValor));
    }
}
