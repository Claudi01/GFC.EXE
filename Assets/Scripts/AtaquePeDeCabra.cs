using UnityEngine;

/// <summary>Golpe de ferramenta: so atinge QuebravelCenario, sem dano a inimigos.</summary>
public class AtaquePeDeCabra : MonoBehaviour
{
    [SerializeField] private ControleDeItens controleItens;
    [SerializeField] private Camera cameraJogador;
    [Header("Sons de impacto por superficie")]
    [SerializeField] private AudioClip[] sonsChapaMetal;
    [SerializeField] private AudioClip[] sonsFerroMacico;
    [SerializeField] private AudioClip[] sonsPortaEnferrujada;
    [SerializeField] private AudioClip[] sonsMadeira;
    [SerializeField] private AudioClip[] sonsConcreto;
    [Header("Impacto")]
    [SerializeField, Min(0.1f)] private float alcance = 2.2f;
    [SerializeField] private LayerMask camadasImpacto = ~0;
    [Header("Animacao (deslocamentos nos eixos da camera)")]
    [SerializeField, Min(0.01f)] private float tempoPreparo = 0.22f;
    [SerializeField, Min(0.01f)] private float tempoGolpe = 0.12f;
    [SerializeField, Min(0.01f)] private float tempoRetorno = 0.34f;
    [SerializeField] private Vector3 posicaoPreparo = new Vector3(0.04f, -0.02f, 0.12f);
    [SerializeField] private Vector3 rotacaoPreparo = new Vector3(-8f, 0f, -12f);
    [SerializeField] private Vector3 posicaoImpacto = new Vector3(-0.1f, 0.12f, 0.15f);
    [SerializeField] private Vector3 rotacaoImpacto = new Vector3(50f, 0f, 25f);
    [SerializeField] private Vector3 arcoGolpe = new Vector3(-0.03f, 0.045f, 0f);
    [Header("Peso do impacto (somente a ferramenta, sem congelar o jogo)")]
    [SerializeField, Min(0f)] private float pausaImpacto = 0.045f;
    [SerializeField, Min(0.01f)] private float tempoRecuo = 0.1f;
    [SerializeField] private Vector3 deslocamentoRecuo = new Vector3(0.025f, 0.015f, -0.09f);
    [SerializeField] private Vector3 rotacaoRecuo = new Vector3(-13f, 3f, -6f);

