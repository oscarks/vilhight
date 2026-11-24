# Follow-ups: Foundation (Marco 0 / épico E0)

> Colhido em 2026-11-24 após `/pipeline Foundation B all` (4 fases, todas `APPROVED_WITH_OBSERVATIONS`; review final com 13/15 CAs PASS e 2 pendentes de verificação humana).
> Fontes: `Review_Phase_1..4.md`, `Review_Final.md`, `.claude/pipeline-state/foundation.json`.
> Não entram aqui: F-01..F-04 (corrigidos na própria run, Step 5b) e F-12 (commit só quando o usuário pedir, CLAUDE.md §8).

## Itens abertos

### FUP-foundation-01 — Inspeção visual do jogo (MAN-01 / CA-F4-13)
- **type**: unverified-ca · **severity**: P1 · **status**: RESOLVED
- **origin**: Review_Phase_4 O-01; Review_Final (CA-11 parte visual, CA-13)
- **description**: A taxa de ticks e a câmera foram verificadas só em modo headless (`godot_smoke.sh`: 19,998 ticks/s em 60 s; `camera_moved=1 zoom_in=1 zoom_out=1`). Ninguém abriu a janela. Falta conferir grade 32x32 centrada, HUD com 4 linhas, "Taxa" entre 19,0 e 21,0, WASD e roda do mouse reais, tick subindo com a câmera em movimento.
- **evidence**: `docs/features/Foundation/Review_Phase_4.md` O-01; `tools/smoke/godot_smoke.sh`.
- **impact**: O critério de saída do Marco 0 ("o jogo abre e mostra o mapa isométrico rodando a Sim a 20 ticks/s") só fica fechado com a inspeção. O item da câmera no CLAUDE.md §9 continua `[ ]` por isso.
- **suggested_action**: Rodar `$GODOT --path game` e conferir o checklist do MAN-01 da AI-SPEC Fase 4.
- **definition_of_done**: OK/falha registrado em `docs/history.md`; CLAUDE.md §9 marcado.
- **references**: AI-SPEC_Foundation_Phase_4.md (MAN-01); Proposal_Foundation.md CA-11, CA-13.
- **resolution**: 2026-11-24 — o usuário abriu `$GODOT --path game` (runbook `docs/runbooks/Abrir_o_Jogo.md`) e confirmou que o jogo funciona. Registrado no `docs/history.md`; CLAUDE.md §9 marcado.

### FUP-foundation-02 — Orçamentos de desempenho informativos nunca medidos
- **type**: tech-debt · **severity**: P2 · **status**: OPEN
- **origin**: Review_Phase_1 m-01; Review_Phase_2 m-01; Review_Final F-06
- **description**: Não foram medidos: 10^6 multiplicações de `Fixed` (< 20 ms), 10^6 `NextU64` (< 10 ms), `Step` vazio (< 0,1 ms), `ComputeHash` (< 0,05 ms). Só o carregamento (42 ms) e o E2E de 1000 ticks (11 ms) constam no history.
- **evidence**: `docs/history.md` (entradas END das fases 1 e 2); proposta §8.
- **impact**: Baixo agora (orçamentos informativos, não falham teste), mas é a linha de base para o RNF-03 (300 unidades) no E3-3.
- **suggested_action**: Medir uma vez fora de `src/Sim` (teste descartável com `Stopwatch`) e anotar os valores no history.
- **definition_of_done**: Quatro valores registrados em `docs/history.md`.
- **references**: AI-SPEC Fase 1 §9.3 e Fase 2 §9.3.

### FUP-foundation-03 — Reconciliar a proposta com as AI-SPECs
- **type**: gap-doc · **severity**: P2 · **status**: OPEN
- **origin**: Review_Final F-07
- **description**: `Proposal_Foundation.md` segue com `Status: Proposta`, diz que .NET e Godot não estão instalados (R-01) e mantém APIs superadas pelos DDs aprovados: `Universe.Systems` (código: `StarSystems`), `GameState.Random` público, `Fixed[] Stock` público, `Register(string, Action<…>)`, `TickCompleted`, `ArgumentException` no lugar de `GameSetupException`, `GlobalizePath` na §5.2.
- **impact**: Quem ler só a proposta vê uma API que não existe.
- **suggested_action**: Marcar o status como implementado e acrescentar nota "superada pelas AI-SPECs/DDs" com ponteiro, sem reescrever o corpo.
- **definition_of_done**: Cabeçalho e notas atualizados.
- **references**: AI-SPEC_Foundation_Phase_1..4 (seções de decisões).

### FUP-foundation-04 — `docs/design.md` ainda marca determinismo como `[Depois]`
- **type**: gap-doc · **severity**: P2 · **status**: OPEN
- **origin**: Review_Final F-08
- **description**: A decisão D1 da inception tornou o determinismo obrigatório desde o início, mas o design não foi atualizado (a inception prevalece, CLAUDE.md §8).
- **suggested_action**: Atualizar `docs/design.md` e a cópia em `docs/inception/sources/` com os itens rebaixados D2–D7 e o determinismo `[MVP]`.
- **definition_of_done**: Design e inception sem divergência.
- **references**: `docs/inception/Risk_Analysis.md` §8.

