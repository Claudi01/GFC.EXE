# Histórico — planejamento anterior do puzzle da elétrica

> **Documento histórico; não representa o estado atual do projeto.** Este plano foi
> escrito antes da implementação do console e dos três tipos de reparo. Algumas
> pendências registradas aqui já foram concluídas. Para o estado vigente, use
> [`PuzzleEletrica_AtivacaoConsole.md`](PuzzleEletrica_AtivacaoConsole.md).

Atualizado em **20/10/2026**. Cena: **Aréa de Carga**.

Este documento registra o código existente e o funcionamento discutido para a próxima versão. **Planejado não significa implementado ou validado em jogo.** Nesta conversa foram feitas leituras do projeto; nenhuma funcionalidade, cena ou posição de objeto foi alterada.

## 1. Objetivo desta etapa

Deixar o puzzle funcional: coletar itens, identificar e corrigir defeitos nos armários, acompanhar a continuidade do circuito e concluir o restabelecimento da energia.

O jogador deverá encontrar as ferramentas espalhadas pelo cenário. Enquanto não acionar a energia, os armários manterão a aparência original. Depois do acionamento, as cores indicarão os estados do puzzle.

**Áudio, iluminação do ambiente e animações ficam para depois.** A mudança de cor dos armários faz parte do retorno visual funcional desta etapa e não exige implementar lâmpadas ou efeitos de iluminação.

## 2. O que já existe no código

| Arquivo | Alcance atual |
|---|---|
| `Assets/Scripts/PuzzleEletrica.cs` | Lista de caixas, validação, contagem de defeitos/reparos, consumo de fio e registro de energia ligada. |
| `Assets/Scripts/CaixaEnergia.cs` | Inspeção com E, indicação de fiação intacta ou rompida e tentativa de reparo. |
| `Assets/Scripts/CentralEnergia.cs` | Informa reparos pendentes e permite ligar a central quando todos forem concluídos. |
| `Assets/Scripts/InteracaoPlayer.cs` | Reconhece caixas/central por componentes, mostra o texto de interação e chama a interação com E. |
| `Assets/Scripts/ItemPickup.cs` | Coleta de itens, quantidades e chave opcional para impedir reaparecimento após a coleta. |
| `Assets/Scripts/InventarioGrade.cs` e `SistemaInventario.cs` | Empilhamento, contagem e consumo de unidades de um item. |
| `Assets/Scripts/EstadoMundo.cs` | Registro de progresso da partida, utilizado para reparos, energia e pickups. |
| `Assets/Data/Itens/Item_FioDeCobre.asset` | Fio de cobre com ID `fio_cobre`, empilhável, tamanho 1×1. |

### Fluxo implementado na primeira versão

1. O defeito é escolhido manualmente pelo campo `necessitaReparo` em cada caixa.
2. O primeiro E inspeciona a caixa. Uma caixa intacta informa que a fiação está boa.
3. Outro E em uma caixa defeituosa tenta consumir **uma unidade de fio de cobre**.
4. Com o consumo bem-sucedido, a caixa é registrada como reparada. Repetir a interação não consome mais fios nela.
5. A central permanece bloqueada enquanto houver reparos pendentes.
6. Após todos os reparos, uma interação separada na central registra a energia ligada.

O fio é utilizado diretamente do inventário, sem precisar de modelo equipado na mão. O sistema já respeita os bloqueios de interação usados pelo inventário e pelo diário.

Reparos, energia e coletas são registrados para persistir entre cenas na mesma partida. Atualmente, `LimparSaveAoIniciarAplicacao = true` reinicia o progresso ao iniciar uma nova execução; não há um fluxo de continuar partida implementado para este puzzle.

Existem campos opcionais para sons, alternância de objetos visuais e eventos. Esses campos não implementam automaticamente as ações associadas e não serão o foco agora.

### Limites do código atual

- Existe somente **um tipo de defeito**, tratado como fiação rompida e resolvido com cobre.
- Não existe sorteio dos armários defeituosos ou dos tipos de defeito.
- A lista de caixas serve para contagem e validação; sua ordem **não representa um circuito**.
- Não existe propagação de alimentação de uma caixa para a seguinte.
- Não existe diferenciação entre defeito próprio e falta de alimentação causada por outra caixa.
- Não existe o sistema de cores discutido para acompanhar a progressão.
- Não existem os reparos com chave de fenda/alicate nem a gambiarra com o pé de cabra.
- Não existe minigame de conexão de fios: o reparo atual é uma interação direta com consumo de item.

