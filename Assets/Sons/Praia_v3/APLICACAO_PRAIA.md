# Áudio da Praia — percurso

Esta versão usa gravações CC0 editadas e camadas separadas. As fontes e licenças estão no catálogo e no documento de créditos desta pasta.

1. Na floresta, chuva nas folhas e vento nas árvores tocam em volume baixo.
2. A zona Audio_Praia/Cabana - chuva abafada reduz essas camadas e filtra os agudos ao entrar na cobertura da Tent.
3. Abrir o diário não dispara a quebra. O primeiro comando para fechar dispara a madeira do bote quebrando, a partir da posição dos destroços, sem acrescentar espera ao fechamento.
4. A pintura real de areia do Terrain (NewLayer 1 / dirt_lighted_down) muda os passos e permite a entrada dos pingos no casco.
5. O mar agitado aumenta ao se aproximar dos destroços; fica separado da chuva e dos pingos no ferro.
6. Ao confirmar o gancho, a tela escurece. Com a tela preta, toca o arremesso de corda/gancho, e a transição continua.

As referências e os volumes foram aplicados à instância do Player na cena Praia. Os prefabs compartilhados e a cena Aréa de Carga não receberam esses novos clipes. O ambiente da Praia é interrompido ao carregar outra cena.

Para ajustar os níveis, abra a Praia, selecione Player na Hierarchy e veja AudioAmbientePraia e AudioMovimentoPlayer. A zona da cabana pode ser selecionada em Audio_Praia. O clipe da madeira fica no MecanicaDiario; o arremesso fica no TransicaoCena do Gatilho_Escalada.

Os lotes antigos continuam no projeto para referência, mas os sons deste percurso ficam nesta pasta Praia_v3.
