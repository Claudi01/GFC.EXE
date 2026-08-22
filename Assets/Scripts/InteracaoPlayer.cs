using UnityEngine;
using TMPro;

public class InteracaoPlayer : MonoBehaviour
{
    [Header("Configuracoes")]
    public float distanciaAlcancada = 5f;

    [Header("Referencias")]
    public ControleDeItens controleItens;
    public GameObject objetoTextoInteracao;
    public TextMeshProUGUI componenteTexto;

    private void Awake()
    {
        if (controleItens == null)
            controleItens = FindFirstObjectByType<ControleDeItens>();
    }

    private void Update()
    {
        if (GameplayState.Instancia != null && GameplayState.Instancia.EstaBloqueado)
        {
            EsconderTexto();
            return;
        }

        VerificarMira();

        if (Input.GetKeyDown(KeyCode.E))
            TentarInteragir();
    }

    private bool TentarRaycast(out RaycastHit acerto)
    {
        Ray laser = new Ray(transform.position, transform.forward);
        return Physics.Raycast(laser, out acerto, distanciaAlcancada);
    }

    private void VerificarMira()
    {
        RaycastHit acerto;
        if (!TentarRaycast(out acerto))
        {
            EsconderTexto();
            return;
        }

        ItemPickup pickup = acerto.collider.GetComponentInParent<ItemPickup>();
        if (pickup != null)
        {
            if (pickup.JaPossui)
                MostrarTexto("Ja possuo " + pickup.Nome);
            else
                MostrarTexto("Pegar " + pickup.Nome);
        }
        else if (acerto.collider.CompareTag("Diario"))
        {
            MostrarTexto("Ler");
        }
        else if (acerto.collider.CompareTag("PontoEscalada"))
        {
            if (controleItens != null && controleItens.GanchoEquipado())
                MostrarTexto("Usar gancho para escalar");
            else
                MostrarTexto("Esta muito alto... Preciso de algo.");
        }
        else if (acerto.collider.CompareTag("Portao"))
        {
            MostrarTexto("Nao posso abandonar a missao agora.");
        }
        else
        {
            EsconderTexto();
        }
    }

    private void TentarInteragir()
    {
        RaycastHit acerto;
        if (!TentarRaycast(out acerto))
            return;

        ItemPickup pickup = acerto.collider.GetComponentInParent<ItemPickup>();
        if (pickup != null)
        {
            ColetarPickup(pickup);
        }
        else if (acerto.collider.CompareTag("Diario"))
        {
            MecanicaDiario diario = acerto.collider.GetComponentInParent<MecanicaDiario>();
            if (diario != null)
                diario.Interagir();

            EsconderTexto();
        }
        else if (acerto.collider.CompareTag("PontoEscalada"))
        {
            if (controleItens != null && controleItens.GanchoEquipado())
            {
                MecanicaEscalada escalada = acerto.collider.GetComponentInParent<MecanicaEscalada>();
                if (escalada != null)
                    escalada.IniciarEscalada();

                EsconderTexto();
            }
        }
    }

    private void ColetarPickup(ItemPickup pickup)
    {
        if (pickup.JaPossui)
        {
            MostrarTexto("Ja possuo " + pickup.Nome);
            return;
        }

        if (!pickup.TentarPegar())
        {
            MostrarTexto("Inventario cheio");
            return;
        }

        if (controleItens != null)
            controleItens.EquiparItem(pickup.Id);

        Destroy(pickup.gameObject);
        EsconderTexto();
    }

    private void MostrarTexto(string mensagem)
    {
        if (componenteTexto != null)
        {
            componenteTexto.text = mensagem;
            componenteTexto.gameObject.SetActive(true);
        }

        if (objetoTextoInteracao != null)
            objetoTextoInteracao.SetActive(true);
    }

    public void EsconderTexto()
    {
        if (componenteTexto != null)
            componenteTexto.gameObject.SetActive(false);

        if (objetoTextoInteracao != null)
            objetoTextoInteracao.SetActive(false);
    }
}