### FUP-foundation-05 — Tratar o `default` do `switch` de `TechEffect` no E8-2
- **type**: tech-debt · **severity**: P3 · **status**: OPEN
- **origin**: Review_Phase_3 m-01
- **description**: O construtor de cópia `protected` que o compilador gera para records impede fechar a hierarquia `TechEffect` (CS8878); um tipo de fora do assembly poderia derivá-la.
- **evidence**: `src/Sim/Data/TechEffects.cs:21`.
- **suggested_action**: No E8-2, os handlers devem tratar o `default` do `switch` por tipo como erro (exceção). Corrigir RN-P3-61 e §5.3.4 da AI-SPEC Fase 3.
- **definition_of_done**: Handlers lançam no `default`; spec corrigida.
- **references**: AI-SPEC_Foundation_Phase_3.md RN-P3-61, UT-BE-140.

### FUP-foundation-06 — Dividir `DataCompiler.cs` por etapa
- **type**: tech-debt · **severity**: P3 · **status**: OPEN
- **origin**: Review_Phase_3 O-02
- **description**: `DataCompiler.cs` tem 1308 linhas, contra "classes pequenas" (CLAUDE.md §6).
- **suggested_action**: Dividir por etapa (estrutura, referências, ciclos, alcançabilidade) quando o E5 acrescentar `buildings.json`, `units.json` e `recipes.json`.
- **definition_of_done**: Arquivos por etapa, testes verdes.
- **references**: `src/Sim/Data/DataCompiler.cs`.

### FUP-foundation-07 — Comentários `// UT-BE-xx` nos testes da Fase 1
- **type**: tech-debt · **severity**: P3 · **status**: OPEN
- **origin**: Review_Phase_1 m-02; Review_Final F-09
- **description**: Os testes da Fase 1 (`FixedTests`, `SimRandomTests`, `State/*`) não trazem o id do caso de teste que as Fases 2–4 usam.
- **suggested_action**: Acrescentar o comentário na primeira linha de cada teste.
- **definition_of_done**: Rastreio teste→spec uniforme.
- **references**: AI-SPEC_Foundation_Phase_1.md §11.

### FUP-foundation-08 — Erro de E/S com `--data-dir` escapa do `SimHost`
- **type**: gap-impl · **severity**: P3 · **status**: OPEN
- **origin**: Review_Final F-11
- **description**: `SimHost._Ready` só captura `DataValidationException`. Com `--data-dir`, falha de leitura em `DataLoader.Load` (permissão, caminho inválido) vira exceção não tratada em vez de aparecer no HUD. O caminho normal (`res://`) não é afetado.
- **evidence**: `game/scripts/SimHost.cs`.
- **suggested_action**: Capturar `IOException`/`UnauthorizedAccessException` e mostrar no HUD (`DATA_ERROR`), com teste no smoke.
- **definition_of_done**: Smoke com `--data-dir` inválido termina com código 2 e mensagem no HUD.
- **references**: AI-SPEC Fase 4 E2E-GD-03.

### FUP-foundation-09 — `DemoSetup` e a fixture de teste coincidem só por convenção
- **type**: tech-debt · **severity**: P3 · **status**: OPEN
- **origin**: Review_Final F-10 (DD-P4-05)
- **description**: `game/scripts/DemoSetup` espelha `SetupFactory.OneSystemThreeCountries(42)`. O E2E-BE-20 testa a fixture, não o `DemoSetup`; uma divergência só apareceria no smoke (`hash100`), que exige o Godot.
- **suggested_action**: Reavaliar quando o E1 trouxer mapa e setup reais.
- **definition_of_done**: Uma única fonte do setup de demonstração, ou teste que compare os dois.

### FUP-foundation-10 — Build a frio do Godot não repetido na review
- **type**: unverified-ca · **severity**: P3 · **status**: OPEN
- **origin**: Review_Phase_4 m-01
- **description**: O revisor não repetiu `rm -rf game/.godot && $GODOT --headless --path game --build-solutions --quit` (bloqueio de segurança do ambiente). O implementador relatou o ciclo a frio no history.
- **suggested_action**: O operador repete o comando e confere as duas DLLs (X-04).
- **definition_of_done**: Resultado anotado no history.

### FUP-foundation-11 — Reavaliar `CommandQueue.Enqueue` (O(n)) em E2/E7
- **type**: tech-debt · **severity**: P3 · **status**: OPEN
- **origin**: Review_Phase_2 O-02
- **description**: A fila usa `List` ordenada com inserção O(n) e `RemoveRange`. Adequado para a fila pequena prevista, mas pode pesar se E2/E7 enfileirarem em volume.
- **evidence**: `src/Sim/Commands/CommandQueue.cs`.
- **suggested_action**: Reavaliar com medição quando houver seleção e comandos em volume.
- **definition_of_done**: Medição registrada; estrutura trocada se necessário.

### FUP-foundation-12 — Conferir ignores antes do primeiro commit da feature
- **type**: known-issue · **severity**: P3 · **status**: OPEN
- **origin**: Review_Final F-12
- **description**: Toda a feature está fora do git (branch `chore/install-harness`), de acordo com a regra de commit só quando pedido. `build/` (147 MB) e `game/.godot/` precisam continuar ignorados; `game/Rts.Game.sln` e os `*.cs.uid` devem ser versionados (DD-P4-14, DD-P4-16).
- **suggested_action**: Antes do commit, rodar `git status --short` e `git check-ignore build/linux/Rts.x86_64 game/.godot`.
- **definition_of_done**: Commit sem artefatos de build.
