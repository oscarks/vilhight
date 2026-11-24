[2026-11-24 11:06 START] Follow-up: Foundation FUP-06 (dividir DataCompiler.cs por etapa, refactor mecânico).

[2026-11-24 11:03 END]   Follow-ups: Foundation FUP-02, FUP-08, FUP-07 — (02) orçamentos medidos em Release, projeto descartável fora do repo: 10^6 multiplicações de Fixed 4,83 ms (<20 ms OK); 10^6 NextU64 3,7 ms (<10 ms OK); Simulation.Step vazio ~0,00004 ms/tick (<0,1 ms OK); ComputeHash ~0,0011 ms (<0,05 ms OK). (08) SimHost._Ready agora captura também IOException e UnauthorizedAccessException de DataLoader.Load (DirectoryNotFoundException já cabe em IOException); antes do fix, resources.json ilegível dava exceção não tratada e smoke NO_OUTPUT (timeout 124); depois, DATA_ERROR e código 2. Pasta inexistente já dava DATA_ERROR antes, pois DataLoader trata arquivo ausente como erro de validação. (07) comentários // UT-BE-NN em 8 arquivos de teste da Fase 1, só comentários. 196 testes verdes, build 0 avisos, format limpo, smoke 10 PASS. Tempo real: ~4 min.
[2026-11-24 10:59 START] Follow-ups: Foundation FUP-02, FUP-08, FUP-07 (medição, SimHost, comentários UT-BE).

[2026-11-24 10:59 START] Follow-ups: Foundation — /resolve-followup auto (FUP-foundation-02, 03, 04, 07, 08, 12 autônomos; 06 e 10 escalonados; 05, 09, 11 ficam abertos por dependerem de épicos futuros).

[2026-11-24 10:56 END]   Verificação: MAN-01 Foundation — o usuário abriu o jogo (`$GODOT --path game`, runbook Abrir_o_Jogo) e confirmou que funciona. Fecha CA-F4-13 e a parte visual de CA-11/CA-13; FUP-foundation-01 RESOLVED; CLAUDE.md §9 marcado. Observação: a confirmação foi geral ("funcionou"), sem os valores individuais do checklist (taxa, hash).

[2026-11-24 10:19 END]   Pipeline: Foundation — 4 fases (Base, Ciclo, Dados, Apresentação) implementadas e revisadas (todas APPROVED_WITH_OBSERVATIONS, 0 CRITICAL/MAJOR); 196 testes verdes; CAs 13/15 PASS e 2 pendentes de inspeção humana (CA-11 visual, CA-13). Follow-ups: 12 itens em docs/followups/INDEX.md (FUP-foundation-01..12). Decisões: ADR-009 Accepted, CLAUDE.md §5.1 atualizado, game/Rts.Game.sln versionado.

[2026-11-24 10:15 START] Follow-up: Foundation — colhendo achados pós-pipeline (Review_Phase_1..4, Review_Final).

[2026-11-24 10:14 END]   Fix: Foundation review final F-01..F-04 (docs) — CLAUDE.md §3/§4/§7/§9 e Architecture_Overview §1/§3/§9 atualizados; só documentos. Tempo real: ~2 min. Follow-ups: nenhum.

[2026-11-24 10:12 START] Fix: Foundation review final F-01..F-04 (documentação).

[2026-11-24 10:20 END]   Review: Foundation Fase 4 (Apresentação) — APPROVED_WITH_OBSERVATIONS (0 CRITICAL, 0 MAJOR, 1 MINOR, 3 OBSERVATION). Build 0 avisos, 196 testes passando, format limpo, smoke 60 s PASS (rate_milli=19998), E2E-GD-03/04 OK, exportação PASS (source=res exported=1). MAN-01 (inspeção visual) segue pendente para o humano (CA-F4-13). Relatório em docs/features/Foundation/Review_Phase_4.md. Follow-ups: nenhum (followup:none).

[2026-11-24 10:05 START] Review: Foundation Fase 4 (Apresentação) — AI-SPEC_Foundation_Phase_4.md.

