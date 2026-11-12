#!/usr/bin/env python3
"""Roteiro de teste de caracterização: chama a API de verdade e grava (status + corpo normalizado) de cada resposta.
Rodado no programa de referência e no programa novo, as saídas precisam ser idênticas.
uso: cenario.py <url base> <banco> <saida.json> [grupos]"""
import sys, json, re, subprocess, urllib.request, urllib.error, datetime

BASE, DB, SAIDA = sys.argv[1], sys.argv[2], sys.argv[3]
GRUPOS = sys.argv[4].split(',') if len(sys.argv) > 4 else ['config', 'cadastros', 'reservas', 'publico', 'ciclo']
HOJE = datetime.datetime.utcnow().strftime('%Y-%m-%d')
reg = []
token = None

def norm(txt):
    txt = re.sub(r'[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}', '<guid>', txt)
    txt = re.sub(r'\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+Z?', '<ts>', txt)
    txt = re.sub(HOJE + r'T[\d:.]+Z?', '<agora>', txt)
    txt = re.sub(r'00-[0-9a-f]{32}-[0-9a-f]{16}-00', '<trace>', txt)
    txt = re.sub(r'eyJ[\w-]+\.[\w-]+\.[\w-]+', '<jwt>', txt)
    return txt

def chamar(metodo, caminho, corpo=None, auth=True, rotulo=None):
    url = BASE + caminho
    dados = json.dumps(corpo).encode() if corpo is not None else None
    req = urllib.request.Request(url, data=dados, method=metodo)
    if dados is not None: req.add_header('Content-Type', 'application/json')
    if auth and token: req.add_header('Authorization', 'Bearer ' + token)
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            status, ctype, raw = r.status, r.headers.get('Content-Type', ''), r.read()
    except urllib.error.HTTPError as e:
        status, ctype, raw = e.code, e.headers.get('Content-Type', ''), e.read()
    if 'application/pdf' in ctype:
        corpo_txt = f'<pdf {"ok" if raw[:4] == b"%PDF" else "invalido"}>'
    else:
        corpo_txt = norm(raw.decode('utf-8', 'replace'))
        try: corpo_txt = json.loads(corpo_txt)
        except Exception: pass
    reg.append({'passo': rotulo or f'{metodo} {caminho}', 'status': status, 'tipo': ctype.split(';')[0], 'corpo': corpo_txt})
    return status, corpo_txt

def sql(q):
    subprocess.run(['su', 'postgres', '-c', f'psql -d {DB} -qc "{q}"'], check=True, capture_output=True)

# ── preparo: cria a loja de chácara e entra ──
chamar('POST', '/api/auth/signup', {'nomeLoja': 'Chácara Teste', 'perfilId': '20000000-0000-0000-0000-00000000000a', 'nomeResponsavel': 'Dono', 'email': 'dono@teste.com', 'senha': '123456', 'telefone': '67999990000'}, auth=False, rotulo='signup')
s, b = chamar('POST', '/api/auth/login', {'email': 'dono@teste.com', 'senha': '123456'}, auth=False, rotulo='login')
raw_login = urllib.request.urlopen(urllib.request.Request(BASE + '/api/auth/login', data=json.dumps({'email': 'dono@teste.com', 'senha': '123456'}).encode(), headers={'Content-Type': 'application/json'})).read()
token = json.loads(raw_login)['token']
sql("UPDATE lojas SET slug='teste' WHERE nome='Chácara Teste'")
chamar('GET', '/api/chacara/info', auth=False, rotulo='sem login -> 401')

