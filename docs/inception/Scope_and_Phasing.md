# Escopo e Fases

> Status: **aprovado (approved)** em 2026-11-17. Papel: Product Owner.
> Base: `Risk_Analysis.md` (aprovado; decisões D1–D7 e fatia vertical da §6) e `sources/design.md`.

## 1. Escopo como hipótese operacionalizada

| Eixo | Definição |
|---|---|
| **Segmento** | Primeiro e único usuário: o próprio autor (dev solo) e 2–3 jogadores de RTS/4X convidados para playtest. Sem público comercial (contexto: projeto pessoal/estudo). |
| **Dor/risco** | Maior risco: V1 (alternar entre países com IA é divertido?) + F1/B1 (escopo solo e abandono) + F2 (IA genérica por dados). `from-source:Risk_Analysis.md` |
| **Situação** | Partida num planeta com 2–3 países; o jogador controla um país e troca para outro quando quiser, enquanto os demais seguem a doutrina. |
| **Resultado** | O jogador toma decisões diferentes ao assumir cada país e entende o que aconteceu enquanto esteve fora. |
| **Solução mínima** | Fatia vertical jogável (Fase 1 abaixo), sem espécies, culturas, agendas, batalhas por parâmetros, névoa nem baixa fidelidade. |

**Hipótese de valor:** *Se* oferecermos um planeta com vários países em que o jogador alterna o controle e os outros seguem uma doutrina → *o jogador* tem amplitude estratégica sem microgerenciar tudo → *com impacto* mensurável: em 20 min de jogo, o testador troca de país de forma deliberada ≥ 3 vezes e consegue dizer o que cada país fez enquanto estava fora.

## 2. Checklist de validação (adaptado: projeto de jogo)

| Pergunta | Resposta |
|---|---|
| Resolve uma dor crítica? | Testa a hipótese central do jogo (V1). Sim. |
| Usável sozinho? | Sim: o jogador joga contra os próprios países (design: fase 1). |
| Pago? | N/A (projeto pessoal). |
| Supera um concorrente? | Não é objetivo da Fase 1; só validar o diferencial. `assumed` (sem pesquisa de mercado) |
| Valor mensurável? | Sim: critérios de saída da §4. |
| Reduz risco/custo? | Sim: ataca V1, F1, F2, U1. |
| Uso recorrente? | Sim, a partida repete o laço de troca de países. |

## 3. Fases

Somente a **Fase 1** é detalhada por completo. As demais são marcos de direção.

| Fase | Nome | Marcos do CLAUDE.md | Resumo |
|---|---|---|---|
| **1** | **Fatia vertical** | 0–8, mais fatias dos marcos 9, 10 e 12 | Ver §4. |
| 2 | Fase 1 do design completa | 9–14 restantes | Receitas e limites de coleta completos, névoa por país, batalhas por parâmetros, espécies e culturas, interface completa, marcos e pontuação, áudio, "sair e continuar simulando". |
| 3 | Profundidade de sistemas | 15–16 | Agendas por país (versão simplificada primeiro), editor de doutrina, aprendizado pelo histórico, estabilidade, união diplomática, corrida espacial, crises e Legado. |
| 4 | Viagem entre mundos | 17 | Ramo espacial, nave, andares, eventos. Fase 2 do jogo (sistema estelar). |
| 5 | Galáxia | fora do roteiro | Dobra, multiplayer, universo persistente. Depende da questão aberta de infraestrutura do servidor. |

## 4. Fase 1: Fatia vertical (completa)

### 4.1 Dentro

| Área | Entra | Referência (design) |
|---|---|---|
| Fundação | `Fixed`, `SimRandom`, ticks a 20/s, comandos com `CountryId`, `GameState` (Universo→Sistema→Mundo→País), `IController`, hash de estado, teste de determinismo, carregador/validador JSON | Diretrizes técnicas; Arquitetura |
| Mapa | Grade isométrica 2:1, terreno caminhável/bloqueado/água, depth sorting, câmera (bordas, WASD, arrasto, zoom, limites) | Mapa e câmera |
| Controle | Seleção (clique, caixa, Shift, grupos de controle), comandos Mover/Parar/Atacar-mover, clique direito contextual | Seleção e controle |
| Movimento | A\* na grade, desvio local, raio de colisão | Movimento e pathfinding |
| Economia | Coleta, entrega, população e moradia, limite de coleta por recurso, barra de recursos dinâmica | Economia (regras MVP) |
| Construções | Posicionar com pré-visualização, estágios, fila de produção (5), ponto de reunião, `produces`, `dropOff`, `housing`, `research`, `extracts`, `recipes` | Construções |
| Recursos | Básicos (comida, madeira, pedra), conhecimento, **1 cadeia oculta**: minério (revelado) → metal (receita) | Economia (progressão tiers 0–2) |
| Árvore | Grafo de ≈5–8 techs, pesquisa com fila, 8 tipos de efeito (um handler por tipo; na Fase 1 só os que a fatia usa, ver §4.3), tela da árvore com pan/zoom e estados | Árvore de tecnologia |
| IA | Doutrina em dados (pesos, plano de pesquisa, postura, exército), 2 modelos prontos (econômico, militarista), IA sem ids fixos, decide a cada 10 ticks, log de decisões por país | IA dos países; Doutrina |
| Países | 2–3 países, 1 espécie, seletor de países no HUD, troca de controle sem pausa, painel "o que aconteceu" mínimo | Troca de controle |
| Combate | Atributos de unidade, melee/ranged, aquisição automática de alvo, morte, **só incursões** (sem limite de 30 nem batalha) | Unidades e combate |
| Fim de partida | País eliminado ao perder todas as construções (design: MVP) | Fluxo de partida |
| Qualidade | `dotnet test` verde, hash determinístico, validação de dados | CLAUDE.md §8 |

