# Review: Foundation — Fase 2 (Ciclo)

**Data**: 2026-11-23
**AI-SPEC**: `docs/features/Foundation/AI-SPEC_Foundation_Phase_2.md`
**Veredito**: **APPROVED_WITH_OBSERVATIONS**
**Contagem**: 0 CRITICAL, 0 MAJOR, 1 MINOR, 3 OBSERVATION

Contexto da revisão: as divergências DD-P2-01..10 foram aprovadas pelo usuário, assim como `Step` aplicar os comandos na ordem recebida e o hash só sob demanda. Nada disso é tratado como DIVERGENT. O projeto não tem multi-tenant, RBAC, i18n, banco, API HTTP nem frontend nesta fase; essas seções do checklist são N/A.

## 1. Validações executadas

| Verificação | Resultado |
|---|---|
| `dotnet build Rts.sln` | PASS: 0 erros, 0 avisos |
| `dotnet test src/Sim.Tests` | PASS: 153/153 (71 da Fase 1 + 82 novos), 0 ignorados, 118 ms |
| `dotnet format Rts.sln --verify-no-changes` | PASS: código de saída 0 |
| `grep` por `float`/`double`/`decimal`/`System.Random`/`Godot` em `src/Sim` (sem comentários) | vazio |
| `grep '"test\.' src/Sim` (CA-F2-12) | vazio |
| `Rts.Sim.csproj` sem `PackageReference`; `.csproj`/`.props` da Fase 1 | inalterados (só há o `InternalsVisibleTo` da Fase 1) |
| Testes `Skip` | nenhum (o único match de "Skip" é o nome `TakeDueCatchesUpSkippedTicks`) |
| Marcadores `// UT-BE-xx` / `// E2E-BE-xx` | todos presentes: UT-BE-70..95, 100..105, 110..113, E2E-BE-01..03 |

## 2. Conformidade com a spec

Todos os arquivos de §5.2 existem com a estrutura e as assinaturas esperadas (`Simulation`, `SimContext`, `SimEvents`, `StateHasher`, `Command*`, `Json/*`, `Control/*`, `ISimSystem`, fixtures). Nenhum arquivo da Fase 1 foi alterado.

Verificado por leitura do código:
- `Simulation.Step`: varredura de nulos antes de qualquer mudança; validação na ordem `UnknownCountry` → `NotController` → `UnknownCommandType` → handler; sistemas em ordem do array copiado; `State.Tick = tick + 1` ao fim; devolve a mesma instância de `SimEvents`. Construtor copia a lista e congela o registro.
- `StateHasher`: ordem canônica de bytes de RN-P2-51 conferida campo a campo; little-endian por deslocamento, sem `BitConverter`; sem alocação (`Stock` como span).
- `CommandCodec`: opções idênticas às da spec (discriminador `type`, `IsRequired`, `Disallow`, `NumberHandling.Strict`); instância, sem estado estático; checagem de registro antes de serializar; `JsonException`/`NotSupportedException` viram `FormatException` com `InnerException`.
- `CommandRegistry`: validação de nome sem `Regex` (equivalente à regex de INV-REG-02), bijeção nome ↔ tipo, congelamento; dicionários só para busca, `List` ordenada iterada pelo codec.
- `CommandQueue`: inserção estável por `(Tick, Sequence)`, `TakeDue` monotônico, `LastTakenTick`.
- Controladores: `HumanController` preserva a ordem e compacta os pendentes sem alocar; `AiController` é stub; `ControllerSet` por busca binária e coleta em ordem de id.
- Encapsulamento (RN-P2-80): `SimEvents.Begin/AddApplied/AddRejection`, construtores de `SimEvents`/`SimContext`, setter de `SimContext.Tick` e `TryGetEntry` são internal; `Rejections` é `ReadOnlyCollection`.

Divergências MISSING / DIVERGENT: nenhuma.
EXTRA: `Fixtures/Refs.cs` e `Fixtures/RecordingController.cs` (helpers de teste; `RecordingController` é citado em UT-BE-93/94). Aceitável, só em `Sim.Tests`.

## 3. Rastreabilidade das regras de negócio

