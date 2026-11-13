# Testes do backend

Projeto `tests/LojaApi.Tests` (xUnit). Hoje cobre os módulos **Chácara**, **Carteira**, **Serviços/Agenda**, **Ajuda**, **Clientes**, **Fiscal**, **Turmas** e **Corretora**; os próximos módulos migrados ganham a
sua pasta ao lado (`tests/LojaApi.Tests/<Modulo>/...` e `Integracao/`).

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

## O que existe

| Pasta | Tipo | O que prova |
|---|---|---|
| `Chacara/Dominio`, `Carteira/Dominio`, `Servicos/Dominio`, `Ajuda/Dominio`, `Fiscal/Dominio`, `Turmas/Dominio`, `Corretora/Dominio` | **Unitários**, rápidos, sem banco | Regras de negócio puras: estados da reserva, cálculo de preço, taxa do cartão, agenda, limite e recebimento da carteira, horários livres e conflito da agenda, comissão, regras do plano de assinatura e da mensalidade |
| `Compartilhado` | Unitários | `Resultado<T>` → 200/400/404/409/500 |
| `Arquitetura` | Unitários | Guardas das camadas: service sem `AppDbContext` nos módulos com repository, Domain sem EF/ASP.NET, Application sem a API |
| `Integracao` | **Integração**, API de verdade + PostgreSQL | Fluxos completos pelo HTTP, com login (JWT), banco real e migrations |

Integração (`Integracao/`):

- `ConfiguracaoChacaraApiTests` – dados da chácara, preços, faixas, horários, períodos especiais, fotos, Mercado Pago
- `ReservasChacaraApiTests` – painel do dono: reserva manual, confirmar, pagamentos, negociação, expirar/reativar, editar, contrato PDF, dashboard
- `SitePublicoChacaraApiTests` – site do cliente: página, disponibilidade, valor, reservar, cancelar, avaliar
- `PagamentoChacaraApiTests` – Pix, cartão e combinado até a confirmação da reserva
- `IsolamentoEntreLojasApiTests` – uma loja nunca vê nem altera dados de outra (multi-loja)
- `RotinasChacaraApiTests` – expiração automática e aviso de 24h
- `ServicosCatalogoApiTests` – cadastro de serviços (padrões, ordem, excluir com agendamentos, isolamento)
- `AgendamentosApiTests` – agenda do painel: criar/editar/listar, status, comissão do profissional, regras do plano ao agendar, isolamento (cliente/profissional de outra loja)
- `PlanosApiTests` – planos, assinantes, mensalidade (marcar, pagar com juros, quitar atraso), histórico, consumos e plano do cliente
- `AgendamentoOnlinePublicoApiTests` – página pública: dados da loja, horários livres, agendar (aprovação/automático), reconhecer cliente pelo telefone
- `ClientesApiTests` – cadastro (CPF único por loja, busca, total de compras e saldo da carteira, excluir só admin e sem saldo)
- `FiscalApiTests` – configuração fiscal, emitir/listar/cancelar NFC-e com provedor falso (`ProvedorFiscalFalso`)
- `AjudaApiTests` – Central de Ajuda: leitura pública, gestão só do superadmin, link do YouTube, renomear/excluir categoria
- `TurmasApiTests` – turmas, matrícula, sessões geradas, falta/remarcação, presença, profissionais e isolamento
- `CorretoraApiTests` – seguradoras, funil, comissão no financeiro, apólices, resumo e dashboard, isolamento
- `CarteiraApiTests` – carteira do cliente (fiado): venda no caixa (inteira ou dividida), limite, recebimento (entra no caixa), extrato, excluir venda/cliente, isolamento entre lojas

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
