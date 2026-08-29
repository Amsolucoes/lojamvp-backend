using LojaApi.Data;
using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using LojaApi.src.Models.OrdemServico;
using LojaApi.src.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LojaApi.src.Controllers.OrdemServico;

[ApiController]
[Route("api/ordemservico")]
[Authorize]
public class OrdemServicoController(AppDbContext db, OrdemServicoNotificacaoService notificacao) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja.FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    private async Task<Guid> ObterOuCriarCategoriaOSAsync(Guid lojaId)
    {
        var categoria = await db.CategoriasFinanceiras
            .FirstOrDefaultAsync(c => c.LojaId == lojaId && c.Nome == "Ordem de Serviço");
        if (categoria != null) return categoria.Id;

        categoria = new CategoriaFinanceira
        {
            LojaId = lojaId,
            Nome = "Ordem de Serviço",
            Tipo = "receber",
            Icone = "🔧",
        };
        db.CategoriasFinanceiras.Add(categoria);
        await db.SaveChangesAsync();
        return categoria.Id;
    }

    // ══════════════ Checklist — categorias e itens ══════════════

    [HttpGet("checklist-categorias")]
    public async Task<IActionResult> ListarChecklistCategorias()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.ChecklistCategorias
            .Where(c => c.LojaId == lojaId)
            .Include(c => c.Itens)
            .OrderBy(c => c.Ordem)
            .Select(c => new
            {
                c.Id,
                c.Nome,
                c.Ordem,
                c.Ativa,
                itens = c.Itens.OrderBy(i => i.Ordem).Select(i => new { i.Id, i.Nome, i.Ordem, i.Ativo }),
            })
            .ToListAsync();

        return Ok(lista);
    }

    public record SalvarChecklistCategoriaRequest(string Nome, int Ordem, bool Ativa);

    [HttpPost("checklist-categorias")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> CriarChecklistCategoria([FromBody] SalvarChecklistCategoriaRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var categoria = new ChecklistCategoria
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Ordem = req.Ordem,
            Ativa = req.Ativa,
        };
        db.ChecklistCategorias.Add(categoria);
        await db.SaveChangesAsync();

        return Ok(new { categoria.Id, categoria.Nome, categoria.Ordem, categoria.Ativa });
    }

    [HttpPut("checklist-categorias/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AtualizarChecklistCategoria(Guid id, [FromBody] SalvarChecklistCategoriaRequest req)
    {
        var lojaId = await GetLojaId();
        var categoria = await db.ChecklistCategorias.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (categoria is null) return NotFound();

        categoria.Nome = req.Nome.Trim();
        categoria.Ordem = req.Ordem;
        categoria.Ativa = req.Ativa;
        await db.SaveChangesAsync();

        return Ok(new { categoria.Id, categoria.Nome, categoria.Ordem, categoria.Ativa });
    }

    [HttpDelete("checklist-categorias/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> ExcluirChecklistCategoria(Guid id)
    {
        var lojaId = await GetLojaId();
        var categoria = await db.ChecklistCategorias.Include(c => c.Itens).FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (categoria is null) return NotFound();

        var itemIds = categoria.Itens.Select(i => i.Id).ToList();
        var emUso = await db.ChecklistRespostasItem.AnyAsync(r => itemIds.Contains(r.ChecklistItemId));
        if (emUso)
            return BadRequest(new { erro = "Não é possível excluir: esta categoria tem itens já usados em orçamentos. Desative os itens em vez de excluir." });

        db.ChecklistCategorias.Remove(categoria); // cascade remove os itens junto
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Categoria excluída." });
    }

    public record SalvarChecklistItemRequest(Guid CategoriaId, string Nome, int Ordem, bool Ativo);

    [HttpPost("checklist-itens")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> CriarChecklistItem([FromBody] SalvarChecklistItemRequest req)
    {
        var lojaId = await GetLojaId();
        var categoria = await db.ChecklistCategorias.FirstOrDefaultAsync(c => c.Id == req.CategoriaId && c.LojaId == lojaId);
        if (categoria is null) return BadRequest(new { erro = "Categoria de checklist não encontrada." });

        var item = new ChecklistItem
        {
            LojaId = lojaId!.Value,
            CategoriaId = categoria.Id,
            Nome = req.Nome.Trim(),
            Ordem = req.Ordem,
            Ativo = req.Ativo,
        };
        db.ChecklistItens.Add(item);
        await db.SaveChangesAsync();

        return Ok(new { item.Id, item.CategoriaId, item.Nome, item.Ordem, item.Ativo });
    }

    [HttpPut("checklist-itens/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AtualizarChecklistItem(Guid id, [FromBody] SalvarChecklistItemRequest req)
    {
        var lojaId = await GetLojaId();
        var item = await db.ChecklistItens.FirstOrDefaultAsync(i => i.Id == id && i.LojaId == lojaId);
        if (item is null) return NotFound();

        item.Nome = req.Nome.Trim();
        item.Ordem = req.Ordem;
        item.Ativo = req.Ativo;
        await db.SaveChangesAsync();

        return Ok(new { item.Id, item.CategoriaId, item.Nome, item.Ordem, item.Ativo });
    }

    [HttpDelete("checklist-itens/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> ExcluirChecklistItem(Guid id)
    {
        var lojaId = await GetLojaId();
        var item = await db.ChecklistItens.FirstOrDefaultAsync(i => i.Id == id && i.LojaId == lojaId);
        if (item is null) return NotFound();

        var emUso = await db.ChecklistRespostasItem.AnyAsync(r => r.ChecklistItemId == id);
        if (emUso)
        {
            item.Ativo = false;
            await db.SaveChangesAsync();
            return Ok(new { mensagem = "Item em uso em orçamentos — foi desativado em vez de excluído." });
        }

        db.ChecklistItens.Remove(item);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Item excluído." });
    }

    // ══════════════ Orçamento / Ordem de Serviço ══════════════

    [HttpGet("orcamentos")]
    public async Task<IActionResult> Listar([FromQuery] string? status, [FromQuery] string? placa)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var q = db.OrcamentosServico.Where(o => o.LojaId == lojaId);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(placa))
        {
            var placaBusca = placa.Trim().ToUpperInvariant();
            q = q.Where(o => o.Placa != null && o.Placa.Contains(placaBusca));
        }

        var lista = await q
            .OrderByDescending(o => o.CriadoEm)
            .Select(o => new
            {
                o.Id,
                o.ClienteId,
                o.VeiculoDescricao,
                o.Placa,
                o.Status,
                o.ValorTotal,
                o.CriadoEm,
                o.AprovadoEm,
                o.ConcluidoEm,
                qtdMecanicos = o.Mecanicos.Count,
                nomesMecanicos = o.Mecanicos.Select(m => m.Profissional!.Nome).ToList(),
            })
            .ToListAsync();

        return Ok(lista);
    }

    [HttpGet("orcamentos/{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id)
    {
        var lojaId = await GetLojaId();
        var o = await db.OrcamentosServico
            .Include(x => x.Itens)
            .Include(x => x.Mecanicos).ThenInclude(m => m.Profissional)
            .Include(x => x.ChecklistRespostas).ThenInclude(r => r.ChecklistItem)
            .FirstOrDefaultAsync(x => x.Id == id && x.LojaId == lojaId);

        if (o is null) return NotFound();

        return Ok(new
        {
            o.Id,
            o.ClienteId,
            o.VeiculoDescricao,
            o.Placa,
            o.Status,
            o.Observacoes,
            o.ValorTotal,
            o.CriadoEm,
            o.AprovadoEm,
            o.ConcluidoEm,
            itens = o.Itens.Select(i => new { i.Id, i.Tipo, i.ProdutoId, i.Descricao, i.Quantidade, i.ValorUnitario, i.ValorTotal }),
            mecanicos = o.Mecanicos.Select(m => new { m.Id, m.ProfissionalId, NomeProfissional = m.Profissional!.Nome, m.ComissaoPercentual }),
            checklist = o.ChecklistRespostas.Select(r => new { r.Id, r.ChecklistItemId, NomeItem = r.ChecklistItem!.Nome, r.Estado, r.Observacao }),
        });
    }

    // ── Histórico de ordens de serviço de um cliente (usado na tela de Clientes) ──
    [HttpGet("orcamentos/cliente/{clienteId:guid}")]
    public async Task<IActionResult> ListarPorCliente(Guid clienteId)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.OrcamentosServico
            .Where(o => o.LojaId == lojaId && o.ClienteId == clienteId)
            .Include(o => o.Itens)
            .OrderByDescending(o => o.CriadoEm)
            .Select(o => new
            {
                o.Id,
                o.VeiculoDescricao,
                o.Placa,
                o.Status,
                o.ValorTotal,
                o.CriadoEm,
                o.ConcluidoEm,
                itens = o.Itens.Select(i => new { i.Descricao, i.Quantidade, i.ValorTotal }),
            })
            .ToListAsync();

        return Ok(lista);
    }

    // ── Resumo agregado (qtd + total) de ordens CONCLUÍDAS por cliente — usado
    // nos cards da lista de Clientes, pra sinalizar rapidamente quem já teve OS.
    // Conta só concluídas (mesmo critério de "vendas" já ser sempre finalizada).
    [HttpGet("resumo-clientes")]
    public async Task<IActionResult> ResumoClientes()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var resumo = await db.OrcamentosServico
            .Where(o => o.LojaId == lojaId && o.Status != "cancelado")
            .GroupBy(o => o.ClienteId)
            .Select(g => new { ClienteId = g.Key, Qtd = g.Count(), Total = g.Sum(o => o.ValorTotal) })
            .ToListAsync();

        return Ok(resumo);
    }

    public record ItemOrcamentoRequest(string Tipo, Guid? ProdutoId, string Descricao, int Quantidade, decimal ValorUnitario);
    public record MecanicoOrcamentoRequest(Guid ProfissionalId, decimal ComissaoPercentual);
    public record ChecklistRespostaRequest(Guid ChecklistItemId, string Estado, string? Observacao);

    public record CriarOrcamentoRequest(
        Guid ClienteId,
        string? VeiculoDescricao,
        string? Placa,
        string? Observacoes,
        List<ItemOrcamentoRequest> Itens,
        List<MecanicoOrcamentoRequest> Mecanicos,
        List<ChecklistRespostaRequest>? ChecklistRespostas
    );

    [HttpPost("orcamentos")]
    public async Task<IActionResult> Criar([FromBody] CriarOrcamentoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (req.Itens is null || req.Itens.Count == 0)
            return BadRequest(new { erro = "O orçamento precisa ter ao menos um item." });

        var cliente = await db.Clientes.FindAsync(req.ClienteId);
        if (cliente is null) return BadRequest(new { erro = "Cliente não encontrado." });

        var orcamento = new OrcamentoServico
        {
            LojaId = lojaId.Value,
            ClienteId = req.ClienteId,
            VeiculoDescricao = req.VeiculoDescricao,
            Placa = string.IsNullOrWhiteSpace(req.Placa) ? null : req.Placa.Trim().ToUpperInvariant(),
            Observacoes = req.Observacoes,
            Status = "pendente",
        };

        decimal valorTotal = 0;
        foreach (var i in req.Itens)
        {
            if (i.Tipo != "peca" && i.Tipo != "servico")
                return BadRequest(new { erro = "Tipo de item inválido — use 'peca' ou 'servico'." });

            var subtotal = i.Quantidade * i.ValorUnitario;
            valorTotal += subtotal;

            orcamento.Itens.Add(new ItemOrcamentoServico
            {
                LojaId = lojaId.Value,
                Tipo = i.Tipo,
                ProdutoId = i.ProdutoId,
                Descricao = i.Descricao.Trim(),
                Quantidade = i.Quantidade,
                ValorUnitario = i.ValorUnitario,
                ValorTotal = subtotal,
            });
        }
        orcamento.ValorTotal = valorTotal;

        if (req.Mecanicos != null)
        {
            foreach (var m in req.Mecanicos)
            {
                var profissional = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == m.ProfissionalId && p.LojaId == lojaId);
                if (profissional is null) return BadRequest(new { erro = "Mecânico/profissional não encontrado." });

                orcamento.Mecanicos.Add(new MecanicoOrcamento
                {
                    LojaId = lojaId.Value,
                    ProfissionalId = profissional.Id,
                    ComissaoPercentual = m.ComissaoPercentual > 0 ? m.ComissaoPercentual : (profissional.ComissaoPadraoPercentual ?? 0),
                });
            }
        }

        if (req.ChecklistRespostas != null)
        {
            foreach (var r in req.ChecklistRespostas)
            {
                orcamento.ChecklistRespostas.Add(new ChecklistRespostaItem
                {
                    LojaId = lojaId.Value,
                    ChecklistItemId = r.ChecklistItemId,
                    Estado = r.Estado,
                    Observacao = r.Observacao,
                });
            }
        }

        db.OrcamentosServico.Add(orcamento);
        await db.SaveChangesAsync();

        return Ok(new { orcamento.Id, orcamento.Status, orcamento.ValorTotal });
    }

    // ── Editar (só pendente ou em_andamento — antes de concluir) ────
    [HttpPut("orcamentos/{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] CriarOrcamentoRequest req)
    {
        var lojaId = await GetLojaId();
        var orcamento = await db.OrcamentosServico
            .Include(o => o.Itens)
            .Include(o => o.Mecanicos)
            .Include(o => o.ChecklistRespostas)
            .FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);

        if (orcamento is null) return NotFound();

        if (orcamento.Status != "pendente" && orcamento.Status != "em_andamento")
            return BadRequest(new { erro = "Só é possível editar uma ordem pendente ou em andamento. Ordens concluídas já geraram financeiro, comissão e baixa de estoque." });

        if (req.Itens is null || req.Itens.Count == 0)
            return BadRequest(new { erro = "O orçamento precisa ter ao menos um item." });

        var cliente = await db.Clientes.FindAsync(req.ClienteId);
        if (cliente is null) return BadRequest(new { erro = "Cliente não encontrado." });

        orcamento.ClienteId = req.ClienteId;
        orcamento.VeiculoDescricao = req.VeiculoDescricao;
        orcamento.Placa = string.IsNullOrWhiteSpace(req.Placa) ? null : req.Placa.Trim().ToUpperInvariant();
        orcamento.Observacoes = req.Observacoes;

        // Remove itens/mecânicos/checklist antigos e recria do zero — seguro aqui
        // porque a ordem ainda não gerou nenhum efeito colateral (estoque/financeiro/comissão)
        db.ItensOrcamentoServico.RemoveRange(orcamento.Itens);
        db.MecanicosOrcamento.RemoveRange(orcamento.Mecanicos);
        db.ChecklistRespostasItem.RemoveRange(orcamento.ChecklistRespostas);

        decimal valorTotal = 0;
        foreach (var i in req.Itens)
        {
            if (i.Tipo != "peca" && i.Tipo != "servico")
                return BadRequest(new { erro = "Tipo de item inválido — use 'peca' ou 'servico'." });

            var subtotal = i.Quantidade * i.ValorUnitario;
            valorTotal += subtotal;

            db.ItensOrcamentoServico.Add(new ItemOrcamentoServico
            {
                LojaId = lojaId!.Value,
                OrcamentoId = orcamento.Id,
                Tipo = i.Tipo,
                ProdutoId = i.ProdutoId,
                Descricao = i.Descricao.Trim(),
                Quantidade = i.Quantidade,
                ValorUnitario = i.ValorUnitario,
                ValorTotal = subtotal,
            });
        }
        orcamento.ValorTotal = valorTotal;

        if (req.Mecanicos != null)
        {
            foreach (var m in req.Mecanicos)
            {
                var profissional = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == m.ProfissionalId && p.LojaId == lojaId);
                if (profissional is null) return BadRequest(new { erro = "Mecânico/profissional não encontrado." });

                db.MecanicosOrcamento.Add(new MecanicoOrcamento
                {
                    LojaId = lojaId.Value,
                    OrcamentoId = orcamento.Id,
                    ProfissionalId = profissional.Id,
                    ComissaoPercentual = m.ComissaoPercentual > 0 ? m.ComissaoPercentual : (profissional.ComissaoPadraoPercentual ?? 0),
                });
            }
        }

        if (req.ChecklistRespostas != null)
        {
            foreach (var r in req.ChecklistRespostas)
            {
                db.ChecklistRespostasItem.Add(new ChecklistRespostaItem
                {
                    LojaId = lojaId.Value,
                    OrcamentoId = orcamento.Id,
                    ChecklistItemId = r.ChecklistItemId,
                    Estado = r.Estado,
                    Observacao = r.Observacao,
                });
            }
        }

        await db.SaveChangesAsync();

        return Ok(new { orcamento.Id, orcamento.Status, orcamento.ValorTotal });
    }

    // ── Aprovar (pendente → em_andamento) ──────────────────────────
    [HttpPatch("orcamentos/{id:guid}/aprovar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Aprovar(Guid id)
    {
        var lojaId = await GetLojaId();
        var orcamento = await db.OrcamentosServico.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);
        if (orcamento is null) return NotFound();

        if (orcamento.Status != "pendente")
            return BadRequest(new { erro = "Só é possível aprovar um orçamento pendente." });

        orcamento.Status = "em_andamento";
        orcamento.AprovadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { orcamento.Id, orcamento.Status });
    }

    // ── Reprovar / Cancelar ─────────────────────────────────────────
    [HttpPatch("orcamentos/{id:guid}/cancelar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        var lojaId = await GetLojaId();
        var orcamento = await db.OrcamentosServico.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);
        if (orcamento is null) return NotFound();

        if (orcamento.Status == "concluido")
            return BadRequest(new { erro = "Não é possível cancelar uma ordem já concluída." });

        orcamento.Status = "cancelado";
        await db.SaveChangesAsync();

        return Ok(new { orcamento.Id, orcamento.Status });
    }

    // ── Desfazer conclusão — reverte estoque, financeiro e comissão ──
    [HttpPatch("orcamentos/{id:guid}/desfazer-conclusao")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> DesfazerConclusao(Guid id)
    {
        var lojaId = await GetLojaId();
        var orcamento = await db.OrcamentosServico
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);

        if (orcamento is null) return NotFound();

        if (orcamento.Status != "concluido")
            return BadRequest(new { erro = "Só é possível desfazer uma ordem concluída." });

        // Trava de segurança: se o lançamento financeiro já foi pago, ou alguma comissão
        // já foi paga, não desfaz sozinho — o dinheiro pode já ter saído/entrado de verdade.
        if (orcamento.LancamentoFinanceiroId.HasValue)
        {
            var lancamento = await db.LancamentosFinanceiros.FindAsync(orcamento.LancamentoFinanceiroId.Value);
            if (lancamento != null && lancamento.Status == "pago")
                return BadRequest(new { erro = "O lançamento financeiro desta ordem já foi marcado como pago. Reverta o pagamento no Financeiro antes de desfazer a conclusão." });
        }

        var comissoes = await db.ComissoesFuncionario
            .Where(c => c.OrigemTipo == "ordem_servico" && c.OrigemId == orcamento.Id)
            .ToListAsync();

        if (comissoes.Any(c => c.Status == "pago"))
            return BadRequest(new { erro = "Uma ou mais comissões geradas por esta ordem já foram pagas. Reverta o pagamento em Comissões antes de desfazer a conclusão." });

        // 1) Repõe o estoque das peças que vieram do estoque
        foreach (var item in orcamento.Itens.Where(i => i.Tipo == "peca" && i.ProdutoId.HasValue))
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
                Observacao = $"Estorno — desfeita conclusão da Ordem de Serviço #{orcamento.Id.ToString()[..8]} - {item.Descricao}",
                LojaId = lojaId,
            });
        }

        // 2) Remove o lançamento financeiro (ainda pendente, já validado acima)
        if (orcamento.LancamentoFinanceiroId.HasValue)
        {
            var lancamento = await db.LancamentosFinanceiros.FindAsync(orcamento.LancamentoFinanceiroId.Value);
            if (lancamento != null) db.LancamentosFinanceiros.Remove(lancamento);
            orcamento.LancamentoFinanceiroId = null;
        }

        // 3) Remove as comissões geradas (ainda pendentes, já validado acima)
        db.ComissoesFuncionario.RemoveRange(comissoes);

        // 4) Volta status — fica em_andamento, pronta pra editar e concluir de novo
        orcamento.Status = "em_andamento";
        orcamento.ConcluidoEm = null;

        await db.SaveChangesAsync();

        return Ok(new { orcamento.Id, orcamento.Status, mensagem = "Conclusão desfeita — estoque, financeiro e comissão foram revertidos." });
    }

    // ── Reabrir uma ordem cancelada — volta pra "em andamento" ─────
    [HttpPatch("orcamentos/{id:guid}/reabrir")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Reabrir(Guid id)
    {
        var lojaId = await GetLojaId();
        var orcamento = await db.OrcamentosServico.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);
        if (orcamento is null) return NotFound();

        if (orcamento.Status != "cancelado")
            return BadRequest(new { erro = "Só é possível reabrir uma ordem cancelada." });

        // Volta direto pra em_andamento — ela já tinha sido aprovada antes de ser cancelada,
        // então não faz sentido pedir aprovação de novo.
        orcamento.Status = "em_andamento";
        await db.SaveChangesAsync();

        return Ok(new { orcamento.Id, orcamento.Status });
    }

    // ── Concluir: gera Financeiro (a receber) + comissão por mecânico + baixa estoque ──
    public record ConcluirOrcamentoRequest(Guid ContaBancariaId, DateTime? Vencimento);

    [HttpPatch("orcamentos/{id:guid}/concluir")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Concluir(Guid id, [FromBody] ConcluirOrcamentoRequest req)
    {
        var lojaId = await GetLojaId();
        var orcamento = await db.OrcamentosServico
            .Include(o => o.Itens)
            .Include(o => o.Mecanicos).ThenInclude(m => m.Profissional)
            .FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);

        if (orcamento is null) return NotFound();

        if (orcamento.Status != "em_andamento")
            return BadRequest(new { erro = "Só é possível concluir uma ordem em andamento (aprovada)." });

        var conta = await db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == req.ContaBancariaId && c.LojaId == lojaId);
        if (conta is null) return BadRequest(new { erro = "Conta bancária não encontrada." });

        // ── Valida e baixa estoque das peças que vieram do estoque ──
        foreach (var item in orcamento.Itens.Where(i => i.Tipo == "peca" && i.ProdutoId.HasValue))
        {
            var produto = await db.Produtos.FindAsync(item.ProdutoId!.Value);
            if (produto is null) return BadRequest(new { erro = $"Produto do item '{item.Descricao}' não encontrado." });
            if (produto.Estoque < item.Quantidade)
                return BadRequest(new { erro = $"Estoque insuficiente para '{produto.Nome}' — disponível: {produto.Estoque:0.###}." });
        }

        foreach (var item in orcamento.Itens.Where(i => i.Tipo == "peca" && i.ProdutoId.HasValue))
        {
            var produto = await db.Produtos.FindAsync(item.ProdutoId!.Value);
            produto!.Estoque -= item.Quantidade;
            produto.AtualizadoEm = DateTime.UtcNow;

            db.Movimentos.Add(new MovimentoEstoque
            {
                ProdutoId = produto.Id,
                Tipo = "saida",
                Quantidade = item.Quantidade,
                Observacao = $"Ordem de Serviço #{orcamento.Id.ToString()[..8]} - {item.Descricao}",
                LojaId = lojaId,
            });
        }

        // ── Gera o lançamento a receber no Financeiro ──
        var categoriaId = await ObterOuCriarCategoriaOSAsync(lojaId!.Value);
        var vencimento = req.Vencimento.HasValue
            ? DateTime.SpecifyKind(req.Vencimento.Value.Date, DateTimeKind.Utc).AddHours(12)
            : DateTime.UtcNow;
        var lancamento = new LancamentoFinanceiro
        {
            LojaId = lojaId.Value,
            ContaBancariaId = conta.Id,
            Tipo = "receber",
            Modo = "avulsa",
            Descricao = $"Ordem de Serviço — {orcamento.VeiculoDescricao ?? orcamento.Id.ToString()[..8]}",
            CategoriaId = categoriaId,
            Valor = orcamento.ValorTotal,
            Vencimento = vencimento,
        };
        db.LancamentosFinanceiros.Add(lancamento);

        orcamento.LancamentoFinanceiroId = lancamento.Id;

        // ── Gera a comissão de cada mecânico vinculado ──
        // Base de cálculo depende do cadastro do funcionário: "total" (peça+serviço) ou
        // "servico" (só mão de obra — ignora o valor das peças usadas na ordem).
        var valorSomenteServicos = orcamento.Itens.Where(i => i.Tipo == "servico").Sum(i => i.ValorTotal);

        foreach (var mecanico in orcamento.Mecanicos)
        {
            if (mecanico.ComissaoPercentual <= 0) continue;

            var baseCalculo = mecanico.Profissional?.ComissaoBaseCalculo == "servico"
                ? valorSomenteServicos
                : orcamento.ValorTotal;

            var valorComissao = Math.Round(baseCalculo * (mecanico.ComissaoPercentual / 100m), 2);
            db.ComissoesFuncionario.Add(new ComissaoFuncionario
            {
                LojaId = lojaId.Value,
                ProfissionalId = mecanico.ProfissionalId,
                OrigemTipo = "ordem_servico",
                OrigemId = orcamento.Id,
                ValorServico = baseCalculo,
                ComissaoPercentual = mecanico.ComissaoPercentual,
                ValorComissao = valorComissao,
            });
        }

        orcamento.Status = "concluido";
        orcamento.ConcluidoEm = DateTime.UtcNow;

        // Uma única gravação no final — ou tudo (baixa de estoque + financeiro + comissão
        // + status) é salvo junto, ou nada é, evitando ficar com efeito parcial se algo falhar no meio.
        await db.SaveChangesAsync();

        return Ok(new { orcamento.Id, orcamento.Status, orcamento.LancamentoFinanceiroId });
    }

    // ── Enviar orçamento por e-mail pro cliente ────────────────────
    [HttpPost("orcamentos/{id:guid}/enviar-email")]
    public async Task<IActionResult> EnviarEmail(Guid id)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var resultado = await notificacao.EnviarPorEmailAsync(id, lojaId.Value);
        if (!resultado.Enviado)
            return BadRequest(new { erro = resultado.Erro });

        return Ok(new { mensagem = "Orçamento enviado por e-mail." });
    }

    // ── Ajustar manualmente a data/hora de entrada (aprovação) e saída (conclusão) ──
    public record AjustarDatasRequest(DateTime? AprovadoEm, DateTime? ConcluidoEm);

    [HttpPatch("orcamentos/{id:guid}/datas")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AjustarDatas(Guid id, [FromBody] AjustarDatasRequest req)
    {
        var lojaId = await GetLojaId();
        var orcamento = await db.OrcamentosServico.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);
        if (orcamento is null) return NotFound();

        if (orcamento.Status == "pendente" || orcamento.Status == "cancelado")
            return BadRequest(new { erro = "Só é possível ajustar entrada/saída de uma ordem aprovada ou concluída." });

        DateTime? aprovadoEm = req.AprovadoEm.HasValue
            ? DateTime.SpecifyKind(req.AprovadoEm.Value, DateTimeKind.Utc)
            : orcamento.AprovadoEm;

        DateTime? concluidoEm = req.ConcluidoEm.HasValue
            ? DateTime.SpecifyKind(req.ConcluidoEm.Value, DateTimeKind.Utc)
            : orcamento.ConcluidoEm;

        if (aprovadoEm.HasValue && concluidoEm.HasValue && concluidoEm.Value < aprovadoEm.Value)
            return BadRequest(new { erro = "A data/hora de saída não pode ser antes da entrada." });

        orcamento.AprovadoEm = aprovadoEm;
        orcamento.ConcluidoEm = concluidoEm;
        await db.SaveChangesAsync();

        return Ok(new { orcamento.Id, orcamento.AprovadoEm, orcamento.ConcluidoEm });
    }

    // ── Excluir (só se ainda pendente) ─────────────────────────────
    [HttpDelete("orcamentos/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var orcamento = await db.OrcamentosServico.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);
        if (orcamento is null) return NotFound();

        if (orcamento.Status != "pendente")
            return BadRequest(new { erro = "Só é possível excluir um orçamento ainda pendente. Cancele em vez de excluir." });

        db.OrcamentosServico.Remove(orcamento);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Orçamento excluído." });
    }
}