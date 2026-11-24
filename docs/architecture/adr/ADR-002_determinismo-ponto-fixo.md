# ADR-002: Determinismo por ponto fixo e RNG com semente

**Status:** Accepted
**Data:** 2026-11-17

## Contexto

Replays, validação em servidor e batalhas verificáveis dependem de resultados idênticos entre execuções e máquinas. No design o determinismo era `[Depois]`; o `Risk_Analysis.md` (decisão D1) e o CLAUDE.md §5.4 o tornam obrigatório desde o início, porque retroagir exigiria reescrever o núcleo. Risco F4.

## Decisão

- Tipo `Fixed` (Q32.32 em `long`) para toda a lógica numérica; multiplicação e divisão via 128 bits (`Int128` ou `Math.BigMul`); raiz quadrada inteira própria.
- `SimRandom` com semente explícita; nenhuma outra fonte de aleatoriedade.
- Proibidos na Sim: `float`/`double`, `System.Random`, `DateTime`, `Stopwatch`, `Guid.NewGuid`, estado estático mutável, threads, iteração de `Dictionary`/`HashSet` quando a ordem importa.
- Hash do estado por tick; teste obrigatório: mesma semente + mesmos comandos ⇒ mesmo hash.

## Consequências

- Positivo: replays, testes de regressão e verificação em servidor viáveis.
- Negativo: matemática mais cara e verbosa (sem trigonometria nativa); cuidado com overflow; `Fixed` precisa de suíte de testes própria (E0-2).
- O hash é a rede de segurança: qualquer não determinismo acidental quebra o teste cedo.

## Alternativas consideradas

- `float` com disciplina: rejeitada (varia entre CPU/compiladores).
- Determinismo só na fase multiplayer (design original): rejeitada pela decisão D1.
- Lockstep com checagem por tolerância: rejeitada (não dá igualdade exata).
