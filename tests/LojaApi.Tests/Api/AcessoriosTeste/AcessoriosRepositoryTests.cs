using LojaApi.Infrastructure.Acessorios;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.AcessoriosTeste;

// Consultas do repository que a API de fábrica não consegue exercitar (a tabela de categorias nunca está vazia).
[Collection(ColecaoApi.Nome)]
public class AcessoriosRepositoryTests(ApiFixture api)
{
    [FatoIntegracao]
    public async Task Maior_ordem_de_categoria_e_menos_um_quando_nao_ha_nenhuma_e_o_maximo_quando_ha()
    {
        await api.NoBancoAsync(async db =>
        {
            await using var transacao = await db.Database.BeginTransactionAsync();
            var repositorio = new AcessoriosRepository(db);

            var comAsDeFabrica = await repositorio.MaiorOrdemDeCategoriaAsync();
            await db.CategoriasAcessorio.ExecuteDeleteAsync();
            var vazia = await repositorio.MaiorOrdemDeCategoriaAsync();

            await transacao.RollbackAsync();      // não deixa a tabela vazia para os outros testes
            Assert.True(comAsDeFabrica >= 3);     // as 4 categorias de fábrica têm ordem 0 a 3
            Assert.Equal(-1, vazia);
        });
    }
}
