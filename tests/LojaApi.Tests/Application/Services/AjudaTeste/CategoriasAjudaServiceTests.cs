using LojaApi.Application.Ajuda;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using LojaApi.Tests.Suporte.Falsos;

namespace LojaApi.Tests.Application.Services.AjudaTeste;

// Service das categorias da Central de Ajuda, testado com repositório em memória (sem banco, sem HTTP).
public class CategoriasAjudaServiceTests
{
    private readonly RepositorioAjudaFalso _repo = new();
    private CategoriasAjudaService Servico() => new(_repo);

    private static SalvarCategoriaRequest Pedido(string nome = "Caixa", int ordem = 1, bool ativa = true) => new(nome, ordem, ativa);

    [Fact]
    public async Task Criar_exige_nome_e_nao_repete()
    {
        var vazio = await Servico().CriarAsync(Pedido("  "));
        Assert.Equal((TipoResultado.Invalido, "Informe o nome da categoria."), (vazio.Tipo, vazio.Mensagem));

        Assert.True((await Servico().CriarAsync(Pedido("Caixa"))).Sucesso);
        var repetido = await Servico().CriarAsync(Pedido("  Caixa "));   // compara já sem os espaços das pontas
        Assert.Equal((TipoResultado.Invalido, "Já existe uma categoria com esse nome."), (repetido.Tipo, repetido.Mensagem));
        Assert.Single(_repo.Categorias);
    }

    [Fact]
    public async Task Renomear_leva_os_videos_junto()
    {
        var categoria = new CategoriaVideoAjuda { Nome = "Antigo", Ativa = true };
        _repo.Categorias.Add(categoria);
        var video = new VideoAjuda { Titulo = "V", Categoria = "Antigo", YoutubeId = "x" };
        var deOutra = new VideoAjuda { Titulo = "W", Categoria = "Outra", YoutubeId = "y" };
        _repo.Videos.AddRange([video, deOutra]);

        var r = await Servico().AtualizarAsync(categoria.Id, Pedido("  Novo  ", 3));

        Assert.True(r.Sucesso);
        Assert.Equal(("Novo", 3), (categoria.Nome, categoria.Ordem));
        Assert.Equal("Novo", video.Categoria);
        Assert.Equal("Outra", deOutra.Categoria);
    }

    [Fact]
    public async Task Manter_o_nome_nao_mexe_nos_videos_e_grava_so_uma_vez()
    {
        var categoria = new CategoriaVideoAjuda { Nome = "Caixa", Ativa = true };
        _repo.Categorias.Add(categoria);

        await Servico().AtualizarAsync(categoria.Id, Pedido("Caixa", 9));

        Assert.Equal(1, _repo.Salvamentos);   // sem o segundo "salvar" da troca de nome
    }

    [Fact]
    public async Task Excluir_categoria_em_uso_so_desativa()
    {
        var categoria = new CategoriaVideoAjuda { Nome = "Caixa", Ativa = true };
        _repo.Categorias.Add(categoria);
        _repo.Videos.Add(new VideoAjuda { Titulo = "V", Categoria = "Caixa", YoutubeId = "x" });

        var r = await Servico().ExcluirAsync(categoria.Id);

        Assert.True(r.Sucesso);
        Assert.False(categoria.Ativa);
        Assert.Contains(categoria, _repo.Categorias);
    }

    [Fact]
    public async Task Excluir_categoria_sem_uso_remove()
    {
        var categoria = new CategoriaVideoAjuda { Nome = "Livre", Ativa = true };
        _repo.Categorias.Add(categoria);

        Assert.True((await Servico().ExcluirAsync(categoria.Id)).Sucesso);
        Assert.Empty(_repo.Categorias);
        Assert.Equal(TipoResultado.NaoEncontrado, (await Servico().ExcluirAsync(categoria.Id)).Tipo);
    }
}
