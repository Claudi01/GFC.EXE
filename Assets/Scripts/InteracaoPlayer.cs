using UnityEngine;

public class InteracaoPlayer : MonoBehaviour
{
    [Header("Configurações do Laser")]
    public float distanciaAlcancada = 5f; // Aumentei um pouco o alcance por segurança

    void Update()
    {
        // Quando o jogador apertar E
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Passo 1: O botão E foi apertado!"); // Avisa que o botão funciona
            TentarInteragir();
        }
    }

    void TentarInteragir()
    {
        // Cria o laser saindo do objeto onde este script está e indo para frente
        Ray laser = new Ray(transform.position, transform.forward);
        RaycastHit acerto; 

        // Desenha a linha vermelha na aba Scene por 2 segundos
        Debug.DrawRay(transform.position, transform.forward * distanciaAlcancada, Color.red, 2f);

        // Dispara o laser físico! Se bater em algo...
        if (Physics.Raycast(laser, out acerto, distanciaAlcancada))
        {
            // Avisa no console o nome exato da coisa em que o laser bateu
            Debug.Log("Passo 2: O laser bateu no objeto chamado -> " + acerto.collider.gameObject.name); 

            // Checa se o objeto tem a etiqueta (Tag) "Diario"
            if (acerto.collider.CompareTag("Diario"))
            {
                Debug.Log("Passo 3: A tag está correta! O alvo é o Diário.");
                
                // Pega o script do diário que está dentro daquele objeto
                MecanicaDiario diario = acerto.collider.GetComponent<MecanicaDiario>();
                if (diario != null)
                {
                    Debug.Log("Passo 4: Script MecanicaDiario encontrado. Acionando a UI!");
                    diario.Interagir();
                }
                else
                {
                    Debug.LogWarning("ERRO: O objeto acertado tem a tag 'Diario', mas NÃO tem o script 'MecanicaDiario' anexado nele!");
                }
            }
            else 
            {
                Debug.Log("Ops: O objeto acertado não tem a tag 'Diario'. A tag dele é: " + acerto.collider.tag);
            }
        }
        else
        {
            Debug.Log("Falha: O laser foi disparado, mas não bateu em nada físico. Chegue mais perto ou ajuste a mira.");
        }
    }
}