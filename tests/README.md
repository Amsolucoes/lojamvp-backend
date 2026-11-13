# Testes do backend

Projeto `tests/LojaApi.Tests` (xUnit). Hoje cobre os módulos **Chácara**, **Carteira**, **Serviços/Agenda**, **Ajuda**, **Clientes**, **Fiscal**, **Turmas**, **Corretora** e **Equipe**; os próximos módulos migrados ganham a
sua pasta `<Modulo>Teste` em cada camada (veja "Como as pastas são organizadas").

## No Visual Studio

Abra **`tests/LojaApi.Tests.sln`** (API + testes juntos): os testes aparecem no *Gerenciador de Testes* e dá para rodar
e depurar um por um. A solução da raiz (`LojaApi.sln`) continua só com a API, de propósito: é ela que o Dockerfile
compila no deploy, e assim o deploy não puxa pacotes de teste. Para os testes de integração, defina a variável
`LOJA_TESTES_DB` (veja abaixo) antes de abrir o Visual Studio (Configurações do sistema → Variáveis de ambiente).

## Como rodar

```bash
# só os testes unitários (não precisam de banco)
dotnet test tests/LojaApi.Tests

# unitários + integração: precisa de um PostgreSQL (qualquer um, local ou docker)
export LOJA_TESTES_DB='Host=localhost;Port=5432;Username=postgres;Password=SUA_SENHA'
dotnet test tests/LojaApi.Tests
```

- Sem `LOJA_TESTES_DB`, os testes de integração aparecem como **ignorados** (não falham).
- Com a variável, os testes criam um banco novo (`loja_teste_xxxx`), rodam as migrations, e **apagam o banco no fim**.
  Use um servidor de desenvolvimento, nunca o de produção. A variável não leva `Database=`.
- Docker, se preferir: `docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:16`.

## Como as pastas são organizadas

As pastas **espelham os projetos do `src`**, e dentro de cada uma há uma subpasta por módulo com o sufixo `Teste`
(`AjudaTeste`, `ClientesTeste`...). O namespace segue a pasta (`LojaApi.Tests.<Camada>.<Modulo>Teste`).

```
tests/LojaApi.Tests/
  Domain/Services/<Modulo>Teste/   regras puras (unitários, rápidos, sem banco)   ex.: Domain/Services/AjudaTeste/YoutubeIdTests.cs
  Domain/Entities/<Modulo>Teste/           comportamento das entidades (Reserva, configurações da chácara)
  Application/Services/<Modulo>Teste/   SERVICES com repository falso (unitários, sem banco)   ex.: VideosAjudaServiceTests.cs
  Application/Shared/                   Resultado<T> -> 200/400/404/409/500
  Api/<Modulo>Teste/                    integração pelo HTTP: API de verdade + PostgreSQL   ex.: Api/AjudaTeste/AjudaApiTests.cs
  Arquitetura/                          guardas das camadas
  Suporte/                              ApiFixture, ClienteApi, Cenarios*, Datas, FatoIntegracao e falsos (MercadoPagoFalso, ProvedorFiscalFalso)
  Suporte/Falsos/                       repositórios e portas em memória (RepositorioAjudaFalso, RepositorioEquipeFalso, LancamentoFixoFinanceiroFalso)
```
`Suporte` é um `using` global do projeto de testes. Ao testar um módulo novo, crie a pasta `<Modulo>Teste` em cada camada que ele precisar
(só as que tiver teste; não precisa criar pasta vazia).

## O que existe

| Pasta | Tipo | O que prova |
|---|---|---|
| `Domain/*Teste` | **Unitários**, rápidos, sem banco | Regras de negócio puras: estados da reserva, cálculo de preço, taxa do cartão, agenda, limite e recebimento da carteira, horários livres e conflito da agenda, comissão, regras do plano e da mensalidade, link do YouTube, fiscal, turmas, corretora |
| `Application/Services/AjudaTeste`, `Application/Services/EquipeTeste` | **Unitários** (repository falso) | As regras dos services sem banco nem HTTP: validações, extrair o id do link, renomear categoria, excluir em uso; na Equipe, salário fixo no Financeiro (porta falsa), fechar/desfazer comissões, conta de outra loja |
| `Application/Shared` | Unitários | `Resultado<T>` → 200/400/404/409/500 |
| `Arquitetura` | Unitários | Guardas das camadas: service sem `AppDbContext` nos módulos com repository, Domain sem EF/ASP.NET, Application sem a API |
| `Api/*Teste` | **Integração**, API de verdade + PostgreSQL | Fluxos completos pelo HTTP, com login (JWT), banco real e migrations |

