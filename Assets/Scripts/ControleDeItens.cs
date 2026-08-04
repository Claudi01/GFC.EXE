using System.Collections;
using UnityEngine;

public class ControleDeItens : MonoBehaviour
{
    [Header("Objetos na Câmera")]
    public GameObject lanterna;
    public GameObject ganchoNaMao;

    private bool possuiGancho = false;
    private int itemEquipado = 0; // 0 = Lanterna, 1 = Gancho
    private bool trocando = false;

    void Start()
    {
        // O estado visual e restaurado pelo item equipado persistente; nao force lanterna em toda cena.
        bool gancho = SistemaInventario.Instancia != null && SistemaInventario.Instancia.ItemEquipadoId == "gancho" && possuiGancho;
        if (lanterna != null) lanterna.SetActive(!gancho);
        if (ganchoNaMao != null) ganchoNaMao.SetActive(gancho);
        itemEquipado = gancho ? 1 : 0;
    }

    void Awake()
    {
        // Mantem a compatibilidade com saves/cenas antigas e restaura o equipamento ao trocar de cena.
        possuiGancho = SistemaInventario.Instancia != null && SistemaInventario.Instancia.Possui("gancho");
    }

    void Update()
    {
        if (SistemaInventario.Instancia != null && SistemaInventario.Instancia.Aberto) return;

        if (possuiGancho && !trocando)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                StartCoroutine(AnimacaoDeTroca());
            }
        }
    }

    public void PegarGancho()
    {
        if (SistemaInventario.Instancia != null && !SistemaInventario.Instancia.Possui("gancho") &&
            !SistemaInventario.Instancia.TentarAdicionar("gancho", "Gancho", 2, 1)) return;
        possuiGancho = true;
        if (!trocando)
        {
            StartCoroutine(FazerTransicao(1));
        }
        Debug.Log("Item adicionado ao inventário!");
    }

    public bool TryPegarGancho()
    {
        if (SistemaInventario.Instancia == null) return false;
        if (!SistemaInventario.Instancia.Possui("gancho") && !SistemaInventario.Instancia.TentarAdicionar("gancho", "Gancho", 2, 1)) return false;
        PegarGancho(); return true;
    }

    IEnumerator AnimacaoDeTroca()
    {
        int proximoItem = (itemEquipado == 0) ? 1 : 0;
        yield return StartCoroutine(FazerTransicao(proximoItem));
    }

    IEnumerator FazerTransicao(int alvoItem)
    {
        trocando = true;
        GameObject saindo = (itemEquipado == 0) ? lanterna : ganchoNaMao;
        GameObject entrando = (alvoItem == 0) ? lanterna : ganchoNaMao;

        Vector3 posOriginalSaindo = saindo.transform.localPosition;
        Vector3 posEscondidaSaindo = posOriginalSaindo + new Vector3(0, -0.6f, 0);

        Vector3 posOriginalEntrando = entrando.transform.localPosition;
        Vector3 posEscondidaEntrando = posOriginalEntrando + new Vector3(0, -0.6f, 0);

        while (Vector3.Distance(saindo.transform.localPosition, posEscondidaSaindo) > 0.01f)
        {
            saindo.transform.localPosition = Vector3.Lerp(saindo.transform.localPosition, posEscondidaSaindo, Time.deltaTime * 12f);
            yield return null;
        }

        saindo.SetActive(false);
        saindo.transform.localPosition = posOriginalSaindo;

        entrando.transform.localPosition = posEscondidaEntrando;
        entrando.SetActive(true);

        while (Vector3.Distance(entrando.transform.localPosition, posOriginalEntrando) > 0.01f)
        {
            entrando.transform.localPosition = Vector3.Lerp(entrando.transform.localPosition, posOriginalEntrando, Time.deltaTime * 12f);
            yield return null;
        }

        entrando.transform.localPosition = posOriginalEntrando;
        itemEquipado = alvoItem;
        if (SistemaInventario.Instancia != null)
            SistemaInventario.Instancia.Selecionar(alvoItem == 1 ? EncontrarItem("gancho") : EncontrarItem("lanterna"));
        trocando = false;
    }

    private InventarioGrade.Item EncontrarItem(string id)
    {
        if (SistemaInventario.Instancia == null) return null;
        foreach (InventarioGrade.Item item in SistemaInventario.Instancia.Grade.Itens)
            if (item.id == id) return item;
        return null;
    }

    // A mágica nova: avisa o sistema se o jogador está segurando o gancho agora
    public bool GanchoEquipado()
    {
        return itemEquipado == 1;
    }
}
