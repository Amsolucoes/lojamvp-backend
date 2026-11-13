using LojaApi.Models;
using LojaApi.src.Models.OrdemServico;

namespace LojaApi.Application.Equipe;

public record ComissaoServicoDto(Guid Id, Guid ServicoId, decimal ComissaoPercentual);

// Funcionário na lista (com as exceções de comissão por serviço)
public record ProfissionalDetalheDto(
    Guid Id, string Nome, bool Ativo, decimal? ComissaoPadraoPercentual, int? DiaPagamentoPadrao, string TipoRemuneracao,
    decimal? SalarioFixo, decimal? ValorDiaria, string? Telefone, string? Cep, string? Endereco, string? Numero, string? Bairro,
    string? Cidade, string? Uf, string ComissaoBaseCalculo, bool ComissaoVendasAtiva, decimal? ComissaoVendasPercentual,
    IEnumerable<ComissaoServicoDto> ComissoesPorServico);

// Funcionário depois de criar/editar
public record ProfissionalDto(
    Guid Id, string Nome, bool Ativo, decimal? ComissaoPadraoPercentual, int? DiaPagamentoPadrao, string TipoRemuneracao,
    decimal? SalarioFixo, decimal? ValorDiaria, string? Telefone, string? Cep, string? Endereco, string? Numero, string? Bairro,
    string? Cidade, string? Uf, string ComissaoBaseCalculo, bool ComissaoVendasAtiva, decimal? ComissaoVendasPercentual);

// Seletores de outras telas (id + nome)
public record ProfissionalAtivoDto(Guid Id, string Nome);

public record ResumoComissaoProfissionalDto(
    Guid ProfissionalId, string ProfissionalNome, int? DiaPagamentoPadrao, string TipoRemuneracao, decimal? ValorDiaria,
    int QtdAtendimentos, decimal ValorTotal);

public record ComissaoDetalheDto(
    Guid Id, decimal ValorServico, decimal ComissaoPercentual, decimal ValorComissao,
    string Status, DateTime? PagoEm, DateTime CriadoEm,
    string? NomeServico, string? NomeCliente, DateTime? DataAtendimento,
    string Tipo = "agendamento", List<ComissaoItemDto>? Itens = null);

// Item que originou a comissão: serviço feito, peça/produto da ordem de serviço ou item da venda
public record ComissaoItemDto(string Descricao, decimal Quantidade, decimal Valor, string Tipo);

public record FechamentoListaDto(
    Guid Id, Guid ProfissionalId, string ProfissionalNome, DateTime PeriodoInicio, DateTime PeriodoFim,
    decimal ValorTotal, int QtdAtendimentos, DateTime PagoEm);

// De onde vieram as comissões (ordens de serviço e vendas, com seus itens e clientes), buscado em lote
public record OrigensDasComissoesDto(
    List<OrcamentoServico> Orcamentos, List<Venda> Vendas, List<ItemVenda> ItensVenda,
    List<ItemOrcamentoServico> ItensOrdemServico, List<Cliente> Clientes);