[2026-11-24 09:59 END]   Implement: Foundation Fase 4 (Apresentação) — CONCLUÍDO (~23 min reais, 09:36–09:59, incluindo download de 1,2 GB dos templates). TickClock/SimLoop (src/Sim/Hosting), 17 testes novos escritos primeiro, 196 testes passando, build 0 avisos, format limpo, --build-solutions OK (+ *.cs.uid). Smoke 60 s: SMOKE result=PASS seconds=60 window_us=60003898 ticks=1200 rate_milli=19998 hash100=8913928992633490408 dropped_us=0 source=res exported=0. E2E-GD-03/04 OK. Exportação (E2E-GD-05): o export de C# exigiu game/Rts.Game.sln (DD-P4-16, versionado; UT-BE-164 ajustado primeiro e falhou antes do arquivo existir): SMOKE result=PASS seconds=10 window_us=10006580 ticks=200 rate_milli=19986 hash100=8913928992633490408 resources=5 techs=4 hud_ok=1 source=res exported=1; build/linux = 147M (Rts.x86_64 + Rts.pck + data_Rts.Game_linuxbsd_x86_64). Templates 4.7.2 mono instalados (SHA-512 conferido). MAN-01 não executado (headless): CA-F4-13 pendente. Follow-ups: nenhum (followup:none).

[2026-11-24 09:36 START] Implement: Foundation Fase 4 (Apresentação) — projeto Godot, SimHost/TickClock/SimLoop, grade, câmera, HUD, smoke e exportação Linux (AI-SPEC_Foundation_Phase_4.md).

[2026-11-23 20:27 END]   Review: Foundation Fase 3 (Dados) — APPROVED_WITH_OBSERVATIONS (0 CRITICAL, 0 MAJOR, 1 MINOR, 3 OBSERVATION). Build 0 avisos, 179 testes passando, format limpo, varreduras de APIs proibidas e de ids de conteúdo vazias. Desvio do TechEffect (CS8878) avaliado como aceitável (m-01). Relatório em docs/features/Foundation/Review_Phase_3.md. Follow-ups: nenhum (followup:none).

[2026-11-23 20:20 START] Review: Foundation Fase 3 (Dados) — AI-SPEC_Foundation_Phase_3.md.

[2026-11-23 20:13 END]   Implement: Foundation Fase 3 (Dados) — src/Sim/Data (DataLoader, DataValidator, DataCompiler etapas 1–6, DataSet, registries, defs, 8 efeitos tipados, DataError/Exception), GameState.Create(GameSetup, DataSet), game/data (5 recursos, 4 techs) e 26 testes novos; testes escritos primeiro (24/26 falharam sobre esqueletos), depois implementação. Build 0 avisos/0 erros, 179 testes passando (153 + 26), dotnet format limpo; game/data carrega com 0 erros e 4 avisos; hash inicial 210089492672693724 mantido; CA-F3-10 verificado manualmente (`Tmp.cs:2: CID ore in "ore"`, desfeito). Tempo real gasto: ~7 min. Medição: GameData_LoadsSampleSet (inclui JIT, Load + asserts) 42 ms, dentro do orçamento de 50 ms. Desvio: o construtor de cópia protegido do record abstrato TechEffect (sintetizado pelo compilador, CS8878 impede torná-lo private protected) faz o UT-BE-140 verificar "nenhum construtor público e o sem parâmetros é private protected" em vez de "construtor único". Follow-ups: nenhum (followup:none).

[2026-11-23 20:06 START] Implement: Foundation Fase 3 (Dados) — carregador/validador de resources.json e techs.json, DataSet, game/data (AI-SPEC_Foundation_Phase_3.md).

[2026-11-23 19:52 END]   Review: Foundation Fase 2 (Ciclo) — APPROVED_WITH_OBSERVATIONS (0 CRITICAL, 0 MAJOR, 1 MINOR, 3 OBSERVATION). Build 0 avisos, 153 testes passando, format limpo, varreduras de APIs proibidas e `"test.` vazias. Relatório em docs/features/Foundation/Review_Phase_2.md. Follow-ups: nenhum (followup:none).

