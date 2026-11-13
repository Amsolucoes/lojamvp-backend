using System.Text.Json;
using LojaApi.src.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Integracao;

// Turmas (aulas recorrentes): cadastro, matrícula, agenda de sessões, falta/remarcação, presença e profissionais.
[Collection(ColecaoApi.Nome)]
public class TurmasApiTests(ApiFixture api)
{
    private static string Hoje(int dias = 0) => DateTime.UtcNow.Date.AddDays(dias).ToString("yyyy-MM-dd");
    private static string Janela => $"de={Hoje(-1)}&ate={Hoje(60)}";

    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync(moduloReservasAtivo: false);

    private static async Task<Guid> NovaTurmaAsync(LojaTeste loja, string nome = "Pilates", int capacidade = 2, string horario = "23:59", int? diaSemana = null)
    {
        var r = await loja.Painel.Post("/api/turmas", new { nome, diaSemana = diaSemana ?? (int)DateTime.UtcNow.DayOfWeek, horario, duracaoMin = 50, capacidade });
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    private static Task<Resposta> Matricular(LojaTeste loja, Guid turma, Guid cliente) => loja.Painel.Post($"/api/turmas/{turma}/matricular", new { clienteId = cliente });

    private static async Task<JsonElement> SessoesAsync(LojaTeste loja) => (await loja.Painel.Get($"/api/turmas/sessoes?{Janela}")).Json;

    private static async Task<Guid> InscricaoAsync(LojaTeste loja, Guid cliente)
    {
        var sessoes = await SessoesAsync(loja);
        return sessoes.EnumerateArray().SelectMany(s => s.GetProperty("alunos").EnumerateArray())
            .First(a => a.GetProperty("clienteId").GetGuid() == cliente).GetProperty("id").GetGuid();
    }

    // ── Cadastro ─────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login()
    {
        var loja = await NovaAsync();
        Assert.Equal(401, (await loja.Site.Get("/api/turmas")).Codigo);
    }

    [FatoIntegracao]
    public async Task Criar_gera_as_sessoes_das_proximas_6_semanas()
    {
        var loja = await NovaAsync();

        var id = await NovaTurmaAsync(loja, "  Pilates Solo ");

        var datas = await api.NoBancoAsync(db => db.SessoesTurma.AsNoTracking().Where(s => s.TurmaId == id).OrderBy(s => s.DataHora).Select(s => s.DataHora).ToListAsync());
        Assert.Equal(6, datas.Count);
        Assert.All(datas, d => Assert.Equal((int)DateTime.UtcNow.DayOfWeek, (int)d.DayOfWeek));
        Assert.All(datas, d => Assert.Equal(new TimeSpan(23, 59, 0), d.TimeOfDay));
        Assert.Equal(7, (datas[1] - datas[0]).Days);
        Assert.Equal("Pilates Solo", (await api.NoBancoAsync(db => db.Turmas.AsNoTracking().FirstAsync(t => t.Id == id))).Nome);
    }

    [FatoIntegracao]
    public async Task Listar_mostra_horario_alunos_e_so_as_turmas_da_loja()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var turma = await NovaTurmaAsync(a, "Pilates", 5, "08:30", 3);
        await NovaTurmaAsync(b, "Da B");
        var ana = await a.ClienteIdAsync("Ana", "67911110000");
        var bia = await a.ClienteIdAsync("Bia", "67922220000");
        await Matricular(a, turma, ana);
        await Matricular(a, turma, bia);

        var lista = (await a.Painel.Get("/api/turmas")).Json;

        var t = Assert.Single(lista.EnumerateArray());
        Assert.Equal(("Pilates", 3, "08:30", 50, 5, true, 2),
            (t.GetProperty("nome").GetString(), t.GetProperty("diaSemana").GetInt32(), t.GetProperty("horario").GetString(), t.GetProperty("duracaoMin").GetInt32(),
             t.GetProperty("capacidade").GetInt32(), t.GetProperty("ativa").GetBoolean(), t.GetProperty("qtdAlunos").GetInt32()));
    }

