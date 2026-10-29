#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Smoke test da Praia pelo menu. Devolve ao EditMode e restaura progresso; nunca encerra o Editor.</summary>
[InitializeOnLoad]
public static class PraiaAudioVerificar
{
    private const string Menu = "Tools/GFC/Audio/Verificar Praia em Play";
    private const string Sessao = "gfc.audio.praia.verificacao.v3.";
    private const string RegistroKey = "gfc.progresso.registro.v1";
    private const string InventarioKey = "inventario.grade.v1";
    private const string CargoPath = "Assets/Scenes/Parte 1/Aréa de Carga.unity";
    private static int etapa = -1;
    private static double inicio, proximo, fechamento;
    private static Report relatorio;
    private static FirstPersonController jogador;
    private static AudioAmbientePraia ambiente;
    private static AudioMovimentoPlayer movimento;
    private static MecanicaDiario diario;
    private static MecanicaEscalada escalada;
    private static TransicaoCena transicao;
    private static float chuvaFloresta;
    private static string destino;

    [Serializable] private sealed class Registro { public List<string> chaves = new List<string>(); }
    [Serializable] private sealed class ValorPref { public string chave, tipo, texto; public bool existia; public int inteiro; public float real; }
    [Serializable] private sealed class Snapshot { public List<ValorPref> valores = new List<ValorPref>(); public string hashCarga; }
    [Serializable] private sealed class Resultado { public string nome, detalhe; public bool passou; }
    [Serializable] private sealed class Report
    {
        public string inicioUtc, fimUtc, estado;
        public bool aprovado, prefsRestauradas, cargoIntacta;
        public List<Resultado> resultados = new List<Resultado>();
    }

    static PraiaAudioVerificar()
    {
        EditorApplication.playModeStateChanged += MudarModo;
        EditorApplication.update += Atualizar;
    }

