using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MecanicaEscalada : MonoBehaviour
{
    public const string ChaveGanchoUsado = "gfc.progresso.gancho.usado";

    private bool transicaoIniciada;

    [Header("Configuração de Cena")]
    public string nomeProximaCena = "Aréa de Carga";

    [Header("Item consumido")]
    public string idItemConsumido = "gancho";

    [Header("Efeitos")]
    public FaderScript fader; // Arraste a UI_Preta aqui

    public void IniciarEscalada()
    {
        if (transicaoIniciada)
        {
            Debug.Log("A transição de escalada já foi iniciada.", this);
            return;
        }

        if (GameplayState.Instancia != null &&
            !GameplayState.Instancia.PodeAssumirControle(GameplayBlockReason.TransicaoCena))
            return;

        transicaoIniciada = true;
        if (GameplayState.Instancia != null)
            GameplayState.Instancia.Bloquear(GameplayBlockReason.TransicaoCena);

        StartCoroutine(ProcessoEscalada());
    }

    IEnumerator ProcessoEscalada()
    {
        if (string.IsNullOrWhiteSpace(nomeProximaCena))
        {
            FalharEscalada("A cena de destino da escalada não foi configurada.");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(nomeProximaCena))
        {
            FalharEscalada("A cena de destino não está disponível na Build Settings: " + nomeProximaCena);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(idItemConsumido))
        {
            FalharEscalada("O ID do item necessário para a escalada não foi configurado.");
            yield break;
        }

        if (SistemaInventario.Instancia == null)
        {
            FalharEscalada("O SistemaInventario não está disponível para a escalada.");
            yield break;
        }

        if (!SistemaInventario.Instancia.Possui(idItemConsumido))
        {
            FalharEscalada("O jogador não possui o item necessário para a escalada: " + idItemConsumido);
            yield break;
        }

        // O item só é consumido depois que a cena e o inventário foram validados.
        if (!SistemaInventario.Instancia.RemoverItem(idItemConsumido))
        {
            FalharEscalada("Não foi possível consumir o item necessário para a escalada: " + idItemConsumido);
            yield break;
        }

        EstadoMundo.MarcarConcluido(ChaveGanchoUsado);

        if (fader != null)
        {
            fader.gameObject.SetActive(true);
            yield return StartCoroutine(fader.FazerFade(true));
        }
        
        SceneManager.LoadScene(nomeProximaCena);
    }

    private void FalharEscalada(string mensagem)
    {
        transicaoIniciada = false;
        if (GameplayState.Instancia != null)
            GameplayState.Instancia.Liberar(GameplayBlockReason.TransicaoCena);

        Debug.LogError(mensagem, this);
    }
}
