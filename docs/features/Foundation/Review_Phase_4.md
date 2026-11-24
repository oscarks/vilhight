# Review: Foundation — Fase 4 (Apresentação)

**Data**: 2026-11-24
**Spec**: `docs/features/Foundation/AI-SPEC_Foundation_Phase_4.md` (revisão 3)
**Veredito**: **APPROVED_WITH_OBSERVATIONS**
**Contagem**: CRITICAL 0 · MAJOR 0 · MINOR 1 · OBSERVATION 3

Item pendente de ação humana (não é falha): **MAN-01** (inspeção visual, CA-F4-13) não foi executado pelo implementador.

## 1. Validações executadas (nesta revisão)

| Verificação | Resultado |
|---|---|
| `dotnet build Rts.sln` | 0 avisos, 0 erros (Rts.Sim, Rts.Sim.Tests e Rts.Game) |
| `dotnet test src/Sim.Tests` | 196/196 (179 anteriores + 17 novos: UT-BE-150..165 e E2E-BE-20), 0 ignorados |
| `dotnet format Rts.sln --verify-no-changes` | código 0 |
| `tools/smoke/godot_smoke.sh 60` (E2E-GD-02) | `PASS`, `window_us=60003906 ticks=1200 rate_milli=19998 hash100=8913928992633490408 resources=5 techs=4 camera_moved=1 zoom_in=1 zoom_out=1 hud_ok=1 dropped_us=0 source=res exported=0` |
| `godot_smoke.sh 10 --data-dir=<vazio>` (E2E-GD-03) | `DATA_ERROR`, código 2 |
| `godot_smoke.sh 10 --data-dir=game/data` (controle) | `PASS`, `source=dir resources=5 techs=4` |
| `--smoke-seconds=5` (E2E-GD-04) | `BAD_ARGS arg=--smoke-seconds=5`, código 3 |
| `$GODOT --headless --path game --build-solutions --quit` (E2E-GD-01) | código 0; `Rts.Game.dll` e `Rts.Sim.dll` em `.godot/mono/temp/bin/Debug/` (ver m-01) |
| `tools/smoke/export_smoke.sh 10` (E2E-GD-05) | código 0; `PASS seconds=10 rate_milli=19986 hash100=8913928992633490408 resources=5 techs=4 hud_ok=1 source=res exported=1` (dados lidos do `.pck`) |
| `git check-ignore build/linux/x` | regra `.gitignore:17 build/` |
| grep em `game/scripts` | `.Raw` só em `FixedConvert.cs`; sem `GlobalizePath`, `"resources.json"`, `"techs.json"` |
| `src/Sim/Hosting` | sem `float`/`double`/`System.Random`/`DateTime`/`Stopwatch`/Godot; UT-BE-112 verde |

## 2. Conformidade com a spec

- **Estrutura de arquivos**: todos os arquivos de §5.2 existem (`project.godot`, `Rts.Game.csproj`, `Rts.Game.sln`, `export_presets.cfg`, `Main.tscn`, 8 scripts + `.cs.uid`, `TickClock`, `SimLoop`, testes, `tools/smoke/*.sh`). `Rts.sln` inclui `game\Rts.Game.csproj`. `.gitignore` contém `build/`.
- **TickClock / SimLoop**: implementação idêntica à §5.3.1/5.3.2 (RN-P4-01..13, INV-TC, INV-SL). Aritmética só em `long`; `Alpha` via `Fixed.FromRatio`.
- **SimHost** (RN-P4-20..23): ordem de montagem, `--data-dir` por último valor, caminho de erro (`LoadError`, `PushError`, `SetProcess(false)`), `DeltaToMicros` e laço `Advance` → `Tick` → `Stepped` conforme a spec; sem alocação por quadro além do próprio evento.
- **ResDataReader** (RN-P4-27, ADR-009): `FileExists`, `GetBuffer(MaxFileBytes+1)`, remoção de BOM e `UTF8Encoding(false,false)`; nomes vêm de `DataFiles`.
- **HudTick** (RN-P4-30..32), **CameraRig** (RN-P4-40..42), **IsoGrid** (RN-P4-50/51), **SmokeRun** (RN-P4-60..67), `DemoSetup`, `FixedConvert`: conforme.
- **ADR-009** com Status `Accepted` (a spec previa `Proposed`; aprovado pelo usuário) e CLAUDE.md §5.1 atualizado com o texto proposto: ALIGNED.
- Nenhum DIVERGENT, MISSING ou EXTRA relevante.

## 3. Rastreabilidade das regras

