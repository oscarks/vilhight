# Documento de Design – RTS Isométrico (Godot C#)

Nov 14, 2026 · @Oscar

## Visão geral

RTS isométrico no estilo Warcraft/StarCraft, feito em Godot 4 com C#, com árvore de tecnologia livre (estilo Civilization) e recursos que surgem e mudam conforme o conhecimento avança.

- **Marcação:** \[MVP\] entra no primeiro protótipo jogável; \[Depois\] fica para iterações seguintes.
- **Uso com a IA:** entregue a seção Diretrizes técnicas e depois uma seção por vez, na ordem da seção Ordem de implementação. Para cada sistema, peça interface pública, estrutura de dados, testes e só então a implementação.
- **Números:** todos os valores são iniciais e ficam em arquivos de dados, nunca no código.
- **Conteúdo original:** nomes, facções e unidades são próprios; Warcraft, StarCraft e Civilization servem só como referência de mecânica.

**Diferenciais:** o jogador possui um planeta inteiro com vários países e alterna o controle entre eles, assumindo a cultura, a tecnologia e a estratégia de cada um; os demais seguem no modo automático pela doutrina definida para eles. No fim da árvore, a viagem espacial e de dobra leva a outros mundos, com o interior da nave jogável andar por andar.

## Diretrizes técnicas

A simulação é C# puro, separada do Godot, roda em ticks fixos e é toda orientada a dados.

- &#91;MVP\] Godot 4.x com C# (.NET 8). A lógica de jogo não depende de `Node`, `Vector2` nem `delta`; os nodes só apresentam o estado.
- &#91;MVP\] Simulação em ticks fixos (20 por segundo); a renderização interpola entre ticks.
- &#91;MVP\] A simulação roda headless, com testes automatizados (xUnit/NUnit) sem abrir o editor.
- &#91;MVP\] Unidades, construções, recursos, receitas e tecnologias vêm de arquivos JSON validados no carregamento (ver Modelo de dados).
- &#91;MVP\] Entidade-componente ou ECS leve; meta de 300 a 500 unidades a 60 FPS.
- &#91;MVP\] Toda ação do jogador vira um comando serializável aplicado num tick. Isso prepara replays e multiplayer.
- &#91;Depois\] Determinismo para replays, batalhas verificáveis e validação no servidor: ponto fixo, RNG com semente, nada de `float` nem iteração de `Dictionary` com ordem indefinida na simulação.

## Arquitetura: mundos, países e controladores

A simulação suporta vários mundos e vários países desde o primeiro dia, mesmo que o MVP use um planeta só; adaptar isso depois obrigaria a reescrever o núcleo.

```
GameState
 ├─ Jogadores[]      (cada um possui planetas)
 ├─ Mundos[]         (planetas e naves; cada um com mapa, depósitos, entidades)
 │   └─ Países[]     (território, estoque, árvore, névoa, doutrina)
 │        └─ Controlador: Humano | IA(doutrina)
 └─ Naves[]          (entidades que saem de um mundo e entram em outro)
```

- &#91;MVP\] Tudo o que é "de quem joga" pertence ao **país**: estoque, população, árvore de tecnologia, recursos revelados, névoa de guerra, diplomacia.
- &#91;MVP\] O jogador é dono do planeta; cada planeta pertence a um único jogador.
- &#91;MVP\] Humano e IA implementam a mesma interface `IController`: leem o estado do país e emitem comandos. A simulação não sabe quem está jogando.
- &#91;MVP\] Todo comando carrega `countryId`; a simulação rejeita comandos para países que o emissor não controla.
- &#91;MVP\] A IA de cada país decide a cada 10 ticks, com deslocamento entre países, para espalhar a carga.
- &#91;MVP\] Simulação de baixa fidelidade para mundos que ninguém está vendo (economia agregada, sem pathfinding unidade a unidade), ainda determinística.
- &#91;MVP\] Hierarquia completa no modelo: Universo → Sistemas estelares (um por jogador) → Mundos → Países. O MVP usa um sistema com um planeta, mas o código não assume isso.
- &#91;MVP\] A baixa fidelidade é usada em três casos: mundos fora de vista durante a viagem da nave, recuperação após "Sair e continuar simulando" no single-player, e sistemas de jogadores offline no multiplayer.
- &#91;Depois\] O servidor do universo persistente roda o mesmo código C# da simulação (headless, .NET), em baixa fidelidade, para sistemas cujo dono está offline. Ao voltar, o jogador recebe o estado do servidor e o resumo do que aconteceu.

## Planeta, países e espécies

Um planeta tem até 8 países (6 por padrão) e até 3 espécies; o jogador assume qualquer país a qualquer momento, e o país deixado passa ao modo automático com a doutrina dele.

**Limites por tamanho de mapa**

| Tamanho do mapa | Países | Espécies |
| --- | --- | --- |
| Pequeno | 3–4 | 1–2 |
| Médio | 5–6 | 2–3 |
| Grande | 7–8 | 3 |

O teto de 8 vem do orçamento de cerca de 500 unidades (uns 60 por país), da atenção do jogador e do espaço de território.

**Espécies e culturas**

- &#91;MVP\] Cada país começa com uma única espécie.
- &#91;MVP\] A espécie define a biologia: atributos-base, necessidades (o que come e respira), unidades e construções próprias e ramos exclusivos na árvore comum.
- &#91;MVP\] A cultura pertence ao país: governo, valores e bônus escolhidos pelo jogador. Dois países da mesma espécie podem ser totalmente diferentes.
- &#91;Depois\] Difusão de tecnologia por comércio, fronteira ou espionagem: desconto para pesquisar o que um vizinho já domina.
- Lançamento com 3 espécies; a arte de unidades e construções é planejada por espécie desde o início.

**Troca de controle**

- &#91;MVP\] Seletor de países no HUD: bandeira, espécie, alertas e situação de cada um; um clique assume o controle e leva a câmera à capital.
- &#91;MVP\] Ao assumir, o jogador passa a ver o mundo como aquele país: névoa, tecnologias, recursos revelados, estoque e diplomacia dele.
- &#91;MVP\] O país deixado recebe o controlador de IA com a própria doutrina, sem pausa.
- &#91;MVP\] Tema do HUD (paleta, ícones) muda com a espécie e a cultura do país ativo.
- &#91;MVP\] Painel de "o que aconteceu" ao assumir: resumo dos eventos desde a última vez que o jogador esteve no país.
- &#91;MVP\] Durante uma batalha, o jogador não pode assumir os países envolvidos nem trocar de país; em incursões, não pode assumir outro país envolvido (ver Incursões e batalhas).

