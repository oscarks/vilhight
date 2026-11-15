# CLAUDE.md — RTS Isométrico (Godot 4 + C#)

Este arquivo orienta o Claude Code neste repositório. Leia-o inteiro no início de cada sessão.

## 1. O projeto

RTS isométrico no estilo Warcraft/StarCraft, com:

- árvore de tecnologia livre (estilo Civilization) e recursos que surgem e mudam com o conhecimento;
- um planeta com até 8 países e até 3 espécies; o jogador alterna o controle entre países, e os demais seguem automáticos pela **doutrina** de cada um;
- incursões (até 30 combatentes, controle em tempo real) e batalhas (acima disso, resolvidas por parâmetros + aleatoriedade, só assistidas);
- fases: planeta natal (1 jogador) → sistema estelar (propulsão nuclear, colônias) → galáxia (dobra, multiplayer em universo persistente).

**Fonte da verdade do design:** `docs/design.md` (exportação em Markdown do Documento de Design).
Antes de implementar qualquer sistema, leia a seção correspondente. Se o código e o design divergirem, pare e pergunte.

## 2. Idioma

- Conversa, documentação e mensagens de commit: **português**.
- Código, identificadores, nomes de arquivos e chaves JSON: **inglês**.

## 3. Stack

- Godot 4.x, edição **.NET** (C#), com .NET 8.
- Linux como ambiente de desenvolvimento.
- Testes: xUnit.
- Dados: JSON em `game/data/`.
- Arte provisória: placeholders gerados por código; pipeline de sprites por scripts Blender (`tools/blender/`).

## 4. Estrutura do repositório

```
/
├─ CLAUDE.md
├─ Rts.sln
├─ docs/
│  ├─ design.md            # documento de design (fonte da verdade)
│  ├─ specs/               # uma spec por sistema, escrita antes do código
│  ├─ adr/                 # decisões de arquitetura (0001-titulo.md)
│  └─ progress.md          # log curto do que foi feito por sessão
├─ src/
│  ├─ Sim/                 # Rts.Sim.csproj — simulação em C# puro, SEM referência ao Godot
│  └─ Sim.Tests/           # Rts.Sim.Tests.csproj — xUnit, roda headless
├─ game/                   # projeto Godot (project.godot, Rts.Game.csproj → referencia Sim)
│  ├─ data/                # JSON: resources, recipes, techs, buildings, units, species, agendas...
│  ├─ scenes/
│  ├─ scripts/             # apresentação: lê o estado da Sim e desenha; nenhuma regra de jogo
│  └─ assets/
└─ tools/
   ├─ blender/             # scripts bpy: modelos low-poly → sprites isométricos em 8 direções
   └─ DataValidator/       # CLI que valida game/data/ (opcional, pode reusar a Sim)
```

## 5. Regras de arquitetura (obrigatórias)

### 5.1 Separação Sim / apresentação
- `src/Sim` **não referencia Godot**. Nada de `Node`, `Vector2`, `GD.Print`, `res://`.
- A apresentação (`game/scripts`) só: envia comandos, lê o estado e reage a eventos. Nenhuma regra de jogo fica nela.
- A Sim recebe o caminho da pasta de dados como string; o Godot passa `ProjectSettings.GlobalizePath("res://data")`.

### 5.2 Loop e comandos
- Simulação em ticks fixos: **20 ticks/s**. A renderização interpola.
- Toda ação do jogador ou da IA é um **comando serializável**, com `CountryId`, aplicado num tick.
- A Sim rejeita comandos para países que o emissor não controla e para unidades em **batalha**.
- API central (nomes indicativos):
  ```csharp
  public sealed class Simulation {
      public SimEvents Step(IReadOnlyList<Command> commands); // avança 1 tick
      public GameState State { get; }
  }
  ```

### 5.3 Modelo de estado
```
GameState
 ├─ Players[]
 ├─ Universe → StarSystems[] (um dono cada) → Worlds[] (planetas e naves)
 │                                              └─ Countries[] → Controller: Human | AI(Doctrine)
 └─ Ships[]
```
- Tudo que é "de quem joga" pertence ao **país**: estoque, população, árvore, recursos revelados, névoa, diplomacia, agendas.
- Humano e IA implementam a mesma interface `IController` (lê o estado do país, emite comandos).
- O MVP usa 1 sistema com 1 planeta, mas **nenhum código pode assumir isso**.

### 5.4 Determinismo (desde o primeiro commit)
Dentro de `src/Sim` é **proibido**:
- `float`/`double` na lógica → use o tipo de ponto fixo `Fixed` (Q32.32 em `long`);
- `System.Random` → use `SimRandom` com semente explícita;
- `DateTime.Now`, `Stopwatch`, `Guid.NewGuid()`, relógio de parede em geral;
- iterar `Dictionary`/`HashSet` quando a ordem afeta o resultado → use arrays ou coleções ordenadas por id;
- estado estático mutável; threads na lógica da Sim.

Teste obrigatório: mesma semente + mesmos comandos ⇒ mesmo hash de estado.

### 5.5 Orientado a dados
- Recursos, receitas, tecnologias, construções, unidades, espécies, culturas, agendas, marcos e crises vêm de JSON.
- **Nenhum id de conteúdo fixo no código** (nem na IA). Código conhece *tipos de efeito* e *tipos de condição*, não conteúdo.
- Todo carregamento passa por validação: ids únicos, referências existentes, árvore sem ciclos, todo recurso alcançável.
- Efeitos de tecnologia são declarativos: `revealResource`, `unlockBuilding`, `unlockUnit`, `unlockRecipe`, `modifyStat`, `raiseGatherCap`, `replaceRequirement`, `obsolete`. Um handler por tipo.

### 5.6 Desempenho
- Meta: 300–500 unidades a 60 FPS.
- Sem alocação por tick em laços quentes (movimento, combate, pathfinding).
- IA de cada país decide a cada 10 ticks, com deslocamento entre países.
- Mundos fora de vista usam simulação de baixa fidelidade.

## 6. Convenções de código

- `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` em `Sim` e `Sim.Tests`.
- Tipos de id fortes (`readonly record struct CountryId(int Value)`), não `int` solto.
- Classes pequenas, um sistema por pasta em `src/Sim/Systems/<Sistema>/`.
- Sistemas da Sim são funções do estado: `void Tick(GameState s, SimContext ctx)`; sem dependências ocultas.
- Comentários só onde a regra não é óbvia; referencie a seção do design (`// design: Economia > Regras`).
- Rode `dotnet format` antes de cada commit.

## 7. Comandos

Ajuste `GODOT` para o binário local (ex.: `~/bin/Godot_v4.x-stable_mono_linux.x86_64`).

```bash
dotnet build Rts.sln                                  # compila tudo
dotnet test src/Sim.Tests                             # testes da simulação (headless)
dotnet format Rts.sln                                 # formatação
$GODOT --headless --path game --build-solutions --quit   # compila o projeto Godot
$GODOT --path game                                    # roda o jogo
```

Na exportação do Godot, inclua `data/*.json` no filtro de arquivos não-recurso.

## 8. Fluxo de trabalho (SDD)

Para **cada sistema**, nesta ordem, uma etapa por vez:

1. **Ler** a seção do sistema em `docs/design.md`.
2. **Escrever a spec** em `docs/specs/<nn>-<sistema>.md`: objetivo, interfaces públicas, estruturas de dados, formato JSON, casos de teste, critérios de aceite (copiados do design), fora de escopo.
3. **Parar e pedir aprovação** da spec.
4. **Escrever os testes** (devem falhar).
5. **Implementar** até os testes passarem.
6. **Apresentação mínima** no Godot, se o sistema for visível.
7. `dotnet build`, `dotnet test`, `dotnet format` sem erros.
8. Atualizar `docs/progress.md` (3–5 linhas) e fazer commit.

Regras:
- Itens marcados **[Depois]** no design não entram até o marco correspondente.
- Uma decisão de arquitetura nova vira ADR em `docs/adr/`.
- Ambiguidade no design → pergunte; não invente regra de jogo.
- Não instale pacotes NuGet sem justificar e perguntar.

### Definição de pronto
- Testes da spec passando, incluindo determinismo quando o sistema toca a Sim.
- Nenhum `float`, `System.Random` ou id de conteúdo fixo dentro de `src/Sim`.
- Dados novos validados pelo carregador.
- `progress.md` atualizado.

## 9. Roteiro

### Marco 0 — Fundação (começar aqui)
- [ ] Criar `Rts.sln`, `src/Sim`, `src/Sim.Tests`, projeto Godot .NET em `game/` referenciando `Sim`.
- [ ] `.gitignore` para Godot (`.godot/`, `.import/`, `bin/`, `obj/`, `*.user`).
- [ ] `Fixed` (ponto fixo) com operações básicas e testes.
- [ ] `SimRandom` com semente e testes de reprodutibilidade.
- [ ] `GameState` com a hierarquia Universe → StarSystem → World → Country.
- [ ] `Command`, fila de comandos por tick, `Simulation.Step`.
- [ ] `IController`, `HumanController`, `AiController` (vazio por enquanto).
- [ ] Hash de estado + teste de determinismo.
- [ ] Carregador e validador de JSON para `resources.json` e `techs.json` com 3–5 itens de exemplo.
- [ ] Cena Godot com grade isométrica de placeholder, câmera (bordas, WASD, arrasto, zoom) e um tick visível.

**Critério de saída do Marco 0:** `dotnet test` verde, incluindo determinismo; o jogo abre e mostra o mapa isométrico rodando a Sim a 20 ticks/s.

### Marcos seguintes (ordem do design)
1. Mapa e câmera
2. Seleção e comandos
3. Movimento e pathfinding (A* + desvio local)
4. Economia básica (coleta, entrega, população)
5. Construções e produção
6. IA dos países com doutrina e modelos prontos
7. Vários países e troca de controle
8. Árvore de tecnologia e conhecimento
9. Recursos ocultos, receitas e limites de coleta
10. Combate: incursões e batalhas; anexação
11. Névoa de guerra por país
12. Interface completa (seletor de países, tela da árvore, "o que aconteceu")
13. Espécies e culturas
14. Fluxo de partida, metas da fase 1 (marcos, pontuação, agendas) e áudio
15. Editor de doutrina e aprendizado pelo histórico
16. União diplomática e estabilidade; corrida espacial, crises e Legado
17. Viagem entre mundos (ramo espacial, nave, andares, eventos)

## 10. Perguntas ainda em aberto (não decidir sozinho)

- Infraestrutura do servidor do universo persistente.
- Estilo de arte final, identidade das 3 espécies, cenário e tom.
- Catálogos de conteúdo (governos, culturas, agendas, crises, tecnologias reais).
