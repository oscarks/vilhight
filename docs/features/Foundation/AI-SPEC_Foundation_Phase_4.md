# AI-SPEC: Foundation — Fase 4: Apresentação (projeto Godot, `SimHost` a 20 ticks/s, grade isométrica, câmera mínima e HUD)

**Data**: 2026-11-23
**Status**: Aprovado com uma mudança (2026-11-23: a exportação Linux entra nesta fase; revisão 2 desta spec)
**Modulo**: `game/` (novo: `project.godot`, `Rts.Game.csproj`, `Rts.Game.sln`, `export_presets.cfg`, `scenes/Main.tscn`, `scripts/*.cs`), `src/Sim` (novo: `Hosting/`), `src/Sim.Tests` (novo: `Hosting/`, `Architecture/PresentationBoundaryTests.cs`), `Rts.sln` e `.gitignore` (modificados), `tools/smoke/` (novo), `docs/architecture/adr/ADR-009` (novo, Proposed)
**Pre-requisitos**:
- Fase 1 (Base): Implementado e revisado (`Review_Phase_1.md`: APPROVED_WITH_OBSERVATIONS). `Fixed`, `SimRandom`, `GameState`, ids fortes, `src/Directory.Build.props` (DD-P1-01: props em `src/`, não na raiz).
- Fase 2 (Ciclo): Implementado e revisado (`Review_Phase_2.md`). `Simulation.Step`, `SimEvents`, `CommandRegistry`, `CommandQueue`, `IController`, `HumanController`, `AiController`, `ControllerSet`, `StateHasher`, `ForbiddenApiScanner`, contrato do laço de host (Fase 2 §5.8.4).
- Fase 3 (Dados): Implementado e revisado (`Review_Phase_3.md`, 179 testes verdes). `DataLoader.Load`, `DataSet`, `DataValidationException`, `GameState.Create(GameSetup, DataSet)`, `game/data/resources.json` (5) e `techs.json` (4), `ContentIdScanner`.
- .NET SDK 8.0.428 em `~/.dotnet`: Implementado (rodar com `zsh -ic '...'`).
- Godot `4.7.2.stable.mono.official.ed1daf0bf` em `~/bin/godot4` (`$GODOT` definido no perfil do zsh): Implementado.
**Dependencias externas**:
- Godot 4.7.2 .NET (binário acima).
- **Export templates do Godot 4.7.2 mono: Pendente, precisa de autorização do usuário antes do `/implement`** (§12.4). Verificado em 2026-11-23: `~/.local/share/godot/export_templates/` só tem `editor_settings-4.7.tres`, `feature_profiles/`, `script_templates/`, `text_editor_themes/` e `editor_doc_cache-4.7.res`; não existe `4.7.2.stable.mono/`. O Godot espera `~/.local/share/godot/export_templates/4.7.2.stable.mono/linux_debug.x86_64` e `.../linux_release.x86_64` (mensagem real do `--export-release`, X-15). Nada foi baixado.
- MSBuild SDK `Godot.NET.Sdk/4.7.2` (NuGet; traz `GodotSharp` 4.7.2 e `Godot.SourceGenerators` 4.7.2). É o SDK obrigatório de todo projeto Godot .NET. Ver §13 (aprovação).
- Nenhum pacote novo em `Rts.Sim` nem em `Rts.Sim.Tests`.
**Proposal**: `docs/features/Foundation/Proposal_Foundation.md` (Fase 4 da §16: história E0-9 e a parte Godot da E0-1; DD-06 aprovado: câmera mínima WASD + zoom)
**ADRs relacionados**: ADR-001 (Sim sem Godot; apresentação só envia comandos e lê estado; conversão `Fixed → float` na borda), ADR-003 (passo fixo de 50 ms, acumular tempo real, interpolar), ADR-004 (comandos como única via de mudança; `IController` comum), ADR-002 (o relógio da Sim não lê tempo real; só inteiros), ADR-005 (nenhum id de conteúdo no código), **ADR-009 (novo, Proposed: o jogo lê `res://data` com `FileAccess` e passa o texto a `DataLoader.Parse`; diverge do CLAUDE.md §5.1)**. ADR-006, 007 e 008 não se aplicam.

---

## 2. Descricao

Esta fase liga a simulação ao Godot e fecha o Marco 0. Ela cria o projeto Godot .NET em `game/`, que referencia `Rts.Sim`, e uma cena `Main` com quatro partes: o `SimHost`, que carrega `game/data`, monta o estado inicial e chama `Step` a 20 ticks/s com o tempo real acumulado; uma grade isométrica 2:1 desenhada por código (placeholder); uma câmera mínima com WASD e zoom pela roda do mouse (DD-06); e um HUD com o tick, a taxa medida, o hash curto e as contagens de conteúdo carregado.

O acúmulo de tempo e o laço de host ficam em duas classes puras dentro da Sim (`Rts.Sim.Hosting.TickClock` e `SimLoop`). Elas só fazem aritmética de inteiros sobre o tempo que o host informa, e por isso são testáveis headless no xUnit. O nó Godot vira um adaptador fino. A fase também cria um modo de verificação headless (`--smoke-seconds=N`) e um script que o executa, para que o CA-11 (20 ± 1 ticks/s em 60 s), o CA-12 (HUD) e o CA-13 (câmera sem efeito na Sim) sejam verificados por comando e não só a olho.

A fase também entrega a exportação Linux x86_64 (`export_presets.cfg`, com `data/*.json` no filtro de não-recursos, como pede o CLAUDE.md §7). Como `GlobalizePath("res://data")` não funciona num build exportado, o jogo passa a ler os dados de `res://data` com `FileAccess` e a entregar o texto à Sim por `DataLoader.Parse(RawData)`, que já existe desde a Fase 3 (ADR-009). Um script exporta o jogo, copia o resultado para uma pasta isolada sem `data/` e roda o mesmo smoke no executável exportado.

Valor para o usuário (autor/jogador): abrir o jogo e ver, na tela, a prova de que a Sim determinística está rodando no ritmo certo, ligada ao Godot pela fronteira aprovada (SC-05, UC-PLY-01, UC-PLY-02).

**Capacidades**
1. Compilar o projeto Godot junto com a solução (`dotnet build Rts.sln`) e pelo Godot (`$GODOT --headless --path game --build-solutions --quit`).
2. Carregar `res://data` (no editor, `game/data`; no exportado, dentro do `.pck`) com `FileAccess` + `DataLoader.Parse`, e mostrar o erro de dados no HUD quando a validação falhar (ADR-009).
3. Executar a Sim a 20 ticks/s, com no máximo 5 ticks por quadro e descarte do excesso (RN-25 da proposta), e expor o fator de interpolação para a renderização.
4. Desenhar uma grade isométrica 2:1 de 32 × 32 losangos.
5. Mover a câmera com WASD e mudar o zoom com a roda do mouse, sem tocar a Sim.
6. Mostrar no HUD: tick, taxa medida, hash curto (8 hex) e "Recursos: 5 · Techs: 4".
7. Verificar tudo isso de forma automática e headless, com código de saída e uma linha `SMOKE` legível por máquina.
8. Exportar para Linux x86_64 e rodar o smoke no executável exportado, provando que os dados vêm do `.pck`.

## 3. Escopo

### 3.1 In Scope (Fase 4 da proposta, §16)

1. **E0-1 (parte Godot)**: `game/project.godot`, `game/Rts.Game.csproj` (referencia `../src/Sim/Rts.Sim.csproj`, não herda `src/Directory.Build.props`), inclusão do projeto em `Rts.sln` (DD-P4-01).
2. **E0-9**: cena `game/scenes/Main.tscn` e scripts `SimHost`, `IsoGrid`, `CameraRig`, `HudTick`, mais `DemoSetup`, `FixedConvert` e `SmokeRun`.
3. `src/Sim/Hosting/TickClock.cs` e `src/Sim/Hosting/SimLoop.cs` (DD-P4-03), com testes headless.
4. Testes de fronteira da apresentação em `src/Sim.Tests/Architecture/PresentationBoundaryTests.cs`.
5. `tools/smoke/godot_smoke.sh`: build + execução headless com verificação (CA-11, CA-12, CA-13).
6. Mapa de entrada `camera_left/right/up/down` (A/D/W/S, teclas físicas) em `project.godot`.
7. **Exportação Linux x86_64** (DD-P4-13): `game/export_presets.cfg` (preset `Linux`, `include_filter="data/*.json"`, `.pck` separado), `game/scripts/ResDataReader.cs`, `tools/smoke/export_smoke.sh`, `build/` no `.gitignore`.
8. **ADR-009** (Proposed) e a carga dos dados por texto no jogo.

### 3.2 Out of Scope

1. **Câmera completa** (bordas da tela, setas, arrasto com botão do meio, limites do mapa, zoom mínimo/máximo configurável): E1-4 (DD-06 da proposta, aprovado). Aqui há só um limite fixo de segurança de zoom (DD-P4-09).
2. **Mapa real, terreno, conversão tela ↔ mundo ↔ grade e depth sorting**: E1-1 e E1-2. A projeção isométrica usada para desenhar a grade é privada de `IsoGrid` e será substituída no E1-2.
3. **Interpolação visual de entidades**: não há entidades (proposta §2, OUT). Esta fase só calcula e expõe o fator `InterpolationAlpha` (DD-P4-04).
4. **Comandos de jogo e envio de comandos pela interface**: não existe nenhum comando de jogo no E0 (DD-07 da proposta). O `HumanController` é criado e ligado, mas a apresentação não emite comandos nesta fase (DD-P4-06). Seleção e ordens: E2.
5. **Outras exportações**: Windows, macOS, Web, `dedicated_server`, PCK embutido no executável, criptografia do PCK, assinatura e empacotamento (AppImage/Flatpak). O M0 só precisa provar o build Linux. Também ficam fora a instalação automática dos export templates (é um passo manual autorizado pelo usuário, §12.4), CI de exportação e suporte a mods (editar dados de um build exportado só via `--data-dir`, ADR-009).
6. **Troca de país, seletor de países, painel "o que aconteceu"**: E7 e E12.
7. **Thread própria para a Sim**: ADR-003 e Q2 da arquitetura fixam a thread principal na Fase 1; medir no E3-3.
8. **Teste automatizado de imagem (screenshot)**: o modo headless usa o renderizador dummy e não desenha. A grade é verificada por inspeção manual (MAN-01).
9. **Projeto de teste para `game/`** (xUnit carregando `GodotSharp`): os tipos do Godot só funcionam dentro do motor. A lógica testável foi movida para `Rts.Sim.Hosting` (DD-P4-03) e o resto é coberto pelo smoke headless.
10. **i18n**: `capabilities.i18n = false`. Textos do HUD em português, literais.

## 4. Situacao Atual

| Aspecto | Estado | Caminho/Referencia |
|---|---|---|
| Solução | Existe, só `Rts.Sim` e `Rts.Sim.Tests` (pasta de solução `src`) | `Rts.sln` — **MODIFY** |
| Flags de build da Sim | Existe; vale só abaixo de `src/` (DD-P1-01) | `src/Directory.Build.props`, `src/Directory.Packages.props` |
| `Directory.Build.props` acima de `game/` | Não existe (verificado: raiz do repo e diretórios pais até `/`) | — |
| Projeto Godot | Não existe | `game/` só tem `data/` |
| Dados de exemplo | Existe (5 recursos, 4 techs; carregam com 0 erros e 4 avisos) | `game/data/resources.json`, `game/data/techs.json` |
| `Simulation`, `SimEvents` | Existe | `src/Sim/Core/Simulation.cs`, `SimEvents.cs` (buffer reutilizado; `CompletedTick`, `AppliedCount`, `Rejections`) |
| Fila e controladores | Existe | `src/Sim/Commands/CommandQueue.cs` (`TakeDue(tick, output)`), `src/Sim/Control/{ControllerSet,HumanController,AiController}.cs` |
| Laço de host | Existe só como contrato | AI-SPEC Fase 2 §5.8.4 (`buffer.Clear(); queue.TakeDue(...); controllers.Collect(...); sim.Step(buffer)`) |
| Hash | Existe | `StateHasher.Compute`, `Simulation.ComputeHash()`; `Refs.S0Hash = 210089492672693724` |
| Fixture de setup | Existe | `src/Sim.Tests/Fixtures/SetupFactory.cs` (`OneSystemThreeCountries(42)`) — **não modificar** |
| Varreduras | Existe | `ForbiddenApiScanner` (UT-BE-112 varre todo `src/Sim`, inclusive a nova pasta `Hosting`), `ContentIdScanner.Scan(root, relativeDir, keys)` |
| Encapsulamento | Existe | `Rts.Sim.csproj`: `InternalsVisibleTo("Rts.Sim.Tests")` apenas; setters de estado são `internal` |
| `.gitignore` | Existe e já cobre o Godot | `.godot/`, `.import/`, `bin/`, `obj/`, `*.user` (verificado com `git check-ignore`) — **MODIFY**: + `build/` (saída da exportação) |
| `tools/` | Não existe | — |
| Entrada de dados por texto | Existe | `DataLoader.Parse(RawData)`, `RawData(string? ResourcesJson, string? TechsJson)` (`null` ⇒ `FileMissing`); limite de 1 MiB medido em bytes UTF-8 do texto (`DataCompiler` etapa 1); nomes em `DataFiles.Resources`/`DataFiles.Techs` (públicos) |
| Export templates | **Não existem no host** | ver Dependências externas e §12.4 |

**Experimentos no ambiente real (2026-11-23, cópias descartáveis no scratchpad; nada foi criado no repositório):**

