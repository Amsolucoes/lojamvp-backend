namespace LojaApi.Domain.Plataforma;

// Regras puras da plataforma (sem banco): dias de atraso e situação da assinatura, link (slug) da loja, preço do cadastro e módulos.
public static class PlataformaRegras
{
    // Bloqueia quando passa 1 dia do vencimento (vence dia D → bloqueia na madrugada de D+1).
    public const int DiasAtrasoBloqueio = 1;

    // Cadastro: as 10 primeiras lojas (fora financeiro puro) pagam o preço promocional nos primeiros meses
    public const int LojasNaPromocao = 10;
    public const decimal MensalidadePromocional = 89.90m;
    public const decimal MensalidadeNormal = 119.90m;
    public const decimal MensalidadeFinanceiroPuro = 39.90m;
    public const int MesesPromocionais = 3;
    public const int DiasDeTrial = 7;

    // Depois de desativar um módulo, só dá para reativar passados estes dias
    public const int CooldownModuloDias = 30;

    private static readonly TimeSpan OffsetBrasilia = TimeSpan.FromHours(-3); // sem horário de verão desde 2019

    // Módulos que o cadastro aceita como extras
    private static readonly HashSet<string> ModulosExtrasPermitidos = ["financeiro", "nf", "servicos"];

    // Dias de atraso contados em dias corridos no horário de Brasília (negativo = ainda não venceu).
    public static int DiasDeAtraso(DateTime vencimentoUtc, DateTime agoraUtc)
        => ((agoraUtc + OffsetBrasilia).Date - (vencimentoUtc + OffsetBrasilia).Date).Days;

    // Fase da assinatura para o contador de trial/carência. "status" é o nome do status da loja (Trial, Ativo, Bloqueado...).
    public static (string Fase, int DiasRestantes) CalcularSituacao(string status, DateTime trialAte, DateTime? proximoVencimento, DateTime agoraUtc)
    {
        if (status == "Trial")
        {
            var diasTrial = (int)Math.Ceiling((trialAte - agoraUtc).TotalDays);
            if (diasTrial >= 0)
                return ("trial", diasTrial);

            // Trial vencido, em carência
            var diasCarencia = DiasAtrasoBloqueio - DiasDeAtraso(trialAte, agoraUtc);
            return ("carencia", Math.Max(0, diasCarencia));
        }

        if (status == "Ativo" && proximoVencimento.HasValue)
        {
            var diasAteVenc = (int)Math.Ceiling((proximoVencimento.Value - agoraUtc).TotalDays);
            if (diasAteVenc < 0)
            {
                var diasCarencia = DiasAtrasoBloqueio - DiasDeAtraso(proximoVencimento.Value, agoraUtc);
                return ("carencia", Math.Max(0, diasCarencia));
            }
            return ("ativo", diasAteVenc);
        }

        return (status.ToLower(), 0);
    }

