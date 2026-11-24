# Review: Foundation — Fase 3 (Dados)

**Data**: 2026-11-23
**AI-SPEC**: `docs/features/Foundation/AI-SPEC_Foundation_Phase_3.md`
**Proposal**: `docs/features/Foundation/Proposal_Foundation.md`
**ADRs**: ADR-001, ADR-002, ADR-005
**Escopo revisado**: `src/Sim/Data/*` (16 arquivos), `src/Sim/State/GameState.cs` (sobrecarga), `game/data/*.json`, `src/Sim.Tests/{Data,Architecture,State,Fixtures}`, `docs/history.md`.

## Veredito: APPROVED_WITH_OBSERVATIONS

| Severidade | Quantidade |
|---|---|
| CRITICAL | 0 |
| MAJOR | 0 |
| MINOR | 1 |
| OBSERVATION | 3 |

## 1. Validações executadas

| Check | Resultado |
|---|---|
| `dotnet build Rts.sln` | PASS: 0 avisos, 0 erros |
| `dotnet test src/Sim.Tests` | PASS: 179 aprovados, 0 falhas, 0 ignorados (153 das Fases 1–2 + 26 novos) |
| `dotnet format Rts.sln --verify-no-changes` | PASS (sem diferenças) |
| Varredura float/double/System.Random/DateTime/Stopwatch/Guid/LINQ/Regex/Godot em `src/Sim/Data` | vazia (UT-BE-112 também verde com os novos arquivos) |
| Literais de id de conteúdo em `src/Sim` (grep manual + UT-BE-142) | vazia |
| `.csproj`/`.props` e `PackageReference` | sem mudança; `Rts.Sim` sem pacotes |
| Iteração de `Dictionary` | só em construtores de registry/compiler para preencher mapas de busca; iteração da lógica é sobre arrays por índice |
| Testes ignorados (`Skip`) | nenhum |

## 2. Conformidade com a spec

- **Arquivos**: todos os 16 arquivos de `src/Sim/Data` previstos na §5.2 existem; `GameState.cs` ganhou só a sobrecarga `Create(GameSetup, DataSet)` com `ThrowIfNull(setup)` e depois `data` (RN-P3-62). Fixtures, testes de `Data/`, `State/` e `Architecture/` presentes (`ContentIdScanner` só em testes).
- **Modelos e API**: `ResourceDef`, `TechDef`, `ResourceAmount`, os 8 registros de efeito, `DataError`/`DataErrorCode`, `DataValidationException`, `DataSet`, registries com construtores `internal`, listas `ReadOnlyCollection` (RN-P3-61, UT-BE-140).
- **Seed**: `game/data/resources.json` e `techs.json` idênticos ao conteúdo exato da §12.2 (5 recursos, 4 techs; ids densos na ordem do arquivo).
- **Divergências DD-P3-01..14**: todas aprovadas pelo usuário e refletidas no código (JsonDocument manual, RawData/Parse, DataError único, esquemas provisórios, raw_hidden exige revealedBy, quota, ids densos, UnresearchableTech, supressão de cascata, JSON estrito, varredura ADR-005, placeholders, GameState sem DataSet). Nenhuma divergência adicional não registrada foi encontrada, exceto o desvio do `TechEffect` abaixo.

### Desvio relatado: construtor de `TechEffect` (CS8878)

A spec (§5.3.4, RN-P3-61, UT-BE-140) pedia `private protected TechEffect()` como construtor único, para que nenhum tipo fora do assembly derivasse do record. O compilador sintetiza, em todo record abstrato/não selado, um construtor de cópia `protected TechEffect(TechEffect original)`, e o CS8878 recusa torná-lo `private protected`. O código mantém `private protected TechEffect()` (`src/Sim/Data/TechEffects.cs`), e o UT-BE-140 foi ajustado para: nenhum construtor público e o sem parâmetros é `IsFamilyAndAssembly`, com comentário explicativo no teste.

**Avaliação: aceitável**, com a ressalva m-01. A limitação é da linguagem (records não podem herdar de classe comum, então não há alternativa de mesmo custo), o teste continua verificando a parte verificável da intenção (nenhum construtor público, construtor padrão restrito ao assembly), o desvio foi registrado no `docs/history.md` e o ajuste está comentado no teste. A consequência prática é pequena: um assembly externo poderia, em tese, derivar `TechEffect` via construtor de cópia, mas teria de implementar `Type` e os handlers (E8-2) fazem `switch` por tipo, de modo que um tipo estranho cairia no caso padrão. Registrado como MINOR para o E8-2 tratar o `default` de forma explícita (ver m-01).

