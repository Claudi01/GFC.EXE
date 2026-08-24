using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>Singleton persistente e UI uGUI criada em runtime, sem alterar cenas manualmente.</summary>
public class SistemaInventario : MonoBehaviour
{
    public static SistemaInventario Instancia { get; private set; }
    public InventarioGrade Grade { get; private set; }
    public bool Aberto { get; private set; }
    public InventarioGrade.Item Selecionado { get; private set; }
    private Canvas canvas;
    private RectTransform gradeUI;
    private readonly Dictionary<InventarioGrade.Item, InventarioItemUI> views = new Dictionary<InventarioGrade.Item, InventarioItemUI>();
    private InventarioItemUI itemArrastado;
    private const float TamanhoCelula = 52f;
    private const string ChaveSave = "inventario.grade.v1";
    // Enquanto nao existe menu de continuar, cada execucao inicia uma nova partida.
    private const bool LimparSaveAoIniciarAplicacao = true;
    private Text detalhes;
    [Serializable] private class SaveData { public string equipado; public List<SaveItem> itens = new List<SaveItem>(); }
    [Serializable] private class SaveItem { public string id, nome; public int largura, altura, x, y; public bool rotacionado, podeRotacionar; }
    public string ItemEquipadoId { get; private set; } = "lanterna";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CriarAutomaticamente()
    {
        if (FindFirstObjectByType<SistemaInventario>() != null) return;

        SistemaInventario sistema = new GameObject("SistemaInventario").AddComponent<SistemaInventario>();
        if (LimparSaveAoIniciarAplicacao)
            sistema.NovaPartida();
    }

