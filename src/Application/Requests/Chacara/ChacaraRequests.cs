namespace LojaApi.Application.Chacara;

public record AtualizarConfiguracaoPrecoRequest(
    int MinimoPessoas,
    int LimitePessoasParaTaxaLimpeza,
    decimal ValorTaxaLimpeza,
    decimal ValorMultaNaoLimpeza,
    decimal PercentualEntradaMinimo,
    decimal? PercentualTaxaCartao = null,
    decimal? ValorLimiteParcelas = null,
    int? ParcelasAteLimite = null,
    int? ParcelasAcimaLimite = null
);

public record SalvarFaixaRequest(int PessoasAte, decimal ValorDiariaSemana, decimal ValorDiariaFimSemana, decimal ValorPacote2DiasFimSemana);

public record AdicionarFotoRequest(string Url);

public record ReordenarFotosRequest(List<int> Ids); // ids na nova ordem desejada

public record SalvarHorarioRequest(string Tipo, string Hora, decimal Ajuste, int Ordem);

public record AtualizarInfoRequest(
    string Descricao, string Endereco, string Comodidades, string? ComodidadesExtras, string? MapaEmbedUrl,
    string? HoraEntrada, string? HoraSaida, string? AvisosUso,
    string? LocadorNome, string? LocadorRg, string? LocadorCpf, string? LocadorEndereco, string? LocadorTelefone, string? CidadeAssinatura
);

public record SalvarPagamentoRequest(string AccessToken, string PublicKey);

public record EscolherPagamentoRequest(string FormaPagamento); // pix | cartao | combinado

public record PagarCartaoRequest(string Token, int Parcelas);

public record SalvarPeriodoRequest(string Nome, DateTime DataInicio, DateTime DataFim, decimal ValorTotal);

public record ConfirmarComPagamentoRequest(decimal? ValorPago);

public record RegistrarPrejuizoRequest(decimal? Valor, string? Observacao);

public record AvaliarClienteRequest(int? Nota, string? Comentario);

public record RegistrarPagamentoRequest(decimal Valor);

public record CriarReservaManualRequest(
    DateTime DataInicio, DateTime DataFim, int Pessoas,
    string ClienteNome, string? ClienteEmail, string? ClienteTelefone,
    string? ClienteDocumento, string? ClienteCep, string? ClienteEndereco, string? ClienteNumero, string? ClienteCidade,
    string? HoraEntrada, string? HoraSaida,
    decimal Valor, decimal? ValorPago
);

public record EditarReservaRequest(
    DateTime DataInicio, DateTime DataFim, int Pessoas, string ClienteNome, string ClienteEmail, string ClienteTelefone,
    string? ClienteDocumento, string? ClienteCep, string? ClienteEndereco, string? ClienteNumero, string? ClienteCidade,
    string? HoraEntrada, string? HoraSaida, decimal? ValorManual
);

public record ReservarPublicoRequest(
    DateTime DataInicio,
    DateTime DataFim,
    int Pessoas,
    string ClienteNome,
    string ClienteEmail,
    string ClienteTelefone,
    string? ClienteDocumento,
    string? ClienteCep,
    string? ClienteEndereco,
    string? HoraEntrada,
    string? HoraSaida
);

public record EnviarAvaliacaoChacaraRequest(int Nota, string? Comentario);
