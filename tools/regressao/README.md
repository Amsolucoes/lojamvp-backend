# Teste de regressão por comparação ("antes x depois")

Não há testes automatizados no projeto. Esta ferramenta serve para provar que uma **refatoração não mudou o
comportamento da API**: um roteiro (`cenario.py`) chama a API de verdade (cria uma loja de chácara, cadastra
preços, cria reservas, passa por todo o ciclo, usa o site público) e grava status + corpo de cada resposta.
Rodando o roteiro no build de **antes** e no de **depois** e comparando, qualquer diferença aparece.

```bash
dotnet build LojaApi.csproj -c Release
cp -r bin/Release/net10.0 /tmp/build-antes          # guarde o build ANTES de refatorar
# ... refatora ...
dotnet build LojaApi.csproj -c Release
tools/regressao/comparar.sh /tmp/build-antes base   # grava resultado-base.json
tools/regressao/comparar.sh bin/Release/net10.0 novo # grava resultado-novo.json
tools/regressao/diff.py                             # precisa terminar em "DIFERENÇAS: 0"
```

Cobre hoje o módulo **Chácara** (grupos `config`, `cadastros`, `reservas`, `publico`, `ciclo`). Ao refatorar outro
módulo, acrescente um grupo novo ao `cenario.py` com os endpoints dele **antes** de mexer no código.

Fora do alcance (precisa de serviço externo): pagamento real no Mercado Pago, envio de e-mail (Resend) e as
rotinas em segundo plano.
