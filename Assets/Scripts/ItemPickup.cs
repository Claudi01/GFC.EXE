using UnityEngine;

/// <summary>
/// Pickup generico. Pode usar um ItemDefinition ou os campos manuais abaixo.
/// Depois de coletado, o objeto pode ser destruido pelo sistema de interacao.
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Header("Definicao preferencial")]
    public ItemDefinition item;

    [Header("Fallback manual")]
    [Tooltip("Use estes campos somente quando nao houver ItemDefinition.")]
    public string idManual = "";
    public string nomeManual = "Item";
    [Min(1)] public int larguraManual = 1;
    [Min(1)] public int alturaManual = 1;
    public bool podeRotacionarManual = true;

    private bool coletado;

    public string Id
    {
        get { return item != null ? item.id : idManual; }
    }

    public string Nome
    {
        get { return item != null ? item.nome : nomeManual; }
    }

    public bool TentarPegar()
    {
        if (coletado || SistemaInventario.Instancia == null || string.IsNullOrWhiteSpace(Id))
            return false;

        bool aceito = SistemaInventario.Instancia.Possui(Id) ||
                      SistemaInventario.Instancia.TentarAdicionar(
                          Id,
                          Nome,
                          item != null ? item.largura : larguraManual,
                          item != null ? item.altura : alturaManual,
                          item != null ? item.canRotate : podeRotacionarManual);

        if (aceito)
            coletado = true;

        return aceito;
    }
}
