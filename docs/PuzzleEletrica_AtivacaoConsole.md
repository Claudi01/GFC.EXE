# Elétrica — console, ferramentas e reparos

Atualizado em **20/10/2026**. Este é o guia vigente do puzzle: ativação, sorteio,
continuidade, cores, ferramentas, configuração da cena e validação.

> Para encontrar os documentos em ordem e distinguir o estado atual dos registros
> antigos, comece por [`docs/README.md`](README.md). Este guia reflete a cena
> salva atualmente em disco, inclusive a reorganização local ainda não commitada.

## Funcionamento aplicado

1. Inicie uma nova partida na cena **Aréa de Carga** (ou chegue nela pela Praia).
2. Mire em **CONTROL_CONSOLE_STRAIGHT**, dentro de **Modelos eletrica**, e pressione **E**.
3. O console ativa a alimentação e sorteia **de 1 a 3 armários com defeito por categoria**, de forma independente: fio rompido, alavanca desalinhada e ligação indevida. São **3 a 9 armários defeituosos no total**, com apenas um defeito por armário.
4. Os armários intactos que recebem alimentação ficam verdes. O primeiro defeito recebe a cor correspondente. Todos os armários posteriores ficam pretos, inclusive quando possuem defeito próprio.
5. Colete os três cubos de ferramenta com E. Eles podem ser coletados antes ou depois de acionar a central.
6. Na primeira caixa com defeito e alimentação, E inspeciona; outro E repara se o item necessário estiver no inventário. O texto informa qual item falta.
7. Cada fio rompido consome uma unidade de cobre. A chave de fenda e o alicate são reutilizáveis.
8. Ao reparar, a passagem avança e o próximo defeito recebe sua cor. Ao concluir todos os reparos, os armários ficam verdes e o console informa **Energia restabelecida**, sem exigir um segundo acionamento final.
9. Interagir novamente com o console não muda o sorteio. Repetir E em caixa reparada não consome mais itens.
10. O estado e os itens coletados são mantidos ao sair e voltar à cena na mesma partida. O reinício da execução continua usando o reset de partida já existente.

Antes de ligar, os materiais mantêm sua aparência original. A coloração utiliza overrides do Renderer e não modifica os materiais compartilhados do projeto.

| Cor | Estado |
|---|---|
| Verde | Intacto ou reparado, recebendo alimentação. |
| Marrom | Fio rompido, recebendo alimentação. |
| Vermelho | Alavanca desalinhada, recebendo alimentação. |
| Roxo | Ligação indevida, recebendo alimentação. |
| Preto | Sem alimentação devido à interrupção anterior; tem prioridade sobre qualquer defeito próprio. |

O preto ter prioridade foi confirmado pelo usuário. Assim, somente o primeiro defeito ainda não resolvido aparece com sua cor de defeito; os demais continuam registrados, mas ficam ocultos pela falta de alimentação.

## Configuração na cena

- `PuzzleEletrica` está no objeto **Modelos Eletrica (posicionados)**.
- `CentralEnergia` está no console **Modelos eletrica/CONTROL_CONSOLE_STRAIGHT**, confirmado pelo usuário. A cópia chamada `CONTROL_CONSOLE_STRAIGHT (1)` não foi configurada.
- Os 16 armários do grupo **Armarios** receberam `CaixaEnergia` e IDs exclusivos.
- Os cubos `Ferramenta 1`, `Ferramenta 2` e `Ferramenta 3` possuem `ItemPickup`, cores próprias e chaves de coleta exclusivas.
- A ordem elétrica é a ordem do array **Caixas** no gerenciador. Ela não deve ser
  deduzida só pelo nome do modelo nem pela ordenação alfabética da Hierarchy.
- Foram acrescentados 11 MeshColliders onde faltavam: cinco nos corpos dos armários antigos e seis nas partes do console.
- A reorganização recente renomeou os armários, remapeou seus IDs e alterou
  geometria ProBuilder das paredes. Para posições e aparência atuais, consulte a
  cena; as regras do puzzle não dependem dos nomes das paredes.

### Ordem configurada

