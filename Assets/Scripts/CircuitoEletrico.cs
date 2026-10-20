using System;

public enum TipoDefeitoEletrico
{
    Nenhum = 0,
    FioRompido = 1,
    AlavancaDesalinhada = 2,
    LigacaoIndevida = 3
}

public enum EstadoArmarioEletrico
{
    Desligado,
    SemAlimentacao,
    Funcionando,
    FioRompido,
    AlavancaDesalinhada,
    LigacaoIndevida
}

/// <summary>Sorteio e continuidade do circuito, independentes dos objetos da cena.</summary>
public static class CircuitoEletrico
{
    public const int MaximoDefeitos = 9;

    public static TipoDefeitoEletrico[] Sortear(int total, int semente)
    {
        if (total < MaximoDefeitos)
            throw new ArgumentOutOfRangeException(nameof(total), "O circuito precisa comportar ate nove defeitos.");

        var sorteio = new Random(semente);
        var indices = new int[total];
        for (int i = 0; i < total; i++) indices[i] = i;
        for (int i = total - 1; i > 0; i--)
        {
            int outro = sorteio.Next(i + 1);
            int anterior = indices[i];
            indices[i] = indices[outro];
            indices[outro] = anterior;
        }

        var resultado = new TipoDefeitoEletrico[total];
        int proximo = 0;
        for (int tipo = 1; tipo <= 3; tipo++)
        {
            // Cada categoria tem sua propria quantidade; as posicoes nao se repetem.
            int quantidade = sorteio.Next(1, 4);
            for (int i = 0; i < quantidade; i++)
                resultado[indices[proximo++]] = (TipoDefeitoEletrico)tipo;
        }
        return resultado;
    }

    public static EstadoArmarioEletrico[] Avaliar(TipoDefeitoEletrico[] defeitos, bool energizado, bool[] reparadas)
    {
        if (defeitos == null) throw new ArgumentNullException(nameof(defeitos));
        if (reparadas == null || reparadas.Length != defeitos.Length)
            throw new ArgumentException("A lista de reparos precisa corresponder aos armarios.", nameof(reparadas));

        var estados = new EstadoArmarioEletrico[defeitos.Length];
        bool passagem = energizado;
        for (int i = 0; i < defeitos.Length; i++)
        {
            if (!Enum.IsDefined(typeof(TipoDefeitoEletrico), defeitos[i]))
                throw new ArgumentException("Tipo de defeito desconhecido.", nameof(defeitos));
            if (!energizado) estados[i] = EstadoArmarioEletrico.Desligado;
            else if (!passagem) estados[i] = EstadoArmarioEletrico.SemAlimentacao;
            else if (reparadas[i] || defeitos[i] == TipoDefeitoEletrico.Nenhum)
                estados[i] = EstadoArmarioEletrico.Funcionando;
            else
            {
                switch (defeitos[i])
                {
                    case TipoDefeitoEletrico.FioRompido: estados[i] = EstadoArmarioEletrico.FioRompido; break;
                    case TipoDefeitoEletrico.AlavancaDesalinhada: estados[i] = EstadoArmarioEletrico.AlavancaDesalinhada; break;
                    default: estados[i] = EstadoArmarioEletrico.LigacaoIndevida; break;
                }
                passagem = false;
            }
        }
        return estados;
    }
}
