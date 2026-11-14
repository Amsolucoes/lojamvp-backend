namespace LojaApi.Application.OrdemServico;

// Comandos de OrdemServico: o que muda dados (cada um tem o seu handler em Application/Handlers/OrdemServico).

// Checklist
public record CriarChecklistCategoriaCommand(Guid? LojaId, SalvarChecklistCategoriaRequest Req);
public record AtualizarChecklistCategoriaCommand(Guid? LojaId, Guid Id, SalvarChecklistCategoriaRequest Req);
public record ExcluirChecklistCategoriaCommand(Guid? LojaId, Guid Id);
public record CriarChecklistItemCommand(Guid? LojaId, SalvarChecklistItemRequest Req);
public record AtualizarChecklistItemCommand(Guid? LojaId, Guid Id, SalvarChecklistItemRequest Req);
public record ExcluirChecklistItemCommand(Guid? LojaId, Guid Id);

// Orçamento / ordem de serviço
public record CriarOrcamentoCommand(Guid? LojaId, CriarOrcamentoRequest Req);
public record AtualizarOrcamentoCommand(Guid? LojaId, Guid Id, CriarOrcamentoRequest Req);
public record AprovarOrcamentoCommand(Guid? LojaId, Guid Id);
public record CancelarOrcamentoCommand(Guid? LojaId, Guid Id);
public record ReabrirOrcamentoCommand(Guid? LojaId, Guid Id);
public record ConcluirOrcamentoCommand(Guid? LojaId, Guid Id, ConcluirOrcamentoRequest Req);
public record DesfazerConclusaoOrcamentoCommand(Guid? LojaId, Guid Id);
public record AjustarDatasOrcamentoCommand(Guid? LojaId, Guid Id, AjustarDatasRequest Req);
public record ExcluirOrcamentoCommand(Guid? LojaId, Guid Id);
public record EnviarOrcamentoPorEmailCommand(Guid? LojaId, Guid Id);

// Patrimônio
public record CriarItemPatrimonioCommand(Guid? LojaId, SalvarItemPatrimonioRequest Req);
public record AtualizarItemPatrimonioCommand(Guid? LojaId, Guid Id, SalvarItemPatrimonioRequest Req);
public record ExcluirItemPatrimonioCommand(Guid? LojaId, Guid Id);
public record CriarContagemPatrimonioCommand(Guid? LojaId, CriarContagemRequest Req);
public record ExcluirContagemPatrimonioCommand(Guid? LojaId, Guid Id);