**Doutrina (modo automático)**

- &#91;MVP\] Cada país tem uma doutrina em dados: pesos de economia, militar, tecnologia e expansão; plano de pesquisa (tecnologias-alvo); postura com cada país (aliado, neutro, rival, alvo); composição de exército preferida.
- &#91;MVP\] Modelos prontos: econômico, militarista, científico, expansionista.
- &#91;MVP\] A IA explica o que está fazendo ("construindo quartel: postura rival com o país B"), num log por país.
- &#91;Depois\] Regras "se, então" editadas pelo jogador (por exemplo: se atacado, chamar aliados).
- &#91;Depois\] Editor de doutrina em abas (prioridades, pesquisa, diplomacia, regras), salvando no mesmo JSON que a simulação lê.
- &#91;Depois\] Aprendizado pelo histórico: enquanto o jogador controla o país, a simulação registra estatísticas (divisão de gastos, ordem de pesquisa, unidades preferidas, momento dos ataques). Ao sair, o perfil ajusta os pesos da doutrina. São contagens e médias, não machine learning, para manter o determinismo; o jogador vê e edita o resultado.

```json
{
  "countryId": "country_b",
  "species": "species_1",
  "culture": { "government": "council", "values": ["science", "trade"] },
  "doctrine": {
    "template": "scientific",
    "weights": { "economy": 0.3, "military": 0.2, "tech": 0.4, "expansion": 0.1 },
    "researchTargets": ["tech_smelting", "tech_arcane_geology"],
    "stance": { "country_a": "ally", "country_c": "rival" },
    "preferredArmy": { "ranged": 0.5, "melee": 0.3, "siege": 0.2 }
  }
}
```

**União de países**

- &#91;MVP\] Conquista ou anexação: o vencedor absorve território, construções e população.
- &#91;Depois\] União diplomática: aliança → federação → união, exigindo relação alta por um tempo mínimo.
- &#91;Depois\] Influência cultural: um país pressiona o vizinho até ele aderir.
- &#91;Depois\] País com várias espécies: cada população mantém a sua, as unidades de ambas ficam disponíveis, as tecnologias se somam e a cultura vira uma mistura escolhida pelo jogador.
- &#91;Depois\] Estabilidade: cai com diversidade e insatisfação; abaixo de um limite, o país pode se dividir.

## Mapa e câmera

O mapa é uma grade isométrica 2:1 carregada de arquivo, e os depósitos de todos os recursos já existem desde a geração, mesmo os ainda ocultos.

**Mapa**

- &#91;MVP\] Tiles isométricos em losango 2:1 com tipos de terreno: caminável, bloqueado, água.
- &#91;MVP\] Conversão entre coordenadas de tela, mundo e grade.
- &#91;MVP\] Ordem de desenho por Y (depth sorting) para unidades, construções e árvores.
- &#91;MVP\] Depósitos de recursos posicionados na geração do mapa, inclusive os que dependem de tecnologia (ver Economia). Distribuição equilibrada entre países.
- &#91;Depois\] Elevação com rampas e vantagem de visão para o terreno alto.
- &#91;Depois\] Mapas em arquivo com posições iniciais e editor simples.

**Câmera**

- &#91;MVP\] Rolagem pelas bordas, teclado (WASD/setas) e arrasto com o botão do meio.
- &#91;MVP\] Limites do mapa e zoom com mínimo e máximo.
- &#91;Depois\] Clique no minimapa move a câmera; atalhos para a base e para o último alerta.

## Seleção, controle e movimento

O controle segue o padrão clássico de RTS, e o movimento usa A\* individual com desvio local, evoluindo para flow fields em grupos grandes.

**Seleção e comandos**

- &#91;MVP\] Clique seleciona; caixa de seleção arrastando; Shift adiciona ou remove.
- &#91;MVP\] Duplo clique ou Ctrl+clique seleciona todas do mesmo tipo visíveis na tela.
- &#91;MVP\] Limite de seleção configurável (padrão 12).
- &#91;MVP\] Clique direito contextual: mover, atacar, coletar, construir ou consertar, conforme o alvo.
- &#91;MVP\] Grupos de controle: Ctrl+1–9 grava, 1–9 seleciona, duplo toque centraliza a câmera.
- &#91;MVP\] Comandos: Mover, Parar, Segurar posição, Atacar-mover (A + clique), Patrulhar.
- &#91;MVP\] Retorno visual: círculo de seleção, barra de vida, marcador no destino.
- &#91;Depois\] Fila de comandos com Shift (waypoints).
- &#91;MVP\] Os comandos valem fora de confrontos e em incursões; unidades em batalha não recebem ordens (ver Incursões e batalhas).

**Movimento e pathfinding**

- &#91;MVP\] A\* na grade para caminhos individuais.
- &#91;MVP\] Desvio local (separação/steering ou RVO) para as unidades não se sobreporem.
- &#91;MVP\] Raio de colisão por unidade; unidades amigas paradas abrem caminho.
- &#91;Depois\] Flow field quando muitas unidades vão ao mesmo destino.
- &#91;Depois\] Formações e grupo andando na velocidade da unidade mais lenta.
- &#91;Depois\] Unidades aéreas que ignoram o terreno.

## Economia e recursos evolutivos

Os recursos não são fixos: tecnologias revelam depósitos ocultos, liberam receitas de refino, aumentam o limite de coleta e aposentam recursos antigos, como em Civilization e Rise of Nations.

**Categorias de recurso**

| Categoria | Armazenamento | Como se obtém | Exemplo inicial |
| --- | --- | --- | --- |
| Básico | Estoque | Trabalhador coleta e entrega no depósito | comida, madeira, pedra |
| Bruto oculto | Estoque | Depósito revelado por tecnologia, extraído por construção | minério, carvão |
| Refinado | Estoque | Receita numa construção (entradas → saída) | metal, combustível |
| Estratégico | Cota | Cada fonte dá N vagas; unidades especiais ocupam vagas | cristal |
| Abstrato | Estoque, sem transporte | Gerado por construções ou trabalhadores alocados | conhecimento, energia |

**Regras**

