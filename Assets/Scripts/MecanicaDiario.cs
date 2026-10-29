using System.Collections;
using UnityEngine;

public class MecanicaDiario : MonoBehaviour
{
    [Header("Interface UI")]
    public GameObject telaDiario;
    public GameObject telaPreta;
    public GameObject textoUIInteracao;
    private FaderScript fader;
    [Header("Referencias do Player")]
    public MonoBehaviour scriptMovimento;
    public MonoBehaviour scriptOlharCamera;
    public InteracaoPlayer scriptInteracao;
    [Header("Bote e itens")]
    public GameObject boteInteiro;
    public GameObject boteDestruido;
    public GameObject ganchoEscalada;
    public AudioSource somImpacto;
    [SerializeField] private AudioClip audioQuedaBote;
    [SerializeField, Range(0f, 1f)] private float volumeQuebraBote = 0.6f;
    [Header("Persistencia do evento")]
    public string chaveEvento = "gfc.evento.diario.bote";
    private bool lendo, jaCaiu, emTransicao;

    private void Start()
    {
        ConfigurarSomBote();
        if (telaPreta != null) fader = telaPreta.GetComponent<FaderScript>();
        jaCaiu = EstadoMundo.EstaConcluido(chaveEvento);
        if (jaCaiu) AplicarEstadoBote();
        if (scriptMovimento == null) scriptMovimento = Object.FindFirstObjectByType<FirstPersonController>();
        if (scriptInteracao == null) scriptInteracao = Object.FindFirstObjectByType<InteracaoPlayer>();
    }

    private void ConfigurarSomBote()
    {
        GameObject origem = new GameObject("Audio - Madeira quebrando ao longe");
        origem.transform.SetParent(transform, false);
        origem.transform.position = boteDestruido != null ? boteDestruido.transform.position : transform.position;
        somImpacto = origem.AddComponent<AudioSource>();
        somImpacto.playOnAwake = false;
        somImpacto.loop = false;
        somImpacto.spatialBlend = 1f;
        somImpacto.dopplerLevel = 0f;
        somImpacto.priority = 40;
        somImpacto.volume = volumeQuebraBote;
        somImpacto.minDistance = 12f;
        somImpacto.maxDistance = 220f;
        somImpacto.rolloffMode = AudioRolloffMode.Custom;
        somImpacto.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.55f, 0.38f), new Keyframe(1f, 0f)));
        AudioLowPassFilter distancia = origem.AddComponent<AudioLowPassFilter>();
        distancia.cutoffFrequency = 3800f;
        if (audioQuedaBote != null) audioQuedaBote.LoadAudioData();
    }

    private void Update()
    {
        if (lendo && !emTransicao && Input.GetKeyDown(KeyCode.E)) StartCoroutine(FecharProcesso());
    }

    public void Interagir()
    {
        if (fader == null || emTransicao) return;
        if (!lendo) StartCoroutine(AbrirProcesso());
    }

    private void AlternarControlePlayer(bool estado)
    {
        if (GameplayState.Instancia != null)
        {
            if (estado) GameplayState.Instancia.Liberar(GameplayBlockReason.Diario);
            else GameplayState.Instancia.Bloquear(GameplayBlockReason.Diario);
        }
        if (scriptInteracao != null) scriptInteracao.EsconderTexto();
        if (textoUIInteracao != null) textoUIInteracao.SetActive(estado);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void AplicarEstadoBote()
    {
        if (boteInteiro != null) boteInteiro.SetActive(false);
        if (boteDestruido != null) boteDestruido.SetActive(true);
        if (ganchoEscalada != null) ganchoEscalada.SetActive(true);
    }

    private void QuebrarBoteAoFechar()
    {
        if (jaCaiu) return;
        AplicarEstadoBote();
        if (somImpacto != null && audioQuedaBote != null) somImpacto.PlayOneShot(audioQuedaBote);
        jaCaiu = true;
        EstadoMundo.MarcarConcluido(chaveEvento);
    }

    private IEnumerator AbrirProcesso()
    {
        emTransicao = true;
        lendo = true;
        AlternarControlePlayer(false);
        telaPreta.SetActive(true);
        yield return StartCoroutine(fader.FazerFade(true));
        telaDiario.SetActive(true);
        yield return StartCoroutine(fader.FazerFade(false));
        emTransicao = false;
    }

    private IEnumerator FecharProcesso()
    {
        emTransicao = true;
        QuebrarBoteAoFechar();
        yield return StartCoroutine(fader.FazerFade(true));
        telaDiario.SetActive(false);
        yield return StartCoroutine(fader.FazerFade(false));
        telaPreta.SetActive(false);
        AlternarControlePlayer(true);
        lendo = false;
        emTransicao = false;
    }
}
