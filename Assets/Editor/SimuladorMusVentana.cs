using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SimuladorMusVentana : EditorWindow
{
    private const string CarpetaSalida = "Entrenamiento";

    private int indiceA = 2;
    private int indiceB = 0;
    private int partidasComparacion = 2000;
    private int generaciones = 30;
    private int partidasPorGeneracion = 1000;
    private int partidasRegistro = 500;
    private int semilla = 2024;
    private Vector2 scroll;
    private readonly StringBuilder log = new();

    [MenuItem("Mus TINKO/Simulador de bots")]
    public static void Abrir()
    {
        GetWindow<SimuladorMusVentana>("Simulador Mus");
    }

    private void OnGUI()
    {
        Dictionary<string, Func<IBotMus>> catalogo = CatalogoConEntrenado();
        string[] nombres = new List<string>(catalogo.Keys).ToArray();
        indiceA = Mathf.Clamp(indiceA, 0, nombres.Length - 1);
        indiceB = Mathf.Clamp(indiceB, 0, nombres.Length - 1);

        EditorGUILayout.LabelField("Reglas", EditorStyles.boldLabel);

        if (GUILayout.Button("Ejecutar pruebas del nucleo de reglas"))
        {
            Limpiar();
            int resultado = PruebasNucleoMus.Ejecutar(Escribir);
            Debug.Log("[SIMULADOR] Pruebas: " + (resultado == 0 ? "superadas" : "con fallos"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Comparar bots (A en un bando, B en el otro)", EditorStyles.boldLabel);
        indiceA = EditorGUILayout.Popup("Bot A", indiceA, nombres);
        indiceB = EditorGUILayout.Popup("Bot B", indiceB, nombres);
        partidasComparacion = EditorGUILayout.IntField("Partidas", Mathf.Max(2, partidasComparacion));
        semilla = EditorGUILayout.IntField("Semilla", semilla);

        if (GUILayout.Button("Comparar"))
        {
            ComparacionBotsMus c = ComparacionBotsMus.Comparar(
                catalogo[nombres[indiceA]], catalogo[nombres[indiceB]], partidasComparacion, semilla);
            Escribir(c.ToString());
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Entrenamiento por autojuego", EditorStyles.boldLabel);
        generaciones = EditorGUILayout.IntField("Generaciones", Mathf.Max(1, generaciones));
        partidasPorGeneracion = EditorGUILayout.IntField("Partidas por generacion", Mathf.Max(100, partidasPorGeneracion));

        if (GUILayout.Button("Entrenar desde Equilibrado"))
            Entrenar();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Registro de experiencias (JSONL para Python)", EditorStyles.boldLabel);
        partidasRegistro = EditorGUILayout.IntField("Partidas", Mathf.Max(1, partidasRegistro));

        if (GUILayout.Button("Generar registro"))
            GenerarRegistro();

        EditorGUILayout.Space();

        if (GUILayout.Button("Limpiar resultados"))
            Limpiar();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(log.ToString(), GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private static string RutaEntrenado => Path.Combine(CarpetaSalida, "bot_entrenado.txt");

    private static Dictionary<string, Func<IBotMus>> CatalogoConEntrenado()
    {
        Dictionary<string, Func<IBotMus>> catalogo = CatalogoBotsMus.Crear();

        if (!File.Exists(RutaEntrenado))
            return catalogo;

        try
        {
            ParametrosBotMus p = EntrenadorBotsMus.Cargar(File.ReadAllText(RutaEntrenado));
            catalogo["Entrenado"] = () => new BotReglasMus("Entrenado", p);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SIMULADOR] No se pudo cargar el bot entrenado: " + e.Message);
        }

        return catalogo;
    }

    private void Entrenar()
    {
        EntrenadorBotsMus entrenador = new() { PartidasPorEvaluacion = partidasPorGeneracion };
        ParametrosBotMus final = entrenador.Entrenar(ParametrosBotMus.Equilibrado(), generaciones, semilla, Escribir);

        Directory.CreateDirectory(CarpetaSalida);
        File.WriteAllText(RutaEntrenado, EntrenadorBotsMus.Guardar(final));
        Escribir("Guardado en " + Path.GetFullPath(RutaEntrenado));

        Escribir(ComparacionBotsMus.Comparar(
            () => new BotReglasMus("Entrenado", final),
            () => new BotReglasMus("Equilibrado", ParametrosBotMus.Equilibrado()),
            4000, semilla + 999999).ToString());
    }

    private void GenerarRegistro()
    {
        Directory.CreateDirectory(CarpetaSalida);
        string ruta = Path.Combine(CarpetaSalida, "experiencias_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jsonl");
        List<Func<IBotMus>> tipos = new(CatalogoConEntrenado().Values);

        using (RegistroExperienciasMus registro = new(new StreamWriter(ruta)))
        {
            SimuladorMus sim = new() { Registro = registro };

            for (int i = 0; i < partidasRegistro; i++)
            {
                IBotMus[] bots = new IBotMus[4];

                for (int j = 0; j < 4; j++)
                    bots[j] = tipos[(i + j) % tipos.Count]();

                sim.JugarPartida(bots, semilla + i, i);
            }

            Escribir(registro.LineasEscritas + " decisiones guardadas en " + Path.GetFullPath(ruta));
        }
    }

    private void Escribir(string linea)
    {
        log.AppendLine(linea);
        Repaint();
    }

    private void Limpiar()
    {
        log.Clear();
    }
}
