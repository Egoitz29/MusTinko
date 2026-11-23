using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidMus
{
    private const string EscenaJuego = "Assets/Scenes/MesamusOnline.unity";
    private const string Identificador = "com.mustinko.juego";
    private const string CarpetaBuilds = "Builds/Android";

    [MenuItem("Mus TINKO/Android/1. Preparar ajustes de Android")]
    public static void Preparar()
    {
        NamedBuildTarget android = NamedBuildTarget.Android;

        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.forceInternetPermission = true;

        string actual = PlayerSettings.GetApplicationIdentifier(android);
        if (string.IsNullOrEmpty(actual) || actual.StartsWith("com.DefaultCompany") || actual.StartsWith("com.Unity"))
            PlayerSettings.SetApplicationIdentifier(android, Identificador);

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        if (PlayerSettings.productName == "MusTinko")
            PlayerSettings.productName = "Mus TINKO";

        EditorUserBuildSettings.buildAppBundle = false;

        Debug.Log("[ANDROID] Ajustes listos: IL2CPP, ARM64, permiso de Internet, horizontal, id " +
                  PlayerSettings.GetApplicationIdentifier(android) + ".");

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            bool cambiar = EditorUtility.DisplayDialog("Mus TINKO - Android",
                "La plataforma activa no es Android. Cambiar ahora? (Unity reimportara recursos y puede tardar unos minutos).",
                "Cambiar a Android", "Ahora no");

            if (cambiar && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                EditorUtility.DisplayDialog("Mus TINKO - Android",
                    "No se pudo cambiar a Android. Instala el modulo 'Android Build Support' (con SDK, NDK y OpenJDK) desde Unity Hub.",
                    "Vale");
        }
    }

    [MenuItem("Mus TINKO/Android/2. Generar APK")]
    public static void GenerarApk() => Construir(false, false);

    [MenuItem("Mus TINKO/Android/3. Generar APK de desarrollo (con logs)")]
    public static void GenerarApkDesarrollo() => Construir(true, false);

    [MenuItem("Mus TINKO/Android/4. Generar e instalar en el movil conectado por USB")]
    public static void GenerarEInstalar() => Construir(true, true);

    [MenuItem("Mus TINKO/Android/5. Build rapida de prueba (Mono, ARMv7)")]
    public static void BuildRapida()
    {
        if (!EditorUtility.DisplayDialog("Build rapida",
                "Cambia a Mono + ARMv7 para compilar mucho mas rapido. Solo funciona en moviles que aceptan apps de 32 bits " +
                "(muchos Motorola si, los Pixel 7 o mas nuevos no). Para la version buena usa otra vez la opcion 1.",
                "Hacer build rapida", "Cancelar"))
            return;

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        Construir(true, false);
    }

    // Las salas por Internet van activadas siempre (el paquete Multiplayer Services ya esta instalado).
    // Si algun dia dan problemas, "Desactivar" pone MUS_TINKO_SIN_INTERNET y se compila sin ellas.
    private const string SimboloSinInternet = "MUS_TINKO_SIN_INTERNET";
    private const string SimboloAntiguo = "MUS_TINKO_INTERNET";

    [MenuItem("Mus TINKO/Internet/Activar salas por Internet (Relay)")]
    public static void ActivarInternet() => CambiarSimbolo(true);

    [MenuItem("Mus TINKO/Internet/Desactivar salas por Internet")]
    public static void DesactivarInternet() => CambiarSimbolo(false);

    private static void CambiarSimbolo(bool activar)
    {
        foreach (NamedBuildTarget destino in new[] { NamedBuildTarget.Standalone, NamedBuildTarget.Android })
        {
            string actuales = PlayerSettings.GetScriptingDefineSymbols(destino);
            System.Collections.Generic.List<string> lista = new(actuales.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));
            lista.Remove(SimboloSinInternet);
            lista.Remove(SimboloAntiguo);

            if (!activar)
                lista.Add(SimboloSinInternet);

            PlayerSettings.SetScriptingDefineSymbols(destino, string.Join(";", lista));
        }

        Debug.Log("[INTERNET] Salas por Internet " + (activar ? "ACTIVADAS" : "desactivadas") +
                  ". Unity recompilara los scripts (si estas en Play, para y vuelve a darle).");
    }

    private static void Construir(bool desarrollo, bool instalar)
    {
        if (!File.Exists(EscenaJuego))
        {
            EditorUtility.DisplayDialog("Mus TINKO - Android", "No encuentro " + EscenaJuego, "Vale");
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Preparar();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                return;
        }

        Directory.CreateDirectory(CarpetaBuilds);
        string ruta = Path.Combine(CarpetaBuilds, "MusTinko" + (desarrollo ? "_dev" : "") + ".apk");

        BuildOptions opciones = BuildOptions.None;
        if (desarrollo) opciones |= BuildOptions.Development;
        if (instalar) opciones |= BuildOptions.AutoRunPlayer;

        BuildPlayerOptions configuracion = new()
        {
            scenes = new[] { EscenaJuego },
            locationPathName = ruta,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = opciones
        };

        BuildReport informe = BuildPipeline.BuildPlayer(configuracion);
        BuildSummary resumen = informe.summary;

        if (resumen.result == BuildResult.Succeeded)
        {
            Debug.Log("[ANDROID] APK generada: " + Path.GetFullPath(ruta) + " (" + (resumen.totalSize / (1024 * 1024)) +
                      " MB, " + resumen.totalTime.TotalMinutes.ToString("0.0") + " min)");
            EditorUtility.RevealInFinder(ruta);
        }
        else
        {
            Debug.LogError("[ANDROID] La build ha fallado: " + resumen.result + ". Revisa los errores de la consola.");
        }
    }
}