Todas as RN-P4 de Sim (01..13) estão COVERED por UT-BE-150..161 e E2E-BE-20. As regras de apresentação (20..27, 30..32, 40..42, 50/51, 60..67, 70..73) estão COVERED pelo smoke headless (editor e exportado), UT-BE-162..165 e inspeção de código; RN-P4-23 (sem alocação em `_Process`) e RN-P4-51/MAN-01 (aspecto visual da grade) dependem de revisão/inspeção, conforme a spec.

## 4. Critérios de aceitação

| CA | Estado |
|---|---|
| CA-F4-01..12, 14..17 | PASS |
| CA-F4-13 (MAN-01 registrado como OK) | **NOT VERIFIABLE** (pendência humana: exige janela/renderizador) |
| CA-FINAL-01..04 | PASS |
| CA-FINAL-05 (saída do Marco 0) | PASS na parte automatizável (`dotnet test` verde com determinismo, Sim a 20 ticks/s no editor e exportado); a parte "mostra a grade isométrica" depende do MAN-01 |

| Tipo | Total | Pass | Fail | Não verificável |
|---|---|---|---|---|
| Per-Phase (CA-F4) | 17 | 16 | 0 | 1 |
| Final (CA-FINAL) | 5 | 4 | 0 | 1 (parcial) |

## 5. Definição de Pronto (CLAUDE.md §8)

- Testes da spec passando, incluindo determinismo (E2E-BE-20 compara hashes dos ticks 0/1/100/1200 com o smoke): SIM.
- Sem `float`, `System.Random` ou id de conteúdo em `src/Sim` (varreduras UT-BE-112/ContentIdScanner verdes): SIM.
- Nenhuma regra de jogo em `game/scripts` (só leitura, câmera, desenho, smoke; `InternalsVisibleTo` restrito a `Rts.Sim.Tests`, UT-BE-162): SIM.
- Dados validados pelo carregador (inclusive via `.pck`): SIM.
- `docs/history.md` atualizado (START/END com tempo, linhas SMOKE, tamanho do build): SIM.
- Ordem testes-primeiro registrada no history (17 testes escritos antes): SIM.

## 6. Critério de saída do Marco 0

`dotnet test` verde (196, com determinismo) e o jogo executa a Sim a 20 ticks/s (19998 milli-ticks/s em 60 s no editor; 19986 no exportado), com HUD correto e dados de `res://`. Falta apenas a confirmação visual da grade (MAN-01). Fase considerada cumprida, condicionada ao MAN-01.

## 7. Findings

### CRITICAL
Nenhum.

### MAJOR
Nenhum.

### MINOR

| # | Seção | Descrição | Sugestão |
|---|---|---|---|
| m-01 | E2E-GD-01 | O `rm -rf game/.godot` do controle "build a frio" não foi repetido nesta revisão (bloqueio da checagem de segurança do ambiente); `--build-solutions` foi verificado sobre o cache existente (código 0, DLLs presentes). O implementador relatou o ciclo a frio no history. | Se desejado, o operador repete `rm -rf game/.godot && $GODOT --headless --path game --build-solutions --quit` e confere as duas DLLs (X-04). |

### OBSERVATION

| # | Descrição |
|---|---|
| O-01 | **MAN-01 pendente (humano)**: abrir `$GODOT --path game` e conferir grade 32x32 centrada, HUD com 4 linhas, "Taxa" entre 19.0 e 21.0, WASD e roda, tick subindo com a câmera em movimento; registrar OK/falha no `history.md` (fecha CA-F4-13). Não é falha da implementação. |
| O-02 | `SmokeRun` e `HudTick` repetem o literal `"Recursos: 5 · Techs: 4"` no veredito (permitido por RN-P4-67 apenas no `SmokeRun`; o `HudTick` monta a string a partir das contagens). Sem ação. |
| O-03 | A spec (§12.4/§13) falava em autorização do usuário para os templates; o history registra a instalação (SHA-512 conferido, ~1,2 GB). `build/linux` fica fora do git (147 MB). Sem ação. |

## 8. Pontos positivos

- Extração de `TickClock`/`SimLoop` para a Sim: lógica de 20 ticks/s e descarte testada no xUnit, sem Godot; o nó Godot ficou um adaptador fino.
- Verificação automática de CA-11/12/13 por comando, com linha `SMOKE` legível por máquina e hash no tick 100 igual ao do teste headless.
- Exportação provada com cópia isolada sem `data/`, `source=res exported=1`.
- Fronteira da apresentação protegida por testes de arquitetura (UT-BE-162..165).
