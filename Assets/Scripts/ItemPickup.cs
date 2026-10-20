using UnityEngine;

/// <summary>
/// Pickup generico. Pode usar um ItemDefinition ou os campos manuais abaixo.
/// Depois de coletado, o objeto pode ser destruido pelo sistema de interacao.
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Header("Definicao preferencial")]
    public ItemDefinition item;

    [Min(1)] public int quantidade = 1;

    [Header("Fallback manual")]
    [Tooltip("Use estes campos somente quando nao houver ItemDefinition.")]
    public string idManual = "";
    public string nomeManual = "Item";
    [Min(1)] public int larguraManual = 1;
    [Min(1)] public int alturaManual = 1;
    public bool podeRotacionarManual = true;

    [Header("Persistencia do mundo")]
    [Tooltip("Chave unica opcional. Preenchida, impede que este pickup reapareca depois de coletado.")]
    public string chaveColeta = "";

    private bool coletado;

    private void Awake()
    {
        if (EstadoMundo.EstaConcluido(chaveColeta) || JaPossui)
            gameObject.SetActive(false);
    }

    public string Id
    {
        get { return item != null ? item.id : idManual; }
    }

    public string Nome
    {
        get { return item != null ? item.nome : nomeManual; }
    }

    public bool JaPossui
    {
        get
        {
            return (item == null || !item.empilhavel) && SistemaInventario.Instancia != null &&
                   SistemaInventario.Instancia.Possui(Id);
        }
    }

    public bool TentarPegar()
    {
        if (coletado || !isActiveAndEnabled || EstadoMundo.EstaConcluido(chaveColeta) ||
            SistemaInventario.Instancia == null || string.IsNullOrWhiteSpace(Id))
            return false;

        if (JaPossui)
            return false;

        bool aceito = SistemaInventario.Instancia.TentarAdicionar(
            Id,
            Nome,
            item != null ? item.largura : larguraManual,
            item != null ? item.altura : alturaManual,
            item != null ? item.canRotate : podeRotacionarManual,
            quantidade,
            item != null && item.empilhavel);

        if (aceito)
        {
            coletado = true;
            EstadoMundo.MarcarConcluido(chaveColeta);
        }

        return aceito;
    }
}
