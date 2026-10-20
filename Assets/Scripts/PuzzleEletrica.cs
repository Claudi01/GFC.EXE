using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>Ativa a alimentacao, sorteia defeitos uma vez e avalia a ordem do circuito.</summary>
public class PuzzleEletrica : MonoBehaviour
{
    [Header("Circuito, na ordem da entrada para a saida")]
    public string chavePuzzle = "carga.eletrica";
    public CaixaEnergia[] caixas = new CaixaEnergia[0];

    [Header("Itens usados nos reparos")]
    [FormerlySerializedAs("permitirReparoComCobre")]
    public bool permitirReparos;
    public ItemDefinition fioCobre;
    public ItemDefinition chaveFenda;
    public ItemDefinition alicate;

    [Header("Efeitos opcionais reservados para outra etapa")]
    public GameObject[] objetosComEnergia = new GameObject[0];
    public GameObject[] objetosSemEnergia = new GameObject[0];
    public AudioSource somMotor;
    public AudioSource somPartida;
    public UnityEvent aoLigarEnergia = new UnityEvent();

    private TipoDefeitoEletrico[] defeitos;
    private EstadoArmarioEletrico[] estados;
    [Serializable]
    private class DistribuicaoSalva
    {
        public string[] ids;
        public TipoDefeitoEletrico[] tipos;
    }

    public bool EnergiaLigada { get { return EstadoMundo.EstaConcluido(chavePuzzle + ".ativada"); } }
    public int TotalDefeituosas { get { return ContarCaixas(false); } }
    public int Reparadas { get { return ContarCaixas(true); } }
    public int Pendentes { get { return TotalDefeituosas - Reparadas; } }
    public bool CircuitoCompleto { get { return EnergiaLigada && Pendentes == 0; } }

    private void Start()
    {
        if (!ValidarConfiguracao(out string erro))
        {
            Debug.LogError("Puzzle eletrica: " + erro, this);
            return;
        }
        RestaurarEstadoVisual();
    }

    public bool ValidarConfiguracao(out string erro)
    {
        erro = null;
        if (string.IsNullOrWhiteSpace(chavePuzzle)) erro = "Preencha a Chave Puzzle.";
        else if (caixas == null || caixas.Length == 0) erro = "Associe os armarios na ordem do circuito.";
        else if (caixas.Length < CircuitoEletrico.MaximoDefeitos)
            erro = "Associe pelo menos nove armarios para sortear de 1 a 3 defeitos de cada categoria.";
        else if (permitirReparos && (fioCobre == null || string.IsNullOrWhiteSpace(fioCobre.id) || !fioCobre.empilhavel))
            erro = "Associe um fio de cobre empilhavel para os reparos.";
        else if (permitirReparos && (chaveFenda == null || alicate == null || string.IsNullOrWhiteSpace(chaveFenda.id) ||
            string.IsNullOrWhiteSpace(alicate.id) || chaveFenda.empilhavel || alicate.empilhavel ||
            chaveFenda.id == alicate.id || chaveFenda.id == fioCobre.id || alicate.id == fioCobre.id))
            erro = "Associe chave de fenda e alicate reutilizaveis, com IDs diferentes entre si e do fio.";
        if (erro != null) return false;

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (CaixaEnergia caixa in caixas)
        {
            if (caixa == null || caixa.puzzle != this)
                erro = "Todo armario precisa existir e apontar para este puzzle.";
            else if (string.IsNullOrWhiteSpace(caixa.idCaixa) || !ids.Add(caixa.idCaixa))
                erro = "Cada armario precisa de um ID exclusivo.";
            if (erro != null) return false;
        }
        if (EnergiaLigada && !GarantirDistribuicao())
        {
            erro = "A distribuicao salva nao corresponde aos armarios. Inicie uma nova partida apos alterar o circuito.";
            return false;
        }
        return true;
    }

    public bool TentarLigar()
    {
        if (!PodeAlterar() || !ValidarConfiguracao(out _) || EnergiaLigada) return false;

        defeitos = CircuitoEletrico.Sortear(caixas.Length, Guid.NewGuid().GetHashCode());
        var registro = new DistribuicaoSalva { ids = new string[caixas.Length], tipos = defeitos };
        for (int i = 0; i < caixas.Length; i++)
            registro.ids[i] = caixas[i].idCaixa;
        // Um unico registro substitui distribuicoes parciais de tentativas interrompidas.
        EstadoMundo.SalvarTexto(chavePuzzle + ".sorteio.v2", JsonUtility.ToJson(registro));
        EstadoMundo.MarcarConcluido(chavePuzzle + ".ativada");
        RestaurarEstadoVisual();
        return true;
    }