- &#91;MVP\] `ResourceRegistry` carregado de dados; nenhum recurso fixo no código.
- &#91;MVP\] O conhecimento existe desde o início e paga as tecnologias (ver Árvore de tecnologia).
- &#91;MVP\] Visibilidade por país: um depósito com `revealedBy` só aparece, é clicável e é visível para a IA inimiga depois da tecnologia. Isso é independente da névoa de guerra.
- &#91;MVP\] Ver e extrair são separados: uma tecnologia revela, outra (ou a mesma) libera a construção extratora.
- &#91;MVP\] Cadeias com no máximo 3 níveis: bruto → refinado → avançado.
- &#91;MVP\] `RecipeProcessor` nas construções: consome entradas do estoque, gasta o tempo em ticks e entrega a saída; fica parado, com ícone de aviso, quando falta entrada.
- &#91;MVP\] Depósitos brutos se esgotam; recursos básicos renováveis (fazendas, florestas replantadas) não.
- &#91;MVP\] Limite de coleta por recurso (por minuto), elevado por tecnologias. Evita bola de neve e faz a evolução significar mais vazão.
- &#91;MVP\] Barra de recursos dinâmica: mostra só os recursos liberados, no máximo 5 ou 6 ao mesmo tempo.
- &#91;MVP\] Limite de população aumentado por construções de moradia.
- &#91;Depois\] Cota estratégica: a unidade reserva a vaga ao nascer e devolve ao morrer. Se o jogador perder fontes e ficar abaixo do que usa, as unidades afetadas sofrem penalidade de combate.
- &#91;Depois\] Obsolescência: tecnologias trocam o recurso exigido por unidades ou receitas (por exemplo, combustível no lugar de madeira); recursos sem uso somem da barra.
- &#91;Depois\] Mercado com câmbio variável entre recursos, como saída para quem não tem um depósito.
- &#91;Depois\] Bônus de produtividade: um bem consumido acelera construções (por exemplo, comida refinada acelera a produção de trabalhadores).

**Progressão de exemplo**

A ordem real depende do caminho que o jogador escolher na árvore; os tiers indicam profundidade mínima.

| Tier | Recurso | Revelado por | Extraído ou produzido por | Uso principal |
| --- | --- | --- | --- | --- |
| 0 | comida, madeira, pedra, conhecimento | início | trabalhador; biblioteca | unidades básicas, construções, pesquisa |
| 1 | minério | Mineração | mina | entrada da fundição |
| 2 | metal | Fundição | fundição (2 minério + 1 madeira) | armas, armaduras, cerco |
| 3 | cristal (estratégico) | Geologia arcana | extrator de cristal (4 vagas por fonte) | unidades de elite |
| 3 | carvão | Prospecção | mina de carvão | entrada de combustível |
| 4 | combustível | Destilação | refinaria (2 carvão → 1) | máquinas; substitui madeira em receitas |
| 5 | energia (abstrato) | Usinas | usina consumindo combustível | aciona construções avançadas |

## Árvore de tecnologia livre

A evolução é um grafo de tecnologias com pré-requisitos, sem eras obrigatórias: cada país tem a própria árvore e escolhe o próprio caminho, e a "era" é só um rótulo calculado a partir do que ele já pesquisou.

**Estrutura**

- &#91;MVP\] Grafo acíclico de tecnologias em dados. Cada tecnologia tem `id`, `tier`, `cost` (em conhecimento e, a partir do tier 2, outros recursos), `researchTicks`, `requires` (todas obrigatórias) e `effects`.
- &#91;Depois\] `requiresAny`: pelo menos uma de uma lista (caminhos alternativos para a mesma tecnologia).
- &#91;MVP\] Validação no carregamento: sem ciclos, sem referências quebradas, todo recurso e toda construção alcançáveis a partir do início.
- &#91;MVP\] Era exibida = tier mais alto com pelo menos N tecnologias pesquisadas. Serve para visual, música e IA, nunca como trava.

**Pesquisa**

- &#91;MVP\] Pesquisa feita em construções de pesquisa (biblioteca e similares); cada uma pesquisa uma tecnologia por vez, com fila.
- &#91;MVP\] Várias construções pesquisam em paralelo, mas nunca a mesma tecnologia.
- &#91;MVP\] Custo pago ao entrar na fila; cancelar devolve 100%.
- &#91;MVP\] Custo cresce 10% por tecnologia já pesquisada no mesmo tier, para evitar que alguém pesquise tudo.
- &#91;Depois\] Atalhos de descoberta (como os "eurekas" do Civilization VI): cumprir uma condição no mapa (construir 3 minas, vencer um combate com cerco) reduz o custo de uma tecnologia em 40%.
- &#91;Depois\] Ramos mutuamente exclusivos: escolher uma doutrina bloqueia a alternativa.

**Efeitos declarativos**

Toda tecnologia age só por efeitos de dados; o código tem um handler por tipo de efeito.

| Efeito | O que faz | Exemplo |
| --- | --- | --- |
| `revealResource` | Torna depósitos visíveis e selecionáveis | Mineração revela minério |
| `unlockBuilding` | Libera uma construção no menu do trabalhador | Mineração libera mina |
| `unlockUnit` | Libera uma unidade numa construção | Fundição libera soldado de metal |
| `unlockRecipe` | Libera uma receita numa construção | Fundição libera receita de metal |
| `modifyStat` | Altera um atributo (soma ou %) | +1 armadura para infantaria |
| `raiseGatherCap` | Aumenta o limite de coleta de um recurso | +25% no limite de madeira |
| `replaceRequirement` | Troca um recurso exigido por outro | combustível no lugar de madeira |
| `obsolete` | Remove uma unidade ou receita do menu | arqueiro sai quando chega o atirador |

**Interface da árvore**

- &#91;MVP\] Tela própria com o grafo inteiro visível, pan e zoom, linhas de dependência e estados: pesquisada, disponível, bloqueada, em pesquisa.
- &#91;MVP\] Tooltip com custo, tempo e tudo o que a tecnologia libera.
- &#91;MVP\] Clique numa tecnologia distante enfileira automaticamente os pré-requisitos que faltam, como no Civilization.
- &#91;Depois\] Busca por nome e filtro "o que libera o recurso X".

## Construções

Construções são liberadas pela árvore de tecnologia e podem produzir unidades, processar receitas, pesquisar ou extrair recursos.

