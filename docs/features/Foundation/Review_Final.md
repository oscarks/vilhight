# Review final (cross-fase): Foundation — épico E0 · Marco 0

**Data**: 2026-11-24
**Proposta**: `docs/features/Foundation/Proposal_Foundation.md` (§15, CA-01..CA-15)
**AI-SPECs**: `AI-SPEC_Foundation_Phase_1.md` .. `_4.md`
**Reviews por fase**: `Review_Phase_1.md` .. `_4.md` (todas APPROVED_WITH_OBSERVATIONS)
**ADRs**: ADR-001..ADR-009
**Escopo**: `src/Sim`, `src/Sim.Tests`, `game/`, `tools/smoke`, `CLAUDE.md`, `docs/design.md`, `docs/inception/`, `docs/history.md`, `docs/followups/`

## Veredito: APPROVED_WITH_OBSERVATIONS

Condicionado à inspeção visual humana (MAN-01), que fecha CA-11 (parte visual) e CA-13.

| Severidade | Quantidade |
|---|---|
| CRITICAL | 0 |
| MAJOR | 0 |
| MINOR | 6 |
| OBSERVATION | 6 |

Critérios de aceite: **13/15 PASS**, 0 FAIL, 2 PENDENTE-HUMANO (CA-11 parte visual, CA-13 parte visual). A parte automatizável dos dois passou.

Não foram tratadas como achado as divergências aprovadas pelo usuário: DD-P1-01..08, DD-P2-01..10, DD-P3-01..14, DD-P4-01..16, ADR-009 e o novo texto do CLAUDE.md §5.1.

---

## 1. Validações executadas nesta revisão

Ambiente: `zsh -ic`, dotnet 8.0.428, `$GODOT=/home/oscar/bin/godot4` (4.7.2 mono).

| Verificação | Resultado |
|---|---|
| `dotnet build Rts.sln --no-incremental` | 0 avisos, 0 erros (Rts.Sim, Rts.Sim.Tests, Rts.Game) |
| `dotnet test src/Sim.Tests` | `Aprovado! Com falha: 0, Aprovado: 196, Ignorado: 0, Total: 196` (182 ms) |
| `dotnet format Rts.sln --verify-no-changes` | código de saída 0 |
| `tools/smoke/godot_smoke.sh 60` | `SMOKE result=PASS seconds=60 window_us=60002905 ticks=1200 rate_milli=19999 hash100=8913928992633490408 resources=5 techs=4 camera_moved=1 zoom_in=1 zoom_out=1 hud_ok=1 dropped_us=0 source=res exported=0`, código 0 |
| `godot_smoke.sh 10 --data-dir=<pasta vazia>` | `SMOKE result=DATA_ERROR message=data validation failed with 2 error(s):`, código 2 |
| `godot_smoke.sh 10 --smoke-seconds=5` | `SMOKE result=BAD_ARGS arg=--smoke-seconds=5`, código 3 |
| `tools/smoke/export_smoke.sh 10` | `SMOKE result=PASS seconds=10 window_us=10006738 ticks=200 rate_milli=19986 hash100=8913928992633490408 resources=5 techs=4 camera_moved=1 zoom_in=1 zoom_out=1 hud_ok=1 dropped_us=0 source=res exported=1`, código 0. O `.pck` contém `res://data/resources.json` e `techs.json`. |
| grep de `float`/`double`/`decimal`/`System.Random`/`DateTime`/`Stopwatch`/`Guid.`/`Thread`/`Task.`/`using Godot`/`System.Linq` em `src/Sim` (sem comentários) | vazio |
| grep dos 9 ids de `game/data` (`food`, `wood`, `knowledge`, `ore`, `metal`, `tech_*`, `mine`, `smelter`) em `src/Sim` | vazio |

Efeito colateral: o `export_smoke.sh` recriou `build/linux/` (ignorado pelo git, `.gitignore:17`). Nenhum código ou documento foi alterado, exceto este relatório.

---

## 2. Parte 1: critérios de aceite (Proposta §15)