### 4.2 Fora (explícito)

Espécies adicionais e culturas · agendas, marcos, pontuação, Legado, estabilidade · batalhas por parâmetros e limite de 30 combatentes · névoa de guerra · baixa fidelidade e "sair e continuar simulando" · cota estratégica (cristal) · dano×armadura, habilidades, elevação · anexação (decisão P1) · editor de doutrina e aprendizado · diplomacia · minimapa · áudio · menu principal e escolha de mapa · salvar/carregar · fases 2 e 3 do jogo · arte final.

### 4.3 Simplificações da Fase 1 (alinhadas ao risco D2–D7)

- Efeitos de tecnologia implementados: `revealResource`, `unlockBuilding`, `unlockRecipe`, `unlockUnit`, `modifyStat`, `raiseGatherCap`. Ficam para depois: `replaceRequirement`, `obsolete`. O validador conhece os 8 tipos desde já.
- Visibilidade de recurso oculto é por país (`revealedBy`), sem névoa.
- "O que aconteceu": lista de eventos do país desde a última visita (construções, pesquisas concluídas, perdas, mudança de estoque relevante). Sem agendas.
- Combate: incursão sem teto. Todo confronto é em tempo real.

### 4.4 Critérios de saída da Fase 1

Todos os critérios são verificáveis. Os aceites vêm do design, em subconjunto.

**Técnicos**
- [ ] `dotnet build`, `dotnet test`, `dotnet format` sem erros. `from-source:CLAUDE.md`
- [ ] Duas simulações com a mesma semente e os mesmos comandos terminam no mesmo hash. `from-source:design.md` §Aceite
- [ ] 50 unidades atravessam um gargalo em <20 s sem ficar presas. `from-source:design.md`
- [ ] Um trabalhador completa o ciclo de coleta repetidamente sem intervenção.
- [ ] Antes de Mineração, nenhum depósito de minério é visível nem clicável; depois, todos os explorados aparecem.
- [ ] A fundição produz exatamente 1 metal a cada 100 ticks e para quando falta entrada.
- [ ] A coleta nunca passa do limite configurado.
- [ ] O validador recusa árvore com ciclo ou recurso inalcançável.
- [ ] A IA pesquisa, refina metal e ataca com unidades de metal **sem id de conteúdo fixo no código**, e continua funcionando com um segundo conjunto de dados (experimento F2).
- [ ] Trocar de país muda tecnologias, recursos revelados e estoque exibidos; o país anterior continua agindo pela doutrina no tick seguinte.
- [ ] Um comando com `CountryId` não controlado pelo emissor é rejeitado.
- [ ] 3 países de IA rodam 30 min simulados sem travar, cada um seguindo o próprio plano de pesquisa.
- [ ] Desempenho medido e registrado: ms/tick com 300 unidades (meta bem abaixo de 50 ms; orçamento final em ADR).

**De produto (hipótese de valor)**
- [ ] Playtest com ≥2 pessoas além do autor, 20 min cada, roteiro de 5 perguntas.
- [ ] Cada testador troca de país deliberadamente ≥3 vezes e descreve o que cada país fez enquanto esteve fora.
- [ ] O autor registra se quer continuar jogando (sim/não e por quê). Se "não", reavaliar V1 antes da Fase 2.
- [ ] Tempo real gasto por marco anotado em `docs/history.md`, para calibrar a Fase 2 (F1/B1).

### 4.5 Marcos internos

| Marco | Entrega | Saída |
|---|---|---|
| M0 | Fundação, determinismo, JSON de recursos/techs, cena com grade e tick visível | `dotnet test` verde; mapa roda a Sim a 20 ticks/s |
| M1–M3 | Mapa/câmera, seleção, movimento | Aceite do gargalo |
| M4–M5 | Economia, construções e produção | Ciclo de coleta; fila de produção |
| M6 | IA com doutrina (**spike F2 aqui**) | IA joga uma economia sozinha sem ids fixos |
| M7 | Vários países e troca de controle (**spike U1: painel "o que aconteceu"**) | Aceite de troca |
| M8 | Árvore e conhecimento, 1 cadeia oculta, incursões | Todos os critérios da §4.4; playtest |

## 5. Decisões tomadas (2026-11-17)

- **P1:** anexação fica fora da Fase 1 (vai para a Fase 2). A Fase 1 só elimina o país ao perder todas as construções.
- **P2:** névoa de guerra fora da Fase 1 (Fase 2). A IA vê o mapa inteiro, mas respeita a visibilidade de recursos ocultos por país.
- **P3:** o bloco "De produto" da §4.4 (playtest) é a porta de saída da Fase 1.
- **P4:** `replaceRequirement` e `obsolete` ficam fora da Fase 1; o validador já conhece os 8 tipos.
