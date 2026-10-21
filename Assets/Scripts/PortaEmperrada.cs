using UnityEngine;

/// <summary>Reacao visual da porta: solta o calco, abre e oscila sem abandonar a dobradica.</summary>
[RequireComponent(typeof(QuebravelCenario))]
public class PortaEmperrada : MonoBehaviour
{
    [SerializeField] private QuebravelCenario resistencia;
    [SerializeField] private Transform pivoDobradica;
    [SerializeField] private Transform calco;
    [Tooltip("Bloqueio fixo da abertura. A colisao da folha acompanha o pivo e nao e desligada.")]
    [SerializeField] private Collider bloqueioPassagem;
    [Header("Emperramento")]
    [SerializeField] private float folgaPorImpacto = -1.5f;
    [SerializeField, Min(0.01f)] private float duracaoBatida = 0.48f;
    [SerializeField, Min(0f)] private float trepidacaoBatida = 6f;
    [Header("Abertura e dobradica frouxa")]
    [SerializeField] private float anguloAberto = -103f;
    [SerializeField, Min(0.01f)] private float duracaoAbertura = 0.48f;
    [SerializeField, Range(10f, 89f)] private float anguloLiberarPassagem = 65f;
    [SerializeField, Min(0.01f)] private float duracaoBalanco = 2f;
    [SerializeField, Min(0f)] private float amplitudeBalanco = 7f;
    [SerializeField, Min(0.1f)] private float frequenciaBalanco = 2f;
    [SerializeField, Min(0.1f)] private float amortecimento = 2.6f;
    [SerializeField] private float inclinacaoAberta = -0.7f;
    [SerializeField, Min(0f)] private float balancoRepouso = 0.2f;
    [Header("Calco solto (relativo a posicao inicial)")]
    [SerializeField] private Vector3 deslocamentoCalcoSolto = new Vector3(-0.35f, -1.43f, 0.55f);
    [SerializeField] private Vector3 rotacaoCalcoSolto = new Vector3(80f, 25f, 65f);

    private Quaternion rotacaoInicial, rotacaoInicialCalco;
    private Vector3 posicaoInicialCalco, posicaoOrigemCalco;
    private Quaternion rotacaoOrigemCalco;
    private float tempo, anguloAtual, inclinacaoAtual, anguloOrigem, inclinacaoOrigem;
    private bool animando, inicializado;

    private void Awake()
    {
        if (resistencia == null) resistencia = GetComponent<QuebravelCenario>();
        if (pivoDobradica != null) rotacaoInicial = pivoDobradica.localRotation;
        if (calco != null)
        {
            posicaoInicialCalco = calco.localPosition;
            rotacaoInicialCalco = calco.localRotation;
        }
        inicializado = true;
    }

    private void OnEnable()
    {
        if (resistencia == null) return;
        resistencia.EstadoAlterado += Sincronizar;
        Sincronizar(false);
    }

    private void OnDisable()
    {
        if (resistencia != null) resistencia.EstadoAlterado -= Sincronizar;
        if (inicializado) Sincronizar(false);
    }

    private void Sincronizar(bool animar)
    {
        if (!inicializado || resistencia == null) return;
        tempo = 0f;
        animando = animar;
        anguloOrigem = anguloAtual;
        inclinacaoOrigem = inclinacaoAtual;
        if (calco != null)
        {
            posicaoOrigemCalco = calco.localPosition;
            rotacaoOrigemCalco = calco.localRotation;
        }
        if (!animar)
        {
            AplicarPorta(resistencia.EstaQuebrado ? anguloAberto : folgaPorImpacto * resistencia.ImpactosRecebidos,
                resistencia.EstaQuebrado ? inclinacaoAberta : 0f);
            AplicarCalco(1f);
        }
        AtualizarBloqueio();
    }

    private void Update() => AvancarAnimacao(Time.deltaTime);