| # | Experimento | Resultado |
|---|---|---|
| X-01 | `dotnet build game/Rts.Game.csproj` com `Sdk="Godot.NET.Sdk/4.7.2"`, `ProjectReference` para a Sim | 0 avisos, 0 erros. Saída em `game/.godot/mono/temp/bin/Debug/Rts.Game.dll`. O pacote `godot.net.sdk/4.7.2` foi baixado de nuget.org para `~/.nuget/packages` nesse momento |
| X-02 | Mesmo projeto com `Nullable=enable`, `TreatWarningsAsErrors=true`, `ImplicitUsings=disable`, `LangVersion=12.0` | 0 avisos (os geradores de código do Godot não geram aviso) |
| X-03 | `Rts.sln` com o projeto do jogo (`dotnet sln add --solution-folder game`), build limpo | `dotnet build Rts.sln` 0/0; `dotnet test src/Sim.Tests` 179/179; `dotnet format Rts.sln --verify-no-changes` código 0 |
| X-04 | `$GODOT --headless --path game --build-solutions --quit` com `.godot/` apagado, 3 vezes | código 0 e `Rts.Game.dll` + `Rts.Sim.dll` em `.godot/mono/temp/bin/Debug/`. **Na primeiríssima execução no host** (antes do X-01 ter populado o cache), o comando devolveu 0 mas deixou a DLL só em `obj/`; nas execuções seguintes, sempre completo. Por isso o E2E-GD-01 confere a DLL, não só o código de saída |
| X-05 | `--build-solutions` com erro de compilação | código **1**, mensagem `Command line option --build-solutions was passed, but the build callback failed` |
| X-06 | `--build-solutions` gera `scripts/<Nome>.cs.uid` | sim (Godot 4.4+); a execução do jogo sem editor não gera |
| X-07 | Jogo headless após só `dotnet build` (sem editor) | roda; `ProjectSettings.GlobalizePath("res://data")` devolve o caminho absoluto de `game/data`; `DataLoader.Load` dá 5/4 |
| X-08 | Medição headless de 60 s (acúmulo por tempo real, 50 000 µs por tick, máx. 5/quadro) | **1200 ticks em 60,007 s**; ~145 quadros/s no headless; soma dos `delta` = 60,082 s (o 1º quadro inclui ~75 ms de carga) |
| X-09 | `Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.D, Pressed = true })` no headless | `Input.IsActionPressed("camera_right")` vira `true` no quadro seguinte; a câmera anda; soltar volta a `false` |
| X-10 | `Input.ParseInputEvent(InputEventMouseButton WheelUp)` no headless | chega a `_UnhandledInput` da `Camera2D` |
| X-11 | `GetTree().Quit(7)` | código de saída do processo = 7 |
| X-12 | Cena com `unique_name_in_owner`, `GetNode<T>("%SimHost")`, `process_priority = -100`, `Label` em `CanvasLayer`, `DrawColoredPolygon`/`DrawPolyline` | funciona no headless (`_Draw` é chamado) |
| X-13 | Hashes de referência (`OneSystemThreeCountries(42)`, 5 recursos, `Step` sem comandos) | tick 0: `210089492672693724` (= `S0Hash`); tick 1: `5242866997788935815`; tick 100: `8913928992633490408` (`0x7bb4a330d6f98be8`); tick 1200: `14084532596178668976` (`0xc3764fc5eea3cdb0`) |
| X-14 | Interpolação de string com `F3` | usa a cultura do sistema (pt-BR: vírgula). Toda saída de máquina usa `CultureInfo.InvariantCulture` |
| X-15 | `export_presets.cfg` da §5.3.10 + `$GODOT --headless --path game --export-release "Linux" <abs>/build/linux/Rts.x86_64`, **sem templates** | preset reconhecido; código **1**; mensagem `Cannot export project with preset "Linux" due to configuration errors:` + "Nenhum modelo de exportação encontrado no caminho esperado: `/home/oscar/.local/share/godot/export_templates/4.7.2.stable.mono/linux_debug.x86_64`" e o mesmo para `linux_release.x86_64`. Com nome inexistente: `Invalid export preset name: Nope.` |
| X-16 | No editor (`--path game`): `FileAccess.FileExists("res://data/" + DataFiles.Resources)`, `FileAccess.Open(...).GetBuffer(DataLimits.MaxFileBytes + 1)`, decodificação UTF-8, `DataLoader.Parse(new RawData(..))` | 5 recursos, 4 techs; arquivo inexistente ⇒ `FileExists == false`; `OS.HasFeature("editor") == true` ao rodar com o binário do editor |
| X-17 | Documentação do `GodotSharp.xml` 4.7.2, `ProjectSettings.GlobalizePath` | "`GlobalizePath` com `res://` não funciona num projeto exportado" (motivo do ADR-009) |

**Não verificado (depende dos templates, que não foram baixados):** se o build exportado lê `res://data/*.json` do `.pck` e se o template de release aceita `--headless`. Ambos são o objeto do E2E-GD-05; se um deles falhar, o `/implement` para e reporta (não há alternativa silenciosa).

**Arquivos modificados:** `Rts.sln` e `.gitignore`. (Revisão 3: `game/Rts.Game.sln` também é novo, DD-P4-16.) Todo o resto é novo. Nenhum arquivo de `src/Sim` existente muda.

## 5. Solucao

### 5.1 Arquitetura Geral

```
 game/ (Rts.Game.csproj, Godot.NET.Sdk/4.7.2)                       src/Sim (Rts.Sim, C# puro)
 ┌──────────────────────────────────────────────────────┐         ┌──────────────────────────────────┐
 │ Main (Node2D)                                         │         │ Hosting/                          │
 │  ├ SimHost (Node, priority -100)                      │ µs      │  TickClock.Advance(µs) → n (0..5) │
 │  │   _Ready: ResDataReader.Read(res://data) → RawData │────────►│  SimLoop.Tick() ×n                │
 │  │     → DataLoader.Parse → GameState.Create(Demo..)  │         │    queue.TakeDue → controllers    │
 │  │   _Process(delta): µs = round(delta·10⁶)           │◄────────│    .Collect → Simulation.Step     │
 │  │   evento Stepped(stateTick)                        │ events  │ Core/ Simulation, SimEvents, Hash │
 │  ├ IsoGrid (Node2D) — só _Draw, não lê a Sim          │         │ Control/ Human/Ai/ControllerSet   │
 │  ├ CameraRig (Camera2D) — WASD + roda, não lê a Sim   │         │ Data/ DataLoader, DataSet         │
 │  ├ Hud (CanvasLayer) └ HudTick (Label) — lê SimHost   │         └──────────────────────────────────┘
 │  └ SmokeRun (Node) — inerte sem --smoke-seconds       │                     ▲ lê
 └──────────────────────────────────────────────────────┘   res://data/*.json (editor: game/data; exportado: Rts.pck)
        ▲ FixedConvert: Fixed → float (só aqui)             --data-dir=X (diagnóstico) → DataLoader.Load(X) (disco)
 tools/smoke/godot_smoke.sh  → dotnet build Rts.sln → $GODOT --headless --path game -- --smoke-seconds=N
 tools/smoke/export_smoke.sh → $GODOT --export-release "Linux" build/linux/Rts.x86_64 → cópia isolada (sem data/)
                               → ./Rts.x86_64 --headless -- --smoke-seconds=N
```

Integração com as fases anteriores: `SimLoop` implementa exatamente o contrato da Fase 2 §5.8.4; `TickClock` implementa a RN-25 da proposta; `SimHost` usa `DataLoader.Parse(RawData)` e `GameState.Create(GameSetup, DataSet)` da Fase 3 (ADR-009). Nenhuma API existente muda.

Opção descartada: usar o passo de física do Godot (`physics/common/physics_ticks_per_second = 20`, `max_physics_steps_per_frame = 5`) como relógio. Ele resolve o acúmulo, mas não é testável headless no xUnit, acopla a Sim ao motor de física e esconde a regra de descarte (DD-P4-07).

### 5.2 Estrutura de Arquivos

```
Rts.sln                                   # MODIFY: + projeto Rts.Game (pasta de solução "game")
.gitignore                                # MODIFY: + "build/" numa seção "# Exportação" no fim do arquivo
docs/architecture/adr/
└── ADR-009_dados-do-jogo-lidos-de-res-como-texto.md   # NOVO (já escrito com esta spec; Status: Proposed)
src/
├── Sim/
│   └── Hosting/
│       ├── TickClock.cs                  # NOVO (§5.3.1)
│       └── SimLoop.cs                    # NOVO (§5.3.2)
└── Sim.Tests/
    ├── Hosting/
    │   ├── TickClockTests.cs             # NOVO: UT-BE-150..156
    │   └── SimLoopTests.cs               # NOVO: UT-BE-157..161, E2E-BE-20
    └── Architecture/
        └── PresentationBoundaryTests.cs  # NOVO: UT-BE-162..165
game/
├── project.godot                         # NOVO (§5.3.4, conteúdo exato)
├── Rts.Game.csproj                       # NOVO (§5.3.3, conteúdo exato)
├── Rts.Game.sln                          # NOVO (DD-P4-16): só o Rts.Game.csproj; exigido pelo export de C# do Godot; versionado
├── export_presets.cfg                    # NOVO (§5.3.10, conteúdo exato)
├── data/                                 # EXISTENTE — não modificar
├── scenes/
│   └── Main.tscn                         # NOVO (§5.3.5, conteúdo exato)
└── scripts/
    ├── SimHost.cs                        # NOVO (§5.3.6)
    ├── DemoSetup.cs                      # NOVO (§5.3.7)
    ├── FixedConvert.cs                   # NOVO (§5.3.8)
    ├── IsoGrid.cs                        # NOVO (§6.2)
    ├── CameraRig.cs                      # NOVO (§6.2)
    ├── HudTick.cs                        # NOVO (§6.2)
    ├── SmokeRun.cs                       # NOVO (§5.3.9)
    ├── ResDataReader.cs                  # NOVO (§5.3.11, ADR-009)
    └── *.cs.uid                          # GERADOS pelo E2E-GD-01 (X-06) — versionar
build/linux/                              # GERADO pela exportação (E2E-GD-05) — ignorado pelo git
tools/
└── smoke/
    ├── godot_smoke.sh                    # NOVO (§11.4), executável (chmod +x)
    └── export_smoke.sh                   # NOVO (§11.4), executável (chmod +x)
```

Não criar `game/Directory.Build.props` nem `icon.svg`. Não versionar nada de `build/`.

### 5.3 Modelos de Dados

Não há banco nem JSON novo. Os "modelos" são tipos C# e arquivos de projeto.

#### 5.3.1 `src/Sim/Hosting/TickClock.cs`

```csharp
namespace Rts.Sim.Hosting;

// ADR-003; RN-25 da proposta. Só aritmética de inteiros sobre o tempo que o host informa (nunca lê relógio).
public sealed class TickClock
{
    public const long TickMicros = 50_000;      // 20 ticks/s
    public const int MaxTicksPerAdvance = 5;

    public long AccumulatedMicros { get; private set; }   // 0 no início
    public long DroppedMicros { get; private set; }       // total descartado; 0 no início
    public long TotalTicks { get; private set; }          // soma dos retornos de Advance; 0 no início

    // Fração do próximo tick já acumulada, em [0, 1). Fixed.FromRatio(AccumulatedMicros, TickMicros).
    public Fixed Alpha { get; }

    // Soma elapsedMicros e devolve quantos ticks o host deve executar agora (0..MaxTicksPerAdvance).
    public int Advance(long elapsedMicros);
}
```

Invariantes:
- `INV-TC-01`: `0 <= AccumulatedMicros < TickMicros` depois de qualquer chamada (inclusive a que lança).
- `INV-TC-02`: `0 <= Alpha.Raw < Fixed.One.Raw`.
- `INV-TC-03`: `DroppedMicros >= 0` e nunca diminui; `TotalTicks >= 0` e nunca diminui.
- `INV-TC-04` (conservação): `TotalTicks * TickMicros + AccumulatedMicros + DroppedMicros == soma de todos os elapsedMicros aceitos`.

#### 5.3.2 `src/Sim/Hosting/SimLoop.cs`

```csharp
namespace Rts.Sim.Hosting;

// Contrato do laço de host da AI-SPEC Fase 2 §5.8.4.
public sealed class SimLoop
{
    public const int CommandBufferCapacity = 64;

    // Pré-condição não verificável (ControllerSet não expõe o estado): controllers foi criado com
    // simulation.State. Quebrá-la faz Collect ler outro estado; não há uso assim no código.
    public SimLoop(Simulation simulation, ControllerSet controllers, CommandQueue queue);

    public Simulation Simulation { get; }
    public ControllerSet Controllers { get; }
    public CommandQueue Queue { get; }

    // null antes do primeiro Tick; depois, o SimEvents devolvido pelo último Step (mesma instância sempre).
    public SimEvents? LastEvents { get; }

    // Executa 1 tick: buffer.Clear(); Queue.TakeDue(State.Tick, buffer); Controllers.Collect(buffer);
    // LastEvents = Simulation.Step(buffer); devolve LastEvents.
    public SimEvents Tick();
}
```

Campo privado `List<Command> _buffer = new(CommandBufferCapacity)`, criado uma vez no construtor.

Invariantes:
- `INV-SL-01`: depois de `Tick()`, `Simulation.State.Tick == (valor antes) + 1` e `LastEvents.CompletedTick == (valor antes)`.
- `INV-SL-02`: `Queue.LastTakenTick == Simulation.State.Tick - 1` depois de cada `Tick()`.

#### 5.3.3 `game/Rts.Game.csproj` (conteúdo exato)

```xml
<Project Sdk="Godot.NET.Sdk/4.7.2">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <AssemblyName>Rts.Game</AssemblyName>
    <RootNamespace>Rts.Game</RootNamespace>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../src/Sim/Rts.Sim.csproj" />
  </ItemGroup>
</Project>
```

O projeto está em `game/`, fora de `src/`, então o MSBuild não importa `src/Directory.Build.props` nem `src/Directory.Packages.props` (DD-P1-01). As flags acima repetem as da Sim, exceto `Deterministic` e `CheckForOverflowUnderflow`, que não importam fora da Sim (DD-P4-02).

#### 5.3.4 `game/project.godot` (conteúdo exato)

```ini
; Engine configuration file.
; Gerado à mão na AI-SPEC Foundation Fase 4; o editor pode reordenar seções ao salvar.

config_version=5

[application]

config/name="Rts"
run/main_scene="res://scenes/Main.tscn"
config/features=PackedStringArray("4.7", "C#")

[dotnet]

project/assembly_name="Rts.Game"

[input]

camera_left={
"deadzone": 0.2,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":65,"key_label":0,"unicode":97,"location":0,"echo":false,"script":null)
]
}
camera_right={
"deadzone": 0.2,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":68,"key_label":0,"unicode":100,"location":0,"echo":false,"script":null)
]
}
camera_up={
"deadzone": 0.2,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":87,"key_label":0,"unicode":119,"location":0,"echo":false,"script":null)
]
}
camera_down={
"deadzone": 0.2,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":83,"key_label":0,"unicode":115,"location":0,"echo":false,"script":null)
]
}
```

