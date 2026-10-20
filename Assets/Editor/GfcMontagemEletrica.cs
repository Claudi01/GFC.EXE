using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Ferramenta de montagem somente do Editor. Nao participa do jogo/build.
public static class GfcMontagemEletrica
{
    public const string NomeMontagem = "Eletrica_Armarios_Adicionados";
    private const string CenaGuid = "62d4d4a5f1ef87d43aad3d8582598bda";
    private const string BaseModelos = "Modelos Eletrica (posicionados)/Eletrica Posicionados/";
    private sealed class Marcacao
    {
        public GameObject objeto;
        public Vector3 centro, normal, parede;
    }
    private sealed class Fontes
    {
        public Transform armario, rede;
        public Transform[] armariosExistentes;
        public MeshFilter reto, curva;
        public Marcacao[] marcacoes;
        public Vector3 frenteOriginal;
        public float diametro, raio;
    }

    [MenuItem("Tools/GFC/Eletrica/Popular armarios e canos")]
    public static void Popular()
    {
        try
        {
            GameObject montagem = MontarCena(SceneManager.GetActiveScene());
            Selection.activeGameObject = montagem;
            Debug.Log("Eletrica: 10 armarios e tubulacao adicionados. Os Cube (1) a (10) foram preservados " +
                "na raiz, desativados. Confira a cena e salve com Ctrl+S. Ctrl+Z desfaz a montagem.", montagem);
        }
        catch (Exception e) { Debug.LogError("Montagem nao aplicada: " + e.Message); }
    }

    [MenuItem("Tools/GFC/Eletrica/Validar montagem antes de aplicar")]
    public static void Validar()
    {
        try
        {
            Preparar(SceneManager.GetActiveScene());
            Debug.Log("Montagem validada: 10 marcadores, modelo do armario, parede de apoio e canos encontrados. " +
                "Nenhum objeto foi alterado. Use Tools > GFC > Eletrica > Popular armarios e canos.");
        }
        catch (Exception e) { Debug.LogError("Montagem: " + e.Message); }
    }

    /// <summary>So executa por comando explicito. Nao salva nem substitui o arquivo da cena.</summary>
    public static GameObject MontarCena(Scene cena)
    {
        Fontes fontes = Preparar(cena); // Todas as pre-condicoes antes da primeira alteracao.
        Undo.IncrementCurrentGroup();
        int grupoUndo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Popular eletrica: armarios e canos");
        try
        {
            GameObject raiz = new GameObject(NomeMontagem);
            SceneManager.MoveGameObjectToScene(raiz, cena);
            Undo.RegisterCreatedObjectUndo(raiz, "Criar montagem da eletrica");
            Transform armarios = Pasta("Armarios", raiz.transform);
            Transform canos = Pasta("Tubulacao", raiz.transform);
            var saidas = new Vector3[10];
            var novos = new GameObject[10];

            for (int i = 0; i < fontes.marcacoes.Length; i++)
            {
                Marcacao m = fontes.marcacoes[i];
                GameObject novo = UnityEngine.Object.Instantiate(fontes.armario.gameObject, armarios, false);
                novo.name = "LARGE_ELECTRICAL_CABINET_C" + (i + 1).ToString("00");
                novo.transform.localScale = fontes.armario.lossyScale;
                novo.transform.rotation = Quaternion.FromToRotation(fontes.frenteOriginal, m.normal) * fontes.armario.rotation;
                novo.transform.position += m.centro - Limites(novo).center;

                // Usa o centro visual da marcacao e encosta o fundo a 2 cm da parede.
                Bounds b = Limites(novo);
                float metadeProfundidade = Mathf.Abs(Vector3.Dot(b.extents, m.normal));
                float afastamento = Vector3.Dot(b.center - m.parede, m.normal);
                novo.transform.position += m.normal * (metadeProfundidade + .02f - afastamento);
                b = Limites(novo);
                saidas[i] = new Vector3(b.center.x, b.max.y - .025f, b.center.z);
                novos[i] = novo;

                // Colisao no corpo, sem scripts de puzzle ou IDs copiados para os novos armarios.
                Transform corpo = novo.transform.Find("LargeCabinet_Body");
                if (corpo != null && corpo.GetComponent<Collider>() == null)
                {
                    Mesh mesh = corpo.GetComponent<MeshFilter>().sharedMesh;
                    BoxCollider colisao = corpo.gameObject.AddComponent<BoxCollider>();
                    colisao.center = mesh.bounds.center;
                    colisao.size = mesh.bounds.size;
                }
            }
            for (int i = 0; i < novos.Length; i++)
                for (int j = i + 1; j < novos.Length; j++)
                    Exigir(!Limites(novos[i]).Intersects(Limites(novos[j])), "Dois armarios ficariam sobrepostos. Revise os marcadores.");

            MontarTubulacao(fontes, saidas, canos);
            // Nao coloque objetos preexistentes dentro de uma hierarquia criada neste Undo.
            // Mantendo os originais na raiz, a remocao da montagem nunca os apaga.
            foreach (Marcacao m in fontes.marcacoes)
            {
                Undo.RecordObject(m.objeto, "Ocultar marcador original");
                m.objeto.SetActive(false);
            }
            Undo.FlushUndoRecordObjects();
            EditorSceneManager.MarkSceneDirty(cena);
            Undo.CollapseUndoOperations(grupoUndo);
            return raiz;
        }
        catch
        {
            Undo.RevertAllDownToGroup(grupoUndo);
            throw;
        }
    }