- &#91;MVP\] Papéis combináveis por dados: `produces` (unidades), `recipes`, `research`, `extracts` (só sobre um depósito do tipo certo), `dropOff` (aceita entrega de recursos), `housing`.
- &#91;MVP\] O trabalhador posiciona a construção com pré-visualização verde ou vermelha; extratoras só ficam verdes sobre um depósito revelado.
- &#91;MVP\] Construção leva tempo, aparece em estágios e pode ser cancelada com reembolso de 75%.
- &#91;MVP\] Fila de produção de até 5 itens com barra de progresso.
- &#91;MVP\] Ponto de reunião para unidades recém-produzidas.
- &#91;MVP\] Construção de receita mostra entradas, saída e motivo de parada (falta de entrada, estoque cheio, sem energia).
- &#91;Depois\] Conserto por trabalhadores.
- &#91;Depois\] Melhoria de construção no lugar (por exemplo, mina → mina profunda), liberada por tecnologia.

## Unidades e combate

Cada unidade é um registro de dados com custo em qualquer combinação de recursos, inclusive cota estratégica.

- &#91;MVP\] Atributos: vida, armadura, dano, alcance, cadência, velocidade, visão, custo (mapa de recursos), tempo de produção, população ocupada.
- &#91;MVP\] Ataque corpo a corpo e à distância (projétil ou instantâneo).
- &#91;MVP\] Aquisição automática de alvos no alcance quando parada; prioridade para quem ataca.
- &#91;MVP\] Morte com animação e cadáver temporário.
- &#91;MVP\] Melhorias de ataque e armadura vêm de efeitos `modifyStat` da árvore.
- &#91;Depois\] Tipos de dano contra tipos de armadura (tabela de multiplicadores em dados).
- &#91;Depois\] Unidades terrestres e aéreas, com regras de quem ataca quem.
- &#91;Depois\] Habilidades com energia ou mana e tempo de recarga, acionadas pelo jogador em incursões e usadas automaticamente em batalhas.
- &#91;Depois\] Unidades de elite que ocupam cota estratégica e sofrem penalidade se a cota faltar.
- &#91;Depois\] Invisibilidade e detectores.

**Incursões e batalhas**

Todo confronto começa como incursão, com controle em tempo real como em Warcraft e StarCraft; se o número de combatentes passar do limite, vira batalha, resolvida por parâmetros e só assistida.

|  | Incursão | Batalha |
| --- | --- | --- |
| Combatentes na área | até o limite (padrão 30, somando os lados) | acima do limite |
| Controle das unidades | o jogador dá ordens normalmente | ninguém dá ordens; o jogador assiste |
| Resultado | simulação normal de combate | cálculo por parâmetros + aleatoriedade |
| Troca de país | pode sair do país; não pode assumir outro país envolvido | bloqueada |

**Incursão**

- &#91;MVP\] Um confronto é o grupo de unidades hostis em combate numa área (raio inicial de 12 tiles); reforços que entram na área passam a contar.
- &#91;MVP\] Contam como combatentes as unidades armadas e as construções defensivas na área; trabalhadores e civis não contam, a menos que ataquem.
- &#91;MVP\] Limite padrão de 30 combatentes, em dados e ajustável por mapa.
- &#91;MVP\] Na incursão, o país controlado pelo jogador recebe ordens em tempo real; os outros países envolvidos são conduzidos pela IA da doutrina deles.
- &#91;MVP\] Durante a incursão, o jogador pode deixar o país atual (a IA assume), mas não pode assumir outro país envolvido até o confronto terminar.
- &#91;MVP\] Ao passar do limite, a incursão vira batalha imediatamente. Uma vez batalha, continua batalha até o fim, mesmo que o número de combatentes caia (evita alternar entre os modos).
- &#91;MVP\] Aviso no HUD quando uma incursão se aproxima do limite (por exemplo, a 80%), para o jogador decidir se manda reforços ou recua.
- &#91;Depois\] Na fase 3, incursão entre jogadores só em tempo real se ambos estiverem online; com o defensor offline, todo confronto é resolvido como batalha.

**Batalha (acima do limite)**

- &#91;MVP\] Uma batalha começa quando um confronto passa do limite de combatentes; todas as unidades na área e as que chegarem entram nela.
- &#91;MVP\] Enquanto a batalha dura, o jogador não pode assumir nenhum país envolvido nem trocar de país.
- &#91;MVP\] O resultado é calculado em rodadas a partir de parâmetros: quantidade e atributos das unidades, tipos de dano e armadura, terreno e elevação, tecnologias, cota estratégica e postura da doutrina.
- &#91;MVP\] Aleatoriedade de ±15% por rodada (valor inicial, em dados), com RNG de semente: a mesma batalha com a mesma semente dá sempre o mesmo resultado.
- &#91;MVP\] A batalha é mostrada como animação das unidades executando o resultado calculado.
- &#91;MVP\] Relatório ao fim: perdas, causas principais e o peso da sorte no resultado.
- &#91;MVP\] Batalhas entre jogadores, na fase 3, usam o mesmo cálculo e funcionam com o defensor offline.
- &#91;MVP\] O jogador só assiste: comandos para unidades envolvidas são recusados até a batalha terminar, inclusive no país que ele controla.
- &#91;MVP\] Habilidades são usadas automaticamente na batalha, conforme a doutrina.
- &#91;MVP\] A influência do jogador vem de antes da batalha: composição do exército, posicionamento, terreno escolhido, tecnologias e doutrina.
- &#91;Depois\] Sugestão: postura de batalha definida antes do combate (formação, limite de perdas para recuar, alvo prioritário), usada como parâmetro do cálculo.

## Névoa de guerra e interface

A névoa de guerra esconde o que está fora da visão; a revelação por tecnologia esconde o que o jogador ainda não sabe reconhecer. São duas camadas independentes.

**Névoa de guerra**

- &#91;MVP\] Por país, três estados por tile: nunca visto, explorado (mostra a última informação conhecida) e visível.
- &#91;MVP\] Raio de visão por unidade e construção.
- &#91;MVP\] Depósito oculto por tecnologia não aparece nem em tile visível; ao pesquisar, os depósitos já explorados aparecem com o estado da última vez que o tile foi visto.
- &#91;Depois\] Bloqueio de visão por elevação.

**Interface (HUD)**

