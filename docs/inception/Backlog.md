# Backlog da Fase 1 (Fatia vertical)

> Status: **aprovado (approved)** em 2026-11-17. Papel: PO/Analista de Negócio.
> Fluxo por feature (CLAUDE.md §8): `/proposal` → `/ai-spec` (aprovação) → `/implement` (testes primeiro) → build/test/format → `/review` → `docs/history.md`. Cada épico abaixo vira uma feature no pipeline.
> Estimativa (`assumed`, dev solo): **S** ≤ 1 dia · **M** 2–3 dias · **L** 4–7 dias · **XL** > 1 semana (deve ser quebrada). Medir o real e recalibrar (risco F1).
> Todas as histórias passam no DoR: valor claro, descrição, critérios de aceite, escopo limitado, dependências e testabilidade. Requisitos em `Requirements.md`.

## Ordem e dependências

```
E0 Fundação ─┬─ E1 Mapa/câmera ─ E2 Seleção ─ E3 Movimento ─ E4 Economia ─ E5 Construções
             │                                                          │
             └─ (dados) ────────────────────────────────── E6 IA ───────┤
                                                       E7 Troca ─ E8 Árvore, recurso oculto e combate
```

Valor primeiro: o primeiro momento que testa V1 é E7; tudo antes o habilita. Os spikes de risco ficam em E3 (desempenho), E6 (IA) e E7 (painel).

---

## E0 · Fundação (M0)

| ID | História | Critérios de aceite | Est. | Deps |
|---|---|---|---|---|
| E0-1 | Como dev, quero a solução (`Rts.sln`, `Sim`, `Sim.Tests`, projeto Godot) e `.gitignore`, para compilar e testar headless. | `dotnet build` e `dotnet test` rodam; `Sim` não referencia Godot; `Nullable` e `TreatWarningsAsErrors` ativos. | S | — |
| E0-2 | `Fixed` (Q32.32): soma, subtração, multiplicação, divisão, comparação, raiz quadrada, conversão de/para inteiro. | Testes de operações e arredondamento; resultados idênticos em duas execuções; sem `float` em `Sim`. | M | E0-1 |
| E0-3 | `SimRandom` com semente explícita. | Mesma semente ⇒ mesma sequência; sementes diferentes divergem. | S | E0-1 |
| E0-4 | `GameState` com Universo → Sistema → Mundo → País, ids fortes. | Teste cria 1 sistema/1 planeta/3 países; nenhuma API assume só um. | S | E0-1 |
| E0-5 | `Command`, fila por tick e `Simulation.Step`. | Comandos aplicados no tick pedido, em ordem estável; comando para país não controlado é rejeitado. | M | E0-4 |
| E0-6 | `IController`, `HumanController`, `AiController` (vazio). | Ambos emitem comandos pela mesma API; a Sim não os distingue. | S | E0-5 |
| E0-7 | Hash do estado e teste de determinismo. | Duas execuções com mesma semente e comandos ⇒ mesmo hash; teste falha se alguém introduz fonte de não determinismo. | M | E0-5 |
| E0-8 | Carregador e validador de `resources.json` e `techs.json` (3–5 itens de exemplo). | Rejeita id duplicado, referência quebrada, ciclo e recurso inalcançável, com mensagem clara; aceita o conjunto válido. | M | E0-1 |
| E0-9 | Cena Godot com grade isométrica placeholder, câmera e tick visível. | O jogo abre, mostra a grade e o contador de ticks a 20/s. | M | E0-5 |

**Saída do M0:** `dotnet test` verde, incluindo determinismo; o jogo abre rodando a Sim a 20 ticks/s.

## E1 · Mapa e câmera (M1)

| ID | História | Critérios de aceite | Est. | Deps |
|---|---|---|---|---|
| E1-1 | Mapa em grade 2:1 com terreno caminhável/bloqueado/água, carregado de arquivo. | Mapa de teste carrega; terreno consultável pela Sim; arquivo inválido gera erro. | M | E0-4 |
| E1-2 | Conversão tela ↔ mundo ↔ grade e depth sorting por Y. | Teste de ida e volta sem erro em pontos de borda; entidades sobrepostas desenham na ordem certa. | M | E1-1 |
| E1-3 | Depósitos de recursos posicionados na geração, inclusive os ocultos. | Todos existem no estado desde o início; ocultos não aparecem no HUD. | S | E1-1 |
| E1-4 | Câmera: bordas, WASD/setas, arrasto com botão do meio, limites, zoom mín/máx. | Câmera nunca sai do mapa; zoom respeita limites. | M | E0-9 |

