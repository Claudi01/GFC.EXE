# Pé de cabra e entrada da elétrica

## Implementado

- A coleta continua usando o `Pickup_PeDeCabra` existente. Não foi alterada a lógica do inventário.
- Com o pé de cabra **equipado**, cada clique esquerdo inicia um golpe: preparação, impacto e retorno. Segurar o botão não repete golpes automaticamente.
- O impacto é verificado pela mira no instante da batida, até **2,2 metros**. Obstáculos bloqueiam o acerto; os colliders do próprio jogador são ignorados.
- O golpe não causa dano a inimigos e não toca áudio.
- Não é possível trocar de equipamento durante o golpe. Abrir inventário/diário ou iniciar transição cancela a animação, restaurando a posição do modelo.
- A porta da elétrica afrouxa nos dois primeiros impactos e cai para dentro, ao lado da passagem, no terceiro. A manivela acompanha a folha. A colisão da porta é desativada na quebra.
- O pé de cabra não é consumido.

## Configuração já aplicada

O prefab `Assets/Prefabs/Player.prefab` contém `AtaquePeDeCabra`, ligado à câmera principal e ao `ControleDeItens`. Não é necessário adicionar novamente o componente nas cenas que usam esse prefab.

Na cena `Assets/Scenes/Parte 1/Aréa de Carga.unity`:

```text
Salas/Eletrica
├── Eletrica Terreno/Parede 1
└── PortaEletrica_Quebravel       (QuebravelCenario + BoxCollider)
    └── VisualPorta_PivoBase
        └── Porta Eletrica       (modelo existente)
            ├── Batente          (desativado nesta instância)
            ├── Porta
            └── Volante
```

`Parede 1` ganhou um recorte octogonal atravessando toda a espessura. Continua sendo uma malha ProBuilder editável e seu MeshCollider está ativo. Foi removido apenas o componente paramétrico de cubo dessa parede: recriar a forma como um cubo preencheria o recorte. As outras paredes não foram alteradas por esta montagem.

A placa larga do batente foi desativada somente na instância; o modelo original não foi modificado. A folha foi ajustada em largura, profundidade e posição para encaixar na abertura.

O progresso usa a chave `carga.eletrica.porta.impactos.v1` no `EstadoMundo`. Os impactos persistem entre cenas na mesma partida e são apagados pelo fluxo de nova partida existente. Não foi implementado menu de continuar.

## Testar na Unity

1. Reabra a cena da área de carga para carregar a versão salva em disco. Se a Unity perguntar sobre alterações externas, recarregue a cena; não sobrescreva essas alterações com uma cópia antiga aberta no Editor.
2. Dê Play e confirme que a porta bloqueia a passagem antes de quebrar.
3. Colete o pé de cabra, espere terminar a troca de equipamento e aproxime-se da porta.
4. Mire na folha e clique três vezes com M1, esperando cada golpe terminar. A tecla E apenas mantém a interação informativa; não quebra a porta.
5. Confira a reação a cada golpe e atravesse a abertura após o terceiro.
6. Teste também golpes no ar, longe da porta, atrás de outra parede e com a lanterna equipada. Nenhum deve danificar a porta.
7. Abra o inventário durante a preparação de um golpe: o modelo deve voltar ao repouso sem bater. Confira também o diário e a troca pelo scroll.
8. Pare e inicie outra partida: a porta deve voltar intacta. A persistência entre cenas não deve ser confundida com iniciar uma nova partida.

## Adicionar outros objetos quebráveis

1. Crie um objeto pai com `QuebravelCenario` e configure `Impactos Para Quebrar`.
2. Coloque a representação visual em um filho, com um pivô adequado, e arraste esse filho para `Visual`.
3. Liste em `Bloqueios` apenas os colliders do obstáculo. Eles devem estar no próprio objeto ou em filhos para o raycast encontrar o componente pai. Não inclua colliders das paredes ao redor.
4. Ajuste os deslocamentos/rotações por impacto e a pose final quebrada. São relativos à pose original do visual.
5. Use uma `Chave Progresso` única por objeto, ou deixe vazia se não precisar persistir. Ajuste também o texto mostrado sem a ferramenta.

A versão atual usa movimento procedural da folha e da ferramenta: não depende de Animator nem de clips externos. Malhas fraturadas, partículas, som e polimento da animação ficam para uma etapa visual posterior.