    private void AvancarAnimacao(float delta)
    {
        if (resistencia == null || pivoDobradica == null) return;
        tempo += delta;
        if (!animando)
        {
            if (resistencia.EstaQuebrado)
            {
                float folga = Mathf.Sin(tempo * 1.4f) * balancoRepouso;
                AplicarPorta(anguloAberto + folga, inclinacaoAberta + folga * 0.3f);
            }
            return;
        }

        if (!resistencia.EstaQuebrado)
        {
            float t = Mathf.Clamp01(tempo / duracaoBatida);
            float impulso = Mathf.Sin(t * Mathf.PI * 4f) * (1f - t) * (1f - t);
            float destino = folgaPorImpacto * resistencia.ImpactosRecebidos;
            AplicarPorta(Mathf.Lerp(anguloOrigem, destino, Mathf.SmoothStep(0f, 1f, t))
                - impulso * trepidacaoBatida, impulso * 0.7f);
            AplicarCalco(Mathf.SmoothStep(0f, 1f, t));
            if (t >= 1f) animando = false;
        }
        else
        {
            float t = Mathf.Clamp01(tempo / duracaoAbertura);
            float abertura = 1f - Mathf.Pow(1f - t, 3f);
            float oscilacao = 0f;
            if (tempo > duracaoAbertura && tempo < duracaoAbertura + duracaoBalanco)
            {
                float depois = tempo - duracaoAbertura;
                oscilacao = Mathf.Sin(depois * Mathf.PI * 2f * frequenciaBalanco) * Mathf.Exp(-amortecimento * depois);
            }
            AplicarPorta(Mathf.Lerp(anguloOrigem, anguloAberto, abertura) - oscilacao * amplitudeBalanco,
                Mathf.Lerp(inclinacaoOrigem, inclinacaoAberta, abertura) + oscilacao * 1.4f);
            AplicarCalco(Mathf.Clamp01(tempo / 0.6f) * Mathf.Clamp01(tempo / 0.6f));
            if (tempo >= duracaoAbertura + duracaoBalanco) animando = false;
        }
        AtualizarBloqueio();
    }

    private void AplicarPorta(float angulo, float inclinacao)
    {
        anguloAtual = angulo;
        inclinacaoAtual = inclinacao;
        if (pivoDobradica != null)
            pivoDobradica.localRotation = rotacaoInicial * Quaternion.Euler(0f, angulo, inclinacao);
        // A posicao do pivo nunca muda: a porta continua presa ao eixo.
    }

    private void AplicarCalco(float t)
    {
        if (calco == null) return;
        int golpes = resistencia.ImpactosRecebidos;
        Vector3 destino = posicaoInicialCalco + (resistencia.EstaQuebrado
            ? deslocamentoCalcoSolto : new Vector3(-0.025f * golpes, -0.01f * golpes, 0.018f * golpes));
        Quaternion rotacao = rotacaoInicialCalco * Quaternion.Euler(resistencia.EstaQuebrado
            ? rotacaoCalcoSolto : new Vector3(0f, 0f, -8f * golpes));
        calco.SetLocalPositionAndRotation(Vector3.Lerp(posicaoOrigemCalco, destino, t),
            Quaternion.Slerp(rotacaoOrigemCalco, rotacao, t));
    }

    private void AtualizarBloqueio()
    {
        if (bloqueioPassagem != null)
            bloqueioPassagem.enabled = !resistencia.EstaQuebrado || Mathf.Abs(anguloAtual) < anguloLiberarPassagem;
    }

    private void OnValidate()
    {
        duracaoBatida = Mathf.Max(0.01f, duracaoBatida);
        duracaoAbertura = Mathf.Max(0.01f, duracaoAbertura);
        duracaoBalanco = Mathf.Max(0.01f, duracaoBalanco);
        anguloLiberarPassagem = Mathf.Min(anguloLiberarPassagem, Mathf.Abs(anguloAberto) * 0.8f);
    }
}
