using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Autoridade do puzzle: valida reparos, consome fios e libera a central.</summary>
public class PuzzleEletrica : MonoBehaviour
{
    [Header("Identidade e requisitos")]
    [Tooltip("Unica por puzzle. Nao mudar depois de iniciar uma partida.")]
    public string chavePuzzle = "carga.eletrica";
    public ItemDefinition fioCobre;
    [Tooltip("Inclua TODAS as caixas investigaveis, inclusive as intactas.")]
    public CaixaEnergia[] caixas = new CaixaEnergia[0];

    [Header("Estado do ambiente (opcional)")]
    [Tooltip("Luzes/efeitos ligados pela central. Nao inclua este gerenciador nem as caixas.")]
    public GameObject[] objetosComEnergia = new GameObject[0];
    public GameObject[] objetosSemEnergia = new GameObject[0];
    [Tooltip("Som continuo. Play On Awake desligado; Loop sera habilitado pelo script.")]
    public AudioSource somMotor;
    [Tooltip("Som curto da partida, diferente do som continuo.")]
    public AudioSource somPartida;
    public UnityEvent aoLigarEnergia = new UnityEvent();

    public bool EnergiaLigada { get { return EstadoMundo.EstaConcluido(chavePuzzle + ".ligada"); } }
    public int TotalDefeituosas { get { return ContarCaixas(false); } }
    public int Reparadas { get { return ContarCaixas(true); } }
    public int Pendentes { get { return TotalDefeituosas - Reparadas; } }

    private void Start()
    {
        if (!ValidarConfiguracao(out string erro))
            Debug.LogError("Puzzle eletrica: " + erro, this);
        RestaurarEstadoVisual();
    }

    public bool ValidarConfiguracao(out string erro)
    {
        erro = null;
        if (string.IsNullOrWhiteSpace(chavePuzzle)) erro = "Preencha a Chave Puzzle.";
        else if (fioCobre == null || string.IsNullOrWhiteSpace(fioCobre.id) || !fioCobre.empilhavel)
            erro = "Associe uma definicao de fio de cobre empilhavel com ID valido.";
        else if (caixas == null || caixas.Length == 0) erro = "Associe as caixas do puzzle.";
        if (erro != null) return false;

        var ids = new HashSet<string>();
        foreach (CaixaEnergia caixa in caixas)
        {
            if (caixa == null || caixa.puzzle != this)
                erro = "Toda caixa da lista precisa existir e apontar para este puzzle.";
            else if (string.IsNullOrWhiteSpace(caixa.idCaixa) || !ids.Add(caixa.idCaixa))
                erro = "Cada caixa precisa de um ID preenchido e diferente (ex.: C01, C02).";
            if (erro != null) return false;
        }
        if (TotalDefeituosas == 0) erro = "Marque pelo menos uma caixa como Necessita Reparo.";
        return erro == null;
    }

    private int ContarCaixas(bool apenasReparadas)
    {
        int total = 0;
        if (caixas != null)
            foreach (CaixaEnergia caixa in caixas)
                if (caixa != null && caixa.necessitaReparo && (!apenasReparadas || CaixaReparada(caixa))) total++;
        return total;
    }

    public bool CaixaReparada(CaixaEnergia caixa)
    {
        return caixa != null && EstadoMundo.EstaConcluido(ChaveReparo(caixa));
    }

    private string ChaveReparo(CaixaEnergia caixa)
    {
        return chavePuzzle + ".caixa." + caixa.idCaixa + ".reparada";
    }

    public bool TentarReparar(CaixaEnergia caixa)
    {
        if (!PodeAlterar() || !ValidarConfiguracao(out _) || EnergiaLigada || caixa == null ||
            !caixa.isActiveAndEnabled || !caixa.Inspecionada || !caixa.necessitaReparo ||
            System.Array.IndexOf(caixas, caixa) < 0 || CaixaReparada(caixa)) return false;

        SistemaInventario inventario = SistemaInventario.Instancia;
        if (inventario == null || !inventario.ConsumirItem(fioCobre.id, 1)) return false;

        EstadoMundo.MarcarConcluido(ChaveReparo(caixa));
        caixa.AtualizarVisual();
        caixa.NotificarReparo();
        return true;
    }

    public bool TentarLigar()
    {
        if (!PodeAlterar() || !ValidarConfiguracao(out _) || EnergiaLigada || Pendentes != 0) return false;

        EstadoMundo.MarcarConcluido(chavePuzzle + ".ligada");
        RestaurarEstadoVisual();
        if (somPartida != null) somPartida.Play();
        aoLigarEnergia.Invoke();
        return true;
    }

    private bool PodeAlterar()
    {
        return isActiveAndEnabled && (GameplayState.Instancia == null || !GameplayState.Instancia.EstaBloqueado);
    }

    public void RestaurarEstadoVisual()
    {
        bool ligada = EnergiaLigada;
        AplicarAtivos(objetosComEnergia, ligada);
        AplicarAtivos(objetosSemEnergia, !ligada);
        if (somMotor != null)
        {
            somMotor.loop = true;
            if (ligada && !somMotor.isPlaying) somMotor.Play();
            else if (!ligada) somMotor.Stop();
        }
        if (caixas != null)
            foreach (CaixaEnergia caixa in caixas)
                if (caixa != null) caixa.AtualizarVisual();
    }

    private static void AplicarAtivos(GameObject[] objetos, bool ativo)
    {
        if (objetos == null) return;
        foreach (GameObject objeto in objetos)
            if (objeto != null) objeto.SetActive(ativo);
    }

    [ContextMenu("Validar configuracao do puzzle")]
    private void ValidarNoInspector()
    {
        if (ValidarConfiguracao(out string erro))
            Debug.Log("Puzzle eletrica configurado: " + TotalDefeituosas + " reparos, 1 fio por reparo.", this);
        else Debug.LogError("Puzzle eletrica: " + erro, this);
    }
}
