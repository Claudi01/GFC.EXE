using UnityEngine;
using TMPro;

public class InteracaoPlayer : MonoBehaviour
{
    [Header("Configurações")]
    public float distanciaAlcancada = 5f;

    [Header("Referências")]
    public ControleDeItens controleItens;
    public GameObject objetoTextoInteracao;
    public TextMeshProUGUI componenteTexto;

    void Update()
    {
        VerificarMira();

        if (Input.GetKeyDown(KeyCode.E))
        {
            TentarInteragir();
        }
    }

    void VerificarMira()
    {
        Ray laser = new Ray(transform.position, transform.forward);
        RaycastHit acerto;

        if (Physics.Raycast(laser, out acerto, distanciaAlcancada))
        {
            if (acerto.collider.CompareTag("Diario")) MostrarTexto("Ler");
            else if (acerto.collider.CompareTag("Gancho")) MostrarTexto("Pegar Gancho");
            else if (acerto.collider.CompareTag("PontoEscalada"))
            {
                if (controleItens != null && controleItens.GanchoEquipado())
                {
                    MostrarTexto("Usar gancho para escalar");
                }
                else
                {
                    MostrarTexto("Está muito alto... Preciso de algo.");
                }
            }
            else EsconderTexto();
        }
        else EsconderTexto();
    }

    void TentarInteragir()
    {
        Ray laser = new Ray(transform.position, transform.forward);
        RaycastHit acerto;

        if (Physics.Raycast(laser, out acerto, distanciaAlcancada))
        {
            if (acerto.collider.CompareTag("Diario"))
            {
                MecanicaDiario diario = acerto.collider.GetComponent<MecanicaDiario>();
                if (diario != null) diario.Interagir();
                EsconderTexto();
            }
            else if (acerto.collider.CompareTag("Gancho"))
            {
                if (controleItens != null)
                {
                    controleItens.PegarGancho();
                    Destroy(acerto.collider.gameObject);
                    EsconderTexto();
                }
            }
            else if (acerto.collider.CompareTag("PontoEscalada"))
            {
                if (controleItens != null && controleItens.GanchoEquipado())
                {
                    MecanicaEscalada escalada = acerto.collider.GetComponent<MecanicaEscalada>();
                    if (escalada != null) escalada.IniciarEscalada();
                    EsconderTexto();
                }
            }
        }
    }

    void MostrarTexto(string mensagem)
    {
        if (componenteTexto != null)
        {
            componenteTexto.text = mensagem;
            objetoTextoInteracao.SetActive(true);
        }
    }

    void EsconderTexto()
    {
        if (objetoTextoInteracao != null)
        {
            objetoTextoInteracao.SetActive(false);
        }
    }
}