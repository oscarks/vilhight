# Módulos

> Status: **aprovado (approved)**. Papel: Product Owner/Arquiteto. Decomposição conceitual; a arquitetura detalhada e os ADRs vêm em `/inception architecture`.
> Regras fixas: `from-source:CLAUDE.md` §4–5 (Sim sem Godot, determinismo, orientado a dados, um sistema por pasta em `src/Sim/Systems/<Sistema>/`).

## Módulos da Fase 1

| # | Módulo | Camada | Responsabilidade | Depende de | Marco |
|---|---|---|---|---|---|
| 1 | **Core** | Sim | `Fixed`, `SimRandom`, ids fortes, `Simulation.Step`, fila de comandos, hash de estado, eventos | — | M0 |
| 2 | **Data** | Sim | Carregador e validador JSON (ids únicos, referências, ciclos, alcançabilidade), registries | Core | M0 |
| 3 | **State** | Sim | `GameState`: Universo→Sistema→Mundo→País; estoque, população, árvore e recursos revelados por país | Core | M0 |
| 4 | **Control** | Sim | `IController`, `HumanController`, `AiController`, rejeição de comandos por `CountryId`, troca de controle | Core, State | M0/M7 |
| 5 | **Map** | Sim | Grade, terreno, depósitos | State | M1 |
| 6 | **Movement** | Sim | A\*, desvio local, colisão | Map | M3 |
| 7 | **Economy** | Sim | Coleta, entrega, população, limites de coleta | Data, Map | M4 |
| 8 | **Production** | Sim | Construções, fila, receitas (`RecipeProcessor`), pesquisa em construção | Economy | M5 |
| 9 | **Tech** | Sim | Grafo, fila de pesquisa, handlers de efeito (um por tipo) | Data, Production | M8 |
| 10 | **Combat** | Sim | Atributos, aquisição de alvo, incursão, morte | Movement | M8 |
| 11 | **AI** | Sim | Doutrina, planejador por grafo de dados, log de decisões | Control, Tech, Economy, Combat | M6 |
| 12 | **Events** | Sim | Registro de eventos por país para "o que aconteceu" | State | M7 |
| 13 | **Presentation** | Godot (`game/scripts`) | Mapa isométrico, câmera, seleção, HUD, seletor de países, tela da árvore, painel "o que aconteceu"; nenhuma regra de jogo | todos via estado/comandos | M0–M8 |
| 14 | **Tools** | tools/ | `DataValidator` (opcional), scripts Blender (só 1 unidade para validar o pipeline, risco F7) | Data | M8 |

## Módulos adiados (fora da Fase 1)

| Módulo | Fase |
|---|---|
| Fog (névoa por país) | 2 |
| Battle (resolução por parâmetros, limite de combatentes) | 2 |
| Species/Culture | 2 |
| LowFidelity (baixa fidelidade, "sair e continuar simulando") | 2 |
| Milestones/Score, Audio, Menu/Saves | 2 |
| Agendas, Doctrine Editor, Learning, Stability, Diplomacy, Legacy, Crises | 3 |
| Space/Ship/Travel | 4 |
| Server/Multiplayer | 5 |

## Fronteiras importantes

- **Presentation → Sim:** só envia `Command` e lê `GameState`/eventos. Nunca altera estado.
- **AI → Sim:** só emite comandos pela mesma API do humano; nenhum id de conteúdo no código.
- **Data → resto:** conteúdo (recursos, techs, construções, unidades, doutrinas) só entra por JSON validado em `game/data/`.
- **Spikes de risco:** AI (F2) em M6; painel "o que aconteceu" (U1) em M7; desempenho/determinismo (F4/F5) medido já em M3.