| Regra | Implementada | Arquivo | Testada | Teste | Veredito |
|---|---|---|---|---|---|
| RN-P2-01..03 | SIM | `Commands/CommandRegistry.cs` | SIM | UT-BE-70, 71 | COVERED |
| RN-P2-04, 05, 06, 07, 08 | SIM | `Commands/CommandCodec.cs`, `Json/*` | SIM | UT-BE-73..76, UT-BE-110/112 (varredura), E2E-BE-03 | COVERED |
| RN-P2-10..12 | SIM | `Commands/CommandQueue.cs` | SIM | UT-BE-77..79 | COVERED |
| RN-P2-20 | SIM | `Core/Simulation.cs` (Step) | SIM | UT-BE-88 | COVERED |
| RN-P2-21 | SIM | `Core/Simulation.cs` | SIM | UT-BE-81 | COVERED |
| RN-P2-22, 23, 27 | SIM | `Core/Simulation.cs` | SIM | UT-BE-82..84 | COVERED |
| RN-P2-24 | SIM | `Core/Simulation.cs` | SIM | UT-BE-85 | COVERED |
| RN-P2-25 | SIM | `Core/Simulation.cs` | SIM | UT-BE-86 | COVERED |
| RN-P2-26 | SIM | `Core/Simulation.cs` | SIM | UT-BE-80 | COVERED |
| RN-P2-28 | SIM | `Core/SimEvents.cs` | SIM | UT-BE-87 | COVERED |
| RN-P2-29 | SIM | `Core/Simulation.cs`, `StateHasher.cs` | SIM | UT-BE-105 | COVERED |
| RN-P2-30 | SIM | `Core/Simulation.cs` | SIM | UT-BE-89 | COVERED |
| RN-P2-40..43 | SIM | `Control/*` | SIM | UT-BE-90..93 | COVERED |
| RN-P2-44 | SIM | `Core/Simulation.cs` | SIM | UT-BE-94, 95 | COVERED |
| RN-P2-50..55 | SIM | `Core/StateHasher.cs` | SIM | UT-BE-100..104 | COVERED |
| RN-P2-60, 61 | SIM | `Fixtures/DeterminismScenario.cs` + Sim | SIM | E2E-BE-01, 02 | COVERED |
| RN-P2-70, 71 | SIM | `Architecture/ForbiddenApiScanner.cs` | SIM | UT-BE-110..112 | COVERED |
| RN-P2-80 | SIM | tipos internal | SIM | UT-BE-113 | COVERED |
| RN-P2-81 | SIM | grep de `"test.` vazio | N/A (revisão) | — | COVERED (por revisão) |

Valores de referência da spec conferidos nos testes e em execução verde: hash inicial `210089492672693724`, `hash[1]` `7140252220498885332`, `hash[100]` `14189062733609157557`, `hash[1000]` `11368851551727198225`, 1495 aplicados e 20 rejeitados, estoques finais da §5.8.3, hashes de controle negativo (RN-P2-54) e divergência exatamente a partir do tick 504 (RN-P2-61a).

## 4. Critérios de aceite

| ID | Estado | Nota |
|---|---|---|
| CA-F2-01 | PASS | build 0/0; nenhum `.csproj`/`.props` mudou; sem `PackageReference` |
| CA-F2-02 | PASS | 153 testes |
| CA-F2-03 | PASS | `dotnet format --verify-no-changes` = 0 |
| CA-F2-04 | PASS | E2E-BE-01 |
| CA-F2-05 | PASS | E2E-BE-02, UT-BE-103 |
| CA-F2-06 | PASS | UT-BE-73, 74, E2E-BE-03 |
| CA-F2-07 | PASS | UT-BE-82..84 |
| CA-F2-08 | PASS | UT-BE-77..79 |
| CA-F2-09 | PASS | UT-BE-90..95 |
| CA-F2-10 | PASS | UT-BE-110..112; verificação manual registrada em `history.md` (não reexecutada aqui) |
| CA-F2-11 | PASS | UT-BE-105 |
| CA-F2-12 | PASS | UT-BE-113; grep de `"test.` vazio |
| CA-F2-13 | PARCIAL | START/END e tempo real (~4 min) registrados; orçamentos informativos de §9.3 não medidos (ver m-01) |

