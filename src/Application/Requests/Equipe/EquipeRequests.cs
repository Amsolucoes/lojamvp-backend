namespace LojaApi.Application.Equipe;

public record SalvarFuncionarioRequest(
    string Nome,
    decimal? ComissaoPadraoPercentual,
    bool Ativo,
    int? DiaPagamentoPadrao = null,
    string TipoRemuneracao = "comissao",
    decimal? SalarioFixo = null,
    Guid? ContaBancariaId = null,
    string? Telefone = null,
    string? Cep = null,
    string? Endereco = null,
    string ComissaoBaseCalculo = "total",
    decimal? ValorDiaria = null,
    bool ComissaoVendasAtiva = false,
    decimal? ComissaoVendasPercentual = null,
    string? Numero = null,
    string? Bairro = null,
    string? Cidade = null,
    string? Uf = null);

// Comissão por serviço (exceção ao padrão do profissional)
public record SalvarComissaoServicoRequest(Guid ServicoId, decimal ComissaoPercentual);

// Fechar e pagar as comissões (+ diárias, se houver) de um profissional num período
public record FecharComissaoRequest(Guid ProfissionalId, DateTime PeriodoInicio, DateTime PeriodoFim, Guid? ContaBancariaId, DateTime? Vencimento, int QtdDiarias = 0);
