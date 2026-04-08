using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MecanicaEscalada : MonoBehaviour
{
    [Header("Configuração de Cena")]
    public string nomeProximaCena = "1 parte - navio"; // Nome exato do seu print

    [Header("Efeitos")]
    public FaderScript fader; // Arraste a UI_Preta aqui

    public void IniciarEscalada()
    {
        StartCoroutine(ProcessoEscalada());
    }

    IEnumerator ProcessoEscalada()
    {
        fader.gameObject.SetActive(true);
        yield return StartCoroutine(fader.FazerFade(true)); // Escurece tudo
        
        SceneManager.LoadScene(nomeProximaCena); // Teleporta o player
    }
}