if 'config' in GRUPOS:
    chamar('GET', '/api/chacara/info')
    chamar('PUT', '/api/chacara/info', {'descricao': '  Chácara linda  ', 'endereco': ' Rua A, 10 ', 'comodidades': 'piscina,wifi', 'comodidadesExtras': '   ', 'mapaEmbedUrl': None, 'horaEntrada': '  ', 'horaSaida': ' 21:30 ', 'avisosUso': ' sem som alto ', 'locadorNome': ' Fulano ', 'locadorRg': '', 'locadorCpf': '123.456.789-00', 'locadorEndereco': None, 'locadorTelefone': ' 67 99999-0000 ', 'cidadeAssinatura': 'Campo Grande – MS'}, rotulo='PUT info completo')
    chamar('PUT', '/api/chacara/info', {'descricao': None, 'endereco': None, 'comodidades': None}, rotulo='PUT info com nulos')
    chamar('GET', '/api/chacara/info', rotulo='GET info depois')
    chamar('GET', '/api/chacara/info/pagamento')
    chamar('PUT', '/api/chacara/info/pagamento', {'accessToken': '', 'publicKey': ''}, rotulo='PUT pagamento vazio')
    chamar('DELETE', '/api/chacara/info/pagamento')
    chamar('GET', '/api/chacara/configuracao-preco')
    chamar('PUT', '/api/chacara/configuracao-preco', {'minimoPessoas': 4, 'limitePessoasParaTaxaLimpeza': 60, 'valorTaxaLimpeza': 300, 'valorMultaNaoLimpeza': 100, 'percentualEntradaMinimo': 40}, rotulo='PUT config básico')
    chamar('PUT', '/api/chacara/configuracao-preco', {'minimoPessoas': 4, 'limitePessoasParaTaxaLimpeza': 60, 'valorTaxaLimpeza': 300, 'valorMultaNaoLimpeza': 100, 'percentualEntradaMinimo': 40, 'percentualTaxaCartao': 20}, rotulo='PUT config taxa inválida')
    chamar('PUT', '/api/chacara/configuracao-preco', {'minimoPessoas': 4, 'limitePessoasParaTaxaLimpeza': 60, 'valorTaxaLimpeza': 300, 'valorMultaNaoLimpeza': 100, 'percentualEntradaMinimo': 40, 'parcelasAteLimite': 0}, rotulo='PUT config parcelas inválidas')
    chamar('PUT', '/api/chacara/configuracao-preco', {'minimoPessoas': 5, 'limitePessoasParaTaxaLimpeza': 70, 'valorTaxaLimpeza': 350, 'valorMultaNaoLimpeza': 120, 'percentualEntradaMinimo': 30, 'percentualTaxaCartao': 7.5, 'valorLimiteParcelas': 900, 'parcelasAteLimite': 3, 'parcelasAcimaLimite': 6}, rotulo='PUT config completo')
    chamar('GET', '/api/chacara/configuracao-preco', rotulo='GET config depois')

