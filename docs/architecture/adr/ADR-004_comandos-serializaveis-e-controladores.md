# ADR-004: Comandos serializáveis como única via de mudança e `IController` comum a humano e IA

**Status:** Accepted
**Data:** 2026-11-17

## Contexto

O jogador e a IA precisam agir pelos mesmos meios; trocar de país não pode quebrar nada; replays e multiplayer exigem registrar as ações. `from-source:design.md` §Arquitetura; `CLAUDE.md` §5.2–5.3.

## Decisão

- Toda ação é um `Command` serializável com `CountryId`, aplicado num tick.
- `Simulation.Step(IReadOnlyList<Command>)` valida e aplica; comandos para país que o emissor não controla, ou para unidades em batalha (Fase 2), são rejeitados com evento de rejeição, sem exceção.
- `IController` lê o estado do país e emite comandos; `HumanController` e `AiController` o implementam. A troca de controle é um comando.

## Consequências

- Positivo: IA e humano são simétricos; assumir um país no meio de uma ação é seguro; replays gravam só comandos.
- Negativo: toda interação de interface precisa virar um comando (mais código na borda).
- Consequência: a apresentação não tem caminho para mudar estado diretamente.

## Alternativas consideradas

- Chamadas diretas a métodos da Sim: rejeitada (não serializável, acopla humano e IA).
