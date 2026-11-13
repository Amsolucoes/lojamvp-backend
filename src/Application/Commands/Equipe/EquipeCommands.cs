namespace LojaApi.Application.Equipe;

// Comandos da Equipe: o que muda dados (cada um tem o seu handler em Application/Handlers/Equipe).

public record CriarFuncionarioCommand(Guid? LojaId, SalvarFuncionarioRequest Req);
public record AtualizarFuncionarioCommand(Guid? LojaId, Guid Id, SalvarFuncionarioRequest Req);
public record AlternarAtivoFuncionarioCommand(Guid? LojaId, Guid Id);
public record ExcluirFuncionarioCommand(Guid? LojaId, Guid Id);
public record DefinirComissaoServicoCommand(Guid? LojaId, Guid Id, SalvarComissaoServicoRequest Req);
public record RemoverComissaoServicoCommand(Guid? LojaId, Guid ComissaoId);
public record FecharComissoesCommand(Guid? LojaId, FecharComissaoRequest Req);
public record DesfazerFechamentoCommand(Guid? LojaId, Guid Id);
