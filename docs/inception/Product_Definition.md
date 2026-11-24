# Definição do Produto

> Status: **aprovado (finalizado)** em 2026-11-17. Papel: PO.
> Consolida o que foi decidido na inception. As etapas de pesquisa de mercado e de discovery com usuários **não foram executadas** (projeto pessoal/estudo); as afirmações de produto abaixo vêm de `sources/design.md` ou são `assumed`.

## Resumo em uma frase

RTS isométrico (Godot 4 + C#) em que o jogador possui um planeta com vários países, alterna o controle entre eles e deixa os demais seguirem uma doutrina, com recursos que surgem e mudam conforme o conhecimento avança. `from-source:design.md`

## Contexto e objetivo

- Projeto pessoal/estudo, desenvolvimento solo, sem meta comercial.
- Objetivo desta inception: validar e recortar o MVP a partir do design, reduzindo o risco de escopo e de o diferencial não funcionar.
- Referências de mecânica (Warcraft, StarCraft, Civilization); conteúdo, nomes e facções são originais. `from-source:design.md`

## Segmento e usuário

| Item | Definição |
|---|---|
| ICP | Jogador de RTS clássico ou de 4X que gosta de planejar em escala e não quer microgerenciar tudo o tempo todo. `assumed` |
| Persona primária | O próprio autor, na Fase 1 (usuário e desenvolvedor). |
| Beachhead | Autor + 2–3 jogadores convidados para playtest. |

## Problema e valor (hipótese)

- **Problema (`assumed`, a validar):** RTS clássicos exigem microgerenciar uma única base; 4X dão amplitude, mas sem ação em tempo real. Falta um meio-termo em que se tenha amplitude estratégica com combate em tempo real.
- **Hipótese de valor:** *Se* oferecermos um planeta com vários países em que o jogador alterna o controle e os demais seguem uma doutrina → *o jogador* tem amplitude estratégica sem microgerenciar tudo → *com impacto* mensurável: em 20 min, o testador troca de país de forma deliberada ≥ 3 vezes e descreve o que cada país fez enquanto estava fora. (`Scope_and_Phasing.md` §1)
- **Diferenciais do design:** alternar o controle entre países com cultura/tecnologia próprias; recursos que surgem por conhecimento (árvore livre); IA por doutrina que usa os mesmos dados e comandos que o jogador; a longo prazo, viagem espacial com nave jogável. `from-source:design.md`

## Produto em fases

| Fase | Conteúdo | Detalhe |
|---|---|---|
| 1 | Fatia vertical: 2–3 países, 1 espécie, IA por doutrina, troca de controle, 1 cadeia oculta, só incursões | `Scope_and_Phasing.md` §4 |
| 2 | Fase 1 do design completa: névoa, batalhas, espécies, anexação, interface completa | direção |
| 3 | Agendas, editor de doutrina, estabilidade, Legado, crises | direção |
| 4 | Viagem entre mundos | direção |
| 5 | Galáxia e multiplayer persistente | direção; infraestrutura em aberto |

## Decisões que valem para todo o projeto

- Determinismo obrigatório desde o início (ponto fixo, RNG com semente, hash de estado). Isso muda o design, que o marcava `[Depois]`. (`Risk_Analysis.md` §8, ADR-002)
- Itens `[MVP]` do design que dependem de sistemas `[Depois]` foram rebaixados para `[Depois]` (estabilidade nas agendas, Legado, cota estratégica, dano×armadura, habilidades e elevação em batalha, batalhas entre jogadores, união de países como marco). (`Risk_Analysis.md` §8)
- Fora da Fase 1: anexação, névoa de guerra, agendas, batalhas por parâmetros, baixa fidelidade, culturas e espécies extras. (`Scope_and_Phasing.md` §5)
- `docs/design.md` **ainda não reflete** essas decisões; onde divergir, valem os documentos de inception, e o design deve ser atualizado.

## Riscos principais

Escopo solo (25) · diferencial divertido? (20) · IA genérica por dados (20) · abandono do projeto (20) · sobrecarga cognitiva (16) · baixa fidelidade coerente (16). Detalhe e experimentos em `Risk_Analysis.md`.

## Critério de sucesso da Fase 1

Testes técnicos verdes (incluindo determinismo) + playtest com ≥2 pessoas além do autor + decisão do autor sobre querer continuar jogando. (`Scope_and_Phasing.md` §4.4)

## Mapa dos documentos

`Risk_Analysis.md` · `Scope_and_Phasing.md` · `Modules.md` · `Requirements.md` · `Backlog.md` · `Architecture_Overview.md` · ADRs em `docs/architecture/adr/` · fonte: `sources/design.md`.
