# Análise de Riscos e Mitigação

> Status: **aprovado (approved)** em 2026-11-17. Decisões do humano na §8.
> Papéis: Arquiteto (viabilidade), PO (valor, usabilidade), Analista de Negócio (negócio).
> Contexto: projeto pessoal/estudo, desenvolvimento solo. Mercado/negócio ficam leves.
> Rastreabilidade: `from-source:design.md` = vem do Documento de Design; `assumed` = estimativa nossa, não verificada; `from-source:CLAUDE.md` = regra do repositório.
> Nota: nenhuma pesquisa web foi feita nesta etapa. Comparações com outros jogos são conhecimento geral (`assumed`).

## 1. Método

Quatro riscos de produto (Cagan/SVPG): **Valor**, **Usabilidade**, **Viabilidade**, **Negócio**.
Pontuação: Probabilidade (P) e Impacto (I) de 1 a 5; **Score = P × I**. Impacto considera irreversibilidade e custo do erro. Atacar primeiro score ≥ 15.

## 2. Inconsistências no design (achados de fonte)

Estas são incertezas já visíveis no próprio documento. Precisam de decisão antes do escopo.

| # | Achado | Fonte |
|---|---|---|
| D1 | **Determinismo é `[Depois]` no design, mas obrigatório desde o 1º commit no CLAUDE.md**, e há critério de aceite "mesma semente → mesmo estado". Decidir qual vale. | design.md §Diretrizes técnicas; CLAUDE.md §5.4 |
| D2 | **"Estabilidade" é usada em agendas `[MVP]`** (custos, bônus, desfechos), mas o sistema de estabilidade é `[Depois]` (passo 17). | design.md §Agendas vs §União de países |
| D3 | **Legado** é recompensa de agenda e marco `[MVP]`, mas o sistema de Legado é `[Depois]`. | design.md §Metas da fase 1 |
| D4 | Cálculo de batalha `[MVP]` usa tipos de dano/armadura, elevação e cota estratégica, todos `[Depois]`. Habilidades "automáticas em batalha" `[MVP]` também dependem de habilidades `[Depois]`. | design.md §Batalha vs §Unidades |
| D5 | "Batalhas entre jogadores na fase 3" está marcado `[MVP]`, apesar de a fase 3 ser `[Depois]`. | design.md §Batalha |
| D6 | A progressão de exemplo e as agendas usam **cristal (cota estratégica)**, mas a cota é `[Depois]`. | design.md §Economia |
| D7 | Marco de exemplo "primeira união de países" `[MVP]`, mas a união diplomática é `[Depois]` (só anexação por conquista é `[MVP]`). | design.md §Marcos |

**Leitura geral:** a tag `[MVP]` cobre cerca de 15 dos 18 passos do roteiro, incluindo espécies, culturas, agendas, batalhas por parâmetros, fidelidade baixa e painel "o que aconteceu". Isso não é um MVP mínimo (ver R1).

## 3. Registro de riscos

### Valor (as pessoas vão querer/se divertir?)

| ID | Risco | P | I | Score | Atribuição |
|---|---|---|---|---|---|
| **V1** | **O diferencial central pode não ser divertido**: alternar entre países com IA por doutrina pode virar microgerenciamento de 6 bases, ou o jogo pode virar "assistir a IA". Sem adversário externo na fase 1 ("joga contra si mesmo"), falta tensão. | 4 | 5 | **20** | `from-source:design.md` §Fases; avaliação `assumed` |
| V2 | A árvore livre sem eras pode gerar decisões sem consequência perceptível (todo caminho converge). | 3 | 4 | 12 | `assumed` |
| V3 | Agendas por país e Legado não têm efeito na fase 1 (Legado só vale na fase 2, `[Depois]`); a motivação de longo prazo pode ser fraca. | 3 | 3 | 9 | `from-source:design.md` §Legado |

### Usabilidade (conseguem usar?)

| ID | Risco | P | I | Score | Atribuição |
|---|---|---|---|---|---|
| **U1** | **Sobrecarga cognitiva**: 6–8 países, doutrinas, agendas, árvore, recursos ocultos, incursão vs batalha. O jogador pode não saber o que cada país está fazendo ao assumi-lo. | 4 | 4 | **16** | `assumed` |
| U2 | Regra de incursão (≤30) vs batalha (>30, só assistida, bloqueia troca de país) é um limite artificial; o jogador pode sentir perda de controle no clímax. | 3 | 4 | 12 | `from-source:design.md` §Incursões; `assumed` |
| U3 | Recursos ocultos revelados por tecnologia exigem boa comunicação (aviso, ping no minimapa) ou o jogador não percebe o que mudou. | 2 | 3 | 6 | `from-source:design.md` §Economia |

