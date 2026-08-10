using UnityEngine;

[CreateAssetMenu(menuName = "Inventario/Definicao de Item", fileName = "NovoItem")]
public class ItemDefinition : ScriptableObject
{
    public string id = "item";
    public string nome = "Item";
    [TextArea] public string descricao;
    public Sprite icone;
    [Min(1)] public int largura = 1;
    [Min(1)] public int altura = 1;
    public bool canRotate = true;
}