| CA | Estado | Evidência |
|---|---|---|
| CA-01 build + format sem avisos; Sim sem Godot | **PASS** | build `--no-incremental` 0/0; `format --verify-no-changes` = 0; `src/Sim/Rts.Sim.csproj` sem `PackageReference`/`ProjectReference`; `Architecture/SimAssemblyTests.SimAssembly_DoesNotReferenceGodot` |
| CA-02 `dotnet test` headless passa | **PASS** | 196/196, 0 ignorados, nenhum `Skip =` em `src/Sim.Tests` |
| CA-03 mesmo hash em cada um dos 1000 ticks | **PASS** | `Determinism/DeterminismTests.SameSeedSameCommands_SameHashEveryTick` (E2E-BE-01): compara 1001 hashes; `hash[1000] == 11368851551727198225` |
| CA-04 controle negativo (semente, comando, estoque) | **PASS** | `DeterminismTests.NegativeControls` (E2E-BE-02: diverge a partir do tick 504); `Core/StateHasherTests` (UT-BE-103: semente 43 ⇒ `6213880399494383774`, alterações de estado com hashes fixados e `NotEqual(S0Hash, …)`) |
| CA-05 comando válido altera estado e faz ida e volta | **PASS** | `CommandCodecTests.Codec_RoundTrip` (1515 comandos, igualdade de valor e de tipo), `Serialize_ExactFormat`; `DeterminismTests.CommandsSurviveSerializationInDeterminismRun` (E2E-BE-03); `SimulationStepTests.Step_AppliesInReceivedOrder` |
| CA-06 emissor errado: evento, hash idêntico, sem exceção | **PASS** | `SimulationStepTests.Step_RejectsNotController` (UT-BE-83): `CommandRejected(0, 2, "test.addStock", NotController)` e `simA.ComputeHash() == simB.ComputeHash()`; também `Step_RejectsUnknownCountry`, `Step_RejectsUnknownCommandType` |
| CA-07 exemplo 5 recursos / 4 techs | **PASS** | `Data/GameDataTests.GameData_LoadsSampleSet` (E2E-BE-10) sobre `game/data`; smoke `resources=5 techs=4` no editor e no exportado |
| CA-08 recusa id duplicado, referência quebrada, ciclo, inalcançável, efeito desconhecido, propriedade desconhecida | **PASS** | `DataStructureTests` (`DuplicateId`, `UnknownProperty`, `UnknownEffectType`), `DataReferenceTests.UnknownReferences_AreReported` (`UnknownResource`/`UnknownTech`), `Cycles_ReportThePath` (`TechCycle` com caminho), `Reachability_UsesFixedPoint` (`UnreachableResource`, `UnresearchableTech`); mensagem com arquivo, caminho JSON, id e código (`DataError.ToString`) |
| CA-09 3 erros numa só `DataValidationException` | **PASS** | `DataErrorReportingTests.Parse_ThrowsWithAllErrors` (UT-BE-133): `ex.Errors.Count == 3` e mensagem exata |
| CA-10 `float`/`System.Random`/`DateTime`/`using Godot` falha com arquivo e linha | **PASS** | `ForbiddenApiScannerTests.Scanner_DetectsForbiddenApis` (FB-01, 03, 04, 07), `Scanner_ReportsLineNumber` (`x.cs:3: FB-01 float c;`), `SimSources_HaveNoForbiddenApis` sobre `src/Sim` real. A injeção num arquivo real de `src/Sim` não foi repetida (revisão sem alterar código); o implementador a registrou na Fase 2 |
| CA-11 grade visível e tick a 20/s (±1/s em 60 s) | **PENDENTE-HUMANO** (parte automatizável **PASS**) | smoke 60 s: `ticks=1200 rate_milli=19999` (19,999 ticks/s), `dropped_us=0`; `TickClockTests.SixtySecondsOfFrames_Yield1200Ticks`. Falta ver a grade 32x32 na janela (MAN-01) |
| CA-12 HUD com hash curto e contagens de `game/data` | **PASS** | smoke `hud_ok=1`: `SmokeRun.Verdict` confere as linhas `Tick:`, `Hash: <8 hex>` (32 bits altos de `ComputeHash`) e `Recursos: 5 · Techs: 4`; vale para o editor e o exportado. A legibilidade na tela entra no MAN-01 |
| CA-13 WASD move a câmera e a roda muda o zoom sem alterar a Sim | **PENDENTE-HUMANO** (parte automatizável **PASS**) | smoke `camera_moved=1 zoom_in=1 zoom_out=1` com eventos injetados por `Input.ParseInputEvent` entre 1 s e 3,5 s (ticks 20..70); `hash100=8913928992633490408` igual a `SmokeRun.ExpectedHashAtCheckTick` e a `SimLoopTests.DemoSetupEquivalent_ReferenceHashes` (E2E-BE-20). Teclado e mouse reais numa janela só uma pessoa confere (MAN-01) |
| CA-14 testes de `Fixed`/`SimRandom` cobrem RN-01..07 | **PASS** | `Core/FixedTests.cs`: 8 asserts de `OverflowException`, 2 de `DivideByZeroException`, `Sqrt_IsFloorOfRoot` (inclusive `Sqrt(2).Raw == 6074000999` e `x < 0`), checksum `128333051705741941`; `Core/SimRandomTests.cs`: vetores SplitMix64/xoshiro256**, `SameSeed_SameSequence`, `DifferentSeeds_Diverge`, `NextInt_VectorsAndGuards` (`n <= 0` lança), `NextFixed_InUnitInterval`, `Seed0_IsValid` |
| CA-15 `Step` sem comandos e sem sistemas não aloca | **PASS** | `Core/CycleAllocationTests.Step_AndHash_DoNotAllocate` (UT-BE-105): delta 0 em 1000 `Step` vazios, também com comandos+sistema e em `ComputeHash`; complementos `SimLoopTests.Tick_DoesNotAllocate`, `TickClockTests.Advance_DoesNotAllocate` |

