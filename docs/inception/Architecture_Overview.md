# Visão da Arquitetura (Fase 1)

> Status: **aprovado (approved)** em 2026-11-17. Papel: Arquiteto de Software.
> Base: `Scope_and_Phasing.md` (Fase 1), `Modules.md`, `Requirements.md`, `Risk_Analysis.md`, `sources/design.md` e `CLAUDE.md` §4–6.
> Decisões significativas estão em `docs/architecture/adr/ADR-001` a `ADR-009`.
> Stack: Godot 4.x (.NET) + C# (.NET 8) + xUnit, já fixada no CLAUDE.md. O aspecto `stack` (`/inception stack`) não é necessário, pois não há apps de servidor nem infraestrutura nesta fase.

## 1. Princípio central

A **simulação é uma biblioteca C# pura e determinística**. O Godot é só uma das apresentações possíveis. A mesma Sim roda no teste (headless), no jogo e, no futuro, num servidor.

```
 ┌──────────────────────── game/ (Godot, .NET) ────────────────────────┐
 │  Presentation: cenas, câmera, seleção, HUD, árvore, "o que aconteceu"│
 │  lê GameState + eventos  ──►  desenha (interpola entre ticks)       │
 │  entrada do jogador      ──►  HumanController ──► Command           │
 └──────────────┬───────────────────────────────▲──────────────────────┘
                │ Command[]                     │ GameState (somente leitura), SimEvents
 ┌──────────────▼───────────────────────────────┴──────────────────────┐
 │ src/Sim (C# puro, sem Godot, sem float, sem System.Random)           │
 │  Simulation.Step(commands) → SimEvents                               │
 │   ├─ Core      Fixed · SimRandom · ids fortes · hash · fila de cmds  │
 │   ├─ Data      carregador + validador JSON → registries              │
 │   ├─ State     Universo→Sistema→Mundo→País                           │
 │   ├─ Control   IController (Human | Ai) · troca de controle         │
 │   └─ Systems/  Map · Movement · Economy · Production · Tech ·        │
 │                Combat · AI · Events                                  │
 └──────────────▲───────────────────────────────────────────────────────┘
                │ lê no carregamento (texto de `res://data` via `Parse`, ou pasta via `Load`; ADR-009)
          game/data/*.json  (recursos, receitas, techs, construções, unidades, doutrinas, mapas)
```

## 2. Fronteiras

| Fronteira | Regra | ADR |
|---|---|---|
| Sim ↔ Godot | `Rts.Sim.csproj` não referencia Godot; `Rts.Game.csproj` referencia `Sim`. Presentation só envia `Command` e lê estado. | 001 |
| Estado ↔ efeito | Todo estado muda só dentro de `Simulation.Step`, por comando ou por sistema. | 004 |
| Conteúdo ↔ código | Ids de recursos, techs, unidades e construções só existem em JSON; o código conhece tipos de efeito e de condição. | 005 |
| Humano ↔ IA | Mesma interface `IController`; a Sim não os distingue. | 004 |
| Tempo | Só `tick`; nada de relógio de parede. | 002, 003 |

## 3. Ciclo de um tick

1. A apresentação acumula o tempo real e chama `Step` quantas vezes forem devidas (20/s); a renderização interpola entre o estado anterior e o atual.
2. `Step(commands)`: valida e aplica os comandos **na ordem recebida** (a ordem por país, crescente por id, é fornecida pelo `ControllerSet`/`SimLoop`); depois executa os sistemas numa **ordem fixa** (Control/AI → Tech → Production → Economy → Movement → Combat → Events) e devolve `SimEvents`. O hash **não** é calculado a cada tick: é obtido sob demanda por `ComputeHash`.

> Nota (2026-11-24): itens 1–2 alinhados à decisão aprovada na AI-SPEC_Foundation_Phase_2 (DD-P2, RN-P2-21), antes descritos como "por país, em ordem de id" e "atualiza o hash" no `Step`. O `TickClock` (acúmulo de tempo → ticks) fica em `src/Sim/Hosting`, sem relógio de parede.
3. A IA de cada país decide a cada 10 ticks, com deslocamento `(tick + índice do país) mod 10`.
4. A apresentação reage aos eventos (aviso de pesquisa concluída, país eliminado) e redesenha.

## 4. Modelo de estado

- Hierarquia `Universe → StarSystem → World → Country` (nada assume 1 sistema/1 planeta).
- Pertence ao **país**: estoque, população, árvore pesquisada, recursos revelados, doutrina, controlador, log de eventos.
- Pertence ao **mundo**: mapa, depósitos, entidades (unidades e construções), com armazenamento em arrays densos indexados por id (ADR-006).
- Ids fortes (`CountryId`, `EntityId`, `ResourceId`, ...). Ids de conteúdo são índices resolvidos pelos registries no carregamento; nomes em JSON são só para humanos.
- Coleções com ordem de iteração definida por id. Sem `Dictionary`/`HashSet` onde a ordem afeta o resultado.

## 5. Dados (JSON em `game/data/`)

| Arquivo | Conteúdo | Validação |
|---|---|---|
| `resources.json` | categorias, `revealedBy`, `extractedBy`, `depletes`, `renewable` | ids únicos; referências existentes |
| `recipes.json` | entradas, saídas, `durationTicks`, construção | construção e recursos existem |
| `techs.json` | tier, custo, `researchTicks`, `requires`, `effects` | sem ciclos; efeitos conhecidos; todo recurso e construção alcançável |
| `buildings.json`, `units.json` | papéis, atributos, custos, tempos | custos referenciam recursos existentes |
| `doctrines.json` | pesos, plano de pesquisa, postura, exército | técnicas e unidades existem |
| `maps/*.json` | terreno, depósitos, posições iniciais | dimensões e referências |

Cada carga passa por validação com mensagens claras; falha impede a partida (RNF-05).

## 6. Efeitos de tecnologia

Um `IEffectHandler` por tipo (`revealResource`, `unlockBuilding`, `unlockUnit`, `unlockRecipe`, `modifyStat`, `raiseGatherCap`), registrado por nome. O validador conhece também `replaceRequirement` e `obsolete`, sem handler na Fase 1.

## 7. IA

Planejador por **encadeamento reverso** sobre o grafo de dados: dado um objetivo ("ter metal"), percorre receitas, construções, techs e recursos revelados para montar a lista de pré-requisitos faltantes, escolhe a ação seguinte pela doutrina e emite comandos comuns. Não há tabela de ids no código (ADR-008, validado pelo spike F2 em E6).

## 8. Apresentação (Godot)

- Cenas por responsabilidade: `World` (mapa, entidades), `Hud` (recursos, seletor de países, painel de seleção), `TechTree`, `WhatHappened`.
- Nós só leem o estado e reagem a eventos; nenhuma regra de jogo em `game/scripts` (RNF-06).
- Conversão `Fixed → float` só aqui, para posição e interpolação.
- Arte provisória por código; 1 unidade pelo pipeline Blender na Fase 1.

## 9. Atributos de qualidade e como a arquitetura os atende

| Atributo | Meta | Mecanismo |
|---|---|---|
| Determinismo | mesma semente + comandos ⇒ mesmo hash | ADR-002, ordem fixa de sistemas, hash sob demanda (`ComputeHash`, E0-7) |
| Testabilidade | toda regra testável headless | ADR-001; `Sim.Tests` sem Godot |
| Desempenho | 300 unidades dentro do orçamento; sem alocação por tick em laços quentes | ADR-006, ADR-007; medição em E3-3 e E8-7 |
| Evolução | novos sistemas, mundos e espécies sem reescrever o núcleo | ADR-005, hierarquia de estado completa desde o início |
| Replays/multiplayer futuros | comandos serializáveis | ADR-004 |

## 10. O que não está nesta arquitetura (Fase 1)

Névoa por país, baixa fidelidade, batalhas por parâmetros, espécies e culturas, agendas, persistência/salvar-carregar, rede e servidor. As fronteiras acima já os comportam; os ADRs correspondentes nascem na fase em que entrarem.

## 11. Questões abertas

| # | Questão | Quando decidir |
|---|---|---|
| Q1 | ~~Local dos ADRs~~ Resolvida: `docs/architecture/adr/ADR-NNN_*.md`; CLAUDE.md atualizado. | Resolvida |
| Q2 | A Sim roda na thread principal do Godot (Fase 1) ou em thread própria com snapshot? Padrão: thread principal; medir em E3-3. | E3-3 |
| Q3 | Orçamento de tick numérico (proposto: ≤ 15 ms com 300 unidades, `assumed`). | E3-3 |
| Q4 | Formato de salvar/carregar e de replay (fora da Fase 1). | Fase 2+ |
| Q5 | Infraestrutura do servidor do universo persistente. | Fase 5 |
