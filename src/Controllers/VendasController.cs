using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/vendas")]
[Authorize]
public class VendasController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate,
        [FromQuery] Guid? clienteId)
    {
        var lojaId = await GetLojaId();

        var q = db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .AsQueryable();

        if (lojaId.HasValue) q = q.Where(v => v.LojaId == lojaId);
        if (de.HasValue) q = q.Where(v => v.CriadaEm >= de.Value);
        if (ate.HasValue) q = q.Where(v => v.CriadaEm <= ate.Value.AddDays(1));
        if (clienteId.HasValue) q = q.Where(v => v.ClienteId == clienteId);

        var lista = await q.OrderByDescending(v => v.CriadaEm).Select(v => ToDto(v)).ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id)
    {
        var lojaId = await GetLojaId();
        var v = await db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (v is null || (lojaId.HasValue && v.LojaId != lojaId)) return NotFound();
        return Ok(ToDto(v));
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] CriarVendaRequest req)
    {
        var lojaId = await GetLojaId();

        if (!req.Itens.Any())
            return BadRequest(new { erro = "A venda deve ter ao menos um item." });

        DateTime? dataVendaFinal = null;
        if (req.DataVenda.HasValue)
        {
            var hoje = DateTime.UtcNow.Date;
            if (req.DataVenda.Value.Date > hoje)
                return BadRequest(new { erro = "A data da venda não pode ser no futuro." });

            // Usa a hora atual no dia escolhido, pra manter ordenação sensata dentro do próprio dia
            var agora = DateTime.UtcNow;
            dataVendaFinal = DateTime.SpecifyKind(req.DataVenda.Value.Date, DateTimeKind.Utc)
                .Add(agora.TimeOfDay);
        }

        // ── Validação (só itens de produto validam estoque) ────────────
        foreach (var item in req.Itens)
        {
            if (item.ServicoId.HasValue)
            {
                var servico = await db.Servicos.FindAsync(item.ServicoId.Value);
                if (servico is null) return BadRequest(new { erro = "Serviço não encontrado." });
                continue; // serviço não tem estoque
            }

            if (!item.ProdutoId.HasValue)
                return BadRequest(new { erro = "Item sem produto nem serviço." });

            var produto = await db.Produtos.FindAsync(item.ProdutoId.Value);
            if (produto is null) return BadRequest(new { erro = $"Produto {item.ProdutoId} não encontrado." });
            if (!produto.Ativo) return BadRequest(new { erro = $"Produto '{produto.Nome}' está inativo." });

            if (item.VariacaoId.HasValue)
            {
                var variacao = await db.ProdutoVariacoes.FindAsync(item.VariacaoId.Value);
                if (variacao is null) return BadRequest(new { erro = "Variação não encontrada." });
                if (variacao.Estoque < item.Quantidade)
                    return BadRequest(new { erro = $"Estoque insuficiente para '{produto.Nome}' ({variacao.Tamanho}/{variacao.Cor})." });
            }
            else
            {
                if (produto.Estoque < item.Quantidade)
                    return BadRequest(new { erro = $"Estoque insuficiente para '{produto.Nome}'." });
            }
        }

        // ── Cria venda ────────────────────────────────────────────────
        decimal total = req.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
        string? origemNome = null;
        if (req.OrigemVendaId.HasValue)
        {
            var origem = await db.OrigensVenda.FirstOrDefaultAsync(o => o.Id == req.OrigemVendaId.Value && o.LojaId == lojaId);
            origemNome = origem?.Nome;
        }

        Profissional? funcionario = null;
        if (req.FuncionarioId.HasValue)
        {
            funcionario = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == req.FuncionarioId.Value && p.LojaId == lojaId);
            if (funcionario is null) return BadRequest(new { erro = "Funcionário não encontrado." });
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
        db.Vendas.Add(venda);

        // ── Itens ─────────────────────────────────────────────────────
        foreach (var item in req.Itens)
        {
            // Item de SERVIÇO — não baixa estoque, não gera movimento
            if (item.ServicoId.HasValue)
            {
                var servico = await db.Servicos.FindAsync(item.ServicoId.Value);
                db.ItensVenda.Add(new ItemVenda
                {
                    VendaId = venda.Id,
                    ProdutoId = null,
                    ServicoId = servico!.Id,
                    NomeProduto = servico.Nome,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    Subtotal = item.Quantidade * item.PrecoUnitario,
                });

                if (item.AgendamentoId.HasValue)
                {
                    var ag = await db.Agendamentos.FindAsync(item.AgendamentoId.Value);
                    if (ag != null && ag.LojaId == lojaId)
                    {
                        ag.Pago = true;
                        ag.VendaId = venda.Id;
                    }
                }

                if (item.AssinaturaId.HasValue)
                {
                    db.ConsumosPlano.Add(new ConsumoPlano
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
            var produto = await db.Produtos.FindAsync(item.ProdutoId!.Value);

            string nomeProduto = produto!.Nome;
            if (item.VariacaoId.HasValue)
            {
                var variacao = await db.ProdutoVariacoes.FindAsync(item.VariacaoId.Value);
                if (variacao != null)
                {
                    var partes = new[] { variacao.Tamanho, variacao.Cor }
                        .Where(s => !string.IsNullOrWhiteSpace(s));
                    var label = string.Join(" / ", partes);
                    if (!string.IsNullOrEmpty(label))
                        nomeProduto = $"{produto.Nome} ({label})";

                    variacao.Estoque -= (int)item.Quantidade;
                    variacao.AtualizadoEm = DateTime.UtcNow;
                }
            }
            else
            {
                produto.Estoque -= item.Quantidade;
                produto.AtualizadoEm = DateTime.UtcNow;
            }

            db.ItensVenda.Add(new ItemVenda
            {
                VendaId = venda.Id,
                ProdutoId = item.ProdutoId,
                NomeProduto = nomeProduto,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                Subtotal = item.Quantidade * item.PrecoUnitario,
            });

            db.Movimentos.Add(new MovimentoEstoque
            {
                ProdutoId = item.ProdutoId!.Value,
                Tipo = "saida",
                Quantidade = item.Quantidade,
                Observacao = $"Venda #{venda.Id.ToString()[..8]} - {nomeProduto}",
                LojaId = lojaId,
            });
        }

        // Desconta crédito usado do cliente
        if (req.CreditoUsado.HasValue && req.CreditoUsado > 0 && req.ClienteId.HasValue)
        {
            var clienteVenda = await db.Clientes.FindAsync(req.ClienteId.Value);
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
                .Sum(i => i.Quantidade * i.PrecoUnitario);

            if (baseComissao > 0)
            {
                var valorComissaoVenda = Math.Round(baseComissao * (funcionario.ComissaoVendasPercentual.Value / 100m), 2);
                db.ComissoesFuncionario.Add(new ComissaoFuncionario
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

        await db.SaveChangesAsync();

        var vendaSalva = await db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstAsync(v => v.Id == venda.Id);

        return CreatedAtAction(nameof(Buscar), new { id = venda.Id }, ToDto(vendaSalva));
    }

    // ── Excluir venda (só do mês atual, com estorno de estoque/agendamento/plano/crédito) ──
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var venda = await db.Vendas
            .Include(v => v.Itens)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venda is null || (lojaId.HasValue && venda.LojaId != lojaId)) return NotFound();

        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        if (venda.CriadaEm < inicioMes || venda.CriadaEm.Date > hoje)
            return BadRequest(new { erro = "Só é possível excluir vendas do mês atual." });

        var temNfceAutorizada = await db.EmissoesFiscais
            .AnyAsync(e => e.VendaId == venda.Id && e.Status == "autorizada");
        if (temNfceAutorizada)
            return BadRequest(new { erro = "Não é possível excluir: já existe uma nota fiscal autorizada para esta venda. Cancele a nota antes." });

        // Comissão gerada por essa venda: se já foi paga num fechamento, não dá
        // pra excluir sem bagunçar o fechamento — se ainda tá pendente, estorna junto.
        var comissaoVenda = await db.ComissoesFuncionario
            .FirstOrDefaultAsync(c => c.OrigemTipo == "venda" && c.OrigemId == venda.Id);
        if (comissaoVenda != null)
        {
            if (comissaoVenda.Status != "pendente")
                return BadRequest(new { erro = "Não é possível excluir: a comissão desta venda já foi paga." });
            db.ComissoesFuncionario.Remove(comissaoVenda);
        }

        // Estorna estoque. Observação: o ItemVenda não guarda qual variação
        // (tamanho/cor) foi vendida quando o produto tem grade — só o produto
        // pai. Por isso, pra produtos com grade, o estorno soma no total do
        // produto, mas não sabe em qual variação específica devolver.
        foreach (var item in venda.Itens.Where(i => i.ProdutoId.HasValue))
        {
            var produto = await db.Produtos.FindAsync(item.ProdutoId!.Value);
            if (produto is null) continue;

            produto.Estoque += item.Quantidade;
            produto.AtualizadoEm = DateTime.UtcNow;

            db.Movimentos.Add(new MovimentoEstoque
            {
                ProdutoId = produto.Id,
                Tipo = "entrada",
                Quantidade = item.Quantidade,
                Observacao = $"Estorno da venda #{venda.Id.ToString()[..8]} (excluída) - {item.NomeProduto}",
                LojaId = lojaId,
            });
        }

        // Reverte agendamentos marcados como pagos por essa venda
        var agendamentos = await db.Agendamentos.Where(a => a.VendaId == venda.Id).ToListAsync();
        foreach (var ag in agendamentos)
        {
            ag.Pago = false;
            ag.VendaId = null;
        }

        // Remove consumos de plano gerados por essa venda (devolve o uso do plano)
        await db.ConsumosPlano.Where(c => c.VendaId == venda.Id).ExecuteDeleteAsync();

        // Devolve crédito de loja usado pelo cliente nessa venda
        if (venda.CreditoUsado.HasValue && venda.CreditoUsado.Value > 0 && venda.ClienteId.HasValue)
        {
            var cliente = await db.Clientes.FindAsync(venda.ClienteId.Value);
            if (cliente != null) cliente.CreditoLoja += venda.CreditoUsado.Value;
        }

        db.ItensVenda.RemoveRange(venda.Itens);
        db.Vendas.Remove(venda);

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Venda excluída. Estoque, agendamento e crédito foram estornados." });
    }

    private static VendaDto ToDto(Venda v) => new(
        v.Id, v.ClienteId, v.Cliente?.Nome,
        v.Total, v.Desconto, v.TotalFinal,
        v.FormaPagamento, v.FormasPagamento,
        v.Troco, v.CriadaEm,
        v.Itens.Select(i => new ItemVendaDto(
            i.Id, i.ProdutoId, i.NomeProduto,
            i.Quantidade, i.PrecoUnitario, i.Subtotal,
            i.ServicoId
        )).ToList(),
        v.OrigemNome,
        v.CreditoUsado,
        v.FuncionarioId,
        v.NomeFuncionario
    );
}