**Resumo**: 13 PASS, 0 FAIL, 2 PENDENTE-HUMANO.

### Itens que só uma pessoa pode verificar (MAN-01)

1. `$GODOT --path game` abre a janela com a grade isométrica 32x32 visível e centrada (CA-11).
2. O HUD mostra 4 linhas legíveis e "Taxa" entre 19,0 e 21,0 ticks/s (CA-11/CA-12).
3. WASD desloca a câmera e a roda do mouse aproxima e afasta, com o tick continuando a subir (CA-13).
4. Registrar OK ou falha em `docs/history.md`.

---

## 3. Parte 2: review cross-fase

### 3.1 Consistência e integração entre as fases

| Ponto | Resultado |
|---|---|
| Hash de referência do estado inicial `210089492672693724` (`Refs.S0Hash`) | Igual na Fase 2 (RN-P2-52), na Fase 3 (E2E-BE-11, `Create(setup, DataSet)` só delega) e na Fase 4 (E2E-BE-20) |
| Hash no tick 100 do jogo `8913928992633490408` | Igual na spec da Fase 4, em `SmokeRun.ExpectedHashAtCheckTick`, em E2E-BE-20 e nas duas saídas de smoke (editor e exportado) |
| Hashes do cenário de 1000 ticks (`7140252220498885332`, `14189062733609157557`, `11368851551727198225`) | Iguais na spec da Fase 2, no teste e no Review_Phase_2 |
| Tipos entre fases | `ResourceId`/`TechId` (Fase 1) são os índices densos dos registries (Fase 3); `Country.Stock` tem `ResourceCount` = `DataSet.Resources.Count`; `SimLoop` (Fase 4) usa `CommandQueue`/`ControllerSet`/`Simulation` (Fase 2) sem adaptação; `ResDataReader` usa `DataFiles` e `DataLimits` (Fase 3) |
| Ordem de comandos | `SimLoop.Tick`: fila primeiro, depois controladores por `CountryId` (`ControllerSet.Collect`); `Step` aplica na ordem recebida (DD-08). Coerente entre Fases 2 e 4 |
| Contrato de dados no jogo | ADR-009 (`Parse(RawData)` com texto do `res://`) usado no editor e no exportado; `Load(dir)` só em testes e `--data-dir` |

Nenhuma inconsistência de código entre as fases. As discrepâncias estão na documentação (§4).

### 3.2 Preocupações transversais

- **Determinismo**: `src/Sim` sem `float`/`double`/`decimal`, `System.Random`, relógio, `Guid`, threads ou LINQ (grep e UT-BE-112). `TickClock` faz só aritmética de inteiros sobre o tempo que o host informa. `Dictionary` aparece apenas para busca (`CommandRegistry`, registries, `DataCompiler._resMap/_techMap`), nunca iterado com efeito na ordem. Estado estático mutável: nenhum (regra FB-09). Hash FNV-1a sem `BitConverter` nem `GetHashCode` (FB-08). OK.
- **Fronteira Sim/Godot**: `Rts.Sim` sem referência ao Godot (teste por reflexão); `InternalsVisibleTo` só para `Rts.Sim.Tests` (UT-BE-162); `.Raw` lido só em `FixedConvert.cs`; nenhum script de `game/` altera `GameState` (setters `internal`). `game/scripts` só lê estado, desenha e mede. OK.
- **Ids de conteúdo**: nenhum dos 9 ids de `game/data` aparece em `src/Sim` (grep e `ContentIdScanner`, UT-BE-141/142); ids de teste (`test.*`, `r_a`, `t_a`) ficam em `Sim.Tests`. O literal `"Recursos: 5 · Techs: 4"` em `SmokeRun` é de diagnóstico em `game/`, permitido por RN-P4-67. OK.
- **Desempenho e alocação**: `Step`, `ComputeHash`, `SimLoop.Tick` e `TickClock.Advance` medidos com 0 byte. `CommandQueue` usa inserção O(n), adequada ao volume previsto (Review_Phase_2 O-02). `IsoGrid._Draw` aloca arrays, mas só desenha uma vez. Os orçamentos de tempo da proposta não foram medidos (F-06).

