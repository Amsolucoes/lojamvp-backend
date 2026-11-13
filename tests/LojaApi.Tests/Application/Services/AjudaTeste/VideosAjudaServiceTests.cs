using LojaApi.Application.Ajuda;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using LojaApi.Tests.Suporte.Falsos;

namespace LojaApi.Tests.Application.Services.AjudaTeste;

// Service dos vídeos da Central de Ajuda, testado com repositório em memória (sem banco, sem HTTP).
public class VideosAjudaServiceTests
{
    private readonly RepositorioAjudaFalso _repo = new();
    private VideosAjudaService Servico() => new(_repo);

    private static SalvarVideoRequest Pedido(string titulo = "Como vender", string categoria = "Caixa", string youtubeId = "abc123", int ordem = 1, bool ativo = true) =>
        new(titulo, categoria, youtubeId, ordem, ativo);

    [Theory]
    [InlineData(" ", "Caixa", "abc")]
    [InlineData("Título", "  ", "abc")]
    [InlineData("Título", "Caixa", "")]
    public async Task Criar_exige_titulo_categoria_e_id_e_nao_grava_nada(string titulo, string categoria, string id)
    {
        var r = await Servico().CriarAsync(Pedido(titulo, categoria, id));

        Assert.Equal(TipoResultado.Invalido, r.Tipo);
        Assert.Equal("Preencha título, categoria e ID do YouTube.", r.Mensagem);
        Assert.Equal(0, _repo.Salvamentos);
        Assert.Empty(_repo.Videos);
    }

    [Fact]
    public async Task Criar_tira_espacos_e_extrai_o_id_do_link()
    {
        var r = await Servico().CriarAsync(Pedido("  Como vender  ", " Caixa ", "  https://youtu.be/xyz789?t=5  "));

        Assert.True(r.Sucesso);
        var video = Assert.Single(_repo.Videos);
        Assert.Equal(("Como vender", "Caixa", "xyz789"), (video.Titulo, video.Categoria, video.YoutubeId));
        Assert.Equal(1, _repo.Salvamentos);
    }

    [Fact]
    public async Task Atualizar_troca_os_dados_e_devolve_nao_encontrado_para_id_desconhecido()
    {
        var existente = new VideoAjuda { Titulo = "Velho", Categoria = "A", YoutubeId = "old", Ordem = 1, Ativo = true };
        _repo.Videos.Add(existente);

        var r = await Servico().AtualizarAsync(existente.Id, Pedido("Novo", "B", "https://www.youtube.com/watch?v=nnn", 7, false));

        Assert.True(r.Sucesso);
        Assert.Equal(("Novo", "B", "nnn", 7, false), (existente.Titulo, existente.Categoria, existente.YoutubeId, existente.Ordem, existente.Ativo));
        Assert.Equal(TipoResultado.NaoEncontrado, (await Servico().AtualizarAsync(Guid.NewGuid(), Pedido())).Tipo);
    }

    [Fact]
    public async Task Alternar_ativo_inverte_e_grava()
    {
        var video = new VideoAjuda { Titulo = "V", Categoria = "A", YoutubeId = "x", Ativo = true };
        _repo.Videos.Add(video);

        await Servico().AlternarAtivoAsync(video.Id);
        Assert.False(video.Ativo);
        await Servico().AlternarAtivoAsync(video.Id);
        Assert.True(video.Ativo);
        Assert.Equal(2, _repo.Salvamentos);
        Assert.Equal(TipoResultado.NaoEncontrado, (await Servico().AlternarAtivoAsync(Guid.NewGuid())).Tipo);
    }

    [Fact]
    public async Task Excluir_remove_o_video_e_da_nao_encontrado_na_segunda_vez()
    {
        var video = new VideoAjuda { Titulo = "V", Categoria = "A", YoutubeId = "x" };
        _repo.Videos.Add(video);

        Assert.True((await Servico().ExcluirAsync(video.Id)).Sucesso);
        Assert.Empty(_repo.Videos);
        Assert.Equal(TipoResultado.NaoEncontrado, (await Servico().ExcluirAsync(video.Id)).Tipo);
    }
}
