using LojaApi.Domain.Bonificacoes;

namespace LojaApi.Application.Bonificacoes;

// Acesso aos dados das bonificações. Os handlers decidem as regras; aqui só se lê e grava.
// Implementado em Infrastructure/Bonificacoes (EF Core). O estoque do produto vem do IProdutosRepository; os dois usam o mesmo AppDbContext,
// então um SalvarAsync grava tudo junto (brinde, baixa e movimento de estoque na mesma transação).
public interface IBonificacoesRepository
{
    Task<string?> ObterNomeDoClienteAsync(Guid clienteId, Guid lojaId);            // nulo = não existe ou é de outra loja

    Task<Bonificacao?> ObterAsync(Guid id, Guid lojaId);
    Task<List<Bonificacao>> ListarAsync(Guid lojaId, Guid? clienteId, Guid? produtoId, DateTime? de, DateTime? ate, bool incluirEstornadas);   // da mais nova para a mais antiga
    void Adicionar(Bonificacao bonificacao);

    Task SalvarAsync();
}