if 'cadastros' in GRUPOS:
    # faixas de preço
    chamar('POST', '/api/chacara/faixas-preco', {'pessoasAte': 0, 'valorDiariaSemana': 1, 'valorDiariaFimSemana': 1, 'valorPacote2DiasFimSemana': 1}, rotulo='faixa inválida')
    chamar('POST', '/api/chacara/faixas-preco', {'pessoasAte': 50, 'valorDiariaSemana': 500, 'valorDiariaFimSemana': 800, 'valorPacote2DiasFimSemana': 1400}, rotulo='faixa 50')
    chamar('POST', '/api/chacara/faixas-preco', {'pessoasAte': 100, 'valorDiariaSemana': 700, 'valorDiariaFimSemana': 1000, 'valorPacote2DiasFimSemana': 1800}, rotulo='faixa 100')
    chamar('POST', '/api/chacara/faixas-preco', {'pessoasAte': 50, 'valorDiariaSemana': 1, 'valorDiariaFimSemana': 1, 'valorPacote2DiasFimSemana': 1}, rotulo='faixa duplicada')
    chamar('GET', '/api/chacara/faixas-preco')
    chamar('PUT', '/api/chacara/faixas-preco/2', {'pessoasAte': 50, 'valorDiariaSemana': 1, 'valorDiariaFimSemana': 1, 'valorPacote2DiasFimSemana': 1}, rotulo='faixa PUT duplicada')
    chamar('PUT', '/api/chacara/faixas-preco/2', {'pessoasAte': 120, 'valorDiariaSemana': 710, 'valorDiariaFimSemana': 1010, 'valorPacote2DiasFimSemana': 1810}, rotulo='faixa PUT ok')
    chamar('PUT', '/api/chacara/faixas-preco/999', {'pessoasAte': 5, 'valorDiariaSemana': 1, 'valorDiariaFimSemana': 1, 'valorPacote2DiasFimSemana': 1}, rotulo='faixa PUT 404')
    chamar('GET', '/api/chacara/faixas-preco', rotulo='faixas depois')
    chamar('DELETE', '/api/chacara/faixas-preco/1', rotulo='faixa DELETE ok')
    chamar('DELETE', '/api/chacara/faixas-preco/1', rotulo='faixa DELETE 404')
    chamar('POST', '/api/chacara/faixas-preco', {'pessoasAte': 50, 'valorDiariaSemana': 500, 'valorDiariaFimSemana': 800, 'valorPacote2DiasFimSemana': 1400}, rotulo='faixa 50 de novo')
    # horários
    chamar('POST', '/api/chacara/horarios', {'tipo': 'xxx', 'hora': '08:00', 'ajuste': 0, 'ordem': 0}, rotulo='horário tipo inválido')
    chamar('POST', '/api/chacara/horarios', {'tipo': 'entrada', 'hora': '25:00', 'ajuste': 0, 'ordem': 0}, rotulo='horário hora inválida')
    chamar('POST', '/api/chacara/horarios', {'tipo': 'entrada', 'hora': '08:00', 'ajuste': 0, 'ordem': 0}, rotulo='horário entrada 08')
    chamar('POST', '/api/chacara/horarios', {'tipo': 'entrada', 'hora': '08:00', 'ajuste': 5, 'ordem': 1}, rotulo='horário duplicado')
    chamar('POST', '/api/chacara/horarios', {'tipo': 'entrada', 'hora': '14:30', 'ajuste': -50, 'ordem': 1}, rotulo='horário entrada 14:30')
    chamar('POST', '/api/chacara/horarios', {'tipo': 'saida', 'hora': '22:00', 'ajuste': 100, 'ordem': 0}, rotulo='horário saída 22')
    chamar('PUT', '/api/chacara/horarios/2', {'tipo': 'entrada', 'hora': '08:00', 'ajuste': 0, 'ordem': 0}, rotulo='horário PUT duplicado')
    chamar('PUT', '/api/chacara/horarios/2', {'tipo': 'entrada', 'hora': '15:00', 'ajuste': -40, 'ordem': 2}, rotulo='horário PUT ok')
    chamar('PUT', '/api/chacara/horarios/2', {'tipo': 'entrada', 'hora': '99:99', 'ajuste': 0, 'ordem': 0}, rotulo='horário PUT hora inválida')
    chamar('PUT', '/api/chacara/horarios/999', {'tipo': 'entrada', 'hora': '10:00', 'ajuste': 0, 'ordem': 0}, rotulo='horário PUT 404')
    chamar('GET', '/api/chacara/horarios')
    chamar('DELETE', '/api/chacara/horarios/3', rotulo='horário DELETE ok')
    chamar('DELETE', '/api/chacara/horarios/3', rotulo='horário DELETE 404')
    # períodos especiais
    chamar('POST', '/api/chacara/periodos-especiais', {'nome': '  ', 'dataInicio': '2026-12-24', 'dataFim': '2026-12-26', 'valorTotal': 5000}, rotulo='período sem nome')
    chamar('POST', '/api/chacara/periodos-especiais', {'nome': 'Natal', 'dataInicio': '2026-12-26', 'dataFim': '2026-12-24', 'valorTotal': 5000}, rotulo='período datas invertidas')
    chamar('POST', '/api/chacara/periodos-especiais', {'nome': '  Natal ', 'dataInicio': '2026-12-24T03:00:00', 'dataFim': '2026-12-26', 'valorTotal': 5000}, rotulo='período Natal')
    chamar('POST', '/api/chacara/periodos-especiais', {'nome': 'Ano Novo', 'dataInicio': '2026-12-30', 'dataFim': '2027-01-02', 'valorTotal': 8000}, rotulo='período Ano Novo')
    chamar('PUT', '/api/chacara/periodos-especiais/1', {'nome': ' Natal 2026 ', 'dataInicio': '2026-12-23', 'dataFim': '2026-12-26', 'valorTotal': 5500}, rotulo='período PUT ok')
    chamar('PUT', '/api/chacara/periodos-especiais/1', {'nome': 'X', 'dataInicio': '2026-12-26', 'dataFim': '2026-12-23', 'valorTotal': 1}, rotulo='período PUT datas inválidas')
    chamar('PUT', '/api/chacara/periodos-especiais/999', {'nome': 'X', 'dataInicio': '2026-12-23', 'dataFim': '2026-12-26', 'valorTotal': 1}, rotulo='período PUT 404')
    chamar('GET', '/api/chacara/periodos-especiais')
    # fotos
    chamar('POST', '/api/chacara/fotos', {'url': '   '}, rotulo='foto vazia')
    for i in range(1, 4): chamar('POST', '/api/chacara/fotos', {'url': f' https://img.test/foto{i}.jpg '}, rotulo=f'foto {i}')
    chamar('GET', '/api/chacara/fotos')
    chamar('PUT', '/api/chacara/fotos/ordem', {'ids': [3, 1, 2, 77]}, rotulo='fotos reordenar')
    chamar('GET', '/api/chacara/fotos', rotulo='fotos depois')
    chamar('DELETE', '/api/chacara/fotos/2', rotulo='foto DELETE ok')
    chamar('DELETE', '/api/chacara/fotos/2', rotulo='foto DELETE 404')

