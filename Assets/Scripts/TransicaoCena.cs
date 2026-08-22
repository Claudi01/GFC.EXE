using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransicaoCena : MonoBehaviour
{
    [Header("Destino")]
    public string nomeCenaDestino = "";
    public string idEntradaDestino = "";

    [Header("Efeitos")]
    public FaderScript fader;

    private bool transicaoIniciada;

    public bool ValidarConfiguracao(out string mensagem)
    {
        if (string.IsNullOrWhiteSpace(nomeCenaDestino))
        {
            mensagem = "A cena de destino nao foi configurada.";
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(nomeCenaDestino))
        {
            mensagem = "A cena de destino nao esta disponivel na Build Settings: " + nomeCenaDestino;
            return false;
        }

        if (string.IsNullOrWhiteSpace(idEntradaDestino))
        {
            mensagem = "O ponto de entrada da cena de destino nao foi configurado.";
            return false;
        }

        mensagem = string.Empty;
        return true;
    }

    public bool PodeIniciar(out string mensagem)
    {
        if (transicaoIniciada)
        {
            mensagem = "Esta transicao de cena ja foi iniciada.";
            return false;
        }

        if (!ValidarConfiguracao(out mensagem))
            return false;

        if (GameplayState.Instancia != null &&
            !GameplayState.Instancia.PodeAssumirControle(GameplayBlockReason.TransicaoCena))
        {
            mensagem = "Outro bloqueio de gameplay esta ativo.";
            return false;
        }

        return true;
    }

    public bool IniciarTransicao()
    {
        string mensagem;
        if (!PodeIniciar(out mensagem))
        {
            Debug.LogError(mensagem, this);
            return false;
        }

        transicaoIniciada = true;
        if (GameplayState.Instancia != null)
            GameplayState.Instancia.Bloquear(GameplayBlockReason.TransicaoCena);

        StartCoroutine(ProcessoTransicao());
        return true;
    }

    private IEnumerator ProcessoTransicao()
    {
        if (fader != null)
        {
            fader.gameObject.SetActive(true);
            yield return StartCoroutine(fader.FazerFade(true));
        }

        ContextoTransicaoCena.DefinirEntrada(idEntradaDestino);
        SceneManager.LoadScene(nomeCenaDestino);
    }
}
