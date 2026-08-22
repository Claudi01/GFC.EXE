using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameplayBlockReason
{
    Inventario = 0,
    Diario = 1,
    TransicaoCena = 2
}

/// <summary>
/// Centraliza os motivos que podem bloquear a jogabilidade.
/// O objeto e criado automaticamente e permanece entre cenas.
/// </summary>
public sealed class GameplayState : MonoBehaviour
{
    public static GameplayState Instancia { get; private set; }

    private int bloqueios;

    public bool EstaBloqueado
    {
        get { return bloqueios != 0; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CriarAutomaticamente()
    {
        if (Instancia != null || FindFirstObjectByType<GameplayState>() != null)
            return;

        new GameObject("GameplayState").AddComponent<GameplayState>();
    }

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    private void OnDestroy()
    {
        if (Instancia == this)
        {
            SceneManager.sceneLoaded -= AoCarregarCena;
            Instancia = null;
        }
    }

    public bool EstaBloqueadoPor(GameplayBlockReason motivo)
    {
        return (bloqueios & Mascara(motivo)) != 0;
    }

    public bool PodeAssumirControle(GameplayBlockReason motivo)
    {
        int outrosBloqueios = bloqueios & ~Mascara(motivo);
        return outrosBloqueios == 0;
    }

    public void Bloquear(GameplayBlockReason motivo)
    {
        bloqueios |= Mascara(motivo);
    }

    public void Liberar(GameplayBlockReason motivo)
    {
        bloqueios &= ~Mascara(motivo);
    }

    private static int Mascara(GameplayBlockReason motivo)
    {
        return 1 << (int)motivo;
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        // Nenhum bloqueio da cena anterior pode contaminar a cena nova.
        bloqueios = 0;
    }
}
