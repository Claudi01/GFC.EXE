using UnityEngine;

/// <summary>Interacao por mira + E, sem criar uma tag para cada puzzle.</summary>
public abstract class InteragivelCenario : MonoBehaviour
{
    public abstract string ObterTextoInteracao();
    public abstract void Interagir();

    protected bool PodeInteragir
    {
        get { return isActiveAndEnabled && (GameplayState.Instancia == null || !GameplayState.Instancia.EstaBloqueado); }
    }
}