    private bool GarantirDistribuicao()
    {
        if (defeitos != null && defeitos.Length == caixas.Length) return true;
        if (!EnergiaLigada)
        {
            defeitos = new TipoDefeitoEletrico[caixas.Length];
            return true;
        }
        try
        {
            var registro = JsonUtility.FromJson<DistribuicaoSalva>(EstadoMundo.LerTexto(chavePuzzle + ".sorteio.v2"));
            if (registro == null || registro.ids == null || registro.tipos == null ||
                registro.ids.Length != caixas.Length || registro.tipos.Length != caixas.Length) return false;
            for (int i = 0; i < caixas.Length; i++)
                if (registro.ids[i] != caixas[i].idCaixa || !Enum.IsDefined(typeof(TipoDefeitoEletrico), registro.tipos[i])) return false;
            defeitos = registro.tipos;
            return true;
        }
        catch (ArgumentException) { return false; }
    }

    public TipoDefeitoEletrico ObterDefeito(CaixaEnergia caixa)
    {
        int indice = caixas == null ? -1 : Array.IndexOf(caixas, caixa);
        if (indice < 0 || !EnergiaLigada) return TipoDefeitoEletrico.Nenhum;
        if (!GarantirDistribuicao()) return TipoDefeitoEletrico.Nenhum;
        return defeitos[indice];
    }

    public EstadoArmarioEletrico ObterEstado(CaixaEnergia caixa)
    {
        int indice = caixas == null ? -1 : Array.IndexOf(caixas, caixa);
        if (indice < 0 || !EnergiaLigada) return EstadoArmarioEletrico.Desligado;
        if (estados == null && !RecalcularEstados()) return EstadoArmarioEletrico.Desligado;
        return estados[indice];
    }

    private bool RecalcularEstados()
    {
        if (!GarantirDistribuicao()) return false;
        var reparos = new bool[caixas.Length];
        for (int i = 0; i < caixas.Length; i++) reparos[i] = CaixaReparada(caixas[i]);
        estados = CircuitoEletrico.Avaliar(defeitos, EnergiaLigada, reparos);
        return true;
    }

    private int ContarCaixas(bool apenasReparadas)
    {
        if (!EnergiaLigada || caixas == null) return 0;
        if (!GarantirDistribuicao()) return 0;
        int total = 0;
        for (int i = 0; i < caixas.Length; i++)
            if (defeitos[i] != TipoDefeitoEletrico.Nenhum && (!apenasReparadas || CaixaReparada(caixas[i]))) total++;
        return total;
    }

    public bool CaixaReparada(CaixaEnergia caixa)
    {
        return caixa != null && EstadoMundo.EstaConcluido(ChaveReparo(caixa));
    }

    private string ChaveReparo(CaixaEnergia caixa) { return chavePuzzle + ".caixa." + caixa.idCaixa + ".reparada"; }
    public ItemDefinition ObterItemReparo(CaixaEnergia caixa)
    {
        switch (ObterDefeito(caixa))
        {
            case TipoDefeitoEletrico.FioRompido: return fioCobre;
            case TipoDefeitoEletrico.AlavancaDesalinhada: return chaveFenda;
            case TipoDefeitoEletrico.LigacaoIndevida: return alicate;
            default: return null;
        }
    }

    public bool TentarReparar(CaixaEnergia caixa)
    {
        if (!permitirReparos || !PodeAlterar() || !ValidarConfiguracao(out _) || !EnergiaLigada ||
            caixa == null || !caixa.isActiveAndEnabled || !caixa.Inspecionada || CaixaReparada(caixa) ||
            Array.IndexOf(caixas, caixa) < 0) return false;
        EstadoArmarioEletrico estado = ObterEstado(caixa);
        if (estado == EstadoArmarioEletrico.Desligado || estado == EstadoArmarioEletrico.SemAlimentacao ||
            estado == EstadoArmarioEletrico.Funcionando) return false;
        SistemaInventario inventario = SistemaInventario.Instancia;
        if (inventario == null || !ReparoEletrico.TentarUsar(ObterDefeito(caixa), fioCobre.id, chaveFenda.id, alicate.id,
            inventario.Possui, id => inventario.ConsumirItem(id, 1))) return false;
        EstadoMundo.MarcarConcluido(ChaveReparo(caixa));
        RestaurarEstadoVisual();
        caixa.NotificarReparo();
        return true;
    }

    private bool PodeAlterar()
    {
        return isActiveAndEnabled && (GameplayState.Instancia == null || !GameplayState.Instancia.EstaBloqueado);
    }

    public void RestaurarEstadoVisual()
    {
        if (!ValidarConfiguracao(out _)) return;
        RecalcularEstados();
        foreach (CaixaEnergia caixa in caixas) caixa.AtualizarVisual();
    }

    [ContextMenu("Validar configuracao do puzzle")]
    private void ValidarNoInspector()
    {
        if (ValidarConfiguracao(out string erro))
            Debug.Log("Circuito configurado: " + caixas.Length + " armarios, 1 a 3 defeitos por categoria (3 a 9 no total) sorteados ao ligar.", this);
        else Debug.LogError("Puzzle eletrica: " + erro, this);
    }
}