| Tipo | Total | Pass | Fail | Parcial |
|---|---|---|---|---|
| Por fase (CA-F2) | 13 | 12 | 0 | 1 |
| Final (CA-FINAL) | 4 | n/a (Fases 3 e 4 pendentes; CA-FINAL-03 parcial: testes desta fase verdes) | — | — |

## 5. Definição de pronto (CLAUDE.md §8)

- Testes da spec passando, com determinismo: SIM.
- Sem `float`, `System.Random`, Godot ou relógio em `src/Sim`: SIM (varredura automatizada e grep manual).
- Nenhum id de conteúdo fixo em `src/Sim`: SIM; os nomes `test.*` ficam só em `Sim.Tests`.
- Sem `Dictionary`/`HashSet` iterado com efeito na ordem: SIM (dicionários só para busca).
- Sem estado estático mutável: SIM.
- Dados novos validados pelo carregador: N/A (Fase 3).
- `docs/history.md` atualizado: SIM, com a ressalva de m-01.
- Testes primeiro: registrado (49 de 153 falharam sobre esqueletos).

## 6. Findings

### CRITICAL
Nenhum.

### MAJOR
Nenhum.

### MINOR

| # | Seção | Descrição | Arquivo | Sugestão |
|---|---|---|---|---|
| m-01 | Documentação (CA-F2-13) | A spec pede registrar em `history.md` os orçamentos informativos de §9.3 (`Step` vazio, `ComputeHash`, E2E). A entrada END diz que não foram medidos individualmente (só a suíte total, 140 ms, e o E2E-BE-01 em 11 ms). O E2E de 11 ms já cobre o orçamento de 500 ms; faltam `Step` vazio e `ComputeHash`. | `docs/history.md` | Medir uma vez (ex.: `Stopwatch` num teste descartável fora de `src/Sim`) e anotar. Não bloqueia: orçamentos são informativos e não falham teste. |

### OBSERVATIONS

| # | Seção | Descrição |
|---|---|---|
| O-01 | Código | `Simulation.Step` inicializa `reason = None` e a reatribui em todos os ramos. Inofensivo; poderia ser declarada sem valor inicial. |
| O-02 | Desempenho | `CommandQueue.Enqueue` insere em `List` ordenada (O(n)) e `TakeDue` usa `RemoveRange`. Adequado para a fila pequena prevista (§9.1), mas reavaliar se E2/E7 enfileirarem em volume. |
| O-03 | Conflito registrado | A spec já anota o conflito entre a proposta e `Architecture_Overview.md` §3 (ordem por país e hash no fim do `Step`). A decisão do usuário (ordem recebida, hash sob demanda) foi seguida; considerar atualizar `Architecture_Overview.md` para evitar nova divergência. |

## 7. Testes

| Camada | Novos arquivos | Com teste | Sem teste |
|---|---|---|---|
| Sim (Core/Commands/Control) | 15 | 15 | 0 |
| Frontend / Handlers / Stores | N/A | N/A | N/A |

Cenários da spec: UT-BE-70..95, 100..105, 110..113 e E2E-BE-01..03 implementados e passando (tabela de 7.4 omitida por ser 1:1, ver §1).

## 8. Pontos positivos

- A varredura `ForbiddenApiScanner` falha com `arquivo:linha: regra texto`, transformando o risco F4 em rede automática.
- `Step` sem alocação confirmada por medição (`GC.GetAllocatedBytesForCurrentThread`) nos três cenários, incluindo `ComputeHash`.
- Valores de hash e estoques fixados por implementação de referência independente batem exatamente.
- Hash sem `BitConverter` e sem `string.GetHashCode`, portanto estável entre máquinas e processos.
- Contratos (`Command`, `ISimSystem`, `IController`, `CommandHandler<T>`) pequenos, estáveis e `sealed`.

## 9. Follow-ups

Nenhum registrado (`followup:none`). m-01 e O-01..03 ficam apenas neste relatório.
