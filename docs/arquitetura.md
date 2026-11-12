# Arquitetura do backend

O backend é um **monólito modular**: um único projeto/deploy, organizado em **módulos de negócio**, e cada módulo
em **camadas** (inspiradas em DDD). A meta é que qualquer regra de negócio tenha um lugar óbvio para morar, e que
os controllers sejam só a "porta de entrada" HTTP.

## Estrutura

```
src/
  Modules/
    Chacara/                      <- um módulo de negócio = uma pasta
      ChacaraModule.cs            <- registra os serviços do módulo (Program.cs só chama AddChacaraModule())
      Domain/                     <- o coração: entidades e regras de negócio
      Application/                <- casos de uso (o que o sistema FAZ), um serviço por assunto
      Infrastructure/             <- coisas técnicas do módulo: PDF do contrato, rotina em segundo plano
      Api/                        <- controllers finos (HTTP)
  Shared/                         <- usado por todos os módulos
    Application/                  <- Resultado<T>, ILojaAtual (loja do usuário logado)
    Api/                          <- ParaResposta(): traduz Resultado em 200/400/404/409/500
  Data/                           <- AppDbContext e Migrations
  Controllers, Services, Models   <- módulos ainda NÃO migrados (ver "Estado atual")
```

## O que vai em cada camada

| Camada | O que tem | Pode depender de | Não pode |
|---|---|---|---|
| **Domain** | Entidades com comportamento (`Reserva.Confirmar`, `ConfiguracaoPrecoChacara.ParcelasMaximas`), constantes de status, cálculos puros (`CalculadoraPrecoChacara`), regras da agenda | só do .NET | banco, HTTP, e-mail, qualquer serviço externo |
| **Application** | Casos de uso: carregam do banco, chamam as regras do Domain, salvam, devolvem `Resultado<T>` | Domain, `AppDbContext`, Infrastructure | `ControllerBase`, `IActionResult`, qualquer coisa de HTTP |
| **Infrastructure** | Integrações e detalhes técnicos: PDF (QuestPDF), rotinas (`BackgroundService`) | Domain | — |
| **Api** | Controllers: leem a requisição, descobrem a loja (`ILojaAtual`), chamam **um** caso de uso, devolvem `ParaResposta(this)` | Application | regra de negócio e acesso direto ao banco |

Regra prática: *se você está escrevendo um `if` de negócio dentro de um controller, ele está no lugar errado.*

## Como uma regra é escrita

Em vez de o controller mudar campos da entidade, a **entidade** decide:

```csharp
// Domain/Reserva.cs
public string? Confirmar(decimal? valorPago, DateTime agora)
{
    if (Status == StatusReserva.Confirmada) return "Esta reserva já está confirmada.";
    ValorPago = valorPago ?? Valor;
    Status = StatusReserva.Confirmada;
    DataConfirmacao = agora;
    return null;
}

// Application/ReservasChacaraService.cs
var erro = reserva.Confirmar(req?.ValorPago, DateTime.UtcNow);
if (erro != null) return Resultado.Invalido(erro);
```

- Os métodos de domínio devolvem a **mensagem de erro** (ou `null` se deu certo).
- Use **métodos**, não propriedades, para valores calculados: as entidades são devolvidas direto no JSON de
  algumas rotas, e uma propriedade nova mudaria a resposta da API.
- Consultas reaproveitáveis ficam em `RegrasAgenda` como `Expression<Func<...>>`, para valer também no banco.

## Como criar um novo módulo

1. Crie `src/Modules/<Modulo>/{Domain,Application,Infrastructure,Api}`.
2. Mova as entidades para `Domain` (namespace `LojaApi.Modules.<Modulo>.Domain`) e atualize os nomes no
   `AppDbContextModelSnapshot.cs`. **Não** altere tabelas/colunas.
3. Para cada controller: crie o serviço de aplicação com a lógica (copie, não reescreva) e deixe o controller fino.
4. Crie `<Modulo>Module.cs` com `AddXModule()` e chame no `Program.cs`.
5. Escreva os testes do módulo (unitários do `Domain` + integração pela API) seguindo `tests/README.md`.
6. Se for uma refatoração, prove que nada mudou com `tools/regressao` (e confira rotas e modelo do banco; veja o README de lá).

## Testes

`tests/LojaApi.Tests` tem testes **unitários** (regras do Domain, sem banco) e de **integração** (API de verdade +
PostgreSQL, serviços externos trocados por dublês). Como rodar e como escrever: `tests/README.md`.
Cada módulo migrado deve vir com os seus testes.

## Estado atual

- **Migrado:** Chácara (reservas, site público, pagamento da reserva, preços, horários, períodos especiais,
  fotos, dados da chácara, painel) e **Carteira do cliente** ("fiado": compra agora, paga depois), ambos **com testes**.
  A Carteira se liga ao caixa pelo `VendasController` (`CarteiraService`) sem que o resto de Vendas precise ser migrado.
- **Ainda no formato antigo** (controllers com a lógica dentro): Financeiro (o maior, ~3.300 linhas), Notas
  fiscais (importação), Administração, Planos/assinantes, Ordem de serviço, Produtos acessório, Corretora,
  Funcionários, Vendas, Turmas, Produtos, Clientes e demais.

## Próximos passos sugeridos

1. Migrar os módulos um a um (um PR por módulo), do menor para o maior, terminando pelo Financeiro.
2. Quando todos estiverem migrados, separar em projetos (`Domain`, `Application`, `Infrastructure`, `Api`),
   o que passa a ser mecânico e faz o compilador **impedir** dependências no sentido errado.
3. Trocar o uso direto do `AppDbContext` na camada Application por abstrações (repositórios ou uma interface do
   contexto) se for desejado isolar totalmente o domínio do EF Core.

## Observação sobre as migrations

O `HasData` do `AppDbContext` usa `DateTime.UtcNow` e hash de senha aleatório; por isso o EF sempre enxerga
"alterações pendentes no modelo". Isso já era assim antes da modularização e não afeta o funcionamento (as
migrations são aplicadas pelos arquivos). Vale corrigir com valores fixos antes de voltar a usar
`dotnet ef migrations add`.