## E2 · Seleção e comandos (M2)

| ID | História | Critérios de aceite | Est. | Deps |
|---|---|---|---|---|
| E2-1 | Seleção por clique, caixa e Shift, com limite configurável (12). | Selecionar 13ª unidade não excede o limite; Shift adiciona/remove. | M | E1-2 |
| E2-2 | Duplo clique/Ctrl+clique seleciona do mesmo tipo visíveis; grupos Ctrl+1–9 / 1–9. | Grupo grava e recupera; duplo toque centraliza a câmera. | M | E2-1 |
| E2-3 | Comandos Mover, Parar, Atacar-mover e clique direito contextual, com feedback visual. | Cada comando gera `Command` serializável; marcador de destino e círculo de seleção aparecem. | M | E2-1, E0-5 |

## E3 · Movimento e pathfinding (M3)

| ID | História | Critérios de aceite | Est. | Deps |
|---|---|---|---|---|
| E3-1 | A\* na grade para caminho individual. | Caminho ótimo em mapas de teste; sem caminho ⇒ comando falha com motivo. | M | E1-1, E0-2 |
| E3-2 | Desvio local e raio de colisão; amigas paradas abrem caminho. | **50 unidades atravessam um gargalo em < 20 s sem ficar presas.** | L | E3-1 |
| E3-3 | **Spike F4/F5:** 300 unidades em movimento com hash determinístico e medição de ms/tick. | Hash igual em 2 execuções; ms/tick registrado em ADR; zero alocação no laço quente (medida). | M | E3-2, E0-7 |

## E4 · Economia básica (M4)

| ID | História | Critérios de aceite | Est. | Deps |
|---|---|---|---|---|
| E4-1 | Trabalhador coleta e entrega em `dropOff`, em ciclo. | **Completa o ciclo repetidamente sem intervenção.** | M | E3-1, E0-8 |
| E4-2 | Limite de coleta por recurso por minuto. | **A coleta nunca passa do limite configurado** (teste com muitos trabalhadores). | S | E4-1 |
| E4-3 | População e moradia. | Produção bloqueada com população cheia, com motivo; moradia aumenta o limite. | S | E4-1 |
| E4-4 | Barra de recursos dinâmica. | Mostra só os recursos liberados (até 5–6), população e taxa contra o limite. | M | E4-2 |

## E5 · Construções e produção (M5)

| ID | História | Critérios de aceite | Est. | Deps |
|---|---|---|---|---|
| E5-1 | Posicionar construção com pré-visualização verde/vermelha. | Posição inválida (bloqueado, ocupado) fica vermelha e é rejeitada pela Sim. | M | E4-1 |
| E5-2 | Estágios de construção e cancelamento com reembolso de 75%. | Valor reembolsado correto, configurado em dados. | M | E5-1 |
| E5-3 | Fila de produção (5), ponto de reunião, `produces`. | A 6ª ordem é recusada; unidade nasce e vai ao ponto. | M | E5-2 |
| E5-4 | `RecipeProcessor` e papel `recipes`. | **Fundição produz exatamente 1 metal a cada 100 ticks e para quando falta entrada**, com aviso. | M | E5-2, E0-8 |
| E5-5 | Papéis `extracts` (sobre depósito do tipo certo; esgota) e `research`. | Extratora só é válida sobre depósito revelado; depósito bruto esgota. | M | E5-2 |

## E6 · IA com doutrina (M6) — spike F2

| ID | História | Critérios de aceite | Est. | Deps |
|---|---|---|---|---|
| E6-1 | Doutrina em JSON com 2 modelos (econômico, militarista) e validação. | Carrega e valida; referências a techs/unidades inexistentes são rejeitadas. | M | E0-8 |
| E6-2 | Planejador: dado um objetivo, deduz do grafo de dados o que coletar, construir ou pesquisar. | Sem ids de conteúdo no código (verificado por busca automatizada nos fontes da IA); IA monta economia e cumpre o plano de pesquisa. | XL → quebrar em E6-2a (planejamento de dependências) e E6-2b (execução por camadas) | E5-5, E6-1 |
| E6-3 | Objetivos em camadas (trabalhadores → população → pesquisa → exército → postura) e decisão a cada 10 ticks com deslocamento. | 3 IAs rodando; decisões distribuídas entre ticks (nenhum pico sistemático). | L | E6-2 |
| E6-4 | Log de decisões por país. | Cada ação da IA gera linha legível, com motivo. | S | E6-3 |
| E6-5 | Ataques em ondas conforme a doutrina. | Intervalo e tamanho vêm dos dados. | M | E6-3, E8-3 |
| E6-6 | **Experimento F2:** repetir o teste com um segundo conjunto de dados (outros ids). | A IA atinge "ter metal" nos dois conjuntos sem mudar código. | M | E6-2 |

