# Teste de mutação manual

`mutar.sh` planta um bug de propósito, roda os testes e diz se algum falhou. É a forma de saber se um teste
**realmente protege** a regra que diz proteger. Veja o cabeçalho do script para o uso.

Boas mutações: tirar o filtro de loja de uma consulta, inverter `>` por `>=` num limite, pular uma validação,
remover o lançamento no caixa, deixar de estornar algo ao excluir. Se o teste não falha, escreva o que falta.