if 'reservas' in GRUPOS:
    chamar('POST', '/api/chacara/reservas', {'dataInicio': '2026-12-24', 'dataFim': '2026-12-26', 'pessoas': 30, 'clienteNome': 'Maria Silva', 'clienteEmail': 'maria@x.com', 'clienteTelefone': '(67) 99999-0000', 'valor': 5500, 'valorPago': 2000}, rotulo='reserva manual Natal')
    chamar('POST', '/api/chacara/reservas', {'dataInicio': '2026-12-25', 'dataFim': '2026-12-25', 'pessoas': 10, 'clienteNome': 'Conflito', 'clienteEmail': '', 'clienteTelefone': '', 'valor': 1}, rotulo='reserva manual conflito')
    chamar('GET', '/api/chacara/reservas')
    chamar('GET', '/api/chacara/periodos-especiais', rotulo='períodos com reserva')
    chamar('GET', '/api/chacara/dashboard')
    chamar('GET', '/api/chacara/dashboard?ano=2026', rotulo='dashboard 2026')
    chamar('GET', '/api/chacara/dashboard?mesesAFrente=3', rotulo='dashboard 3 meses')


if 'publico' in GRUPOS:
    sql("UPDATE lojas SET modulos_ativos='chacara_reservas' WHERE slug='teste'")
    P = '/api/publico/teste/chacara'
    chamar('GET', '/api/publico/naoexiste/chacara/dados', auth=False, rotulo='site slug inexistente')
    chamar('GET', P + '/dados', auth=False, rotulo='site dados (locador incompleto)')
    chamar('PUT', '/api/chacara/info', {'descricao': 'Chácara linda', 'endereco': 'Rua A, 10', 'comodidades': 'piscina,wifi,xx', 'comodidadesExtras': 'Rede\nSauna', 'horaEntrada': '08:00', 'horaSaida': '22:00', 'avisosUso': 'Sem som alto\nSem animais', 'locadorNome': 'Fulano de Tal', 'locadorCpf': '123.456.789-00', 'locadorEndereco': 'Rua do Locador, 5', 'locadorTelefone': '67999990000', 'cidadeAssinatura': 'Campo Grande'}, rotulo='info com locador completo')
    chamar('GET', P + '/dados', auth=False, rotulo='site dados completo')
    chamar('GET', P + '/disponibilidade?dataInicio=2027-02-05&dataFim=2027-02-07', auth=False, rotulo='disponível')
    chamar('GET', P + '/disponibilidade?dataInicio=2026-12-25&dataFim=2026-12-25', auth=False, rotulo='indisponível (Natal)')
    chamar('GET', '/api/publico/naoexiste/chacara/disponibilidade?dataInicio=2027-02-05&dataFim=2027-02-07', auth=False, rotulo='disponibilidade slug inexistente')
    chamar('GET', P + '/datas-ocupadas?meses=24', auth=False, rotulo='datas ocupadas')
    chamar('GET', P + '/valor?dataInicio=2027-02-05&dataFim=2027-02-07&pessoas=2', auth=False, rotulo='valor poucas pessoas')
    chamar('GET', P + '/valor?dataInicio=2027-02-05&dataFim=2027-02-07&pessoas=30', auth=False, rotulo='valor fim de semana')
    chamar('GET', P + '/valor?dataInicio=2027-02-05&dataFim=2027-02-07&pessoas=30&horaEntrada=15:00&horaSaida=22:00', auth=False, rotulo='valor com horários')
    chamar('GET', P + '/valor?dataInicio=2026-12-23&dataFim=2026-12-26&pessoas=30', auth=False, rotulo='valor período especial')
    chamar('GET', P + '/valor?dataInicio=2027-02-01&dataFim=2027-04-01&pessoas=30', auth=False, rotulo='valor mais de 30 dias')
    chamar('GET', P + '/valor?dataInicio=2027-03-01&dataFim=2027-03-02&pessoas=80', auth=False, rotulo='valor com taxa de limpeza')
    chamar('POST', P + '/reservar', {'dataInicio': '2027-02-05', 'dataFim': '2027-02-07', 'pessoas': 30, 'clienteNome': '', 'clienteEmail': 'a@a.com', 'clienteTelefone': '1'}, auth=False, rotulo='reservar sem nome')
    chamar('POST', P + '/reservar', {'dataInicio': '2027-02-07', 'dataFim': '2027-02-05', 'pessoas': 30, 'clienteNome': 'X', 'clienteEmail': 'a@a.com', 'clienteTelefone': '1'}, auth=False, rotulo='reservar datas invertidas')
    chamar('POST', P + '/reservar', {'dataInicio': '2027-02-05', 'dataFim': '2027-02-07', 'pessoas': 1, 'clienteNome': 'X', 'clienteEmail': 'a@a.com', 'clienteTelefone': '1'}, auth=False, rotulo='reservar poucas pessoas')
    chamar('POST', P + '/reservar', {'dataInicio': '2026-12-25', 'dataFim': '2026-12-25', 'pessoas': 30, 'clienteNome': 'X', 'clienteEmail': 'a@a.com', 'clienteTelefone': '1'}, auth=False, rotulo='reservar conflito')
    chamar('POST', P + '/reservar', {'dataInicio': '2027-02-05', 'dataFim': '2027-02-07', 'pessoas': 30, 'clienteNome': '  João Cliente ', 'clienteEmail': ' joao@x.com ', 'clienteTelefone': '(67) 98888-7777', 'clienteDocumento': ' 111.222.333-44 ', 'clienteCep': '', 'clienteEndereco': ' Rua B ', 'horaEntrada': '15:00', 'horaSaida': '22:00'}, auth=False, rotulo='reservar ok (id 2)')
    chamar('POST', P + '/reservar', {'dataInicio': '2027-02-06', 'dataFim': '2027-02-06', 'pessoas': 30, 'clienteNome': 'Outro', 'clienteEmail': 'o@o.com', 'clienteTelefone': '2'}, auth=False, rotulo='reservar conflito com pendente')
    chamar('GET', P + '/avaliacao/2', auth=False, rotulo='avaliar reserva pendente')
    chamar('POST', P + '/reservas/2/pagamento/escolher', {'formaPagamento': 'boleto'}, auth=False, rotulo='escolher forma inválida')
    chamar('POST', P + '/reservas/2/pagamento/escolher', {'formaPagamento': 'pix'}, auth=False, rotulo='escolher pix sem MP')
    chamar('POST', P + '/reservas/2/pagamento/pix', auth=False, rotulo='pix sem escolher')
    chamar('POST', P + '/reservas/2/pagamento/cartao', {'token': 't', 'parcelas': 1}, auth=False, rotulo='cartão sem escolher')
    chamar('GET', P + '/reservas/2/pagamento/status', auth=False, rotulo='status do pagamento')
    chamar('GET', P + '/reservas/999/pagamento/status', auth=False, rotulo='status reserva inexistente')
    chamar('POST', P + '/reservas/2/cancelar', auth=False, rotulo='cancelar pelo cliente')
    chamar('POST', P + '/reservas/2/cancelar', auth=False, rotulo='cancelar de novo')
    chamar('POST', P + '/reservas/2/pagamento/escolher', {'formaPagamento': 'pix'}, auth=False, rotulo='escolher em reserva cancelada')

