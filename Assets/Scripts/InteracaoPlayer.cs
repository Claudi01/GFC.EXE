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

    void Awake()
    {
        // A referencia era configurada somente em uma das cenas originais.
        if (controleItens == null) controleItens = FindFirstObjectByType<ControleDeItens>();
    }

    void Update()
    {
        if (SistemaInventario.Instancia != null && SistemaInventario.Instancia.Aberto)
        {
            EsconderTexto();
            return;
        }

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
            else if (acerto.collider.GetComponentInParent<ItemPickup>() != null)
            {
                ItemPickup pickup = acerto.collider.GetComponentInParent<ItemPickup>();
                if (pickup.item != null) MostrarTexto("Pegar " + pickup.item.nome);
                else EsconderTexto();
            }
            else if (acerto.collider.CompareTag("PontoEscalada"))
            {
                if (controleItens != null && controleItens.GanchoEquipado()) MostrarTexto("Usar gancho para escalar");
                else MostrarTexto("Está muito alto... Preciso de algo.");
            }
            else if (acerto.collider.CompareTag("Portao"))
            {
                MostrarTexto("Não posso abandonar a missão agora.");
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
                MecanicaDiario diario = acerto.collider.GetComponentInParent<MecanicaDiario>();
                if (diario != null) diario.Interagir();
                EsconderTexto();
            }
            else if (acerto.collider.CompareTag("Gancho"))
            {
                if (controleItens != null)
                {
                    if (controleItens.TryPegarGancho())
                    {
                        Destroy(acerto.collider.gameObject);
                        EsconderTexto();
                    }
                    else MostrarTexto("Inventário cheio");
                }
            }
            else if (acerto.collider.GetComponentInParent<ItemPickup>() != null)
            {
                ItemPickup pickup = acerto.collider.GetComponentInParent<ItemPickup>();
                if (pickup.TentarPegar())
                {
                    Destroy(pickup.gameObject);
                    EsconderTexto();
                }
                else MostrarTexto("InventÃ¡rio cheio");
            }
            else if (acerto.collider.CompareTag("PontoEscalada"))
            {
                if (controleItens != null && controleItens.GanchoEquipado())
                {
                    MecanicaEscalada escalada = acerto.collider.GetComponentInParent<MecanicaEscalada>();
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
            if (objetoTextoInteracao != null) objetoTextoInteracao.SetActive(true);
        }
    }

    // Agora é PUBLIC para o Diário poder sumir com o texto à força
    public void EsconderTexto()
    {
        if (objetoTextoInteracao != null)
        {
            objetoTextoInteracao.SetActive(false);
        }
    }
}
