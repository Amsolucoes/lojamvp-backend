using LojaApi.src.Models.OrdemServico;

namespace LojaApi.Application.OrdemServico;

// Porta: gera os PDFs do patrimônio (cadastro atual e contagem). Implementada em Infrastructure/OrdemServico (QuestPDF).
public interface IRelatorioPatrimonioPdf
{
    byte[] GerarCadastro(string? nomeLoja, IReadOnlyList<ItemPatrimonio> itens);
    byte[] GerarContagem(string? nomeLoja, ContagemPatrimonio contagem);
}
