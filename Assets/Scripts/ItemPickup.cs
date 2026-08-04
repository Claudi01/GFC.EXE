using UnityEngine;

/// <summary>Pickup generico: pode ser colocado em qualquer objeto com Collider.</summary>
public class ItemPickup : MonoBehaviour
{
    public ItemDefinition item;
    public bool TentarPegar()
    {
        if (item == null || SistemaInventario.Instancia == null) return false;
        // O mundo tambem deve refletir um item restaurado do save.
        return SistemaInventario.Instancia.Possui(item.id) || SistemaInventario.Instancia.TentarAdicionar(item);
    }
}
