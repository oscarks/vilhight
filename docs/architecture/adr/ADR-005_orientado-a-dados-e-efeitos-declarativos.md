# ADR-005: Conteúdo orientado a dados, validado, com efeitos declarativos

**Status:** Accepted
**Data:** 2026-11-17

## Contexto

Recursos, tecnologias, construções, unidades e doutrinas mudam muito durante o balanceamento; a IA precisa funcionar com qualquer conteúdo. Riscos F2, F6 e F8. `from-source:design.md` §Modelo de dados; `CLAUDE.md` §5.5.

## Decisão

- Todo conteúdo vem de JSON em `game/data/`, carregado para registries imutáveis.
- Nenhum id de conteúdo fixo em `src/Sim` (nem na IA). O código conhece tipos de efeito e de condição.
- Validação obrigatória no carregamento: ids únicos, referências existentes, árvore sem ciclos, todo recurso e construção alcançável.
- Efeitos de tecnologia: um handler por tipo; o validador reconhece os 8 tipos, a Fase 1 implementa 6.
- Verificação automatizada: teste que procura literais de id de conteúdo nos fontes da Sim e da IA.

## Consequências

- Positivo: balancear sem recompilar; reuso por espécies/mapas; testes com conjuntos de dados alternativos.
- Negativo: o JSON pode virar uma DSL difícil de manter (F8); erros de dados aparecem em tempo de carga, não de compilação.
- Mitigação: validador forte e mensagens claras; schema versionado quando o formato estabilizar.

## Alternativas consideradas

- Conteúdo em código C#: rejeitado (acopla balanceamento a recompilação e quebra a IA genérica).
- Scripting embutido (Lua etc.): adiado; excessivo para a Fase 1.