## E7 · Vários países e troca de controle (M7) — spike U1

| ID | História | Critérios de aceite | Est. | Deps |
|---|---|---|---|---|
| E7-1 | Mundo com 2–3 países, território e capital iniciais; mesma espécie. | Cada país tem estoque, população, árvore e recursos revelados independentes. | M | E4-3 |
| E7-2 | Comando de troca de controle sem pausa. | Após a troca, visão, tecnologias, recursos revelados e estoque exibidos são do novo país; o anterior age pela doutrina no tick seguinte. | M | E7-1, E6-3 |
| E7-3 | Seletor de países no HUD (bandeira, situação) e câmera na capital. | Um clique troca; destaque do país ativo. | M | E7-2 |
| E7-4 | Registro de eventos por país. | Eventos gravados na Sim de forma determinística. | M | E7-1 |
| E7-5 | Painel "o que aconteceu" ao assumir um país. | Lista só eventos desde a última visita; testador consegue descrever o que o país fez. | M | E7-4 |
| E7-6 | Eliminação de país ao perder todas as construções. | País eliminado sai do seletor; o planeta continua com ≥1 país. | S | E7-1 |

## E8 · Árvore, recurso oculto e combate (M8)

| ID | História | Critérios de aceite | Est. | Deps |
|---|---|---|---|---|
| E8-1 | Pesquisa com fila e custo (cresce 10% por tech do tier; cancelar devolve 100%). | Valores vêm de dados; paralelo sem repetir a mesma tech. | M | E5-5, E0-8 |
| E8-2 | Handlers dos 6 efeitos da Fase 1; validador reconhece os 8. | Cada efeito tem teste próprio. **Antes de Mineração, nenhum minério é visível nem clicável; depois, todos os explorados aparecem.** | L | E8-1 |
| E8-3 | Combate: atributos, melee/ranged, aquisição de alvo, morte, incursão. | Unidade parada ataca alvo no alcance; morte remove a unidade e deixa cadáver temporário. | L | E3-2 |
| E8-4 | Tela da árvore com grafo, estados, tooltip e enfileiramento de pré-requisitos. | **Clicar numa tech de tier 3 enfileira todos os pré-requisitos na ordem certa.** | L | E8-2 |
| E8-5 | Validador: ciclo e recurso inalcançável. | **Recusa árvore com ciclo ou recurso inalcançável.** | S | E0-8 |
| E8-6 | Cadeia minério → metal completa e conjunto de dados de exemplo (5–8 techs). | A IA inimiga pesquisa, refina metal e ataca com unidades de metal. | M | E6-6, E8-2 |
| E8-7 | Partida de 30 min com 3 IAs e relatório de desempenho. | **Sem travar; cada país segue o próprio plano de pesquisa;** ms/tick registrado. | M | E8-6 |
| E8-8 | 1 unidade pelo pipeline Blender (8 direções), para validar a ferramenta. | Unidade aparece no jogo; tempo gasto registrado (F7). | M | E1-2 |

## Gate de saída da Fase 1

| ID | Item | Critério |
|---|---|---|
| G-1 | Playtest | ≥2 testadores + autor, 20 min, roteiro de 5 perguntas; ≥3 trocas deliberadas cada; descrevem o que cada país fez. |
| G-2 | Decisão V1 | Autor registra se quer continuar jogando; se "não", reavaliar antes da Fase 2. |
| G-3 | Calibragem | Tempo real por marco em `docs/history.md`; recalibrar estimativas da Fase 2. |

## Resumo

- 9 épicos (E0–E8), **48 histórias** (E0: 9 · E1: 4 · E2: 3 · E3: 3 · E4: 4 · E5: 5 · E6: 6 · E7: 6 · E8: 8), mais 3 itens de gate. E6-2 deve ser quebrada em duas antes de entrar no pipeline.
- Maior risco de estimativa: **E6-2 (planejador da IA)**. Reservar tempo para refazer.
- Primeiro feature para o pipeline: **E0 Fundação** (`/proposal Foundation`).