### Viabilidade (conseguimos construir?)

| ID | Risco | P | I | Score | Atribuição |
|---|---|---|---|---|---|
| **F1** | **Escopo desproporcional para desenvolvedor solo**: ver §2. 18 sistemas, 3 espécies com arte própria, 3 escalas de jogo. Chance alta de nunca chegar a um protótipo jogável. | 5 | 5 | **25** | `from-source:design.md`; `assumed` |
| **F2** | **IA genérica orientada a dados**: nenhum id fixo no código, a IA deduz "do que preciso para o próximo objetivo" navegando o grafo de tecnologias/receitas/recursos ocultos. É planejamento em grafo, difícil de acertar e de depurar. Critério: a IA pesquisa, refina metal e ataca sem regra fixa. | 4 | 5 | **20** | `from-source:design.md` §IA; CLAUDE.md §5.5 |
| **F3** | **Duas simulações (alta e baixa fidelidade)** que precisam ser coerentes e determinísticas. "8 h fora → recuperar em <30 s" = ~576 mil ticks; exige modelo agregado separado, que duplica regras de economia, pesquisa e batalha. | 4 | 4 | 16 | `from-source:design.md` §Fluxo; cálculo `assumed` |
| **F4** | **Determinismo com ponto fixo (Q32.32)** para 300–500 unidades: pathfinding, steering/RVO, raiz quadrada, distâncias e interpolação para o Godot sem `float`. Fácil quebrar o determinismo por acidente. | 3 | 4 | 12 | CLAUDE.md §5.4 |
| F5 | **Desempenho**: 300–500 unidades a 60 FPS, A\* individual + desvio local, 20 ticks/s, sem alocação por tick, em C#. | 3 | 4 | 12 | `from-source:design.md` §Diretrizes |
| F6 | **Balanceamento combinatório**: árvore livre × recursos ocultos × limites de coleta × 3 espécies × cultura. Validação de alcançabilidade ajuda, mas não garante equilíbrio. | 4 | 3 | 12 | `assumed` |
| F7 | **Arte**: 3 espécies, sprites isométricos em 8 direções via Blender para solo dev. | 4 | 3 | 12 | `from-source:design.md` §Espécies; CLAUDE.md §3 |
| F8 | Modelo de dados (JSON validado) pode crescer a um DSL difícil de manter (efeitos, condições de agenda, métricas com vetores de pesos). | 3 | 3 | 9 | `from-source:design.md` §Agendas |

### Negócio (faz sentido?)

| ID | Risco | P | I | Score | Atribuição |
|---|---|---|---|---|---|
| **B1** | **Esgotamento/abandono do projeto** (tempo, motivação) antes de haver algo jogável; é o risco de negócio principal em projeto pessoal. | 4 | 5 | 20 | `assumed` |
| B2 | Servidor do universo persistente (fase 3) sem decisão de infraestrutura e custo. Bloqueia só a fase 3. | 3 | 2 | 6 | `from-source:design.md` §Questões em aberto |
| B3 | Referências (Warcraft, StarCraft, Civilization) como inspiração: risco de conteúdo/nomes derivados. O design já exige conteúdo original. | 1 | 3 | 3 | `from-source:design.md` §Visão geral |

## 4. Priorização

Ordem de ataque (score decrescente, desempate por irreversibilidade):

1. **F1** (25) escopo solo
2. **V1** (20) o diferencial é divertido?
3. **F2** (20) IA genérica por dados
4. **B1** (20) abandono do projeto
5. **U1** (16) sobrecarga cognitiva
6. **F3** (16) baixa fidelidade coerente
7. Demais (F4, F5, F6, F7, V2, U2 = 12), depois os de score <10

Observação: F1 e B1 são o mesmo problema visto por dois ângulos. A mitigação é a mesma: recorte agressivo + algo jogável cedo.

## 5. Experimentos de mitigação

