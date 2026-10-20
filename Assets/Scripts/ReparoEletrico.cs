using System;

/// <summary>Escolhe o item do reparo e consome apenas o material descartavel.</summary>
public static class ReparoEletrico
{
    public static string ItemNecessario(TipoDefeitoEletrico tipo, string fio, string chaveFenda, string alicate)
    {
        switch (tipo)
        {
            case TipoDefeitoEletrico.FioRompido: return fio;
            case TipoDefeitoEletrico.AlavancaDesalinhada: return chaveFenda;
            case TipoDefeitoEletrico.LigacaoIndevida: return alicate;
            default: return null;
        }
    }

    public static bool TentarUsar(TipoDefeitoEletrico tipo, string fio, string chaveFenda, string alicate,
        Func<string, bool> possui, Func<string, bool> consumir)
    {
        string id = ItemNecessario(tipo, fio, chaveFenda, alicate);
        if (string.IsNullOrWhiteSpace(id) || possui == null || !possui(id)) return false;
        return tipo != TipoDefeitoEletrico.FioRompido || (consumir != null && consumir(id));
    }
}