- &#91;MVP\] Painel inferior com retrato, atributos e grade de comandos da seleção.
- &#91;MVP\] Ícones da seleção múltipla; clicar seleciona aquela unidade.
- &#91;MVP\] Barra de recursos dinâmica no topo, com população e taxa atual contra o limite de coleta.
- &#91;MVP\] Minimapa com terreno, unidades, névoa e depósitos revelados.
- &#91;MVP\] Atalho de teclado por comando; tooltip com custo em todos os recursos.
- &#91;MVP\] Botão e atalho para a tela da árvore de tecnologia; indicador da pesquisa em andamento.
- &#91;MVP\] Aviso quando uma tecnologia revela um recurso novo, com ping no minimapa nos depósitos já explorados.
- &#91;Depois\] Alertas de ataque e mensagens de erro (recursos insuficientes, população máxima, cota esgotada).
- &#91;Depois\] Painel de economia com produção e consumo por recurso, em verde ou vermelho.

## IA dos países

A IA é o sistema central do jogo: a qualquer momento, quase todos os países são controlados por ela, seguindo a doutrina de cada um e usando os mesmos dados e comandos que o jogador.

- &#91;MVP\] Nenhuma lista de recursos, unidades ou tecnologias fixa no código da IA: ela pergunta aos dados "do que preciso para o próximo objetivo" e decide o que coletar, construir ou pesquisar.
- &#91;MVP\] Objetivos em camadas, ponderados pela doutrina: manter trabalhadores ocupados → expandir população → seguir o plano de pesquisa → montar o exército preferido → agir conforme a postura com cada país.
- &#91;MVP\] A IA respeita a mesma visibilidade do país: não enxerga depósitos ocultos nem através da névoa.
- &#91;MVP\] Assumir um país no meio de uma ação não quebra nada: a IA só emite comandos, e as unidades continuam a ordem atual até o jogador mandar outra.
- &#91;MVP\] Log de decisões por país, legível pelo jogador.
- &#91;MVP\] Ataques em ondas com intervalo e tamanho definidos pela doutrina.
- &#91;Depois\] Defesa da base, expansão para depósitos recém-revelados e disputa por fontes estratégicas.
- &#91;Depois\] Diplomacia entre países (aliança, comércio, união) conduzida pela doutrina.
- &#91;Depois\] Níveis de dificuldade para países de outros jogadores ou de planetas controlados pela IA.

## Fluxo de partida e áudio

A partida termina pela destruição do inimigo no MVP; outras vitórias e o multiplayer vêm depois.

**Fases da partida**

O jogo cresce em três escalas, e o multiplayer só começa quando a dobra conecta sistemas estelares.

| Fase | Liberada por | Escala | Jogadores | Adversário |
| --- | --- | --- | --- | --- |
| 1. Planeta natal | início | um planeta, vários países | 1 | os próprios países do jogador, controlados pela IA |
| 2. Sistema estelar | Propulsão nuclear | planetas do mesmo sistema; colônias | 1 | os próprios países e colônias; ambiente hostil |
| 3. Galáxia | Navegação de dobra | sistemas de outros jogadores | vários | outros jogadores e seus países |

- &#91;MVP\] Fase 1: o jogador joga contra si mesmo, alternando entre países; o objetivo é avançar a árvore até sair do planeta.
- &#91;Depois\] Fase 2: com a propulsão nuclear, naves alcançam os outros planetas do sistema e fundam colônias. O jogador continua sozinho e ganha mais pontos de vista.
- &#91;Depois\] Colônia nasce como território do país fundador e pode se tornar um país próprio, que o jogador também pode assumir. O limite de 8 países vale por planeta.
- &#91;Depois\] Fase 3: com a dobra, a nave alcança outros sistemas, cada um pertencente a outro jogador; a partida passa a ser multiplayer.
- &#91;Depois\] Cada sistema estelar pertence a um único jogador; planetas e colônias dentro dele também.
- &#91;Depois\] O universo é persistente: um servidor central guarda os sistemas estelares, as rotas de dobra e as naves em trânsito.
- &#91;Depois\] Um jogador só vê e alcança sistemas de jogadores que também já dominam a dobra.
- &#91;Depois\] Cada sistema é simulado na máquina do dono enquanto ele joga; interações entre sistemas (chegada de naves, comércio, ataques) passam pelo servidor de forma assíncrona, com batalhas resolvidas por parâmetros.
- &#91;MVP\] No single-player (fases 1 e 2), ao sair o jogador escolhe "Sair e pausar" ou "Sair e continuar simulando".
- &#91;MVP\] "Sair e continuar simulando" sem servidor: o jogo grava a hora da saída e, ao voltar, simula o tempo decorrido em baixa fidelidade (economia, pesquisa e batalhas por parâmetros), com teto configurável (por exemplo, 24 horas de jogo). Ao final, mostra o painel de "o que aconteceu".
- &#91;Depois\] No multiplayer, quando o dono sai, o servidor continua simulando o sistema dele em baixa fidelidade.
- &#91;MVP\] A fase 1 tem metas: agendas por país, marcos, pontuação e, depois, corrida espacial, crises e Legado (ver Metas da fase 1).

* &#91;MVP\] Um país é eliminado ao perder todas as construções; o planeta continua enquanto restar ao menos um país.
* &#91;MVP\] Pausa, menu principal e escolha de mapa.
* &#91;Depois\] Vitória tecnológica: pesquisar uma tecnologia final cara e defendê-la durante a pesquisa.
* &#91;Depois\] Campanha com missões e objetivos em script.
* &#91;Depois\] Salvar e carregar; replays gravando só os comandos.
* &#91;Depois\] Multiplayer em universo persistente, ativado pela dobra (ver Fases da partida).
* &#91;MVP\] Sons de seleção, ordem, ataque, morte, construção concluída e pesquisa concluída.
* &#91;Depois\] Música que muda com a era exibida e falas das unidades.

## Metas da fase 1

As metas combinam cinco camadas: agendas por país (o núcleo, que dá motivo para trocar de país), marcos planetários, crises, corrida espacial interna e pontuação do planeta. Um relógio global fica como modo opcional; campanha narrativa está fora.

| Camada | Função | Fase do projeto |
| --- | --- | --- |
| Agendas por país | Dar a cada país metas próprias, às vezes em conflito | MVP |
| Marcos planetários | Trilha comum até o espaço, com recompensas | MVP |
| Pontuação do planeta | Resumo sempre visível; reputação na fase 3 | MVP |
| Corrida espacial interna | Fechar a fase 1 e ligar à fase 2 | Depois |
| Crises | Pressão sem adversário externo | Depois |
| Legado | Moeda que leva o resultado da fase 1 para as colônias | Depois |
| Relógio global | Modo opcional "Planeta em colapso" | Depois |