## 3. Situação dos objetos na cena salva

Na verificação dos arquivos salvos do projeto:

- A cena contém cinco armários nomeados `LARGE_ELECTRICAL_CABINET (1)` até `(5)` no grupo de modelos posicionados, além de tubulação e do objeto `Motor Eletrica`.
- Há cubos numerados usados como marcadores pela ferramenta de montagem. Sua presença não comprova que a distribuição final dos armários esteja concluída.
- Não foram encontradas referências aos componentes `PuzzleEletrica`, `CaixaEnergia` ou `CentralEnergia` nas cenas e prefabs salvos examinados, excluindo as cópias de recuperação.
- A definição de fio de cobre existe, mas não foi encontrada associada a pickups ou a um gerenciador nos arquivos de conteúdo examinados.
- O pé de cabra já possui definição de item e pickup na Aréa de Carga. Chave de fenda e alicate ainda não possuem definições em `Assets/Data/Itens`.

Essas observações se referem ao conteúdo em disco. Alterações ainda não salvas no Editor não foram inspecionadas.

A ferramenta `Assets/Editor/GfcMontagemEletrica.cs` serve para montar armários e tubulação por comando explícito. Ela não adiciona scripts ou IDs do puzzle aos armários. Sua utilização e a distribuição física deverão ser conferidas antes de qualquer aplicação; este planejamento não autoriza reposicionar os objetos existentes.

## 4. Defeitos e soluções discutidos

| Defeito | Solução prevista | Consumo | Estado |
|---|---|---|---|
| Fio rompido | Fio de cobre | Uma unidade por reparo | Lógica existente; falta associar à cena. |
| Alavanca desalinhada | Chave de fenda para ajustar o mecanismo | Ferramenta reutilizável, conforme proposta discutida | Ainda não implementado. |
| Ligação indevida causando curto | Alicate de corte para remover a ligação indevida | Ferramenta reutilizável, conforme proposta discutida | Ainda não implementado. |

O preto, usado para indicar falta de alimentação, **não é um quarto defeito nem substitui um dos três tipos**.

### Easter egg do pé de cabra

A caixa com a alavanca desalinhada poderá aceitar uma solução improvisada: encaixar o pé de cabra para sustentar o mecanismo. O defeito será resolvido, mas **o pé de cabra ficará preso no armário e será removido do inventário**.

A consequência desejada é o jogador perceber depois que sacrificou uma ferramenta essencial para a sequência do jogo. Isso ainda não existe no código.

Permanecem abertas a forma de escolher essa interação alternativa, a representação temporária do pé de cabra preso e a possibilidade de recuperá-lo. Não foi decidido se perder a ferramenta exigirá reiniciar ou se haverá uma forma posterior de desfazer a gambiarra.

## 5. Sorteio e circuito em sequência

### Requisitos solicitados

- Trabalhar com um conjunto de armários candidatos a apresentar defeitos.
- Um script deverá sortear os armários defeituosos.
- Os armários deverão formar uma sequência de passagem de eletricidade.
- Uma falha deverá interromper a alimentação dos armários posteriores, mesmo quando eles não tiverem defeito próprio.

Cada armário precisará guardar separadamente seu **defeito próprio** e sua **situação de alimentação**. Reparar um ponto não deverá apagar os defeitos sorteados nos seguintes.

Exemplo de ordem: `Entrada → C01 → C02 → C03 → C04 → C05`.

Se C02 interromper a passagem, C03 em diante ficarão sem alimentação. Ao reparar C02, a continuidade poderá avançar até encontrar a próxima interrupção.

### Proposta técnica ainda não implementada

- Manter a ordem do circuito fixa e configurável.
- Sortear um subconjunto dos armários elegíveis e atribuir os tipos de defeito.
- Fazer o sorteio uma vez por partida, preservando o resultado ao sair e voltar à cena.
- Recalcular a alimentação após cada reparo.
- Garantir quantidade de cobre suficiente para os defeitos sorteados e acesso às ferramentas necessárias.

Quantidade de falhas, distribuição dos tipos, possibilidade de repetição e eventuais restrições por armário ainda precisam ser definidas. A sugestão anterior de três caixas com defeito fixo foi substituída pelo pedido de randomização.

## 6. Cores e acionamento da energia

**Antes de o personagem acionar a energia, os armários ficam com sua aparência atual.** Após o acionamento, as cores passam a indicar a progressão.

