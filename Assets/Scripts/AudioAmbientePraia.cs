using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Paisagem sonora local da Praia. As zonas e os clips sao configurados na instancia da cena.</summary>
public class AudioAmbientePraia : MonoBehaviour
{
    [Header("Gravacoes separadas")]
    [SerializeField] private AudioClip chuvaVentoPraia;
    [SerializeField] private AudioClip ventoNasArvores;
    [SerializeField] private AudioClip ondasDoMar;
    [SerializeField] private AudioClip chuvaNoCascoEMar;
    [Header("Volumes da Praia")]
    [SerializeField, Range(0f, 1f)] private float volumeChuvaVentoPraia = 0.32f;
    [SerializeField, Range(0f, 1f)] private float volumeVento = 0.24f;
    [SerializeField, Range(0f, 1f)] private float volumeOndas = 0.2f;
    [SerializeField, Range(0f, 1f)] private float volumeNavio = 0.24f;
    [Header("Regioes desta cena")]
    [SerializeField] private BoxCollider zonaCabana;
    [SerializeField] private BoxCollider zonaAreia;
    [SerializeField] private Collider colisorCasco;
    [SerializeField] private Transform destrocosBote;
    [SerializeField, Min(0.1f)] private float suavidadeZona = 0.6f;
    [SerializeField] private float inicioCasco = 38f;
    [SerializeField] private float cascoCompleto = 7f;
    [SerializeField] private float inicioMar = 28f;
    [SerializeField] private float marCompleto = 8f;

    private AudioSource chuva, vento, mar, casco;
    private AudioLowPassFilter filtroChuva, filtroVento;
    private AudioMovimentoPlayer movimento;
    private bool ativo;
    private float proximaAmostraAreia, alvoAreia;
    public float IntensidadeCabana { get; private set; }
    public float IntensidadeAreia { get; private set; }
    public float IntensidadeMar { get; private set; }
    public float IntensidadeCasco { get; private set; }

    private void Awake() => movimento = GetComponent<AudioMovimentoPlayer>();
    private void OnEnable() => SceneManager.sceneLoaded += AoCarregarCena;
    private void Start() => AtivarNaCena(SceneManager.GetActiveScene());
    private void AoCarregarCena(Scene cena, LoadSceneMode modo) => AtivarNaCena(cena);

    private void AtivarNaCena(Scene cena)
    {
        ativo = cena.name.Equals("Praia", System.StringComparison.OrdinalIgnoreCase);
        if (!ativo) { Parar(); return; }
        if (chuva == null)
        {
            chuva = CriarFonte("Chuva nas folhas", chuvaVentoPraia);
            vento = CriarFonte("Vento nas arvores", ventoNasArvores);
            mar = CriarFonte("Mar junto aos destrocos", ondasDoMar);
            casco = CriarFonte("Pingos no casco", chuvaNoCascoEMar);
            filtroChuva = chuva.gameObject.AddComponent<AudioLowPassFilter>();
            filtroVento = vento.gameObject.AddComponent<AudioLowPassFilter>();
            filtroChuva.cutoffFrequency = filtroVento.cutoffFrequency = 18000f;
        }
    }

    private AudioSource CriarFonte(string nome, AudioClip clip)
    {
        GameObject objeto = new GameObject("Audio Praia - " + nome);
        objeto.transform.SetParent(transform, false);
        AudioSource fonte = objeto.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = true;
        fonte.spatialBlend = 0f;
        fonte.dopplerLevel = 0f;
        fonte.priority = 100;
        fonte.volume = 0f;
        fonte.clip = clip;
        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
        return fonte;
    }