    // Texto vira link de URL: minúsculo, sem acento, só letras/números e hífens (sem hífens repetidos nem nas pontas)
    public static string GerarSlug(string texto)
    {
        var normalizado = texto.Trim().ToLowerInvariant();
        var semAcento = new string(normalizado
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray());
        var slug = new string(semAcento.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }

    public static bool SlugValido(string slug) => slug.Length >= 3;

    public static string GerarSchemaNome(string nomeLoja, DateTime agoraUtc)
    {
        var slug = new string(nomeLoja.ToLower()
            .Replace(" ", "_").Replace("-", "_")
            .Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        return $"loja_{slug}_{agoraUtc.Ticks % 10000}";
    }

    // "automatico" ou "aprovacao" (qualquer outro texto vira aprovação)
    public static string ConfirmacaoDoAgendamentoOnline(string confirmacao) => confirmacao == "automatico" ? "automatico" : "aprovacao";

    // Agenda: início de 0 a 23 e fim de 1 a 24
    public static (int Inicio, int Fim) HorarioDaAgenda(int inicio, int fim) => (Math.Clamp(inicio, 0, 23), Math.Clamp(fim, 1, 24));

    public static string[] SepararModulos(string? modulos) =>
        (modulos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // Dias que faltam para poder reativar um módulo desativado em "ultimaDesativacao" (0 = já pode)
    public static int DiasParaReativarModulo(DateTime ultimaDesativacao, DateTime agoraUtc)
    {
        var diasPassados = (agoraUtc - ultimaDesativacao).TotalDays;
        return diasPassados < CooldownModuloDias ? (int)Math.Ceiling(CooldownModuloDias - diasPassados) : 0;
    }

    // Fim do dia de retorno da pausa (23:59 UTC); a data tem de ser hoje ou futura
    public static bool PausaValida(DateTime? pausaAte, DateTime agoraUtc) => pausaAte.HasValue && pausaAte.Value.Date >= agoraUtc.Date;
    public static DateTime FimDaPausa(DateTime pausaAte) => DateTime.SpecifyKind(pausaAte.Date, DateTimeKind.Utc).AddHours(23).AddMinutes(59);

    public static string MensagemDaPausa(string? mensagem) =>
        string.IsNullOrWhiteSpace(mensagem) ? "Estamos temporariamente fechados. Voltamos em breve!" : mensagem.Trim();

    // ── Cadastro ──

    // Só as chaves que o cadastro aceita, sem repetir
    public static List<string> ModulosExtrasValidos(IEnumerable<string>? extras) =>
        (extras ?? []).Where(ModulosExtrasPermitidos.Contains).Distinct().ToList();

    public static bool EhPromocional(bool financeiroPuro, int totalLojas) => !financeiroPuro && totalLojas < LojasNaPromocao;

    public static decimal MensalidadeBase(bool financeiroPuro, bool promocional) =>
        financeiroPuro ? MensalidadeFinanceiroPuro : (promocional ? MensalidadePromocional : MensalidadeNormal);

    // Módulos que o perfil escolhido liga sozinho
    public static List<string> ModulosDoPerfil(string tipoPlanoAplica, string nomePerfil)
    {
        var modulos = new List<string>();
        if (tipoPlanoAplica == "servicos" || tipoPlanoAplica == "loja_modulos")
            modulos.Add("servicos");
        else if (nomePerfil == "Corretora")
            modulos.Add("corretora");
        else if (nomePerfil.StartsWith("Pilates"))
            modulos.Add("turmas");
        return modulos;
    }

    // ── Cobrança ──

    // Vencimento do próximo mês (pagou em julho → vence em agosto), às 12:00 UTC para não "voltar um dia" no fuso local
    public static DateTime ProximoVencimento(int dia, DateTime agoraUtc)
    {
        var mesAlvo = new DateTime(agoraUtc.Year, agoraUtc.Month, 1, 12, 0, 0, DateTimeKind.Utc).AddMonths(1);
        var maxDia = DateTime.DaysInMonth(mesAlvo.Year, mesAlvo.Month);
        return new DateTime(mesAlvo.Year, mesAlvo.Month, Math.Min(dia, maxDia), 12, 0, 0, DateTimeKind.Utc);
    }

    // Próxima ocorrência do dia de vencimento (de 1 a 28): se já passou (ou é agora), vai para o mês seguinte
    public static DateTime ProximoDiaDeCobranca(int dia, DateTime agoraUtc)
    {
        var diaValido = Math.Clamp(dia, 1, 28);
        var candidato = new DateTime(agoraUtc.Year, agoraUtc.Month, diaValido, 12, 0, 0, DateTimeKind.Utc);
        if (candidato <= agoraUtc.AddHours(1))
            candidato = candidato.AddMonths(1);
        return candidato;
    }

    public static string NormalizarEmail(string? email) => (email ?? "").Trim().ToLower();
}
