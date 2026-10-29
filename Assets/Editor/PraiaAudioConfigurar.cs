using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Configura apenas a instancia da Praia; nao aplica overrides aos prefabs compartilhados.</summary>
public static class PraiaAudioConfigurar
{
    private const string Pasta = "Assets/Sons/Praia_v3/";
    [Serializable] private class Catalogo { public Clip[] clips; }
    [Serializable] private class Clip { public string role, path, guid; }
    private static Catalogo catalogo;

    [MenuItem("Tools/GFC/Audio/Aplicar percurso da Praia")]
    public static void Aplicar()
    {
        Scene cena = SceneManager.GetActiveScene();
        if (Application.isPlaying || cena.name != "Praia")
            throw new InvalidOperationException("Abra a cena Praia fora do Play Mode para aplicar o audio.");
        catalogo = JsonUtility.FromJson<Catalogo>(File.ReadAllText(Pasta + "catalogo.json"));
        if (catalogo == null || catalogo.clips == null) throw new Exception("Catalogo de audio ausente.");
        foreach (string role in new[] { "rain", "wind", "sea", "hull", "boat", "hook", "step_grass", "step_sand", "step_wood" }) Clips(role, true);
        FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
        MecanicaDiario diario = Object.FindFirstObjectByType<MecanicaDiario>();
        MecanicaEscalada escalada = Object.FindFirstObjectByType<MecanicaEscalada>();
        GameObject tent = GameObject.Find("Tent");
        GameObject navio = GameObject.Find("Ship");
        if (player == null || diario == null || escalada == null || tent == null || navio == null)
            throw new Exception("Referencias obrigatorias da Praia nao encontradas.");
        Collider casco = navio.GetComponentsInChildren<BoxCollider>().OrderByDescending(c => c.bounds.size.sqrMagnitude).FirstOrDefault();
        if (casco == null || diario.boteDestruido == null) throw new Exception("Casco/destrocos sem referencia.");
        AudioAmbientePraia ambiente = player.GetComponent<AudioAmbientePraia>();
        AudioMovimentoPlayer movimento = player.GetComponent<AudioMovimentoPlayer>();
        if (ambiente == null || movimento == null) throw new Exception("Componentes de audio faltando no Player.");

        GameObject raiz = GameObject.Find("Audio_Praia");
        if (raiz == null) { raiz = new GameObject("Audio_Praia"); Undo.RegisterCreatedObjectUndo(raiz, "Audio Praia"); }
        Transform zonaTransform = raiz.transform.Find("Cabana - chuva abafada");
        if (zonaTransform == null)
        {
            GameObject zona = new GameObject("Cabana - chuva abafada");
            Undo.RegisterCreatedObjectUndo(zona, "Zona da cabana");
            zona.transform.SetParent(raiz.transform, false);
            zonaTransform = zona.transform;
        }
        Undo.RecordObject(zonaTransform, "Posicionar abrigo");
        zonaTransform.SetPositionAndRotation(tent.transform.TransformPoint(new Vector3(0f, 1.5f, 0f)),
            Quaternion.Euler(0f, tent.transform.eulerAngles.y, 0f));
        zonaTransform.localScale = Vector3.one;
        BoxCollider abrigo = zonaTransform.GetComponent<BoxCollider>();
        if (abrigo == null) abrigo = Undo.AddComponent<BoxCollider>(zonaTransform.gameObject);
        Undo.RecordObject(abrigo, "Volume do abrigo");
        abrigo.center = Vector3.zero;
        abrigo.size = new Vector3(4.65f * Mathf.Abs(tent.transform.lossyScale.x),
            3f * Mathf.Abs(tent.transform.lossyScale.y), 7.05f * Mathf.Abs(tent.transform.lossyScale.z));
        abrigo.isTrigger = true;
        abrigo.enabled = false; // Medido matematicamente; nao participa da fisica.

        SerializedObject a = Preparar(ambiente);
        Objeto(a, "chuvaVentoPraia", Primeiro("rain"));
        Objeto(a, "ventoNasArvores", Primeiro("wind"));
        Objeto(a, "ondasDoMar", Primeiro("sea"));
        Objeto(a, "chuvaNoCascoEMar", Primeiro("hull"));
        Objeto(a, "zonaCabana", abrigo);
        Objeto(a, "zonaAreia", null);
        Objeto(a, "colisorCasco", casco);
        Objeto(a, "destrocosBote", diario.boteDestruido.transform);
        Numero(a, "volumeChuvaVentoPraia", 0.32f);
        Numero(a, "volumeVento", 0.24f);
        Numero(a, "volumeOndas", 0.2f);
        Numero(a, "volumeNavio", 0.24f);
        Numero(a, "suavidadeZona", 0.6f);
        Numero(a, "inicioCasco", 38f);
        Numero(a, "cascoCompleto", 7f);
        Numero(a, "inicioMar", 28f);
        Numero(a, "marCompleto", 8f);
        Concluir(a);

        SerializedObject m = Preparar(movimento);
        DefinirArray(m, "passosTerraGrama", "step_grass");
        DefinirArray(m, "passosAreia", "step_sand");
        DefinirArray(m, "passosMadeira", "step_wood");
        DefinirArray(m, "passosMetal", "step_metal", false);
        DefinirArray(m, "passosConcretoPedra", "step_stone", false);
        DefinirArray(m, "passosAguaRasa", "step_water", false);
        Numero(m, "volumePassos", 0.22f);
        Numero(m, "volumePulo", 0.18f);
        Numero(m, "volumeAterrissagem", 0.24f);
        Numero(m, "volumeCorda", 0.2f);
        Numero(m, "distanciaEntrePassos", 2f);
        Concluir(m);

        SerializedObject d = Preparar(diario);
        Objeto(d, "audioQuedaBote", Primeiro("boat"));
        Numero(d, "volumeQuebraBote", 0.6f);
        Concluir(d);
        TransicaoCena transicao = escalada.GetComponent<TransicaoCena>();
        SerializedObject t = Preparar(transicao);
        Objeto(t, "somAoEscurecer", Primeiro("hook"));
        Numero(t, "volumeSomAoEscurecer", 0.35f);
        Concluir(t);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        Debug.Log("PRAIA_AUDIO_V3_CONFIGURADO: somente Praia salva; floresta, abrigo, areia, casco, destrocos e gancho ligados.");
        Selection.activeGameObject = raiz;
    }

