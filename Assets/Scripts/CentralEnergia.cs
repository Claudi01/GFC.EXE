using UnityEngine;

public class CentralEnergia : InteragivelCenario
{
    public PuzzleEletrica puzzle;

    public override string ObterTextoInteracao()
    {
        if (puzzle == null || !puzzle.ValidarConfiguracao(out _)) return "Central sem configuracao";
        if (puzzle.EnergiaLigada) return "Energia restabelecida";
        if (puzzle.Pendentes > 0)
            return "Central bloqueada: " + puzzle.Pendentes + " caixa(s) com defeito nos corredores.";
        return "[E] Ligar energia central";
    }

    public override void Interagir()
    {
        if (PodeInteragir && puzzle != null) puzzle.TentarLigar();
    }
}
