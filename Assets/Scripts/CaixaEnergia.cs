using UnityEngine;
using UnityEngine.Events;

/// <summary>Primeiro E inspeciona; um segundo E repara quando ha fio disponivel.</summary>
public class CaixaEnergia : InteragivelCenario
{
    public PuzzleEletrica puzzle;
    public string idCaixa = "C01";
    public bool necessitaReparo = true;

    [Header("Filhos visuais opcionais (nao usar o objeto com este script)")]
    public GameObject visualFiosDanificados;
    public GameObject visualFiosIntactos;
    public GameObject tampaFechada;
    public GameObject tampaAberta;
    public AudioSource somReparo;
    public UnityEvent aoInspecionar = new UnityEvent();
    public UnityEvent aoReparar = new UnityEvent();

    public bool Inspecionada { get; private set; }
    public bool Reparada { get { return puzzle != null && puzzle.CaixaReparada(this); } }

    private void Start() { AtualizarVisual(); }

    public override string ObterTextoInteracao()
    {
        if (puzzle == null || !puzzle.ValidarConfiguracao(out _)) return "Caixa sem configuracao";
        if (Reparada) return idCaixa + ": reparada";
        if (!Inspecionada) return "[E] Inspecionar caixa " + idCaixa;
        if (!necessitaReparo) return idCaixa + ": fiacao intacta";
        if (SistemaInventario.Instancia == null || !SistemaInventario.Instancia.Possui(puzzle.fioCobre.id))
            return idCaixa + ": fio rompido. Preciso de 1 fio de cobre.";
        return "[E] Reparar " + idCaixa + " (consome 1 fio de cobre)";
    }

    public override void Interagir()
    {
        if (!PodeInteragir || puzzle == null || !puzzle.ValidarConfiguracao(out _) || Reparada) return;
        if (!Inspecionada)
        {
            Inspecionada = true;
            AtualizarVisual();
            aoInspecionar.Invoke();
            return;
        }
        puzzle.TentarReparar(this);
    }

    public void AtualizarVisual()
    {
        bool reparada = Reparada;
        bool aberta = Inspecionada || reparada;
        if (visualFiosDanificados != null) visualFiosDanificados.SetActive(necessitaReparo && !reparada);
        if (visualFiosIntactos != null) visualFiosIntactos.SetActive(!necessitaReparo || reparada);
        if (tampaFechada != null) tampaFechada.SetActive(!aberta);
        if (tampaAberta != null) tampaAberta.SetActive(aberta);
    }

    internal void NotificarReparo()
    {
        if (somReparo != null) somReparo.Play();
        aoReparar.Invoke();
    }
}