    private static Fontes Preparar(Scene cena)
    {
        Exigir(!EditorApplication.isPlayingOrWillChangePlaymode, "Saia do Play Mode antes de montar.");
        Exigir(cena.IsValid() && cena.isLoaded && AssetDatabase.AssetPathToGUID(cena.path) == CenaGuid,
            "Abra a cena Area de Carga original.");
        GameObject[] raizes = cena.GetRootGameObjects();
        Exigir(!raizes.Any(o => o.name == NomeMontagem), "A montagem ja existe. Desfaca com Ctrl+Z antes de executar novamente.");
        Transform[] todos = raizes.SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform armario = Unico(todos, BaseModelos + "LARGE_ELECTRICAL_CABINET (2)");
        Transform rede = Unico(todos, BaseModelos + "PIPE_T (1)");
        MeshFilter reto = Unico(todos, BaseModelos + "PIPE_T (1)/Pipe_T_Branch").GetComponent<MeshFilter>();
        MeshFilter curva = Unico(todos, BaseModelos + "Pipe_Elbow (2)").GetComponent<MeshFilter>();
        Exigir(armario.gameObject.activeInHierarchy && rede.gameObject.activeInHierarchy, "Os modelos de referencia precisam estar ativos.");
        Exigir(armario.GetComponentsInChildren<MonoBehaviour>(true).Length == 0,
            "O armario de referencia recebeu scripts. Use uma referencia somente visual para nao duplicar IDs do puzzle.");
        Exigir(reto != null && reto.sharedMesh != null && curva != null && curva.sharedMesh != null, "Malha dos canos ausente.");
        Transform frente = armario.Find("LargeCabinet_Front");
        Transform corpo = armario.Find("LargeCabinet_Body");
        Exigir(frente != null && corpo != null, "O modelo precisa de LargeCabinet_Body e LargeCabinet_Front.");
        Vector3 frenteOriginal = (frente.position - corpo.position).normalized;
        Exigir(Vector3.Dot(frenteOriginal, Vector3.left) > .99f, "A orientacao do armario de referencia mudou.");

        var fontes = new Fontes { armario = armario, rede = rede, reto = reto, curva = curva,
            frenteOriginal = frenteOriginal, marcacoes = new Marcacao[10] };
        fontes.armariosExistentes = Enumerable.Range(2, 4)
            .Select(i => Unico(todos, BaseModelos + "LARGE_ELECTRICAL_CABINET (" + i + ")")).ToArray();
        Bounds meshCurva = curva.sharedMesh.bounds;
        float escalaCurva = curva.transform.lossyScale.x;
        fontes.diametro = reto.sharedMesh.bounds.size.x * reto.transform.lossyScale.x;
        fontes.raio = (meshCurva.size.x - meshCurva.size.y * .5f) * escalaCurva;
        Exigir(Mathf.Abs(meshCurva.max.x) < .00001f && Mathf.Abs(meshCurva.min.z) < .00001f &&
            Mathf.Abs(fontes.diametro - meshCurva.size.y * escalaCurva) < .01f && fontes.raio > .1f,
            "O formato dos canos mudou. Revise os encaixes antes de montar.");

        Physics.SyncTransforms();
        for (int i = 0; i < 10; i++)
        {
            GameObject[] candidatos = raizes.Where(o => o.name == "Cube (" + (i + 1) + ")").ToArray();
            Exigir(candidatos.Length == 1 && candidatos[0].activeInHierarchy, "Falta um unico Cube (" + (i + 1) + ") ativo na raiz.");
            GameObject marcador = candidatos[0];
            Bounds b = Limites(marcador);
            Exigir(b.size.sqrMagnitude > .01f, marcador.name + " esta sem geometria.");
            Vector3 normal = i < 3 ? Vector3.right : i < 6 ? Vector3.left : Vector3.forward;
            RaycastHit[] paredes = Physics.RaycastAll(b.center, -normal, 1.5f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider.gameObject.scene == cena && Caminho(h.collider.transform).StartsWith("Salas/Eletrica/Eletrica Terreno/", StringComparison.Ordinal) &&
                    Vector3.Dot(h.normal, normal) > .99f).OrderBy(h => h.distance).ToArray();
            Exigir(paredes.Length > 0, "Nao encontrei parede atras de " + marcador.name + ".");
            fontes.marcacoes[i] = new Marcacao { objeto = marcador, centro = b.center, normal = normal, parede = paredes[0].point };
        }
        return fontes;
    }