    private void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this; DontDestroyOnLoad(gameObject);
        Grade = new InventarioGrade(10, 6);
        Restaurar();
        if (!Possui("lanterna"))
        {
            TentarAdicionar("lanterna", "Lanterna", 1, 2, true);
        }
        else
        {
            // Migra saves antigos, nos quais a lanterna foi criada sem rotação.
            foreach (InventarioGrade.Item item in Grade.Itens)
                if (item.id == "lanterna") item.podeRotacionar = true;
            Salvar();
        }
        // Nenhum item deve nascer selecionado: a seleção passa a depender do mouse.
        Selecionado = null;
        SceneManager.sceneLoaded += AoCarregarCena;
        CriarUI(); AtualizarUI();
    }

    private void OnDestroy()
    {
        if (Instancia == this) SceneManager.sceneLoaded -= AoCarregarCena;
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        GarantirEventSystem();
    }

    private static void GarantirEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(es);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)) Alternar();
        if (Aberto && (Input.GetKeyDown(KeyCode.Escape))) Alternar();

        if (!Aberto) return;

        Vector2 posicaoMouse = ObterPosicaoMouse();

        // Rotação faz parte do drag: fora dele, R não altera nenhum item.
        if (itemArrastado != null && Input.GetKeyDown(KeyCode.R))
        {
            if (Grade.TentarRotacionar(itemArrastado.item)) Salvar();
            AtualizarUI();
        }

        if (itemArrastado == null && BotaoEsquerdoApertadoNesteFrame())
        {
            InventarioGrade.Item itemClicado = EncontrarItemNaTela(posicaoMouse);
            if (itemClicado != null)
            {
                InventarioItemUI view;
                if (views.TryGetValue(itemClicado, out view))
                {
                    itemArrastado = view;
                    itemArrastado.IniciarArraste(posicaoMouse);
                }
            }
            else Selecionar(null);
        }

        if (itemArrastado == null) return;

        // MouseDown iniciou o estado de drag. Enquanto não chegar um MouseUp
        // explícito, o item acompanha o ponteiro em todos os frames.
        itemArrastado.AtualizarArraste(posicaoMouse);
        if (BotaoEsquerdoSoltoNesteFrame())
        {
            itemArrastado.FinalizarArraste(posicaoMouse);
            itemArrastado = null;
        }
    }

    private static Vector2 ObterPosicaoMouse()
    {
        return Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;
    }

    private static bool BotaoEsquerdoApertadoNesteFrame()
    {
        return Mouse.current != null
            ? Mouse.current.leftButton.wasPressedThisFrame
            : Input.GetMouseButtonDown(0);
    }

    private static bool BotaoEsquerdoSoltoNesteFrame()
    {
        return Mouse.current != null
            ? Mouse.current.leftButton.wasReleasedThisFrame
            : Input.GetMouseButtonUp(0);
    }

    public void Alternar()
    {
        if (!Aberto && GameplayState.Instancia != null &&
            !GameplayState.Instancia.PodeAssumirControle(GameplayBlockReason.Inventario))
            return;

        if (Aberto && itemArrastado != null)
        {
            itemArrastado.CancelarArraste();
            itemArrastado = null;
        }
        Aberto = !Aberto;
        if (GameplayState.Instancia != null)
        {
            if (Aberto)
                GameplayState.Instancia.Bloquear(GameplayBlockReason.Inventario);
            else
                GameplayState.Instancia.Liberar(GameplayBlockReason.Inventario);
        }

        canvas.gameObject.SetActive(Aberto);
        Cursor.lockState = Aberto ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = Aberto;
    }
    public bool TentarAdicionar(ItemDefinition definicao)
    {
        return definicao != null && TentarAdicionar(definicao.id, definicao.nome, definicao.largura, definicao.altura, definicao.canRotate);
    }
    public bool TentarAdicionar(string id, string nome, int largura = 1, int altura = 1, bool podeRotacionar = true)
    {
        InventarioGrade.Item item;
        if (Possui(id)) return false;
        bool aceito = Grade.TentarAdicionar(id, nome, largura, altura, out item, podeRotacionar);
        if (aceito) { Selecionado = item; Salvar(); AtualizarUI(); }
        return aceito;
    }
    public bool Possui(string id) { return Grade != null && Grade.Contem(id); }

    public bool RemoverItem(string id)
    {
        if (Grade == null || string.IsNullOrWhiteSpace(id)) return false;

        InventarioGrade.Item item = null;
        foreach (InventarioGrade.Item candidato in Grade.Itens)
        {
            if (string.Equals(candidato.id, id, StringComparison.OrdinalIgnoreCase))
            {
                item = candidato;
                break;
            }
        }

        if (item == null || !Grade.Remover(item)) return false;

        if (Selecionado == item)
            Selecionado = null;

        if (string.Equals(ItemEquipadoId, id, StringComparison.OrdinalIgnoreCase))
            ItemEquipadoId = Possui("lanterna") ? "lanterna" : string.Empty;

        Salvar();
        AtualizarUI();
        return true;
    }

    [ContextMenu("Nova partida (apagar save)")]
    public void NovaPartida()
    {
        if (itemArrastado != null)
        {
            itemArrastado.CancelarArraste();
            itemArrastado = null;
        }

        if (canvas != null)
            canvas.gameObject.SetActive(false);

        Aberto = false;
        if (GameplayState.Instancia != null)
            GameplayState.Instancia.Liberar(GameplayBlockReason.Inventario);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        foreach (InventarioItemUI view in views.Values)
            if (view != null) Destroy(view.gameObject);
        views.Clear();

        PlayerPrefs.DeleteKey(ChaveSave);
        PlayerPrefs.DeleteKey(MecanicaEscalada.ChaveGanchoUsado);
        EstadoMundo.LimparTudo();
        PlayerPrefs.Save();

        Grade = new InventarioGrade(10, 6);
        ItemEquipadoId = "lanterna";
        Selecionado = null;
        TentarAdicionar("lanterna", "Lanterna", 1, 2, true);
    }

    public void Selecionar(InventarioGrade.Item item)
    {
        Selecionado = item;
        if (item != null) ItemEquipadoId = item.id;
        else if (detalhes != null) detalhes.text = "";
        Salvar();
        AtualizarUI();
    }
    public bool Mover(InventarioGrade.Item item, Vector2 tela)
    {
        InventarioItemUI view;
        if (item == null || !views.TryGetValue(item, out view)) return false;
        return Mover(item, view.GetComponent<RectTransform>());
    }

    internal bool Mover(InventarioGrade.Item item, RectTransform itemRect)
    {
        int x, y;
        if (!TentarObterCelulaDoItem(itemRect, item.LarguraAtual, item.AlturaAtual, out x, out y))
        {
            AtualizarUI();
            return false;
        }
        bool ok = Grade.TentarMover(item, x, y); if (ok) Salvar(); AtualizarUI(); return ok;
    }

    internal bool PodeMover(InventarioGrade.Item item, RectTransform itemRect)
    {
        int x, y;
        if (!TentarObterCelulaDoItem(itemRect, item.LarguraAtual, item.AlturaAtual, out x, out y)) return false;
        int ox=item.x, oy=item.y; item.x=x; item.y=y; bool ok=Grade.TentarMover(item,x,y); item.x=ox; item.y=oy; return ok;
    }

    internal bool TentarObterCelula(Vector2 tela, out int x, out int y)
    {
        x = y = -1;
        if (gradeUI == null || !gradeUI.gameObject.activeInHierarchy) return false;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gradeUI, tela, null, out local)) return false;
        Rect grade = gradeUI.rect;
        float larguraCelula = grade.width / Grade.colunas;
        float alturaCelula = grade.height / Grade.linhas;
        if (larguraCelula <= 0f || alturaCelula <= 0f || !grade.Contains(local)) return false;

        x = Mathf.FloorToInt((local.x - grade.xMin) / larguraCelula);
        y = Mathf.FloorToInt((grade.yMax - local.y) / alturaCelula);
        return x >= 0 && y >= 0 && x < Grade.colunas && y < Grade.linhas;
    }

    /// <summary>
    /// Obtém a célula superior esquerda a partir do próprio item visual.
    /// O mouse não participa da indexação: ele só desloca o RectTransform durante o arraste.
    /// </summary>
    internal bool TentarObterCelulaDoItem(RectTransform itemRect, int largura, int altura, out int x, out int y)
    {
        x = y = -1;
        if (itemRect == null || gradeUI == null || !gradeUI.gameObject.activeInHierarchy || largura < 1 || altura < 1) return false;

        Vector3[] cantos = new Vector3[4];
        itemRect.GetWorldCorners(cantos);
        Vector3 centroItem = (cantos[0] + cantos[2]) * 0.5f;
        Vector3 centroLocal = gradeUI.InverseTransformPoint(centroItem);
        Rect grade = gradeUI.rect;
        float larguraCelula = grade.width / Grade.colunas;
        float alturaCelula = grade.height / Grade.linhas;
        if (larguraCelula <= 0f || alturaCelula <= 0f) return false;

        // O centro e o tamanho do item determinam qual quadrado da grade é o seu primeiro.
        x = Mathf.RoundToInt((centroLocal.x - grade.xMin) / larguraCelula - largura * 0.5f);
        y = Mathf.RoundToInt((grade.yMax - centroLocal.y) / alturaCelula - altura * 0.5f);
        return true;
    }

    private InventarioGrade.Item EncontrarItemNaTela(Vector2 tela)
    {
        int x, y;
        if (!TentarObterCelula(tela, out x, out y)) return null;

        foreach (InventarioGrade.Item item in Grade.Itens)
        {
            if (x >= item.x && x < item.x + item.LarguraAtual &&
                y >= item.y && y < item.y + item.AlturaAtual)
                return item;
        }
        return null;
    }
    private void Salvar()
    {
        SaveData data=new SaveData { equipado=ItemEquipadoId };
        foreach(var i in Grade.Itens) data.itens.Add(new SaveItem { id=i.id,nome=i.nome,largura=i.largura,altura=i.altura,x=i.x,y=i.y,rotacionado=i.rotacionado,podeRotacionar=i.podeRotacionar });
        PlayerPrefs.SetString(ChaveSave, JsonUtility.ToJson(data)); PlayerPrefs.Save();
    }
    private void Restaurar()
    {
        if (!PlayerPrefs.HasKey(ChaveSave)) return;
        try { var data=JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(ChaveSave)); if(data==null||data.itens==null)return; foreach(var s in data.itens) { if(string.IsNullOrEmpty(s.id)||s.largura<1||s.altura<1)continue; Grade.TentarRestaurar(new InventarioGrade.Item { id=s.id,nome=s.nome,largura=s.largura,altura=s.altura,x=s.x,y=s.y,rotacionado=s.rotacionado,podeRotacionar=s.podeRotacionar }); } ItemEquipadoId=string.IsNullOrEmpty(data.equipado)?"lanterna":data.equipado; }
        catch (Exception) { PlayerPrefs.DeleteKey(ChaveSave); }
    }

    private void CriarUI()
    {
        GameObject root = new GameObject("MaletaUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(root);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        root.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        Image fundo = CriarImagem(root.transform, "Fundo", new Color(0.02f, .025f, .02f, .92f));
        fundo.raycastTarget = false;
        RectTransform fr = fundo.rectTransform; fr.anchorMin = fr.anchorMax = new Vector2(.5f, .5f); fr.sizeDelta = new Vector2(640, 470);
        Text titulo = CriarTexto(root.transform, "INVENTÁRIO  [TAB / I / ESC] fechar    R rotaciona", 20); titulo.rectTransform.anchoredPosition = new Vector2(0, 205);
        detalhes = CriarTexto(root.transform, "", 16); detalhes.rectTransform.anchoredPosition = new Vector2(0, -205);
        Image painel = CriarImagem(root.transform, "Grade", new Color(.10f, .13f, .10f, 1)); gradeUI = painel.rectTransform; gradeUI.anchorMin = gradeUI.anchorMax = new Vector2(.5f, .5f); gradeUI.sizeDelta = new Vector2(10 * TamanhoCelula, 6 * TamanhoCelula);
        painel.raycastTarget = false;
        for (int y = 0; y < 6; y++) for (int x = 0; x < 10; x++) { Image celula = CriarImagem(gradeUI, "Celula", new Color(.22f, .28f, .20f, 1)); celula.raycastTarget = false; RectTransform r = celula.rectTransform; r.anchorMin = r.anchorMax = new Vector2(.5f,.5f); r.sizeDelta = new Vector2(TamanhoCelula - 3, TamanhoCelula - 3); r.anchoredPosition = Posicao(x,y,1,1); }
        canvas.gameObject.SetActive(false);
    }
    private void AtualizarUI()
    {
        if (Grade == null || gradeUI == null) return;
        var removidos = new List<InventarioGrade.Item>(); foreach (var par in views) if (!Grade.Itens.Contains(par.Key)) { Destroy(par.Value.gameObject); removidos.Add(par.Key); } foreach (var i in removidos) views.Remove(i);
        foreach (var item in Grade.Itens) { InventarioItemUI view; if (!views.TryGetValue(item, out view)) { GameObject o = new GameObject("Item_" + item.nome, typeof(Image), typeof(CanvasGroup), typeof(InventarioItemUI)); o.transform.SetParent(gradeUI, false); view = o.GetComponent<InventarioItemUI>(); view.sistema = this; view.item = item; views.Add(item, view); } view.Configurar(item == Selecionado); }
    }
    private Vector2 Posicao(int x, int y, int w, int h) { return new Vector2(-gradeUI.rect.width*.5f + (x + w*.5f)*TamanhoCelula, gradeUI.rect.height*.5f - (y + h*.5f)*TamanhoCelula); }
    private static Image CriarImagem(Transform pai, string nome, Color cor) { GameObject o = new GameObject(nome, typeof(Image)); o.transform.SetParent(pai, false); Image i = o.GetComponent<Image>(); i.color = cor; return i; }
    private static Text CriarTexto(Transform pai, string valor, int tamanho) { GameObject o = new GameObject("Texto", typeof(Text)); o.transform.SetParent(pai, false); Text t = o.GetComponent<Text>(); t.text = valor; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = tamanho; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false; RectTransform r=t.rectTransform; r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.sizeDelta=new Vector2(600,40); return t; }
    internal void ReposicionarView(InventarioItemUI v) { RectTransform r=v.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.sizeDelta=new Vector2(v.item.LarguraAtual*TamanhoCelula-6,v.item.AlturaAtual*TamanhoCelula-6); r.anchoredPosition=Posicao(v.item.x,v.item.y,v.item.LarguraAtual,v.item.AlturaAtual); if(detalhes!=null && v.item==Selecionado) detalhes.text=v.item.nome+"  ("+v.item.LarguraAtual+"x"+v.item.AlturaAtual+")"; }
}