    [MenuItem(Menu, false, 205)]
    public static void Iniciar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "Praia")
        {
            Debug.LogError("Abra a cena Praia em EditMode antes de verificar o audio.");
            return;
        }
        if (SessionState.GetBool(Sessao + "ativa", false)) return;
        Snapshot snapshot = CapturarPrefs();
        snapshot.hashCarga = HashArquivo(CargoPath);
        SessionState.SetString(Sessao + "snapshot", JsonUtility.ToJson(snapshot));
        relatorio = new Report { inicioUtc = DateTime.UtcNow.ToString("o"), estado = "aguardando_play" };
        SalvarRelatorioSessao();
        SessionState.SetBool(Sessao + "ativa", true);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem(Menu, true)]
    private static bool PodeIniciar() => !EditorApplication.isPlayingOrWillChangePlaymode &&
        !SessionState.GetBool(Sessao + "ativa", false) && SceneManager.GetActiveScene().name == "Praia";

    private static void MudarModo(PlayModeStateChange modo)
    {
        if (!SessionState.GetBool(Sessao + "ativa", false)) return;
        if (modo == PlayModeStateChange.EnteredPlayMode)
        {
            relatorio = JsonUtility.FromJson<Report>(SessionState.GetString(Sessao + "relatorio", "{}"));
            relatorio.estado = "em_execucao";
            inicio = EditorApplication.timeSinceStartup;
            proximo = inicio + 1.5;
            etapa = 0;
        }
        else if (modo == PlayModeStateChange.ExitingPlayMode)
        {
            if (relatorio != null && relatorio.estado == "em_execucao")
            {
                Adicionar("Conclusao do roteiro", false, "Play interrompido antes de concluir o roteiro.");
                relatorio.estado = "interrompido";
            }
            SalvarRelatorioSessao();
        }
        else if (modo == PlayModeStateChange.EnteredEditMode)
        {
            RestaurarEGravar();
        }
    }

    private static void Atualizar()
    {
        if (!SessionState.GetBool(Sessao + "ativa", false) || !EditorApplication.isPlaying || etapa < 0) return;
        if (EditorApplication.timeSinceStartup - inicio > 45.0)
        {
            Adicionar("Tempo limite", false, "Roteiro ultrapassou 45 segundos em Play.");
            Encerrar();
            return;
        }
        if (EditorApplication.timeSinceStartup < proximo) return;
        try { ExecutarEtapa(); }
        catch (Exception erro)
        {
            Adicionar("Excecao na etapa " + etapa, false, erro.ToString());
            Encerrar();
        }
    }

    private static void ExecutarEtapa()
    {
        switch (etapa)
        {
            case 0:
                jogador = Object.FindFirstObjectByType<FirstPersonController>();
                ambiente = Object.FindFirstObjectByType<AudioAmbientePraia>();
                movimento = Object.FindFirstObjectByType<AudioMovimentoPlayer>();
                diario = Object.FindFirstObjectByType<MecanicaDiario>();
                escalada = Object.FindFirstObjectByType<MecanicaEscalada>();
                Exigir(jogador != null && ambiente != null && movimento != null && diario != null && escalada != null,
                    "Componentes de player/ambiente/diario/escalada disponiveis");
                jogador.enabled = false;
                AudioSource chuva = Campo<AudioSource>(ambiente, "chuva");
                AudioSource vento = Campo<AudioSource>(ambiente, "vento");
                chuvaFloresta = chuva != null ? chuva.volume : 0f;
                Adicionar("Floresta inicial", ambiente.IntensidadeCabana < 0.05f && ambiente.IntensidadeAreia < 0.05f,
                    "Cabana=" + ambiente.IntensidadeCabana + "; areia=" + ambiente.IntensidadeAreia);
                Adicionar("Chuva e vento tocando", Tocando(chuva) && Tocando(vento) && chuva.volume > 0f && vento.volume > 0f,
                    Descrever(chuva) + "; " + Descrever(vento));
                Adicionar("Volumes de floresta", chuva != null && vento != null && chuva.volume <= 0.35f && vento.volume <= 0.35f,
                    "chuva=" + chuvaFloresta + "; vento=" + (vento != null ? vento.volume : -1f));
                BoxCollider cabana = Campo<BoxCollider>(ambiente, "zonaCabana");
                Exigir(cabana != null, "Zona de cabana atribuida");
                MoverPlayer(cabana.transform.TransformPoint(cabana.center));
                Agendar(1, 1.3);
                break;
            case 1:
                AudioSource abrigada = Campo<AudioSource>(ambiente, "chuva");
                AudioLowPassFilter filtro = abrigada != null ? abrigada.GetComponent<AudioLowPassFilter>() : null;
                Adicionar("Cabana abafa chuva", ambiente.IntensidadeCabana > 0.8f && abrigada != null &&
                    abrigada.volume < chuvaFloresta * 0.5f && filtro != null && filtro.cutoffFrequency < 3500f,
                    "peso=" + ambiente.IntensidadeCabana + "; volume=" + (abrigada != null ? abrigada.volume : -1f) +
                    "; corte=" + (filtro != null ? filtro.cutoffFrequency : -1f));
                diario.Interagir();
                Adicionar("Abrir diario nao quebra bote", !EstadoMundo.EstaConcluido(diario.chaveEvento) &&
                    diario.somImpacto != null && !diario.somImpacto.isPlaying,
                    "evento=" + EstadoMundo.EstaConcluido(diario.chaveEvento));
                Agendar(2, 1.55);
                break;
            case 2:
                Adicionar("Diario aberto sem impacto", diario.telaDiario != null && diario.telaDiario.activeSelf &&
                    !EstadoMundo.EstaConcluido(diario.chaveEvento) && !diario.somImpacto.isPlaying, "Abertura concluida.");
                fechamento = EditorApplication.timeSinceStartup;
                MethodInfo fechar = typeof(MecanicaDiario).GetMethod("FecharProcesso", BindingFlags.NonPublic | BindingFlags.Instance);
                Exigir(fechar != null, "Coroutine de fechar diario encontrada");
                diario.StartCoroutine((IEnumerator)fechar.Invoke(diario, null));
                Adicionar("Fechar toca quebra 3D nos destrocos", diario.somImpacto.isPlaying &&
                    diario.somImpacto.spatialBlend > 0.99f && diario.boteDestruido != null &&
                    Vector3.Distance(diario.somImpacto.transform.position, diario.boteDestruido.transform.position) < 0.1f &&
                    EstadoMundo.EstaConcluido(diario.chaveEvento), Descrever(diario.somImpacto));
                Agendar(3, 1.55);
                break;
            case 3:
                bool fechou = !Campo<bool>(diario, "emTransicao") && !Campo<bool>(diario, "lendo");
                Adicionar("Fechamento sem espera de audio", fechou && EditorApplication.timeSinceStartup - fechamento < 1.85,
                    "fechado=" + fechou + "; segundos=" + (EditorApplication.timeSinceStartup - fechamento));
                MoverPlayer(NoSolo(new Vector3(45f, 2f, 0f)));
                Agendar(4, 1.1);
                break;
            case 4:
                SuperficieAudio superficie = movimento.ObterSuperficieSobPes();
                Adicionar("Areia real muda passos", superficie == SuperficieAudio.Areia && ambiente.IntensidadeAreia > 0.65f,
                    "superficie=" + superficie + "; peso=" + ambiente.IntensidadeAreia);
                int antes = movimento.PassosTocados;
                movimento.TocarPasso(superficie);
                Adicionar("Passo com ganho baixo", movimento.PassosTocados == antes + 1 && movimento.FontePassos.isPlaying &&
                    Campo<float>(movimento, "volumePassos") <= 0.3f,
                    "volumePassos=" + Campo<float>(movimento, "volumePassos"));
                Collider cascoFisico = Campo<Collider>(ambiente, "colisorCasco");
                Exigir(cascoFisico != null, "Collider do casco atribuido");
                Vector3 pontoCasco = cascoFisico.ClosestPoint(jogador.transform.position);
                Vector3 paraAreia = jogador.transform.position - pontoCasco;
                paraAreia.y = 0f;
                if (paraAreia.sqrMagnitude < 0.01f) paraAreia = Vector3.left;
                MoverPlayer(NoSolo(pontoCasco + paraAreia.normalized * 3f));
                Agendar(5, 1.3);
                break;
            case 5:
                AudioSource pingos = Campo<AudioSource>(ambiente, "casco");
                Adicionar("Pingos junto ao casco", ambiente.IntensidadeCasco > 0.5f && Tocando(pingos) &&
                    pingos.volume > 0.03f && pingos.volume <= 0.3f,
                    "peso=" + ambiente.IntensidadeCasco + "; " + Descrever(pingos));
                Exigir(diario.boteDestruido != null, "Destrocos disponiveis");
                MoverPlayer(NoSolo(diario.boteDestruido.transform.position + new Vector3(-4f, 1f, 0f)));
                Agendar(6, 1.3);
                break;
            case 6:
                AudioSource mar = Campo<AudioSource>(ambiente, "mar");
                AudioSource casco = Campo<AudioSource>(ambiente, "casco");
                Adicionar("Mar junto aos destrocos", ambiente.IntensidadeMar > 0.5f && Tocando(mar) && mar.volume > 0.03f,
                    "peso=" + ambiente.IntensidadeMar + "; " + Descrever(mar));
                Adicionar("Mar e pingos com ganho moderado", mar != null && casco != null && mar.volume <= 0.3f && casco.volume <= 0.3f,
                    Descrever(mar) + "; " + Descrever(casco));
                transicao = escalada.GetComponent<TransicaoCena>();
                Exigir(transicao != null && transicao.somAoEscurecer != null, "Som de gancho configurado na transicao");
                destino = transicao.nomeCenaDestino;
                Exigir(SistemaInventario.Instancia != null, "Inventario runtime disponivel");
                if (!SistemaInventario.Instancia.Possui("gancho"))
                    Exigir(SistemaInventario.Instancia.TentarAdicionar("gancho", "Gancho", 1, 2, true), "Gancho inserido para o teste");
                escalada.IniciarEscalada();
                Agendar(7, 0.8);
                break;
            case 7:
                AudioSource gancho = null;
                if (transicao != null)
                    foreach (AudioSource fonte in transicao.GetComponents<AudioSource>())
                        if (fonte.clip == transicao.somAoEscurecer) gancho = fonte;
                Image tela = transicao != null && transicao.fader != null ? transicao.fader.GetComponent<Image>() : null;
                Adicionar("Gancho toca com tela preta", Tocando(gancho) && tela != null && tela.color.a > 0.95f,
                    Descrever(gancho) + "; alpha=" + (tela != null ? tela.color.a : -1f));
                Adicionar("Gancho consumido na escalada", SistemaInventario.Instancia != null &&
                    !SistemaInventario.Instancia.Possui("gancho") && EstadoMundo.EstaConcluido(MecanicaEscalada.ChaveGanchoUsado), "Consumo e registro.");
                Agendar(8, 0.2);
                break;
            case 8:
                if (SceneManager.GetActiveScene().name != destino) { proximo = EditorApplication.timeSinceStartup + 0.15; return; }
                bool vazando = false;
                foreach (AudioAmbientePraia item in Object.FindObjectsByType<AudioAmbientePraia>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    foreach (AudioSource fonte in item.GetComponentsInChildren<AudioSource>(true))
                        if (fonte.name.StartsWith("Audio Praia -", StringComparison.Ordinal) && fonte.isPlaying) vazando = true;
                Adicionar("Carga sem ambiente da Praia", !vazando, "cena=" + destino + "; vazamento=" + vazando);
                Encerrar();
                break;
        }
    }

    private static void Agendar(int novaEtapa, double segundos)
    {
        etapa = novaEtapa;
        proximo = EditorApplication.timeSinceStartup + segundos;
        SalvarRelatorioSessao();
    }

    private static void MoverPlayer(Vector3 posicao)
    {
        CharacterController cc = jogador.GetComponent<CharacterController>();
        bool ativo = cc != null && cc.enabled;
        if (ativo) cc.enabled = false;
        jogador.transform.position = posicao;
        if (ativo) cc.enabled = true;
        movimento.SuspenderMovimento();
        Physics.SyncTransforms();
    }

    private static Vector3 NoSolo(Vector3 posicao)
    {
        Terrain escolhido = null;
        float erro = float.PositiveInfinity;
        foreach (Terrain terreno in Terrain.activeTerrains)
        {
            Vector3 local = terreno.transform.InverseTransformPoint(posicao);
            Vector3 size = terreno.terrainData.size;
            if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z) continue;
            float y = terreno.SampleHeight(posicao) + terreno.transform.position.y;
            if (Mathf.Abs(y - posicao.y) < erro) { escolhido = terreno; erro = Mathf.Abs(y - posicao.y); }
        }
        if (escolhido != null) posicao.y = escolhido.SampleHeight(posicao) + escolhido.transform.position.y + 1.02f;
        return posicao;
    }

    private static T Campo<T>(object objeto, string nome)
    {
        FieldInfo campo = objeto.GetType().GetField(nome, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (campo == null) throw new MissingFieldException(objeto.GetType().Name, nome);
        return (T)campo.GetValue(objeto);
    }

    private static bool Tocando(AudioSource fonte) => fonte != null && fonte.clip != null && fonte.isPlaying;
    private static string Descrever(AudioSource fonte)
    {
        if (fonte == null) return "fonte nula";
        float[] amostras = new float[1024];
        fonte.GetOutputData(amostras, 0);
        double energia = 0;
        float pico = 0f;
        foreach (float valor in amostras) { energia += valor * valor; pico = Mathf.Max(pico, Mathf.Abs(valor)); }
        double db = 20 * Math.Log10(Math.Max(Math.Sqrt(energia / amostras.Length), 0.0000001));
        return fonte.name + "; tocando=" + fonte.isPlaying + "; volume=" + fonte.volume +
            "; RMSdB=" + db.ToString("F1", CultureInfo.InvariantCulture) + "; pico=" + pico +
            "; clip=" + (fonte.clip != null ? fonte.clip.name : "OneShot");
    }
    private static void Exigir(bool passou, string nome)
    {
        Adicionar(nome, passou, string.Empty);
        if (!passou) throw new InvalidOperationException(nome);
    }
    private static void Adicionar(string nome, bool passou, string detalhe)
    {
        if (relatorio == null) relatorio = new Report();
        relatorio.resultados.Add(new Resultado { nome = nome, passou = passou, detalhe = detalhe });
    }
    private static void SalvarRelatorioSessao()
    {
        if (relatorio != null) SessionState.SetString(Sessao + "relatorio", JsonUtility.ToJson(relatorio));
    }
    private static void Encerrar()
    {
        etapa = -1;
        relatorio.estado = "concluido";
        SalvarRelatorioSessao();
        EditorApplication.ExitPlaymode();
    }

    private static Snapshot CapturarPrefs()
    {
        HashSet<string> chaves = new HashSet<string> { InventarioKey, RegistroKey, MecanicaEscalada.ChaveGanchoUsado, "gfc.evento.diario.bote" };
        foreach (MecanicaDiario item in Object.FindObjectsByType<MecanicaDiario>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!string.IsNullOrWhiteSpace(item.chaveEvento)) chaves.Add(item.chaveEvento);
        foreach (string chave in ChavesRegistro()) chaves.Add(chave);
        Snapshot snapshot = new Snapshot();
        foreach (string chave in chaves)
        {
            ValorPref valor = new ValorPref { chave = chave, existia = PlayerPrefs.HasKey(chave) };
            if (valor.existia)
            {
                string marcador = "__gfc_audio_tipo_6db121__";
                string texto = PlayerPrefs.GetString(chave, marcador);
                if (texto != marcador) { valor.tipo = "string"; valor.texto = texto; }
                else
                {
                    int numero = PlayerPrefs.GetInt(chave, int.MinValue);
                    if (numero != int.MinValue) { valor.tipo = "int"; valor.inteiro = numero; }
                    else { valor.tipo = "float"; valor.real = PlayerPrefs.GetFloat(chave, 0f); }
                }
            }
            snapshot.valores.Add(valor);
        }
        return snapshot;
    }

    private static List<string> ChavesRegistro()
    {
        try { return JsonUtility.FromJson<Registro>(PlayerPrefs.GetString(RegistroKey, "{}"))?.chaves ?? new List<string>(); }
        catch { return new List<string>(); }
    }

    private static void RestaurarEGravar()
    {
        try
        {
            relatorio = JsonUtility.FromJson<Report>(SessionState.GetString(Sessao + "relatorio", "{}")) ?? new Report();
            Snapshot snapshot = JsonUtility.FromJson<Snapshot>(SessionState.GetString(Sessao + "snapshot", "{}"));
            foreach (string chave in ChavesRegistro()) PlayerPrefs.DeleteKey(chave);
            foreach (ValorPref valor in snapshot.valores)
            {
                if (!valor.existia) PlayerPrefs.DeleteKey(valor.chave);
                else if (valor.tipo == "string") PlayerPrefs.SetString(valor.chave, valor.texto);
                else if (valor.tipo == "int") PlayerPrefs.SetInt(valor.chave, valor.inteiro);
                else PlayerPrefs.SetFloat(valor.chave, valor.real);
            }
            PlayerPrefs.Save();
            relatorio.prefsRestauradas = true;
            relatorio.cargoIntacta = snapshot.hashCarga == HashArquivo(CargoPath);
            Adicionar("Cena Carga em disco preservada", relatorio.cargoIntacta, CargoPath);
            relatorio.fimUtc = DateTime.UtcNow.ToString("o");
            relatorio.aprovado = relatorio.resultados.TrueForAll(x => x.passou) && relatorio.prefsRestauradas;
            string pasta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp"));
            Directory.CreateDirectory(pasta);
            string arquivo = Path.Combine(pasta, "PraiaAudioVerificacao.json");
            File.WriteAllText(arquivo, JsonUtility.ToJson(relatorio, true));
            Debug.Log("[GFC_AUDIO_PRAIA] aprovado=" + relatorio.aprovado + "; progresso restaurado; relatorio=" + arquivo);
        }
        catch (Exception erro) { Debug.LogError("Falha ao restaurar verificacao da Praia: " + erro); }
        finally
        {
            SessionState.SetBool(Sessao + "ativa", false);
            SessionState.EraseString(Sessao + "snapshot");
            SessionState.EraseString(Sessao + "relatorio");
            etapa = -1;
        }
    }

    private static string HashArquivo(string caminho)
    {
        string absoluto = Path.GetFullPath(Path.Combine(Application.dataPath, "..", caminho));
        if (!File.Exists(absoluto)) return "arquivo_ausente";
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(absoluto))).Replace("-", string.Empty);
    }
}
#endif
