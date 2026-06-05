# Loja API — Backend C# ASP.NET Core

API REST para o sistema de gestão de loja. PostgreSQL + JWT + Entity Framework Core.

## Stack

- **Runtime**: .NET 8
- **Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core 8
- **Banco**: PostgreSQL (Npgsql)
- **Auth**: JWT Bearer (8h de expiração)
- **Docs**: Swagger UI em `/swagger`

## Rodar localmente

### Pré-requisitos
- .NET 8 SDK → https://dotnet.microsoft.com/download
- PostgreSQL rodando na porta 5432

### Passos

```bash
# 1. Instalar EF Tools (uma vez só)
dotnet tool install --global dotnet-ef

# 2. Criar o banco (se não existir)
# No psql ou pgAdmin: CREATE DATABASE loja_db;

# 3. Rodar as migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

# 4. Rodar a API
dotnet run
```

API disponível em `https://localhost:5001` e `http://localhost:5000`  
Swagger em `https://localhost:5001/swagger`

## Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| POST | `/api/auth/login` | ❌ | Login, retorna JWT |
| GET | `/api/auth/me` | ✅ | Dados do usuário logado |
| GET | `/api/produtos` | ✅ | Listar produtos |
| POST | `/api/produtos` | Admin | Criar produto |
| PUT | `/api/produtos/{id}` | Admin | Editar produto |
| DELETE | `/api/produtos/{id}` | Admin | Inativar produto |
| GET | `/api/clientes` | ✅ | Listar clientes |
| POST | `/api/clientes` | ✅ | Criar cliente |
| PUT | `/api/clientes/{id}` | ✅ | Editar cliente |
| DELETE | `/api/clientes/{id}` | Admin | Excluir cliente |
| GET | `/api/vendas` | ✅ | Listar vendas |
| POST | `/api/vendas` | ✅ | Registrar venda (baixa estoque automático) |
| GET | `/api/estoque/movimentos` | ✅ | Histórico de movimentos |
| POST | `/api/estoque/ajuste` | Admin | Entrada/ajuste manual |
| GET | `/api/estoque/alertas` | ✅ | Produtos com estoque baixo |
| GET | `/api/relatorios/resumo` | ✅ | Resumo de vendas |
| GET | `/api/relatorios/produtos-ranking` | ✅ | Produtos mais vendidos |
| GET | `/api/relatorios/fluxo-diario` | ✅ | Fluxo diário por mês |
| GET | `/api/relatorios/fluxo-mensal` | ✅ | Fluxo mensal por ano |

## Deploy no Railway

### 1. Criar projeto no Railway
1. Acesse https://railway.app e crie uma conta
2. Clique em **New Project → Deploy from GitHub repo**
3. Conecte o repositório do `loja-api`

### 2. Adicionar PostgreSQL
1. No projeto, clique em **+ New → Database → PostgreSQL**
2. O Railway cria o banco e expõe a variável `DATABASE_URL` automaticamente

### 3. Configurar variáveis de ambiente
No painel do Railway, vá em **Variables** e adicione:

```
ConnectionStrings__Default=Host=SEU_HOST;Port=5432;Database=railway;Username=postgres;Password=SENHA
Jwt__Secret=CHAVE_SECRETA_COM_MINIMO_32_CARACTERES_AQUI_123
Jwt__Issuer=loja-api
Jwt__Audience=loja-frontend
AllowedOrigins=https://SEU_FRONTEND.vercel.app
ASPNETCORE_ENVIRONMENT=Production
```

> Copie o valor de `DATABASE_URL` do PostgreSQL e converta para o formato acima.  
> `DATABASE_URL` = `postgresql://user:pass@host:port/db`  
> Converte para: `Host=host;Port=port;Database=db;Username=user;Password=pass`

### 4. Deploy automático
O Railway detecta o `Dockerfile` e faz o build automaticamente.  
As migrations rodam sozinhas na inicialização (`db.Database.Migrate()` no `Program.cs`).

## Conectar o frontend React

Substitua as funções no `AppContext.tsx` por chamadas à API:

```typescript
// Exemplo: buscar produtos
const res = await fetch(`${import.meta.env.VITE_API_URL}/api/produtos`, {
  headers: { Authorization: `Bearer ${token}` }
});
const produtos = await res.json();
```

Crie um `.env` na raiz do frontend:
```
VITE_API_URL=https://seu-backend.railway.app
```

## Usuário padrão (seed)
- **E-mail**: admin@loja.com  
- **Senha**: admin123  
- **Role**: admin

Altere a senha pelo banco após o primeiro acesso em produção.
