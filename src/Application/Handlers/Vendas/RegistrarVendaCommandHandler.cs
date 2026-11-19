using LojaApi.Application.Carteira;
using LojaApi.Domain.Bonificacoes;
using LojaApi.Domain.Carteira;
using LojaApi.Domain.Servicos;
using LojaApi.Domain.Vendas;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;
using static LojaApi.Application.Vendas.VendasApoio;

namespace LojaApi.Application.Vendas;

// Registra uma venda do caixa: valida os itens, baixa o estoque, usa plano/crédito/carteira, marca o agendamento como pago e gera a comissão.
// Item marcado como bonificação é um brinde dentro da venda (só o administrador): preço zero, estoque baixado e registro no cliente.
public class RegistrarVendaCommandHandler(IVendasRepository repositorio, CarteiraService carteira) : ICommandHandler<RegistrarVendaCommand>
{
    public async Task<Resultado<object>> HandleAsync(RegistrarVendaCommand command)
    {
        var (lojaId, req, podeBonificar, usuarioId) = command;

        if (!req.Itens.Any())
            return Resultado.Invalido("A venda deve ter ao menos um item.");

        // Brinde dentro da venda: só o administrador, só produto, e o cliente (dono do brinde) precisa estar na venda
        if (req.Itens.Any(i => i.Bonificacao))
        {
            if (!podeBonificar) return Resultado.Proibido("Somente o administrador pode dar bonificação.");
            if (!lojaId.HasValue) return Resultado.Invalido("Loja não encontrada.");
            if (!req.ClienteId.HasValue) return Resultado.Invalido("Informe o cliente para registrar a bonificação.");
            if (req.Itens.Any(i => i.Bonificacao && (i.ServicoId.HasValue || !i.ProdutoId.HasValue)))
                return Resultado.Invalido("Bonificação só vale para produtos.");
            foreach (var brinde in req.Itens.Where(i => i.Bonificacao))
            {
                var erroQuantidade = BonificacoesRegras.ValidarQuantidade(brinde.Quantidade, brinde.VariacaoId.HasValue);
                if (erroQuantidade is not null) return Resultado.Invalido(erroQuantidade);
            }
        }

        var (dataVendaFinal, erroData) = VendasRegras.DataDaVenda(req.DataVenda, DateTime.UtcNow);
        if (erroData is not null) return Resultado.Invalido(erroData);

        // ── Validação (só itens de produto validam estoque) ────────────
        var usosPlanoNestaVenda = new Dictionary<Guid, int>();
        foreach (var item in req.Itens)
        {
            if (item.ServicoId.HasValue)
            {
                var servico = await repositorio.ObterServicoAsync(item.ServicoId.Value, lojaId);
                if (servico is null) return Resultado.Invalido("Serviço não encontrado.");

                // Serviço usado dentro de um plano: respeita os dias da semana e o limite de usos por mês
                if (item.AssinaturaId.HasValue)
                {
                    var assinaturaUso = await repositorio.ObterAssinaturaAsync(item.AssinaturaId.Value, lojaId);
                    var planoUso = assinaturaUso is null ? null : await repositorio.ObterPlanoAsync(assinaturaUso.PlanoId);
                    if (assinaturaUso is null || planoUso is null)
                        return Resultado.Invalido("Assinatura do plano não encontrada.");

                    var dataUso = dataVendaFinal ?? DateTime.UtcNow;
                    if (!PlanoUsoRegras.DiaPermitido(planoUso.DiasSemanaPermitidos, dataUso))
                        return Resultado.Invalido($"O plano '{planoUso.Nome}' só pode ser usado em: {PlanoUsoRegras.DescreverDias(planoUso.DiasSemanaPermitidos)}.");

                    if (planoUso.LimiteUsosMes.HasValue)
                    {
                        var (inicioMes, fimMes) = PlanoUsoRegras.IntervaloDoMesUtc(dataUso);
                        var jaUsados = await repositorio.ContarConsumosDeAssinaturaAsync(assinaturaUso.Id, inicioMes, fimMes);
                        usosPlanoNestaVenda.TryGetValue(assinaturaUso.Id, out var nestaVenda);
                        if (jaUsados + nestaVenda + 1 > planoUso.LimiteUsosMes.Value)
                            return Resultado.Invalido($"O plano '{planoUso.Nome}' já atingiu o limite de {planoUso.LimiteUsosMes} uso(s) neste mês.");
                        usosPlanoNestaVenda[assinaturaUso.Id] = nestaVenda + 1;
                    }
                }
                continue; // serviço não tem estoque
            }

            if (!item.ProdutoId.HasValue)
                return Resultado.Invalido("Item sem produto nem serviço.");

            var produto = await repositorio.ObterProdutoAsync(item.ProdutoId.Value, lojaId);
            if (produto is null) return Resultado.Invalido($"Produto {item.ProdutoId} não encontrado.");
            if (!produto.Ativo) return Resultado.Invalido($"Produto '{produto.Nome}' está inativo.");

            if (item.VariacaoId.HasValue)
            {
                var variacao = await repositorio.ObterVariacaoAsync(item.VariacaoId.Value, lojaId);
                if (variacao is null) return Resultado.Invalido("Variação não encontrada.");
                if (variacao.Estoque < item.Quantidade)
                    return Resultado.Invalido($"Estoque insuficiente para '{produto.Nome}' ({variacao.Tamanho}/{variacao.Cor}).");
            }
            else
            {
                if (produto.Estoque < item.Quantidade)
                    return Resultado.Invalido($"Estoque insuficiente para '{produto.Nome}'.");
            }
        }

        // O cliente da venda tem que ser desta loja
        Cliente? clienteDaVenda = null;
        if (req.ClienteId.HasValue && lojaId.HasValue)
        {
            clienteDaVenda = await repositorio.ObterClienteAsync(req.ClienteId.Value, lojaId);
            if (clienteDaVenda is null) return Resultado.Invalido("Cliente não encontrado.");
        }

        // ── Cria venda ────────────────────────────────────────────────
        // O brinde não custa nada ao cliente: o preço enviado é ignorado
        static decimal PrecoDoItem(ItemVendaRequest i) => i.Bonificacao ? 0m : i.PrecoUnitario;
        decimal total = req.Itens.Sum(i => i.Quantidade * PrecoDoItem(i));
        string? origemNome = null;
        if (req.OrigemVendaId.HasValue)
        {
            var origem = await repositorio.ObterOrigemAsync(req.OrigemVendaId.Value, lojaId);
            origemNome = origem?.Nome;
        }

        Profissional? funcionario = null;
        if (req.FuncionarioId.HasValue)
        {
            funcionario = await repositorio.ObterFuncionarioDaLojaAsync(req.FuncionarioId.Value, lojaId);
            if (funcionario is null) return Resultado.Invalido("Funcionário não encontrado.");
        }
        string? nomeFuncionario = funcionario?.Nome;

        var venda = new Venda
        {
            ClienteId = req.ClienteId,
            Total = total,
            Desconto = req.Desconto,
            TotalFinal = total - req.Desconto,
            FormaPagamento = req.FormaPagamento,
            FormasPagamento = req.FormasPagamento,
            Troco = req.Troco,
            LojaId = lojaId,
            OrigemVendaId = req.OrigemVendaId,
            OrigemNome = origemNome,
            FuncionarioId = req.FuncionarioId,
            NomeFuncionario = nomeFuncionario,
            CreditoUsado = req.CreditoUsado,
        };
        if (dataVendaFinal.HasValue) venda.CriadaEm = dataVendaFinal.Value;

        // ── Carteira: parte (ou a venda toda) fica para o cliente pagar depois ──
        var valorDevido = venda.TotalFinal - (req.CreditoUsado ?? 0);
        var valorCarteira = RegrasCarteira.ValorNaCarteira(req.FormasPagamento, req.FormaPagamento, valorDevido);
        if (valorCarteira > 0)
        {
            if (valorCarteira > valorDevido + 0.01m)
                return Resultado.Invalido("O valor na carteira não pode ser maior que o total da venda.");

            var compra = await carteira.LancarCompraDaVendaAsync(lojaId!.Value, req.ClienteId, venda.Id, valorCarteira, venda.CriadaEm);
            if (!compra.Sucesso) return Resultado.Invalido(compra.Mensagem!);
            venda.ValorCarteira = valorCarteira;
        }

        repositorio.Adicionar(venda);

        // ── Itens ─────────────────────────────────────────────────────
        foreach (var item in req.Itens)
        {
            // Item de SERVIÇO — não baixa estoque, não gera movimento
            if (item.ServicoId.HasValue)
            {
                var servico = await repositorio.ObterServicoAsync(item.ServicoId.Value, lojaId);
                repositorio.Adicionar(new ItemVenda
                {
                    VendaId = venda.Id,
                    ProdutoId = null,
                    ServicoId = servico!.Id,
                    NomeProduto = servico.Nome,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = PrecoDoItem(item),
                    Subtotal = item.Quantidade * PrecoDoItem(item),
                });

                if (item.AgendamentoId.HasValue)
                {
                    var ag = await repositorio.ObterAgendamentoAsync(item.AgendamentoId.Value);
                    if (ag != null && ag.LojaId == lojaId)
                    {
                        ag.Pago = true;
                        ag.VendaId = venda.Id;
                    }
                }

                if (item.AssinaturaId.HasValue)
                {
                    repositorio.Adicionar(new ConsumoPlano
                    {
                        AssinaturaId = item.AssinaturaId.Value,
                        LojaId = lojaId!.Value,
                        ServicoId = servico!.Id,
                        NomeServico = servico.Nome,
                        VendaId = venda.Id,
                    });
                }

                continue;
            }

            // Item de PRODUTO
            var produto = await repositorio.ObterProdutoAsync(item.ProdutoId!.Value, lojaId);

            string nomeProduto = produto!.Nome;
            if (item.VariacaoId.HasValue)
            {
                var variacao = await repositorio.ObterVariacaoAsync(item.VariacaoId.Value, lojaId);
                if (variacao != null)
                {
                    nomeProduto = VendasRegras.NomeDoProdutoComVariacao(produto.Nome, variacao.Tamanho, variacao.Cor);

                    variacao.Estoque -= (int)item.Quantidade;
                    variacao.AtualizadoEm = DateTime.UtcNow;
                }
            }
            else
            {
                produto.Estoque -= item.Quantidade;
                produto.AtualizadoEm = DateTime.UtcNow;
            }

            repositorio.Adicionar(new ItemVenda
            {
                VendaId = venda.Id,
                ProdutoId = item.ProdutoId,
                NomeProduto = nomeProduto,
                Quantidade = item.Quantidade,
                PrecoUnitario = PrecoDoItem(item),
                Subtotal = item.Quantidade * PrecoDoItem(item),
                Bonificacao = item.Bonificacao,
            });

            repositorio.Adicionar(new MovimentoEstoque
            {
                ProdutoId = item.ProdutoId!.Value,
                Tipo = "saida",
                Quantidade = item.Quantidade,
                Observacao = item.Bonificacao
                    ? BonificacoesRegras.ObservacaoDoMovimentoNaVenda(venda.Id, nomeProduto)
                    : $"Venda #{venda.Id.ToString()[..8]} - {nomeProduto}",
                LojaId = lojaId,
            });

            // Brinde: fica registrado no cliente, com o custo do produto
            if (item.Bonificacao)
            {
                repositorio.Adicionar(new Bonificacao
                {
                    LojaId = lojaId!.Value, ClienteId = req.ClienteId, NomeCliente = clienteDaVenda!.Nome,
                    ProdutoId = item.ProdutoId, VariacaoId = item.VariacaoId, NomeProduto = nomeProduto,
                    Quantidade = item.Quantidade, CustoUnitario = produto.PrecoCusto,
                    VendaId = venda.Id, UsuarioId = usuarioId,
                });
            }
        }

        // Desconta crédito usado do cliente
        if (req.CreditoUsado.HasValue && req.CreditoUsado > 0 && req.ClienteId.HasValue)
        {
            var clienteVenda = await repositorio.ObterClienteAsync(req.ClienteId.Value, lojaId);
            if (clienteVenda != null)
            {
                clienteVenda.CreditoLoja = Math.Max(0, clienteVenda.CreditoLoja - req.CreditoUsado.Value);
            }
        }

        // Comissão do funcionário sobre a venda — só itens que não vieram de um
        // agendamento (esses já geram comissão própria quando o agendamento é
        // concluído; comissionar de novo aqui duplicaria o valor).
        if (funcionario != null && funcionario.ComissaoVendasAtiva && funcionario.ComissaoVendasPercentual is > 0)
        {
            decimal baseComissao = req.Itens
                .Where(i => !i.AgendamentoId.HasValue)
                .Sum(i => i.Quantidade * PrecoDoItem(i));

            if (baseComissao > 0)
            {
                var valorComissaoVenda = VendasRegras.ValorDaComissao(baseComissao, funcionario.ComissaoVendasPercentual.Value);
                repositorio.Adicionar(new ComissaoFuncionario
                {
                    LojaId = lojaId!.Value,
                    ProfissionalId = funcionario.Id,
                    OrigemTipo = "venda",
                    OrigemId = venda.Id,
                    ValorServico = baseComissao,
                    ComissaoPercentual = funcionario.ComissaoVendasPercentual.Value,
                    ValorComissao = valorComissaoVenda,
                });
            }
        }

        await repositorio.SalvarAsync();

        var vendaSalva = await repositorio.ObterVendaCompletaAsync(venda.Id);

        return Resultado.Ok(ParaDto(vendaSalva!));
    }
}
