namespace LojaApi.Tests.Suporte;

// Teste de integração: sobe a API de verdade e usa um PostgreSQL real.
// Só roda se a variável LOJA_TESTES_DB apontar para um servidor (veja tests/README.md); sem ela, fica "ignorado".
public sealed class FatoIntegracaoAttribute : FactAttribute
{
    public FatoIntegracaoAttribute()
    {
        if (!ApiFixture.Disponivel)
            Skip = $"Defina {ApiFixture.VariavelDoBanco} para rodar os testes de integração (veja tests/README.md).";
    }
}

public sealed class TeoriaIntegracaoAttribute : TheoryAttribute
{
    public TeoriaIntegracaoAttribute()
    {
        if (!ApiFixture.Disponivel)
            Skip = $"Defina {ApiFixture.VariavelDoBanco} para rodar os testes de integração (veja tests/README.md).";
    }
}