`physical_keycode` 65/68/87/83 = A/D/W/S físicas (independe do layout do teclado). `assembly_name` tem de ser igual ao `AssemblyName` do `.csproj`.

#### 5.3.5 `game/scenes/Main.tscn` (conteúdo exato)

```ini
[gd_scene format=3]

[ext_resource type="Script" path="res://scripts/SimHost.cs" id="1_host"]
[ext_resource type="Script" path="res://scripts/IsoGrid.cs" id="2_grid"]
[ext_resource type="Script" path="res://scripts/CameraRig.cs" id="3_cam"]
[ext_resource type="Script" path="res://scripts/HudTick.cs" id="4_hud"]
[ext_resource type="Script" path="res://scripts/SmokeRun.cs" id="5_smoke"]

[node name="Main" type="Node2D"]

[node name="SimHost" type="Node" parent="."]
unique_name_in_owner = true
process_priority = -100
script = ExtResource("1_host")

[node name="IsoGrid" type="Node2D" parent="."]
script = ExtResource("2_grid")

[node name="CameraRig" type="Camera2D" parent="."]
unique_name_in_owner = true
position = Vector2(0, 512)
script = ExtResource("3_cam")

[node name="Hud" type="CanvasLayer" parent="."]

[node name="HudTick" type="Label" parent="Hud"]
unique_name_in_owner = true
offset_left = 8.0
offset_top = 8.0
script = ExtResource("4_hud")

[node name="SmokeRun" type="Node" parent="."]
script = ExtResource("5_smoke")
```

Ordem de processamento por quadro (X-12): `SimHost` primeiro (`process_priority = -100`), depois os demais em ordem de árvore: `IsoGrid`, `CameraRig`, `HudTick`, `SmokeRun`. Assim o HUD e o smoke sempre veem o estado do quadro atual. `position = (0, 512)` é o centro da grade (§6.2, RN-P4-50). Se o editor acrescentar `uid=` aos `ext_resource` ao salvar, é aceitável.

#### 5.3.6 `game/scripts/SimHost.cs`

```csharp
namespace Rts.Game;

public partial class SimHost : Godot.Node
{
    public const string DataResPath = "res://data";
    public const string DataDirArgPrefix = "--data-dir=";

    public DataSet? Data { get; private set; }
    public string DataSource { get; private set; } = "";      // "res://data" ou o caminho de --data-dir (para o smoke)
    public Simulation? Simulation { get; private set; }
    public HumanController? LocalHuman { get; private set; }   // controlador de DemoSetup.LocalPlayer
    public string? LoadError { get; private set; }             // mensagem da DataValidationException
    public bool IsRunning => Simulation is not null;
    public float InterpolationAlpha { get; }                   // FixedConvert.ToFloat(_clock.Alpha); 0 se !IsRunning
    public long DroppedMicros { get; }                         // _clock.DroppedMicros; 0 se !IsRunning

    // Disparado depois de cada Step, com State.Tick já incrementado (1, 2, 3, ...).
    public event System.Action<long>? Stepped;

    public override void _Ready();                 // RN-P4-20..23
    public override void _Process(double delta);   // RN-P4-22

    // Auxiliar público e estático para o SmokeRun e o HUD não duplicarem a regra.
    public static long DeltaToMicros(double delta); // RN-P4-22
}
```

Campos privados: `TickClock _clock`, `SimLoop? _loop`.

#### 5.3.7 `game/scripts/DemoSetup.cs`

Setup de demonstração do M0. Espelha **exatamente** `SetupFactory.OneSystemThreeCountries(42)` (`src/Sim.Tests/Fixtures/SetupFactory.cs`) para que o hash do jogo seja comparável aos valores de referência (X-13). Mapas e fluxo de partida substituem isto no E1/E14.

```csharp
namespace Rts.Game;

public static class DemoSetup
{
    public const ulong Seed = 42;
    public static readonly PlayerId LocalPlayer = new(1);

    // Jogador 1 "Player One"; sistema 1 (dono 1); mundo 1; países:
    //   1 "Alpha" (Human, jogador 1), 2 "Beta" (Ai, jogador 1), 3 "Gamma" (Ai, jogador 1).
    public static GameSetup Create();
}
```

#### 5.3.8 `game/scripts/FixedConvert.cs`

```csharp
namespace Rts.Game;

// ADR-001 / P-06: a única conversão Fixed → float do projeto.
public static class FixedConvert
{
    public static float ToFloat(Fixed value) => (float)(value.Raw / 4294967296.0); // 2^32
}
```

#### 5.3.9 `game/scripts/SmokeRun.cs`

Nó de diagnóstico, inerte sem o argumento `--smoke-seconds=N` (depois de `--` na linha de comando). Regras em RN-P4-60..67; máquina de estados em 5.8.3.

```csharp
namespace Rts.Game;

public partial class SmokeRun : Godot.Node
{
    public const string SecondsArgPrefix = "--smoke-seconds=";
    public const int MinSeconds = 10;
    public const int MaxSeconds = 600;
    public const long HashCheckTick = 100;
    public const ulong ExpectedHashAtCheckTick = 8913928992633490408UL;   // X-13, E2E-BE-20
    public const int ExitPass = 0, ExitFail = 1, ExitDataError = 2, ExitBadArgs = 3;

    public override void _Ready();
    public override void _Process(double delta);
}
```

#### 5.3.10 `game/export_presets.cfg` (conteúdo exato)

```ini
[preset.0]

name="Linux"
platform="Linux"
runnable=true
dedicated_server=false
custom_features=""
export_filter="all_resources"
include_filter="data/*.json"
exclude_filter=""
export_path="../build/linux/Rts.x86_64"
encryption_include_filters=""
encryption_exclude_filters=""
encrypt_pck=false
encrypt_directory=false

[preset.0.options]

custom_template/debug=""
custom_template/release=""
binary_format/embed_pck=false
binary_format/architecture="x86_64"
```

- `include_filter="data/*.json"` é o filtro de arquivos não-recurso do CLAUDE.md §7: garante que `resources.json` e `techs.json` entrem no `.pck` com o texto intacto.
- `embed_pck=false`: a exportação gera `Rts.x86_64` + `Rts.pck` + a pasta de assemblies .NET `data_Rts.Game_linuxbsd_x86_64/` (nome gerado pelo Godot; não confundir com os dados do jogo).
- Este conteúdo foi reconhecido pelo Godot 4.7.2 (X-15). As opções omitidas recebem o padrão do Godot. Se o editor reescrever o arquivo com mais chaves ao salvar, é aceitável, desde que `name`, `platform`, `include_filter`, `export_path`, `embed_pck` e `architecture` mantenham os valores acima (UT-BE-165).
- O Godot compila o C# na configuração `ExportRelease`; `Rts.Sim` é compilada nessa mesma configuração pela `ProjectReference` (sem otimização própria, o que é aceitável no M0).

#### 5.3.11 `game/scripts/ResDataReader.cs` (ADR-009)

```csharp
namespace Rts.Game;

// ADR-009: lê os dados de res:// (editor: pasta game/data; exportado: Rts.pck) e entrega o texto à Sim.
public static class ResDataReader
{
    // resDir sem barra final, ex.: "res://data". Arquivo ausente ⇒ null (a Sim reporta FileMissing).
    public static RawData Read(string resDir);

    // Lê no máximo DataLimits.MaxFileBytes + 1 bytes; remove BOM UTF-8 (EF BB BF);
    // decodifica com new System.Text.UTF8Encoding(false, false). Arquivo inexistente ⇒ null.
    internal static string? ReadText(string resPath);
}
```

Caminhos lidos: `resDir + "/" + DataFiles.Resources` e `resDir + "/" + DataFiles.Techs` (constantes públicas da Sim; nenhum nome de arquivo literal em `game/`). Existência por `Godot.FileAccess.FileExists`; leitura por `FileAccess.Open(path, FileAccess.ModeFlags.Read)` em `using` e `GetBuffer(DataLimits.MaxFileBytes + 1)`. Se `FileAccess.Open` devolver `null` com o arquivo existente, o resultado é `null` e o erro do Godot (`FileAccess.GetOpenError()`) vai para `GD.PushError`.

### 5.4 API pública (substitui "Endpoints"; não há HTTP)

| Tipo/Membro | Descrição | Erros |
|---|---|---|
| `TickClock.Advance(long elapsedMicros) → int` | Acumula e devolve ticks devidos (0..5) | `ArgumentOutOfRangeException` se `elapsedMicros < 0` ou `> long.MaxValue - TickMicros`; estado inalterado |
| `TickClock.Alpha → Fixed` | Fator de interpolação em [0, 1) | — |
| `TickClock.AccumulatedMicros`, `DroppedMicros`, `TotalTicks` | Contadores (INV-TC-04) | — |
| `new SimLoop(Simulation, ControllerSet, CommandQueue)` | Cria o laço | `ArgumentNullException(paramName)` para cada nulo, na ordem dos parâmetros |
| `SimLoop.Tick() → SimEvents` | 1 tick no contrato §5.8.4 da Fase 2 | propaga só o que `Step`/`TakeDue` lançarem (não ocorre com fila e controladores válidos) |
| `SimHost.Stepped` (evento) | `State.Tick` depois de cada `Step` | — |
| `SimHost.InterpolationAlpha`, `LoadError`, `Data`, `Simulation`, `LocalHuman`, `DroppedMicros` | Leitura para HUD/smoke/renderização | — |
| `SimHost.DeltaToMicros(double) → long` | Conversão de `delta` | — |
| `DemoSetup.Create() → GameSetup` | Setup do M0 | — |
| `FixedConvert.ToFloat(Fixed) → float` | Borda numérica | — |
| `ResDataReader.Read(string resDir) → RawData` | Texto dos dois arquivos de `res://` (ADR-009) | nunca lança por arquivo ausente (devolve `null` no campo) |
| `SimHost.DataSource` | `"res://data"` ou o caminho de `--data-dir` | — |
| Linha de comando do jogo (após `--`) | `--smoke-seconds=N` (SmokeRun), `--data-dir=CAMINHO` (SimHost) | argumentos desconhecidos são ignorados |

### 5.5 Formato de entrada e saída (substitui "DTOs")

**Argumentos do usuário** (`OS.GetCmdlineUserArgs()`, que devolve só o que vem depois de `--`):

| Argumento | Regra | Inválido |
|---|---|---|
| `--smoke-seconds=N` | `N` inteiro decimal (`int.TryParse` com `NumberStyles.None`, `InvariantCulture`), `10 <= N <= 600`. Se repetido, vale o último | não inteiro ou fora do intervalo ⇒ `SMOKE result=BAD_ARGS arg=<argumento inteiro>`, saída 3 |
| `--data-dir=CAMINHO` | pasta no disco, absoluta ou relativa ao diretório de trabalho, lida por `DataLoader.Load` (disco, não `res://`); valor vazio é ignorado (usa `res://data`). Se repetido, vale o último. Funciona no editor e no exportado | não há validação própria: o erro aparece como erro de dados (`FileMissing`) |

**Linha de resultado do smoke** (impressa uma única vez com `GD.Print`, campos separados por um espaço, números em `InvariantCulture`):

```
SMOKE result=PASS seconds=60 window_us=60001234 ticks=1200 rate_milli=19999 hash100=8913928992633490408 resources=5 techs=4 camera_moved=1 zoom_in=1 zoom_out=1 hud_ok=1 dropped_us=0 source=res exported=0
SMOKE result=FAIL seconds=60 window_us=... ticks=... rate_milli=... hash100=... resources=... techs=... camera_moved=0|1 zoom_in=0|1 zoom_out=0|1 hud_ok=0|1 dropped_us=... source=res|dir exported=0|1
SMOKE result=DATA_ERROR message=<primeira linha de LoadError>
SMOKE result=BAD_ARGS arg=<argumento>
```

`hash100` é decimal sem sinal; vale `0` se o tick 100 não foi alcançado. Booleanos são `1`/`0`. `source=res` quando os dados vieram de `res://data` (ADR-009) e `source=dir` quando vieram de `--data-dir`. `exported = OS.HasFeature("template") ? 1 : 0` (1 só no binário exportado). `source` e `exported` são informativos: não entram no veredito do `SmokeRun`; quem os exige é o `export_smoke.sh` (E2E-GD-05).

**Texto do HUD** (`HudTick.Text`, 4 linhas separadas por `\n`, `InvariantCulture`):

```
Tick: 1234
Taxa: 20.0 ticks/s
Hash: 1a2b3c4d
Recursos: 5 · Techs: 4
```

- Linha 2 antes da primeira janela de 1 s completa: `Taxa: -- ticks/s`.
- Linha 3: `(hash >> 32).ToString("x8", InvariantCulture)` (os 8 primeiros dígitos de `hash.ToString("x16")`).
- Separador da linha 4: `" · "` (espaço, U+00B7, espaço).
- Com erro de dados: `Erro ao carregar dados:\n` + `LoadError`.

### 5.6 Tratamento de erros

| Ponto | Erro | Condição | Comportamento |
|---|---|---|---|
| `TickClock.Advance` | `ArgumentOutOfRangeException` (`paramName = "elapsedMicros"`) | `elapsedMicros < 0 \|\| elapsedMicros > long.MaxValue - TickMicros` | lança; nenhum contador muda |
| `SimLoop` ctor | `ArgumentNullException` | `simulation`, `controllers` ou `queue` nulo (verificados nessa ordem) | lança |
| `SimHost._Ready` | `DataValidationException` | dados inválidos ou ausentes | `LoadError = ex.Message`; `GD.PushError(ex.Message)`; `SetProcess(false)`; `Simulation` fica `null` |
| `SimHost._Ready` | qualquer outra exceção | erro de programação (ex.: `GameSetupException` do `DemoSetup`) | não é capturada; o Godot registra e o smoke não imprime `PASS` (o script falha por ausência de linha ou código ≠ 0) |
| `SmokeRun._Ready` | argumento inválido | ver 5.5 | `BAD_ARGS`, `Quit(3)` |
| `SmokeRun._Ready` | `host.LoadError != null` e smoke ativo | — | `DATA_ERROR`, `Quit(2)` |
| `SmokeRun` | algum critério falha | RN-P4-64 | `FAIL`, `Quit(1)` |
| `godot_smoke.sh` | build falha / Godot não termina em `N + 60` s / sem linha `SMOKE` | — | sai com o código do `dotnet build`, `124` (timeout) ou o código do Godot; sem linha imprime `SMOKE result=NO_OUTPUT` |
| `ResDataReader.Read` | arquivo ausente em `res://data` | `!FileAccess.FileExists(path)` | campo `null` ⇒ `DataValidationException` com `FileMissing` no `Parse` ⇒ caminho de erro de dados acima |
| `ResDataReader.Read` | arquivo maior que 1 MiB | `GetBuffer` devolveu `MaxFileBytes + 1` bytes | o texto truncado vai ao `Parse`, que acusa `FileTooLarge` (mensagem cita o tamanho lido, `1048577`, não o real) |
| `export_smoke.sh` | templates ausentes | falta `~/.local/share/godot/export_templates/4.7.2.stable.mono/linux_release.x86_64` ou `linux_debug.x86_64` | imprime `SMOKE result=NO_TEMPLATES path=<arquivo>` e sai com **4**, sem chamar o Godot |
| `export_smoke.sh` | exportação falha | `--export-release` ≠ 0, ou faltam `Rts.x86_64`/`Rts.pck` | imprime `SMOKE result=NO_EXPORT` e sai com **5** (X-15: o Godot sai com 1 e explica o motivo) |
| `export_smoke.sh` | o executável exportado não leu do `.pck` | linha sem `source=res` ou sem `exported=1`, ou existe `data/` na pasta isolada | sai com **1** e imprime `SMOKE result=FAIL reason=<source|exported|data_dir>` depois da linha do jogo |

