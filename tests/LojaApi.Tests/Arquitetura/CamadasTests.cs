using System.Reflection;
using LojaApi.Data;
using LojaApi.Shared.Application;

namespace LojaApi.Tests.Arquitetura;

// Guardas da arquitetura em camadas: o que não pode depender de quê.
public class CamadasTests
{
    // Módulos que já separam "regra de negócio" (service) de "consulta ao banco" (repository)
    private static readonly string[] ModulosComRepositorio = ["Ajuda", "Clientes", "Fiscal", "Turmas", "Corretora", "Equipe", "Acessorios", "Vendas"];

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

    private static IEnumerable<(Type Handler, Type Contrato)> Handlers() =>
        Application.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) || i.GetGenericTypeDefinition() == typeof(IQueryHandler<>)))
                .Select(i => (t, i)));

    [Fact]
    public void Handlers_seguem_a_convencao_de_nome_e_cada_um_cuida_de_um_unico_caso_de_uso()
    {
        var problemas = new List<string>();
        foreach (var grupo in Handlers().GroupBy(h => h.Handler))
        {
            var (handler, contrato) = (grupo.Key, grupo.Single().Contrato);   // Single: um handler não implementa dois contratos
            var comando = contrato.GetGenericTypeDefinition() == typeof(ICommandHandler<>);
            var mensagem = contrato.GetGenericArguments()[0];

            var sufixo = comando ? "Command" : "Query";
            if (!mensagem.Name.EndsWith(sufixo)) problemas.Add($"{mensagem.Name} deveria terminar em {sufixo}");
            if (handler.Name != mensagem.Name + "Handler") problemas.Add($"{handler.Name} deveria se chamar {mensagem.Name}Handler");
            if (handler.Namespace != mensagem.Namespace) problemas.Add($"{handler.Name} e {mensagem.Name} deveriam estar no mesmo módulo");
        }

        Assert.True(problemas.Count == 0, string.Join("; ", problemas));
        Assert.NotEmpty(Handlers());
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
