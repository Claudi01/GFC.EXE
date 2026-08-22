using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guarda estados permanentes da partida que pertencem ao mundo,
/// como pickups coletados e itens consumidos.
/// </summary>
public static class EstadoMundo
{
    private const string ChaveRegistro = "gfc.progresso.registro.v1";

    [Serializable]
    private class Registro
    {
        public List<string> chaves = new List<string>();
    }

    public static bool EstaConcluido(string chave)
    {
        return !string.IsNullOrWhiteSpace(chave) && PlayerPrefs.GetInt(chave, 0) == 1;
    }

    public static void MarcarConcluido(string chave)
    {
        if (string.IsNullOrWhiteSpace(chave))
            return;

        PlayerPrefs.SetInt(chave, 1);
        Registrar(chave);
        PlayerPrefs.Save();
    }

    public static void LimparTudo()
    {
        Registro registro = LerRegistro();
        foreach (string chave in registro.chaves)
        {
            if (!string.IsNullOrWhiteSpace(chave))
                PlayerPrefs.DeleteKey(chave);
        }

        PlayerPrefs.DeleteKey(ChaveRegistro);
        PlayerPrefs.Save();
    }

    private static void Registrar(string chave)
    {
        Registro registro = LerRegistro();
        if (registro.chaves.Contains(chave))
            return;

        registro.chaves.Add(chave);
        PlayerPrefs.SetString(ChaveRegistro, JsonUtility.ToJson(registro));
    }

    private static Registro LerRegistro()
    {
        if (!PlayerPrefs.HasKey(ChaveRegistro))
            return new Registro();

        try
        {
            Registro registro = JsonUtility.FromJson<Registro>(PlayerPrefs.GetString(ChaveRegistro));
            if (registro == null)
                return new Registro();

            if (registro.chaves == null)
                registro.chaves = new List<string>();

            return registro;
        }
        catch (Exception)
        {
            PlayerPrefs.DeleteKey(ChaveRegistro);
            return new Registro();
        }
    }
}
