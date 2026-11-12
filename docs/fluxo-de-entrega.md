# Fluxo de entrega (como uma mudança chega ao ar)

1. **Claude implementa num branch** (`claude/...`) e roda os testes na nuvem.
2. **Abre um PR** e espera o teste automático do GitHub (aba *Actions*) ficar verde.
3. **O PR fica parado.** O Claude **não mescla sozinho**: avisa que está pronto e diz como testar.
4. **Você testa local** (PowerShell, na pasta do projeto):
   ```powershell
   git fetch origin
   git checkout -B claude/modest-mccarthy-6tcfey origin/claude/modest-mccarthy-6tcfey
   $env:LOJA_TESTES_DB = "Host=localhost;Port=5432;Username=postgres;Password=SUA_SENHA"
   dotnet test tests\LojaApi.Tests
   ```
   Depois abra a API (`tests\LojaApi.Tests.sln`, projeto de inicialização `LojaApi`) e o frontend, e teste a tela.
   Use `checkout -B ... origin/...` (e não `pull`): o branch é reescrito a cada entrega e o `pull` pode reclamar.
5. **Você diz "pode mesclar"** (ou pede ajuste). Só então o Claude mescla (squash) e acompanha o deploy.
6. **Ordem de deploy:** quando a mudança altera a API que o frontend usa, mescla-se o **backend primeiro**, espera o deploy
   ficar estável e só depois o frontend.

Se você mexer em algo local, suba para um branch seu (`git checkout -b ajuste-local` e `git push -u origin ajuste-local`),
**nunca direto no `main`**, e avise: o Claude continua a partir desse branch.