    private static void MontarTubulacao(Fontes f, Vector3[] p, Transform pai)
    {
        float y = f.rede.position.y, r = f.raio;
        float xa = p.Take(3).Average(v => v.x), xb = p.Skip(3).Take(3).Average(v => v.x);
        float zc = p.Skip(6).Average(v => v.z);
        Exigir(p.All(v => y - v.y > r * 2), "A tubulacao precisa ficar acima dos armarios.");
        Exigir(p[0].z < p[1].z && p[1].z < p[2].z && p[3].z < p[4].z && p[4].z < p[5].z &&
            p[6].x > p[7].x && p[7].x > p[8].x && p[8].x > p[9].x,
            "A ordem das marcacoes mudou; esta montagem atende as tres fileiras atuais.");
        Exigir(p.Take(3).All(v => Mathf.Abs(v.x - xa) < .04f) && p.Skip(3).Take(3).All(v => Mathf.Abs(v.x - xb) < .04f) &&
            p.Skip(6).All(v => Mathf.Abs(v.z - zc) < .04f), "As fileiras precisam estar alinhadas para receber os canos.");

        // A: deriva da travessia ja existente, contorna e desce no ultimo armario.
        Vector3 a0 = new Vector3(xa, y, f.rede.position.z);
        Vector3 a1 = new Vector3(xa, y, p[2].z);
        Transform ramalFinal = f.rede.Find("Pipe_T_Branch (4)");
        Exigir(ramalFinal != null, "Falta o ramal final Pipe_T_Branch (4) da rede existente.");
        Bounds ramalExistente = Limites(ramalFinal.gameObject);
        Exigir(a0.x + r >= ramalExistente.min.x && a0.x + r <= ramalExistente.max.x,
            "A curva nova nao alcança o ramal existente. Revise a rede de referencia.");
        Exigir(p[0].z - a0.z > 2 * r, "Sem espaco para curvar a chegada da rede.");
        Curva(f, pai, "A_Chegada", a0, Vector3.left, Vector3.forward);
        Trecho(f, pai, "A_Coletor", a0 + Vector3.forward * r, a1 - Vector3.forward * r);
        Curva(f, pai, "A_DescidaFinal", a1, Vector3.forward, Vector3.down);
        Trecho(f, pai, "A_Descida_C03", a1 + Vector3.down * r, p[2]);
        for (int i = 0; i < 2; i++) Trecho(f, pai, "A_Ramal_C0" + (i + 1), p[i], new Vector3(xa, y, p[i].z));

        // B+C: um coletor em L acompanha as duas paredes do segundo corredor.
        Vector3 c0 = new Vector3(p[9].x, y, zc);
        Vector3 canto = new Vector3(xb, y, zc);
        Vector3 b1 = new Vector3(xb, y, p[5].z);
        Exigir(canto.x - c0.x > 2 * r && b1.z - canto.z > 2 * r, "Sem espaco para o coletor em L.");
        Trecho(f, pai, "C_Descida_C10", p[9], c0 - Vector3.up * r);
        Curva(f, pai, "C_Inicio", c0, Vector3.up, Vector3.right);
        Trecho(f, pai, "C_Coletor", c0 + Vector3.right * r, canto - Vector3.right * r);
        Curva(f, pai, "BC_Canto", canto, Vector3.right, Vector3.forward);
        Trecho(f, pai, "B_Coletor", canto + Vector3.forward * r, b1 - Vector3.forward * r);
        Curva(f, pai, "B_DescidaFinal", b1, Vector3.forward, Vector3.down);
        Trecho(f, pai, "B_Descida_C06", b1 + Vector3.down * r, p[5]);
        for (int i = 3; i < 5; i++) Trecho(f, pai, "B_Ramal_C0" + (i + 1), p[i], new Vector3(xb, y, p[i].z));
        for (int i = 6; i < 9; i++) Trecho(f, pai, "C_Ramal_C0" + (i + 1), p[i], new Vector3(p[i].x, y, zc));

        // Travessia curta da divisoria, na altura da rede, liga as tres fileiras.
        float zLigacao = (zc + p[3].z) * .5f;
        Exigir(zLigacao > zc + r && zLigacao < p[0].z, "Sem espaco para a ligacao entre os corredores.");
        Trecho(f, pai, "Ligacao_AB", new Vector3(xa, y, zLigacao), new Vector3(xb, y, zLigacao));

        // Completa tambem as derivacoes dos armarios antigos, sem mover a rede existente.
        Bounds[] antigos = f.armariosExistentes.Select(t => Limites(t.gameObject)).OrderBy(b => b.center.z).ToArray();
        float xOriginal = f.rede.position.x;
        Exigir(antigos.All(b => b.min.x < xOriginal && b.max.x > xOriginal && b.max.y < y - 2 * r),
            "Os armarios antigos nao estao alinhados com a tubulacao de referencia.");
        Vector3 ultimo = new Vector3(xOriginal, y, antigos[3].center.z);
        Trecho(f, pai, "Originais_Coletor", new Vector3(xOriginal, y, f.rede.position.z + 2f), ultimo - Vector3.forward * r);
        Curva(f, pai, "Originais_DescidaFinal", ultimo, Vector3.forward, Vector3.down);
        Trecho(f, pai, "Originais_DescidaFinal", ultimo + Vector3.down * r,
            new Vector3(xOriginal, antigos[3].max.y - .025f, antigos[3].center.z));
        for (int i = 1; i < 3; i++)
            Trecho(f, pai, "Originais_Ramal_" + i, new Vector3(xOriginal, antigos[i].max.y - .025f, antigos[i].center.z),
                new Vector3(xOriginal, y, antigos[i].center.z));
    }