### 5.7 Regras de Negocio

Não há regra de jogo nesta fase. As regras abaixo são de hospedagem e apresentação.

**TickClock (`src/Sim/Hosting`)**
- `RN-P4-01 [BLOCK]`: constantes. Expression: `TickClock.TickMicros == 50_000 && TickClock.MaxTicksPerAdvance == 5`. Violação: UT-BE-150 falha.
- `RN-P4-02 [BLOCK]`: entrada válida. Expression: `0 <= e && e <= long.MaxValue - TickMicros`. Violação: `ArgumentOutOfRangeException`, estado inalterado (UT-BE-153).
- `RN-P4-03 [BLOCK]`: acúmulo sem teto. Com `total = AccumulatedMicros + e` e `due = total / TickMicros` (divisão inteira): se `due <= MaxTicksPerAdvance`, então `n = due` e `AccumulatedMicros = total - n * TickMicros`. Violação: UT-BE-151/154 falham.
- `RN-P4-04 [WARN]`: teto e descarte (RN-25 da proposta). Se `due > MaxTicksPerAdvance`, então `n = MaxTicksPerAdvance`, `DroppedMicros += total - MaxTicksPerAdvance * TickMicros` e `AccumulatedMicros = 0` (descarta inclusive a fração). WARN: o tempo é perdido de propósito e fica contado em `DroppedMicros`. Violação: UT-BE-152.
- `RN-P4-05 [BLOCK]`: `TotalTicks += n`; `Advance` devolve `n`. INV-TC-04 vale sempre.
- `RN-P4-06 [BLOCK]`: `Alpha == Fixed.FromRatio(AccumulatedMicros, TickMicros)`.
- `RN-P4-07 [BLOCK]`: `Advance` e `Alpha` não alocam memória gerenciada (UT-BE-156). Sem `float`/`double` (UT-BE-112 existente).

**SimLoop (`src/Sim/Hosting`)**
- `RN-P4-10 [BLOCK]`: ordem do tick. Expression: os comandos passados a `Step` são, nesta ordem, os de `Queue.TakeDue(State.Tick)` (ordem da fila) seguidos dos de `Controllers.Collect` (ordem de `CountryId`). Violação: UT-BE-158.
- `RN-P4-11 [BLOCK]`: `Tick()` devolve o `SimEvents` do `Step` e o guarda em `LastEvents` (mesma referência). INV-SL-01 e INV-SL-02 valem (UT-BE-159).
- `RN-P4-12 [BLOCK]`: comando agendado para o tick `t` só entra no `Step` executado com `State.Tick == t` (ou depois, se `t` já passou na hora do `Enqueue`, o que a fila já proíbe). Violação: UT-BE-160.
- `RN-P4-13 [BLOCK]`: `Tick()` sem comandos, com `HumanController` e `AiController` ligados, não aloca (UT-BE-161).

**SimHost (`game/scripts`)**
- `RN-P4-20 [BLOCK]`: origem dos dados (ADR-009). Expression: `Data == (existe último --data-dir=X com X != "" ? DataLoader.Load(X) : DataLoader.Parse(ResDataReader.Read(DataResPath)))`, e `DataSource` vale `X` ou `"res://data"`, respectivamente. `ProjectSettings.GlobalizePath` **não** é usado em `game/scripts` (não funciona no exportado, X-17).
- `RN-P4-21 [BLOCK]`: montagem, nesta ordem: `Data` conforme RN-P4-20; `state = GameState.Create(DemoSetup.Create(), Data)`; `Simulation = new Simulation(state, System.Array.Empty<ISimSystem>(), new CommandRegistry())`; `controllers = new ControllerSet(state)`; para cada `country` em `state.Countries` (ordem de id): se `country.Controller.Kind == Human`, liga o `HumanController` do jogador `country.Controller.Player` (um por jogador, criado na primeira vez); senão, liga `new AiController(country.Controller.Player)`; `LocalHuman` = o `HumanController` de `DemoSetup.LocalPlayer` (ou `null` se não houver); `_loop = new SimLoop(Simulation, controllers, new CommandQueue())`; `_clock = new TickClock()`. Nenhum `Step` no `_Ready`.
- `RN-P4-22 [BLOCK]`: por quadro, se `IsRunning`: `n = _clock.Advance(DeltaToMicros(delta))`; repetir `n` vezes: `_loop.Tick()` e depois `Stepped?.Invoke(Simulation.State.Tick)`. `DeltaToMicros(d) = d > 0 ? (long)System.Math.Round(d * 1_000_000.0, System.MidpointRounding.ToEven) : 0`, e se o resultado passar de `long.MaxValue - TickClock.TickMicros` vira esse limite. (O `double` aqui é permitido: está em `game/`.)
- `RN-P4-23 [BLOCK]`: o `_Process` do `SimHost` não aloca quando não há comandos (sem LINQ, sem strings, sem closures novas). Verificado por revisão.
- `RN-P4-24 [BLOCK]`: a apresentação não altera `GameState`. Expression: o único `InternalsVisibleTo` de `Rts.Sim` é `"Rts.Sim.Tests"` (UT-BE-162), então `Rts.Game` não compila se tentar usar `Tick`, `Controller`, `SetStock` ou `Random`. Toda mudança passa por `Step` (ADR-004; RN-26 da proposta).
- `RN-P4-25 [BLOCK]`: `Fixed → float` só em `FixedConvert.ToFloat`. Expression: `grep -rn "\.Raw" game/scripts --include=*.cs` só encontra `FixedConvert.cs`. Violação: revisão rejeita.
- `RN-P4-26 [BLOCK]`: nenhum literal de `game/scripts` contém id de conteúdo de `game/data` (ADR-005; UT-BE-163).
- `RN-P4-27 [BLOCK]`: leitura de `res://` (ADR-009). Para cada arquivo: `!FileAccess.FileExists(p) ⇒ null`; senão `bytes = Open(p).GetBuffer(DataLimits.MaxFileBytes + 1)`; `off = (bytes.Length >= 3 && bytes[0..3] == EF BB BF) ? 3 : 0`; `texto = new UTF8Encoding(false, false).GetString(bytes, off, bytes.Length - off)`. Os nomes vêm de `DataFiles`; `game/scripts` não contém os literais `"resources.json"` nem `"techs.json"` (UT-BE-163). Equivalência: para os arquivos de `game/data`, `Parse(ResDataReader.Read("res://data"))` e `Load(game/data)` dão as mesmas contagens e chaves (verificado por E2E-GD-02, `source=res resources=5 techs=4`, e E2E-BE-10 da Fase 3).

**Exportação (`game/export_presets.cfg`, `tools/smoke/export_smoke.sh`)**
- `RN-P4-70 [BLOCK]`: o preset `Linux` existe com `platform="Linux"`, `include_filter="data/*.json"`, `binary_format/embed_pck=false`, `binary_format/architecture="x86_64"`, `export_path="../build/linux/Rts.x86_64"` (UT-BE-165).
- `RN-P4-71 [BLOCK]`: o build exportado lê os dados do `.pck`. Expression: rodando `Rts.x86_64 --headless -- --smoke-seconds=N` numa pasta que contém só a saída da exportação (sem `data/`), a linha é `result=PASS` **e** contém `source=res` **e** `exported=1` (E2E-GD-05).
- `RN-P4-72 [BLOCK]`: sem os templates, o script não tenta exportar nem baixar nada: `NO_TEMPLATES`, código 4.
- `RN-P4-73 [BLOCK]`: nada em `build/` é versionado (`.gitignore` contém `build/`; `git check-ignore build/linux/Rts.x86_64` encontra a regra).

**HUD (`HudTick`)**
- `RN-P4-30 [BLOCK]`: texto exato da §5.5. O `Text` só é reatribuído quando `State.Tick` mudou desde a última atribuição ou quando a janela de taxa fechou (no máximo 21 atribuições por segundo).
- `RN-P4-31 [BLOCK]`: taxa. Janela de `1_000_000` µs de tempo real (`Time.GetTicksUsec()`), iniciada no primeiro `_Process` com `IsRunning`. Quando `agora - inicio >= 1_000_000`: `taxa = (tickAgora - tickInicio) * 1e6 / (agora - inicio)`, formatada `"0.0"`; a janela recomeça em `agora`/`tickAgora`.
- `RN-P4-32 [BLOCK]`: o HUD não chama `ComputeHash` mais de uma vez por mudança de tick.

**Câmera (`CameraRig`)**
- `RN-P4-40 [BLOCK]`: pan. `Position += Input.GetVector("camera_left", "camera_right", "camera_up", "camera_down") * PanSpeed * (float)delta / Zoom.X`, com `PanSpeed = 800f` (pixels de tela por segundo; a divisão pelo zoom mantém a velocidade na tela constante).
- `RN-P4-41 [BLOCK]`: zoom. Em `_UnhandledInput`, `InputEventMouseButton` com `Pressed == true`: `WheelUp` ⇒ `z = Min(z * ZoomStep, MaxZoom)`; `WheelDown` ⇒ `z = Max(z / ZoomStep, MinZoom)`; `Zoom = new Vector2(z, z)`. `ZoomStep = 1.1f`, `MinZoom = 0.25f`, `MaxZoom = 4f`, zoom inicial `1`. Outros botões são ignorados.
- `RN-P4-42 [BLOCK]`: `CameraRig.cs` e `IsoGrid.cs` não contêm `Rts.Sim` nem `SimHost` (UT-BE-163). A câmera não lê nem altera a Sim (CA-13).

**Grade (`IsoGrid`)**
- `RN-P4-50 [BLOCK]`: `GridWidth = 32`, `GridHeight = 32`, `TileWidth = 64`, `TileHeight = 32` (2:1). Centro da célula `(x, y)`: `cx = (x - y) * TileWidth / 2`, `cy = (x + y) * TileHeight / 2` (em `float`). Losango: `(cx, cy - TileHeight/2)`, `(cx + TileWidth/2, cy)`, `(cx, cy + TileHeight/2)`, `(cx - TileWidth/2, cy)`. Preenchimento `DrawColoredPolygon` com `ColorA = new Color(0.30f, 0.55f, 0.30f)` se `(x + y) % 2 == 0`, senão `ColorB = new Color(0.26f, 0.48f, 0.26f)`; contorno `DrawPolyline` fechado (5 pontos), `new Color(0.15f, 0.30f, 0.15f)`, largura `1f`. Centro da grade = célula (16, 16) = `(0, 512)`, posição inicial da câmera.
- `RN-P4-51 [BLOCK]`: a grade é desenhada uma vez (`_Draw` sem `QueueRedraw` por quadro). Os arrays de pontos podem ser alocados dentro do `_Draw`.

**Smoke (`SmokeRun`)**
- `RN-P4-60 [BLOCK]`: sem `--smoke-seconds=` nos argumentos, `SetProcess(false)` e nada mais (o jogo normal não muda).
- `RN-P4-61 [BLOCK]`: com o argumento: valida (5.5); se `host.LoadError != null`, `DATA_ERROR`; senão assina `host.Stepped` para guardar `hash100 = host.Simulation.ComputeHash()` quando o argumento recebido for igual a `HashCheckTick`.
- `RN-P4-62 [BLOCK]`: janela de medição. No primeiro `_Process`: `startUsec = Time.GetTicksUsec()`, `startTick = State.Tick`. Em cada quadro `t = agora - startUsec` (µs).
- `RN-P4-63 [BLOCK]`: roteiro da câmera, cada passo no primeiro quadro em que `t` atinge o limite, nesta ordem: `t >= 1_000_000`: guarda `P0 = cam.Position`, `Z0 = cam.Zoom.X` e envia `InputEventKey { PhysicalKeycode = Key.D, Pressed = true }` e `{ PhysicalKeycode = Key.S, Pressed = true }` por `Input.ParseInputEvent`; `t >= 2_000_000`: envia os dois com `Pressed = false` e guarda `P1`; `t >= 2_500_000`: envia `InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true }`; `t >= 3_000_000`: guarda `Z1` e envia `WheelDown` com `Pressed = true`; `t >= 3_500_000`: guarda `Z2`. Todos terminam antes do tick 100 (≈ 5 s), então `hash100` prova que a câmera não afetou a Sim.
- `RN-P4-64 [BLOCK]`: fim e veredito, no primeiro quadro com `t >= N * 1_000_000`: `ticks = State.Tick - startTick`; `window_us = t`; `rate_milli = ticks * 1_000_000_000 / window_us` (inteiros `long`). **PASS** se e só se todos: `19_000 <= rate_milli <= 21_000` (CA-11: 20 ± 1 ticks/s); `hash100 == ExpectedHashAtCheckTick`; `resources == 5 && techs == 4` (de `host.Data`; CA-12); `camera_moved = P1.X > P0.X && P1.Y > P0.Y`; `zoom_in = Z1 > Z0`; `zoom_out = Z2 < Z1`; `hud_ok` (RN-P4-65). Imprime a linha (5.5) e `GetTree().Quit(ExitPass | ExitFail)`.
- `RN-P4-65 [BLOCK]`: `hud_ok = linhas[0] == "Tick: " + State.Tick && linhas[2] == "Hash: " + (ComputeHash() >> 32).ToString("x8") && linhas[3] == "Recursos: 5 · Techs: 4"`, com `linhas = hud.Text.Split('\n')` lido no mesmo quadro (o HUD processa antes do `SmokeRun`, 5.3.5).
- `RN-P4-66 [BLOCK]`: o `SmokeRun` não chama `Step`, não envia comandos e não altera o `SimHost`; só lê e simula entrada de câmera.
- `RN-P4-67 [BLOCK]`: valores esperados (`5`, `4`, `ExpectedHashAtCheckTick`) existem só no `SmokeRun` e no teste E2E-BE-20; nenhum outro script os usa.

