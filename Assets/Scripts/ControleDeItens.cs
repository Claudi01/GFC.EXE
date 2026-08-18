using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControleDeItens : MonoBehaviour
{
    [Serializable]
    public class ConfiguracaoItemEquipavel
    {
        [Header("Dados do item")]
        [Tooltip("Opcional. Se preenchido, os dados abaixo serao lidos da definicao.")]
        public ItemDefinition definicao;
        public string id = "item";
        public string nome = "Item";

        [Header("Visual na mao")]
        public GameObject objetoNaMao;

        [Header("Tamanho no inventario")]
        [Min(1)] public int larguraInventario = 1;
        [Min(1)] public int alturaInventario = 1;
        public bool podeRotacionar = true;

        public string Id
        {
            get { return definicao != null ? definicao.id : id; }
        }

        public string Nome
        {
            get { return definicao != null ? definicao.nome : nome; }
        }

        public int Largura
        {
            get { return definicao != null ? definicao.largura : larguraInventario; }
        }

        public int Altura
        {
            get { return definicao != null ? definicao.altura : alturaInventario; }
        }

        public bool PodeRotacionar
        {
            get { return definicao != null ? definicao.canRotate : podeRotacionar; }
        }
    }

    [Header("Objetos antigos na camera")]
    [Tooltip("Mantidos para nao quebrar as cenas atuais. Novos itens devem ser adicionados na lista abaixo.")]
    public GameObject lanterna;
    public GameObject ganchoNaMao;
    public GameObject peDeCabraNaMao;

    [Header("Catalogo de itens adicionais")]
    [Tooltip("Adicione aqui os proximos itens equipaveis sem precisar alterar o codigo.")]
    [SerializeField] private List<ConfiguracaoItemEquipavel> itensAdicionais = new List<ConfiguracaoItemEquipavel>();

    private readonly List<ConfiguracaoItemEquipavel> catalogo = new List<ConfiguracaoItemEquipavel>();
    private int itemEquipado = -1;
    private bool trocando;

    private void Awake()
    {
        MontarCatalogo();
    }

    private void Start()
    {
        int alvo = EncontrarIndice(SistemaInventario.Instancia != null
            ? SistemaInventario.Instancia.ItemEquipadoId
            : "lanterna");

        if (!PodeEquipar(alvo))
            alvo = EncontrarPrimeiroPossuido();

        if (alvo < 0)
            alvo = EncontrarIndice("lanterna");

        if (alvo >= 0 && TemVisual(alvo))
            AplicarVisualInstantaneo(alvo);
        else
            DesativarTodosOsVisuais();
    }

    private void Update()
    {
        if (SistemaInventario.Instancia != null && SistemaInventario.Instancia.Aberto)
            return;

        if (trocando || catalogo.Count <= 1)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f)
            return;

        int direcao = scroll > 0f ? 1 : -1;
        int proximo = EncontrarProximoPossuido(direcao);
        if (proximo >= 0 && proximo != itemEquipado)
            StartCoroutine(FazerTransicao(proximo));
    }

    private void MontarCatalogo()
    {
        catalogo.Clear();

        // Compatibilidade com as cenas existentes.
        AdicionarSeAindaNaoExiste(CriarConfiguracaoLegada("lanterna", "Lanterna", lanterna, 1, 2, true));
        AdicionarSeAindaNaoExiste(CriarConfiguracaoLegada("gancho", "Gancho", ganchoNaMao, 2, 1, true));
        AdicionarSeAindaNaoExiste(CriarConfiguracaoLegada("pe_de_cabra", "Pe de cabra", peDeCabraNaMao, 2, 1, true));

        if (itensAdicionais == null)
            return;

        foreach (ConfiguracaoItemEquipavel item in itensAdicionais)
            AdicionarSeAindaNaoExiste(item);
    }

    private static ConfiguracaoItemEquipavel CriarConfiguracaoLegada(
        string id, string nome, GameObject visual, int largura, int altura, bool podeRotacionar)
    {
        return new ConfiguracaoItemEquipavel
        {
            id = id,
            nome = nome,
            objetoNaMao = visual,
            larguraInventario = largura,
            alturaInventario = altura,
            podeRotacionar = podeRotacionar
        };
    }

    private void AdicionarSeAindaNaoExiste(ConfiguracaoItemEquipavel item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Id))
            return;

        if (EncontrarIndice(item.Id) < 0)
            catalogo.Add(item);
    }

    public bool TryPegarItem(string id, string nome, int largura = 1, int altura = 1, bool podeRotacionar = true)
    {
        if (SistemaInventario.Instancia == null || string.IsNullOrWhiteSpace(id))
            return false;

        bool jaPossui = SistemaInventario.Instancia.Possui(id);
        if (!jaPossui && !SistemaInventario.Instancia.TentarAdicionar(id, nome, largura, altura, podeRotacionar))
            return false;

        EquiparItem(id);
        return true;
    }

    public void PegarGancho()
    {
        TryPegarItem("gancho", "Gancho", 2, 1, true);
    }

    public bool TryPegarGancho()
    {
        return TryPegarItem("gancho", "Gancho", 2, 1, true);
    }

    public bool EquiparItem(string id)
    {
        int alvo = EncontrarIndice(id);
        if (!PodeEquipar(alvo) || trocando)
            return false;

        if (itemEquipado == alvo)
        {
            AplicarVisualInstantaneo(alvo);
            return true;
        }

        StartCoroutine(FazerTransicao(alvo));
        return true;
    }

    public bool GanchoEquipado()
    {
        return itemEquipado >= 0 && itemEquipado < catalogo.Count && catalogo[itemEquipado].Id == "gancho";
    }

    private int EncontrarIndice(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return -1;

        for (int i = 0; i < catalogo.Count; i++)
        {
            if (string.Equals(catalogo[i].Id, id, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private int EncontrarPrimeiroPossuido()
    {
        for (int i = 0; i < catalogo.Count; i++)
        {
            if (PodeEquipar(i))
                return i;
        }

        return -1;
    }

    private int EncontrarProximoPossuido(int direcao)
    {
        if (catalogo.Count == 0)
            return -1;

        int atual = itemEquipado < 0 ? 0 : itemEquipado;
        for (int passo = 1; passo <= catalogo.Count; passo++)
        {
            int candidato = (atual + direcao * passo) % catalogo.Count;
            if (candidato < 0)
                candidato += catalogo.Count;

            if (PodeEquipar(candidato))
                return candidato;
        }

        return -1;
    }

    private bool PodeEquipar(int indice)
    {
        if (indice < 0 || indice >= catalogo.Count || !TemVisual(indice))
            return false;

        return SistemaInventario.Instancia != null &&
               SistemaInventario.Instancia.Possui(catalogo[indice].Id);
    }

    private bool TemVisual(int indice)
    {
        return indice >= 0 && indice < catalogo.Count && catalogo[indice].objetoNaMao != null;
    }

    private IEnumerator FazerTransicao(int alvo)
    {
        if (!TemVisual(alvo))
            yield break;

        trocando = true;

        GameObject entrando = catalogo[alvo].objetoNaMao;
        GameObject saindo = itemEquipado >= 0 && itemEquipado < catalogo.Count
            ? catalogo[itemEquipado].objetoNaMao
            : null;

        if (saindo == null || saindo == entrando)
        {
            AplicarVisualInstantaneo(alvo);
            trocando = false;
            yield break;
        }

        Vector3 posOriginalSaindo = saindo.transform.localPosition;
        Vector3 posEscondidaSaindo = posOriginalSaindo + new Vector3(0f, -0.6f, 0f);

        Vector3 posOriginalEntrando = entrando.transform.localPosition;
        Vector3 posEscondidaEntrando = posOriginalEntrando + new Vector3(0f, -0.6f, 0f);

        saindo.SetActive(true);
        while (Vector3.Distance(saindo.transform.localPosition, posEscondidaSaindo) > 0.01f)
        {
            saindo.transform.localPosition = Vector3.Lerp(
                saindo.transform.localPosition, posEscondidaSaindo, Time.deltaTime * 12f);
            yield return null;
        }

        saindo.SetActive(false);
        saindo.transform.localPosition = posOriginalSaindo;

        entrando.transform.localPosition = posEscondidaEntrando;
        entrando.SetActive(true);

        while (Vector3.Distance(entrando.transform.localPosition, posOriginalEntrando) > 0.01f)
        {
            entrando.transform.localPosition = Vector3.Lerp(
                entrando.transform.localPosition, posOriginalEntrando, Time.deltaTime * 12f);
            yield return null;
        }

        entrando.transform.localPosition = posOriginalEntrando;
        itemEquipado = alvo;
        SalvarItemEquipado();
        trocando = false;
    }

    private void AplicarVisualInstantaneo(int alvo)
    {
        DesativarTodosOsVisuais();
        catalogo[alvo].objetoNaMao.SetActive(true);
        itemEquipado = alvo;
        SalvarItemEquipado();
    }

    private void DesativarTodosOsVisuais()
    {
        foreach (ConfiguracaoItemEquipavel item in catalogo)
        {
            if (item != null && item.objetoNaMao != null)
                item.objetoNaMao.SetActive(false);
        }
    }

    private void SalvarItemEquipado()
    {
        if (SistemaInventario.Instancia == null || itemEquipado < 0 || itemEquipado >= catalogo.Count)
            return;

        InventarioGrade.Item item = EncontrarItem(catalogo[itemEquipado].Id);
        if (item != null)
            SistemaInventario.Instancia.Selecionar(item);
    }

    private InventarioGrade.Item EncontrarItem(string id)
    {
        if (SistemaInventario.Instancia == null || SistemaInventario.Instancia.Grade == null)
            return null;

        foreach (InventarioGrade.Item item in SistemaInventario.Instancia.Grade.Itens)
        {
            if (item.id == id)
                return item;
        }

        return null;
    }
}
