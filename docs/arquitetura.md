# Arquitetura do backend

O backend é um **monólito modular**: um único projeto/deploy, organizado em **módulos de negócio**, e cada módulo
em **camadas** (inspiradas em DDD). A meta é que qualquer regra de negócio tenha um lugar óbvio para morar, e que
os controllers sejam só a "porta de entrada" HTTP.

## Estrutura

As pastas são **por camada** e, dentro de cada camada, **uma subpasta por módulo de negócio**:

```
src/
  Controllers/<Modulo>/      <- SÓ controllers (HTTP). Ex.: Controllers/Chacara/ReservaChacaraController.cs
  Application/<Modulo>/      <- casos de uso (o que o sistema FAZ), um serviço por assunto
  Domain/<Modulo>/           <- o coração: entidades e regras de negócio (sem banco, sem HTTP)
  Infrastructure/<Modulo>/   <- detalhes técnicos (PDF, rotinas em segundo plano) e o registro do módulo (<Modulo>Module.cs)
  Shared/                    <- usado por todos os módulos: Resultado<T>, ILojaAtual, ParaResposta()
  Data/                      <- AppDbContext e Migrations
  Services, Models, DTOs     <- ainda compartilhados (código dos módulos ainda não migrados)
```

Namespaces seguem as pastas: `LojaApi.Controllers.<Modulo>`, `LojaApi.Application.<Modulo>`, `LojaApi.Domain.<Modulo>`,
`LojaApi.Infrastructure.<Modulo>`.

Hoje só **Chacara** e **Carteira** têm as quatro camadas. Os demais módulos têm só `Controllers/<Modulo>` (a lógica ainda está
dentro do controller) e, ao serem migrados, ganham `Application/<Modulo>` e `Domain/<Modulo>`.

> **Cuidado com `Domain`:** o EF Core percorre as entidades em ordem alfabética do nome completo e o nome padrão das chaves
> estrangeiras depende disso. Por isso o `AppDbContext` fixa, para toda entidade em `LojaApi.Domain.*`, o nome da FK como
> `f_k_<tabela>_<tabela principal>_<colunas>` (igual ao que já está no banco). Ao criar um módulo novo, confira com
> `dotnet ef dbcontext script` que o DDL não mudou.

## O que vai em cada camada

| Camada | O que tem | Pode depender de | Não pode |
|---|---|---|---|
| **Domain** | Entidades com comportamento (`Reserva.Confirmar`, `ConfiguracaoPrecoChacara.ParcelasMaximas`), constantes de status, cálculos puros (`CalculadoraPrecoChacara`), regras da agenda | só do .NET | banco, HTTP, e-mail, qualquer serviço externo |
| **Application** | Casos de uso: carregam do banco, chamam as regras do Domain, salvam, devolvem `Resultado<T>` | Domain, `AppDbContext`, Infrastructure | `ControllerBase`, `IActionResult`, qualquer coisa de HTTP |
| **Infrastructure** | Integrações e detalhes técnicos: PDF (QuestPDF), rotinas (`BackgroundService`) | Domain | — |
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

// Application/ReservasChacaraService.cs
var erro = reserva.Confirmar(req?.ValorPago, DateTime.UtcNow);
if (erro != null) return Resultado.Invalido(erro);
```

- Os métodos de domínio devolvem a **mensagem de erro** (ou `null` se deu certo).
- Use **métodos**, não propriedades, para valores calculados: as entidades são devolvidas direto no JSON de
  algumas rotas, e uma propriedade nova mudaria a resposta da API.
- Consultas reaproveitáveis ficam em `RegrasAgenda` como `Expression<Func<...>>`, para valer também no banco.

## Como criar um novo módulo

1. Crie `src/Domain/<Modulo>`, `src/Application/<Modulo>` e `src/Infrastructure/<Modulo>` (os controllers já estão em `src/Controllers/<Modulo>`).
2. Mova as entidades para `src/Domain/<Modulo>` (namespace `LojaApi.Domain.<Modulo>`) e atualize os nomes no
   `AppDbContextModelSnapshot.cs`. **Não** altere tabelas/colunas.
3. Para cada controller: crie o serviço de aplicação com a lógica (copie, não reescreva) e deixe o controller fino.
4. Crie `<Modulo>Module.cs` com `AddXModule()` e chame no `Program.cs`.
5. Escreva os testes do módulo (unitários do `Domain` + integração pela API) seguindo `tests/README.md`.
6. Se for uma refatoração, prove que nada mudou com `tools/regressao` (e confira rotas e modelo do banco; veja o README de lá).

## Testes

`tests/LojaApi.Tests` tem testes **unitários** (regras do Domain, sem banco) e de **integração** (API de verdade +
PostgreSQL, serviços externos trocados por dublês). Como rodar e como escrever: `tests/README.md`.
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
| **Fiscal** | EmissaoFiscal | só `Controllers` |
| **Produtos** | Produtos, Estoque, Categorias, Marcas, Fornecedores, NfImportacao, Etiquetas | só `Controllers` |
| **Clientes** | Clientes (cadastro de clientes da loja) | só `Controllers` |
| **Servicos** | Servicos, Agendamentos, Planos, Publico (agendamento online) | só `Controllers` |
| **Equipe** | Funcionarios, Comissoes | só `Controllers` |
| **Corretora** | Corretora | só `Controllers` |
| **Turmas** | Turmas | só `Controllers` |
| **OrdemServico** | OrdemServico, Patrimonio | só `Controllers` |
| **Acessorios** | PedidosAcessorio, ProdutosAcessorio (venda de acessórios da plataforma) | só `Controllers` |
| **Plataforma** | Admin, Auth, Cliente (área do assinante), Loja, Perfis, ModulosPreco, Pagamentos (assinatura), Push | só `Controllers` |
| **Ajuda** | VideoAjuda, CategoriasVideoAjuda | só `Controllers` |

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