---

## 4. Achados

### CRITICAL
Nenhum.

### MAJOR
Nenhum.

### MINOR

| # | Fase | Descrição | Onde | Sugestão |
|---|---|---|---|---|
| F-01 | Transversal (docs) | O CLAUDE.md §9 (Marco 0) está com todos os itens desmarcados, embora estejam entregues. O item da câmera ainda pede "bordas, WASD, arrasto, zoom", e o DD-06 aprovado entregou só WASD + zoom, com bordas e arrasto no E1-4. A divergência está aprovada, mas o roteiro não diz isso, e quem ler o CLAUDE.md conclui que o M0 está incompleto ou que a câmera está fora do escopo. | `CLAUDE.md` §9 | Marcar os itens entregues e anotar no item da câmera: "(WASD + zoom; bordas/arrasto em E1-4, DD-06)". |
| F-02 | Transversal (docs) | A árvore do CLAUDE.md §4 está desatualizada. Faltam `tools/smoke/`, `src/Directory.Build.props`/`Directory.Packages.props` (DD-P1-01), `src/Sim/Hosting` (DD-P4-03), `game/Rts.Game.sln` (DD-P4-16), `game/export_presets.cfg` e `build/` (saída ignorada). Aparecem como se existissem `tools/blender/`, `tools/DataValidator/` e `game/assets/`, que não existem. | `CLAUDE.md` §4 | Acrescentar o que existe e marcar o que é planejado (ex.: "(futuro)"). |
| F-03 | Fase 4 (docs) | O CLAUDE.md §3 diz "Godot 4.x", mas o projeto fixou a versão 4.7.2: `Godot.NET.Sdk/4.7.2` em `game/Rts.Game.csproj`, e os smokes exigem templates `4.7.2.stable.mono`. O §7 não lista `tools/smoke/godot_smoke.sh`, `export_smoke.sh` nem `dotnet format --verify-no-changes`. O exemplo de `GODOT` também difere do binário usado (`~/bin/godot4`). | `CLAUDE.md` §3, §7 | Documentar a versão fixada e acrescentar os comandos de smoke e de exportação ao §7. |
| F-04 | Fases 2 e 4 vs inception | O `Architecture_Overview.md` §3 ainda diz que `Step` "valida e aplica os comandos do tick (por país, em ordem de id)" e "por fim atualiza o hash". O código, com a decisão aprovada, aplica na ordem recebida (ordem canônica no `ControllerSet`) e calcula o hash sob demanda. Já era o O-03 do Review_Phase_2 e não foi resolvido. Como o CLAUDE.md §8 manda que "vale a inception", uma spec de E1+ pode reintroduzir o comportamento antigo. | `docs/inception/Architecture_Overview.md` §3, itens 1–2 | Atualizar o §3 (ordem recebida, ordem por país garantida pelo `ControllerSet`/`SimLoop`, hash sob demanda, `TickClock` na Sim) ou registrar uma nota de errata. |
| F-05 | Transversal (processo) | As pendências das quatro fases não foram registradas em `/followup`. O `docs/followups/INDEX.md` tem 0 itens e foi atualizado em 2026-11-18; todas as reviews dizem `followup:none`. Pendências em aberto: MAN-01 (CA-F4-13), orçamentos de desempenho (P1 m-01, P2 m-01), tratar o `default` do `switch` de `TechEffect` no E8-2 (P3 m-01), dividir `DataCompiler.cs` (1308 linhas) no E5 (P3 O-02), atualizar o `Architecture_Overview` (P2 O-03, F-04) e o build a frio do Godot (P4 m-01). O CLAUDE.md §8 diz: "Pendências vão para `/followup`". | `docs/followups/INDEX.md` | Registrar ao menos MAN-01 (`unverified-ca`), P3 m-01 e F-04 (`gap-doc`). |
| F-06 | Fases 1 e 2 (persistente) | Os orçamentos da proposta §8 ("medidos e registrados": `Step` vazio < 0,1 ms, `ComputeHash` < 0,05 ms) e os da Fase 1 (10^6 multiplicações `Fixed` < 20 ms, 10^6 `NextU64` < 10 ms) nunca foram medidos. Só o carregamento (42 ms, Fase 3) e o E2E de 1000 ticks (11 ms) aparecem no history. P1 m-01 e P2 m-01 continuam abertos. | `docs/history.md` | Medir uma vez fora de `src/Sim` (ex.: teste descartável) e anotar. Não bloqueia: são informativos. |

