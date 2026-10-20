using UnityEngine;

public class CentralEnergia : InteragivelCenario
{
    public PuzzleEletrica puzzle;

    public override string ObterTextoInteracao()
    {
        if (puzzle == null || !puzzle.ValidarConfiguracao(out _)) return "Central sem configuracao";
        if (puzzle.CircuitoCompleto) return "Energia restabelecida";
        if (puzzle.EnergiaLigada)
            return "Circuito energizado: " + puzzle.Pendentes + " armario(s) com defeito.";
        return "[E] Ligar energia central";
    }

    public override void Interagir()
    {
        if (PodeInteragir && puzzle != null) puzzle.TentarLigar();
    }
}