**Agendas por país**

Cada país tem 2 ou 3 agendas, e o jogador escolhe, por país, como elas são definidas.

| Modo | Como funciona | Quando usar |
| --- | --- | --- |
| Sorteada (padrão) | O jogo sorteia do catálogo, com pesos pela cultura e pela espécie | Países em que o jogador não quer pensar |
| Escolhida | O jogo oferece 3 a 5 opções coerentes com a cultura e o jogador escolhe | Direcionar sem montar do zero |
| Definida pelo jogador | O jogador monta a agenda no editor, a partir do catálogo de condições | Experimentar uma ótica própria |

- &#91;MVP\] Cada país tem o próprio modo; misturar modos no mesmo planeta é permitido.
- &#91;MVP\] Uma agenda é uma condição em dados: métrica, comparação, valor, alvo opcional (outro país, recurso, região) e prazo opcional. O mesmo formato vale para os três modos.
- &#91;MVP\] Ao assumir um país, o jogador vê as agendas dele em destaque e o progresso de cada uma.
- &#91;MVP\] A agenda ajusta os pesos da doutrina: cada métrica do catálogo tem um vetor de pesos associado, então a IA persegue também as agendas definidas pelo jogador.
- &#91;MVP\] Recompensa em Legado proporcional à dificuldade calculada (distância entre o estado atual e o alvo, prazo, conflito com agendas de outros países).

**Proteções para agendas definidas pelo jogador**

- &#91;MVP\] Validação: a agenda não pode estar já cumprida nem ser impossível (alvo inexistente, recurso fora do mapa).
- &#91;MVP\] Agendas fáceis valem pouco: abaixo de uma dificuldade mínima, a recompensa é zero.
- &#91;MVP\] Conflito vale mais: agendas que competem com agendas de outros países recebem multiplicador de recompensa.
- &#91;MVP\] Coerência com a cultura: agenda contrária aos valores do país custa estabilidade enquanto estiver ativa; agenda alinhada dá bônus.
- &#91;MVP\] Troca com tempo de espera (por exemplo, uma vez a cada 10 minutos de jogo); abandonar uma agenda em andamento custa estabilidade, para evitar trocar só quando ela está para falhar.
- &#91;Depois\] Teto de Legado por período vindo de agendas definidas pelo jogador, para manter a justiça quando o Legado tiver efeito na fase 3.

```json
{
  "id": "agenda_custom_01",
  "country": "country_b",
  "mode": "custom",
  "condition": { "metric": "controlledSources", "resource": "crystal",
                  "op": ">=", "value": 3, "deadlineMinutes": 40 },
  "computed": { "difficulty": 0.72, "conflictsWith": ["country_c"], "legacyReward": 45 }
}
```

**Desfechos das agendas**

Cumprir fortalece o país, falhar o enfraquece, e os dois, repetidos, mudam a identidade dele. A recompensa é sempre maior que a punição, para o jogador não fugir de agendas ambiciosas.

| Desfecho | País | Planeta | Relações | Depois |
| --- | --- | --- | --- | --- |
| Cumprida | Bônus do tipo da agenda por 15 min; +10 de estabilidade | Legado e pontuação | +prestígio com todos | Nova agenda no mesmo modo, um degrau mais difícil |
| Falhou no prazo | −10 de estabilidade (−5 se chegou a 75%) | Sem Legado; metade se chegou a 75% | −prestígio | Nova agenda após 5 min |
| Impossibilitada por outro país | Sem penalidade | Sem Legado | Ressentimento contra o causador | Nova agenda imediata; a doutrina passa a ver o causador como rival |
| Abandonada pelo jogador | Custo de estabilidade | Sem Legado | — | Tempo de espera para trocar |
| Sem prazo e parada (20 min sem progresso) | Pequena perda contínua de estabilidade | — | — | O jogo sugere trocar a agenda |

- &#91;MVP\] Os desfechos valem igual com o país sob controle do jogador ou da IA.
- &#91;MVP\] Agenda com prazo rende mais Legado que a mesma sem prazo, porque pode falhar.
- &#91;MVP\] O painel de "o que aconteceu" lista as agendas cumpridas e falhadas desde a última visita do jogador ao país.
- &#91;MVP\] Todos os valores (bônus, penalidades, 75%, 15 e 20 min) ficam em dados.
- &#91;Depois\] Escalada: cada agenda cumprida libera a seguinte da mesma cadeia (por exemplo, 3 fontes de cristal, depois 5).
- &#91;Depois\] Ressentimento: relação cai com o país que tornou a agenda impossível e se recupera devagar com o tempo.
- &#91;Depois\] Tradições: 3 agendas cumpridas da mesma categoria (científica, militar, econômica, expansionista, diplomática) dão uma tradição permanente (por exemplo, −10% no custo de pesquisa) e deslocam os valores da cultura nessa direção.
- &#91;Depois\] 3 falhas na mesma categoria afastam a cultura dela e encarecem agendas desse tipo para o país.
- &#91;Depois\] Estabilidade muito baixa por falhas seguidas pode disparar uma crise social (revolta, secessão) pelo diretor de crises.

**Marcos planetários e pontuação**

- &#91;MVP\] Marcos em dados, com condição e efeitos (os mesmos tipos de efeito da árvore, mais Legado), concedidos uma única vez.
- &#91;MVP\] Exemplos: primeira tecnologia de cada tier (desconto de pesquisa para todos), população total de 200 (nova moradia), primeira união de países (bônus de estabilidade), primeiro satélite (revela o mapa do sistema).
- &#91;MVP\] Pontuação do planeta: soma ponderada de economia, ciência, população, estabilidade, marcos e agendas cumpridas, sempre visível no HUD.

**Corrida espacial, crises e Legado**

- &#91;Depois\] O primeiro país a alcançar a órbita vira a agência espacial do planeta e lidera a colonização na fase 2; os demais contribuem com recursos.
- &#91;Depois\] Diretor de crises com um orçamento de tensão que cresce com o tempo e com a pontuação, sorteado com semente.
- &#91;Depois\] Tipos de crise: natural (terremoto, seca, meteoro), social (revolta, secessão), biológica (pandemia entre espécies), ecológica (poluição por combustível e energia), externa (criaturas nativas).
- &#91;Depois\] Legado acumulado na fase 1 compra vantagens para as colônias na fase 2.
- &#91;Depois\] Derrota suave: se o planeta entrar em colapso, a partida termina com pontuação e o jogador recomeça mantendo parte do Legado.