| Posição no array `Caixas` | `idCaixa` | Nome atual na Hierarchy |
|---|---|---|
| 1 | C06 | `LARGE_ELECTRICAL_CABINET_C06` |
| 2 | C05 | `LARGE_ELECTRICAL_CABINET_C05` |
| 3 | C07 | `LARGE_ELECTRICAL_CABINET_C07` |
| 4 | C08 | `LARGE_ELECTRICAL_CABINET_C08` |
| 5 | C09 | `LARGE_ELECTRICAL_CABINET_C09` |
| 6 | C10 | `LARGE_ELECTRICAL_CABINET_C010` |
| 7 | C11 | `LARGE_ELECTRICAL_CABINET_C011` |
| 8 | C12 | `LARGE_ELECTRICAL_CABINET_C012` |
| 9 | C13 | `LARGE_ELECTRICAL_CABINET_C013` |
| 10 | C14 | `LARGE_ELECTRICAL_CABINET_C014` |
| 11 | C15 | `LARGE_ELECTRICAL_CABINET_C015` |
| 12 | C0 | `LARGE_ELECTRICAL_CABINET_C0` |
| 13 | C01 | `LARGE_ELECTRICAL_CABINET_C01` |
| 14 | C03 | `LARGE_ELECTRICAL_CABINET_C03` |
| 15 | C04 | `LARGE_ELECTRICAL_CABINET_C04` |
| 16 | C02 | `LARGE_ELECTRICAL_CABINET_C02` |

A sequência acima foi lida do array **Caixas** da cena, não ordenada pelo texto
do ID. Os 16 IDs atuais são preenchidos e únicos. `C0` é o ID literal do armário
na posição 12; o código aceita esse valor, mas a interação também exibirá `C0`.
Nomes como `C010` são apenas nomes de GameObject: a lógica usa `idCaixa` e a
referência serializada no array. Alterar IDs ou a ordem exige iniciar uma nova
partida, pois o sorteio salvo verifica a sequência.

As alterações recentes nas paredes são geometria ProBuilder da cena, separada
do estado do circuito. Depois de editar dimensões ou colisores, valide o acesso
físico e o raycast de interação em Play Mode.

A quantidade é sorteada separadamente para cada categoria, entre **1 e 3**, assim como as posições dos armários afetados. Por exemplo, uma partida pode ter 1 fio rompido, 3 alavancas desalinhadas e 2 ligações indevidas: seis armários com defeito. O circuito precisa ter pelo menos nove armários para comportar qualquer resultado; a cena atual tem 16. O antigo campo de quantidade fixa foi removido.

O sorteio continua sendo preservado durante a partida. Uma distribuição já ativada permanece igual; inicie uma nova partida para obter um sorteio com a regra atualizada.

## Código desta etapa

| Arquivo | Responsabilidade |
|---|---|
| `Assets/Scripts/CircuitoEletrico.cs` | Sorteio dos tipos/posições e avaliação da continuidade, sem dependência da Unity. |
| `Assets/Scripts/PuzzleEletrica.cs` | Ativação, persistência da distribuição e cálculo dos estados dos armários. |
| `Assets/Scripts/CentralEnergia.cs` | Interação do console e indicação da quantidade de defeitos. |
| `Assets/Scripts/CaixaEnergia.cs` | Inspeção e aplicação/restauração das cores. |
| `Assets/Scripts/ReparoEletrico.cs` | Escolha do item correto e regra de consumo: cobre é gasto, ferramentas são preservadas. |
| `Assets/Scripts/ItemPickup.cs` e `SistemaInventario.cs` | Sistemas existentes utilizados para coletar os placeholders, guardar os itens e consumir cobre. |
| `Assets/Scripts/EstadoMundo.cs` | Leitura/gravação de texto registrado para o reset da partida limpar o sorteio. |

O acionamento inicial do circuito está separado de **CircuitoCompleto**, que só será verdadeiro quando não restarem defeitos.

## Placeholders e uso dos itens

| Cubo existente | Cor no cenário | Item e quantidade | Uso |
|---|---|---|---|
| `Ferramenta 1` | Laranja | Fio de cobre ×3 (`fio_cobre`) | Uma unidade por fio rompido. |
| `Ferramenta 2` | Azul | Chave de fenda ×1 (`chave_fenda`) | Ajusta alavancas desalinhadas sem gastar a ferramenta. |
| `Ferramenta 3` | Amarelo | Alicate de corte ×1 (`alicate`) | Resolve ligações indevidas sem gastar a ferramenta. |

