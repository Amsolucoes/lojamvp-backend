using LojaApi.Domain.Ajuda;

namespace LojaApi.Tests.Domain.AggregatesModel.AjudaTeste;

public class YoutubeIdTests
{
    [Theory]
    [InlineData("dQw4w9WgXcQ", "dQw4w9WgXcQ")]                                                   // já é o ID
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?feature=share&v=ABC123xyz", "ABC123xyz")]          // v= não é o primeiro parâmetro
    [InlineData("https://www.youtube.com/watch?v=ABC&t=30s", "ABC")]
    [InlineData("https://youtu.be/xyz789", "xyz789")]
    [InlineData("https://youtu.be/xyz789?t=10", "xyz789")]
    [InlineData("https://www.youtube.com/shorts/SHORT1", "SHORT1")]
    [InlineData("https://www.youtube.com/shorts/SHORT1?feature=share", "SHORT1")]
    public void Extrai_o_id_dos_formatos_de_link(string entrada, string esperado) =>
        Assert.Equal(esperado, YoutubeId.Extrair(entrada));

    [Theory]
    [InlineData("https://youtube.com/x?a=1")]    // sem parâmetro v
    [InlineData("youtube.com/watch?v=semhttp")]  // sem esquema: não é URL válida
    [InlineData("https://www.youtube.com/")]
    public void O_que_nao_e_reconhecido_volta_do_jeito_que_veio(string entrada) =>
        Assert.Equal(entrada, YoutubeId.Extrair(entrada));

    [Fact]
    public void Texto_sem_youtube_e_tratado_como_id() => Assert.Equal("qualquer-coisa", YoutubeId.Extrair("qualquer-coisa"));
}
