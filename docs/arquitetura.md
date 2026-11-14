# Arquitetura do backend

O backend é um **monólito modular**: um único projeto/deploy, organizado em **módulos de negócio**, e cada módulo
em **camadas** (inspiradas em DDD). A meta é que qualquer regra de negócio tenha um lugar óbvio para morar, e que
os controllers sejam só a "porta de entrada" HTTP.

## Estrutura

As pastas são **por camada** e, dentro de cada camada, **uma subpasta por módulo de negócio**. Três camadas são **projetos
próprios** (cada uma com o seu `.csproj`), então o compilador **impede** dependências no sentido errado:

```
src/
  Domain/            projeto LojaApi.Domain       <- só depende do .NET. AggregatesModel/<Modulo>Aggregate (uma pasta por agregado/módulo, como no modelo de domínio da Microsoft).
  Data/              projeto LojaApi.Data         <- AppDbContext, Entities (tabelas do banco) e Migrations. Depende do Domain.
  Application/       projeto LojaApi.Application  <- casos de uso (Chacara, Carteira, Servicos, Ajuda, Clientes, Fiscal, Turmas, Corretora, Equipe)
                                                     + Resultado<T> e ILojaAtual (Shared).
                                                     Depende do Domain e do Data. Nunca da API.
                                                     Organizada por TIPO e, dentro dele, por módulo (veja abaixo).
  Controllers/<Modulo>/    (projeto da API: LojaApi.csproj, na raiz)  <- SÓ controllers (HTTP)
  Infrastructure/<Modulo>/ (projeto da API)  <- PDF, rotinas em segundo plano, registro do módulo e os adaptadores das portas
  Shared/Api/ , Services/ , DTOs/ , Middleware/ , Utils/   (projeto da API; Services/DTOs ainda atendem os módulos não migrados)
```

Quem depende de quem (seta = "referencia"):

```
LojaApi (API) ──> Application ──> Data ──> Domain
      │               │                       ▲
      └───────────────┴───────────────────────┘      (Domain não referencia ninguém)
```

Namespaces seguem as pastas: `LojaApi.Controllers.<Modulo>`, `LojaApi.Application.<Modulo>`, `LojaApi.Domain.<Modulo>`,
`LojaApi.Infrastructure.<Modulo>`. As entidades antigas ficam na pasta `src/Data/Entities`, mas o namespace delas continua `LojaApi.Models` / `LojaApi.src.Models` (a pasta foi renomeada, o namespace não).

### Como a pasta Application é organizada (por tipo, depois por módulo)
```
Application/
  Dtos/<Modulo>/<Modulo>Dtos.cs            <- o que a API DEVOLVE (records "…Dto"); um arquivo por módulo
  Requests/<Modulo>/<Modulo>Requests.cs    <- o que a API RECEBE no corpo (records "…Request"); um arquivo por módulo
  Interfaces/<Modulo>/I<Nome>.cs           <- repositórios (I<Modulo>Repository) e portas (IProvedorFiscal...); um arquivo por interface
  Services/<Modulo>/<Nome>Service.cs       <- casos de uso (regras de negócio); um arquivo por service (padrão antigo, ainda usado pela maioria dos módulos)
  Commands/<Modulo>/<Modulo>Commands.cs    <- o que MUDA dados (records "…Command"); só nos módulos em Command/Handler
  Queries/<Modulo>/<Modulo>Queries.cs      <- o que só LÊ (records "…Query"); só nos módulos em Command/Handler
  Handlers/<Modulo>/<Nome>Handler.cs       <- um handler por Command/Query (o caso de uso); só nos módulos em Command/Handler
  Shared/Resultado.cs                      <- Resultado<T> (+ Interfaces/Shared/ILojaAtual.cs e Services/Shared/LojaAtualService.cs)
```
- **O nome do tipo termina com o que ele é**: `ClienteDto`, `SalvarClienteRequest`, `ClientesService`, e as interfaces começam com `I`
  (`IClientesRepository`, `IPagamentoOnlineChacara`). Um resultado que uma porta devolve também é DTO (`ResultadoEmissaoFiscalDto`).
- **Namespace por módulo, não por pasta**: tudo de um módulo é `LojaApi.Application.<Modulo>`, em qualquer pasta de tipo
  (quem usa um módulo importa um namespace só). A pasta só organiza os arquivos.
- Ao criar um módulo novo, crie o arquivo dele em cada pasta de tipo (só as que ele precisar).

### Command e Handler (padrão novo: Equipe e Acessorios)
Em vez de um service com vários métodos, **cada caso de uso é um Command (ou Query) e um Handler**, sem biblioteca de mediador:

- `Commands/<Modulo>`: records que **mudam** dados (`CriarFuncionarioCommand(LojaId, Req)`); `Queries/<Modulo>`: records que só **leem** (`ResumoComissoesQuery`).
- `Handlers/<Modulo>/<Nome>Handler.cs`: implementa `ICommandHandler<T>` ou `IQueryHandler<T>` (`Interfaces/Shared/IHandlers.cs`), recebe o
  repository pelo construtor e devolve `Resultado<object>`. O nome é sempre `<Mensagem>Handler` (um teste de arquitetura confere).
- O controller recebe o handler que a ação precisa por `[FromServices]` e chama `HandleAsync(new(lojaId, ...))`.
- O registro é automático: `services.AddHandlersDoModulo("<Modulo>")` no `<Modulo>Module` acha todos os handlers do namespace.
- O que dois handlers compartilham fica em uma classe de apoio na mesma pasta (`FuncionarioApoio`). O repository continua igual.
- Consultas pesadas podem, no futuro, trocar o repository por Dapper só no lado das Queries, sem mexer nos Commands.

### Portas: como o Application fala com o mundo externo sem depender dele
O Application não pode referenciar a API. O que vem de fora (Mercado Pago, criptografia de segredos, gerador de PDF) entra por
**interface** definida no Application (`Application/Interfaces/Chacara`: `IPagamentoOnlineChacara`, `IProtetorDeSegredos`,
`IGeradorContratoChacara`) e é **implementado na API** (`Infrastructure/Chacara/AdaptadoresChacara.cs`), ligado em `ChacaraModule`.
Os testes trocam o Mercado Pago por um falso exatamente por esse caminho.

### Service x Repository (módulos novos)
Nos módulos migrados a partir de **Ajuda, Clientes, Fiscal, Turmas, Corretora e Equipe** a camada Application separa as duas coisas:

- **Service** (`Application/Services/<Modulo>/<X>Service.cs`) ou, na Equipe, **Handler** (`Application/Handlers/<Modulo>`, veja "Command e Handler"): as regras de negócio e a orquestração. Devolve `Resultado<T>`. **Não usa o `AppDbContext`.**
- **Repository** (interface `I<Modulo>Repository` em `Application/Interfaces/<Modulo>`, implementação EF Core em `Infrastructure/<Modulo>`): só lê e grava,
  com métodos nomeados pelo que fazem (`ObterTurmaAsync`, `CpfJaCadastradoAsync`, `ListarApolicesAsync`...). Sem repository genérico (o EF já é
  um). Expõe `SalvarAsync()` para gravar o que o service alterou. A implementação mora na API (e não no `Data`) porque o Application já
  depende do Data: assim não há referência circular, e é o mesmo caminho das portas acima.
- **Domain** (`Domain/AggregatesModel/<Modulo>Aggregate`): como no modelo de domínio da Microsoft, uma pasta por agregado/módulo com tudo dele junto:
  as entidades com comportamento (`Reserva`), as listas fixas de valores (`StatusReserva`) e as regras e cálculos puros (`CalculadoraPrecoChacara`,
  `EquipeRegras`: calcular, validar, decidir). Sem banco; é onde ficam os testes unitários. Os namespaces continuam `LojaApi.Domain.<Modulo>`
  (a pasta só organiza; o `AppDbContext` depende desse nome). As interfaces de repositório ficam no Application (e não no Domain, como na Microsoft)
  porque devolvem DTOs e o Domain não enxerga o Application.
- **Registro**: `Infrastructure/<Modulo>/<Modulo>Module.cs` (`AddXModule()`), chamado no `Program.cs`.

Ganhos: o service pode ser testado com um repository falso (sem banco) e a regra não se mistura com a consulta. Um teste de arquitetura
(`tests/LojaApi.Tests/Arquitetura/CamadasTests.cs`) **falha** se um service desses módulos voltar a receber o `AppDbContext`, se o Domain
passar a depender de EF/ASP.NET ou se o Application passar a depender da API. Ao migrar um módulo novo, acrescente o nome dele na lista
`ModulosComRepositorio` desse teste. **Chacara, Carteira e Servicos** (migrados antes) ainda consultam o banco direto no service.

### Migrations (mudou de lugar)
As migrations moram no projeto **Data**. Comandos do EF Core agora precisam dos dois projetos:
```powershell
dotnet ef migrations add NomeDaMigration --project src\Data --startup-project .
dotnet ef database update --project src\Data --startup-project .
```
No Visual Studio (Console do Gerenciador de Pacotes): **Projeto padrão = `LojaApi.Data`** e **Projeto de inicialização = `LojaApi`**.

