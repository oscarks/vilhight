# Requisitos da Fase 1 (Fatia vertical)

> Status: **aprovado (approved)** em 2026-11-17. Papel: Analista de Negócio/PO.
> Base: `Scope_and_Phasing.md` §4 (aprovado), `Modules.md`, `sources/design.md`, `CLAUDE.md`.
> Rastreio: cada requisito cita a seção do design. Valores numéricos são iniciais e ficam em dados (`game/data/`), nunca no código.
> Conteúdo fora da Fase 1 não aparece aqui.

## 1. Requisitos funcionais

### Fundação e dados
| ID | Requisito | Design |
|---|---|---|
| RF-01 | A Sim avança em ticks fixos de 20/s via `Simulation.Step(commands)`; a renderização interpola. | Diretrizes técnicas |
| RF-02 | Toda ação (jogador ou IA) é um comando serializável com `CountryId`, aplicado num tick. | Arquitetura |
| RF-03 | A Sim rejeita comandos para países que o emissor não controla. | Arquitetura |
| RF-04 | O `GameState` tem a hierarquia Universo → Sistema → Mundo → País; nenhum código assume 1 sistema/1 planeta. | Arquitetura |
| RF-05 | Humano e IA implementam `IController`; a Sim não distingue quem joga. | Arquitetura |
| RF-06 | Recursos, receitas, techs, construções, unidades e doutrinas vêm de JSON validado no carregamento: ids únicos, referências existentes, árvore sem ciclos, todo recurso e construção alcançável. | Modelo de dados |
| RF-07 | Existe hash do estado; mesma semente + mesmos comandos ⇒ mesmo hash. | Aceite; CLAUDE.md §5.4 |

### Mapa, câmera, seleção
| ID | Requisito | Design |
|---|---|---|
| RF-10 | Grade isométrica 2:1 com terreno caminhável, bloqueado e água; conversão tela↔mundo↔grade; depth sorting por Y. | Mapa |
| RF-11 | Câmera por bordas, WASD/setas e arrasto com botão do meio; limites do mapa e zoom mín/máx. | Câmera |
| RF-12 | Seleção por clique, caixa, Shift; duplo clique/Ctrl+clique seleciona o mesmo tipo visível; limite de seleção configurável (padrão 12); grupos de controle (Ctrl+1–9 / 1–9). | Seleção |
| RF-13 | Comandos Mover, Parar, Atacar-mover e clique direito contextual (mover, atacar, coletar, construir). Feedback: círculo, barra de vida, marcador de destino. | Seleção |

### Movimento
| ID | Requisito | Design |
|---|---|---|
| RF-20 | A\* individual na grade; desvio local; raio de colisão; unidades amigas paradas abrem caminho. | Movimento |

### Economia e produção
| ID | Requisito | Design |
|---|---|---|
| RF-30 | Trabalhadores coletam recursos básicos e entregam em construção `dropOff`, em ciclo sem intervenção. | Economia |
| RF-31 | Limite de coleta por recurso (por minuto), elevável por tecnologia. | Economia |
| RF-32 | População limitada por construções `housing`. | Economia |
| RF-33 | Conhecimento existe desde o início, é gerado por construções/trabalhadores e paga tecnologias. | Economia |
| RF-34 | Recurso oculto (minério) só aparece, é clicável e é visível para a IA do país depois da tecnologia que o revela. Ver e extrair são separados (`revealResource` vs `unlockBuilding`). | Economia |
| RF-35 | Construção `extracts` só fica válida sobre um depósito revelado do tipo certo; depósitos brutos se esgotam. | Construções |
| RF-36 | Receitas (`RecipeProcessor`): consome entradas, gasta ticks, entrega a saída; para com aviso quando falta entrada ou o estoque está cheio. Ex.: fundição 2 minério + 1 madeira → 1 metal em 100 ticks. | Economia |
| RF-37 | Barra de recursos dinâmica: só recursos liberados, até 5–6 ao mesmo tempo, com população e taxa contra o limite de coleta. | Economia/HUD |
| RF-40 | Construção posicionada com pré-visualização verde/vermelha, em estágios, cancelável com reembolso de 75%. | Construções |
| RF-41 | Fila de produção de até 5 itens com progresso; ponto de reunião. | Construções |
| RF-42 | Papéis de construção combináveis por dados: `produces`, `recipes`, `research`, `extracts`, `dropOff`, `housing`. | Construções |

### Árvore de tecnologia
| ID | Requisito | Design |
|---|---|---|
| RF-50 | Cada tech tem `id`, `tier`, `cost`, `researchTicks`, `requires`, `effects`. Pesquisa em construções de pesquisa, uma por vez com fila; várias em paralelo, nunca a mesma tech. | Árvore |
| RF-51 | Custo pago ao enfileirar; cancelar devolve 100%; custo cresce 10% por tech já pesquisada no mesmo tier. | Árvore |
| RF-52 | Efeitos: um handler por tipo. Na Fase 1 operam `revealResource`, `unlockBuilding`, `unlockUnit`, `unlockRecipe`, `modifyStat`, `raiseGatherCap`. O validador reconhece também `replaceRequirement` e `obsolete`, sem implementação de runtime. | Árvore; decisão P4 |
| RF-53 | Tela da árvore com grafo inteiro, pan/zoom, dependências, estados (pesquisada, disponível, bloqueada, em pesquisa) e tooltip com custo, tempo e o que libera. | Interface da árvore |
| RF-54 | Clicar numa tech distante enfileira os pré-requisitos que faltam, na ordem certa. | Interface da árvore; Aceite |
| RF-55 | A era exibida é um rótulo calculado, nunca uma trava. | Árvore |

