using UnityEngine;

/// <summary>
/// Mostra um modelo decorativo quando uma etapa da partida foi concluida.
/// O objeto que possui este componente deve permanecer ativo; apenas o modelo
/// visual deve ser colocado no campo "modelo".
/// </summary>
public class ItemDeixadoNoCenario : MonoBehaviour
{
    [Header("Estado da partida")]
    [Tooltip("Chave PlayerPrefs que indica quando o item deve aparecer.")]
    [SerializeField] private string chaveEstado = MecanicaEscalada.ChaveGanchoUsado;

    [Header("Visual")]
    [SerializeField] private GameObject modelo;

    private void Awake()
    {
        AtualizarVisual();
    }

    public void AtualizarVisual()
    {
        if (modelo == null)
            return;

        modelo.SetActive(PlayerPrefs.GetInt(chaveEstado, 0) == 1);
    }
}
