public static class ContextoTransicaoCena
{
    private static string entradaSolicitada;

    public static void DefinirEntrada(string idEntrada)
    {
        entradaSolicitada = idEntrada;
    }

    public static string ConsumirEntrada()
    {
        string entrada = entradaSolicitada;
        entradaSolicitada = null;
        return entrada;
    }
}