public class InventarioItemUI : MonoBehaviour
{
    public SistemaInventario sistema; public InventarioGrade.Item item; private CanvasGroup grupo;
    private Vector2 deslocamentoVisual;
    private bool arrastando;
    public void Configurar(bool selecionado) { Image i=GetComponent<Image>(); i.color=selecionado?new Color(.95f,.75f,.18f,1):new Color(.35f,.55f,.28f,1); sistema.ReposicionarView(this); if (grupo == null) { grupo = gameObject.GetComponent<CanvasGroup>(); if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>(); } }

    public void IniciarArraste(Vector2 posicaoTela)
    {
        sistema.Selecionar(item);
        deslocamentoVisual = (Vector2)GetComponent<RectTransform>().position - posicaoTela;
        transform.SetAsLastSibling();
        grupo.blocksRaycasts = false;
        arrastando = true;
        AtualizarArraste(posicaoTela);
    }

    public void AtualizarArraste(Vector2 posicaoTela)
    {
        if (!arrastando) return;
        transform.position = posicaoTela + deslocamentoVisual;
        GetComponent<Image>().color = sistema.PodeMover(item, GetComponent<RectTransform>())
            ? new Color(.25f,.9f,.3f,1)
            : new Color(.9f,.2f,.2f,1);
    }

    public void FinalizarArraste(Vector2 posicaoTela)
    {
        if (!arrastando) return;
        arrastando = false;
        grupo.blocksRaycasts = true;
        sistema.Mover(item, GetComponent<RectTransform>());
        sistema.Selecionar(null);
    }

    public void CancelarArraste()
    {
        if (!arrastando) return;
        arrastando = false;
        grupo.blocksRaycasts = true;
        sistema.ReposicionarView(this);
        sistema.Selecionar(null);
    }
}