if 'ciclo' in GRUPOS:
    P = '/api/publico/teste/chacara'
    # reserva pendente nova (id 3) para exercitar as transições
    chamar('POST', P + '/reservar', {'dataInicio': '2027-03-10', 'dataFim': '2027-03-11', 'pessoas': 20, 'clienteNome': 'Ciclo Um', 'clienteEmail': 'c1@x.com', 'clienteTelefone': '67911112222', 'clienteDocumento': '99988877766'}, auth=False, rotulo='ciclo: reservar (id 3)')
    chamar('GET', '/api/chacara/reservas', rotulo='ciclo: lista')
    chamar('PATCH', '/api/chacara/reservas/3/desfazer-negociacao', rotulo='ciclo: desfazer negociação')
    chamar('PATCH', '/api/chacara/reservas/3/manter-negociacao', rotulo='ciclo: manter negociação')
    chamar('PATCH', '/api/chacara/reservas/999/manter-negociacao', rotulo='ciclo: manter 404')
    chamar('GET', '/api/chacara/dashboard', rotulo='ciclo: dashboard com negociação')
    chamar('PATCH', '/api/chacara/reservas/3/marcar-expirada', rotulo='ciclo: marcar expirada')
    chamar('PATCH', '/api/chacara/reservas/3/marcar-expirada', rotulo='ciclo: marcar expirada de novo')
    chamar('GET', '/api/chacara/reservas/perdidas', rotulo='ciclo: perdidas')
    chamar('GET', '/api/chacara/reservas/perdidas?mes=' + datetime.datetime.utcnow().strftime('%m').lstrip('0') + '&ano=' + HOJE[:4], rotulo='ciclo: perdidas do mês')
    chamar('GET', '/api/chacara/reservas/perdidas?de=2020-01-01&ate=2099-01-01&pagina=1&porPagina=5', rotulo='ciclo: perdidas por período')
    chamar('PATCH', '/api/chacara/reservas/3/reativar', rotulo='ciclo: reativar')
    chamar('PATCH', '/api/chacara/reservas/3/reativar', rotulo='ciclo: reativar de novo')
    chamar('PATCH', '/api/chacara/reservas/3/prejuizo', {'valor': -5, 'observacao': 'x'}, rotulo='ciclo: prejuízo negativo')
    chamar('PATCH', '/api/chacara/reservas/3/prejuizo', {'valor': 150.5, 'observacao': '  quebrou cadeira '}, rotulo='ciclo: prejuízo ok')
    chamar('PATCH', '/api/chacara/reservas/3/avaliar-cliente', {'nota': 9, 'comentario': 'x'}, rotulo='ciclo: avaliar cliente inválido')
    chamar('PATCH', '/api/chacara/reservas/3/avaliar-cliente', {'nota': 4, 'comentario': ' ótimo '}, rotulo='ciclo: avaliar cliente ok')
    chamar('PATCH', '/api/chacara/reservas/3/registrar-pagamento', {'valor': 10}, rotulo='ciclo: pagamento em pendente')
    chamar('PATCH', '/api/chacara/reservas/3/confirmar', {'valorPago': 500}, rotulo='ciclo: confirmar parcial')
    chamar('PATCH', '/api/chacara/reservas/3/confirmar', {'valorPago': 500}, rotulo='ciclo: confirmar de novo')
    chamar('PATCH', '/api/chacara/reservas/3/registrar-pagamento', {'valor': 0}, rotulo='ciclo: pagamento zero')
    chamar('PATCH', '/api/chacara/reservas/3/registrar-pagamento', {'valor': 100}, rotulo='ciclo: pagamento adicional')
    chamar('PATCH', '/api/chacara/reservas/3/registrar-pagamento', {'valor': 999999}, rotulo='ciclo: pagamento excedente')
    chamar('GET', '/api/chacara/reservas/3/contrato-pdf', rotulo='ciclo: contrato pdf')
    chamar('GET', '/api/chacara/reservas/999/contrato-pdf', rotulo='ciclo: contrato pdf 404')
    chamar('POST', '/api/chacara/reservas/3/enviar-contrato', rotulo='ciclo: enviar contrato')
    chamar('PUT', '/api/chacara/reservas/3', {'dataInicio': '2027-03-12', 'dataFim': '2027-03-10', 'pessoas': 20, 'clienteNome': 'X', 'clienteEmail': 'x@x.com', 'clienteTelefone': '1'}, rotulo='ciclo: editar datas invertidas')
    chamar('PUT', '/api/chacara/reservas/3', {'dataInicio': '2027-03-01', 'dataFim': '2027-05-01', 'pessoas': 20, 'clienteNome': 'X', 'clienteEmail': 'x@x.com', 'clienteTelefone': '1'}, rotulo='ciclo: editar mais de 30 dias')
    chamar('PUT', '/api/chacara/reservas/3', {'dataInicio': '2027-03-10', 'dataFim': '2027-03-11', 'pessoas': 1, 'clienteNome': 'X', 'clienteEmail': 'x@x.com', 'clienteTelefone': '1'}, rotulo='ciclo: editar poucas pessoas')
    chamar('PUT', '/api/chacara/reservas/3', {'dataInicio': '2026-12-25', 'dataFim': '2026-12-25', 'pessoas': 20, 'clienteNome': 'X', 'clienteEmail': 'x@x.com', 'clienteTelefone': '1'}, rotulo='ciclo: editar com conflito')
    chamar('PUT', '/api/chacara/reservas/3', {'dataInicio': '2027-03-10', 'dataFim': '2027-03-12', 'pessoas': 20, 'clienteNome': ' Ciclo Dois ', 'clienteEmail': ' c2@x.com ', 'clienteTelefone': '(67) 9 1111-3333', 'clienteDocumento': '  ', 'clienteCep': '79000-000', 'horaEntrada': '15:00'}, rotulo='ciclo: editar recalculando')
    chamar('PUT', '/api/chacara/reservas/3', {'dataInicio': '2027-03-10', 'dataFim': '2027-03-12', 'pessoas': 20, 'clienteNome': 'Ciclo Dois', 'clienteEmail': 'c2@x.com', 'clienteTelefone': '67911113333', 'valorManual': 777}, rotulo='ciclo: editar valor manual')
    chamar('PUT', '/api/chacara/reservas/999', {'dataInicio': '2027-03-10', 'dataFim': '2027-03-12', 'pessoas': 20, 'clienteNome': 'X', 'clienteEmail': 'x@x.com', 'clienteTelefone': '1'}, rotulo='ciclo: editar 404')
    # reserva do passado (confirmada) para a avaliação
    chamar('POST', '/api/chacara/reservas', {'dataInicio': '2020-01-10', 'dataFim': '2020-01-11', 'pessoas': 10, 'clienteNome': 'Passado', 'clienteEmail': '', 'clienteTelefone': '', 'valor': 1000, 'valorPago': 1000}, rotulo='ciclo: reserva do passado (id 4)')
    chamar('GET', P + '/avaliacao/4', auth=False, rotulo='ciclo: avaliação abrir')
    chamar('POST', P + '/avaliacao/4', {'nota': 9, 'comentario': 'x'}, auth=False, rotulo='ciclo: avaliação nota inválida')
    chamar('POST', P + '/avaliacao/4', {'nota': 5, 'comentario': '  Maravilhoso  '}, auth=False, rotulo='ciclo: avaliação enviar')
    chamar('POST', P + '/avaliacao/4', {'nota': 3, 'comentario': ''}, auth=False, rotulo='ciclo: avaliação atualizar')
    chamar('GET', P + '/avaliacao/4', auth=False, rotulo='ciclo: avaliação depois')
    chamar('GET', P + '/avaliacao/3', auth=False, rotulo='ciclo: avaliação estadia não terminou')
    chamar('DELETE', '/api/chacara/reservas/4', rotulo='ciclo: excluir')
    chamar('DELETE', '/api/chacara/reservas/4', rotulo='ciclo: excluir de novo')
    chamar('GET', '/api/chacara/reservas', rotulo='ciclo: lista final')
    chamar('GET', '/api/chacara/dashboard', rotulo='ciclo: dashboard final')

json.dump(reg, open(SAIDA, 'w'), ensure_ascii=False, indent=1, sort_keys=True)
print(len(reg), 'chamadas gravadas em', SAIDA)
