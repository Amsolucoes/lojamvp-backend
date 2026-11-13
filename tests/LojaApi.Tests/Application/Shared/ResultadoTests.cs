using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Tests.Application.Shared;

public class ResultadoTests
{
    private sealed class ControllerDeTeste : ControllerBase { }

    private static IActionResult Converter<T>(Resultado<T> r) => r.ParaResposta(new ControllerDeTeste());

    [Fact]
    public void Ok_vira_200_com_o_valor()
    {
        var res = Assert.IsType<OkObjectResult>(Converter(Resultado.Ok(new { id = 1 })));
        Assert.Equal(200, res.StatusCode);
        Assert.NotNull(res.Value);
    }

    [Fact]
    public void Invalido_vira_400_com_corpo_erro()
    {
        var res = Assert.IsType<BadRequestObjectResult>(Converter(Resultado.Invalido("faltou algo")));
        Assert.Equal(400, res.StatusCode);
        Assert.Equal("faltou algo", res.Value!.GetType().GetProperty("erro")!.GetValue(res.Value));
    }

    [Fact]
    public void NaoEncontrado_sem_mensagem_vira_404_sem_corpo()
    {
        var res = Assert.IsType<NotFoundResult>(Converter(Resultado.NaoEncontrado()));
        Assert.Equal(404, res.StatusCode);
    }

    [Fact]
    public void NaoEncontrado_com_mensagem_vira_404_com_corpo_erro()
    {
        var res = Assert.IsType<NotFoundObjectResult>(Converter(Resultado.NaoEncontrado("Reserva não encontrada.")));
        Assert.Equal("Reserva não encontrada.", res.Value!.GetType().GetProperty("erro")!.GetValue(res.Value));
    }

    [Fact]
    public void Conflito_vira_409()
    {
        var res = Assert.IsType<ConflictObjectResult>(Converter(Resultado.Conflito("datas ocupadas")));
        Assert.Equal(409, res.StatusCode);
    }

    [Fact]
    public void Falha_vira_500_com_corpo_erro()
    {
        var res = Assert.IsType<ObjectResult>(Converter(Resultado.Falha("deu ruim")));
        Assert.Equal(500, res.StatusCode);
        Assert.Equal("deu ruim", res.Value!.GetType().GetProperty("erro")!.GetValue(res.Value));
    }

    [Fact]
    public void Sucesso_so_e_verdadeiro_para_Ok()
    {
        Assert.True(Resultado<int>.Ok(1).Sucesso);
        Assert.False(Resultado<int>.Invalido("x").Sucesso);
        Assert.False(Resultado<int>.Conflito("x").Sucesso);
        Assert.False(Resultado<int>.NaoEncontrado().Sucesso);
        Assert.False(Resultado<int>.Falha("x").Sucesso);
    }
}
