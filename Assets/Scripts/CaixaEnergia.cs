using UnityEngine;
using UnityEngine.Events;

/// <summary>Primeiro E inspeciona; outro E repara com o item correto no inventario.</summary>
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

    private Renderer[] renderizadores;
    private MaterialPropertyBlock[][] propriedadesOriginais;
    private bool corAplicada;

    private void Awake() { GuardarAparencia(); }

    private void Start() { AtualizarVisual(); }

    public override string ObterTextoInteracao()
    {
        if (puzzle == null || !puzzle.ValidarConfiguracao(out _)) return "Caixa sem configuracao";
        if (!puzzle.EnergiaLigada) return idCaixa + ": energia desligada. Acione o console.";
        EstadoArmarioEletrico estado = puzzle.ObterEstado(this);
        if (estado == EstadoArmarioEletrico.SemAlimentacao) return idCaixa + ": sem alimentacao de um armario anterior.";
        if (!Inspecionada) return "[E] Inspecionar caixa " + idCaixa;
        if (estado == EstadoArmarioEletrico.Funcionando) return idCaixa + ": funcionando";
        string defeito = estado == EstadoArmarioEletrico.FioRompido ? "fio rompido" :
            estado == EstadoArmarioEletrico.AlavancaDesalinhada ? "alavanca desalinhada" : "ligacao indevida causando curto";
        ItemDefinition item = puzzle.ObterItemReparo(this);
        if (!puzzle.permitirReparos || item == null) return idCaixa + ": " + defeito;
        if (SistemaInventario.Instancia == null || !SistemaInventario.Instancia.Possui(item.id))
            return idCaixa + ": " + defeito + ". Preciso de " + item.nome + ".";
        return "[E] Reparar " + idCaixa + " com " + item.nome +
            (estado == EstadoArmarioEletrico.FioRompido ? " (consome 1)" : "");
    }

    public override void Interagir()
    {
        if (!PodeInteragir || puzzle == null || !puzzle.ValidarConfiguracao(out _) || !puzzle.EnergiaLigada ||
            puzzle.ObterEstado(this) == EstadoArmarioEletrico.SemAlimentacao) return;
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
        if (puzzle == null || !puzzle.EnergiaLigada)
        {
            RestaurarAparencia();
            return;
        }
        EstadoArmarioEletrico estado = puzzle.ObterEstado(this);
        if (estado == EstadoArmarioEletrico.Desligado) { RestaurarAparencia(); return; }
        Color cor;
        switch (estado)
        {
            case EstadoArmarioEletrico.Funcionando: cor = new Color(0.1f, 0.8f, 0.15f, 1f); break;
            case EstadoArmarioEletrico.FioRompido: cor = new Color(0.45f, 0.23f, 0.08f, 1f); break;
            case EstadoArmarioEletrico.AlavancaDesalinhada: cor = new Color(0.85f, 0.04f, 0.04f, 1f); break;
            case EstadoArmarioEletrico.LigacaoIndevida: cor = new Color(0.6f, 0.1f, 0.85f, 1f); break;
            default: cor = Color.black; break;
        }
        AplicarCor(cor);
        bool reparada = Reparada;
        bool aberta = Inspecionada || reparada;
        bool temDefeito = puzzle.ObterDefeito(this) != TipoDefeitoEletrico.Nenhum;
        if (visualFiosDanificados != null) visualFiosDanificados.SetActive(temDefeito && !reparada);
        if (visualFiosIntactos != null) visualFiosIntactos.SetActive(!temDefeito || reparada);
        if (tampaFechada != null) tampaFechada.SetActive(!aberta);
        if (tampaAberta != null) tampaAberta.SetActive(aberta);
    }

    internal void NotificarReparo()
    {
        if (somReparo != null) somReparo.Play();
        aoReparar.Invoke();
    }

    private void GuardarAparencia()
    {
        if (renderizadores != null) return;
        renderizadores = GetComponentsInChildren<Renderer>(true);
        propriedadesOriginais = new MaterialPropertyBlock[renderizadores.Length][];
        for (int r = 0; r < renderizadores.Length; r++)
        {
            propriedadesOriginais[r] = new MaterialPropertyBlock[renderizadores[r].sharedMaterials.Length];
            for (int m = 0; m < propriedadesOriginais[r].Length; m++)
            {
                propriedadesOriginais[r][m] = new MaterialPropertyBlock();
                renderizadores[r].GetPropertyBlock(propriedadesOriginais[r][m], m);
            }
        }
    }

    private void AplicarCor(Color cor)
    {
        GuardarAparencia();
        var bloco = new MaterialPropertyBlock();
        for (int r = 0; r < renderizadores.Length; r++)
        {
            if (renderizadores[r] == null) continue;
            Material[] materiais = renderizadores[r].sharedMaterials;
            for (int m = 0; m < materiais.Length; m++)
            {
                if (materiais[m] == null) continue;
                bloco.Clear();
                // Um override por material tem prioridade sobre o bloco geral do renderer.
                // Quando nao havia override por material, preservar tambem o bloco geral.
                if (propriedadesOriginais[r][m].isEmpty) renderizadores[r].GetPropertyBlock(bloco);
                else renderizadores[r].GetPropertyBlock(bloco, m);
                if (materiais[m].HasProperty("_Color")) bloco.SetColor("_Color", cor);
                if (materiais[m].HasProperty("_BaseColor")) bloco.SetColor("_BaseColor", cor);
                renderizadores[r].SetPropertyBlock(bloco, m);
            }
        }
        corAplicada = true;
    }

    private void RestaurarAparencia()
    {
        if (!corAplicada || renderizadores == null) return;
        for (int r = 0; r < renderizadores.Length; r++)
            if (renderizadores[r] != null)
                for (int m = 0; m < propriedadesOriginais[r].Length; m++)
                    renderizadores[r].SetPropertyBlock(propriedadesOriginais[r][m].isEmpty ? null : propriedadesOriginais[r][m], m);
        corAplicada = false;
    }

    private void OnEnable() { if (puzzle != null) AtualizarVisual(); }
    private void OnDisable() { RestaurarAparencia(); }
}
