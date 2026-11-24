# Review: Foundation — Fase 1 (Base)

**Data**: 2026-11-23
**AI-SPEC**: `docs/features/Foundation/AI-SPEC_Foundation_Phase_1.md`
**Proposta**: `docs/features/Foundation/Proposal_Foundation.md`
**Escopo revisado**: `Rts.sln`, `src/Directory.*.props`, `src/Sim/**`, `src/Sim.Tests/**`, `.gitignore`, `docs/history.md`

## Veredito: APPROVED_WITH_OBSERVATIONS

| Severidade | Quantidade |
|---|---|
| CRITICAL | 0 |
| MAJOR | 0 |
| MINOR | 2 |
| OBSERVATION | 3 |

## 1. Conformidade com a spec

- Estrutura de arquivos: todos os arquivos da §5.2 existem e nenhum extra foi criado (as pastas `Commands/`, `Control/`, `Data/` e `Systems/` não foram criadas, como pede a spec).
- `Fixed` (§5.3.1): assinaturas, constantes e algoritmos conferem com RN-P1-01..09. `+`, `-` e `-x` usam `checked`. `*` e `/` usam `Int128` com conversão `checked`. `Sqrt` usa o algoritmo dígito a dígito com `UInt128`. `ToString` usa `UInt128` para cobrir `long.MinValue`.
- `SimRandom` (§5.3.2): SplitMix64 e xoshiro256** conferem com RN-P1-10/11. Os corpos estão em `unchecked` (RN-P1-16). `NextInt` valida o argumento antes de consumir valor (RN-P1-14). `NextFixed` é `NextU64() >> 32` (RN-P1-15).
- Ids, `ControllerKind` (Human=1, Ai=2), `ControllerRef`, `GameSetup` e registros, `GameStateLimits`, `GameSetupError` e `GameSetupException` conferem com a §5.3.3 a 5.3.6.
- `GameState`, `Universe`, `StarSystem`, `World`, `Country`, `Player` (§5.3.7):
  - Construtores `internal`, setters `internal` e `Random` `internal`.
  - Listas são `ReadOnlyCollection` via `Array.AsReadOnly`.
  - Busca binária manual, sem `Dictionary` ou LINQ.
  - `Stock` é `ReadOnlySpan<Fixed>` e `SetStock` é `internal`.
- Ordem de validação (§5.6): conferida linha a linha em `GameState.Validate`. A validação roda inteira antes de qualquer objeto ser construído (RN-P1-30). O `HashSet<int>` é usado só para pertinência.
- Divergências DD-P1-01..04 em relação à proposta: aprovadas pelo usuário e implementadas como descrito (props em `src/`, `Create(GameSetup, int)`, `Random` internal, estoque encapsulado). Não há contradição com os ADR-001, 002, 004, 005 e 006.
- Itens EXTRA: nenhum relevante. MISSING: nenhum. DIVERGENT: nenhum.

## 2. Rastreabilidade das regras de negócio

