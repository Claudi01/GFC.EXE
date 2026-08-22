using UnityEngine;

public class PosicionadorPlayerCena : MonoBehaviour
{
    private void Start()
    {
        string idEntrada = ContextoTransicaoCena.ConsumirEntrada();
        if (string.IsNullOrWhiteSpace(idEntrada))
            return;

        PontoEntradaCena ponto = EncontrarPonto(idEntrada);
        if (ponto == null)
        {
            Debug.LogError("Nao foi encontrado o ponto de entrada '" + idEntrada + "' na cena atual.", this);
            return;
        }

        CharacterController characterController = GetComponent<CharacterController>();
        bool controllerEstavaAtivo = characterController != null && characterController.enabled;

        if (controllerEstavaAtivo)
            characterController.enabled = false;

        transform.SetPositionAndRotation(ponto.transform.position, ponto.transform.rotation);

        if (controllerEstavaAtivo)
            characterController.enabled = true;
    }

    private static PontoEntradaCena EncontrarPonto(string idEntrada)
    {
        PontoEntradaCena[] pontos = FindObjectsByType<PontoEntradaCena>(FindObjectsSortMode.None);
        foreach (PontoEntradaCena ponto in pontos)
        {
            if (ponto != null && ponto.idEntrada == idEntrada)
                return ponto;
        }

        return null;
    }
}