## Viagem entre mundos

A viagem espacial e de dobra é o ramo final da árvore; a nave é uma megaconstrução, e o interior dela é um conjunto de andares jogáveis que reaproveitam mapa, unidades e receitas.

**Ramo espacial na árvore**

| Tier | Tecnologia | Libera |
| --- | --- | --- |
| 5 | Astronomia | observatório; mapa do sistema estelar |
| 6 | Foguetes | estaleiro; combustível químico; satélites |
| 7 | Órbita | estação orbital; montagem da nave |
| 8 | Propulsão nuclear | viagem entre planetas do mesmo sistema; colônias (fase 2) |
| 8 | Suporte de vida | andares habitáveis; receitas de oxigênio e água |
| 9 | Campo de dobra | núcleo de dobra; recurso estratégico cristal de dobra |
| 10 | Navegação de dobra | viagem entre sistemas estelares; contato com outros jogadores (fase 3) |

Nomes próprios para a tecnologia de dobra e os cristais; nada de termos ou naves de Star Trek.

**A nave**

- &#91;Depois\] Megaconstrução em etapas (casco, motores, andares), cada módulo com custo em metal, combustível, energia e cristal de dobra.
- &#91;Depois\] Pertence ao país que a construiu; leva tripulantes e carga desse país.
- &#91;Depois\] Tempo de viagem em minutos de jogo, proporcional à distância: tempo = distância ÷ velocidade do nível de propulsão (nuclear ou de dobra), com mínimo e máximo em dados.
- &#91;Depois\] Durante a viagem, a nave roda em fidelidade total e os mundos fora de vista continuam em baixa fidelidade.

**Interior em andares**

- &#91;Depois\] Cada andar (comando central, máquinas, restaurante, dormitórios e outros) é um mapa isométrico pequeno dentro do mundo da nave.
- &#91;Depois\] Um andar visível por vez; todos simulam o tempo todo.
- &#91;Depois\] Seletor de andar: corte vertical da nave com ícones de alerta por andar; clique ou PgUp/PgDn troca.
- &#91;Depois\] Elevadores ligam andares como portais no pathfinding.
- &#91;Depois\] Tripulantes são unidades que trabalham em estações.
- &#91;Depois\] Sistemas como receitas: suporte de vida (energia + água → oxigênio), cozinha (comida bruta → refeições), reator (combustível → energia).
- Escopo: layout fixo de andares primeiro; salas posicionadas pelo jogador e necessidades da tripulação (sono, fome, moral) depois.

**Painéis**

| Painel | Mostra e controla |
| --- | --- |
| Ecossistema | oxigênio, água, comida, temperatura, resíduos; produção contra consumo |
| Navegação | destino, rota, velocidade normal ou nível de dobra, chegada estimada, consumo |
| Recursos | carga, reservas e distribuição de energia entre motores, escudos e suporte de vida |

O painel está em todo andar e abre na aba ligada a ele (navegação no comando, reator nas máquinas).

**Eventos de viagem**

- &#91;Depois\] Eventos em dados, com condição, escolhas e consequências: falha de máquina, asteroides, sinal desconhecido, tripulante doente, abordagem.

**Godot**

- Uma cena `Node2D` com `TileMapLayer` por andar; trocar de andar alterna o nó visível e reposiciona a câmera.
- Painéis em `Control` dentro de `CanvasLayer`, só lendo o estado da simulação.
- Shader de estrelas em parallax atrás das janelas, com estiramento durante a dobra.
- Três visões sobre o mesmo `GameState`: planeta (RTS), mapa estelar e nave.

## Modelo de dados

Recursos, receitas, tecnologias, construções e unidades ficam em JSON. A IA deve gerar classes C# correspondentes e um validador.

```json
{
  "resources": [
    { "id": "wood", "category": "basic", "storage": "stock", "renewable": true },
    { "id": "knowledge", "category": "abstract", "storage": "stock" },
    { "id": "ore", "category": "raw_hidden", "storage": "stock",
      "revealedBy": "tech_mining", "extractedBy": "mine", "depletes": true },
    { "id": "metal", "category": "refined", "storage": "stock" },
    { "id": "crystal", "category": "strategic", "storage": "quota",
      "revealedBy": "tech_arcane_geology", "extractedBy": "crystal_extractor",
      "quotaPerSource": 4 }
  ],
  "recipes": [
    { "id": "smelt_metal", "building": "smelter",
      "inputs": { "ore": 2, "wood": 1 }, "outputs": { "metal": 1 },
      "durationTicks": 100 }
  ],
  "techs": [
    { "id": "tech_mining", "tier": 1, "cost": { "knowledge": 50 },
      "researchTicks": 600, "requires": [],
      "effects": [
        { "type": "revealResource", "resource": "ore" },
        { "type": "unlockBuilding", "building": "mine" } ] },
    { "id": "tech_smelting", "tier": 2, "cost": { "knowledge": 120, "ore": 50 },
      "researchTicks": 900, "requires": ["tech_mining"],
      "effects": [
        { "type": "unlockBuilding", "building": "smelter" },
        { "type": "unlockRecipe", "recipe": "smelt_metal" },
        { "type": "raiseGatherCap", "resource": "wood", "percent": 25 } ] }
  ],
  "eraLabels": [
    { "tier": 0, "name": "Fundação", "minTechs": 0 },
    { "tier": 2, "name": "Metal", "minTechs": 3 }
  ]
}
```

## Ordem de implementação e critérios de aceite

Implemente nesta ordem, um sistema por vez, cada um com testes headless passando antes do próximo.

1. Diretrizes técnicas e arquitetura: loop de ticks, comandos com `countryId`, `GameState` com mundos e países, `IController`, carregamento e validação de dados.
2. Mapa e câmera.
3. Seleção e comandos.
4. Movimento e pathfinding.
5. Economia básica: coleta, entrega, população.
6. Construções e produção de unidades.
7. IA dos países com doutrina e modelos prontos.
8. Vários países no planeta e troca de controle.
9. Árvore de tecnologia e conhecimento.
10. Recursos ocultos, receitas e limites de coleta.
11. Combate e anexação.
12. Névoa de guerra por país.
13. Interface completa: seletor de países, tela da árvore, painel de "o que aconteceu".
14. Espécies e culturas.
15. Fluxo de partida, metas da fase 1 (marcos, pontuação, agendas nos três modos) e áudio.
16. Editor de doutrina e aprendizado pelo histórico.
17. União diplomática e estabilidade; corrida espacial, diretor de crises e Legado.
18. Viagem entre mundos: ramo espacial, nave e mapa estelar; depois o interior em andares e os eventos.