    private static SerializedObject Preparar(Object objeto)
    {
        Undo.RecordObject(objeto, "Configurar audio Praia");
        return new SerializedObject(objeto);
    }
    private static void Objeto(SerializedObject s, string campo, Object valor) => s.FindProperty(campo).objectReferenceValue = valor;
    private static void Numero(SerializedObject s, string campo, float valor) => s.FindProperty(campo).floatValue = valor;
    private static void Concluir(SerializedObject s)
    {
        s.ApplyModifiedProperties();
        if (PrefabUtility.IsPartOfPrefabInstance(s.targetObject))
            PrefabUtility.RecordPrefabInstancePropertyModifications(s.targetObject);
        EditorUtility.SetDirty(s.targetObject);
    }
    private static AudioClip Primeiro(string role) => Clips(role, true)[0];
    private static AudioClip[] Clips(string role, bool obrigatorio)
    {
        List<AudioClip> lista = new List<AudioClip>();
        foreach (Clip clip in catalogo.clips.Where(c => c.role == role))
        {
            AudioClip audio = AssetDatabase.LoadAssetAtPath<AudioClip>(Pasta + clip.path);
            if (audio == null) throw new Exception("Audio nao importado: " + clip.path);
            lista.Add(audio);
        }
        if (obrigatorio && lista.Count == 0) throw new Exception("Categoria ausente: " + role);
        return lista.ToArray();
    }
    private static void DefinirArray(SerializedObject s, string campo, string role, bool obrigatorio = true)
    {
        AudioClip[] clips = Clips(role, obrigatorio);
        if (clips.Length == 0) return;
        SerializedProperty p = s.FindProperty(campo);
        p.arraySize = clips.Length;
        for (int i = 0; i < clips.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
    }
}
