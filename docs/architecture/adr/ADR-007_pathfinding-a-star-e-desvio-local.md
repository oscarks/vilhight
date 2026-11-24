# ADR-007: Pathfinding A* individual com desvio local em ponto fixo

**Status:** Proposed
**Data:** 2026-11-17

## Contexto

Movimento é o laço mais caro e o mais sensível ao determinismo. O critério de aceite exige 50 unidades atravessando um gargalo em menos de 20 s. `from-source:design.md` §Movimento.

## Decisão

- A\* na grade com custos e heurística em `Fixed`/inteiros, buffers pré-alocados, desempate determinístico (por id de tile).
- Desvio local por separação/steering com raio de colisão; unidades amigas paradas abrem caminho.
- Limite de pedidos de caminho por tick, com fila, para evitar picos.
- Flow fields e formações ficam para depois (design `[Depois]`).

## Consequências

- Positivo: simples e testável; encaixa no orçamento de tick esperado.
- Negativo: grupos grandes indo ao mesmo ponto podem ser caros e travar em gargalos (reavaliar com flow field).
- Validação: critério do gargalo e medição em E3-3.

## Alternativas consideradas

- Flow field desde o início: adiado, mais complexo para a Fase 1.
- RVO completo: considerado; começar por separação simples e subir se necessário.