| Regra | Implementada | Arquivo | Testada | Teste | Situação |
|---|---|---|---|---|---|
| RN-P1-01 | SIM | `Fixed.cs:17` | SIM | `FromInt_IsExact`, `Constants_HaveExpectedRaw` | COVERED |
| RN-P1-02 | SIM | `Fixed.cs:31` | SIM | `ToIntFloor_IsFloor` | COVERED |
| RN-P1-03 | SIM | `Fixed.cs:33-37` | SIM | `AddSubtractNegate` | COVERED |
| RN-P1-04 | SIM | `Fixed.cs:39` | SIM | `Multiply_RoundsDownAndChecksOverflow` | COVERED |
| RN-P1-05 | SIM | `Fixed.cs:42` | SIM | `Divide_TruncatesTowardZeroAndChecks` | COVERED |
| RN-P1-06 | SIM | `Fixed.cs:21` | SIM | `FromRatio_TruncatesTowardZero`, `FromRatio_Errors` | COVERED |
| RN-P1-07 | SIM | `Fixed.cs:61` | SIM | `Sqrt_IsFloorOfRoot` | COVERED |
| RN-P1-08 | SIM | `Fixed.cs:94-110` | SIM | `AbsMinMaxClamp`, `Comparison` | COVERED |
| RN-P1-09 | SIM | `Fixed.cs:113` | SIM | `ToString_IsExactDecimal` | COVERED |
| RN-P1-10 | SIM | `SimRandom.cs:71` | SIM | `Seeding_MatchesSplitMix64` | COVERED |
| RN-P1-11 | SIM | `SimRandom.cs:25` | SIM | `NextU64_MatchesXoshiro256StarStar` | COVERED |
| RN-P1-12 | SIM | `SimRandom.cs` | SIM | `SameSeed_SameSequence` | COVERED |
| RN-P1-13 | SIM | `SimRandom.cs` | SIM | `DifferentSeeds_Diverge` | COVERED |
| RN-P1-14 | SIM | `SimRandom.cs:42` | SIM | `NextInt_VectorsAndGuards`, `_DistributionIsExactForSeed42`, `_LargeBound` | COVERED |
| RN-P1-15 | SIM | `SimRandom.cs:65` | SIM | `NextFixed_InUnitInterval` | COVERED |
| RN-P1-16 | SIM | `SimRandom.cs` (blocos `unchecked`) | revisão de código | n/a | COVERED (por revisão) |
| RN-P1-17 | SIM | `SimRandom.cs:67` | SIM | `Seeding_MatchesSplitMix64` | COVERED |
| RN-P1-18 | SIM | `SimRandom.cs` | SIM | `FixedAndRandom_DoNotAllocate` | COVERED |
| RN-P1-20 | SIM | `GameState.cs:246` | SIM | `Create_CountryLimit` | COVERED |
| RN-P1-21 | SIM | `GameState.cs:188,215,240,266` | SIM | `Create_RejectsDuplicateIds` | COVERED |
| RN-P1-22 | SIM | `GameState.cs:272-285` | SIM | `Create_RejectsInvalidController` | COVERED |
| RN-P1-23 | SIM | `GameState.cs:220` | SIM | `Create_RejectsOtherInvalidSetups` | COVERED |
| RN-P1-24 | SIM | `GameState.cs:110-152` | SIM | `Create_OrdersCanonically` | COVERED |
| RN-P1-25 | SIM | `GameState.cs` | SIM | `Create_TwoSystemsTwoPlayers` | COVERED |
| RN-P1-26 | SIM | `Country.cs:16`, `GameState.cs:21` | SIM | `Create_OneSystemThreeCountries`, `Create_SeedsRandom` | COVERED |
| RN-P1-27 | SIM | `GameState.cs:291` | SIM | `Create_RejectsOtherInvalidSetups`, `Create_AcceptsMaxLengthName_AndRejectsNulls` | COVERED |
| RN-P1-28 | SIM | `GameState.cs:182,210,235,261` | SIM | `Create_RejectsOtherInvalidSetups` | COVERED |
| RN-P1-29 | SIM | `ControllerKind.cs` | SIM | `Create_RejectsInvalidController` | COVERED |
| RN-P1-30 | SIM | `GameState.cs:98-101` | SIM | `Create_ValidationOrderIsDeterministic` | COVERED |
| RN-P1-31 | SIM | acessibilidades dos membros | SIM | `State_IsReadOnlyForPresentation` | COVERED |
| RN-P1-32 | SIM | `Country.cs:25-43` | SIM | `Country_Stock` | COVERED |
| RN-P1-33 | SIM | `GameState.cs:45-95` | SIM | `Lookups_DoNotAllocate` | COVERED |
| RN-P1-40 | SIM | `Rts.Sim.csproj` | SIM | `SimAssembly_DoesNotReferenceGodot` | COVERED |
| RN-P1-41 | SIM | `src/Sim` | SIM (grep CA-F1-08) | n/a | COVERED (textual, como previsto) |
| RN-P1-42 | SIM | `src/Sim` | revisão de código | n/a | COVERED (por revisão) |

## 3. Critérios de aceite

| Critério | Resultado | Evidência |
|---|---|---|
| CA-F1-01 | PASS | `dotnet build Rts.sln`: 0 avisos, 0 erros. `Rts.sln` contém só `Rts.Sim` e `Rts.Sim.Tests`. |
| CA-F1-02 | PASS | `src/Directory.Build.props` confere com a §5.2.1. `Rts.Sim.csproj` não tem `PackageReference` nem `ProjectReference`. |
| CA-F1-03 | PASS | `dotnet test src/Sim.Tests`: 71 aprovados, 0 falhas, 0 ignorados. Todos os UT-BE-01, 10..21, 30..39 e 50..63 têm teste correspondente. |
| CA-F1-04 | PASS | `dotnet format Rts.sln --verify-no-changes` retornou código 0. |
| CA-F1-05 | PASS | O checksum `128333051705741941` é verificado duas vezes em `FixedTests.cs:200-201`. |
| CA-F1-06 | PASS | Vetores de UT-BE-30, 31 e 34..37 estão em `SimRandomTests.cs` e passam. |
| CA-F1-07 | PASS | `Create_OneSystemThreeCountries`, `Create_TwoSystemsTwoPlayers` e `Create_CountryLimit`, `Create_RejectsDuplicateIds`, `Create_RejectsInvalidController` e `Create_ValidationOrderIsDeterministic` passam. |
| CA-F1-08 | PASS | Os dois greps retornaram vazio (reexecutados nesta revisão). |
| CA-F1-09 | PASS | `State_IsReadOnlyForPresentation` passa. Revisão manual: nenhum setter ou construtor público de estado. |
| CA-F1-10 | PASS | `FixedAndRandom_DoNotAllocate` e `Lookups_DoNotAllocate` passam, com delta de 0 byte. |
| CA-F1-11 | PASS | `.gitignore` mantém as 4 linhas do harness e acrescenta `.godot/`, `.import/`, `bin/`, `obj/`, `*.user`, `TestResults/`. `bin/` e `obj/` aparecem como ignorados (`!!`). |
| CA-F1-12 | PASS | `docs/history.md` tem START (09:30) e END (09:34) da implementação, com o tempo gasto. |