Integração (`Api/`):

- `ChacaraTeste/` – `ConfiguracaoChacaraApiTests` (dados, preços, faixas, horários, períodos, fotos, Mercado Pago), `ReservasChacaraApiTests` (painel do dono: reserva manual, confirmar, pagamentos, negociação, expirar/reativar, editar, contrato PDF, dashboard), `SitePublicoChacaraApiTests` (site do cliente: disponibilidade, valor, reservar, cancelar, avaliar), `PagamentoChacaraApiTests` (Pix, cartão e combinado), `RotinasChacaraApiTests` (expiração automática e aviso de 24h)
- `IsolamentoTeste/` – `IsolamentoEntreLojasApiTests`: uma loja nunca vê nem altera dados de outra (multi-loja)
- `ServicosTeste/` – `ServicosCatalogoApiTests` (cadastro de serviços), `AgendamentosApiTests` (agenda, status, comissão, regras do plano), `PlanosApiTests` (planos, assinantes, mensalidade), `AgendamentoOnlinePublicoApiTests` (página pública)
- `ClientesTeste/` – `ClientesApiTests`: CPF único por loja, busca, total de compras e saldo da carteira, excluir só admin e sem saldo
- `FiscalTeste/` – `FiscalApiTests`: configuração, emitir/listar/cancelar NFC-e com provedor falso
- `AjudaTeste/` – `AjudaApiTests`: leitura pública, gestão só do superadmin, link do YouTube, renomear/excluir categoria
- `TurmasTeste/` – `TurmasApiTests`: turmas, matrícula, sessões geradas, falta/remarcação, presença, profissionais
- `CorretoraTeste/` – `CorretoraApiTests`: seguradoras, funil, comissão no financeiro, apólices, resumo e dashboard
- `EquipeTeste/` – `FuncionariosApiTests` (cadastro, remuneração, salário fixo → lançamento fixo no Financeiro, endereço, comissão por serviço) e `ComissoesApiTests` (resumo, detalhe por origem, fechar/pagar, histórico, desfazer)
- `CarteiraTeste/` – `CarteiraApiTests`: fiado — venda no caixa (inteira ou dividida), limite, recebimento, extrato, excluir venda/cliente

## Como os testes funcionam por dentro

- Para testar outros papéis (superadmin, operador) use `api.SuperadminAsync(loja)` / `api.OperadorAsync(loja)` / `api.ComPapelAsync(loja, "admin")` (`CenariosPapeis`).
- `ApiFixture` sobe a API em memória (`WebApplicationFactory`) uma única vez, com um banco novo.
- Cada teste chama `api.NovaLojaAsync()` (ou `PrepararParaReservasAsync()`): cria **a sua própria loja** (cadastro + login).
  Por isso os testes não dependem da ordem nem se atrapalham.
- Serviços externos são trocados por dublês, então **nada sai para a internet**:
  - e-mail (Resend): `Substitute.For<IResend>()` — dá para conferir quais e-mails foram disparados;
  - Mercado Pago: `MercadoPagoFalso` — o teste decide se o Pix foi aprovado, se o cartão foi recusado etc.
    (`MercadoPagoService` tem quatro métodos `virtual` justamente para permitir isso).
- As rotinas em segundo plano ficam desligadas; os testes chamam o método da rotina direto (`ExpirarReservasVencidasAsync`).
- As datas dos testes são calculadas a partir de hoje (`Datas.Sexta()`), então os testes não "envelhecem".

## Escrevendo um teste novo

```csharp
[Collection(ColecaoApi.Nome)]
public class MeuModuloApiTests(ApiFixture api)
{
    [FatoIntegracao]
    public async Task Exemplo()
    {
        var loja = await api.PrepararParaReservasAsync();           // loja nova, já configurada
        var r = await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado());
        Assert.Equal(200, r.Codigo);
        Assert.Equal("confirmada", r.Json.GetProperty("status").GetString());
    }
}
```

Regra de ouro: um teste novo deve **falhar** se você estragar a regra que ele protege. Antes de confiar nele,
quebre a regra de propósito no código e veja o teste ficar vermelho.

## Fora do alcance

- Chamadas reais ao Mercado Pago e ao Resend (os dublês cobrem o comportamento da *nossa* parte).
- O envio de WhatsApp e a tela (frontend).
- Carga/performance.

A ferramenta `tools/regressao` é outra coisa: compara o comportamento de **duas versões** da API (antes x depois de
uma refatoração).
