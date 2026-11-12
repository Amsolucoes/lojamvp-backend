#!/usr/bin/env python3
"""Compara resultado-base.json com resultado-novo.json (mesma pasta deste script)."""
import json, os
aqui = os.path.dirname(os.path.abspath(__file__))
a = json.load(open(os.path.join(aqui, 'resultado-base.json')))
b = json.load(open(os.path.join(aqui, 'resultado-novo.json')))
dif = [(x, y) for x, y in zip(a, b) if x != y]
print('chamadas base/novo:', len(a), len(b), '| DIFERENÇAS:', len(dif) + abs(len(a) - len(b)))
for x, y in dif[:10]:
    print('--', x['passo'])
    print('   base:', x['status'], json.dumps(x['corpo'], ensure_ascii=False)[:240])
    print('   novo:', y['status'], json.dumps(y['corpo'], ensure_ascii=False)[:240])
