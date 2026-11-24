# ADR-008: IA por planejador de encadeamento reverso sobre o grafo de dados

**Status:** Proposed
**Data:** 2026-11-17

## Contexto

A IA não pode ter ids de conteúdo fixos (ADR-005) e precisa pesquisar, refinar e atacar com qualquer conjunto de dados. É o maior risco técnico (F2, score 20). `from-source:design.md` §IA.

## Decisão

- Objetivos em camadas ponderadas pela doutrina: trabalhadores ocupados → população → plano de pesquisa → exército → postura.
- Para cada objetivo, o planejador pergunta aos dados "o que preciso?": caminha para trás por receita → construção → tech → recurso revelado, e produz a lista do que falta.
- A IA escolhe a próxima ação, emite `Command`s comuns e escreve uma linha no log de decisões com o motivo.
- Respeita a visibilidade do país (não vê depósito oculto).
- Validação: o teste de aceite roda com dois conjuntos de dados com ids diferentes (E6-6) e um teste procura ids de conteúdo nos fontes da IA.

## Consequências

- Positivo: IA funciona com qualquer conteúdo; log explica decisões ao jogador.
- Negativo: planejamento em grafo é difícil de acertar; pode gerar comportamentos estranhos com dados incomuns.
- Status Proposed: confirmar ou substituir após o spike E6-2/E6-6; se falhar, considerar uma árvore de comportamento parametrizada por papéis de dados.

## Alternativas consideradas

- Árvore de comportamento fixa com ids: rejeitada (viola ADR-005).
- Utility AI pura sem planejamento: possível, mas não resolve dependências longas (ex.: minério antes de metal).
- Aprendizado de máquina: rejeitado (quebra o determinismo; design só prevê estatística simples).
