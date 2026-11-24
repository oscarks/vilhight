# ADR-006: Entidades em arrays densos indexados por id (ECS leve)

**Status:** Proposed
**Data:** 2026-11-17

## Contexto

Meta de 300–500 unidades, sem alocação por tick em laços quentes e iteração em ordem definida. O design pede "ECS leve". Risco F5.

## Decisão

- Unidades e construções vivem em arrays de structs (um por componente relevante: posição, movimento, vida, combate, trabalho, produção), indexados por `EntityId` denso com lista de ids livres e geração.
- Iteração sempre em ordem crescente de id.
- Sem LINQ nem alocação nos laços de movimento, combate e pathfinding; buffers reaproveitados.
- Os tipos de componente são definidos na spec do E0/E3; decisão a confirmar com o spike de desempenho (E3-3).

## Consequências

- Positivo: cache-friendly, determinístico, sem GC no laço quente.
- Negativo: mais código de infraestrutura que classes de objeto; refatorar componentes custa mais.
- Revisitar se E3-3 mostrar que a abordagem simples (classes) já cumpre o orçamento.

## Alternativas consideradas

- Hierarquia de classes com objetos por entidade: rejeitada por alocação e ordem de iteração.
- Biblioteca ECS de terceiros: não instalar pacote sem justificar (CLAUDE.md §8); reavaliar se o custo próprio for alto.