### Ao criar um projeto novo
Acrescente o `COPY` do `.csproj` dele no `Dockerfile` (o restore em camadas precisa), referencie-o no `LojaApi.csproj`, adicione-o
às duas soluções (`LojaApi.sln` e `tests/LojaApi.Tests.sln`) e exclua a pasta dele da compilação do `LojaApi.csproj` (`Compile Remove`).

Hoje **Chacara**, **Carteira**, **Servicos**, **Ajuda**, **Clientes**, **Fiscal**, **Turmas**, **Corretora**, **Equipe** e **Acessorios** têm as quatro camadas. Os demais módulos têm só `Controllers/<Modulo>` (a lógica ainda está
dentro do controller) e, ao serem migrados, ganham `Application/{Services,Interfaces,Dtos,Requests}/<Modulo>` e `Domain/AggregatesModel/<Modulo>Aggregate`.

> **Cuidado com `Domain`:** o EF Core percorre as entidades em ordem alfabética do nome completo e o nome padrão das chaves
> estrangeiras depende disso. Por isso o `AppDbContext` fixa, para toda entidade em `LojaApi.Domain.*`, o nome da FK como
> `f_k_<tabela>_<tabela principal>_<colunas>` (igual ao que já está no banco). Ao criar um módulo novo, confira com
> `dotnet ef dbcontext script --project src\Data --startup-project .` que o DDL não mudou.
>
> As entidades do Domain **não têm propriedade de navegação para `Loja`** (só `LojaId`): o Domain não enxerga o projeto Data.

## O que vai em cada camada

| Camada | O que tem | Pode depender de | Não pode |
|---|---|---|---|
| **Domain** | Entidades com comportamento (`Reserva.Confirmar`, `ConfiguracaoPrecoChacara.ParcelasMaximas`), constantes de status, cálculos puros (`CalculadoraPrecoChacara`), regras da agenda | só do .NET | banco, HTTP, e-mail, qualquer serviço externo |
| **Application** | Casos de uso: carregam do banco, chamam as regras do Domain, salvam, devolvem `Resultado<T>` | Domain, Data (`AppDbContext`) e as **portas** que ele mesmo define | `ControllerBase`, `IActionResult`, qualquer coisa de HTTP |
| **Infrastructure** | Integrações e detalhes técnicos: PDF (QuestPDF), rotinas (`BackgroundService`), adaptadores das portas | Domain, Application | — |
| **Controllers** | Controllers: leem a requisição, descobrem a loja (`ILojaAtual`), chamam **um** caso de uso, devolvem `ParaResposta(this)` | Application | regra de negócio e acesso direto ao banco |

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