    [FatoIntegracao]
    public async Task Atualizar_e_alternar_ativa_e_isolamento()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var id = await NovaTurmaAsync(a);

        var r = await a.Painel.Put($"/api/turmas/{id}", new { nome = " Yoga ", diaSemana = 5, horario = "19:15", duracaoMin = 90, capacidade = 12 });
        Assert.Equal(200, r.Codigo);
        var t = await api.NoBancoAsync(db => db.Turmas.AsNoTracking().FirstAsync(x => x.Id == id));
        Assert.Equal(("Yoga", 5, new TimeSpan(19, 15, 0), 90, 12), (t.Nome, t.DiaSemana, t.Horario, t.DuracaoMin, t.Capacidade));

        Assert.False((await a.Painel.Patch($"/api/turmas/{id}/ativo")).Json.GetProperty("ativa").GetBoolean());
        Assert.True((await a.Painel.Patch($"/api/turmas/{id}/ativo")).Json.GetProperty("ativa").GetBoolean());

        Assert.Equal(404, (await b.Painel.Put($"/api/turmas/{id}", new { nome = "x", diaSemana = 1, horario = "10:00", duracaoMin = 60, capacidade = 1 })).Codigo);
        Assert.Equal(404, (await b.Painel.Patch($"/api/turmas/{id}/ativo")).Codigo);
        Assert.Equal(404, (await b.Painel.Delete($"/api/turmas/{id}")).Codigo);
        Assert.Equal(404, (await a.Painel.Put($"/api/turmas/{Guid.NewGuid()}", new { nome = "x", diaSemana = 1, horario = "10:00", duracaoMin = 60, capacidade = 1 })).Codigo);
        Assert.Equal("Yoga", (await api.NoBancoAsync(db => db.Turmas.AsNoTracking().FirstAsync(x => x.Id == id))).Nome);
    }

    [FatoIntegracao]
    public async Task Turma_com_aluno_matriculado_nao_e_excluida_ate_desmatricular()
    {
        var loja = await NovaAsync();
        var turma = await NovaTurmaAsync(loja);
        var cliente = await loja.ClienteIdAsync();
        await Matricular(loja, turma, cliente);

        var bloqueada = await loja.Painel.Delete($"/api/turmas/{turma}");
        Assert.Equal((400, "Não é possível excluir: esta turma tem alunos matriculados. Desative em vez de excluir."), (bloqueada.Codigo, bloqueada.Erro));

        var matricula = await api.NoBancoAsync(db => db.MatriculasTurma.AsNoTracking().FirstAsync(m => m.TurmaId == turma));
        (await loja.Painel.Delete($"/api/turmas/matricula/{matricula.Id}")).DeveSerSucesso();
        Assert.Equal("Turma excluída.", (await loja.Painel.Delete($"/api/turmas/{turma}")).Json.GetProperty("mensagem").GetString());
    }

    // ── Matrícula ────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Matricular_inscreve_o_aluno_em_todas_as_sessoes_futuras()
    {
        var loja = await NovaAsync();
        var turma = await NovaTurmaAsync(loja);
        var cliente = await loja.ClienteIdAsync("Ana");

        var r = await Matricular(loja, turma, cliente);

        Assert.Equal("Aluno matriculado.", r.Json.GetProperty("mensagem").GetString());
        var inscricoes = await api.NoBancoAsync(db => db.InscricoesSessao.AsNoTracking().Where(i => i.ClienteId == cliente).ToListAsync());
        Assert.Equal(6, inscricoes.Count);
        Assert.All(inscricoes, i => Assert.Equal(("fixo", "confirmado", loja.Id), (i.Tipo, i.Status, i.LojaId)));
    }

    [FatoIntegracao]
    public async Task Matricular_recusa_repetido_e_turma_cheia()
    {
        var loja = await NovaAsync();
        var turma = await NovaTurmaAsync(loja, capacidade: 1);
        var ana = await loja.ClienteIdAsync("Ana", "67911110000");
        var bia = await loja.ClienteIdAsync("Bia", "67922220000");
        (await Matricular(loja, turma, ana)).DeveSerSucesso();

        var repetido = await Matricular(loja, turma, ana);
        Assert.Equal((400, "Aluno já matriculado nesta turma."), (repetido.Codigo, repetido.Erro));

        var cheia = await Matricular(loja, turma, bia);
        Assert.Equal((400, "Turma cheia (1 vaga(s))."), (cheia.Codigo, cheia.Erro));
        Assert.Equal(404, (await Matricular(loja, Guid.NewGuid(), bia)).Codigo);
    }

    [FatoIntegracao]
    public async Task Listar_alunos_so_os_matriculados_ativos()
    {
        var loja = await NovaAsync();
        var turma = await NovaTurmaAsync(loja);
        var ana = await loja.ClienteIdAsync("Ana", "67911110000");
        var bia = await loja.ClienteIdAsync("Bia", "67922220000");
        await Matricular(loja, turma, ana);
        await Matricular(loja, turma, bia);
        var matriculaBia = await api.NoBancoAsync(db => db.MatriculasTurma.AsNoTracking().FirstAsync(m => m.ClienteId == bia));
        await loja.Painel.Delete($"/api/turmas/matricula/{matriculaBia.Id}");

        var alunos = (await loja.Painel.Get($"/api/turmas/{turma}/alunos")).Json;

        var aluno = Assert.Single(alunos.EnumerateArray());
        Assert.Equal(("Ana", "67911110000", ana), (aluno.GetProperty("nome").GetString(), aluno.GetProperty("telefone").GetString(), aluno.GetProperty("clienteId").GetGuid()));
        Assert.Equal(404, (await loja.Painel.Get($"/api/turmas/{Guid.NewGuid()}/alunos")).Codigo);
    }

    [FatoIntegracao]
    public async Task Desmatricular_tira_as_inscricoes_futuras_dessa_turma_e_so_dela()
    {
        var loja = await NovaAsync();
        var pilates = await NovaTurmaAsync(loja, "Pilates");
        var yoga = await NovaTurmaAsync(loja, "Yoga");
        var cliente = await loja.ClienteIdAsync();
        await Matricular(loja, pilates, cliente);
        await Matricular(loja, yoga, cliente);
        var matricula = await api.NoBancoAsync(db => db.MatriculasTurma.AsNoTracking().FirstAsync(m => m.TurmaId == pilates));

        var r = await loja.Painel.Delete($"/api/turmas/matricula/{matricula.Id}");

        Assert.Equal("Aluno desmatriculado.", r.Json.GetProperty("mensagem").GetString());
        Assert.False((await api.NoBancoAsync(db => db.MatriculasTurma.AsNoTracking().FirstAsync(m => m.Id == matricula.Id))).Ativa);
        var restantes = await api.NoBancoAsync(db => db.InscricoesSessao.AsNoTracking().Include(i => i.SessaoTurma).Where(i => i.ClienteId == cliente).ToListAsync());
        Assert.Equal(6, restantes.Count);                                   // só as da Yoga
        Assert.All(restantes, i => Assert.Equal(yoga, i.SessaoTurma!.TurmaId));
        Assert.Equal(404, (await loja.Painel.Delete($"/api/turmas/matricula/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Turma_inativa_nao_gera_sessoes()
    {
        var loja = await NovaAsync();
        var inativa = await NovaTurmaAsync(loja, "Inativa");
        await loja.Painel.Patch($"/api/turmas/{inativa}/ativo");                                        // desativa
        await api.NoBancoAsync(db => db.SessoesTurma.Where(s => s.TurmaId == inativa).ExecuteDeleteAsync());

        var ativa = await NovaTurmaAsync(loja, "Ativa");                                                // dispara a geração de todas as turmas

        Assert.Equal(6, await api.NoBancoAsync(db => db.SessoesTurma.CountAsync(s => s.TurmaId == ativa)));
        Assert.Equal(0, await api.NoBancoAsync(db => db.SessoesTurma.CountAsync(s => s.TurmaId == inativa)));
    }

    [FatoIntegracao]
    public async Task Desmatricular_preserva_o_historico_e_as_remarcacoes()
    {
        var loja = await NovaAsync();
        var turma = await NovaTurmaAsync(loja);
        var cliente = await loja.ClienteIdAsync();
        await Matricular(loja, turma, cliente);
        var passada = new SessaoTurma { LojaId = loja.Id, TurmaId = turma, DataHora = DateTime.UtcNow.AddDays(-7), Status = "realizada" };
        var futura = await api.NoBancoAsync(db => db.SessoesTurma.AsNoTracking().Where(s => s.TurmaId == turma).OrderBy(s => s.DataHora).FirstAsync());
        await api.NoBancoAsync(async db =>
        {
            db.SessoesTurma.Add(passada);
            await db.SaveChangesAsync();
            db.InscricoesSessao.Add(new InscricaoSessao { LojaId = loja.Id, SessaoTurmaId = passada.Id, ClienteId = cliente, Tipo = "fixo", Status = "compareceu" });       // aula que já aconteceu
            db.InscricoesSessao.Add(new InscricaoSessao { LojaId = loja.Id, SessaoTurmaId = futura.Id, ClienteId = cliente, Tipo = "remarcacao", Status = "confirmado" });  // vaga de reposição
            await db.SaveChangesAsync();
        });
        var matricula = await api.NoBancoAsync(db => db.MatriculasTurma.AsNoTracking().FirstAsync(m => m.TurmaId == turma));

        (await loja.Painel.Delete($"/api/turmas/matricula/{matricula.Id}")).DeveSerSucesso();

        var restantes = await api.NoBancoAsync(db => db.InscricoesSessao.AsNoTracking().Where(i => i.ClienteId == cliente).ToListAsync());
        Assert.Equal(2, restantes.Count);
        Assert.Contains(restantes, i => i.SessaoTurmaId == passada.Id && i.Tipo == "fixo");
        Assert.Contains(restantes, i => i.SessaoTurmaId == futura.Id && i.Tipo == "remarcacao");
    }

    [FatoIntegracao]
    public async Task Sessoes_incluem_o_ultimo_dia_da_janela()
    {
        var loja = await NovaAsync();
        await NovaTurmaAsync(loja, "Noturna", horario: "23:59");

        var r = (await loja.Painel.Get($"/api/turmas/sessoes?de={Hoje()}&ate={Hoje()}")).Json;

        Assert.Equal(1, r.GetArrayLength());   // a sessão de hoje às 23:59
    }

    // ── Sessões ──────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Sessoes_da_janela_trazem_alunos_vagas_ocupadas_e_so_da_loja()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var turma = await NovaTurmaAsync(a, "Pilates", 3);
        await NovaTurmaAsync(b, "Da B");
        var ana = await a.ClienteIdAsync("Ana", "67911110000");
        var bia = await a.ClienteIdAsync("Bia", "67922220000");
        await Matricular(a, turma, ana);
        await Matricular(a, turma, bia);

        var sessoes = await SessoesAsync(a);

        Assert.Equal(6, sessoes.GetArrayLength());
        var primeira = sessoes[0];
        Assert.Equal(("Pilates", 3, 2, "aberta"), (primeira.GetProperty("nomeTurma").GetString(), primeira.GetProperty("capacidade").GetInt32(),
            primeira.GetProperty("vagasOcupadas").GetInt32(), primeira.GetProperty("status").GetString()));
        Assert.Equal(["Ana", "Bia"], primeira.GetProperty("alunos").EnumerateArray().Select(x => x.GetProperty("nome").GetString()).OrderBy(x => x).ToList());
        Assert.Equal(0, (await a.Painel.Get($"/api/turmas/sessoes?de={Hoje(400)}&ate={Hoje(401)}")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Sessoes_com_vaga_escondem_as_lotadas()
    {
        var loja = await NovaAsync();
        var cheia = await NovaTurmaAsync(loja, "Cheia", 1);
        var folgada = await NovaTurmaAsync(loja, "Folgada", 3);
        await Matricular(loja, cheia, await loja.ClienteIdAsync("Ana", "67911110000"));

        var lista = (await loja.Painel.Get($"/api/turmas/sessoes-com-vaga?{Janela}")).Json.EnumerateArray().ToList();

        Assert.Equal(6, lista.Count);
        Assert.All(lista, s => Assert.Equal(("Folgada", 3), (s.GetProperty("nomeTurma").GetString(), s.GetProperty("vagas").GetInt32())));
    }

    // ── Falta, remarcação e presença ─────────────────────────────────────────

    [FatoIntegracao]
    public async Task Falta_avisada_sem_destino_so_registra()
    {
        var loja = await NovaAsync();
        var turma = await NovaTurmaAsync(loja);
        var cliente = await loja.ClienteIdAsync();
        await Matricular(loja, turma, cliente);
        var inscricao = await InscricaoAsync(loja, cliente);

        var r = await loja.Painel.Post($"/api/turmas/inscricoes/{inscricao}/falta", new { sessaoDestinoId = (Guid?)null });

        Assert.Equal("Falta registrada.", r.Json.GetProperty("mensagem").GetString());
        var gravada = await api.NoBancoAsync(db => db.InscricoesSessao.AsNoTracking().FirstAsync(i => i.Id == inscricao));
        Assert.Equal(("falta_avisada", null), (gravada.Status, gravada.RemarcadoParaSessaoId?.ToString()));
    }

    [FatoIntegracao]
    public async Task Falta_avisada_com_destino_remarca_e_cria_a_vaga_na_outra_sessao()
    {
        var loja = await NovaAsync();
        var turma = await NovaTurmaAsync(loja, "Pilates", 3);
        var outra = await NovaTurmaAsync(loja, "Reposição", 3);
        var cliente = await loja.ClienteIdAsync();
        await Matricular(loja, turma, cliente);
        var inscricao = await InscricaoAsync(loja, cliente);
        var destino = await api.NoBancoAsync(db => db.SessoesTurma.AsNoTracking().Where(s => s.TurmaId == outra).OrderBy(s => s.DataHora).Select(s => s.Id).FirstAsync());

        var r = await loja.Painel.Post($"/api/turmas/inscricoes/{inscricao}/falta", new { sessaoDestinoId = destino });

        Assert.Equal("Remarcado com sucesso.", r.Json.GetProperty("mensagem").GetString());
        var origem = await api.NoBancoAsync(db => db.InscricoesSessao.AsNoTracking().FirstAsync(i => i.Id == inscricao));
        Assert.Equal(("falta_avisada", destino), (origem.Status, origem.RemarcadoParaSessaoId));
        var nova = await api.NoBancoAsync(db => db.InscricoesSessao.AsNoTracking().SingleAsync(i => i.SessaoTurmaId == destino && i.ClienteId == cliente));
        Assert.Equal(("remarcacao", "confirmado"), (nova.Tipo, nova.Status));
    }

    [FatoIntegracao]
    public async Task Remarcar_recusa_destino_inexistente_ou_lotado_sem_alterar_a_inscricao()
    {
        var loja = await NovaAsync();
        var turma = await NovaTurmaAsync(loja, "Pilates", 3);
        var lotada = await NovaTurmaAsync(loja, "Lotada", 1);
        var ana = await loja.ClienteIdAsync("Ana", "67911110000");
        var bia = await loja.ClienteIdAsync("Bia", "67922220000");
        await Matricular(loja, turma, ana);
        await Matricular(loja, lotada, bia);
        var inscricao = await InscricaoAsync(loja, ana);
        var destinoLotado = await api.NoBancoAsync(db => db.SessoesTurma.AsNoTracking().Where(s => s.TurmaId == lotada).OrderBy(s => s.DataHora).Select(s => s.Id).FirstAsync());

        var inexistente = await loja.Painel.Post($"/api/turmas/inscricoes/{inscricao}/falta", new { sessaoDestinoId = Guid.NewGuid() });
        Assert.Equal((404, "Sessão de destino não encontrada."), (inexistente.Codigo, inexistente.Erro));

        var semVaga = await loja.Painel.Post($"/api/turmas/inscricoes/{inscricao}/falta", new { sessaoDestinoId = destinoLotado });
        Assert.Equal((400, "Sessão de destino está sem vagas."), (semVaga.Codigo, semVaga.Erro));

        Assert.Equal("confirmado", (await api.NoBancoAsync(db => db.InscricoesSessao.AsNoTracking().FirstAsync(i => i.Id == inscricao))).Status);
    }

    [FatoIntegracao]
    public async Task Presenca_marca_compareceu_ou_faltou()
    {
        var loja = await NovaAsync();
        var turma = await NovaTurmaAsync(loja);
        var cliente = await loja.ClienteIdAsync();
        await Matricular(loja, turma, cliente);
        var inscricao = await InscricaoAsync(loja, cliente);

        Assert.Equal("compareceu", (await loja.Painel.Post($"/api/turmas/inscricoes/{inscricao}/presenca", new { compareceu = true })).Json.GetProperty("status").GetString());
        Assert.Equal("faltou", (await loja.Painel.Post($"/api/turmas/inscricoes/{inscricao}/presenca", new { compareceu = false })).Json.GetProperty("status").GetString());
        Assert.Equal(404, (await loja.Painel.Post($"/api/turmas/inscricoes/{Guid.NewGuid()}/presenca", new { compareceu = true })).Codigo);

        // quem faltou some da lista da sessão
        var emTodas = (await SessoesAsync(loja)).EnumerateArray().SelectMany(s => s.GetProperty("alunos").EnumerateArray()).Count(a => a.GetProperty("id").GetGuid() == inscricao);
        Assert.Equal(0, emTodas);
    }

    // ── Profissionais ────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Profissionais_criar_listar_ativos_e_alternar()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var zeca = (await a.Painel.Post("/api/turmas/profissionais", new { nome = "  Zeca  " })).Json;
        await a.Painel.Post("/api/turmas/profissionais", new { nome = "Ana" });
        await b.Painel.Post("/api/turmas/profissionais", new { nome = "Da B" });
        Assert.Equal("Zeca", zeca.GetProperty("nome").GetString());

        Assert.Equal(["Ana", "Zeca"], (await a.Painel.Get("/api/turmas/profissionais")).Json.EnumerateArray().Select(p => p.GetProperty("nome").GetString()).ToList());

        var id = zeca.GetProperty("id").GetGuid();
        Assert.False((await a.Painel.Patch($"/api/turmas/profissionais/{id}/ativo")).Json.GetProperty("ativo").GetBoolean());
        Assert.Equal(["Ana"], (await a.Painel.Get("/api/turmas/profissionais")).Json.EnumerateArray().Select(p => p.GetProperty("nome").GetString()).ToList());
        Assert.Equal(404, (await b.Painel.Patch($"/api/turmas/profissionais/{id}/ativo")).Codigo);
    }

    [FatoIntegracao]
    public async Task Definir_profissional_do_aluno_na_sessao_e_aparece_na_agenda()
    {
        var loja = await NovaAsync();
        var turma = await NovaTurmaAsync(loja);
        var cliente = await loja.ClienteIdAsync();
        await Matricular(loja, turma, cliente);
        var inscricao = await InscricaoAsync(loja, cliente);
        var prof = (await loja.Painel.Post("/api/turmas/profissionais", new { nome = "Joana" })).Json.GetProperty("id").GetGuid();

        var r = await loja.Painel.Patch($"/api/turmas/inscricoes/{inscricao}/profissional", new { profissionalId = prof });

        Assert.Equal(prof, r.Json.GetProperty("profissionalId").GetGuid());
        var aluno = (await SessoesAsync(loja)).EnumerateArray().SelectMany(s => s.GetProperty("alunos").EnumerateArray()).First(a => a.GetProperty("id").GetGuid() == inscricao);
        Assert.Equal("Joana", aluno.GetProperty("profissionalNome").GetString());

        var limpou = await loja.Painel.Patch($"/api/turmas/inscricoes/{inscricao}/profissional", new { profissionalId = (Guid?)null });
        Assert.Equal(JsonValueKind.Null, limpou.Json.GetProperty("profissionalId").ValueKind);
        Assert.Equal(404, (await loja.Painel.Patch($"/api/turmas/inscricoes/{Guid.NewGuid()}/profissional", new { profissionalId = prof })).Codigo);
    }

    // Correção desta migração: antes, aluno (cliente) e profissional de OUTRA loja eram aceitos na matrícula / na inscrição.

    [FatoIntegracao]
    public async Task Nao_matricula_cliente_de_outra_loja()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var turmaB = await NovaTurmaAsync(b);
        var clienteDeA = await a.ClienteIdAsync("Cliente da A");

        var r = await Matricular(b, turmaB, clienteDeA);

        Assert.Equal((400, "Cliente não encontrado."), (r.Codigo, r.Erro));
        Assert.Equal(0, await api.NoBancoAsync(db => db.MatriculasTurma.CountAsync(m => m.ClienteId == clienteDeA)));
        Assert.Equal(0, await api.NoBancoAsync(db => db.InscricoesSessao.CountAsync(i => i.ClienteId == clienteDeA)));
        Assert.Equal(400, (await Matricular(b, turmaB, Guid.NewGuid())).Codigo);   // inexistente também
    }

    [FatoIntegracao]
    public async Task Nao_define_profissional_de_outra_loja_na_inscricao()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var turma = await NovaTurmaAsync(b);
        var cliente = await b.ClienteIdAsync();
        await Matricular(b, turma, cliente);
        var inscricao = await InscricaoAsync(b, cliente);
        var profissionalDeA = (await a.Painel.Post("/api/turmas/profissionais", new { nome = "Da A" })).Json.GetProperty("id").GetGuid();

        var r = await b.Painel.Patch($"/api/turmas/inscricoes/{inscricao}/profissional", new { profissionalId = profissionalDeA });

        Assert.Equal((400, "Profissional não encontrado."), (r.Codigo, r.Erro));
        Assert.Null((await api.NoBancoAsync(db => db.InscricoesSessao.AsNoTracking().FirstAsync(i => i.Id == inscricao))).ProfissionalId);
        Assert.Equal(400, (await b.Painel.Patch($"/api/turmas/inscricoes/{inscricao}/profissional", new { profissionalId = Guid.NewGuid() })).Codigo);
    }

    // ── Isolamento entre lojas ───────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Uma_loja_nao_mexe_na_matricula_nem_nas_inscricoes_de_outra()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var turma = await NovaTurmaAsync(a);
        var cliente = await a.ClienteIdAsync();
        await Matricular(a, turma, cliente);
        var inscricao = await InscricaoAsync(a, cliente);
        var matricula = await api.NoBancoAsync(db => db.MatriculasTurma.AsNoTracking().FirstAsync(m => m.TurmaId == turma));
        var clienteB = await b.ClienteIdAsync("Da B", "67900000009");

        Assert.Equal(404, (await Matricular(b, turma, clienteB)).Codigo);
        Assert.Equal(404, (await b.Painel.Delete($"/api/turmas/matricula/{matricula.Id}")).Codigo);
        Assert.Equal(404, (await b.Painel.Post($"/api/turmas/inscricoes/{inscricao}/falta", new { sessaoDestinoId = (Guid?)null })).Codigo);
        Assert.Equal(404, (await b.Painel.Post($"/api/turmas/inscricoes/{inscricao}/presenca", new { compareceu = true })).Codigo);
        Assert.Equal(0, (await b.Painel.Get($"/api/turmas/sessoes?{Janela}")).Json.GetArrayLength());
        Assert.Equal("confirmado", (await api.NoBancoAsync(db => db.InscricoesSessao.AsNoTracking().FirstAsync(i => i.Id == inscricao))).Status);
        Assert.True((await api.NoBancoAsync(db => db.MatriculasTurma.AsNoTracking().FirstAsync(m => m.Id == matricula.Id))).Ativa);
    }
}
