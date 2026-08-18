using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MecanicaEscalada : MonoBehaviour
{
    [Header("Configuração de Cena")]
    public string nomeProximaCena = "Aréa de Carga";

    [Header("Efeitos")]
    public FaderScript fader; // Arraste a UI_Preta aqui

    public void IniciarEscalada()
    {
        StartCoroutine(ProcessoEscalada());
    }

    IEnumerator ProcessoEscalada()
    {
        if (fader != null)
        {
            fader.gameObject.SetActive(true);
            yield return StartCoroutine(fader.FazerFade(true));
        }
        
        SceneManager.LoadScene(nomeProximaCena);
    }
}
