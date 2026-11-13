using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Cadastro de funcionários/profissionais: remuneração (comissão, salário fixo ou diária), endereço e comissão por serviço.
// O salário fixo vira um lançamento fixo no Financeiro (pela porta ILancamentoFixoFinanceiro).
public class FuncionariosService(IEquipeRepository repositorio, ILancamentoFixoFinanceiro financeiro)
{
    private const string LojaNaoEncontrada = "Loja não encontrada.";
    private const string ContaNaoEncontrada = "Conta bancária não encontrada.";

    private static ProfissionalDto Dto(Profissional p) => new(
        p.Id, p.Nome, p.Ativo, p.ComissaoPadraoPercentual, p.DiaPagamentoPadrao, p.TipoRemuneracao, p.SalarioFixo, p.ValorDiaria,
        p.Telefone, p.Cep, p.Endereco, p.Numero, p.Bairro, p.Cidade, p.Uf, p.ComissaoBaseCalculo, p.ComissaoVendasAtiva, p.ComissaoVendasPercentual);

    private static void AplicarEndereco(Profissional p, SalvarFuncionarioRequest req)
    {
        p.Cep = EquipeRegras.Limpar(req.Cep, 9);
        p.Endereco = EquipeRegras.Limpar(req.Endereco, 200);
        p.Numero = EquipeRegras.Limpar(req.Numero, 20);
        p.Bairro = EquipeRegras.Limpar(req.Bairro, 100);
        p.Cidade = EquipeRegras.Limpar(req.Cidade, 100);
        p.Uf = EquipeRegras.Uf(req.Uf);
    }

    // Categoria do Financeiro onde caem os salários (criada na primeira vez)
    private async Task<Guid> ObterOuCriarCategoriaDeSalarioAsync(Guid lojaId)
    {
        var categoria = await repositorio.ObterCategoriaFinanceiraAsync(lojaId, EquipeRegras.CategoriaSalarios);
        if (categoria != null) return categoria.Id;

        categoria = new CategoriaFinanceira { LojaId = lojaId, Nome = EquipeRegras.CategoriaSalarios, Tipo = "pagar", Icone = EquipeRegras.IconeSalarios };
        repositorio.Adicionar(categoria);
        await repositorio.SalvarAsync();
        return categoria.Id;
    }

    private static LancamentoFixo NovoSalarioFixo(Guid lojaId, Profissional profissional, SalvarFuncionarioRequest req, Guid categoriaId) => new()
    {
        LojaId = lojaId,
        ContaBancariaId = req.ContaBancariaId!.Value,
        Tipo = "pagar",
        Descricao = $"Salário — {profissional.Nome}",
        CategoriaId = categoriaId,
        Valor = req.SalarioFixo!.Value,
        DiaVencimento = EquipeRegras.DiaDoLancamentoFixo(req.DiaPagamentoPadrao),
    };