| Tipo | Total | Pass | Fail | Não verificável |
|---|---|---|---|---|
| Por fase (CA-F1) | 12 | 12 | 0 | 0 |
| Finais (CA-FINAL) | 4 | n/a | n/a | 4 (dependem das Fases 2 e 4) |

## 4. Revisão de código

- Arquitetura: `src/Sim` não tem referência a Godot. Verificado por `GetReferencedAssemblies` no teste e por grep. Não há PackageReference em `Rts.Sim`.
- Determinismo (CLAUDE.md §5.4): nenhum `float`, `double`, `decimal`, `System.Random`, `DateTime`, `Stopwatch`, `Guid.NewGuid`, `Thread`, `Task.Run` ou campo `static` mutável. Os `static readonly` de `Fixed` são structs imutáveis. Nenhuma coleção sem ordem é iterada: o `HashSet<int>` só testa pertinência.
- Dados (§5.5): não há id de conteúdo no código. Os literais são só mensagens de erro e tipos de validação.
- Convenções (§6): `Nullable` e `TreatWarningsAsErrors` ativos. Ids fortes. Pasta por sistema (`Core/`, `State/`). Comentários referenciam o design.
- Código morto, TODO, `NotImplementedException` ou `Skip`: nenhum encontrado.
- Pacotes (§8): só `Microsoft.NET.Test.Sdk` 17.12.0, `xunit` 2.9.3 e `xunit.runner.visualstudio` 2.8.2, nas versões aprovadas. Não há `coverlet`.
- Itens que não se aplicam: seções de HTTP, banco, frontend, i18n, multi-tenant, RBAC e eventos.

## 5. Registro de achados

### CRITICAL
Nenhum.

### MAJOR
Nenhum.

### MINOR

| # | Seção | Descrição | Arquivo | Sugestão |
|---|---|---|---|---|
| m-01 | 9.3 / 11 | Os orçamentos informativos de desempenho (10^6 multiplicações `Fixed` abaixo de 20 ms, 10^6 `NextU64` abaixo de 10 ms) não foram medidos. A spec pede registrar o valor real em `docs/history.md`. O próprio END da implementação diz "não medidos". | `docs/history.md` | Medir uma vez (script descartável) e anotar o valor no history. Não precisa falhar teste. |
| m-02 | 11 | Os testes não trazem o id UT-BE-xx no nome ou em comentário. Os nomes de método seguem a spec, mas o rastreio teste para spec depende de convenção. | `src/Sim.Tests/**` | Opcional: comentário `// UT-BE-xx` na primeira linha de cada teste. |

### OBSERVATIONS

| # | Seção | Descrição |
|---|---|---|
| O-01 | 5.6 | `ArgumentNullException` de elemento nulo usa o `paramName` da lista (por exemplo `Players`), o que está de acordo com a spec ("nome do parâmetro/propriedade"). `CheckName` usa `nameof(name)`, que é "name" e não a propriedade. É cosmético e a spec não testa o `paramName`. |
| O-02 | 5.7 | RN-P1-16 e RN-P1-42 são verificadas só por revisão de código. O teste de varredura automática vem na Fase 2 (RN-17 da proposta). |
| O-03 | 5.3.7 | `Create` cria objetos e depois ordena. Está correto e fora do caminho por tick. Se a criação virar caminho quente (carregar save), vale revisitar. |

## 6. Estatísticas

| Categoria | Pass | Fail | N/A |
|---|---|---|---|
| Conformidade com a spec | 6 | 0 | 0 |
| Rastreabilidade das regras | 35 | 0 | 0 |
| Critérios de aceite (F1) | 12 | 0 | 0 |
| Revisão de código | 6 | 0 | 5 |
| Consistência entre camadas | 0 | 0 | 1 |
| Migração e seed | 0 | 0 | 1 |
| Testes | 71 | 0 | 0 |
| Análise estática e build | 3 | 0 | 0 |
| Documentação | 1 | 0 | 1 |
| Boas práticas | 4 | 0 | 5 |

## 7. Pontos positivos

- Os vetores numéricos da spec (SplitMix64, xoshiro, checksum de `Fixed`) foram reproduzidos exatamente.
- Encapsulamento firme: nenhuma coleção mutável exposta, e a verificação é por reflexão.
- Validação em dois passos (valida tudo, depois constrói) com ordem determinística e testada.
- Disciplina de TDD registrada no history (69 de 71 testes falharam antes da implementação).
- Zero alocação medida em `Fixed`, `SimRandom` e nas buscas.

## 8. Resultados de build e testes (executados nesta revisão)

| Verificação | Status | Erros | Avisos |
|---|---|---|---|
| Build (`Rts.sln`) | PASS | 0 | 0 |
| Testes (`src/Sim.Tests`) | PASS (71/71) | 0 | 0 |
| Formatação (`dotnet format --verify-no-changes`) | PASS | 0 | n/a |
| Grep CA-F1-08 (APIs proibidas e `static` mutável) | PASS | 0 | n/a |

Nada foi corrigido nesta passada: o pedido foi de revisão sem alteração de código. Os itens m-01 e m-02 não bloqueiam. Follow-ups não foram gerados (`followup:none`).