## 3. Rastreabilidade das regras de negócio

| Regra | Implementada | Testada | Teste |
|---|---|---|---|
| RN-P3-01 coleta total | SIM (`DataCompiler`) | SIM | UT-BE-133 |
| RN-P3-02 ordem de emissão | SIM | SIM | UT-BE-133, 134 |
| RN-P3-03 arquivo utilizável | SIM | SIM | UT-BE-124 |
| RN-P3-04 supressão de cascata | SIM | SIM | UT-BE-132 |
| RN-P3-05 tamanho 1 MiB | SIM (`DataLoader.Load`, `DataCompiler`) | SIM | UT-BE-136, 137 |
| RN-P3-10 formato de id | SIM | SIM | UT-BE-125 |
| RN-P3-11 unicidade | SIM | SIM | UT-BE-125, 126 |
| RN-P3-12 mapas de chave | SIM (`_resMap`, `_techMap`) | SIM | UT-BE-128 |
| RN-P3-20..28 estrutura | SIM | SIM | UT-BE-125..127 |
| RN-P3-30..33 referências/coerência/pendências | SIM | SIM | UT-BE-122, 128, 129 |
| RN-P3-40 ciclos | SIM | SIM | UT-BE-130 |
| RN-P3-50, 51 alcançabilidade | SIM | SIM | UT-BE-131, 132 |
| RN-P3-60 montagem canônica | SIM | SIM | UT-BE-121 |
| RN-P3-61 imutabilidade | SIM | SIM | UT-BE-140 (ajustado, ver acima) |
| RN-P3-62 `GameState.Create` | SIM (`GameState.cs:98`) | SIM | UT-BE-139, E2E-BE-11 |
| RN-P3-63 sem id de conteúdo na Sim | SIM | SIM | UT-BE-141, 142 |
| RN-P3-64 determinismo | SIM | SIM | UT-BE-143, UT-BE-112 |

Veredito por regra: todas COVERED. Todos os 26 IDs de teste da spec (UT-BE-120..143, E2E-BE-10, E2E-BE-11) têm teste com o comentário `// ID` e passam.

## 4. Critérios de aceitação

| ID | Estado | Evidência |
|---|---|---|
| CA-F3-01 | PASS | build 0/0; nenhum csproj/props alterado; sem PackageReference |
| CA-F3-02 | PASS | 179 testes verdes (153 + 26) |
| CA-F3-03 | PASS | `dotnet format --verify-no-changes` limpo |
| CA-F3-04 | PASS | E2E-BE-10 |
| CA-F3-05 | PASS | UT-BE-125..131 |
| CA-F3-06 | PASS | UT-BE-133 |
| CA-F3-07 | PASS | UT-BE-122 |
| CA-F3-08 | PASS | UT-BE-124, 136, 137 |
| CA-F3-09 | PASS | UT-BE-139, E2E-BE-11 (hash 210089492672693724) |
| CA-F3-10 | PASS | UT-BE-142 verde; verificação manual registrada no history (`Tmp.cs:2: CID ore in "ore"`); não repetida aqui por ser revisão somente leitura, coberta por UT-BE-141 |
| CA-F3-11 | PASS com ressalva | UT-BE-140; ver desvio do `TechEffect` |
| CA-F3-12 | PASS | UT-BE-121, 143 |
| CA-F3-13 | PASS | START/END da implementação no history, com tempo (~7 min) e medição de Load (42 ms, < 50 ms) |

| Tipo | Total | Pass | Fail | Não verificável |
|---|---|---|---|---|
| Por fase (CA-F3) | 13 | 13 | 0 | 0 |
| Finais (CA-FINAL) | 4 | 0 | 0 | 4 (dependem da Fase 4; CA-FINAL-02 N/A, CA-FINAL-03 parcial verde) |

## 5. Definição de Pronto (CLAUDE.md §8)

