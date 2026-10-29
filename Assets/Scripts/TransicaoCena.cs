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
    [Tooltip("Opcional. Toca depois que a tela fica preta, antes de carregar a proxima cena.")]
    public AudioClip somAoEscurecer;
    [Range(0f, 1f)] public float volumeSomAoEscurecer = 0.35f;
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
        if (transicaoIniciada) { mensagem = "Esta transicao de cena ja foi iniciada."; return false; }
        if (!ValidarConfiguracao(out mensagem)) return false;
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
        if (!PodeIniciar(out string mensagem)) { Debug.LogError(mensagem, this); return false; }
        transicaoIniciada = true;
        if (GameplayState.Instancia != null) GameplayState.Instancia.Bloquear(GameplayBlockReason.TransicaoCena);
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
        if (somAoEscurecer != null)
        {
            AudioSource fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f;
            fonte.dopplerLevel = 0f;
            fonte.volume = volumeSomAoEscurecer;
            fonte.clip = somAoEscurecer;
            fonte.Play();
            yield return new WaitForSecondsRealtime(somAoEscurecer.length);
        }
        ContextoTransicaoCena.DefinirEntrada(idEntradaDestino);
        SceneManager.LoadScene(nomeCenaDestino);
    }
}
