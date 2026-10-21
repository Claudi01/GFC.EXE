# Histórico — puzzle da elétrica, primeira versão

> **Documento histórico, não é o guia para configurar a cena atual.** Ele descreve
> a versão original, com defeitos fixos e reparo somente com cobre. Para o fluxo
> vigente, consulte [`PuzzleEletrica_AtivacaoConsole.md`](PuzzleEletrica_AtivacaoConsole.md).

Cada caixa defeituosa consome **uma unidade de fio de cobre**. As unidades ficam
empilhadas em um espaço 1x1 da maleta. Lanterna, gancho e pé de cabra continuam
sendo itens únicos. O fio é usado direto do inventário: não precisa de modelo na mão
nem de entrada no catálogo de `ControleDeItens`.

Nenhuma cena ou posição de objeto foi alterada pela implementação. Configure na
cena **Aréa de Carga**, fora do Play Mode. A porta e sua animação são outra etapa.

## Fluxo de jogo

1. O jogador vê caixas nos corredores, mas não precisa ter os fios para inspecioná-las.
2. Ao mirar em uma caixa e apertar **E**, inspeciona sua fiação.
3. Uma caixa intacta informa que está boa. Não consome fios.
4. Uma caixa danificada pede um fio; com ele na maleta, outro **E** repara e consome uma unidade.
5. Reparar a última caixa **não** liga a energia. É necessário voltar à central e apertar **E**.
6. A central ativa as luzes/efeitos e o som do motor configurados no Inspector.

Os defeitos são fixos e configuráveis, sem sorteio: os testes podem seguir sempre
a mesma rota. Comece, por exemplo, com seis caixas, das quais três precisam de reparo.
Isso é uma sugestão de montagem, não uma quantidade fixada no código.

## 1. Gerenciador

- Crie um Empty chamado `Puzzle_Eletrica` e adicione `PuzzleEletrica`.
- Mantenha `Chave Puzzle = carga.eletrica`. Outro puzzle precisa de outra chave.
- Em `Fio Cobre`, arraste `Assets/Data/Itens/Item_FioDeCobre.asset`.
- Deixe o objeto do gerenciador ativo durante toda a cena.

## 2. Caixas dos corredores

Para cada caixa investigável:

- Adicione `CaixaEnergia` ao objeto raiz da caixa.
- Adicione/ajuste um **Box Collider**, com **Is Trigger desmarcado**. O collider
  deve estar nesse objeto ou em um filho. Não use a layer `Ignore Raycast`.
- Arraste `Puzzle_Eletrica` para o campo `Puzzle`.
- Dê um `Id Caixa` exclusivo: `C01`, `C02`, `C03` etc. Ao duplicar caixas, troque esse ID.
- Marque `Necessita Reparo` só nas defeituosas.
- Opcional: associe filhos em `Visual Fios Danificados` e `Visual Fios Intactos`.
  Podem ser modelos provisórios de fio rompido e de fio conectado.
- Opcional: `Tampa Fechada` e `Tampa Aberta` são dois filhos visuais alternados ao
  inspecionar. A primeira versão troca estados; não anima a dobradiça.
- Não coloque o objeto raiz, o script ou o único collider de interação em filhos
  visuais que serão desligados. Use um collider fixo no corpo da caixa.
- Não adicione `ItemPickup` às caixas e não crie tags novas. A interação reconhece o componente.

No gerenciador, preencha o array **Caixas** com **todas** as caixas investigáveis,
inclusive as intactas. Não inclua objetos decorativos sem esse componente.
Referências faltando, IDs duplicados ou lista sem caixas defeituosas bloqueiam o
puzzle e são apontados pela validação.

## 3. Fios no andar superior

- Crie `Pickup_FioDeCobre` no local de coleta, usando um modelo provisório se necessário.
- Adicione `ItemPickup` e um collider não-trigger nesse objeto ou em um filho.
- Arraste `Item_FioDeCobre` para o campo `Item`.
- Ajuste `Quantidade`: para três reparos, pode ser **3 em um único pickup**, ou
  três pickups com **1** cada. O consumo continua sendo uma unidade por caixa.
- Preencha `Chave Coleta` com uma chave exclusiva por pickup, por exemplo
  `carga.eletrica.fios.bancada01`. Ao duplicar, use `bancada02`, `bancada03` etc.
  Essa chave impede que fios já coletados reapareçam ao recarregar a cena.
