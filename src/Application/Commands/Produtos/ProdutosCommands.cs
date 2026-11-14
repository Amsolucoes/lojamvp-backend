namespace LojaApi.Application.Produtos;

// Comandos de Produtos: o que muda dados (cada um tem o seu handler em Application/Handlers/Produtos).

// Cadastro
public record CriarProdutoCommand(Guid? LojaId, SalvarProdutoRequest Req);
public record AtualizarProdutoCommand(Guid? LojaId, Guid Id, SalvarProdutoRequest Req);
public record AtualizarPrecosEmLoteCommand(Guid? LojaId, AtualizarPrecosEmLoteRequest Req);
public record AtualizarStatusEmLoteCommand(Guid? LojaId, AtualizarStatusEmLoteRequest Req);
public record ExcluirProdutoCommand(Guid? LojaId, Guid Id);
public record AdicionarVariacaoCommand(Guid? LojaId, Guid ProdutoId, SalvarVariacaoRequest Req);
public record AtualizarVariacaoCommand(Guid? LojaId, Guid ProdutoId, Guid VariacaoId, SalvarVariacaoRequest Req);
public record RemoverVariacaoCommand(Guid? LojaId, Guid ProdutoId, Guid VariacaoId);

// Estoque
public record AjusteEstoqueCommand(Guid? LojaId, AjusteEstoqueRequest Req);
public record AjusteVariacaoCommand(Guid? LojaId, AjusteVariacaoRequest Req);

// Categorias e marcas
public record CriarCategoriaCommand(Guid? LojaId, CriarCategoriaRequest Req);
public record AtualizarCategoriaCommand(Guid? LojaId, Guid Id, CriarCategoriaRequest Req);
public record ExcluirCategoriaCommand(Guid? LojaId, Guid Id);
public record CriarMarcaCommand(Guid? LojaId, CriarMarcaRequest Req);
public record AtualizarMarcaCommand(Guid? LojaId, Guid Id, CriarMarcaRequest Req);
public record ExcluirMarcaCommand(Guid? LojaId, Guid Id);

// Fornecedores
public record CriarFornecedorCommand(Guid? LojaId, SalvarFornecedorRequest Req);
public record AtualizarFornecedorCommand(Guid? LojaId, Guid Id, SalvarFornecedorRequest Req);
public record ExcluirFornecedorCommand(Guid? LojaId, Guid Id);

// Modelos de etiqueta (listar cria o modelo "Padrão" na primeira vez, por isso é um comando)
public record ListarModelosEtiquetaCommand(Guid? LojaId);
public record CriarModeloEtiquetaCommand(Guid? LojaId, SalvarModeloRequest Req);
public record AtualizarModeloEtiquetaCommand(Guid? LojaId, Guid Id, SalvarModeloRequest Req);
public record ExcluirModeloEtiquetaCommand(Guid? LojaId, Guid Id);
public record MarcarModeloPadraoCommand(Guid? LojaId, Guid Id);
