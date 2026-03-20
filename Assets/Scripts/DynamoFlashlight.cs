using UnityEngine;

public class DynamoFlashlight : MonoBehaviour
{
    [Header("Referências")]
    public Light spotlight; // O componente de luz da lanterna
    public Animator anim;   // O animador que vai tocar a alavanca

    [Header("Configurações de Energia")]
    public float energiaMaxima = 15f;       // Tempo máximo que a lanterna fica ligada (em segundos)
    public float energiaPorAperto = 4f;     // Quantos segundos de luz o jogador ganha por cada aperto de 'F'
    public float limiteParaFraquejar = 4f;  // Faltando X segundos, a luz começa a perder a força

    [Header("Configurações de Luz")]
    public float intensidadeMaxima = 3f;    // O brilho normal da lanterna

    private float energiaAtual = 0f;

    void Start()
    {
        // O jogo começa com a lanterna zerada (desligada)
        if (spotlight != null)
        {
            spotlight.intensity = 0f;
            spotlight.enabled = false;
        }
    }

    void Update()
    {
        // 1. O Jogador aperta o gatilho ('F')
        if (Input.GetKeyDown(KeyCode.F))
        {
            DarCorda();
        }

        // 2. A bateria descarregando com o tempo
        if (energiaAtual > 0)
        {
            energiaAtual -= Time.deltaTime; // O relógio rodando para trás

            // 3. Controle da intensidade da luz
            if (energiaAtual <= 0)
            {
                // Acabou a energia
                energiaAtual = 0;
                spotlight.intensity = 0f;
                spotlight.enabled = false;
            }
            else if (energiaAtual <= limiteParaFraquejar)
            {
                // A mágica do "apagando aos poucos". 
                // Ele calcula a porcentagem de energia restante e diminui a luz junto.
                float porcentagemRestante = energiaAtual / limiteParaFraquejar;
                spotlight.intensity = intensidadeMaxima * porcentagemRestante;
            }
            else
            {
                // Tem energia sobrando, brilha no máximo!
                spotlight.intensity = intensidadeMaxima;
                spotlight.enabled = true;
            }
        }
    }

    void DarCorda()
    {
        // Toca a animação da alavanca (precisaremos criar esse gatilho "ApertarGatilho" no Animator depois)
        if (anim != null)
        {
            anim.SetTrigger("ApertarGatilho");
        }

        // Adiciona energia
        energiaAtual += energiaPorAperto;

        // Trava para não deixar a energia passar do limite máximo
        if (energiaAtual > energiaMaxima)
        {
            energiaAtual = energiaMaxima;
        }

        // Garante que a luz acenda caso estivesse totalmente apagada
        if (spotlight != null)
        {
            spotlight.enabled = true;
        }
    }
}