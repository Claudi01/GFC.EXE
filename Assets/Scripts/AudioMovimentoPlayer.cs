using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public sealed class AudioMovimentoPlayer : MonoBehaviour
{
    private const string CENA_CARGA = "Aréa de Carga";

    [Header("Passos por superficie")]
    [SerializeField] private AudioClip[] passosAreia;
    [SerializeField] private AudioClip[] passosTerraGrama;
    [SerializeField] private AudioClip[] passosMetal;
    [SerializeField] private AudioClip[] passosMadeira;
    [SerializeField] private AudioClip[] passosConcretoPedra;
    [SerializeField] private AudioClip[] passosAguaRasa;

    [Header("Aterrissagem por superficie")]
    [SerializeField] private AudioClip[] aterrissagensAreia;
    [SerializeField] private AudioClip[] aterrissagensTerraGrama;
    [SerializeField] private AudioClip[] aterrissagensMetal;
    [SerializeField] private AudioClip[] aterrissagensMadeira;
    [SerializeField] private AudioClip[] aterrissagensConcretoPedra;
    [SerializeField] private AudioClip[] aterrissagensAguaRasa;

    [Header("Acoes")]
    [SerializeField] private AudioClip[] pulos;
    [SerializeField] private AudioClip[] escaladaCorda;
    [SerializeField, Min(0.2f)] private float distanciaEntrePassos = 1.7f;
    [SerializeField, Range(0f, 1f)] private float volumePassos = 0.75f;
    [SerializeField, Range(0f, 1f)] private float volumePulo = 0.55f;
    [SerializeField, Range(0f, 1f)] private float volumeAterrissagem = 0.75f;
    [SerializeField, Range(0f, 1f)] private float volumeCorda = 0.7f;

    private CharacterController controlador;
    private AudioSource fontePassos;
    private AudioSource fonteAcoes;
    private AudioSource fonteCorda;
    private Coroutine rotinaEscalada;
    private readonly int[] ultimoPasso = { -1, -1, -1, -1, -1, -1 };
    private readonly int[] ultimoPouso = { -1, -1, -1, -1, -1, -1 };
    private int ultimoPulo = -1, ultimoCorda = -1;
    private float distanciaAcumulada, tempoNoAr, maiorVelocidadeQueda;
    private bool amostraInicializada, jaEsteveNoChao, estavaNoAr;
    private Vector3 ultimaPosicao;
    private SuperficieAudio? superficieDaRegiao;
    private bool regiaoSomenteTerrain;

    public SuperficieAudio UltimaSuperficie { get; private set; }
    public int PassosTocados { get; private set; }
    public int PulosTocados { get; private set; }
    public int AterrissagensTocadas { get; private set; }
    public int MovimentosCordaTocados { get; private set; }
    public bool Escalando => rotinaEscalada != null;
    public AudioSource FontePassos => fontePassos;
    public AudioSource FonteAcoes => fonteAcoes;
    public AudioSource FonteCorda => fonteCorda;
    public SuperficieAudio? SuperficieDaRegiao => superficieDaRegiao;
    public bool RegiaoSomenteTerrain => regiaoSomenteTerrain;

    private bool PassosAtivosNestaCena => SceneManager.GetActiveScene().name != CENA_CARGA;

    private void Awake()
    {
        controlador = GetComponent<CharacterController>();
        fontePassos = CriarFonte();
        fonteAcoes = CriarFonte();
        fonteCorda = CriarFonte();
    }

    private void OnEnable() => SceneManager.sceneLoaded += AoCarregarCena;

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
        DefinirSuperficieDaRegiao(null);
        PararEscalada();
        SuspenderMovimento();
        if (fontePassos != null) fontePassos.Stop();
        if (fonteAcoes != null) fonteAcoes.Stop();
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        DefinirSuperficieDaRegiao(null);
        PararEscalada();
        SuspenderMovimento();
    }

    private AudioSource CriarFonte()
    {
        AudioSource fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = false;
        fonte.spatialBlend = 0f;
        fonte.dopplerLevel = 0f;
        fonte.priority = 80;
        return fonte;
    }

    /// <summary>Recebe o deslocamento resultante do CharacterController.Move, nunca apenas o input.</summary>
    public void RegistrarMovimento(Vector3 posicaoAntes, Vector3 posicaoDepois, bool chaoAntes,
        bool chaoDepois, bool pulou, float velocidadeVertical)
    {
        if (!isActiveAndEnabled)
        {
            SuspenderMovimento();
            return;
        }
        if (Time.deltaTime <= 0f || Escalando ||
            (GameplayState.Instancia != null && GameplayState.Instancia.EstaBloqueado))
        {
            PausarCadencia();
            return;
        }

        Vector3 deslocamento = posicaoDepois - posicaoAntes;
        bool teleporte = deslocamento.sqrMagnitude > 9f ||
            (amostraInicializada && (posicaoAntes - ultimaPosicao).sqrMagnitude > 0.5625f);
        if (teleporte) SuspenderMovimento();
        ultimaPosicao = posicaoDepois;
        amostraInicializada = true;
        if (teleporte) return;

        if (chaoAntes) jaEsteveNoChao = true;
        if (pulou) chaoDepois = false;
        if (!chaoDepois)
        {
            estavaNoAr = true;
            tempoNoAr += Time.deltaTime;
            maiorVelocidadeQueda = Mathf.Max(maiorVelocidadeQueda, -velocidadeVertical);
            distanciaAcumulada = 0f;
            return;
        }

        if (estavaNoAr && jaEsteveNoChao && tempoNoAr >= 0.1f && maiorVelocidadeQueda >= 2.2f)
            TocarAterrissagem(ObterSuperficieSobPes(), Mathf.Clamp01(maiorVelocidadeQueda / 9f));
        estavaNoAr = false;
        jaEsteveNoChao = true;
        tempoNoAr = 0f;
        maiorVelocidadeQueda = 0f;

        deslocamento.y = 0f;
        if (deslocamento.sqrMagnitude < 0.000004f) return;
        distanciaAcumulada += deslocamento.magnitude;
        if (distanciaAcumulada >= distanciaEntrePassos)
        {
            distanciaAcumulada %= distanciaEntrePassos;
            if (PassosAtivosNestaCena) TocarPasso(ObterSuperficieSobPes());
        }
    }

    /// <summary>Pausa passos sem perder a queda em curso. O tempo no ar so avanca quando o controller move.</summary>
    public void PausarCadencia()
    {
        distanciaAcumulada = 0f;
    }

    /// <summary>Reinicia a amostragem ao surgir, teleportar ou trocar de cena/controlador.</summary>
    public void SuspenderMovimento()
    {
        distanciaAcumulada = tempoNoAr = maiorVelocidadeQueda = 0f;
        amostraInicializada = jaEsteveNoChao = estavaNoAr = false;
    }

    /// <summary>Contexto opcional da zona atual. Nao altera clips nem volumes do prefab.</summary>
    public void DefinirSuperficieDaRegiao(SuperficieAudio? superficie, bool somenteTerrain = false)
    {
        superficieDaRegiao = superficie;
        regiaoSomenteTerrain = superficie.HasValue && somenteTerrain;
    }

    public SuperficieAudio ObterSuperficieSobPes()
    {
        Bounds limites = controlador != null ? controlador.bounds : new Bounds(transform.position, Vector3.one * 2f);
        RaycastHit[] acertos = Physics.RaycastAll(limites.center + Vector3.up * 0.1f, Vector3.down,
            limites.extents.y + 0.8f, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(acertos, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit acerto in acertos)
        {
            if (acerto.collider.transform.IsChildOf(transform)) continue;
            SuperficieAudio superficie = DetectorSuperficieAudio.Identificar(acerto.collider, acerto.point);
            // Areas de agua podem ser triggers; volumes de interacao comuns nao sao piso.
            if (acerto.collider.isTrigger && superficie != SuperficieAudio.AguaRasa) continue;
            if (!acerto.collider.isTrigger && superficieDaRegiao.HasValue &&
                (!regiaoSomenteTerrain || acerto.collider is TerrainCollider))
                return superficieDaRegiao.Value;
            return superficie;
        }
        return DetectorSuperficieAudio.Identificar(null, transform.position);
    }

    public void TocarPasso(SuperficieAudio superficie)
    {
        if (!isActiveAndEnabled || !PassosAtivosNestaCena) return;
        AudioClip[] clips = ObterPassos(superficie);
        if (Tocar(fontePassos, clips, ref ultimoPasso[(int)superficie], volumePassos))
        {
            UltimaSuperficie = superficie;
            PassosTocados++;
        }
    }

    public void TocarPulo()
    {
        if (!isActiveAndEnabled) return;
        distanciaAcumulada = 0f;
        if (Tocar(fonteAcoes, pulos, ref ultimoPulo, volumePulo)) PulosTocados++;
    }

    public void TocarAterrissagem(SuperficieAudio superficie, float intensidade = 1f)
    {
        if (!isActiveAndEnabled) return;
        float ganho = volumeAterrissagem * Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(intensidade));
        if (Tocar(fonteAcoes, ObterPousos(superficie), ref ultimoPouso[(int)superficie], ganho))
        {
            UltimaSuperficie = superficie;
            AterrissagensTocadas++;
        }
        // O contato de pouso ja substitui o primeiro passo depois do salto.
        distanciaAcumulada = 0f;
    }

    public void IniciarEscalada()
    {
        PararEscalada();
        SuspenderMovimento();
        if (isActiveAndEnabled) rotinaEscalada = StartCoroutine(TocarEscalada());
    }

    public void PararEscalada()
    {
        if (rotinaEscalada != null) StopCoroutine(rotinaEscalada);
        rotinaEscalada = null;
        if (fonteCorda != null) fonteCorda.Stop();
    }

    private IEnumerator TocarEscalada()
    {
        while (true)
        {
            if (Tocar(fonteCorda, escaladaCorda, ref ultimoCorda, volumeCorda)) MovimentosCordaTocados++;
            yield return new WaitForSecondsRealtime(Random.Range(0.3f, 0.4f));
        }
    }

    private static bool Tocar(AudioSource fonte, AudioClip[] clips, ref int ultimo, float volume)
    {
        if (fonte == null || clips == null || clips.Length == 0) return false;
        int indice = Random.Range(0, clips.Length);
        if (clips.Length > 1 && indice == ultimo) indice = (indice + Random.Range(1, clips.Length)) % clips.Length;
        if (clips[indice] == null) return false;
        ultimo = indice;
        fonte.pitch = Random.Range(0.96f, 1.04f);
        fonte.PlayOneShot(clips[indice], volume);
        return true;
    }

    private AudioClip[] ObterPassos(SuperficieAudio superficie)
    {
        switch (superficie)
        {
            case SuperficieAudio.Areia: return passosAreia;
            case SuperficieAudio.TerraGrama: return passosTerraGrama;
            case SuperficieAudio.Metal: return passosMetal;
            case SuperficieAudio.Madeira: return passosMadeira;
            case SuperficieAudio.ConcretoPedra: return passosConcretoPedra;
            case SuperficieAudio.AguaRasa: return passosAguaRasa;
            default: return passosConcretoPedra;
        }
    }

    private AudioClip[] ObterPousos(SuperficieAudio superficie)
    {
        switch (superficie)
        {
            case SuperficieAudio.Areia: return aterrissagensAreia;
            case SuperficieAudio.TerraGrama: return aterrissagensTerraGrama;
            case SuperficieAudio.Metal: return aterrissagensMetal;
            case SuperficieAudio.Madeira: return aterrissagensMadeira;
            case SuperficieAudio.ConcretoPedra: return aterrissagensConcretoPedra;
            case SuperficieAudio.AguaRasa: return aterrissagensAguaRasa;
            default: return aterrissagensConcretoPedra;
        }
    }
}