| Risco | Experimento | Sucesso se | Custo |
|---|---|---|---|
| F1, B1 | **Fatia vertical mínima** (ver §6) com placeholders, sem espécies, sem agendas, sem batalhas. Medir tempo real gasto por marco. | Jogável em N semanas; estimativa realista dos marcos seguintes. | Baixo/médio |
| V1 | **Playtest de papel/protótipo**: 2 países, 1 doutrina simples, troca de controle. Jogar 20 min e anotar se a troca gera decisões interessantes ou tédio. | Você quer continuar jogando; ≥1 momento de "agora vou assumir B porque…". | Baixo |
| V1 | **Teste com 2–3 jogadores de RTS/4X** (amigos) com o protótipo acima, roteiro de 5 perguntas. | Maioria entende a mecânica de troca sem explicação longa. | Baixo |
| F2 | **Spike da IA**: com 5 techs/recursos de exemplo, a IA atinge "ter metal" sem ids fixos (planejamento de trás para frente no grafo). Depois trocar o JSON e verificar que ainda funciona. | Critério de aceite "IA refina metal" passa com dois conjuntos de dados diferentes. | Médio |
| F3 | **Spike de baixa fidelidade**: modelo agregado de economia + pesquisa; comparar com a alta fidelidade em 10 min de jogo e medir tempo para 8 h simuladas. | Divergência dentro de tolerância definida; 8 h em <30 s. | Médio |
| F4, F5 | **Spike de pathfinding determinístico**: 300–500 unidades em ponto fixo, hash de estado igual em 2 execuções, medir ms/tick. | Hash igual; tick < orçamento (50 ms a 20 ticks/s, meta bem menor). | Médio |
| U1 | **Protótipo do seletor de países + painel "o que aconteceu"** antes de implementar toda a lógica, com dados falsos. | Testador diz o que cada país fazia ao assumi-lo. | Baixo |
| F6 | Validador de dados + simulação headless de 30 min com IA em 3 sementes; relatório de tempo até cada marco. | Sem caminho trivialmente dominante. | Médio |
| F7 | Placeholder por código primeiro; testar pipeline Blender com **1** espécie e 1 unidade. | 1 unidade em 8 direções no jogo em ≤1 dia de trabalho. | Baixo |

## 6. Reenquadramento do MVP como experimento

> Proposta (não decidida): o MVP é o experimento que reduz **V1 + F1 + F2** de uma vez.

**Pergunta do experimento:** *trocar entre países controlados por IA de doutrina gera jogo interessante, e conseguimos construir isso sozinhos em tempo razoável?*

**Fatia candidata:** 1 espécie, 2–3 países, grade isométrica, coleta/entrega/população, 1 cadeia de recurso oculto (minério → metal), árvore pequena (≈5–8 techs), IA por doutrina (2 modelos), troca de controle com painel "o que aconteceu", combate só como incursão. Fora: agendas, culturas, espécies extras, batalhas por parâmetros, baixa fidelidade, estabilidade, Legado, fases 2/3.

Isso coincide com os Marcos 0 a 8 do CLAUDE.md. A decisão formal fica para `/inception scope`.

## 7. Decisões pendentes para o humano

1. **D1**: determinismo obrigatório desde o início (CLAUDE.md) ou `[Depois]` (design)? Recomendação: obrigatório. Custa pouco no início e muito para retroagir. Atualizar o design.
2. **D2–D7**: resolver as marcações `[MVP]`/`[Depois]` conflitantes (por padrão, rebaixar para `[Depois]` o que depende de `[Depois]`).
3. Confirmar a fatia candidata da §6 como base do escopo.
4. Quais riscos você considera fora do alcance desta inception (ex.: B2 só na fase 3)?

## 8. Decisões tomadas (2026-11-17)

| Item | Decisão |
|---|---|
| D1 | Determinismo **obrigatório desde o início** (vale o CLAUDE.md). O design deve ser atualizado: sai de `[Depois]` para `[MVP]`. |
| D2–D7 | Itens `[MVP]` que dependem de sistemas `[Depois]` são **rebaixados para `[Depois]`**: efeitos de estabilidade nas agendas, recompensa em Legado, cota estratégica (cristal), dano×armadura, elevação e habilidades no cálculo de batalha, batalhas entre jogadores, marco de "união de países". Onde a agenda ou a batalha continuarem no MVP, entram em versão simplificada, definida em `/inception scope`. |
| §6 | **Fatia vertical aprovada como base do escopo**: 1 espécie, 2–3 países, grade isométrica, coleta/entrega/população, 1 cadeia de recurso oculto, árvore pequena, IA por doutrina, troca de controle com painel "o que aconteceu", só incursões. |
