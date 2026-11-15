namespace LojaApi.Application.Plataforma;

// O que o painel do superadmin e a cobrança da mensalidade RECEBEM (os nomes das propriedades são o JSON do corpo)

// ── Lojas ──
public record CriarLojaRequest(
    string Nome, string Email,
    string? Cnpj, string? Cpf,
    string? Telefone, string? Endereco,
    string CorPrimaria,
    int MensalidadeDia,
    decimal MensalidadeValor,
    string AdminNome,
    string AdminEmail,
    string AdminSenha,
    string? PerfilId = null,
    string? TipoPlano = null,
    string? ModulosAtivos = null,
    bool EhTeste = false
);

public record AtualizarLojaRequest(
    string Nome, string Email,
    string? Cnpj, string? Cpf,
    string? Telefone, string? Endereco,
    string CorPrimaria, string? LogoUrl,
    int MensalidadeDia, decimal MensalidadeValor,
    string? TipoPlano, string? ModulosAtivos,
    bool? EhTeste
);

public record AlterarStatusLojaRequest(string Status, string? Motivo);
public record AtualizarPlanoRequest(string TipoPlano, string? ModulosAtivos);
public record AlterarTrialRequest(DateTime TrialAte);

public record TrocarEmailRequest(
    string NovoEmail,
    bool TrocarLogin,   // troca o e-mail do usuário (login)
    bool TrocarLoja     // troca o e-mail de contato da loja
);

public record AtualizarValorRequest(
    decimal NovoValor,
    bool SincronizarAssinatura   // true = também atualiza no MP; false = só próximas faturas
);

// ── Pagamentos e comunicados ──
public record RegistrarPagamentoManualRequest(
    Guid LojaId, decimal Valor,
    DateTime Vencimento, DateTime PagoEm,
    string FormaPagamento, string? Observacao
);

public record EnviarComunicadoRequest(List<Guid>? LojaIds, bool TodasLojas, string Assunto, string CorpoHtml, List<string>? EmailsExtras = null);

public record CriarPagamentoMpRequest(
    Guid PagamentoId,
    string FormaPagamento, // pix | boleto | cartao
    // Cartão de crédito
    string? CardToken,
    int? Parcelas,
    // Dados do pagador
    string? CpfPagador,
    string? NomePagador,
    string? EmailPagador
);

public record CriarAssinaturaRequest(
    string CardToken,
    string? CpfPagador,
    string? NomePagador,
    string? EmailPagador,
    string? CartaoFinal   // últimos 4 dígitos (exibição)
);
