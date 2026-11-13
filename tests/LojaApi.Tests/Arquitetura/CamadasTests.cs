using System.Reflection;
using LojaApi.Data;
using LojaApi.Shared.Application;

namespace LojaApi.Tests.Arquitetura;

// Guardas da arquitetura em camadas: o que não pode depender de quê.
public class CamadasTests
{
    // Módulos que já separam "regra de negócio" (service) de "consulta ao banco" (repository)
    private static readonly string[] ModulosComRepositorio = ["Ajuda", "Clientes", "Fiscal", "Turmas", "Corretora"];

    private static readonly Assembly Application = typeof(Resultado<>).Assembly;
    private static readonly Assembly Domain = typeof(LojaApi.Domain.Chacara.DiaDeReserva).Assembly;

    private static IEnumerable<Type> TiposDoModulo(string modulo) =>
        Application.GetTypes().Where(t => t.Namespace == $"LojaApi.Application.{modulo}" && !t.Name.Contains('<'));

    [Fact]
    public void Services_dos_modulos_com_repositorio_nao_acessam_o_banco_direto()
    {
        var infratores = new List<string>();
        foreach (var modulo in ModulosComRepositorio)
        foreach (var tipo in TiposDoModulo(modulo))
        {
            var usaDbContext =
                tipo.GetConstructors().Any(c => c.GetParameters().Any(p => typeof(AppDbContext).IsAssignableFrom(p.ParameterType))) ||
                tipo.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Any(f => typeof(AppDbContext).IsAssignableFrom(f.FieldType));
            if (usaDbContext) infratores.Add(tipo.FullName!);
        }

        Assert.True(infratores.Count == 0, "Estes tipos acessam o AppDbContext direto (use o repository): " + string.Join(", ", infratores));
    }

    [Fact]
    public void Cada_modulo_com_repositorio_tem_a_interface_do_repository_na_application()
    {
        foreach (var modulo in ModulosComRepositorio)
            Assert.Contains(TiposDoModulo(modulo), t => t.IsInterface && t.Name.EndsWith("Repository"));
    }

    [Fact]
    public void Domain_nao_depende_de_banco_nem_das_outras_camadas()
    {
        var referencias = Domain.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        Assert.DoesNotContain(referencias, n => n.StartsWith("Microsoft.EntityFrameworkCore") || n.StartsWith("Microsoft.AspNetCore") || n.StartsWith("LojaApi"));
    }

    [Fact]
    public void Application_nao_depende_da_api()
    {
        var referencias = Application.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        Assert.DoesNotContain("LojaApi", referencias);                                   // o projeto da API
        Assert.DoesNotContain(referencias, n => n.StartsWith("Microsoft.AspNetCore.Mvc"));
    }
}
