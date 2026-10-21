using UnityEngine;

/// <summary>Obstaculo quebravel reutilizavel; nao faz parte de um sistema de combate.</summary>
public class QuebravelCenario : InteragivelCenario
{
    [SerializeField, Min(1)] private int impactosParaQuebrar = 3;
    [Tooltip("Unica por objeto. Vazia = nao persiste ao trocar de cena.")]
    [SerializeField] private string chaveProgresso;
    [SerializeField] private string textoSemFerramenta = "Preciso de um pe de cabra para quebrar isto.";
    [SerializeField] private string textoComFerramenta = "Equipe o pe de cabra e bata com M1";
    [Tooltip("Pivo do visual. Para uma porta caindo, coloque-o na base da folha.")]
    [SerializeField] private Transform visual;
    [Tooltip("Somente os colliders que bloqueiam a passagem, nunca os da parede.")]
    [SerializeField] private Collider[] bloqueios = new Collider[0];
    [Header("Dano visual local")]
    [SerializeField] private Vector3 deslocamentoPorImpacto = new Vector3(0f, 0f, 0.035f);
    [SerializeField] private Vector3 rotacaoPorImpacto = new Vector3(3f, 0f, -1.5f);
    [SerializeField] private Vector3 deslocamentoQuebrado = new Vector3(0f, 0.03f, 0.15f);
    [SerializeField] private Vector3 rotacaoQuebrado = new Vector3(90f, 0f, 0f);
    [SerializeField, Min(0.01f)] private float duracaoReacao = 0.16f;
    [SerializeField, Min(0.01f)] private float duracaoQueda = 0.5f;

    private int impactos;
    private Vector3 posicaoInicial, posicaoOrigem, posicaoAlvo;
    private Quaternion rotacaoInicial, rotacaoOrigem, rotacaoAlvo;
    private float tempoReacao, duracaoAtual;
    private bool animando;
    public int ImpactosRecebidos => impactos;
    public bool EstaQuebrado => impactos >= impactosParaQuebrar;
    // Permite reacoes especificas sem acoplar o ataque a portas ou a outros puzzles.
    // O argumento informa se deve animar ou restaurar diretamente um estado salvo.
    public event System.Action<bool> EstadoAlterado;

    private void Awake()
    {
        if (visual != null)
        {
            posicaoInicial = visual.localPosition;
            rotacaoInicial = visual.localRotation;
        }
        int.TryParse(EstadoMundo.LerTexto(chaveProgresso), out impactos);
        impactos = Mathf.Clamp(impactos, 0, impactosParaQuebrar);
        AtualizarEstado(false);
    }

    public bool ReceberImpacto()
    {
        if (!PodeInteragir || EstaQuebrado) return false;
        impactos++;
        EstadoMundo.SalvarTexto(chaveProgresso, impactos.ToString());
        AtualizarEstado(true);
        return true;
    }

    private void AtualizarEstado(bool animar)
    {
        foreach (Collider bloqueio in bloqueios)
            if (bloqueio != null) bloqueio.enabled = !EstaQuebrado;
        EstadoAlterado?.Invoke(animar);
        if (visual == null) return;

        posicaoAlvo = posicaoInicial + (EstaQuebrado ? deslocamentoQuebrado : deslocamentoPorImpacto * impactos);
        rotacaoAlvo = rotacaoInicial * Quaternion.Euler(EstaQuebrado ? rotacaoQuebrado : rotacaoPorImpacto * impactos);
        if (!animar)
        {
            visual.SetLocalPositionAndRotation(posicaoAlvo, rotacaoAlvo);
            animando = false;
            return;
        }
        posicaoOrigem = visual.localPosition;
        rotacaoOrigem = visual.localRotation;
        tempoReacao = 0f;
        duracaoAtual = Mathf.Max(0.01f, EstaQuebrado ? duracaoQueda : duracaoReacao);
        animando = true;
    }

    private void Update()
    {
        if (!animando || visual == null) return;
        tempoReacao += Time.deltaTime;
        float t = Mathf.Clamp01(tempoReacao / duracaoAtual);
        float curva = EstaQuebrado ? t * t : Mathf.SmoothStep(0f, 1f, t);
        visual.SetLocalPositionAndRotation(Vector3.Lerp(posicaoOrigem, posicaoAlvo, curva),
            Quaternion.Slerp(rotacaoOrigem, rotacaoAlvo, curva));
        if (t >= 1f) animando = false;
    }

    public override string ObterTextoInteracao()
    {
        if (EstaQuebrado) return string.Empty;
        if (SistemaInventario.Instancia == null || !SistemaInventario.Instancia.Possui("pe_de_cabra"))
            return textoSemFerramenta;
        return textoComFerramenta + " (" + impactos + "/" + impactosParaQuebrar + ")";
    }

    // O texto usa o raycast de interacao existente; E nao substitui o golpe com M1.
    public override void Interagir() { }
}