[2026-11-23 19:46 START] Review: Foundation Fase 2 (Ciclo) — AI-SPEC_Foundation_Phase_2.md.

[2026-11-23 19:42 END]   Implement: Foundation Fase 2 (Ciclo) — Command/Registry/Codec/Queue, Simulation.Step, SimEvents, controladores, StateHasher, DeterminismScenario e ForbiddenApiScanner; testes escritos primeiro (49/153 falharam sobre esqueletos), depois implementação. Build 0 avisos/0 erros, 153 testes passando (71 da Fase 1 + 82 novos), dotnet format limpo; valores de referência conferidos (hash inicial, ticks 1/100/1000, controle negativo no tick 504, 1495 aplicados/20 rejeitados); CA-F2-10 verificado manualmente (FB-01 com arquivo:linha). Tempo real gasto: ~4 min. Orçamentos informativos §9.3 (Step/ComputeHash/E2E) não medidos individualmente; suíte completa 140 ms (E2E-BE-01 11 ms). Sem desvios da spec. Follow-ups: nenhum (followup:none).

[2026-11-23 19:38 START] Implement: Foundation Fase 2 (Ciclo) — Command, Step, controladores, hash e determinismo (AI-SPEC_Foundation_Phase_2.md).

[2026-11-23 09:45 END]   Review: Foundation Fase 1 (Base) — APPROVED_WITH_OBSERVATIONS (0 CRITICAL, 0 MAJOR, 2 MINOR, 3 OBSERVATION). Build 0 avisos, 71 testes passando, format limpo, greps CA-F1-08 vazios. Relatório em docs/features/Foundation/Review_Phase_1.md. Follow-ups: nenhum (followup:none).

[2026-11-23 09:40 START] Review: Foundation Fase 1 (Base) — AI-SPEC_Foundation_Phase_1.md.

[2026-11-23 09:34 END]   Implement: Foundation Fase 1 (Base) — Rts.sln, Rts.Sim (Fixed, SimRandom, GameState) e Rts.Sim.Tests; testes escritos primeiro (69/71 falharam), depois implementação. Build 0 avisos/0 erros, 71 testes passando, dotnet format limpo, greps do CA-F1-08 vazios. Tempo real gasto: ~4 min (orçamentos de desempenho não medidos). Follow-ups: nenhum (followup:none sob pipeline).

[2026-11-23 09:30 START] Implement: Foundation Fase 1 (Base) — solução .NET, Fixed, SimRandom, GameState (AI-SPEC_Foundation_Phase_1.md).

[2026-11-22 14:18 START] Pipeline: Foundation — modo B, 4 fases (Base, Ciclo, Dados, Apresentação); sem pipeline.config.json, usa os modelos padrão (ai-spec opus, implement/review sonnet).

[2026-11-18 09:40 END]   Proposal: Foundation — criado docs/features/Foundation/Proposal_Foundation.md (17 seções, 9 UCs, 15 CAs, 4 fases de implementação). Achado: .NET 8 SDK e Godot 4 .NET não estão instalados no host (R-01). Divergência registrada: câmera mínima no E0 (DD-06).

[2026-11-18 09:15 START] Proposal: Foundation — expandir o seed do épico E0 (Fundação, M0).

[2026-11-18 09:00 END]   Config: Harness Platform no repositório — harness.project.json criado, CLAUDE.md §8 reescrito para o pipeline do harness, history.md e followups/INDEX.md criados.

[2026-11-18 08:50 START] Config: Harness Platform no repositório — adotar o Spection Flow como metodologia (substitui docs/specs e progress.md).

[2026-11-17 23:00 END]   Inception: RTS isométrico — finalizada. Riscos, escopo (Fase 1 = fatia vertical), requisitos, backlog (9 épicos, 48 histórias), arquitetura e 8 ADRs; 9 seeds de proposta em docs/features/.

[2026-11-17 20:27 START] Inception: RTS isométrico — init, risk, scope, refine, architecture, finalize a partir de docs/design.md.