| Cor | Significado discutido |
|---|---|
| Verde | Armário funcionando e recebendo alimentação. |
| Marrom | Fio rompido, associado ao reparo com cobre. |
| Vermelho | Alavanca desalinhada, associada à chave de fenda e ao easter egg. |
| Roxo | Ligação indevida, associada ao alicate. |
| Preto | Falta de alimentação por interrupção anterior. |

A existência das cinco cores e o preto como falta de alimentação foram pedidos pelo usuário. A associação de marrom/vermelho/roxo aos três defeitos acima é a proposta registrada na conversa, ainda sujeita à confirmação final.

### Prioridade de cor ainda em aberto

O exemplo informado foi: **C01 verde, C02 marrom, C03 preto, C04 vermelho e C05 roxo**.

Como uma falha em C02 também corta a alimentação de C04 e C05, é necessário escolher a regra quando um armário possui defeito próprio e está sem alimentação:

- **Defeito próprio tem prioridade:** C04 e C05 exibem vermelho/roxo; um armário intacto sem alimentação, como C03, exibe preto. Essa foi a interpretação apresentada, ainda não confirmada.
- **Falta de alimentação tem prioridade:** todos os armários posteriores à interrupção exibem preto até a passagem ser restabelecida.

Também está pendente se será possível diagnosticar e reparar defeitos posteriores antes de sua alimentação voltar.

### Mudança necessária na central

Hoje, `TentarLigar()` rejeita o acionamento enquanto `Pendentes != 0`. Isso não atende sozinho ao fluxo novo, em que o jogador aciona a energia antes de concluir os reparos para revelar os estados.

A implementação futura deverá distinguir **acionamento inicial do circuito** e **puzzle totalmente resolvido**. Ainda precisa ser decidido se haverá uma segunda interação final na central ou se a conclusão ocorrerá automaticamente ao restabelecer toda a sequência. Nenhuma dessas mudanças foi feita.

## 7. Ferramentas e placeholders

O usuário distribuirá os itens pelo cenário. As ferramentas sem modelo usarão cubos temporários, com uma cor distinta para cada item. O pé de cabra aproveitará o objeto existente.

Convenção sugerida, ainda não aplicada:

| Objeto | Nome sugerido | Cor sugerida |
|---|---|---|
| Fio de cobre | `PUZ_Pickup_FioCobre` | Laranja |
| Chave de fenda | `PUZ_Pickup_ChaveFenda` | Azul |
| Alicate | `PUZ_Pickup_Alicate` | Amarelo |
| Marcador da central | `PUZ_Central` | Verde |
| Marcadores dos armários | `PUZ_C01`, `PUZ_C02` etc. | Sem cor de defeito fixa |

As cores dos pickups identificam os itens temporários; são independentes das cores de estado dos armários. Os marcadores dos armários devem identificar sua ordem, pois o defeito será sorteado. Os nomes anteriores que fixavam um defeito por posição deixam de representar o planejamento atual.

Ainda não foi fechado se bastará possuir a ferramenta no inventário ou se será necessário equipá-la. O fio de cobre já funciona por posse e consumo direto. A implementação inicial deverá aproveitar a interação existente com E, sem exigir animações.

## 8. Trabalho pendente

1. Fechar as regras abertas de cores, sorteio, central, uso de ferramentas e consequência do pé de cabra preso.
2. Conferir os armários existentes, definir os participantes e registrar sua ordem no circuito.
3. Ampliar o código para os três defeitos, soluções e alternativa do pé de cabra.
4. Implementar sorteio persistente durante a partida, alimentação em sequência e cores após o acionamento.
5. Associar os componentes aos objetos e configurar os pickups distribuídos pelo usuário.
6. Validar o fluxo em execução, incluindo consumo de cobre, propagação, retorno à cena e perda da ferramenta no easter egg.

Até esta atualização, a análise foi estática: **não foi executado Play Mode, build ou teste do puzzle**. O código existente não foi declarado validado em execução.

## 9. Regras de trabalho e documentação anterior

- Somente o usuário opera o Git. Não executar comandos Git.
- Testes criados pelo assistente serão temporários e deverão ser excluídos após a execução, junto com os caches e resíduos gerados por eles. Preservar arquivos e caches preexistentes.
- Manter o foco na funcionalidade; áudio, iluminação do ambiente e animações não fazem parte desta etapa.

`docs/PuzzleEletrica.md` documenta a primeira versão, de defeitos fixos e reparo exclusivamente com cobre. `docs/MontagemEletrica.md` documenta a ferramenta de montagem física. Ambos foram preservados; este arquivo registra as diferenças entre essa base e o novo planejamento.
