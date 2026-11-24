# Runbook: abrir o jogo (Marco 0)

> Escopo: rodar o jogo do Marco 0 (grade isométrica, tick a 20/s, HUD) e fazer a inspeção visual MAN-01 (FUP-foundation-01).
> Ambiente testado: Ubuntu 26.04 x86_64, .NET SDK 8.0.428, Godot 4.7.2 mono.
> O jogo só tem câmera (WASD e zoom) e HUD. Não há seleção, unidades nem comandos ainda (E1 em diante).

## 1. Pré-requisitos

| Item | Como conferir | Esperado |
|---|---|---|
| .NET 8 SDK | `dotnet --version` | começa com `8.` |
| Godot 4.7.2 .NET | `$GODOT --version` | `4.7.2.stable.mono.official...` |
| Variáveis | `echo $GODOT $DOTNET_ROOT` | `~/bin/godot4` e `~/.dotnet` |

As variáveis vêm do `~/.zshrc`. Em um terminal já aberto, rode `source ~/.zshrc`.

Se `dotnet` ou `$GODOT` não forem encontrados, veja a seção 6.

## 2. Abrir o jogo

Na raiz do repositório:

```bash
cd ~/workspace/oaks/vilhight
dotnet build Rts.sln          # compila Sim, testes e Rts.Game (0 avisos esperados)
$GODOT --path game            # abre a janela do jogo
```

Se preferir o editor do Godot (útil para ver a cena e depurar):

```bash
$GODOT --path game --editor   # abre o editor; F5 roda o jogo
```

Na primeira abertura o Godot importa o projeto e compila o C#. Pode demorar alguns segundos.

## 3. Inspeção visual (MAN-01, fecha CA-11 e CA-13)

Com o jogo aberto, confira cada item:

| # | O que conferir | Esperado | OK? |
|---|---|---|---|
| 1 | Grade isométrica | 32x32, centrada na janela, losangos 2:1 | |
| 2 | HUD (canto da tela) | 4 linhas: `Tick`, `Taxa`, `Hash`, `Recursos: 5 · Techs: 4` | |
| 3 | Contador de tick | sobe continuamente | |
| 4 | `Taxa` | entre 19,0 e 21,0 ticks/s | |
| 5 | `W` `A` `S` `D` | a câmera se desloca nas 4 direções | |
| 6 | Roda do mouse | para cima aproxima, para baixo afasta; limite 0,25x a 4x | |
| 7 | Tick durante o movimento | continua subindo com a câmera em movimento | |
| 8 | Hash | não muda por causa da câmera (só muda quando o tick avança) | |

Feche a janela quando terminar.

## 4. Registrar o resultado

1. Adicione uma entrada no topo de `docs/history.md` (use o editor, nunca sobrescreva o arquivo):

   ```
   [AAAA-MM-DD HH:MM END]   Verificação: MAN-01 Foundation — grade, HUD, taxa e câmera conferidos (OK | falha: <o que>).
   ```
2. Se tudo estiver OK, peça para marcar o item da câmera no `CLAUDE.md` §9 e fechar o `FUP-foundation-01` com `/followup resolve FUP-foundation-01`.
3. Se algo falhou, anote o item da tabela e o comportamento observado; vira correção na Foundation.

## 5. Verificações sem janela (opcionais)

```bash
dotnet test src/Sim.Tests                       # 196 testes
dotnet format Rts.sln --verify-no-changes       # sem alterações pendentes
bash tools/smoke/godot_smoke.sh 60              # 60 s headless; espera "SMOKE result=PASS ... ticks=1200"
bash tools/smoke/export_smoke.sh 10             # exporta e roda o build exportado; espera "PASS ... exported=1"
```

O `export_smoke.sh` precisa dos export templates em `~/.local/share/godot/export_templates/4.7.2.stable.mono/`. Sem eles, termina com código 4. A saída vai para `build/linux/` (ignorada pelo git).

## 6. Problemas comuns

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| `dotnet: command not found` | PATH sem `~/.dotnet` | `source ~/.zshrc`; confira as linhas `DOTNET_ROOT` e `PATH` |
| `$GODOT` vazio | variável não carregada | `source ~/.zshrc` ou `export GODOT=$HOME/bin/godot4` |
| Janela abre mas o HUD mostra `Erro ao carregar dados:` | JSON de `game/data` inválido ou ausente | leia a mensagem do HUD; os erros vêm do validador (`DataValidationException`) |
| Erro de compilação C# ao abrir | projeto Godot desatualizado | `dotnet build Rts.sln`; depois `$GODOT --headless --path game --build-solutions --quit` |
| Godot reclama de "no solution file" | falta `game/Rts.Game.sln` | o arquivo é versionado; restaure-o do git |
| `Taxa` fora de 19-21 | máquina sobrecarregada ou VSync/FPS baixo | feche outros programas; a Sim usa o `delta` e descarta ticks acima de 5 por quadro |
| `export_smoke.sh` sai com código 4 | sem export templates | instalar templates mono 4.7.2.stable (godotengine/godot-builds, conferir SHA-512) |

## 7. Referências

- AI-SPEC da Fase 4: `docs/features/Foundation/AI-SPEC_Foundation_Phase_4.md` (MAN-01, E2E-GD-01..05).
- ADR-009: dados lidos de `res://data` como texto.
- Follow-up: `FUP-foundation-01` em `docs/features/Foundation/Follow_Up_Foundation.md`.