| Item | Estado |
|---|---|
| Testes da spec passando, incluindo determinismo | PASS (179/179; UT-BE-143, hash inicial inalterado) |
| Sem `float`, `System.Random`, Godot em `src/Sim` | PASS (varredura de `src/Sim/Data` e UT-BE-112) |
| Sem id de conteúdo fixo em `src/Sim` | PASS (UT-BE-142, 12 chaves; grep manual vazio) |
| Dados novos validados pelo carregador | PASS (E2E-BE-10 carrega `game/data` e `Validate` não dá erro) |
| `docs/history.md` atualizado com tempo real | PASS (START 20:06, END 20:13 com tempo e medição) |
| Convenções (§6): `Nullable`, `TreatWarningsAsErrors`, ids fortes, `dotnet format` | PASS |

## 6. Arquitetura, qualidade e segurança

- Separação Sim/apresentação mantida: `Rts.Sim` sem referência a Godot; o caminho de dados entra como `string` (ADR-001).
- `DataCompiler` tem ~1300 linhas em um único arquivo; é coeso (etapas 1–6) e determinístico, mas grande (ver O-02).
- Entrada não confiável tratada: limite de 1 MiB checado antes de ler, `MaxDepth = 16`, JSON estrito, inteiros de 32 bits, propriedades desconhecidas e duplicadas recusadas.
- Sem código morto, TODO ou FIXME. Sem alocação restrita nesta fase (carregamento único fora do tick).
- Camadas inexistentes nesta fase (HTTP, banco, i18n, multi-tenant, RBAC, migrações, frontend): N/A.

## 7. Registro de achados

### CRITICAL
Nenhum.

### MAJOR
Nenhum.

### MINOR

| # | Seção | Descrição | Arquivo | Sugestão |
|---|---|---|---|---|
| m-01 | 2 / 5.3.4 | O fechamento da hierarquia `TechEffect` não é total: o construtor de cópia protegido sintetizado permite, em tese, derivar o record fora do assembly (consequência do CS8878). A spec (RN-P3-61, UT-BE-140) prometia construtor único `private protected`. | `src/Sim/Data/TechEffects.cs:21` | Aceitar o desvio. No E8-2, os handlers devem tratar o `default` do `switch` por tipo como erro (exceção), e a spec da Fase 3 pode ser corrigida (RN-P3-61, §5.3.4) para refletir o construtor de cópia. |

### OBSERVATIONS

| # | Seção | Descrição |
|---|---|---|
| O-01 | 7.4 | CA-F3-10 (verificação manual de injetar literal) não foi repetida nesta revisão, que não altera código; a evidência é o registro do implementador mais UT-141/142 verdes. |
| O-02 | 4.4 | `DataCompiler.cs` (1308 linhas) pode ser dividido por etapa (estrutura, referências, ciclos, alcançabilidade) quando o E5 acrescentar `buildings.json`/`units.json`/`recipes.json`, para manter arquivos pequenos (CLAUDE.md §6). |
| O-03 | 9 | O orçamento de 50 ms foi medido incluindo JIT e asserts (42 ms); é um limite superior conservador, adequado como informação. |

## 8. Destaques positivos

- Coleta total de erros com caminho JSON, código e mensagem exata, e supressão de cascata bem definida; o caminho de erro é testado com sequência exata (UT-BE-133).
- `ContentIdScanner` reutilizável protege o ADR-005 desde já e será reusado pela IA (E6-2).
- Testes escritos primeiro (24/26 falharam sobre esqueletos), conforme a regra do projeto, com o desvio documentado no history e comentado no teste.
- Imutabilidade por construtores `internal` e `ReadOnlyCollection`, com teste de reflexão.
- Hash inicial das Fases 1–2 preservado, o que confirma que `GameState.Create(setup, data)` só delega.

## 9. Estatísticas

| Categoria | Pass | Fail | N/A |
|---|---|---|---|
| Conformidade com a spec | 1 (com desvio aceito) | 0 | 0 |
| Rastreabilidade de regras | 17 | 0 | 0 |
| Critérios de aceitação (Fase) | 13 | 0 | 0 |
| Revisão de código | 5 | 0 | 6 |
| Consistência entre camadas | 0 | 0 | todas (sem outras camadas) |
| Migração e seed | 1 (seed JSON idêntico à spec) | 0 | migração N/A |
| Testes | 1 | 0 | 0 |
| Análise estática e build | 3 | 0 | 0 |
| Documentação (history) | 1 | 0 | 0 |

Follow-ups: nenhum (`followup:none`).