### 5.8 Fluxos e Diagramas de Sequencia

#### 5.8.1 Caminho feliz (quadro com 1 tick devido)

```mermaid
sequenceDiagram
  participant G as Godot (main loop)
  participant H as SimHost
  participant C as TickClock
  participant L as SimLoop
  participant S as Simulation
  participant U as HudTick
  G->>H: _Process(delta)
  H->>C: Advance(DeltaToMicros(delta))
  C-->>H: n = 1
  H->>L: Tick()
  L->>L: buffer.Clear; Queue.TakeDue; Controllers.Collect
  L->>S: Step(buffer)
  S-->>L: SimEvents
  H-->>H: Stepped(State.Tick)
  G->>U: _Process(delta)
  U->>H: Simulation.State.Tick, ComputeHash(), Data
  U-->>U: Text = "Tick: ..."
```

#### 5.8.2 Caminho de erro: dados inválidos

```mermaid
sequenceDiagram
  participant H as SimHost
  participant D as DataLoader
  participant U as HudTick
  participant R as SmokeRun
  H->>D: Load(dataDir)
  D-->>H: throw DataValidationException
  H-->>H: LoadError = ex.Message; GD.PushError; SetProcess(false)
  U->>H: LoadError != null
  U-->>U: Text = "Erro ao carregar dados:\n" + LoadError
  R->>H: (smoke ativo) LoadError != null
  R-->>R: print "SMOKE result=DATA_ERROR ..."; Quit(2)
```

#### 5.8.3 Máquinas de estados

**SimHost**: estados `Created` (inicial), `Running`, `Failed` (terminais na sessão).
- `Created -> Running [DataLoader.Parse/Load (RN-P4-20) não lançou] (_Ready)`
- `Created -> Failed [DataValidationException] (_Ready)`
- `Running -> Running [qualquer quadro] (_Process)`
- Não há `Failed -> Running` nem volta a `Created` (recarregar dados é fora de escopo).

**SmokeRun**: estados `Inactive`, `Measuring`, `Done`.
- `Inactive -> Inactive [sem --smoke-seconds=] (_Ready)` — terminal.
- `Inactive -> Done(BAD_ARGS) [argumento inválido] (_Ready)`
- `Inactive -> Done(DATA_ERROR) [argumento válido && host.LoadError != null] (_Ready)`
- `Inactive -> Measuring [argumento válido && host.IsRunning] (_Ready)`
- `Measuring -> Done(PASS|FAIL) [t >= N·10⁶ µs] (_Process)`
- `Done` é terminal: imprime a linha uma vez e chama `Quit`.

## 6. Frontend

Não há rotas, stores, cliente HTTP nem i18n. A "interface" é a cena Godot.

### 6.1 Views e Rotas

| "Rota" | Cena | Arquivo | Descrição | Acesso |
|---|---|---|---|---|
| cena inicial (`run/main_scene`) | `Main` | `game/scenes/Main.tscn` | grade, câmera, HUD | local, sem autenticação |

Navegação: não há menu. `Main` abre direto com `$GODOT --path game`.

### 6.2 Componentes

| Componente | Arquivo | Propriedades (`[Export]`) | Lê / Escuta | Descrição |
|---|---|---|---|---|
| `SimHost : Node` | `scripts/SimHost.cs` | — | `OS.GetCmdlineUserArgs()` | §5.3.6, RN-P4-20..23 |
| `IsoGrid : Node2D` | `scripts/IsoGrid.cs` | `GridWidth = 32`, `GridHeight = 32`, `TileWidth = 64`, `TileHeight = 32` (int) | nada | RN-P4-50/51 |
| `CameraRig : Camera2D` | `scripts/CameraRig.cs` | `PanSpeed = 800f`, `ZoomStep = 1.1f`, `MinZoom = 0.25f`, `MaxZoom = 4f` | `Input` | RN-P4-40..42 |
| `HudTick : Label` | `scripts/HudTick.cs` | — | `GetNode<SimHost>("%SimHost")` | RN-P4-30..32 |
| `SmokeRun : Node` | `scripts/SmokeRun.cs` | — | `%SimHost`, `%CameraRig`, `%HudTick`, args | §5.3.9, RN-P4-60..67 |
| `DemoSetup` (static) | `scripts/DemoSetup.cs` | — | — | §5.3.7 |
| `FixedConvert` (static) | `scripts/FixedConvert.cs` | — | — | §5.3.8 |
| `ResDataReader` (static) | `scripts/ResDataReader.cs` | — | `Godot.FileAccess` em `res://data` | §5.3.11, RN-P4-27, ADR-009 |

Todas as classes Godot são `public partial class` no namespace `Rts.Game` (exigência dos geradores de código do Godot). Um tipo por arquivo; nome do arquivo = nome da classe.

Wireframe (janela padrão do Godot, 1152 × 648):

```
┌──────────────────────────────────────────────┐
│ Tick: 1234                                    │
│ Taxa: 20.0 ticks/s           ◇◇◇◇◇            │
│ Hash: 1a2b3c4d            ◇◇◇◇◇◇◇◇◇           │
│ Recursos: 5 · Techs: 4   ◇◇◇◇◇◇◇◇◇◇◇          │
│                            ◇◇◇◇◇◇◇◇◇           │
│                              ◇◇◇◇◇            │
└──────────────────────────────────────────────┘
```

### 6.3 UI States

| Estado | Comportamento |
|---|---|
| Carregando | não há estado visível: `_Ready` é síncrono (carga medida em 42 ms na Fase 3) |
| Rodando | HUD com 4 linhas (5.5); "Taxa: --" até fechar a 1ª janela de 1 s |
| Erro de dados | HUD mostra `Erro ao carregar dados:` + mensagem; grade e câmera continuam funcionando; tick parado |
| Vazio / sucesso / validação de formulário | não se aplicam |

### 6.4 – 6.7

Stores, cliente HTTP, modelos de domínio do frontend e i18n: não se aplicam. O "modelo" lido pela apresentação é o `GameState` público da Sim.

## 7. Integracoes

| Módulo | Tipo | Direção | Ponto | Payload |
|---|---|---|---|---|
| `Rts.Sim.Core` | Código | Consome | `Simulation`, `Step`, `ComputeHash`, `SimEvents` | — |
| `Rts.Sim.Control` | Código | Consome | `ControllerSet`, `HumanController`, `AiController` | — |
| `Rts.Sim.Commands` | Código | Consome | `CommandRegistry` (vazio), `CommandQueue` | — |
| `Rts.Sim.Data` | Código | Consome | `DataLoader.Parse(RawData)` (padrão, ADR-009), `DataLoader.Load(string)` (só `--data-dir`), `DataFiles`, `DataLimits`, `DataSet`, `DataValidationException` | texto dos JSON ou caminho `string` |
| Godot export (Linux) | Ferramenta | Consome | `export_presets.cfg` + templates 4.7.2 mono | `Rts.x86_64`, `Rts.pck` |
| `game/data` | Arquivo | Lê | `resources.json`, `techs.json` | — |
| E2 (seleção e comandos) | Contrato | Emite | `SimHost.LocalHuman.Enqueue(Command)`; registro de comandos antes de criar a `Simulation` | `Command` |
| E1-2/E1-4 | Contrato | Emite | `IsoGrid` e `CameraRig` são substituídos/estendidos | — |
| E3 (movimento) | Contrato | Emite | `SimHost.InterpolationAlpha` para interpolar posições | `float` |

`Stepped` não tem consumidor com efeito colateral além do `SmokeRun`; idempotência não se aplica.

## 8. Seguranca

### 8.1 Data Isolation
Não se aplica (`multiTenant = false`).

### 8.2 Authorization
Não há RBAC nem rede. O controle equivalente é o de fronteira: a apresentação não acessa membros `internal` da Sim (RN-P4-24, UT-BE-162) e só mudaria estado por `Command` validado no `Step` contra o controlador do país (RF-03). Nesta fase ela não envia comandos.

### 8.3 Data Sensitivity
Nenhum campo sensível nesta feature. `--data-dir` aceita qualquer caminho local, lido com o limite de 1 MiB por arquivo da Fase 3; o jogo roda localmente com as permissões do usuário.

## 9. Performance

### 9.1 Índices
Não se aplica. Buscas por país usam a busca binária existente.

### 9.2 Paginação
Não se aplica.

### 9.3 Padrões de risco

| Risco | Mitigação |
|---|---|
| Espiral de morte num quadro lento (ex.: travada de 1 s) | teto de 5 ticks/quadro e descarte (RN-P4-04); `DroppedMicros` no smoke |
| Alocação por tick no host | `SimLoop` reusa um buffer (RN-P4-13, UT-BE-161); `TickClock` sem alocação (UT-BE-156); `SimHost._Process` sem LINQ/strings (RN-P4-23) |
| HUD gerando string todo quadro (~145 fps no headless, X-08) | só reatribui quando o tick muda ou a janela fecha (RN-P4-30) |
| `ComputeHash` por quadro | uma vez por mudança de tick (RN-P4-32); a Fase 2 mediu o hash sem alocação |
| Deriva de arredondamento `delta → µs` | ≤ 0,5 µs por quadro (≈ 72 µs/s a 145 fps = 0,0015 tick/s), irrelevante para o CA-11 |

Orçamentos (informativos, registrar no `history.md` na implementação): taxa medida pelo smoke de 60 s (meta: `rate_milli` entre 19 000 e 21 000; X-08 obteve 1200 ticks em 60,007 s); tempo de `dotnet build Rts.sln` incremental.

## 10. Observabilidade

### 10.1 Domain Events
Nenhum evento de domínio novo. `SimHost.Stepped(long stateTick)` é um evento C# de apresentação.

### 10.2 Logging

| Operação | Nível | O que logar |
|---|---|---|
| Falha de dados | ERROR (`GD.PushError`) | `ex.Message` completo (lista de `DataError` da Fase 3) |
| Resultado do smoke | INFO (`GD.Print`) | a linha `SMOKE ...` (5.5), uma vez |
| Execução normal | — | nada por quadro ou por tick |

A Sim continua sem log (proposta §11).

### 10.3 Auditoria
Nenhuma ação auditável nesta fase.

## 11. Testes

Ordem obrigatória (CLAUDE.md §8): os testes de 11.1 primeiro, sobre esqueletos que lançam `NotImplementedException`, confirmando que falham; depois a implementação até passarem; depois a apresentação Godot e os E2E de 11.4.

IDs: `UT-BE-150..164` e `E2E-BE-20` continuam a numeração das Fases 1–3 (última usada: UT-BE-143, E2E-BE-11). Cada teste tem o comentário `// <ID>` na primeira linha do corpo, como nas fases anteriores. Atalhos: `Refs.S0()`, `Refs.NewSim`, `Refs.AddStock`, `Refs.Human1`, `Refs.Ai1` de `src/Sim.Tests/Fixtures/Refs.cs`.

### 11.1 Unit Tests — Sim (`src/Sim.Tests/Hosting`, `src/Sim.Tests/Architecture`)