### OBSERVATION

| # | Fase | Descrição |
|---|---|---|
| F-07 | Proposta | A proposta não foi reconciliada com as specs. Ela segue com `Status: Proposta` e diz que .NET e Godot "não instalados" (cabeçalho, §3, R-01). Também mantém as APIs superadas pelos DDs aprovados: `Universe.Systems` (o código usa `StarSystems`), `GameState.Random` público, `Fixed[] Stock` público, `Register(string, Action<…>)`, eventos `TickCompleted`, `ArgumentException` em vez de `GameSetupException` e `GlobalizePath` na tabela da §5.2. Não é falha, mas uma nota "superada pelas AI-SPECs/DDs" evitaria leitura errada. |
| F-08 | Design | O `docs/design.md` ainda marca o determinismo como `[Depois]`, contra a decisão D1 da inception. A divergência é conhecida e a inception prevalece (CLAUDE.md §8). Sem ação nesta feature. |
| F-09 | Fase 1 | Os testes da Fase 1 (`FixedTests`, `SimRandomTests`, `State/*`, exceto `GameStateCreateWithDataTests`) não têm o comentário `// UT-BE-xx` que as Fases 2–4 usam (P1 m-02 ainda aberto). A rastreabilidade teste → spec fica desigual entre as fases. |
| F-10 | Fase 4 | `DemoSetup` (jogo) espelha `SetupFactory.OneSystemThreeCountries(42)` (testes) só por convenção (DD-P4-05). O E2E-BE-20 testa a fixture, não o `DemoSetup`, então uma divergência entre os dois só aparece no smoke (`hash100`), que exige o Godot. O risco é aceitável e já está mitigado pelo smoke. |
| F-11 | Fases 3 e 4 | `SimHost._Ready` só captura `DataValidationException`. Com `--data-dir`, um erro de E/S de `DataLoader.Load` (permissão, caminho inválido) escapa como exceção não tratada, em vez de aparecer no HUD. Afeta só o caminho de diagnóstico; o caminho normal (`res://`) usa `ResDataReader`, que devolve `null` → `FileMissing`. |
| F-12 | Transversal | Toda a feature está fora do git: `src/`, `game/`, `tools/`, `docs/features/`, `Rts.sln` e `.gitignore` aparecem como não rastreados no branch `chore/install-harness`. Isso está de acordo com o CLAUDE.md §8 (commit só quando o usuário pedir). Antes do commit, conferir que `build/` (147 MB) e `game/.godot/` continuam ignorados. |

---

## 5. Definição de pronto (CLAUDE.md §8), visão consolidada

| Item | Estado |
|---|---|
| Testes da spec passando, incluindo determinismo | SIM (196/196; E2E-BE-01/02/03, E2E-BE-20) |
| Nenhum `float`, `System.Random` ou id de conteúdo fixo em `src/Sim` | SIM (UT-BE-112, UT-BE-142, greps desta revisão) |
| Dados novos validados pelo carregador | SIM (E2E-BE-10; smoke via `.pck`) |
| `docs/history.md` com tempo real | SIM (START/END das 4 implementações e reviews), com a ressalva F-06 |
| Critério de saída do Marco 0 | Cumprido na parte automatizável (`dotnet test` verde com determinismo; Sim a 19,999 ticks/s no editor e 19,986 no exportado). Falta só a confirmação visual (MAN-01) |

## 6. Recomendação

Aprovar o épico E0. Antes de abrir o E1:

1. Executar o MAN-01 e registrar no history.
2. Resolver F-01..F-04 (documentação; nenhuma mudança de código).
3. Registrar as pendências de F-05 em `/followup`.