    private static void Trecho(Fontes f, Transform pai, string nome, Vector3 inicio, Vector3 fim)
    {
        Vector3 delta = fim - inicio;
        Exigir(delta.magnitude > .02f, "Trecho de cano sem comprimento: " + nome);
        int quantidade = Mathf.CeilToInt(delta.magnitude / 2f);
        Quaternion rotacao = Quaternion.FromToRotation(Vector3.forward, delta.normalized);
        Bounds malha = f.reto.sharedMesh.bounds;
        for (int i = 0; i < quantidade; i++)
        {
            Vector3 a = Vector3.Lerp(inicio, fim, (float)i / quantidade);
            Vector3 b = Vector3.Lerp(inicio, fim, (float)(i + 1) / quantidade);
            GameObject tubo = UnityEngine.Object.Instantiate(f.reto.gameObject, pai, false);
            tubo.name = "Cano_" + nome + "_" + (i + 1).ToString("00");
            Vector3 escala = new Vector3(f.diametro / malha.size.x, f.diametro / malha.size.y, (Vector3.Distance(a, b) + .01f) / malha.size.z);
            tubo.transform.localScale = escala;
            tubo.transform.rotation = rotacao;
            tubo.transform.position = (a + b) * .5f - rotacao * Vector3.Scale(malha.center, escala);
        }
    }

    private static void Curva(Fontes f, Transform pai, string nome, Vector3 canto, Vector3 chegada, Vector3 saida)
    {
        GameObject curva = UnityEngine.Object.Instantiate(f.curva.gameObject, pai, false);
        curva.name = "Curva_" + nome;
        curva.transform.localScale = Vector3.one * f.curva.transform.lossyScale.x;
        curva.transform.rotation = Quaternion.LookRotation(chegada, Vector3.Cross(chegada, saida));
        curva.transform.position = canto - chegada * f.raio + saida * f.raio;
    }

