# ADR-009: O jogo lê os dados de `res://data` e entrega o texto à Sim (`DataLoader.Parse`)

**Status:** Accepted
**Data:** 2026-11-23

## Contexto

A Sim carrega o conteúdo com `DataLoader.Load(string dataDir)`, que lê os arquivos do disco com `System.IO.File` (ADR-001; CLAUDE.md §5.1: "a Sim recebe o caminho da pasta de dados como string; o Godot passa `ProjectSettings.GlobalizePath("res://data")`"). Isso funciona ao rodar pelo editor (`$GODOT --path game`), onde `res://` é a pasta `game/`.

Num build exportado, não funciona. A documentação do Godot 4.7.2 (`GodotSharp.xml`, `ProjectSettings.GlobalizePath`) diz: "`GlobalizePath` com `res://` não funciona num projeto exportado". Os arquivos ficam dentro do `.pck`, e só o `FileAccess` do Godot os enxerga. Ao mesmo tempo, o CLAUDE.md §7 manda incluir `data/*.json` no filtro de exportação, isto é, levar os dados **dentro** do `.pck`. As duas regras do CLAUDE.md não são compatíveis no build exportado. A AI-SPEC Foundation Fase 4 (DD-P4-13) põe a exportação Linux no escopo do M0 e precisa resolver isso.

A Fase 3 já oferece uma segunda entrada sem disco: `DataLoader.Parse(RawData)`, com `RawData(string? ResourcesJson, string? TechsJson)` (DD-P3-02). Ela aplica as mesmas validações, inclusive o limite de 1 MiB (calculado sobre o texto) e `FileMissing` para `null`.

## Opções

| Critério | A: copiar `data/` para ao lado do executável e usar `Load(caminho)` | B: ler `res://data` com `FileAccess` e chamar `Parse(RawData)` | C: `Load` no editor e `Parse` no exportado |
|---|---|---|---|
| Regra do CLAUDE.md §5.1 (caminho) | mantém ao pé da letra | **diverge** no jogo (a Sim recebe texto); `Load(caminho)` continua para testes e `--data-dir` | mantém no editor, diverge no exportado |
| Regra do CLAUDE.md §7 (dados no `.pck`) | contradiz: o filtro vira inútil, e os dados ficam soltos | **cumpre** | cumpre |
| Passo extra na exportação | sim: plugin `EditorExportPlugin` ou script de pós-exportação; esquecê-lo gera um build quebrado | nenhum | nenhum |
| Caminhos de código no jogo | 2 (editor: `GlobalizePath`; exportado: pasta do executável) | 1 (igual no editor e no exportado) | 2 |
| Distribuição | executável + `.pck` + pasta `data/` | executável + `.pck` | executável + `.pck` |
| Edição de dados pelo usuário (mods) | fácil (arquivos soltos) | exige `--data-dir` ou recompilar o `.pck` | igual a B |
| Validação e determinismo | iguais (o mesmo `DataCompiler`) | iguais | iguais |
| Testabilidade headless | `Load` já testado (Fase 3) | `Parse` já testado (Fase 3); a leitura do `.pck` só se testa exportando | os dois |

## Decisão

**Opção B.** Em `game/`, um leitor (`ResDataReader`) abre `res://data/<DataFiles.Resources>` e `res://data/<DataFiles.Techs>` com `Godot.FileAccess`, decodifica UTF-8 (remove o BOM, como o `File.ReadAllText` da Fase 3) e monta `RawData`, com `null` para arquivo ausente. O `SimHost` chama `DataLoader.Parse(raw)`. O mesmo caminho vale para o editor e para o build exportado. Os nomes dos arquivos vêm das constantes públicas `Rts.Sim.Data.DataFiles`; o leitor não conhece nenhum id de conteúdo.

`DataLoader.Load(string dataDir)` continua existindo e sendo a entrada dos testes, de ferramentas (`tools/DataValidator`, futuro servidor) e do argumento de diagnóstico `--data-dir=CAMINHO` do jogo.

**Divergência explícita do CLAUDE.md §5.1.** A frase "a Sim recebe o caminho da pasta de dados como string; o Godot passa `ProjectSettings.GlobalizePath("res://data")`" deixa de descrever o jogo. Ao aceitar este ADR, ela deve virar: "A Sim não lê arquivos do Godot. Ela aceita o caminho de uma pasta (`DataLoader.Load`, usado por testes e ferramentas) ou o texto dos arquivos (`DataLoader.Parse(RawData)`). O jogo lê `res://data` com `FileAccess` e passa o texto (ADR-009)." A parte essencial do ADR-001 continua valendo: a Sim não referencia Godot e não conhece `res://`.

## Consequências

- Positivo: um só caminho de carga no jogo, igual no editor e no exportado; cumpre o filtro `data/*.json` do CLAUDE.md §7; nenhum passo de pós-exportação.
- Positivo: nenhuma mudança na Sim (`Parse` e `RawData` já existem e estão testados).
- Negativo: a leitura do `.pck` só é verificada exportando (E2E-GD-05 da AI-SPEC Fase 4), o que exige os export templates.
- Negativo: o limite de 1 MiB passa a ser medido sobre o texto (bytes UTF-8), não sobre o tamanho do arquivo. Para não ler um arquivo enorme, o leitor lê no máximo `DataLimits.MaxFileBytes + 1` bytes, e o `Parse` então acusa `FileTooLarge`.
- Negativo: editar dados num build exportado exige `--data-dir`. Suporte a mods fica para quando for pedido.
- A mensagem de erro de dados mostra o arquivo pelo nome (`resources.json`), não pelo caminho `res://`.

## Alternativas consideradas

- A (copiar `data/` para fora do `.pck`): rejeitada pelo passo extra frágil e pela contradição com o CLAUDE.md §7.
- C (híbrido por `OS.HasFeature("editor")`): rejeitada. São dois caminhos para o mesmo efeito, e o do build exportado só seria exercitado exportando.
- Ler pelo `ResourceLoader` como recurso `JSON` do Godot: rejeitada. Converteria o conteúdo para tipos do Godot antes da Sim (mais conversão, perda do texto exato que o validador estrito da Fase 3 exige).
