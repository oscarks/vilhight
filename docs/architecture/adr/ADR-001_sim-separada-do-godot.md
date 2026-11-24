# ADR-001: Simulação em C# puro, separada do Godot

**Status:** Accepted
**Data:** 2026-11-17

## Contexto

O jogo precisa de testes headless, determinismo verificável e, no futuro, a mesma lógica num servidor. O Godot traz tipos (`Node`, `Vector2`, `delta`) e relógio próprios que quebram essas propriedades. `from-source:design.md` §Diretrizes; `CLAUDE.md` §5.1.

## Decisão

`src/Sim` (`Rts.Sim.csproj`) não referencia Godot. `game/` (`Rts.Game.csproj`) referencia `Sim` e contém só apresentação. A Sim recebe o caminho dos dados como string. A apresentação envia comandos e lê estado.

## Consequências

- Positivo: testes xUnit rápidos sem editor; reuso em servidor; fronteira clara.
- Negativo: conversões na borda (`Fixed` → `float`); duplicar nada de regra na apresentação exige disciplina.
- Verificação: `dotnet build` falha se `Sim` referenciar Godot; revisão procura `float`/`Vector2` em `src/Sim`.

## Alternativas consideradas

- Lógica dentro de nós do Godot: rejeitada (não testável, não determinística).
- Sim em GDScript: rejeitada (stack é C#).