    private void Update()
    {
        if (!ativo) return;
        Vector3 posicao = transform.position;
        IntensidadeCabana = PesoZona(zonaCabana, posicao, suavidadeZona);
        if (Time.unscaledTime >= proximaAmostraAreia)
        {
            alvoAreia = PesoAreiaPintada(posicao);
            proximaAmostraAreia = Time.unscaledTime + 0.2f;
        }
        IntensidadeAreia = Mathf.MoveTowards(IntensidadeAreia, alvoAreia, Time.unscaledDeltaTime * 1.5f);

        float distanciaCasco = colisorCasco != null
            ? Vector3.Distance(posicao, colisorCasco.ClosestPoint(posicao)) : float.PositiveInfinity;
        float distanciaDestrocos = float.PositiveInfinity;
        if (destrocosBote != null)
        {
            Vector3 diferenca = posicao - destrocosBote.position;
            diferenca.y = 0f;
            distanciaDestrocos = diferenca.magnitude;
        }
        IntensidadeCasco = IntensidadeAreia * Aproximacao(inicioCasco, cascoCompleto, distanciaCasco);
        IntensidadeMar = IntensidadeAreia * Aproximacao(inicioMar, marCompleto, distanciaDestrocos);
        float abrigoChuva = Mathf.Lerp(1f, 0.22f, IntensidadeCabana);
        float abrigoVento = Mathf.Lerp(1f, 0.15f, IntensidadeCabana);
        AtualizarFonte(chuva, volumeChuvaVentoPraia * abrigoChuva);
        AtualizarFonte(vento, volumeVento * Mathf.Lerp(1f, 0.45f, IntensidadeAreia) * abrigoVento);
        AtualizarFonte(casco, volumeNavio * IntensidadeCasco * abrigoChuva);
        AtualizarFonte(mar, volumeOndas * IntensidadeMar * abrigoChuva);
        float corte = Mathf.Lerp(18000f, 1000f, IntensidadeCabana);
        if (filtroChuva != null) filtroChuva.cutoffFrequency = Mathf.Lerp(filtroChuva.cutoffFrequency, corte, Time.unscaledDeltaTime * 5f);
        if (filtroVento != null) filtroVento.cutoffFrequency = Mathf.Lerp(filtroVento.cutoffFrequency, corte, Time.unscaledDeltaTime * 5f);
    }

    private static float Aproximacao(float inicio, float completo, float distancia)
        => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inicio, completo, distancia));

    private float PesoAreiaPintada(Vector3 posicao)
    {
        Terrain escolhido = null;
        float menorAltura = float.PositiveInfinity;
        foreach (Terrain terreno in Terrain.activeTerrains)
        {
            Vector3 local = terreno.transform.InverseTransformPoint(posicao);
            Vector3 tamanho = terreno.terrainData.size;
            if (local.x < 0f || local.z < 0f || local.x > tamanho.x || local.z > tamanho.z) continue;
            float altura = Mathf.Abs(posicao.y - 1f - terreno.SampleHeight(posicao) - terreno.transform.position.y);
            if (altura < menorAltura) { escolhido = terreno; menorAltura = altura; }
        }
        if (escolhido != null)
            return DetectorSuperficieAudio.PesoAreiaNoTerrain(escolhido.GetComponent<TerrainCollider>(), posicao);
        return PesoZona(zonaAreia, posicao, 1.5f);
    }

    public static float PesoZona(BoxCollider zona, Vector3 posicao, float margem)
    {
        if (zona == null) return 0f;
        Vector3 p = zona.transform.InverseTransformPoint(posicao) - zona.center;
        Vector3 q = new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z)) - zona.size * 0.5f;
        Vector3 fora = new Vector3(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f), Mathf.Max(q.z, 0f));
        float distancia = fora.magnitude + Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0f);
        return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-margem, margem, distancia));
    }

    private static void AtualizarFonte(AudioSource fonte, float volume)
    {
        if (fonte == null || fonte.clip == null) return;
        if (!fonte.isPlaying && fonte.clip.loadState == AudioDataLoadState.Loaded) fonte.Play();
        fonte.volume = Mathf.MoveTowards(fonte.volume, volume, Time.unscaledDeltaTime * 0.35f);
    }

    private void Parar()
    {
        foreach (AudioSource fonte in new[] { chuva, vento, mar, casco })
            if (fonte != null) { fonte.Stop(); fonte.volume = 0f; }
        if (movimento != null) movimento.DefinirSuperficieDaRegiao(null);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
        Parar();
    }
}
