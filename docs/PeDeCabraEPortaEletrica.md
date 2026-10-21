# Pé de cabra e entrada da elétrica

## Implementado

- A coleta continua usando o `Pickup_PeDeCabra` existente. Não foi alterada a lógica do inventário.
- Com o pé de cabra **equipado**, cada clique esquerdo inicia um golpe: preparação suave, aceleração em arco, contato, recuo e retorno. Segurar o botão não repete golpes automaticamente.
- Acertar uma superfície segura a ferramenta por 0,045 segundo antes do recuo. Isso não altera o tempo do jogo nem trava a câmera. No ar não há essa pausa: o golpe continua e depois retorna.
- O impacto é verificado pela mira no instante da batida, até **2,2 metros**. Obstáculos bloqueiam o acerto; os colliders do próprio jogador são ignorados.
- O golpe não causa dano a inimigos e não toca áudio.
- Não é possível trocar de equipamento durante o golpe. Abrir inventário/diário ou iniciar transição cancela a animação, restaurando a posição do modelo.
- Um calço metálico enferrujado prende a porta. Os dois primeiros impactos fazem a folha trepidar e soltam o calço progressivamente. No terceiro, o calço se desprende e a porta abre rapidamente para dentro.
- A porta **não cai**: permanece presa à dobradiça lateral, oscila, perde força e fica com um balanço discreto. A manivela acompanha a folha.
- O bloqueio fixo do vão só é removido após a abertura alcançar 65 graus. A colisão da própria folha acompanha a dobradiça e permanece ativa mesmo com a porta aberta.
- O pé de cabra não é consumido.

## Configuração já aplicada

O prefab `Assets/Prefabs/Player.prefab` contém `AtaquePeDeCabra`, ligado à câmera principal e ao `ControleDeItens`. Não é necessário adicionar novamente o componente nas cenas que usam esse prefab.

Na cena `Assets/Scenes/Parte 1/Aréa de Carga.unity`:

```text
Salas/Eletrica
├── Eletrica Terreno/Parede 1
└── PortaEletrica_Quebravel       (QuebravelCenario + PortaEmperrada + bloqueio do vão)
    ├── VisualPorta_Dobradica
    │   ├── Porta Eletrica       (modelo existente: Batente desativado, Porta e Volante)
    │   └── ColisaoFolha
    ├── CalcoMetalico_Emperrando
    ├── DobradicaInferior
    └── DobradicaSuperior
```

`Parede 1` ganhou um recorte octogonal atravessando toda a espessura. Continua sendo uma malha ProBuilder editável e seu MeshCollider está ativo. Foi removido apenas o componente paramétrico de cubo dessa parede: recriar a forma como um cubo preencheria o recorte. As outras paredes não foram alteradas por esta montagem.

A placa larga do batente foi desativada somente na instância; o modelo original não foi modificado. A folha foi ajustada em largura, profundidade e posição para encaixar na abertura.

A folha usa a malha derivada `Assets/Models/Portas/Porta Eleltrica/PortaEletrica_Espessa.asset`: sua traseira foi estendida em 13 cm, dando aproximadamente 20–22 cm de espessura à chapa deformada. A frente, a manivela, o contorno e o eixo de abertura permanecem nas mesmas posições. `ColisaoFolha` acompanha o novo volume; o arquivo Blender original permanece intacto. O volume total de colisão é maior que a espessura da chapa porque inclui sua deformação.

Essa malha derivada também corrige a orientação invertida das faces herdada do modelo original: os triângulos e as normais agora apontam para fora. As laterais são visíveis pelo lado correto, sem precisar de material dupla face. A correção não modifica as posições dos vértices, as UVs ou a colisão.

Na versão com dobradiça, a folha fica na borda interna da abertura para girar para dentro. O recorte ProBuilder não foi alterado novamente. `PortaEmperrada` anima a folha e o calço, e controla o bloqueio do vão. O campo `Visual` e a lista `Bloqueios` do `QuebravelCenario` ficam vazios **nesta porta**, evitando executar também a antiga animação de queda. O componente genérico continua contando/persistindo os impactos e notifica a reação específica.

O progresso usa a chave `carga.eletrica.porta.impactos.v1` no `EstadoMundo`. Os impactos persistem entre cenas na mesma partida e são apagados pelo fluxo de nova partida existente. Não foi implementado menu de continuar.

## Testar na Unity

1. Reabra a cena da área de carga para carregar a versão salva em disco. Se a Unity perguntar sobre alterações externas, recarregue a cena; não sobrescreva essas alterações com uma cópia antiga aberta no Editor.
2. Dê Play e confirme que a porta bloqueia a passagem enquanto está emperrada.
3. Colete o pé de cabra, espere terminar a troca de equipamento e aproxime-se da porta.
4. Mire na folha e clique três vezes com M1, esperando cada golpe terminar. A tecla E apenas mantém a interação informativa; não quebra a porta.
5. Confira a trepidação e o calço soltando a cada golpe. No terceiro, a porta deve abrir para dentro, balançar sem sair da dobradiça e liberar a entrada. A folha aberta deve continuar sólida.
6. Teste também golpes no ar, longe da porta, atrás de outra parede e com a lanterna equipada. Nenhum deve danificar a porta.
7. Abra o inventário durante a preparação de um golpe: o modelo deve voltar ao repouso sem bater. Confira também o diário e a troca pelo scroll.
8. Pare e inicie outra partida: a porta deve voltar fechada e com o calço. A persistência entre cenas não deve ser confundida com iniciar uma nova partida. Ao retornar à cena na mesma partida, uma porta já destravada volta aberta, não caída.

## Adicionar outros objetos quebráveis

1. Crie um objeto pai com `QuebravelCenario` e configure `Impactos Para Quebrar`.
2. Coloque a representação visual em um filho, com um pivô adequado, e arraste esse filho para `Visual`.
3. Liste em `Bloqueios` apenas os colliders do obstáculo. Eles devem estar no próprio objeto ou em filhos para o raycast encontrar o componente pai. Não inclua colliders das paredes ao redor.
4. Ajuste os deslocamentos/rotações por impacto e a pose final quebrada. São relativos à pose original do visual.
5. Use uma `Chave Progresso` única por objeto, ou deixe vazia se não precisar persistir. Ajuste também o texto mostrado sem a ferramenta.

Para outra porta emperrada, adicione também `PortaEmperrada`, ligue a resistência, o pivô lateral, o calço e o bloqueio fixo. A folha deve ter um collider filho do pivô. Nesse caso deixe a animação genérica (`Visual`/`Bloqueios` do `QuebravelCenario`) vazia, e use uma chave de progresso exclusiva.

A versão atual usa movimento procedural da folha e da ferramenta: não depende de Animator, clips externos ou HingeJoint/Rigidbody. Os tempos do golpe ficam no componente `AtaquePeDeCabra` do prefab Player. A abertura, a intensidade e o amortecimento do balanço ficam em `PortaEmperrada`. Som, texturas definitivas e partículas continuam fora desta etapa.