### Países, controle e IA
| ID | Requisito | Design |
|---|---|---|
| RF-60 | Um planeta tem 2–3 países na Fase 1, todos da mesma espécie. Estoque, população, árvore e recursos revelados são do país. | Planeta; Arquitetura |
| RF-61 | Seletor de países no HUD (bandeira, situação); um clique assume o país e leva a câmera à capital; a visão passa a ser a do país assumido. | Troca de controle |
| RF-62 | O país deixado recebe a IA com a própria doutrina, sem pausa; assumir no meio de uma ação não quebra nada. | Troca de controle; IA |
| RF-63 | Painel "o que aconteceu" ao assumir: eventos do país desde a última visita (construções, pesquisas concluídas, perdas, mudanças relevantes de estoque). | Troca de controle |
| RF-64 | Doutrina em dados: pesos (economia, militar, tecnologia, expansão), plano de pesquisa, postura por país (aliado, neutro, rival, alvo), composição de exército. Modelos: econômico, militarista. | Doutrina |
| RF-65 | A IA decide a cada 10 ticks, com deslocamento entre países; objetivos em camadas: trabalhadores ocupados → população → plano de pesquisa → exército → postura. | IA |
| RF-66 | **Nenhum id de conteúdo no código da IA**: ela consulta os dados para saber o que precisa para o próximo objetivo. Respeita a visibilidade de recursos ocultos do país. | IA; CLAUDE.md §5.5 |
| RF-67 | Log de decisões da IA por país, legível pelo jogador. | IA |
| RF-68 | Ataques em ondas com intervalo e tamanho definidos pela doutrina. | IA |

### Combate
| ID | Requisito | Design |
|---|---|---|
| RF-70 | Atributos de unidade: vida, armadura, dano, alcance, cadência, velocidade, visão, custo, tempo de produção, população. | Unidades |
| RF-71 | Corpo a corpo e à distância; aquisição automática de alvo quando parada; prioridade a quem ataca; morte com animação e cadáver temporário. | Unidades |
| RF-72 | Todo confronto é incursão (controle em tempo real). Não há limite de combatentes nem batalha na Fase 1. | Decisão de escopo |
| RF-73 | Um país é eliminado ao perder todas as construções; o planeta continua enquanto restar um país. | Fluxo de partida |

## 2. Requisitos não funcionais

| ID | Requisito | Fonte |
|---|---|---|
| RNF-01 | `src/Sim` não referencia Godot; sem `float`/`double`, `System.Random`, relógio de parede, iteração de coleção sem ordem definida, estado estático mutável ou threads na lógica. | CLAUDE.md §5.1, 5.4 |
| RNF-02 | `Nullable` habilitado e `TreatWarningsAsErrors` em `Sim` e `Sim.Tests`. Ids fortes (`CountryId`, etc.). | CLAUDE.md §6 |
| RNF-03 | Desempenho: 300 unidades dentro do orçamento de tick; meta de 300–500 a 60 FPS; sem alocação por tick em laços quentes (movimento, combate, pathfinding). O número medido é registrado. | Design; CLAUDE.md §5.6 |
| RNF-04 | 3 países de IA rodam 30 min simulados sem travar. | Aceite |
| RNF-05 | Dados novos sempre passam pelo validador; falha de validação impede o carregamento com mensagem clara. | CLAUDE.md §8 |
| RNF-06 | Apresentação sem regra de jogo; só envia comandos, lê estado e reage a eventos. | CLAUDE.md §5.1 |
| RNF-07 | `dotnet build`, `dotnet test` e `dotnet format` sem erros antes de cada commit. | CLAUDE.md §7–8 |
| RNF-08 | Arte provisória gerada por código; no máximo 1 unidade pelo pipeline Blender, para validar a ferramenta (risco F7). | Risk_Analysis F7 |

## 3. Regras de negócio e exceções

- **Troca de país:** não pausa a Sim. A troca é por comando; o `HumanController` passa a emitir comandos para o novo país e a IA reassume o anterior no tick seguinte.
- **Comando inválido:** rejeitado sem alterar estado; gera evento de rejeição legível (sem exceção).
- **Ordem sem recurso suficiente / população cheia / estoque cheio:** o comando falha com motivo; a receita fica parada com aviso.
- **Tech enfileirada com pré-requisito faltando:** os pré-requisitos entram antes, na ordem topológica.
- **Depósito esgotado:** a construção extratora para; recurso renovável não esgota.
- **Dados inválidos:** ciclo, id duplicado, referência quebrada ou recurso inalcançável ⇒ erro de carga.

## 4. Rastreabilidade com o risco

| Risco | Requisitos que o reduzem |
|---|---|
| V1 diferencial divertido | RF-61, RF-62, RF-63, playtest (Scope §4.4) |
| F1/B1 escopo e abandono | Fora de escopo (Scope §4.2), registro de tempo por marco |
| F2 IA genérica | RF-65, RF-66 e AC de dois conjuntos de dados |
| F4/F5 determinismo e desempenho | RF-07, RNF-01, RNF-03 |
| U1 sobrecarga cognitiva | RF-61, RF-63, RF-67 |
