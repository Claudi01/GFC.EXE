# Documentação do projeto

Esta pasta reúne a documentação da elétrica em ordem de uso. O guia atual é
`PuzzleEletrica_AtivacaoConsole.md`; os documentos de versões anteriores foram
mantidos como histórico para preservar decisões e instruções antigas sem
confundi-las com o funcionamento vigente.

## Leitura recomendada

1. **`PuzzleEletrica_AtivacaoConsole.md` — estado atual.** Fluxo, ferramentas,
   configuração registrada na cena, ordem dos armários, testes e pendências.
2. **`MontagemEletrica.md` — ferramenta de montagem.** Como funciona o comando
   de Editor que criou a população física original de armários e canos. Não é
   necessário executá-lo novamente se o grupo de montagem já existe.
3. **`PuzzleEletrica.md` — histórico da primeira versão.** Defeitos fixos e
   reparo somente com cobre; não use como instrução para configurar a versão
   atual.
4. **`PuzzleEletrica_EstadoEPlanejamento.md` — histórico de planejamento.**
   Registra propostas anteriores à implementação atual; itens marcados como
   pendentes ali podem já ter sido implementados depois.

## Fonte de verdade

Para a entrada da sala, consulte também [Pé de cabra e porta da elétrica](PeDeCabraEPortaEletrica.md): golpe com M1, porta quebrável, configuração da cena e roteiro de testes.

- A cena `Assets/Scenes/Parte 1/Aréa de Carga.unity` é a referência para a
  hierarquia, os nomes, os IDs e a ordem serializada atual dos armários.
- Os scripts em `Assets/Scripts` definem as regras executadas pelo jogo.
- O guia de estado atual resume esses dados, mas deve ser revisto sempre que a
  cena ou os scripts da elétrica forem alterados.

As tabelas de armários no guia atual refletem a cena salva em disco, incluindo
as alterações locais de organização ainda não commitadas.
