using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MecanicaEscalada : MonoBehaviour
{
    public const string ChaveGanchoUsado = "gfc.progresso.gancho.usado";

    [Header("Configuração de Cena")]
    public string nomeProximaCena = "Aréa de Carga";

    [Header("Item consumido")]
    public string idItemConsumido = "gancho";

    [Header("Efeitos")]
    public FaderScript fader; // Arraste a UI_Preta aqui

    public void IniciarEscalada()
    {
        StartCoroutine(ProcessoEscalada());
    }

    IEnumerator ProcessoEscalada()
    {
        if (SistemaInventario.Instancia == null ||
            !SistemaInventario.Instancia.RemoverItem(idItemConsumido))
        {
            Debug.LogError("Não foi possível consumir o item necessário para a escalada: " + idItemConsumido);
            yield break;
        }

        PlayerPrefs.SetInt(ChaveGanchoUsado, 1);
        PlayerPrefs.Save();

        if (fader != null)
        {
            fader.gameObject.SetActive(true);
            yield return StartCoroutine(fader.FazerFade(true));
        }
        
        SceneManager.LoadScene(nomeProximaCena);
    }
}