- Coloque pelo menos tantas unidades acessíveis quanto caixas defeituosas.
  Não esconda os fios atrás de algo que já exija a energia desse mesmo puzzle.

Coletar outro pickup de fio aumenta a pilha existente, mesmo com a grade ocupada.
Sem espaço para uma pilha nova, a coleta falha sem sumir com o objeto.
No inventário, o contador `xN` mostra quantas unidades restam.

## 4. Caixa de força central

- Adicione `CentralEnergia` ao objeto da caixa central no andar superior.
- Arraste o gerenciador para `Puzzle`.
- Coloque um collider não-trigger acessível ao raycast, sem `ItemPickup`.
- Enquanto faltarem reparos, o texto informa o número de caixas pendentes.
- Após os reparos, o texto muda para **[E] Ligar energia central**.

O terminal ainda não recebe UI própria nesta versão. Uma placa/bilhete próximo
pode explicar: “Reparar os circuitos dos corredores antes de acionar a central.
Fiação sobressalente na bancada”. Um terminal sem energia não precisa funcionar
para dar essa pista.

## 5. Luzes, motor e retorno visual

No gerenciador:

- `Objetos Com Energia`: GameObjects de luzes, indicadores ou efeitos que devem
  ficar ativos quando a central for ligada.
- `Objetos Sem Energia`: indicadores/efeitos que devem apagar quando ela ligar.
- **Não inclua o próprio gerenciador, as caixas, a central, o motor inteiro se ele
  deve continuar visível desligado, nem pais desses objetos.** As listas alternam
  GameObjects inteiros; um componente Light desabilitado não é habilitado sozinho.
- `Som Motor`: AudioSource com clip contínuo, **Play On Awake desligado**. O script
  liga o Loop e inicia/para o áudio conforme o estado. Seu GameObject deve estar ativo.
- `Som Partida`: outro AudioSource, com **Play On Awake e Loop desligados**, para o
  som único ao acionar. Deixe esse campo vazio se ainda não houver áudio.
- `Ao Ligar Energia`: evento opcional para outros efeitos. Dispara uma vez ao
  acionar a central, não de novo ao recarregar a cena. Para estados que precisam
  ser restaurados, use as listas acima ou consulte `EnergiaLigada` no consumidor.

Nas caixas, `Som Reparo`, `Ao Inspecionar` e `Ao Reparar` são opcionais. Deixe vazios
para validar a lógica primeiro. Sons devem usar Play On Awake desligado.

Se usar a iluminação vermelha da referência antes do puzzle, trate-a como luz de
emergência independente; ela não precisa significar que a energia principal funciona.
Use sinais físicos de avaria (fio partido, marca de queimado), não só cores/texto.

## 6. Validação e roteiro de teste

No menu do componente `PuzzleEletrica`, execute **Validar configuracao do puzzle**.
Depois dê Play e verifique:

1. A central não liga antes dos reparos.
2. Inspecionar uma caixa intacta não gasta fios.
3. Uma caixa defeituosa sem fio informa o que falta e permanece quebrada.
4. Coletar dois pickups soma as quantidades na mesma pilha.
5. Inspecionar não consome fio; reparar consome exatamente **1**.
6. Apertar E repetidamente numa caixa reparada não consome mais unidades.
7. A última unidade remove a pilha vazia da maleta.
8. A última caixa reparada não liga o motor automaticamente.
9. A central liga os objetos/som apenas depois de todos os reparos e apenas uma vez.
10. Inventário/diário aberto bloqueiam as interações como antes.
11. Saindo e voltando à cena na mesma partida, reparos/energia e pickups coletados
    mantêm o estado. Parando e dando Play novamente, o reset existente inicia tudo de novo.
12. Lanterna, gancho e pé de cabra continuam coletando/equipando como antes; o drag
    e a rotação da maleta também devem continuar funcionando.

Não altere IDs, lista de defeitos ou chaves durante a partida de teste. Após mudar
essas configurações, pare o Play e inicie outra vez. O reset manual de inventário
em uma cena já carregada não substitui reiniciar/recarregar a cena.

Esta entrega cobre lógica, integração e instruções de montagem. Modelagem final,
animação de reparo, partículas e balanceamento da rota ficam para a etapa seguinte.
