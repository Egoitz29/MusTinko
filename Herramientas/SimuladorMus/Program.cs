using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            case "entrenar": Entrenar(n > 0 ? n : 30, args.Length > 2 ? int.Parse(args[2]) : 1000); return 0;
            case "tabla": Tabla(n); return 0;
            case "registrar": Registrar(n > 0 ? n : 200, args.Length > 2 ? args[2] : "salida/experiencias.jsonl"); return 0;
            case "tablas": Tablas(n > 0 ? n : 40000); return 0;
            case "probabilista": Probabilista(n > 0 ? n : 60, args.Length > 2 ? int.Parse(args[2]) : 800); return 0;
            case "evaluarprob": EvaluarProbabilista(n > 0 ? n : 20000); return 0;
            default:
                Console.WriteLine("Uso: pruebas | comparar [partidas] | entrenar [generaciones] [partidasPorGen] | registrar [partidas] [archivo]\n" +
                                  "     tablas [muestrasPorCaso] | probabilista [generaciones] [partidasPorRival] | evaluarprob [partidas]");
                return 1;
        }
    }

    private static void Tabla(int semilla)
    {
        string[] nombres = { "LILA", "MOSTAZA", "MENTA", "CORAL" };
        PartidaMus p = new(semilla, 40, 4);
        p.IniciarPartida();
        IBotMus[] bots = { new BotReglasMus("A", ParametrosBotMus.Agresivo()), new BotReglasMus("B", ParametrosBotMus.Equilibrado()),
                           new BotReglasMus("C", ParametrosBotMus.Agresivo()), new BotReglasMus("D", ParametrosBotMus.Equilibrado()) };
        Random azar = new(semilla);
        while (p.Fase != FaseMus.FinMano && !p.PartidaTerminada)
        {
            int j = p.JugadorActual;
            p.Aplicar(j, bots[j].Decidir(p.Observar(j), p.AccionesLegales(j), azar));
        }
        int[] todas = new int[16];
        for (int j = 0; j < 4; j++)
        {
            for (int c = 0; c < 4; c++) todas[j * 4 + c] = p.Carta(j, c);
            Console.WriteLine(nombres[j] + ": " + string.Join(" ", Array.ConvertAll(p.CartasDe(j), CartasMus.TextoCorto)));
        }
        foreach (EventoMus e in p.Historial) Console.WriteLine("   " + e);
        TablaTanteoMus.Tabla t = TablaTanteoMus.Construir(p.Desglose, todas, 0, nombres);
        Console.WriteLine();
        Console.WriteLine(string.Format("{0,-8}| {1,-40}| {2}", "", "NOSOTROS (LILA-MENTA)", "ELLOS"));
        foreach (TablaTanteoMus.Fila f in t.Filas)
            Console.WriteLine(string.Format("{0,-8}| {1,-40}| {2}", f.Lance, string.Join("; ", f.Nosotros), string.Join("; ", f.Ellos)));
        Console.WriteLine(string.Format("{0,-8}| {1,-40}| {2}", "TOTAL", "+" + t.TotalNosotros, "+" + t.TotalEllos));
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

    // ---------------------------------------------------------------- bot probabilista

    private const string CarpetaBots = "../../Assets/Resources/Bots/";
    private const string ArchivoTablas = CarpetaBots + "ProbabilidadesMus.txt";
    private const string ArchivoProbabilista = CarpetaBots + "BotProbabilistaMus.txt";
    private const string ArchivoReglasEntrenado = "../../Assets/Resources/BotEntrenadoMus.txt";

    private static void Tablas(int muestras)
    {
        Stopwatch reloj = Stopwatch.StartNew();
        TablasProbabilidadMus.Generar(muestras, 31337, Console.WriteLine);
        Console.WriteLine("Tiempo: " + reloj.Elapsed.TotalSeconds.ToString("0.0") + " s");
        Directory.CreateDirectory(CarpetaBots);
        File.WriteAllText(ArchivoTablas, TablasProbabilidadMus.Guardar());
        Console.WriteLine("Guardado en " + Path.GetFullPath(ArchivoTablas));
        MostrarEjemplos();
    }

    private static void MostrarEjemplos()
    {
        // Algunas manos para ver que los numeros tienen sentido.
        (string, int[])[] ejemplos =
        {
            ("Duples de reyes (R R R R)", new[] { 9, 19, 29, 39 }),
            ("31 real (R R S As)", new[] { 9, 19, 7, 0 }),
            ("Cuatro ases", new[] { 0, 10, 20, 30 }),
            ("Medias de sotas (S S S 4)", new[] { 7, 17, 27, 3 }),
            ("Nada (4 5 6 7)", new[] { 3, 4, 5, 6 })
        };
        string[] lances = { "grande", "chica", "pares", "pares+comp", "juego", "juego+comp", "punto" };

        foreach ((string nombre, int[] cartas) in ejemplos)
        {
            Console.WriteLine(nombre + "  [" + string.Join(" ", cartas.Select(CartasMus.TextoCorto)) + "]");

            for (int pos = 0; pos < 4; pos += 3)
                Console.WriteLine("   " + (pos == 0 ? "mano  " : "postre") + ": " + string.Join("  ", Enumerable.Range(0, TablasProbabilidadMus.Lances)
                    .Select(l => lances[l] + " " + (TablasProbabilidadMus.Probabilidad(cartas, pos, l) * 100).ToString("0") + "%")));
        }
    }

    private static bool CargarTablas()
    {
        if (TablasProbabilidadMus.Cargadas)
            return true;

        if (File.Exists(ArchivoTablas) && TablasProbabilidadMus.Cargar(File.ReadAllText(ArchivoTablas)))
            return true;

        Console.WriteLine("No hay tablas guardadas: ejecuta primero  dotnet run -- tablas");
        return false;
    }

    private static List<EntrenadorProbabilistaMus.Rival> RivalesFijos()
    {
        List<EntrenadorProbabilistaMus.Rival> r = new();

        if (File.Exists(ArchivoReglasEntrenado))
        {
            ParametrosBotMus entrenado = EntrenadorBotsMus.Cargar(File.ReadAllText(ArchivoReglasEntrenado));
            r.Add(new EntrenadorProbabilistaMus.Rival { Nombre = "Reglas entrenado (el de ahora)", Crear = () => new BotReglasMus("Entrenado", entrenado) });
        }

        r.Add(new EntrenadorProbabilistaMus.Rival { Nombre = "Equilibrado", Crear = () => new BotReglasMus("Equilibrado", ParametrosBotMus.Equilibrado()) });
        r.Add(new EntrenadorProbabilistaMus.Rival { Nombre = "Agresivo", Crear = () => new BotReglasMus("Agresivo", ParametrosBotMus.Agresivo()) });
        r.Add(new EntrenadorProbabilistaMus.Rival { Nombre = "Prudente", Crear = () => new BotReglasMus("Prudente", ParametrosBotMus.Prudente()) });
        r.Add(new EntrenadorProbabilistaMus.Rival { Nombre = "Probabilista sin entrenar", Crear = () => new BotProbabilistaMus("Base", new ParametrosProbabilistaMus()) });
        return r;
    }

    private static void Probabilista(int generaciones, int partidasPorRival)
    {
        if (!CargarTablas())
            return;

        EntrenadorProbabilistaMus entrenador = new() { PartidasPorRival = partidasPorRival };
        ParametrosProbabilistaMus inicial = new();

        if (File.Exists(ArchivoProbabilista))
            inicial = ParametrosProbabilistaMus.Cargar(File.ReadAllText(ArchivoProbabilista)) ?? inicial;

        Console.WriteLine("Parametros iniciales: " + inicial);
        Stopwatch reloj = Stopwatch.StartNew();
        ParametrosProbabilistaMus final = entrenador.Entrenar(inicial, RivalesFijos(), generaciones, 4242, Console.WriteLine);
        Console.WriteLine("Partidas de entrenamiento: " + entrenador.PartidasJugadas.ToString("N0") + " en " + reloj.Elapsed.TotalMinutes.ToString("0.0") + " min");
        Console.WriteLine("Parametros finales:   " + final);

        Directory.CreateDirectory(CarpetaBots);
        File.WriteAllText(ArchivoProbabilista, final.Guardar());
        Console.WriteLine("Guardado en " + Path.GetFullPath(ArchivoProbabilista));
        EvaluarProbabilista(20000);
    }

    private static void EvaluarProbabilista(int partidas)
    {
        if (!CargarTablas())
            return;

        ParametrosProbabilistaMus p = File.Exists(ArchivoProbabilista) ? ParametrosProbabilistaMus.Cargar(File.ReadAllText(ArchivoProbabilista)) : null;
        p ??= new ParametrosProbabilistaMus();
        List<EntrenadorProbabilistaMus.Rival> rivales = RivalesFijos();
        string[] lineas = new string[rivales.Count];

        Console.WriteLine();
        Console.WriteLine("== Evaluacion con partidas nuevas (" + partidas + " por rival) ==");
        Parallel.For(0, rivales.Count, i =>
            lineas[i] = ComparacionBotsMus.Comparar(() => new BotProbabilistaMus("Probabilista", p), rivales[i].Crear, partidas, 13579 + i).ToString()
                        .Replace(" vs " + rivales[i].Crear().Nombre, " vs " + rivales[i].Nombre));

        foreach (string l in lineas)
            Console.WriteLine(l);
    }
}