**Critérios de aceite**

- [ ] 50 unidades atravessam um gargalo em menos de 20 s sem ficar presas.
- [ ] Um trabalhador completa o ciclo de coleta repetidamente sem intervenção.
- [ ] Antes de Mineração, nenhum depósito de minério é visível nem clicável; depois, todos os explorados aparecem.
- [ ] A fundição produz exatamente 1 metal a cada 100 ticks com entrada suficiente e para quando falta entrada.
- [ ] A coleta de um recurso nunca passa do limite configurado.
- [ ] Clicar numa tecnologia de tier 3 enfileira todos os pré-requisitos que faltam, na ordem certa.
- [ ] O validador recusa uma árvore com ciclo ou com recurso inalcançável.
- [ ] A IA inimiga pesquisa, refina metal e ataca com unidades de metal sem nenhuma regra fixa no código.
- [ ] Duas simulações com a mesma semente e os mesmos comandos terminam no mesmo estado.
- [ ] Trocar de país muda névoa, tecnologias, recursos revelados e estoque exibidos, e o país anterior continua agindo pela doutrina no tick seguinte.
- [ ] Um comando com `countryId` de um país não controlado pelo emissor é rejeitado.
- [ ] Seis países de IA rodam uma partida de 30 minutos simulados sem travar e com cada um seguindo o próprio plano de pesquisa.
- [ ] Um país anexado transfere território, construções e população ao vencedor.
- [ ] Durante uma batalha, toda tentativa de trocar de país ou assumir um país envolvido é recusada.
- [ ] A mesma batalha com a mesma semente dá o mesmo resultado; com 1.000 sementes, a variação fica dentro da margem configurada.
- [ ] Um comando para uma unidade em batalha é recusado, inclusive no país controlado pelo jogador.
- [ ] Depois de "Sair e continuar simulando" e 8 horas fora, ao voltar o jogo recupera o tempo em menos de 30 segundos e mostra o resumo.
- [ ] Um confronto com 30 combatentes continua como incursão e aceita ordens; ao entrar o 31º, vira batalha e as ordens passam a ser recusadas.
- [ ] Uma batalha que cai abaixo de 30 combatentes continua batalha até terminar.
- [ ] Trabalhadores que não atacam não contam para o limite.
- [ ] Uma viagem com o dobro da distância leva o dobro do tempo, respeitando mínimo e máximo.
- [ ] Durante a viagem, os planetas continuam produzindo e pesquisando em baixa fidelidade.
- [ ] Cumprir um marco da fase 1 concede a recompensa definida nos dados uma única vez.
- [ ] Uma agenda definida pelo jogador que já está cumprida ou é impossível é recusada pelo validador.
- [ ] Uma agenda abaixo da dificuldade mínima rende zero Legado; uma em conflito com outro país rende mais que a mesma sem conflito.
- [ ] Com o jogador fora do país, a IA muda as prioridades para perseguir uma agenda definida por ele.
- [ ] Uma agenda cumprida concede Legado, bônus de 15 min e +10 de estabilidade, e uma nova agenda surge no mesmo modo.
- [ ] Uma agenda que falha no prazo com 80% de progresso aplica −5 de estabilidade e metade do Legado.
- [ ] Quando o país A torna impossível a agenda do país B, B não é penalizado e passa a tratar A como rival na doutrina.
- [ ] Três agendas científicas cumpridas pelo mesmo país concedem a tradição científica uma única vez.

## Questões em aberto

Estas decisões mudam objetivos e regras e precisam de resposta antes das fases correspondentes.

1. **Infraestrutura do servidor:** onde hospedar e quantos sistemas offline cada servidor simula; define o custo do universo persistente.

**Decidido:**

- Na fase 1 o jogador é o único no planeta e joga contra si mesmo; a propulsão nuclear abre o próprio sistema estelar, ainda com um jogador; a dobra abre outros sistemas, de outros jogadores, e a partida vira multiplayer.
- O universo é persistente.
- Um jogador só acessa sistemas de jogadores que também já chegaram à dobra.
- Ao iniciar uma batalha, o jogador não pode assumir nem trocar de país; a batalha é resolvida por parâmetros mais uma porcentagem de aleatoriedade.
- Nas batalhas, o jogador só assiste.
- No single-player, ao sair, o jogador escolhe entre "Sair e pausar" e "Sair e continuar simulando".
- Confrontos até um limite de combatentes são incursões, com controle em tempo real como em Warcraft e StarCraft; acima do limite, viram batalhas resolvidas por parâmetros.
- No multiplayer, quando o dono sai, o sistema dele continua em baixa fidelidade.
- A viagem dura minutos de jogo, proporcional à distância entre os mundos.
- Durante a viagem, a simulação dos mundos continua em baixa fidelidade.
- O jogo lança com 3 espécies.
- A fase 1 tem metas.
- Metas da fase 1 em camadas, com agendas por país sorteadas, escolhidas ou definidas pelo jogador, conforme ele preferir para cada país.

## Referências

- [Civilization VI Wiki – Resource](https://civ6.fandom.com/wiki/Resource): recursos estratégicos invisíveis até a tecnologia.
- [Civilization Wiki – Resources in Civ5](https://civilization.fandom.com/wiki/List_of_resources_in_Civ5): categorias e penalidade por falta de recurso.
- [Well of Souls – Civ5 resources](https://well-of-souls.com/civ/civ5_resources.html): recursos alocados voltam quando a unidade morre.
- [Civilopedia – Consumable Resources](https://www.civilopedia.net/gathering-storm/concepts/consumeable_resources): estoque de combustível e material.
- [Endless Legend Wiki – Strategic Resources](https://endlesslegend.wiki.gg/wiki/Strategic_Resources): tiers de recursos por era e mercado.
- [Giant Bomb – Rise of Nations](https://www.giantbomb.com/games/3030-11993/): recursos liberados por era e limite de coleta.
- [StrategyWiki – The Settlers III Goods](https://strategywiki.org/wiki/The_Settlers_III/Goods): proporções de cadeias de produção.
- [Anno Union – Residential Tiers](https://www.anno-union.com/en/devblog-residential-tiers/): progressão que libera cadeias novas.
