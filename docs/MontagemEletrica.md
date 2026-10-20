# Popular a elétrica com os armários e canos existentes

A ferramenta fica em **Tools > GFC > Eletrica**. Ela só executa quando você
escolhe um comando: não altera a cena ao compilar ou ao dar Play.

## Aplicar na cena aberta

1. Fora do Play Mode, abra **Aréa de Carga**.
2. Use **Validar montagem antes de aplicar**. Leia o resultado no Console.
3. Use **Popular armarios e canos**.
4. Confira o resultado e salve a cena com **Ctrl+S**. Um **Ctrl+Z** desfaz toda a
   montagem; o Undo também reativa os cubos, nas posições originais.

Se o menu ainda não apareceu, aguarde a compilação ou use **Assets > Refresh**.
Não é necessário fechar a Unity. A ferramenta atua sobre a cena atualmente
carregada, incluindo posições editadas e ainda não salvas, sem substituí-la por
uma cópia do arquivo no disco.

## O que ela faz

- Usa apenas os **Cube (1) até Cube (10) ativos na raiz** como marcadores.
- Usa o centro da **malha visível**, pois os pivôs atuais do ProBuilder estão
  deslocados. Não usa o Transform como posição visual do armário.
- Duplica `LARGE_ELECTRICAL_CABINET (2)` da montagem existente, preservando tamanho
  e materiais. Orienta as portas para os corredores e deixa o fundo a cerca de
  2 cm da superfície da parede.
- Coloca colisão no corpo de cada armário novo.
- Reaproveita as malhas `Pipe_T_Branch` e `Pipe_Elbow (2)` já usadas na instalação.
  Preserva o diâmetro, divide os trechos retos em peças de até aproximadamente
  2 m e usa as curvas existentes nas mudanças de direção.
- Liga a primeira fileira à travessia existente. A segunda e a terceira recebem
  um coletor em L, conectado à primeira por um trecho que atravessa a divisória
  na altura da tubulação. Não recorta nem altera a geometria das paredes.
- Completa as derivações dos outros três armários antigos do térreo; o armário
  que já tinha uma descida continua usando sua ligação original.
- Cria o grupo `Eletrica_Armarios_Adicionados`, com `Armarios` e `Tubulacao`.
  Os cubos originais ficam **na raiz, desativados**, sem mudar de posição ou de
  pai. Eles não são apagados nem colocados dentro do grupo novo.
- Não modifica os armários/canos antigos e não atribui scripts ou IDs do puzzle
  aos novos armários. Essa configuração será feita separadamente.

O **Cube sem número**, atualmente sem malha, fica intocado. Os demais objetos
chamados Cube dentro de `Eletrica Terreno` também não são marcadores e não são
alterados.

## Limites e proteções

A montagem atende às **três fileiras atuais**, não é um gerador genérico de
qualquer corredor:

- `Cube (1)` a `(3)`: primeiro lado da divisória;
- `Cube (4)` a `(6)`: lado oposto;
- `Cube (7)` a `(10)`: parede perpendicular, formando o L.

Se faltarem referências, paredes, alinhamento ou espaço para os encaixes, a
ferramenta interrompe a operação. Se algo falhar durante a criação, ela desfaz
suas próprias mudanças. Renomear/mover os modelos de referência ou inverter a
ordem dos marcadores pode exigir ajustar a ferramenta.

Executar novamente com `Eletrica_Armarios_Adicionados` já presente é bloqueado,
para não duplicar tudo. Para refazer imediatamente, desfaça a montagem primeiro.
Se já houver outras edições posteriores, não desfaça indiscriminadamente:
preserve essas edições e revise o grupo manualmente.

Depois de salvar, reiniciar o Play não desfaz esta montagem: são objetos da cena,
não itens gerados em runtime. Nenhum código desta ferramenta vai para o build,
pois ela fica em `Assets/Editor`.
