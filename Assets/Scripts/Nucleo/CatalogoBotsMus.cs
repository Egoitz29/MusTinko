using System;
using System.Collections.Generic;

public static class CatalogoBotsMus
{
    public static Dictionary<string, Func<IBotMus>> Crear()
    {
        return new Dictionary<string, Func<IBotMus>>
        {
            { "Aleatorio", () => new BotAleatorioMus() },
            { "Prudente", () => new BotReglasMus("Prudente", ParametrosBotMus.Prudente()) },
            { "Equilibrado", () => new BotReglasMus("Equilibrado", ParametrosBotMus.Equilibrado()) },
            { "Agresivo", () => new BotReglasMus("Agresivo", ParametrosBotMus.Agresivo()) },
            { "SinSenales", () => new BotReglasMus("SinSenales", ParametrosBotMus.SinSenales()) }
        };
    }
}
