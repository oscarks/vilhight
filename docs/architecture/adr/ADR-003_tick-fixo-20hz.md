# ADR-003: Simulação em ticks fixos de 20 por segundo com renderização interpolada

**Status:** Accepted
**Data:** 2026-11-17

## Contexto

A lógica não pode depender de `delta` nem de taxa de quadros. Meta de 300–500 unidades a 60 FPS. `from-source:design.md`; `CLAUDE.md` §5.2.

## Decisão

- Passo fixo de 50 ms de tempo de jogo (20 ticks/s). A apresentação acumula o tempo real e chama `Step` quando devido; desenha interpolando o estado anterior e o atual.
- Na Fase 1 a Sim roda na thread principal do Godot. Se a medição (E3-3) mostrar que o tick estoura o orçamento, reavaliar thread própria com snapshot (questão Q2).
- Orçamento proposto de ≤ 15 ms por tick com 300 unidades (`assumed`; confirmar em E3-3).

## Consequências

- Positivo: lógica independente da taxa de quadros; base para replay.
- Negativo: movimento exige interpolação visual; picos de tick causam engasgos visíveis.
- A IA decide a cada 10 ticks, deslocada por país, para espalhar a carga.

## Alternativas consideradas

- Passo variável com `delta`: rejeitado (quebra determinismo).
- 30 ticks/s: possível, mas custo maior sem ganho claro na Fase 1.
