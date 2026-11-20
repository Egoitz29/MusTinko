using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public static class Program
{
    public static int Main(string[] args)
    {
        string comando = args.Length > 0 ? args[0] : "pruebas";
        int n = args.Length > 1 ? int.Parse(args[1]) : 0;

        switch (comando)
        {
            case "pruebas": return PruebasNucleoMus.Ejecutar(Console.WriteLine) == 0 ? 0 : 1;
            case "comparar": Comparar(n > 0 ? n : 2000); return 0;
            case "entrenar": Entrenar(n > 0 ? n : 30, args.Length > 2 ? int.Parse(args[2]) : 400); return 0;
            case "registrar": Registrar(n > 0 ? n : 200, args.Length > 2 ? args[2] : "salida/experiencias.jsonl"); return 0;
            default:
                Console.WriteLine("Uso: pruebas | comparar [partidas] | entrenar [generaciones] [partidasPorGen] | registrar [partidas] [archivo]");
                return 1;
        }
    }

    private static void Comparar(int partidas)
    {
        Dictionary<string, Func<IBotMus>> bots = CatalogoBotsMus.Crear();
        List<string> nombres = new(bots.Keys);

        for (int a = 0; a < nombres.Count; a++)
            for (int b = a + 1; b < nombres.Count; b++)
                Console.WriteLine(ComparacionBotsMus.Comparar(bots[nombres[a]], bots[nombres[b]], partidas, 777));
    }

    private static void Entrenar(int generaciones, int partidasPorGeneracion)
    {
        EntrenadorBotsMus entrenador = new() { PartidasPorEvaluacion = partidasPorGeneracion };
        ParametrosBotMus inicial = ParametrosBotMus.Equilibrado();
        ParametrosBotMus final = entrenador.Entrenar(inicial, generaciones, 2024, Console.WriteLine);

        Directory.CreateDirectory("salida");
        File.WriteAllText("salida/bot_entrenado.txt", EntrenadorBotsMus.Guardar(final));

        Console.WriteLine();
        Console.WriteLine("== Evaluacion final con partidas nuevas ==");
        Console.WriteLine(ComparacionBotsMus.Comparar(
            () => new BotReglasMus("Entrenado", final),
            () => new BotReglasMus("Equilibrado", ParametrosBotMus.Equilibrado()), 4000, 999999));
        Console.WriteLine(ComparacionBotsMus.Comparar(
            () => new BotReglasMus("Entrenado", final),
            () => new BotReglasMus("Agresivo", ParametrosBotMus.Agresivo()), 4000, 888888));
        Console.WriteLine(ComparacionBotsMus.Comparar(
            () => new BotReglasMus("Entrenado", final),
            () => new BotReglasMus("Prudente", ParametrosBotMus.Prudente()), 4000, 777777));
    }

    private static void Registrar(int partidas, string archivo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(archivo)));
        using RegistroExperienciasMus registro = new(new StreamWriter(archivo));
        SimuladorMus sim = new() { Registro = registro };
        Func<IBotMus>[] tipos =
        {
            () => new BotReglasMus("Prudente", ParametrosBotMus.Prudente()),
            () => new BotReglasMus("Equilibrado", ParametrosBotMus.Equilibrado()),
            () => new BotReglasMus("Agresivo", ParametrosBotMus.Agresivo()),
            () => new BotAleatorioMus()
        };

        for (int i = 0; i < partidas; i++)
        {
            IBotMus[] bots = new IBotMus[4];
            for (int j = 0; j < 4; j++) bots[j] = tipos[(i + j) % tipos.Length]();
            sim.JugarPartida(bots, i, i);
        }

        Console.WriteLine("Decisiones registradas: " + registro.LineasEscritas + " en " + archivo);
    }
}
