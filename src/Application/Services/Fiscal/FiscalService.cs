using LojaApi.Domain.Fiscal;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Fiscal;

namespace LojaApi.Application.Fiscal;

// Emissão fiscal (NFC-e) das vendas: configuração da loja, emitir, listar e cancelar.
public class FiscalService(IFiscalRepository repositorio, IProvedorFiscal provedorFiscal)
{
    private const string LojaNaoEncontrada = "Loja não encontrada.";

    private static ConfiguracaoFiscalDto Dto(ConfiguracaoFiscal c) => new(
        c.Ativo, c.Provedor, c.Ambiente, c.InscricaoEstadual, c.RegimeTributario,
        c.SerieNfce, c.ProximoNumeroNfce, !string.IsNullOrEmpty(c.CredencialToken));

    private static EmissaoFiscalDto Dto(EmissaoFiscal e) => new(
        e.Id, e.VendaId, e.Tipo, e.Status, e.Provedor, e.ChaveAcesso, e.NumeroNota,
        e.SerieNota, e.Protocolo, e.UrlDanfe, e.UrlXml, e.MotivoErro, e.SolicitadaEm, e.ProcessadaEm);

    // ── Configuração fiscal da loja ──────────────────────────────────────

    public async Task<Resultado<object>> BuscarConfiguracaoAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);

        var config = await repositorio.ObterConfiguracaoAsync(lojaId.Value)
                     ?? new ConfiguracaoFiscal { LojaId = lojaId.Value };   // padrão, sem gravar

        return Resultado.Ok(Dto(config));
    }

    public async Task<Resultado<object>> SalvarConfiguracaoAsync(Guid? lojaId, SalvarConfiguracaoFiscalRequest req)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);
        if (!FiscalRegras.AmbienteValido(req.Ambiente)) return Resultado.Invalido("Ambiente inválido.");

        var config = await repositorio.ObterConfiguracaoAsync(lojaId.Value);
        if (config is null)
        {
            config = new ConfiguracaoFiscal { LojaId = lojaId.Value };
            repositorio.Adicionar(config);
        }

        config.Provedor = req.Provedor;
        config.Ambiente = req.Ambiente;
        if (!string.IsNullOrWhiteSpace(req.CredencialToken)) config.CredencialToken = req.CredencialToken;   // vazio mantém a antiga
        config.InscricaoEstadual = req.InscricaoEstadual;
        config.RegimeTributario = req.RegimeTributario;
        config.SerieNfce = FiscalRegras.SerieOuPadrao(req.SerieNfce);
        config.Ativo = FiscalRegras.ConfiguracaoAtiva(config.Provedor, config.CredencialToken);
        config.AtualizadoEm = DateTime.UtcNow;

        await repositorio.SalvarAsync();
        return Resultado.Ok(Dto(config));
    }

    // ── Emissão ──────────────────────────────────────────────────────────

    public async Task<Resultado<object>> EmitirAsync(Guid? lojaId, Guid usuarioId, Guid vendaId)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);

        var loja = await repositorio.ObterLojaAsync(lojaId.Value);
        if (loja is null) return Resultado.NaoEncontrado();

        if (!FiscalRegras.ModuloAtivo(loja.ModulosAtivos, FiscalRegras.ModuloNfce))
            return Resultado.Invalido("O módulo de emissão fiscal (NFC-e) não está ativo para esta loja.");

        var venda = await repositorio.ObterVendaAsync(vendaId, lojaId.Value);
        if (venda is null) return Resultado.NaoEncontrado("Venda não encontrada.");

        var config = await repositorio.ObterConfiguracaoAsync(lojaId.Value);
        if (config is null || !config.Ativo)
            return Resultado.Invalido("Emissão fiscal ainda não está configurada. Contrate e configure um provedor antes de emitir.");

        var emissao = new EmissaoFiscal
        {
            LojaId = lojaId.Value,
            VendaId = venda.Id,
            Tipo = "nfce",
            Status = StatusEmissaoFiscal.Processando,
            Provedor = config.Provedor,
            SolicitadaPorUsuarioId = usuarioId,
        };
        repositorio.Adicionar(emissao);

        var resultado = await provedorFiscal.EmitirNfceAsync(venda, config);

        emissao.Status = resultado.Status;
        emissao.ChaveAcesso = resultado.ChaveAcesso;
        emissao.NumeroNota = resultado.NumeroNota;
        emissao.SerieNota = resultado.SerieNota;
        emissao.Protocolo = resultado.Protocolo;
        emissao.UrlDanfe = resultado.UrlDanfe;
        emissao.UrlXml = resultado.UrlXml;
        emissao.MotivoErro = resultado.MotivoErro;
        emissao.ProcessadaEm = DateTime.UtcNow;

        await repositorio.SalvarAsync();
        return Resultado.Ok(Dto(emissao));
    }

    public async Task<Resultado<object>> ListarEmissoesAsync(Guid? lojaId, Guid vendaId)
    {
        var lista = await repositorio.ListarEmissoesDaVendaAsync(vendaId, lojaId);
        return Resultado.Ok(lista.Select(Dto).ToList());
    }

    public async Task<Resultado<object>> CancelarAsync(Guid? lojaId, Guid emissaoId, CancelarEmissaoRequest req)
    {
        var emissao = await repositorio.ObterEmissaoAsync(emissaoId, lojaId);
        if (emissao is null) return Resultado.NaoEncontrado();

        if (!FiscalRegras.PodeCancelar(emissao.Status))
            return Resultado.Invalido("Só é possível cancelar uma nota autorizada.");

        if (!FiscalRegras.JustificativaValida(req.Justificativa))
            return Resultado.Invalido($"Justificativa deve ter pelo menos {FiscalRegras.MinimoCaracteresJustificativa} caracteres.");

        var config = await repositorio.ObterConfiguracaoAsync(emissao.LojaId);
        if (config is null) return Resultado.Invalido("Configuração fiscal não encontrada.");

        var resultado = await provedorFiscal.CancelarNfceAsync(emissao, req.Justificativa.Trim(), config);
        if (!resultado.Sucesso)
            return Resultado.Invalido(resultado.MotivoErro ?? "Não foi possível cancelar a nota.");

        emissao.Status = StatusEmissaoFiscal.Cancelada;
        emissao.ProcessadaEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(Dto(emissao));
    }
}
