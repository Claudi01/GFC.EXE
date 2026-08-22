using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TransicaoCena))]
public class MecanicaEscalada : MonoBehaviour
{
    public const string ChaveGanchoUsado = "gfc.progresso.gancho.usado";

    private bool transicaoIniciada;
    private TransicaoCena transicaoCena;

    [Header("Item consumido")]
    public string idItemConsumido = "gancho";

    private void Awake()
    {
        transicaoCena = GetComponent<TransicaoCena>();
    }

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
        if (transicaoCena == null)
        {
            FalharEscalada("O componente de transicao de cena nao esta disponivel.");
            yield break;
        }

        string mensagemTransicao;
        if (!transicaoCena.PodeIniciar(out mensagemTransicao))
        {
            FalharEscalada(mensagemTransicao);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(idItemConsumido))
        {
            FalharEscalada("O ID do item necessario para a escalada nao foi configurado.");
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
        transicaoCena.IniciarTransicao();
    }

    private void FalharEscalada(string mensagem)
    {
        transicaoIniciada = false;
        if (GameplayState.Instancia != null)
            GameplayState.Instancia.Liberar(GameplayBlockReason.TransicaoCena);

        Debug.LogError(mensagem, this);
    }
}
