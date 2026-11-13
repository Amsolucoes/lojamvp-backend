using LojaApi.Domain.Corretora;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models;

namespace LojaApi.Application.Corretora;

// Corretora de seguros: seguradoras, funil de vendas, apólices (vigência) e os resumos.
public class CorretoraService(ICorretoraRepository repositorio)
{
    private const string LojaNaoEncontrada = "Loja não encontrada.";
    private const string ClienteNaoEncontrado = "Cliente não encontrado.";
    private const string SeguradoraNaoEncontrada = "Seguradora não encontrada.";
    private const string ContaNaoEncontrada = "Conta bancária não encontrada.";

    // ══════════════════ SEGURADORAS ══════════════════

    public async Task<Resultado<object>> ListarSeguradorasAsync(Guid? lojaId, bool todas)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());
        return Resultado.Ok(await repositorio.ListarSeguradorasAsync(lojaId.Value, apenasAtivas: !todas));
    }

    public async Task<Resultado<object>> CriarSeguradoraAsync(Guid? lojaId, SalvarSeguradoraRequest req)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);

        var seg = new Seguradora { LojaId = lojaId.Value, Nome = req.Nome.Trim() };
        repositorio.Adicionar(seg);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { seg.Id, seg.Nome });
    }

    public async Task<Resultado<object>> AlternarSeguradoraAsync(Guid? lojaId, Guid id)
    {
        var seg = await repositorio.ObterSeguradoraAsync(id, lojaId);
        if (seg is null) return Resultado.NaoEncontrado();

        seg.Ativa = !seg.Ativa;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { seg.Id, seg.Ativa });
    }

    public async Task<Resultado<object>> AtualizarSeguradoraAsync(Guid? lojaId, Guid id, AtualizarSeguradoraRequest req)
    {
        var seg = await repositorio.ObterSeguradoraAsync(id, lojaId);
        if (seg is null) return Resultado.NaoEncontrado();

        seg.Nome = req.Nome.Trim();
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { seg.Id, seg.Nome });
    }

    // Seguradora já usada em oportunidade/apólice é desativada em vez de excluída
    public async Task<Resultado<object>> ExcluirSeguradoraAsync(Guid? lojaId, Guid id)
    {
        var seg = await repositorio.ObterSeguradoraAsync(id, lojaId);
        if (seg is null) return Resultado.NaoEncontrado();

        if (await repositorio.SeguradoraEmUsoAsync(id))
        {
            seg.Ativa = false;
            await repositorio.SalvarAsync();
            return Resultado.Ok(new { mensagem = "Seguradora em uso — foi desativada em vez de excluída." });
        }

        repositorio.Remover(seg);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Seguradora excluída." });
    }

    // ══════════════════ FUNIL DE VENDAS ══════════════════

    public async Task<Resultado<object>> ListarOportunidadesAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());
        return Resultado.Ok(await repositorio.ListarOportunidadesAsync(lojaId.Value));
    }

    // O cliente e a seguradora da oportunidade têm que ser desta mesma loja
    private async Task<string?> ValidarVinculosDaOportunidadeAsync(Guid lojaId, SalvarOportunidadeRequest req)
    {
        if (await repositorio.ObterClienteDaLojaAsync(req.ClienteId, lojaId) is null) return ClienteNaoEncontrado;
        if (req.SeguradoraId.HasValue && !await repositorio.SeguradoraDaLojaExisteAsync(req.SeguradoraId.Value, lojaId)) return SeguradoraNaoEncontrada;
        return null;
    }

    public async Task<Resultado<object>> CriarOportunidadeAsync(Guid? lojaId, SalvarOportunidadeRequest req)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);

        var erro = await ValidarVinculosDaOportunidadeAsync(lojaId.Value, req);
        if (erro is not null) return Resultado.Invalido(erro);

        var maxOrdem = await repositorio.MaiorOrdemNaEtapaAsync(lojaId.Value, EtapaOportunidade.Lead);

        var op = new Oportunidade
        {
            LojaId = lojaId.Value,
            ClienteId = req.ClienteId,
            SeguradoraId = req.SeguradoraId,
            PlanoDesejado = req.PlanoDesejado,
            ValorEstimado = req.ValorEstimado,
            QuantidadeVidas = req.QuantidadeVidas,
            Observacao = req.Observacao,
            Ordem = maxOrdem + 1,
        };
        repositorio.Adicionar(op);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { op.Id });
    }

    public async Task<Resultado<object>> AtualizarOportunidadeAsync(Guid? lojaId, Guid id, SalvarOportunidadeRequest req)
    {
        var op = await repositorio.ObterOportunidadeAsync(id, lojaId);
        if (op is null) return Resultado.NaoEncontrado();

        var erro = await ValidarVinculosDaOportunidadeAsync(lojaId!.Value, req);
        if (erro is not null) return Resultado.Invalido(erro);

        op.ClienteId = req.ClienteId;
        op.SeguradoraId = req.SeguradoraId;
        op.PlanoDesejado = req.PlanoDesejado;
        op.ValorEstimado = req.ValorEstimado;
        op.QuantidadeVidas = req.QuantidadeVidas;
        op.Observacao = req.Observacao;
        op.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { op.Id });
    }

    // Mover o card entre etapas (arrastar e soltar). Ao chegar em "ganho", lança a comissão no Financeiro.
    public async Task<Resultado<object>> MoverEtapaAsync(Guid? lojaId, Guid id, MoverEtapaRequest req)
    {
        var op = await repositorio.ObterOportunidadeAsync(id, lojaId);
        if (op is null) return Resultado.NaoEncontrado();

        if (!CorretoraRegras.EtapaValida(req.Etapa)) return Resultado.Invalido("Etapa inválida.");

        var lancarComissao = req.Etapa == EtapaOportunidade.Ganho && op.LancamentoFinanceiroId is null && req.ContaBancariaId.HasValue;
        if (lancarComissao && !await repositorio.ContaBancariaDaLojaExisteAsync(req.ContaBancariaId!.Value, lojaId!.Value))
            return Resultado.Invalido(ContaNaoEncontrada);

        op.Etapa = req.Etapa;
        op.Ordem = req.Ordem;
        op.AtualizadoEm = DateTime.UtcNow;
        if (req.Etapa == EtapaOportunidade.Perdido) op.MotivoPerda = req.MotivoPerda;

        var comissaoLancada = false;
        if (lancarComissao)
        {
            var cliente = await repositorio.ObterClienteDaLojaAsync(op.ClienteId, lojaId!.Value);
            var lancamento = new LancamentoFinanceiro
            {
                LojaId = lojaId.Value,
                ContaBancariaId = req.ContaBancariaId!.Value,
                Tipo = "receber",
                Modo = "avulsa",
                Descricao = CorretoraRegras.DescricaoDaComissao(op.PlanoDesejado ?? "Plano", cliente?.Nome),
                Valor = CorretoraRegras.ValorDaComissao(req.Valor, op.ValorEstimado),
                Vencimento = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc).AddHours(12),
                Avisar = true,
            };
            repositorio.Adicionar(lancamento);
            await repositorio.SalvarAsync();
            op.LancamentoFinanceiroId = lancamento.Id;
            comissaoLancada = true;
        }
        else if (op.LancamentoFinanceiroId.HasValue)
        {
            comissaoLancada = true; // já tinha sido lançada antes
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { op.Id, op.Etapa, comissaoLancada });
    }

    public async Task<Resultado<object>> ExcluirOportunidadeAsync(Guid? lojaId, Guid id)
    {
        var op = await repositorio.ObterOportunidadeAsync(id, lojaId);
        if (op is null) return Resultado.NaoEncontrado();

        repositorio.Remover(op);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Removido." });
    }

    // O cliente já tem oportunidade em aberto? (para avisar antes de criar outra)
    public async Task<Resultado<object>> VerificarClienteJaTemOportunidadeAsync(Guid? lojaId, Guid clienteId)
    {
        if (lojaId is null) return Resultado.Ok(new { existe = false });

        var existentes = await repositorio.ListarOportunidadesEmAbertoDoClienteAsync(lojaId.Value, clienteId);
        return Resultado.Ok(new { existe = existentes.Count > 0, quantidade = existentes.Count, oportunidades = existentes });
    }

    // ══════════════════ APÓLICES (VIGÊNCIA) ══════════════════

    public async Task<Resultado<object>> ListarApolicesAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());
        return Resultado.Ok(await repositorio.ListarApolicesAsync(lojaId.Value, DateTime.UtcNow.Date));
    }

    public async Task<Resultado<object>> CriarApoliceAsync(Guid? lojaId, SalvarApoliceRequest req)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);

        // Tudo o que a apólice referencia tem que ser desta mesma loja
        var cliente = await repositorio.ObterClienteDaLojaAsync(req.ClienteId, lojaId.Value);
        if (cliente is null) return Resultado.Invalido(ClienteNaoEncontrado);
        if (!await repositorio.SeguradoraDaLojaExisteAsync(req.SeguradoraId, lojaId.Value)) return Resultado.Invalido(SeguradoraNaoEncontrada);

        Oportunidade? oportunidade = null;
        if (req.OportunidadeId.HasValue)
        {
            oportunidade = await repositorio.ObterOportunidadeAsync(req.OportunidadeId.Value, lojaId);
            if (oportunidade is null) return Resultado.Invalido("Oportunidade não encontrada.");
        }

        var gerarFinanceiro = req.GerarNoFinanceiro && req.ContaBancariaId.HasValue;
        if (gerarFinanceiro && !await repositorio.ContaBancariaDaLojaExisteAsync(req.ContaBancariaId!.Value, lojaId.Value))
            return Resultado.Invalido(ContaNaoEncontrada);

        var apolice = new Apolice
        {
            LojaId = lojaId.Value,
            ClienteId = req.ClienteId,
            OportunidadeId = req.OportunidadeId,
            SeguradoraId = req.SeguradoraId,
            NomePlano = req.NomePlano.Trim(),
            NumeroApolice = req.NumeroApolice,
            ValorPremio = req.ValorPremio,
            ValorComissao = req.ValorComissao,
            PercentualComissao = req.PercentualComissao,
            VigenciaInicio = CorretoraRegras.NormalizarVigencia(req.VigenciaInicio),
            VigenciaFim = CorretoraRegras.NormalizarVigencia(req.VigenciaFim),
        };
        repositorio.Adicionar(apolice);

        // Se veio de uma oportunidade, ela vira "ganho"
        if (oportunidade is not null)
        {
            oportunidade.Etapa = EtapaOportunidade.Ganho;
            oportunidade.AtualizadoEm = DateTime.UtcNow;
        }

        // Gera automaticamente um "a receber" no Financeiro com o valor da comissão
        if (gerarFinanceiro)
        {
            var lancamento = new LancamentoFinanceiro
            {
                LojaId = lojaId.Value,
                ContaBancariaId = req.ContaBancariaId!.Value,
                Tipo = "receber",
                Modo = "avulsa",
                Descricao = CorretoraRegras.DescricaoDaComissao(req.NomePlano, cliente.Nome),
                Valor = req.ValorComissao,
                Vencimento = apolice.VigenciaInicio,
                Avisar = true,
            };
            repositorio.Adicionar(lancamento);
            await repositorio.SalvarAsync();
            apolice.LancamentoFinanceiroId = lancamento.Id;
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { apolice.Id });
    }

    public async Task<Resultado<object>> AtualizarApoliceAsync(Guid? lojaId, Guid id, SalvarApoliceRequest req)
    {
        var apolice = await repositorio.ObterApoliceAsync(id, lojaId);
        if (apolice is null) return Resultado.NaoEncontrado();

        if (!await repositorio.SeguradoraDaLojaExisteAsync(req.SeguradoraId, lojaId!.Value))
            return Resultado.Invalido(SeguradoraNaoEncontrada);

        apolice.SeguradoraId = req.SeguradoraId;
        apolice.NomePlano = req.NomePlano.Trim();
        apolice.NumeroApolice = req.NumeroApolice;
        apolice.ValorPremio = req.ValorPremio;
        apolice.ValorComissao = req.ValorComissao;
        apolice.PercentualComissao = req.PercentualComissao;
        apolice.VigenciaInicio = CorretoraRegras.NormalizarVigencia(req.VigenciaInicio);
        apolice.VigenciaFim = CorretoraRegras.NormalizarVigencia(req.VigenciaFim);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { apolice.Id });
    }

    public async Task<Resultado<object>> AlterarStatusApoliceAsync(Guid? lojaId, Guid id, AlterarStatusApoliceRequest req)
    {
        var apolice = await repositorio.ObterApoliceAsync(id, lojaId);
        if (apolice is null) return Resultado.NaoEncontrado();

        if (!CorretoraRegras.StatusApoliceValido(req.Nome)) return Resultado.Invalido("Status inválido.");

        apolice.Status = req.Nome;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { apolice.Id, apolice.Status });
    }

    // ══════════════════ RESUMOS ══════════════════

    public async Task<Resultado<object>> ResumoAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(new { });

        var apolices = await repositorio.ListarApolicesNaoCanceladasAsync(lojaId.Value);
        var funil = await repositorio.ContarOportunidadesPorEtapaAsync(lojaId.Value);
        var hoje = DateTime.UtcNow.Date;

        return Resultado.Ok(new
        {
            totalApolices = apolices.Count,
            totalPremios = apolices.Sum(a => a.ValorPremio),
            totalComissoes = apolices.Sum(a => a.ValorComissao),
            vencendoEm30Dias = apolices.Count(a => CorretoraRegras.VenceEmAte30Dias(a.Status, a.VigenciaFim, hoje)),
            funil,
        });
    }

    // Oportunidades criadas nos últimos N dias: em andamento x implantadas
    public async Task<Resultado<object>> DashboardAsync(Guid? lojaId, int dias)
    {
        if (lojaId is null) return Resultado.Ok(new { });

        var desde = DateTime.UtcNow.AddDays(-dias);
        var oportunidades = await repositorio.ListarOportunidadesCriadasDesdeAsync(lojaId.Value, desde);

        var resumo = CorretoraRegras.Resumir(oportunidades.Select(o => new LinhaDoFunil(o.Etapa, o.QuantidadeVidas, o.ValorEstimado)));

        return Resultado.Ok(new
        {
            emAndamento = new { propostas = resumo.EmAndamento.Propostas, vidas = resumo.EmAndamento.Vidas, valorTotal = resumo.EmAndamento.ValorTotal },
            implantadas = new { propostas = resumo.Implantadas.Propostas, vidas = resumo.Implantadas.Vidas, valorTotal = resumo.Implantadas.ValorTotal },
        });
    }
}