```
UT-BE-150: TickClockTests.TickClock_InitialState
  - Dado: new TickClock()
  - Assert: TickClock.TickMicros == 50_000; TickClock.MaxTicksPerAdvance == 5
  - Assert: AccumulatedMicros == 0; DroppedMicros == 0; TotalTicks == 0; Alpha == Fixed.Zero

UT-BE-151: TickClockTests.Advance_AccumulatesAndCarries
  - Assert: Advance(49_999) == 0; AccumulatedMicros == 49_999
  - Assert: Advance(1) == 1; AccumulatedMicros == 0
  - Assert: Advance(125_000) == 2; AccumulatedMicros == 25_000; Alpha.Raw == 2_147_483_648
  - Assert: Advance(0) == 0; AccumulatedMicros == 25_000
  - Assert: TotalTicks == 3; DroppedMicros == 0

UT-BE-152: TickClockTests.Advance_CapsAtFiveAndDropsExcess
  - Dado: clock novo
  - Assert: Advance(260_000) == 5; AccumulatedMicros == 10_000; DroppedMicros == 0   (due == 5: sem descarte)
  - Assert: Advance(300_000) == 5; DroppedMicros == 60_000; AccumulatedMicros == 0  (total 310_000, due 6)
  - Assert: Advance(10_000_000) == 5; DroppedMicros == 60_000 + 9_750_000; AccumulatedMicros == 0
  - Assert: TotalTicks == 15
  - Assert: INV-TC-04: 15 * 50_000 + 0 + 9_810_000 == 260_000 + 300_000 + 10_000_000

UT-BE-153: TickClockTests.Advance_RejectsInvalidElapsed
  - Dado: clock com Advance(30_000) feito
  - Quando: Advance(-1); Advance(long.MinValue); Advance(long.MaxValue - 50_000 + 1)
  - Assert: cada chamada lança ArgumentOutOfRangeException com ParamName == "elapsedMicros"
  - Assert: depois das três, AccumulatedMicros == 30_000, DroppedMicros == 0, TotalTicks == 0
  - Assert: Advance(long.MaxValue - 50_000) não lança em clock novo e devolve 5

UT-BE-154: TickClockTests.SixtySecondsOfFrames_Yield1200Ticks
  - Caso A: 3600 × Advance(16_667) → soma dos retornos == 1200; AccumulatedMicros == 1_200; DroppedMicros == 0
  - Caso B: 1500 × (Advance(7_000), Advance(33_000)) → soma == 1200; AccumulatedMicros == 0; DroppedMicros == 0
  - Caso C (travada de 1 s): 1800 × Advance(16_667), Advance(1_000_000), 1800 × Advance(16_667)
      → soma == 1205; DroppedMicros == 750_600; AccumulatedMicros == 600
  - Assert: em todos os casos, nenhuma chamada devolveu valor fora de [0, 5]

UT-BE-155: TickClockTests.Alpha_IsFractionOfNextTick
  - Assert: após Advance(12_500): Alpha.Raw == 1_073_741_824 (0,25)
  - Assert: após mais Advance(37_499): Alpha.Raw == Fixed.FromRatio(49_999, 50_000).Raw e Alpha < Fixed.One
  - Assert: após mais Advance(1): Alpha == Fixed.Zero

UT-BE-156: TickClockTests.Advance_DoesNotAllocate
  - Dado: clock; 100 chamadas de aquecimento Advance(16_667) lendo Alpha
  - Quando: 10_000 × (Advance(16_667); acc += Alpha.Raw) entre GC.GetAllocatedBytesForCurrentThread()
  - Assert: diferença == 0; acc != 0

UT-BE-157: SimLoopTests.Ctor_RejectsNull
  - Dado: sim = Refs.NewSim(Refs.S0()); cs = new ControllerSet(sim.State); q = new CommandQueue()
  - Assert: new SimLoop(null!, cs, q) lança ArgumentNullException ParamName "simulation"
  - Assert: new SimLoop(sim, null!, q) → "controllers"; new SimLoop(sim, cs, null!) → "queue"
  - Assert: loop válido tem LastEvents == null e Simulation/Controllers/Queue == os objetos passados

UT-BE-158: SimLoopTests.Tick_QueueCommandsComeBeforeControllerCommands
  - Dado: sim = Refs.NewSim(Refs.S0()); cs com HumanController(PlayerId 1) no país 1 e AiController(PlayerId 1) nos países 2 e 3
  - Dado: q.Enqueue(0, Refs.AddStock(2, Refs.Human1, 0, Fixed.One))     // país 2 é Ai ⇒ NotController
  - Dado: human.Enqueue(Refs.AddStock(1, Refs.Ai1, 0, Fixed.One))       // país 1 é Human ⇒ NotController
  - Quando: ev = loop.Tick()
  - Assert: ev.Rejections.Count == 2; ev.AppliedCount == 0
  - Assert: ev.Rejections[0].CommandIndex == 0 e Country == CountryId(2) e Reason == NotController
  - Assert: ev.Rejections[1].CommandIndex == 1 e Country == CountryId(1) e Reason == NotController
  - Assert: human.PendingCount == 0; q.Count == 0

UT-BE-159: SimLoopTests.Tick_AdvancesStateAndReturnsEvents
  - Dado: loop sobre Refs.NewSim(Refs.S0()) com controladores como em UT-BE-158, sem comandos
  - Quando: e1 = loop.Tick(); e2 = loop.Tick()
  - Assert: ReferenceEquals(e1, e2); ReferenceEquals(loop.LastEvents, e2)
  - Assert: e2.CompletedTick == 1; sim.State.Tick == 2; q.LastTakenTick == 1
  - Assert: e2.AppliedCount == 0; e2.Rejections.Count == 0

UT-BE-160: SimLoopTests.Tick_HonorsScheduledTick
  - Dado: q.Enqueue(2, Refs.AddStock(1, Refs.Human1, 3, Fixed.FromInt(7)))
  - Quando: Tick() três vezes, guardando AppliedCount após cada um
  - Assert: AppliedCount == 0, 0, 1 nessa ordem
  - Assert: sim.State.FindCountry(new CountryId(1))!.GetStock(new ResourceId(3)) == Fixed.FromInt(7)

UT-BE-161: SimLoopTests.Tick_DoesNotAllocate
  - Dado: loop como em UT-BE-159; 100 Tick() de aquecimento
  - Quando: 1000 Tick() entre GC.GetAllocatedBytesForCurrentThread()
  - Assert: diferença == 0; sim.State.Tick == 1100

UT-BE-162: PresentationBoundaryTests.SimInternals_AreVisibleOnlyToTests
  - Quando: attrs = typeof(Simulation).Assembly.GetCustomAttributes<InternalsVisibleToAttribute>()
  - Assert: attrs.Select(a => a.AssemblyName) é exatamente ["Rts.Sim.Tests"]   (LINQ permitido em teste)

UT-BE-163: PresentationBoundaryTests.GameScripts_RespectPresentationBoundary
  - Dado: root = ForbiddenApiScanner.FindRepoRoot(); keys = ContentIdScanner.ContentKeys(DataLoader.Load(root/game/data))
  - Assert: ContentIdScanner.Scan(root, "game/scripts", keys) vazio (mensagem: violações unidas por "\n")
  - Assert: ForbiddenApiScanner.ListSourceFiles(root, "game/scripts") contém "game/scripts/SimHost.cs",
            "game/scripts/CameraRig.cs", "game/scripts/IsoGrid.cs", "game/scripts/HudTick.cs" (com '/' como separador, como o scanner devolve)
  - Assert: o texto sem comentários (StripComments) de CameraRig.cs e de IsoGrid.cs não contém "Rts.Sim" nem "SimHost"
  - Assert (ADR-009): nenhum arquivo de game/scripts, sem comentários, contém "GlobalizePath", "\"resources.json\"" ou "\"techs.json\"";
            ListSourceFiles contém "game/scripts/ResDataReader.cs", e o texto dele contém "DataFiles.Resources", "DataFiles.Techs" e "DataLimits.MaxFileBytes"

UT-BE-164: PresentationBoundaryTests.GameProject_IsWiredAsSpecified
  - Dado: csproj = File.ReadAllText(root/game/Rts.Game.csproj); sln = File.ReadAllText(root/Rts.sln); gameSln = File.ReadAllText(root/game/Rts.Game.sln)
  - Assert: csproj contém Sdk="Godot.NET.Sdk/4.7.2", <ProjectReference Include="../src/Sim/Rts.Sim.csproj" />,
            <Nullable>enable</Nullable>, <TreatWarningsAsErrors>true</TreatWarningsAsErrors>, <TargetFramework>net8.0</TargetFramework>
  - Assert: sln contém "game\Rts.Game.csproj"; gameSln contém "Rts.Game.csproj" (DD-P4-16)
  - Assert: não existem root/Directory.Build.props, root/Directory.Packages.props, root/game/Directory.Build.props (DD-P1-01)
  - Assert: Rts.Sim.csproj não contém "Godot" (complementa UT-BE-01)

UT-BE-165: PresentationBoundaryTests.ExportPreset_IsLinuxWithDataFilter
  - Dado: cfg = File.ReadAllLines(root/game/export_presets.cfg); gi = File.ReadAllLines(root/.gitignore)
  - Assert: cfg contém as linhas exatas name="Linux", platform="Linux", include_filter="data/*.json",
            export_path="../build/linux/Rts.x86_64", binary_format/embed_pck=false, binary_format/architecture="x86_64"
  - Assert: há exatamente uma linha que começa com "[preset." e termina com "]" sem ".options" (um único preset)
  - Assert: gi contém a linha "build/"
```

UT-BE-112 (existente, APIs proibidas) passa a varrer `src/Sim/Hosting/*.cs` sem mudança de código; deve continuar verde.

### 11.2 Unit Tests — Frontend

Não há infraestrutura de teste unitário para `game/` (os tipos do Godot só funcionam dentro do motor; ver 3.2 item 9). A lacuna é coberta por: (1) lógica movida para `Rts.Sim.Hosting` (11.1); (2) testes de fronteira UT-BE-162..165; (3) smoke headless no editor (E2E-GD-02..04) e no build exportado (E2E-GD-05); (4) MAN-01.

### 11.3 Unit Tests — outros serviços

Não se aplica.

### 11.4 E2E Tests

**Sim headless (`src/Sim.Tests/Hosting/SimLoopTests.cs`)**

```
E2E-BE-20: SimLoopTests.DemoSetupEquivalent_ReferenceHashes
  - Setup: data = DataLoader.Load(root/game/data); state = GameState.Create(SetupFactory.OneSystemThreeCountries(42), data);
           sim = new Simulation(state, Array.Empty<ISimSystem>(), new CommandRegistry());
           controladores ligados como na RN-P4-21 (Human jogador 1 no país 1; Ai nos 2 e 3); loop = new SimLoop(sim, cs, new CommandQueue())
  - Assert: sim.ComputeHash() == 210089492672693724UL (== Refs.S0Hash)
  - Step 1: loop.Tick() → sim.ComputeHash() == 5242866997788935815UL
  - Step 2: até State.Tick == 100 → == 8913928992633490408UL (== SmokeRun.ExpectedHashAtCheckTick)
  - Step 3: até State.Tick == 1200 → == 14084532596178668976UL
  - Assert: nenhuma rejeição em nenhum tick
```

Se o E2E-BE-20 e o smoke concordarem, o jogo roda a mesma Sim, com o mesmo setup, que os testes.

**Godot headless** (fora do `dotnet test`, que precisa continuar sem Godot; comandos rodados com `zsh -ic '...'` a partir da raiz do repo)

`tools/smoke/godot_smoke.sh` (conteúdo exato):

```bash
#!/usr/bin/env bash
# Smoke headless do jogo (AI-SPEC Foundation Fase 4, E2E-GD-02/03).
# Uso: GODOT=~/bin/godot4 tools/smoke/godot_smoke.sh [segundos=60] [argumentos extras do jogo...]
set -euo pipefail
secs="${1:-60}"
if [ "$#" -gt 0 ]; then shift; fi
: "${GODOT:?defina GODOT com o binário do Godot 4.7.2 mono}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
dotnet build "$root/Rts.sln" -nologo -clp:ErrorsOnly
set +e
out="$(timeout "$((secs + 60))" "$GODOT" --headless --path "$root/game" -- --smoke-seconds="$secs" "$@" 2>&1)"
code=$?
set -e
line="$(printf '%s\n' "$out" | grep '^SMOKE ' | tail -n 1 || true)"
if [ -z "$line" ]; then
  printf '%s\n' "$out" | tail -n 20
  echo "SMOKE result=NO_OUTPUT"
  [ "$code" -eq 0 ] && code=1
else
  echo "$line"
fi
exit "$code"
```

```
E2E-GD-01: build pelo Godot (CA-FINAL-04)
  - Comando: rm -rf game/.godot && $GODOT --headless --path game --build-solutions --quit
  - Assert: código de saída 0
  - Assert: existem game/.godot/mono/temp/bin/Debug/Rts.Game.dll e Rts.Sim.dll (X-04: o código 0 sozinho não basta)
  - Efeito esperado: gera game/scripts/*.cs.uid (X-06), que devem ser versionados
  - Controle negativo (manual, uma vez): acrescentar um erro de compilação num script → código 1 (X-05); desfazer

E2E-GD-02: smoke de 60 s (CA-11, CA-12, CA-13)
  - Comando: tools/smoke/godot_smoke.sh 60
  - Assert: código de saída 0
  - Assert: a linha começa com "SMOKE result=PASS seconds=60 " e contém "hash100=8913928992633490408",
            "resources=5", "techs=4", "camera_moved=1", "zoom_in=1", "zoom_out=1", "hud_ok=1", "source=res", "exported=0"
  - Assert: 19000 <= rate_milli <= 21000
  - Registrar no history.md: window_us, ticks, rate_milli, dropped_us
  - Variante rápida para uso frequente: tools/smoke/godot_smoke.sh 10 (mesmos asserts com seconds=10)

E2E-GD-03: erro de dados
  - Comando: d="$(mktemp -d)"; tools/smoke/godot_smoke.sh 10 --data-dir="$d"
  - Assert: código de saída 2
  - Assert: a linha começa com "SMOKE result=DATA_ERROR message="
  - Controle (ADR-009): tools/smoke/godot_smoke.sh 10 --data-dir="$PWD/game/data" → PASS com "source=dir resources=5 techs=4"

E2E-GD-04: argumento inválido
  - Comando: $GODOT --headless --path game -- --smoke-seconds=5   (após dotnet build Rts.sln)
  - Assert: código 3; saída contém "SMOKE result=BAD_ARGS arg=--smoke-seconds=5"

E2E-GD-05: build exportado lê os dados do .pck e roda o smoke (RN-P4-71; pré-requisito: templates, §12.4)
  - Comando: tools/smoke/export_smoke.sh 10
  - Assert: código 0
  - Assert: existem build/linux/Rts.x86_64 e build/linux/Rts.pck; não existe build/linux/data/
  - Assert: a linha começa com "SMOKE result=PASS seconds=10 " e contém "resources=5", "techs=4", "hash100=8913928992633490408",
            "hud_ok=1", "source=res", "exported=1"
  - Registrar no history.md: a linha SMOKE e o tamanho de build/linux (du -sh)
  - Controle negativo: sem templates (estado atual do host), o script imprime "SMOKE result=NO_TEMPLATES path=..." e sai com 4,
    sem executar o Godot — este controle é verificável já, antes da autorização
```

`tools/smoke/export_smoke.sh` (conteúdo exato):

```bash
#!/usr/bin/env bash
# Exporta o jogo (Linux x86_64) e roda o smoke no executável exportado (AI-SPEC Foundation Fase 4, E2E-GD-05; ADR-009).
# Uso: GODOT=~/bin/godot4 tools/smoke/export_smoke.sh [segundos=10]
set -euo pipefail
secs="${1:-10}"
: "${GODOT:?defina GODOT com o binário do Godot 4.7.2 mono}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
tpl="$HOME/.local/share/godot/export_templates/4.7.2.stable.mono"
for f in linux_debug.x86_64 linux_release.x86_64; do
  if [ ! -f "$tpl/$f" ]; then
    echo "SMOKE result=NO_TEMPLATES path=$tpl/$f"
    exit 4
  fi
done
out_dir="$root/build/linux"
rm -rf "$out_dir"
mkdir -p "$out_dir"
dotnet build "$root/Rts.sln" -nologo -clp:ErrorsOnly
if ! timeout 900 "$GODOT" --headless --path "$root/game" --export-release "Linux" "$out_dir/Rts.x86_64"; then
  echo "SMOKE result=NO_EXPORT"
  exit 5
fi
if [ ! -f "$out_dir/Rts.x86_64" ] || [ ! -f "$out_dir/Rts.pck" ]; then
  echo "SMOKE result=NO_EXPORT"
  exit 5
fi
run_dir="$(mktemp -d)"
cp -a "$out_dir/." "$run_dir/"
if [ -e "$run_dir/data" ]; then
  echo "SMOKE result=FAIL reason=data_dir"
  exit 1
fi
set +e
out="$(cd "$run_dir" && timeout "$((secs + 60))" ./Rts.x86_64 --headless -- --smoke-seconds="$secs" 2>&1)"
code=$?
set -e
line="$(printf '%s\n' "$out" | grep '^SMOKE ' | tail -n 1 || true)"
if [ -z "$line" ]; then
  printf '%s\n' "$out" | tail -n 20
  echo "SMOKE result=NO_OUTPUT"
  [ "$code" -eq 0 ] && code=1
  exit "$code"
fi
echo "$line"
if [ "$code" -ne 0 ]; then exit "$code"; fi
case "$line" in *" source=res"*) ;; *) echo "SMOKE result=FAIL reason=source"; exit 1 ;; esac
case "$line" in *" exported=1"*) ;; *) echo "SMOKE result=FAIL reason=exported"; exit 1 ;; esac
exit 0
```

