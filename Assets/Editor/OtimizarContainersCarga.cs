using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OtimizarContainersCarga
{
    private const string CaminhoCena = "Assets/Scenes/Parte 1/Aréa de Carga.unity";

    [MenuItem("Tools/GFC/Desempenho/Otimizar containers da Area de Carga")]
    private static void Aplicar()
    {
        Scene cena = SceneManager.GetActiveScene();
        if (!cena.IsValid() || cena.path != CaminhoCena)
        {
            EditorUtility.DisplayDialog(
                "Cena incorreta",
                "Abra a cena Aréa de Carga antes de aplicar a otimizacao.",
                "OK");
            return;
        }

        var renderizadoresDosContainers = new HashSet<Renderer>();
        int containersAtivos = 0;
        foreach (GameObject raiz in cena.GetRootGameObjects())
        {
            foreach (Transform transform in raiz.GetComponentsInChildren<Transform>(true))
            {
                bool eRaizDoContainer = transform.parent != null &&
                    (transform.parent.name == "Containers" || transform.parent.name == "Containers2");
                if (!transform.gameObject.activeInHierarchy ||
                    !eRaizDoContainer ||
                    !transform.name.StartsWith("Container_", System.StringComparison.Ordinal))
                    continue;

                containersAtivos++;
                foreach (Renderer renderizador in transform.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderizador.gameObject.activeInHierarchy && !(renderizador is SkinnedMeshRenderer))
                        renderizadoresDosContainers.Add(renderizador);
                }
            }
        }

        if (containersAtivos == 0 || renderizadoresDosContainers.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Containers nao encontrados",
                "Nao encontrei containers ativos com renderizadores nesta cena.",
                "OK");
            return;
        }

        int renderizadoresMarcados = 0;
        foreach (Renderer renderizador in renderizadoresDosContainers)
        {
            GameObject objeto = renderizador.gameObject;
            Undo.RecordObject(objeto, "Ativar batching estatico dos containers");
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(objeto);
            GameObjectUtility.SetStaticEditorFlags(objeto, flags | StaticEditorFlags.BatchingStatic);
            PrefabUtility.RecordPrefabInstancePropertyModifications(objeto);
            EditorUtility.SetDirty(objeto);
            renderizadoresMarcados++;
        }

        GameObject gerenciador = GameObject.Find("Otimizacao_Carga");
        if (gerenciador == null)
        {
            gerenciador = new GameObject("Otimizacao_Carga");
            Undo.RegisterCreatedObjectUndo(gerenciador, "Adicionar otimizacao da Area de Carga");
        }
        if (gerenciador.GetComponent<QualidadeTemporariaCarga>() == null)
            Undo.AddComponent<QualidadeTemporariaCarga>(gerenciador);

        EditorSceneManager.MarkSceneDirty(cena);
        if (!EditorSceneManager.SaveScene(cena))
        {
            EditorUtility.DisplayDialog(
                "Falha ao salvar",
                "As alteracoes foram aplicadas, mas a cena nao foi salva.",
                "OK");
            return;
        }

        Debug.Log($"[GFC_CARGA] Otimizacao salva. {containersAtivos} containers ativos encontrados; {renderizadoresMarcados} renderizadores receberam Static Batching. Sombras, cascatas e LOD sao reduzidos apenas durante esta cena e restaurados ao sair.");
        EditorUtility.DisplayDialog(
            "Otimizacao aplicada",
            $"{containersAtivos} containers ativos encontrados; {renderizadoresMarcados} renderizadores agora usam Static Batching. A cena tambem limita sombras a 70 m, 2 cascatas e LOD Bias 1 apenas durante o jogo na Area de Carga.",
            "OK");
    }
}
