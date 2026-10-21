using UnityEngine;

/// <summary>Golpe de ferramenta: so atinge QuebravelCenario, sem dano a inimigos.</summary>
public class AtaquePeDeCabra : MonoBehaviour
{
    [SerializeField] private ControleDeItens controleItens;
    [SerializeField] private Camera cameraJogador;
    [Header("Impacto")]
    [SerializeField, Min(0.1f)] private float alcance = 2.2f;
    [SerializeField] private LayerMask camadasImpacto = ~0;
    [Header("Animacao (deslocamentos nos eixos da camera)")]
    [SerializeField, Min(0.01f)] private float tempoPreparo = 0.18f;
    [SerializeField, Min(0.01f)] private float tempoGolpe = 0.12f;
    [SerializeField, Min(0.01f)] private float tempoRetorno = 0.28f;
    [SerializeField] private Vector3 posicaoPreparo = new Vector3(0.04f, -0.02f, 0.12f);
    [SerializeField] private Vector3 rotacaoPreparo = new Vector3(-8f, 0f, -12f);
    [SerializeField] private Vector3 posicaoImpacto = new Vector3(-0.1f, 0.12f, 0.15f);
    [SerializeField] private Vector3 rotacaoImpacto = new Vector3(50f, 0f, 25f);

    private Transform visual;
    private Vector3 posicaoRepouso;
    private Quaternion rotacaoRepouso;
    private float tempo;
    private bool acertou;
    private bool golpeEmAndamento;
    public bool GolpeEmAndamento => golpeEmAndamento;

    private void Awake()
    {
        if (controleItens == null) controleItens = GetComponent<ControleDeItens>();
        if (cameraJogador == null)
        {
            var controlador = GetComponent<FirstPersonController>();
            if (controlador != null) cameraJogador = controlador.playerCamera;
        }
    }

    // Depois dos Updates que abrem inventario/diario ou iniciam uma transicao.
    private void LateUpdate()
    {
        if (GameplayState.Instancia != null && GameplayState.Instancia.EstaBloqueado)
        {
            CancelarGolpe();
            return;
        }

        if (!GolpeEmAndamento)
        {
            if (!Input.GetMouseButtonDown(0) || !TentarGolpear()) return;
        }

        AvancarAnimacao(Time.deltaTime);
    }

    public bool TentarGolpear()
    {
        if (!isActiveAndEnabled || GolpeEmAndamento || Time.timeScale <= 0f ||
            cameraJogador == null || controleItens == null ||
            !controleItens.TentarIniciarUso("pe_de_cabra", out visual)) return false;

        posicaoRepouso = visual.localPosition;
        rotacaoRepouso = visual.localRotation;
        tempo = 0f;
        acertou = false;
        golpeEmAndamento = true;
        return true;
    }

    private void AvancarAnimacao(float delta)
    {
        if (visual == null || !visual.gameObject.activeInHierarchy || controleItens == null ||
            !controleItens.EstaEquipado("pe_de_cabra") ||
            (GameplayState.Instancia != null && GameplayState.Instancia.EstaBloqueado))
        {
            CancelarGolpe();
            return;
        }

        tempo += delta;
        float instanteImpacto = tempoPreparo + tempoGolpe;
        if (tempo < tempoPreparo)
            AplicarPose(Vector3.zero, Vector3.zero, posicaoPreparo, rotacaoPreparo, tempo / tempoPreparo);
        else if (tempo < instanteImpacto)
            AplicarPose(posicaoPreparo, rotacaoPreparo, posicaoImpacto, rotacaoImpacto,
                (tempo - tempoPreparo) / tempoGolpe);
        else
        {
            if (!acertou)
            {
                acertou = true;
                AplicarImpacto();
            }

            AplicarPose(posicaoImpacto, rotacaoImpacto, Vector3.zero, Vector3.zero,
                (tempo - instanteImpacto) / tempoRetorno);
            if (tempo >= instanteImpacto + tempoRetorno) CancelarGolpe();
        }
    }

    private void AplicarPose(Vector3 inicio, Vector3 rotInicio, Vector3 fim, Vector3 rotFim, float t)
    {
        t = Mathf.SmoothStep(0f, 1f, t);
        visual.localPosition = posicaoRepouso + Vector3.Lerp(inicio, fim, t);
        visual.localRotation = Quaternion.Slerp(Quaternion.Euler(rotInicio), Quaternion.Euler(rotFim), t) * rotacaoRepouso;
    }

    private void AplicarImpacto()
    {
        // Ordena pela distancia e ignora apenas os colliders do proprio jogador.
        // Uma parede/objeto nao quebravel continua bloqueando o golpe.
        RaycastHit[] acertos = Physics.RaycastAll(cameraJogador.transform.position,
            cameraJogador.transform.forward, alcance, camadasImpacto, QueryTriggerInteraction.Ignore);
        System.Array.Sort(acertos, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit acerto in acertos)
        {
            if (acerto.collider.transform.IsChildOf(transform)) continue;
            QuebravelCenario alvo = acerto.collider.GetComponentInParent<QuebravelCenario>();
            if (alvo != null) alvo.ReceberImpacto();
            break;
        }
    }

    private void CancelarGolpe()
    {
        if (!golpeEmAndamento) return;
        if (visual != null)
        {
            visual.localPosition = posicaoRepouso;
            visual.localRotation = rotacaoRepouso;
        }
        visual = null;
        golpeEmAndamento = false;
        if (controleItens != null) controleItens.EncerrarUso();
    }

    private void OnDisable() => CancelarGolpe();

    private void OnValidate()
    {
        tempoPreparo = Mathf.Max(0.01f, tempoPreparo);
        tempoGolpe = Mathf.Max(0.01f, tempoGolpe);
        tempoRetorno = Mathf.Max(0.01f, tempoRetorno);
    }
}