A cópia para `mktemp -d` e o `cd` garantem que nem o diretório de trabalho nem a pasta do executável têm `data/`: o único lugar de onde os JSON podem vir é o `Rts.pck`.

MAN-01: inspeção visual (não automatizável: o headless não renderiza)
  - Comando: $GODOT --path game
  - Verificar: grade de losangos verdes 2:1 (32 × 32) centrada; HUD no canto superior esquerdo com as 4 linhas;
    "Tick" subindo; "Taxa" entre 19.0 e 21.0 após 1 s; W/A/S/D movem a câmera; a roda muda o zoom;
    o tick continua subindo enquanto a câmera se move
  - Registrar o resultado (OK/falha) no history.md
```

### 11.5 / 11.6

Não há outros serviços nem E2E de frontend web.

## 12. Migracao / Seed

### 12.1 Migration
Não se aplica.

### 12.2 Seed Data
Nenhum dado novo. `game/data` não muda. O setup inicial do jogo é código (`DemoSetup`, 5.3.7), com os valores exatos da fixture `OneSystemThreeCountries(42)`.

### 12.3 Environment Variables

| Variável | Descrição | Default | Obrigatória? |
|---|---|---|---|
| `GODOT` | Binário do Godot 4.7.2 mono (já definido no perfil do zsh: `/home/oscar/bin/godot4`) | — | SIM para E2E-GD-*; NÃO para `dotnet build/test/format` |

### 12.4 Pré-requisito: export templates (precisa de autorização do usuário antes do `/implement`)

**Estado no host (verificado em 2026-11-23, nada baixado):** `~/.local/share/godot/export_templates/` contém só `editor_settings-4.7.tres`, `feature_profiles/`, `script_templates/`, `text_editor_themes/` e `editor_doc_cache-4.7.res`. Não há `4.7.2.stable.mono/`. O Godot procura exatamente `~/.local/share/godot/export_templates/4.7.2.stable.mono/linux_debug.x86_64` e `.../linux_release.x86_64` (X-15).

**Versão exata necessária:** templates **mono** (.NET) de `4.7.2.stable` (os templates padrão, sem .NET, não servem para um projeto C#). A pasta tem de se chamar `4.7.2.stable.mono`, igual ao `version.txt` dentro do arquivo.

**É um download grande de fonte externa** (o arquivo `.tpz` traz templates de todas as plataformas; o tamanho não foi consultado). Por isso **o `/implement` não baixa nada sozinho**: antes de E2E-GD-05, o usuário autoriza explicitamente e escolhe uma das formas:

1. Pelo editor (recomendado): `$GODOT --path game` (com janela) → Editor → Manage Export Templates → Download and Install (baixa do espelho oficial).
2. Manual, na linha de comando (o nome do arquivo segue o padrão das versões anteriores; confirmar na página de lançamento `https://github.com/godotengine/godot/releases/tag/4.7.2-stable` antes de baixar):
   ```bash
   d="$(mktemp -d)"
   curl -L -o "$d/tpl.tpz" https://github.com/godotengine/godot/releases/download/4.7.2-stable/Godot_v4.7.2-stable_mono_export_templates.tpz
   # conferir o SHA-512 publicado na página de lançamento (arquivo SHA512-SUMS.txt) antes de extrair
   mkdir -p ~/.local/share/godot/export_templates/4.7.2.stable.mono
   unzip -j "$d/tpl.tpz" 'templates/*' -d ~/.local/share/godot/export_templates/4.7.2.stable.mono
   cat ~/.local/share/godot/export_templates/4.7.2.stable.mono/version.txt   # deve imprimir 4.7.2.stable.mono
   ls ~/.local/share/godot/export_templates/4.7.2.stable.mono/linux_{debug,release}.x86_64
   ```

**Se o usuário não autorizar:** todo o resto da fase é implementado e verificado; E2E-GD-05 roda só o controle negativo (`NO_TEMPLATES`, código 4); CA-F4-15 fica **pendente**, e a fase não é considerada pronta (a exportação foi colocada no escopo pelo usuário).

## 13. Dependencias de Pacotes

| Pacote | Versão | Layer | Justificativa |
|---|---|---|---|
| `Godot.NET.Sdk` (MSBuild SDK; traz `GodotSharp` e `Godot.SourceGenerators`) | 4.7.2 (exata, igual ao binário) | `game/Rts.Game.csproj` | Obrigatório para qualquer projeto Godot .NET (CLAUDE.md §3: Godot 4 edição .NET). Sem ele não há `Node`, cenas em C# nem build pelo editor |

**Precisa de aprovação do operador** (CLAUDE.md §8: "não instale pacotes NuGet sem justificar e perguntar"). Observação: o experimento X-01 já restaurou `godot.net.sdk/4.7.2`, `godotsharp/4.7.2` e `godot.sourcegenerators/4.7.2` de nuget.org para o cache `~/.nuget/packages` (fora do repositório). Sem rede, a fonte alternativa é a pasta local que vem com o Godot: `~/bin/godot/Godot_v4.7.2-stable_mono_linux_x86_64/GodotSharp/Tools/nupkgs`. `Rts.Sim` e `Rts.Sim.Tests` continuam com os mesmos pacotes.

## 14. Fases de Implementacao

Esta spec é a **Fase 4** das quatro da proposta (§16):

| Fase | Escopo | Dependências | Entregável | Spec |
|---|---|---|---|---|
| Fase 1: Base | E0-1 (.NET), E0-2, E0-3, E0-4 | — | `Fixed`, `SimRandom`, `GameState` | `AI-SPEC_Foundation_Phase_1.md` (implementada) |
| Fase 2: Ciclo | E0-5, E0-6, E0-7 | Fase 1 | `Step`, comandos, controladores, hash | `AI-SPEC_Foundation_Phase_2.md` (implementada) |
| Fase 3: Dados | E0-8 | Fase 1 | carregador/validador, `game/data` | `AI-SPEC_Foundation_Phase_3.md` (implementada) |
| **Fase 4: Apresentação** | E0-1 (Godot), E0-9, exportação Linux | Fases 2 e 3; templates 4.7.2 mono (§12.4) | jogo roda a Sim a 20 ticks/s, no editor e exportado | **este documento** |

Ordem de trabalho:
0. **Antes de começar, perguntar ao usuário** se autoriza baixar e instalar os export templates 4.7.2 mono (§12.4) e por qual forma. Não baixar nada sem essa resposta. A resposta só bloqueia o passo 8.
1. Registrar START no `docs/history.md`.
2. Esqueletos de `TickClock` e `SimLoop` com `throw new NotImplementedException()`; criar `game/Rts.Game.csproj` (5.3.3) e incluí-lo na solução: `dotnet sln Rts.sln add --solution-folder game game/Rts.Game.csproj`.
3. Escrever UT-BE-150..165 e E2E-BE-20 (devem falhar: os de `Hosting` por `NotImplementedException`; UT-BE-163..165 enquanto `game/scripts`, `export_presets.cfg` e a linha `build/` não existirem). Registrar quantos falham.
4. Implementar `TickClock` (UT-BE-150..156) e `SimLoop` (UT-BE-157..161, E2E-BE-20).
5. Criar `project.godot`, `export_presets.cfg`, `Main.tscn` e os scripts (5.3.4–5.3.11, 6.2); acrescentar `build/` ao `.gitignore`. UT-BE-162..165 passam.
6. `zsh -ic 'dotnet build Rts.sln'`, `zsh -ic 'dotnet test src/Sim.Tests'`, `zsh -ic 'dotnet format Rts.sln --verify-no-changes'`.
7. E2E-GD-01 (gera os `.cs.uid`), E2E-GD-02 (60 s), E2E-GD-03 (com o controle `source=dir`), E2E-GD-04, controle negativo do E2E-GD-05 (`NO_TEMPLATES`, se ainda sem templates), MAN-01.
8. Com os templates instalados (autorização do passo 0): E2E-GD-05 completo.
9. Registrar END no `history.md` com o tempo real, as linhas `SMOKE` do E2E-GD-02 e do E2E-GD-05, o tamanho de `build/linux` e o resultado do MAN-01. Se o E2E-GD-05 não puder rodar (sem autorização), registrar a pendência com `/followup` e marcar CA-F4-15 como pendente.

### Decisões de design desta fase e divergências da proposta (para aprovação)

Cada item diverge da proposta (`Proposal_Foundation.md`) ou a completa onde ela é omissa. Revisão 2: o usuário aprovou DD-P4-01..15 e mudou só o DD-P4-13 (a exportação entra nesta fase). O DD-P4-13 agora diverge do CLAUDE.md §5.1 e da proposta §5.2, e por isso tem um ADR novo (ADR-009, Proposed). Nenhum outro item contradiz ADR.