    private static Transform Pasta(string nome, Transform pai)
    {
        Transform t = new GameObject(nome).transform;
        t.SetParent(pai, false);
        return t;
    }

    private static Transform Unico(Transform[] todos, string caminho)
    {
        Transform[] encontrados = todos.Where(t => Caminho(t) == caminho).ToArray();
        Exigir(encontrados.Length == 1, "Referencia ausente ou duplicada: " + caminho);
        return encontrados[0];
    }

    private static Bounds Limites(GameObject objeto)
    {
        Renderer[] rs = objeto.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.enabled && r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null).ToArray();
        Exigir(rs.Length > 0, "Objeto sem malha: " + objeto.name);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }

    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao) throw new InvalidOperationException(mensagem);
    }

    [Serializable] private class Relatorio { public string cena; public List<Objeto> objetos = new List<Objeto>(); }
    [Serializable] private class Objeto
    {
        public string caminho, id;
        public Vector3 posicao, euler, escala, minimo, maximo;
        public bool ativo;
        public List<Superficie> superficies = new List<Superficie>();
        public List<Malha> malhas = new List<Malha>();
    }
    [Serializable] private class Superficie { public string objeto; public Vector3 ponto, normal; public float distancia; }
    [Serializable] private class Malha
    {
        public string caminho, asset;
        public Vector3 posicao, euler, escala, centroLocal, tamanhoLocal, minimo, maximo;
    }

    [MenuItem("Tools/GFC/Eletrica/Inspecionar montagem")]
    public static void Inspecionar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene cena = SceneManager.GetActiveScene();
        if (!cena.path.EndsWith("Aréa de Carga.unity", StringComparison.Ordinal)) return;
        InspecionarCena(cena, ".utmp/GfcMontagemEletrica/inspecao.json");
    }

    public static void InspecionarCena(Scene cena, string destino)
    {
        var relatorio = new Relatorio { cena = cena.path };
        Physics.SyncTransforms();
        foreach (Transform t in cena.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)))
        {
            string n = t.name;
            bool relevante = (t.parent == null && n.StartsWith("Cube")) ||
                n.StartsWith("LARGE_ELECTRICAL_CABINET") || n.StartsWith("PIPE_") ||
                n.StartsWith("Pipe_Elbow") || n.StartsWith("Parede", StringComparison.OrdinalIgnoreCase);
            if (!relevante) continue;
            Renderer[] renderers = t.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new Bounds(t.position, Vector3.zero);
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                foreach (Renderer r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            }
            var objeto = new Objeto { caminho = Caminho(t), id = GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString(),
                posicao = t.position, euler = t.eulerAngles, escala = t.lossyScale,
                minimo = bounds.min, maximo = bounds.max, ativo = t.gameObject.activeInHierarchy };
            if (t.parent == null && n.StartsWith("Cube"))
                foreach (Vector3 direcao in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                    foreach (RaycastHit hit in Physics.RaycastAll(bounds.center, direcao, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                        if (hit.collider.transform.root != t && !hit.collider.transform.root.name.StartsWith("Cube"))
                            objeto.superficies.Add(new Superficie { objeto = Caminho(hit.collider.transform), ponto = hit.point, normal = hit.normal, distancia = hit.distance });
            foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Renderer r = mf.GetComponent<Renderer>();
                objeto.malhas.Add(new Malha { caminho = Caminho(mf.transform), asset = AssetDatabase.GetAssetPath(mf.sharedMesh),
                    posicao = mf.transform.position, euler = mf.transform.eulerAngles, escala = mf.transform.lossyScale,
                    centroLocal = mf.sharedMesh.bounds.center, tamanhoLocal = mf.sharedMesh.bounds.size,
                    minimo = r != null ? r.bounds.min : mf.transform.position,
                    maximo = r != null ? r.bounds.max : mf.transform.position });
            }
            relatorio.objetos.Add(objeto);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(destino));
        File.WriteAllText(destino, JsonUtility.ToJson(relatorio, true));
    }

    private static string Caminho(Transform t)
    {
        return t.parent == null ? t.name : Caminho(t.parent) + "/" + t.name;
    }
}