    private Transform visual;
    private Vector3 posicaoRepouso;
    private Quaternion rotacaoRepouso;
    private float tempo;
    private bool acertou;
    private bool atingiuSuperficie;
    private bool golpeEmAndamento;
    private readonly int[] ultimoSomPorSuperficie = { -1, -1, -1, -1, -1 };
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
        atingiuSuperficie = false;
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
            AplicarPose(Vector3.zero, Vector3.zero, posicaoPreparo, rotacaoPreparo, Suavizar(tempo / tempoPreparo));
        else if (tempo < instanteImpacto)
        {
            // Aceleracao ate o contato; o arco evita uma simples translacao reta.
            float t = Mathf.Clamp01((tempo - tempoPreparo) / tempoGolpe);
            AplicarPose(posicaoPreparo, rotacaoPreparo, posicaoImpacto, rotacaoImpacto,
                t * t, arcoGolpe * Mathf.Sin(t * Mathf.PI));
        }
        else
        {
            if (!acertou)
            {
                acertou = true;
                atingiuSuperficie = AplicarImpacto();
            }

            float aposImpacto = tempo - instanteImpacto;
            float pausa = atingiuSuperficie ? pausaImpacto : 0f;
            Vector3 posFinal = posicaoImpacto + (atingiuSuperficie
                ? deslocamentoRecuo : new Vector3(-0.04f, -0.025f, 0.10f));
            Vector3 rotFinal = rotacaoImpacto + (atingiuSuperficie
                ? rotacaoRecuo : new Vector3(10f, 0f, 4f));
            if (aposImpacto < pausa)
                AplicarPose(posicaoImpacto, rotacaoImpacto, posicaoImpacto, rotacaoImpacto, 0f);
            else if (aposImpacto < pausa + tempoRecuo)
            {
                float t = Mathf.Clamp01((aposImpacto - pausa) / tempoRecuo);
                AplicarPose(posicaoImpacto, rotacaoImpacto, posFinal, rotFinal, 1f - Mathf.Pow(1f - t, 3f));
            }
            else
            {
                AplicarPose(posFinal, rotFinal, Vector3.zero, Vector3.zero,
                    Suavizar((aposImpacto - pausa - tempoRecuo) / tempoRetorno));
                if (aposImpacto >= pausa + tempoRecuo + tempoRetorno) CancelarGolpe();
            }
        }
    }

    private static float Suavizar(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    private void AplicarPose(Vector3 inicio, Vector3 rotInicio, Vector3 fim, Vector3 rotFim, float t, Vector3 arco = default)
    {
        visual.localPosition = posicaoRepouso + Vector3.Lerp(inicio, fim, t) + arco;
        visual.localRotation = Quaternion.Slerp(Quaternion.Euler(rotInicio), Quaternion.Euler(rotFim), t) * rotacaoRepouso;
    }

    private bool AplicarImpacto()
    {
        // Ordena pela distancia e ignora apenas os colliders do proprio jogador.
        // Uma parede/objeto nao quebravel continua bloqueando o golpe.
        RaycastHit[] acertos = Physics.RaycastAll(cameraJogador.transform.position,
            cameraJogador.transform.forward, alcance, camadasImpacto, QueryTriggerInteraction.Ignore);
        System.Array.Sort(acertos, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit acerto in acertos)
        {
            if (acerto.collider.transform.IsChildOf(transform)) continue;
            TocarSomDeImpacto(acerto);
            QuebravelCenario alvo = acerto.collider.GetComponentInParent<QuebravelCenario>();
            if (alvo != null) alvo.ReceberImpacto();
            return true;
        }
        return false;
    }

    private void TocarSomDeImpacto(RaycastHit hit)
    {
        AudioClip[] variacoes = ObterSons(ClassificarSuperficie(hit.collider));
        if (variacoes == null || variacoes.Length == 0) return;

        int tipo = (int)ClassificarSuperficie(hit.collider);
        int indice = Random.Range(0, variacoes.Length);
        if (variacoes.Length > 1 && indice == ultimoSomPorSuperficie[tipo])
            indice = (indice + Random.Range(1, variacoes.Length)) % variacoes.Length;
        ultimoSomPorSuperficie[tipo] = indice;

        AudioClip clip = variacoes[indice];
        if (clip != null) AudioSource.PlayClipAtPoint(clip, hit.point, 0.7f);
    }

    private enum SuperficieImpacto
    {
        ChapaMetal = 0,
        FerroMacico = 1,
        PortaEnferrujada = 2,
        Madeira = 3,
        Concreto = 4
    }

    private SuperficieImpacto ClassificarSuperficie(Collider collider)
    {
        if (collider.GetComponentInParent<PortaEmperrada>() != null)
            return SuperficieImpacto.PortaEnferrujada;

        string nomes = string.Empty;
        Transform atual = collider.transform;
        while (atual != null)
        {
            nomes += " " + atual.name.ToLowerInvariant();
            atual = atual.parent;
        }

        if (nomes.Contains("enferruj") || nomes.Contains("rust"))
            return SuperficieImpacto.PortaEnferrujada;
        if (nomes.Contains("madeira") || nomes.Contains("wood") || nomes.Contains("bote") || nomes.Contains("boat"))
            return SuperficieImpacto.Madeira;
        if (nomes.Contains("concreto") || nomes.Contains("concrete") || nomes.Contains("cimento") ||
            nomes.Contains("pedra") || nomes.Contains("stone"))
            return SuperficieImpacto.Concreto;
        if (nomes.Contains("chapa") || nomes.Contains("metal") || nomes.Contains("sheet") ||
            nomes.Contains("plate") || nomes.Contains("porta"))
            return SuperficieImpacto.ChapaMetal;

        // A maioria dos alvos quebraveis do jogo e metal/ferro.
        return SuperficieImpacto.FerroMacico;
    }

    private AudioClip[] ObterSons(SuperficieImpacto superficie)
    {
        switch (superficie)
        {
            case SuperficieImpacto.ChapaMetal: return sonsChapaMetal;
            case SuperficieImpacto.FerroMacico: return sonsFerroMacico;
            case SuperficieImpacto.PortaEnferrujada: return sonsPortaEnferrujada;
            case SuperficieImpacto.Madeira: return sonsMadeira;
            case SuperficieImpacto.Concreto: return sonsConcreto;
            default: return null;
        }
    }    private void CancelarGolpe()
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
        tempoRecuo = Mathf.Max(0.01f, tempoRecuo);
        pausaImpacto = Mathf.Max(0f, pausaImpacto);
    }
}