- **DD-P4-01 · `Rts.Game` entra em `Rts.sln`** (proposta omissa). Verificado (X-03): `dotnet build Rts.sln` compila Sim, testes e jogo com 0 avisos sem abrir o Godot; `dotnet test src/Sim.Tests` não é afetado; `dotnet format Rts.sln` passa a cobrir `game/scripts` (CLAUDE.md §6); e o `harness.project.json` já usa `dotnet build Rts.sln` como teste do frontend. Custo: a primeira restauração precisa de nuget.org ou da pasta `nupkgs` do Godot (§13). Alternativa rejeitada: deixar o jogo fora da solução, o que só detectaria erro de compilação do jogo pelo Godot e deixaria `game/scripts` sem `dotnet format`.
- **DD-P4-02 · Flags próprias do `Rts.Game.csproj`.** Confirma o DD-P1-01: o projeto não herda `src/Directory.Build.props`. Ele declara `Nullable`, `TreatWarningsAsErrors`, `ImplicitUsings=disable` e `LangVersion 12.0` (verificado sem avisos com os geradores do Godot, X-02). Não usa `Directory.Packages.props` (o SDK do Godot gerencia as próprias versões).
- **DD-P4-03 · `TickClock` e `SimLoop` em `src/Sim/Hosting` (divergência).** A proposta (§5.2) põe o acúmulo de 50 ms e o limite de catch-up dentro do `SimHost` em `game/`. A spec move essa lógica para duas classes puras da Sim, em pasta nova, porque assim a RN-25 e os 20 ticks/s são testáveis no xUnit, sem Godot (UT-BE-150..161). Elas não leem relógio (o host informa os µs), usam só `long` e passam pela varredura de APIs proibidas. Também servem a um host futuro (servidor, replay). O `SimHost` fica como adaptador fino.
- **DD-P4-04 · Interpolação = fator exposto, sem entidade interpolada.** A proposta tira a interpolação de entidades do E0. A spec entrega o fator (`TickClock.Alpha`, `SimHost.InterpolationAlpha`), testado (UT-BE-155), para o E3 usar.
- **DD-P4-05 · `DemoSetup` em `game/scripts`, espelhando a fixture de teste** (proposta omissa sobre o setup do jogo). Os valores são iguais aos de `OneSystemThreeCountries(42)`, o que permite comparar o hash do jogo com os valores de referência (E2E-BE-20 × E2E-GD-02). Os nomes ("Alpha", "Beta", "Gamma") são placeholders; mapas e fluxo de partida (E1, E14) substituem o setup. O setup não fica na Sim para não colocar dados de partida no núcleo.
- **DD-P4-06 · Nenhum comando enviado nesta fase.** A proposta diz que a apresentação "envia comandos", mas o E0 não tem comando de jogo (DD-07). O `CommandRegistry` do jogo fica vazio, o `HumanController` é ligado e exposto (`LocalHuman`) para o E2, e a câmera não é comando, porque não é estado de jogo.
- **DD-P4-07 · Fonte de tempo: `delta` do `_Process`, convertido para µs** (proposta: "acumula tempo real"). O `delta` respeita `Engine.TimeScale` e pausa, que podem ser usados depois. A medição de 60 s (X-08) deu a taxa exata. Rejeitado: o passo de física do Godot como relógio (5.1).
- **DD-P4-08 · Regra exata do descarte** (a RN-25 da proposta só diz "o excesso é descartado"). Com mais de 5 ticks devidos, executa 5 e descarta todo o resto, inclusive a fração, contando em `DroppedMicros` (RN-P4-04). Com exatamente 5 devidos, nada é descartado.
- **DD-P4-09 · Limite fixo de zoom [0,25; 4] (divergência pequena).** O DD-06 deixa "zoom mín/máx" para o E1-4. Sem nenhum limite, a roda leva o zoom a valores degenerados, então a spec põe limites fixos de segurança e passo de 1,1. O E1-4 os torna configuráveis e acrescenta bordas, setas, arrasto e limites do mapa. Velocidade de pan: 800 px/s na tela.
- **DD-P4-10 · Verificação automática do CA-11/12/13** (a proposta só previa abrir o jogo e olhar). Modo `--smoke-seconds=N` + `tools/smoke/godot_smoke.sh`, com critérios inteiros (RN-P4-64) e checagem do hash no tick 100. A inspeção visual continua (MAN-01), porque o headless não renderiza. Pasta nova `tools/smoke/`, ao lado das previstas no CLAUDE.md §4.
- **DD-P4-11 · HUD com taxa medida e textos em português.** A proposta lista tick, hash curto e contagens. A spec acrescenta a taxa medida em janelas de 1 s, para conferir o CA-11 a olho, e fixa o hash curto como os 32 bits altos (8 hex). Os textos ficam literais em português (`i18n = false`).
- **DD-P4-12 · Erro de dados no HUD e `--data-dir`** (proposta: "a apresentação mostra a mensagem"). O jogo não fecha: mostra a mensagem completa e não inicia a Sim. `--data-dir` existe só para permitir testar esse caminho de erro (E2E-GD-03) sem mexer em `game/data`.
- **DD-P4-13 · Exportação Linux no escopo; dados lidos de `res://` como texto (ADR-009).** Decisão do usuário na aprovação: a exportação entra nesta fase. `export_presets.cfg` com o preset `Linux` x86_64 e `include_filter="data/*.json"` (CLAUDE.md §7). Num build exportado, `ProjectSettings.GlobalizePath("res://data")` não aponta para uma pasta real (X-17), então o jogo lê os JSON com `FileAccess` (`ResDataReader`) e chama `DataLoader.Parse(RawData)`, que já existe (DD-P3-02). Isso vale também no editor, para haver um só caminho. Opções comparadas no ADR-009: (A) copiar `data/` para ao lado do executável e usar `Load(caminho)`; (B) ler de `res://` e passar texto, a escolhida; (C) híbrido por `OS.HasFeature("editor")`. A foi rejeitada por exigir um passo de pós-exportação frágil e contradizer o §7; C, por manter dois caminhos.
  - **Divergência explícita do CLAUDE.md §5.1** ("a Sim recebe o caminho da pasta de dados como string; o Godot passa `ProjectSettings.GlobalizePath("res://data")`") e da proposta §5.2 (tabela do `SimHost`). No jogo, a Sim passa a receber o texto dos arquivos. `DataLoader.Load(string)` continua existindo para testes, ferramentas e `--data-dir`. A Sim continua sem Godot e sem `res://` (ADR-001 intacto). O ADR-009 propõe o novo texto do §5.1; o CLAUDE.md não foi alterado e fica para o usuário aceitar o ADR.
  - **Verificação:** E2E-GD-05 exporta, copia a saída para uma pasta sem `data/` e exige `PASS`, `source=res` e `exported=1`. Depende dos export templates, que não estão no host e precisam de autorização do usuário para o download (§12.4).
  - **Riscos não verificados** (sem templates): se os JSON entram intactos no `.pck` e se o template de release aceita `--headless`. O E2E-GD-05 é o teste de ambos.
- **DD-P4-14 · Versionar os `*.cs.uid`.** O Godot 4.4+ gera um `.uid` por script na varredura do editor (X-06). Eles entram no repositório. A cena referencia scripts por caminho; `uid=` nos `ext_resource` é aceito se o editor acrescentar.
- **DD-P4-16 · `game/Rts.Game.sln` versionado (revisão 3, decisão do usuário).** O E2E-GD-05 revelou que o `ExportPlugin` do GodotTools falha com `res://scripts/SmokeRun.cs is a C# file but no solution file exists` se não houver `game/<assembly>.sln`; a revisão 2 proibia esse arquivo por engano. Criado com `dotnet new sln -n Rts.Game` + `dotnet sln add Rts.Game.csproj`, contém só o jogo e não é ignorado pelo git. `Rts.sln` da raiz continua sendo a solução de `dotnet build/test/format` (o `game/Rts.Game.sln` não é descoberto por eles; sem build duplicado). Experimento: com o arquivo, a exportação e o smoke exportado dão PASS (`source=res exported=1`).
- **DD-P4-15 · Fronteira da apresentação verificada por teste** (a RN-26 da proposta previa "revisão e busca"). UT-BE-162 (só os testes veem `internal`), UT-BE-163 (sem id de conteúdo em `game/scripts`; câmera e grade não referenciam a Sim; sem `GlobalizePath`), UT-BE-164 (ligação dos projetos e DD-P1-01) e UT-BE-165 (preset de exportação e `build/` ignorado).

## 15. Criterios de Aceitacao

### 15.1 Fase 4

- `CA-F4-01`: `zsh -ic 'dotnet build Rts.sln'` compila `Rts.Sim`, `Rts.Sim.Tests` e `Rts.Game` com 0 erros e 0 avisos; `Rts.Sim` continua sem `PackageReference` e sem referência a Godot. (CA-01 da proposta)
- `CA-F4-02`: `zsh -ic 'dotnet test src/Sim.Tests'` passa os 179 testes das Fases 1–3 e os desta fase (UT-BE-150..165, E2E-BE-20), sem Godot no processo. UT-BE-112 continua verde com `src/Sim/Hosting`. (CA-02)
- `CA-F4-03`: `zsh -ic 'dotnet format Rts.sln --verify-no-changes'` termina com código 0, inclusive para `game/scripts`.
- `CA-F4-04`: E2E-GD-01: `$GODOT --headless --path game --build-solutions --quit` sai com 0 e produz `Rts.Game.dll` e `Rts.Sim.dll` em `game/.godot/mono/temp/bin/Debug/`. (CA-FINAL-04, E0-1)
- `CA-F4-05`: E2E-GD-02 com 60 s: `SMOKE result=PASS`, `19000 <= rate_milli <= 21000`. (CA-11: tick a 20/s ± 1 tick/s em 60 s)
- `CA-F4-06`: E2E-GD-02: `resources=5 techs=4 hud_ok=1 source=res`, isto é, os dados vieram de `res://data` (ADR-009) e isto é, o HUD mostra o tick atual, o hash curto correto e "Recursos: 5 · Techs: 4". (CA-12)
- `CA-F4-07`: E2E-GD-02: `camera_moved=1 zoom_in=1 zoom_out=1` e `hash100=8913928992633490408`, isto é, WASD e a roda mudam a câmera e a Sim fica idêntica à referência. UT-BE-163: `CameraRig` não referencia a Sim. (CA-13)
- `CA-F4-08`: E2E-BE-20: a Sim com o setup equivalente ao `DemoSetup` dá os hashes de referência nos ticks 0, 1, 100 e 1200.
- `CA-F4-09`: UT-BE-152/154: no máximo 5 ticks por avanço, excesso descartado e contado; 60 s de quadros irregulares dão exatamente 1200 ticks. (RN-25)
- `CA-F4-10`: UT-BE-156/161: `TickClock.Advance` e `SimLoop.Tick` sem comandos não alocam. (CA-15 estendido ao host)
- `CA-F4-11`: E2E-GD-03: dados ausentes dão `SMOKE result=DATA_ERROR` e código 2, e `--data-dir=game/data` dá `PASS` com `source=dir`; E2E-GD-04: argumento inválido dá código 3.
- `CA-F4-12`: UT-BE-162/163: `Rts.Sim` só expõe `internal` para `Rts.Sim.Tests`; nenhum id de conteúdo, nome de arquivo de dados literal nem `GlobalizePath` em `game/scripts`; `grep -rn "\.Raw" game/scripts` só acha `FixedConvert.cs`. (RN-26, P-06, ADR-005, ADR-009)
- `CA-F4-13`: MAN-01 registrado como OK no `history.md`: janela com a grade, HUD com as 4 linhas e câmera respondendo. (UC-PLY-01, UC-PLY-02)
- `CA-F4-14`: `docs/history.md` tem START/END da implementação, com o tempo real gasto e as linhas `SMOKE` do E2E-GD-02 e do E2E-GD-05. Os `*.cs.uid` gerados estão no repositório; nada de `build/` está. (Definição de pronto, gate G-3)
- `CA-F4-15`: E2E-GD-05: `tools/smoke/export_smoke.sh 10` sai com 0. O preset `Linux` gera `build/linux/Rts.x86_64` + `Rts.pck` sem pasta `data/`, e o executável, rodando numa cópia isolada, imprime `SMOKE result=PASS` com `resources=5 techs=4 hash100=8913928992633490408 hud_ok=1 source=res exported=1`. Exige os templates (§12.4); sem autorização, fica **pendente** e a fase não está pronta. (CLAUDE.md §7, ADR-009)
- `CA-F4-16`: UT-BE-165 verde (preset Linux com `include_filter="data/*.json"`, `embed_pck=false`, `x86_64`; `build/` no `.gitignore`). Sem templates, `export_smoke.sh` dá `NO_TEMPLATES` e código 4 sem chamar o Godot nem baixar nada. (RN-P4-70, 72, 73)
- `CA-F4-17`: `docs/architecture/adr/ADR-009_dados-do-jogo-lidos-de-res-como-texto.md` existe com `Status: Proposed`, compara as opções A/B/C e registra a divergência do CLAUDE.md §5.1. A implementação segue a opção B.

### 15.2 Final (feature Foundation, após as 4 fases)

- `CA-FINAL-01`: os CA-01 a CA-15 da proposta estão verdes, cada um verificado na fase correspondente (CA-11, CA-12 e CA-13 nesta fase; CA-01 e CA-02 revalidados com o jogo na solução).
- `CA-FINAL-02`: i18n não se aplica (`capabilities.i18n = false`).
- `CA-FINAL-03`: `dotnet test src/Sim.Tests` passa, incluindo determinismo (Fase 2), dados (Fase 3) e hospedagem (esta fase).
- `CA-FINAL-04`: `dotnet build Rts.sln` e `$GODOT --headless --path game --build-solutions --quit` compilam sem erros; o HUD mostra as contagens de `game/data` (5 recursos, 4 techs).
- `CA-FINAL-05`: critério de saída do Marco 0 (CLAUDE.md §9): `dotnet test` verde, incluindo determinismo; o jogo abre e mostra o mapa isométrico rodando a Sim a 20 ticks/s (E2E-GD-02 + MAN-01), também como build Linux exportado (E2E-GD-05).

## 16. Cross-Layer Mapping

Não há banco, DTO HTTP nem i18n. Mapeamento equivalente: requisito → Sim → apresentação → verificação.

| Requisito | Sim (`src/Sim`) | Apresentação (`game/`) | Regra | Verificação |
|---|---|---|---|---|
| RF-01 / ADR-003 (20 ticks/s) | `TickClock.TickMicros`, `Advance` | `SimHost._Process`, `DeltaToMicros` | RN-P4-01..05, 22 | UT-BE-150..154, E2E-GD-02 |
| RN-25 da proposta (máx. 5/quadro) | `TickClock.MaxTicksPerAdvance`, `DroppedMicros` | `SmokeRun` (`dropped_us`) | RN-P4-04 | UT-BE-152, 154 |
| ADR-003 (interpolação) | `TickClock.Alpha` (`Fixed`) | `SimHost.InterpolationAlpha` (`float`, via `FixedConvert`) | RN-P4-06, 25 | UT-BE-155 |
| Fase 2 §5.8.4 (laço de host) | `SimLoop.Tick` | `SimHost` | RN-P4-10..13 | UT-BE-157..161 |
| ADR-009 (dados por texto; diverge do CLAUDE.md §5.1) | `DataLoader.Parse(RawData)`, `DataFiles`, `DataLimits` | `ResDataReader.Read("res://data")` | RN-P4-20, 27 | E2E-GD-02 (`source=res resources=5`), UT-BE-163 |
| ADR-001 (caminho como string, mantido para diagnóstico) | `DataLoader.Load(string)` | `--data-dir=X` | RN-P4-20 | E2E-GD-03 (`source=dir`) |
| CLAUDE.md §7 (exportação com `data/*.json`) | — | `export_presets.cfg`, `tools/smoke/export_smoke.sh` | RN-P4-70..73 | UT-BE-165, E2E-GD-05 |
| RNF-05 (erro claro) | `DataValidationException.Message` | `SimHost.LoadError` → HUD | RN-P4-21, 5.6 | E2E-GD-03 |
| RNF-06 / RN-26 (sem regra na apresentação) | setters `internal`, `InternalsVisibleTo` | nenhum acesso a `internal` | RN-P4-24 | UT-BE-162 |
| P-06 (`Fixed → float` só em `game/`) | `Fixed.Raw` | `FixedConvert.ToFloat` | RN-P4-25 | CA-F4-12 (grep) |
| ADR-005 (sem id de conteúdo) | registries | `HudTick` usa só `Count` | RN-P4-26 | UT-BE-163 |
| CA-12 (HUD) | `State.Tick`, `ComputeHash`, `Resources.Count`, `Techs.Count` | `HudTick.Text` | RN-P4-30..32 | E2E-GD-02 (`hud_ok`), MAN-01 |
| CA-13 / DD-06 (câmera mínima) | — | `CameraRig`, ações `camera_*` | RN-P4-40..42 | E2E-GD-02, UT-BE-163, MAN-01 |
| E0-9 (grade isométrica) | — | `IsoGrid` | RN-P4-50, 51 | MAN-01 |
| RF-03 (só o controlador emite) | `ControllerSet`, `HumanController` | `SimHost.LocalHuman` | RN-P4-21 | UT-BE-158 |
| Setup do M0 | `GameState.Create(GameSetup, DataSet)` | `DemoSetup` | 5.3.7 | E2E-BE-20 × E2E-GD-02 (`hash100`) |
| E0-1 (projeto Godot) | `Rts.Sim.csproj` | `Rts.Game.csproj`, `project.godot`, `Rts.sln` | DD-P4-01, 02 | UT-BE-164, E2E-GD-01 |

---

## Checklist de qualidade (autoverificação)

- [x] Invariantes como predicados (`INV-TC-01..04`, `INV-SL-01..02`)
- [x] Toda regra com id, severidade e expressão (`RN-P4-*`); a única `[WARN]` é RN-P4-04 (descarte de tempo)
- [x] Máquinas de estados de `SimHost` e `SmokeRun` com guardas (5.8.3)
- [x] Auditoria: "nenhuma"
- [x] Sem termos vagos: tempos em µs, limites numéricos, textos e linhas de saída exatos
- [x] Rastreio: E0-1/E0-9 → CA-11..13 da proposta → RN-P4 → UT-BE/E2E-BE/E2E-GD/MAN → arquivo
- [x] Estrutura de arquivos exata; só `Rts.sln` e `.gitignore` modificados (mais `game/Rts.Game.sln` novo, DD-P4-16)
- [x] Pacote novo (`Godot.NET.Sdk 4.7.2`) justificado e marcado para aprovação (§13)
- [x] Comportamento do ambiente verificado por experimento, não suposto (§4, X-01..X-17); o que depende dos templates está marcado como não verificado e é o objeto do E2E-GD-05
- [x] Divergências registradas (DD-P4-01..15): DD-P4-03 e 09 divergem da proposta; DD-P4-13 (revisão 2: exportação no escopo) diverge da proposta §5.2 e do CLAUDE.md §5.1, com ADR-009 (Proposed); as demais completam a proposta
- [x] Export templates: estado do host, versão exata e procedimento documentados (§12.4); download marcado como passo que o usuário autoriza antes do `/implement`; nada baixado
- [x] Módulo-alvo: sem descritores de módulo; feature em `docs/features/Foundation/`