// Application/Services/Chacara/ReservasChacaraService.cs
var erro = reserva.Confirmar(req?.ValorPago, DateTime.UtcNow);
if (erro != null) return Resultado.Invalido(erro);
```

- Os métodos de domínio devolvem a **mensagem de erro** (ou `null` se deu certo).
- Use **métodos**, não propriedades, para valores calculados: as entidades são devolvidas direto no JSON de
  algumas rotas, e uma propriedade nova mudaria a resposta da API.
- Consultas reaproveitáveis ficam em `RegrasAgenda` como `Expression<Func<...>>`, para valer também no banco.

## Como criar um novo módulo

1. Crie `src/Domain/AggregatesModel/<Modulo>Aggregate`, `src/Application/{Services,Interfaces,Dtos,Requests}/<Modulo>` e `src/Infrastructure/<Modulo>` (os controllers já estão em `src/Controllers/<Modulo>`).
2. Mova as entidades para `src/Domain/AggregatesModel/<Modulo>Aggregate` (namespace `LojaApi.Domain.<Modulo>`) e atualize os nomes no
   `AppDbContextModelSnapshot.cs`. **Não** altere tabelas/colunas.
3. Para cada controller: crie o serviço de aplicação com a lógica (copie, não reescreva) e deixe o controller fino.
4. Crie `<Modulo>Module.cs` com `AddXModule()` e chame no `Program.cs`.
5. Escreva os testes do módulo (unitários do `Domain` + integração pela API) seguindo `tests/README.md`.
6. Se for uma refatoração, prove que nada mudou com `tools/regressao` (e confira rotas e modelo do banco; veja o README de lá).

## Testes

`tests/LojaApi.Tests` tem testes **unitários** (regras do Domain e services com repository falso, sem banco) e de **integração**
(API de verdade + PostgreSQL, serviços externos trocados por dublês). As pastas espelham os projetos do `src`
(`Domain/AggregatesModel/<Modulo>Teste`, `Application/Services/<Modulo>Teste`, `Application/Handlers/<Modulo>Teste`, `Api/<Modulo>Teste`). Como rodar e como escrever: `tests/README.md`.
Cada módulo migrado deve vir com os seus testes.

## Como uma mudança chega ao ar

Veja `docs/fluxo-de-entrega.md` (branch → PR → teste local → "pode mesclar" → deploy).

## Agente do projeto

`.claude/agents/revisor-testador.md` é um agente do Claude Code que revisa e testa uma mudança antes da entrega
(suíte completa, teste de mutação com `tools/mutacao/mutar.sh`, migration x modelo, checklist de revisão) e devolve
um relatório. Ele não altera código de produção.

## Estado atual

Todo controller mora em `src/Controllers/<Modulo>`. Só alguns módulos já estão **de verdade** em camadas (Domain/Application);
nos demais, o controller ainda tem a lógica dentro (migrar é o próximo passo).

| Módulo | Controllers | Camadas |
|---|---|---|
| **Chacara** | reservas, site público, preços, horários, períodos, fotos, dados, painel | ✅ Domain/Application/Infrastructure/Controllers + testes |
| **Carteira** | carteira do cliente (fiado) | ✅ Domain/Application/Controllers + testes |
| **Financeiro** | `FinanceiroController` (+ Planejamento) — o maior, ~3.600 linhas | só `Controllers` (a migrar) |
| **Vendas** | Vendas, Trocas, MovimentosCaixa, OrigensVenda, Relatorios | só `Controllers` |
| **Fiscal** | EmissaoFiscal (NFC-e; provedor entra pela porta `IProvedorFiscal`) | ✅ Domain/Application (service + repository)/Infrastructure/Controllers + testes |
| **Produtos** | Produtos, Estoque, Categorias, Marcas, Fornecedores, NfImportacao, Etiquetas | só `Controllers` |
| **Clientes** | Clientes (cadastro de clientes da loja) | ✅ Domain/Application (service + repository)/Infrastructure/Controllers + testes |
| **Servicos** | Servicos, Agendamentos, Planos, Publico (agendamento online) | ✅ Domain/Application/Infrastructure/Controllers + testes |
| **Equipe** | Funcionarios, Comissoes (o salário fixo entra no Financeiro pela porta `ILancamentoFixoFinanceiro`) | ✅ Domain/Application (Command/Handler + repository, módulo piloto)/Infrastructure/Controllers + testes |
| **Corretora** | Corretora (seguradoras, funil, apólices) | ✅ Domain/Application (service + repository)/Infrastructure/Controllers + testes |
| **Turmas** | Turmas (matrícula, sessões, remarcação; rotina que gera as sessões) | ✅ Domain/Application (service + repository)/Infrastructure/Controllers + testes |
| **OrdemServico** | OrdemServico, Patrimonio | só `Controllers` |
| **Acessorios** | PedidosAcessorio, ProdutosAcessorio (venda de acessórios da plataforma; Mercado Pago pela porta `IPagamentoOnlineAcessorio`) | ✅ Domain/Application (Command/Handler + repository)/Infrastructure/Controllers + testes |
| **Plataforma** | Admin, Auth, Cliente (área do assinante), Loja, Perfis, ModulosPreco, Pagamentos (assinatura), Push | só `Controllers` |
| **Ajuda** | VideoAjuda, CategoriasVideoAjuda | ✅ Domain/Application (service + repository)/Infrastructure/Controllers + testes |

## Próximos passos sugeridos

1. Migrar os módulos um a um (um PR por módulo), do menor para o maior, terminando pelo Financeiro.
2. Quando todos estiverem migrados, separar em projetos (`Domain`, `Application`, `Infrastructure`, `Api`),
   o que passa a ser mecânico e faz o compilador **impedir** dependências no sentido errado.
3. Os módulos novos já usam repository (veja "Service x Repository"). Falta, se for desejado, levar o mesmo padrão a Chacara, Carteira e
   Servicos, que ainda consultam o banco direto no service.

## Observação sobre as migrations

O `HasData` do `AppDbContext` usa `DateTime.UtcNow` e hash de senha aleatório; por isso o EF sempre enxerga
"alterações pendentes no modelo". Isso já era assim antes da modularização e não afeta o funcionamento (as
migrations são aplicadas pelos arquivos). Vale corrigir com valores fixos antes de voltar a usar
`dotnet ef migrations add`.