    private static DateTime MesAtualUtc() { var agora = DateTime.UtcNow; return new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc); }

    // O salário fixo precisa cair numa conta bancária desta mesma loja
    private async Task<string?> ValidarContaDoSalarioAsync(Guid lojaId, SalvarFuncionarioRequest req) =>
        req.TipoRemuneracao == TipoRemuneracao.SalarioFixo && req.ContaBancariaId.HasValue &&
        !await repositorio.ContaBancariaDaLojaExisteAsync(req.ContaBancariaId.Value, lojaId)
            ? ContaNaoEncontrada : null;

    // ── Consultas ────────────────────────────────────────────────────────────

    public async Task<Resultado<object>> ListarAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());
        return Resultado.Ok(await repositorio.ListarProfissionaisAsync(lojaId.Value));
    }

    // Lista simplificada (id + nome), usada em seletores de outras telas
    public async Task<Resultado<object>> ListarAtivosAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());
        return Resultado.Ok(await repositorio.ListarProfissionaisAtivosAsync(lojaId.Value));
    }

    // ── Criar / editar ───────────────────────────────────────────────────────

    public async Task<Resultado<object>> CriarAsync(Guid? lojaId, SalvarFuncionarioRequest req)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);
        if (string.IsNullOrWhiteSpace(req.Nome)) return Resultado.Invalido("Nome é obrigatório.");

        var erro = EquipeRegras.ValidarRemuneracao(req.TipoRemuneracao, req.SalarioFixo, req.ValorDiaria, req.ComissaoVendasAtiva, req.ComissaoVendasPercentual)
                   ?? await ValidarContaDoSalarioAsync(lojaId.Value, req);
        if (erro is not null) return Resultado.Invalido(erro);

        var profissional = new Profissional
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            // Não zera em salario_fixo/diaria: o funcionário pode ter salário fixo/diária E ainda ganhar comissão
            // sobre Ordem de Serviço/Agendamento.
            ComissaoPadraoPercentual = req.ComissaoPadraoPercentual,
            DiaPagamentoPadrao = EquipeRegras.DiaValido(req.DiaPagamentoPadrao),
            Ativo = req.Ativo,
            TipoRemuneracao = req.TipoRemuneracao,
            SalarioFixo = EquipeRegras.SalarioAplicavel(req.TipoRemuneracao, req.SalarioFixo),
            ValorDiaria = EquipeRegras.DiariaAplicavel(req.TipoRemuneracao, req.ValorDiaria),
            Telefone = req.Telefone,
            ComissaoBaseCalculo = EquipeRegras.BaseDeCalculo(req.ComissaoBaseCalculo),
            ComissaoVendasAtiva = req.ComissaoVendasAtiva,
            ComissaoVendasPercentual = EquipeRegras.PercentualVendasAplicavel(req.ComissaoVendasAtiva, req.ComissaoVendasPercentual),
        };
        AplicarEndereco(profissional, req);
        repositorio.Adicionar(profissional);
        await repositorio.SalvarAsync();

        if (req.TipoRemuneracao == TipoRemuneracao.SalarioFixo && req.ContaBancariaId.HasValue)
        {
            var categoriaId = await ObterOuCriarCategoriaDeSalarioAsync(lojaId.Value);
            var fixo = NovoSalarioFixo(lojaId.Value, profissional, req, categoriaId);
            repositorio.Adicionar(fixo);
            await repositorio.SalvarAsync();

            await financeiro.GerarLoteAsync(fixo, MesAtualUtc());
            await repositorio.SalvarAsync();

            profissional.LancamentoFixoId = fixo.Id;
            await repositorio.SalvarAsync();
        }

        return Resultado.Ok(Dto(profissional));
    }

    public async Task<Resultado<object>> AtualizarAsync(Guid? lojaId, Guid id, SalvarFuncionarioRequest req)
    {
        var profissional = await repositorio.ObterProfissionalAsync(id, lojaId);
        if (profissional is null) return Resultado.NaoEncontrado();

        var erro = EquipeRegras.ValidarRemuneracao(req.TipoRemuneracao, req.SalarioFixo, req.ValorDiaria, req.ComissaoVendasAtiva, req.ComissaoVendasPercentual)
                   ?? await ValidarContaDoSalarioAsync(lojaId!.Value, req);
        if (erro is not null) return Resultado.Invalido(erro);

        var tipoAnterior = profissional.TipoRemuneracao;

        profissional.Nome = req.Nome.Trim();
        profissional.ComissaoPadraoPercentual = req.ComissaoPadraoPercentual;
        profissional.DiaPagamentoPadrao = EquipeRegras.DiaValido(req.DiaPagamentoPadrao);
        profissional.Ativo = req.Ativo;
        profissional.TipoRemuneracao = req.TipoRemuneracao;
        profissional.SalarioFixo = EquipeRegras.SalarioAplicavel(req.TipoRemuneracao, req.SalarioFixo);
        profissional.ValorDiaria = EquipeRegras.DiariaAplicavel(req.TipoRemuneracao, req.ValorDiaria);
        profissional.Telefone = req.Telefone;
        AplicarEndereco(profissional, req);
        profissional.ComissaoBaseCalculo = EquipeRegras.BaseDeCalculo(req.ComissaoBaseCalculo);
        profissional.ComissaoVendasAtiva = req.ComissaoVendasAtiva;
        profissional.ComissaoVendasPercentual = EquipeRegras.PercentualVendasAplicavel(req.ComissaoVendasAtiva, req.ComissaoVendasPercentual);

        // Saiu do salário fixo — desativa o lançamento fixo antigo, se existir
        if (tipoAnterior == TipoRemuneracao.SalarioFixo && req.TipoRemuneracao != TipoRemuneracao.SalarioFixo && profissional.LancamentoFixoId.HasValue)
        {
            var fixoAntigo = await repositorio.ObterLancamentoFixoAsync(profissional.LancamentoFixoId.Value);
            if (fixoAntigo != null) fixoAntigo.Ativa = false;
            profissional.LancamentoFixoId = null;
        }
        // Continua ou entrou em salário fixo — cria ou atualiza o lançamento fixo
        else if (req.TipoRemuneracao == TipoRemuneracao.SalarioFixo && req.ContaBancariaId.HasValue)
        {
            if (profissional.LancamentoFixoId.HasValue)
            {
                var fixo = await repositorio.ObterLancamentoFixoAsync(profissional.LancamentoFixoId.Value);
                if (fixo != null)
                {
                    fixo.ContaBancariaId = req.ContaBancariaId.Value;
                    fixo.Valor = req.SalarioFixo!.Value;
                    fixo.DiaVencimento = EquipeRegras.DiaValido(req.DiaPagamentoPadrao) ?? fixo.DiaVencimento;
                    fixo.Ativa = true;

                    await financeiro.LimparFuturosAsync(fixo.Id);
                    await financeiro.GerarLoteAsync(fixo, MesAtualUtc());
                }
            }
            else
            {
                var categoriaId = await ObterOuCriarCategoriaDeSalarioAsync(lojaId!.Value);
                var fixo = NovoSalarioFixo(lojaId.Value, profissional, req, categoriaId);
                repositorio.Adicionar(fixo);
                await repositorio.SalvarAsync();

                await financeiro.GerarLoteAsync(fixo, MesAtualUtc());

                profissional.LancamentoFixoId = fixo.Id;
            }
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(Dto(profissional));
    }

    public async Task<Resultado<object>> AlternarAtivoAsync(Guid? lojaId, Guid id)
    {
        var profissional = await repositorio.ObterProfissionalAsync(id, lojaId);
        if (profissional is null) return Resultado.NaoEncontrado();

        profissional.Ativo = !profissional.Ativo;

        // O salário fixo acompanha: desativado não gera mais lançamentos
        if (profissional.LancamentoFixoId.HasValue)
        {
            var fixo = await repositorio.ObterLancamentoFixoAsync(profissional.LancamentoFixoId.Value);
            if (fixo != null) fixo.Ativa = profissional.Ativo;
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { profissional.Id, profissional.Ativo });
    }

    // Quem já tem agendamento ou comissão não some (o histórico perderia o vínculo): é desativado.
    public async Task<Resultado<object>> ExcluirAsync(Guid? lojaId, Guid id)
    {
        var profissional = await repositorio.ObterProfissionalAsync(id, lojaId);
        if (profissional is null) return Resultado.NaoEncontrado();

        if (await repositorio.ProfissionalEmUsoAsync(id))
        {
            profissional.Ativo = false;
            await repositorio.SalvarAsync();
            return Resultado.Ok(new { mensagem = "Profissional em uso — foi desativado em vez de excluído." });
        }

        repositorio.Remover(profissional);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Profissional excluído." });
    }

    // ── Comissão por serviço (exceção ao padrão) ─────────────────────────────

    public async Task<Resultado<object>> DefinirComissaoServicoAsync(Guid? lojaId, Guid id, SalvarComissaoServicoRequest req)
    {
        var profissional = await repositorio.ObterProfissionalAsync(id, lojaId);
        if (profissional is null) return Resultado.NaoEncontrado();

        if (!await repositorio.ServicoDaLojaExisteAsync(req.ServicoId, lojaId!.Value))
            return Resultado.Invalido("Serviço não encontrado.");

        var existente = await repositorio.ObterComissaoServicoAsync(id, req.ServicoId);
        if (existente != null)
        {
            existente.ComissaoPercentual = req.ComissaoPercentual;
        }
        else
        {
            repositorio.Adicionar(new ComissaoServicoProfissional
            {
                LojaId = lojaId.Value,
                ProfissionalId = id,
                ServicoId = req.ServicoId,
                ComissaoPercentual = req.ComissaoPercentual,
            });
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Comissão do serviço definida." });
    }

    public async Task<Resultado<object>> RemoverComissaoServicoAsync(Guid? lojaId, Guid comissaoId)
    {
        var comissao = await repositorio.ObterComissaoServicoPorIdAsync(comissaoId, lojaId);
        if (comissao is null) return Resultado.NaoEncontrado();

        repositorio.Remover(comissao);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Exceção removida — volta a usar a comissão padrão do profissional." });
    }
}
