namespace LojaApi.Domain.Servicos;

public static class StatusAgendamento
{
    public const string Agendado = "agendado";
    public const string Concluido = "concluido";
    public const string Cancelado = "cancelado";
    public const string Pendente = "pendente";   // pedido online esperando a loja aprovar
}

// Horário já ocupado na agenda (início e duração em minutos)
public readonly record struct Ocupacao(DateTime Inicio, int DuracaoMin);

public static class AgendamentoRegras
{
    // O painel só troca entre estes três (o "pendente" nasce do agendamento online)
    public static bool StatusValido(string? status) =>
        status is StatusAgendamento.Agendado or StatusAgendamento.Concluido or StatusAgendamento.Cancelado;

    // Um agendamento já pago virou venda no caixa: não pode ser cancelado nem excluído por aqui
    public static string? ValidarCancelamento(string novoStatus, bool pago) =>
        novoStatus == StatusAgendamento.Cancelado && pago
            ? "Não é possível cancelar: este agendamento já foi pago. Para estornar, faça o processo pelo caixa/vendas."
            : null;

    public static string? ValidarExclusao(bool pago) =>
        pago ? "Não é possível excluir: este agendamento já foi pago e gerou uma venda. Você pode cancelá-lo em vez de excluir." : null;

    // A comissão nasce só na primeira vez que o atendimento vira "concluido" (reabrir e concluir de novo não duplica)
    public static bool GeraComissao(string novoStatus, string statusAnterior, bool temProfissional) =>
        novoStatus == StatusAgendamento.Concluido && statusAnterior != StatusAgendamento.Concluido && temProfissional;

    // Valor da comissão (2 casas); sem percentual, ou percentual zero, não há comissão
    public static decimal? ValorComissao(decimal precoDoServico, decimal? percentual) =>
        percentual is > 0 ? Math.Round(precoDoServico * (percentual.Value / 100m), 2) : null;

    // Preço/duração informados na tela valem; zerados, vale o cadastro do serviço
    public static decimal PrecoFinal(decimal informado, decimal precoDoServico) => informado > 0 ? informado : precoDoServico;
    public static int DuracaoFinal(int informada, int duracaoDoServico) => informada > 0 ? informada : duracaoDoServico;

    // Dois horários se sobrepõem quando um começa antes de o outro terminar (encostar não conta)
    public static bool Conflita(DateTime inicio, DateTime fim, IEnumerable<Ocupacao> ocupados) =>
        ocupados.Any(o => inicio < o.Inicio.AddMinutes(o.DuracaoMin) && fim > o.Inicio);

    // Horários de início livres de um dia: de 30 em 30 min dentro do expediente, sem passar do fim,
    // sem horário já passado e sem sobrepor o que já está agendado.
    public static List<string> HorariosLivres(DateTime dia, int horaInicio, int horaFim, int duracaoDoServico,
        DateTime agoraLocal, IReadOnlyCollection<Ocupacao> ocupados)
    {
        var duracao = duracaoDoServico > 0 ? duracaoDoServico : 30;
        var slots = new List<string>();

        for (int h = horaInicio; h < horaFim; h++)
        {
            foreach (var m in new[] { 0, 30 })
            {
                var inicioSlot = dia.AddHours(h).AddMinutes(m);
                var fimSlot = inicioSlot.AddMinutes(duracao);

                if (fimSlot > dia.AddHours(horaFim)) continue;   // não pode passar da faixa de funcionamento
                if (inicioSlot <= agoraLocal) continue;          // não oferece horário que já passou
                if (Conflita(inicioSlot, fimSlot, ocupados)) continue;

                slots.Add(inicioSlot.ToString("HH:mm"));
            }
        }
        return slots;
    }

    // Loja sem configuração usa aprovação manual
    public static string ConfirmacaoOnline(string? configurada) => string.IsNullOrEmpty(configurada) ? "aprovacao" : configurada;

    public static string StatusDoAgendamentoOnline(string confirmacao) =>
        confirmacao == "automatico" ? StatusAgendamento.Agendado : StatusAgendamento.Pendente;

    public static string MensagemDoAgendamentoOnline(string status) =>
        status == StatusAgendamento.Pendente
            ? "Solicitação enviada! A loja vai confirmar seu horário em breve."
            : "Agendamento confirmado!";

    // Cliente é reconhecido pelo telefone só com dígitos (ignora parênteses, traços e espaços)
    public static string SoDigitos(string? telefone) => new((telefone ?? "").Where(char.IsDigit).ToArray());

    // Para a tela pública só mostramos o primeiro nome (privacidade)
    public static string PrimeiroNome(string nome) =>
        nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
}