As posições, escalas, nomes e colisões dos três cubos foram preservados. Seus materiais são próprios, em `Assets/Data/Itens/Placeholders`, sem alterar materiais compartilhados de outros objetos.

Os três fios de cobre atendem ao máximo de três defeitos dessa categoria. A coleta não deve ser refeita ao retornar à cena: cada pickup tem uma chave persistente. Sem espaço no inventário, a coleta não remove o cubo.

**Basta possuir o item na maleta; não é necessário equipá-lo na mão nem selecioná-lo para reparar.** TAB/I abre o inventário existente. A coleta aproveita os nomes e a contagem já suportados pelo inventário; os cubos permanecem placeholders apenas do objeto no cenário.

O gerenciador está com **Permitir Reparos habilitado** e com as referências para fio, chave de fenda e alicate. O campo anterior `permitirReparoComCobre` foi migrado com `FormerlySerializedAs`; agora a autorização abrange as três categorias.

## O que ficou para depois

- Pé de cabra preso na alavanca, remoção do inventário e consequência posterior.
- Áudio, iluminação do ambiente, animações e ações de porta.

O fluxo normal dos três defeitos está associado à cena. O easter egg do pé de cabra continua separado: ele não substitui automaticamente a chave de fenda nesta versão.

Os campos opcionais de sons, listas de luzes/objetos e eventos continuam reservados. Eles não são acionados pela ativação inicial desta etapa.

## Verificação realizada e limites

- O sorteio atualizado passou em **7.011 verificações temporárias**, incluindo 1.000 sementes e todas as 27 combinações de quantidades (1 a 3 por categoria), sem sobreposição e com total de 3 a 9. Também foram conferidas a prioridade do preto, a aparência desligada e a continuidade após reparo simulado.
- Os reparos passaram em **25.098 verificações temporárias**, executadas com o Mono da instalação da Unity e o `InventarioGrade` real. Foram concluídos 1.000 circuitos sorteados com estoque de três fios e as duas ferramentas, conferindo ferramentas incorretas/ausentes, consumo de uma unidade, remoção da pilha vazia, reutilização e conclusão de todos os armários.
- Todos os scripts de runtime em Assets foram compilados com o compilador e as referências reais desta instalação da Unity, **sem erros**. O compilador emitiu nove avisos em scripts existentes de exemplos/shaders e em ItemDeixadoNoCenario.
- Na etapa inicial, a associação da cena foi conferida por referências locais e pela preservação de 616 blocos existentes de transforms, malhas, renderizadores e prefabs. Na etapa dos cubos, foram preservados 1.199 blocos da cena salva pelo usuário, incluindo todos os transforms e colliders; as mudanças ficaram nos três pickups, em seus materiais e na configuração do gerenciador.
- Uma revisão independente verificou o código. Foram ajustados o salvamento da distribuição em um registro único e a preservação de overrides visuais gerais.
- **A interação em Play Mode e a aparência dentro da cena ainda precisam de validação.** A tentativa anterior de integração em uma cópia temporária da Unity foi impedida pela inicialização da licença. Os testes atuais executam as regras e o inventário real no Mono, sem simular uma sessão de Play Mode.
- Os arquivos de teste, resultados, binários de verificação e a cópia temporária foram removidos ao finalizar esta etapa, conforme a regra do projeto. Os arquivos e caches preexistentes foram preservados. Nenhum comando Git foi executado.

## Outros documentos

- [`PuzzleEletrica.md`](PuzzleEletrica.md): histórico da primeira versão, com
  defeitos fixos e reparo exclusivamente com cobre.
- [`PuzzleEletrica_EstadoEPlanejamento.md`](PuzzleEletrica_EstadoEPlanejamento.md):
  decisões e propostas anteriores à implementação atual.
- [`MontagemEletrica.md`](MontagemEletrica.md): ferramenta de Editor usada na
  montagem física original dos armários e da tubulação.
