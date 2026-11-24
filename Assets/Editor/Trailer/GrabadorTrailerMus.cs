#if MUS_RECORDER
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// GRABAR EL TRAILER EN MP4 (con Unity Recorder)
// Menu "Mus TINKO > Trailer > Grabar trailer en MP4":
//   abre la mesa online, entra en Play, el director (TrailerMus) rueda el trailer y, al acabar, sale de Play.
//   Mientras tanto Unity Recorder graba la vista de juego a 1920x1080 y 30 fotogramas por segundo.
//   El video queda en la carpeta "Trailer" del proyecto (al lado de Assets) y se abre la carpeta al terminar.
// Este archivo solo se compila si el paquete Unity Recorder esta instalado (asmdef con versionDefines).
[InitializeOnLoad]
public static class GrabadorTrailerMus
{
    private const string ClaveGrabando = "MusTinko.GrabandoTrailer";
    private const string ClaveArchivo = "MusTinko.ArchivoTrailer";
    private const string ClavePedido = "MusTinko_Trailer";   // la misma que TrailerMus.ClavePedido
    private const string Escena = "Assets/Scenes/MesamusOnline.unity";

    private static RecorderController controlador;

    static GrabadorTrailerMus()
    {
        EditorApplication.playModeStateChanged += AlCambiarPlay;
    }

    [MenuItem("Mus TINKO/Trailer/Grabar trailer en MP4")]
    public static void Grabar()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Mus TINKO - Trailer", "Para el juego (Stop) y vuelve a pulsar Grabar trailer.", "Vale");
            return;
        }

        if (SceneManager.GetActiveScene().path != Escena)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(Escena);
        }

        PlayerPrefs.SetInt(ClavePedido, 2);   // 2 = rodar y salir de Play al acabar
        PlayerPrefs.Save();
        SessionState.SetBool(ClaveGrabando, true);
        EditorApplication.EnterPlaymode();
    }

    private static void AlCambiarPlay(PlayModeStateChange cambio)
    {
        if (!SessionState.GetBool(ClaveGrabando, false))
            return;

        switch (cambio)
        {
            case PlayModeStateChange.EnteredPlayMode:
                Empezar();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                Parar();
                break;
            case PlayModeStateChange.EnteredEditMode:
                SessionState.SetBool(ClaveGrabando, false);
                string archivo = SessionState.GetString(ClaveArchivo, "");

                if (File.Exists(archivo))
                {
                    Debug.Log("[TRAILER] Listo: " + archivo);
                    EditorUtility.RevealInFinder(archivo);
                }
                else
                {
                    Debug.LogWarning("[TRAILER] No se encontro el video (" + archivo + "). Mira la consola por si Recorder dio algun error.");
                }

                break;
        }
    }

    private static void Empezar()
    {
        try
        {
            RecorderControllerSettings ajustes = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            controlador = new RecorderController(ajustes);

            string carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Trailer"));
            Directory.CreateDirectory(carpeta);
            string archivo = Path.Combine(carpeta, "MusTinko_trailer_" + DateTime.Now.ToString("yyyyMMdd_HHmm"));

            MovieRecorderSettings video = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            video.name = "Trailer Mus TINKO";
            video.Enabled = true;
            video.EncoderSettings = new CoreEncoderSettings
            {
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
                Codec = CoreEncoderSettings.OutputCodec.MP4
            };
            video.CaptureAlpha = false;
            video.CaptureAudio = true;
            video.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
            video.OutputFile = archivo;   // Recorder le pone el .mp4

            ajustes.AddRecorderSettings(video);
            ajustes.SetRecordModeToManual();
            ajustes.FrameRatePlayback = FrameRatePlayback.Constant;
            ajustes.FrameRate = 30f;
            ajustes.CapFrameRate = true;
            RecorderOptions.VerboseMode = false;

            controlador.PrepareRecording();
            controlador.StartRecording();
            SessionState.SetString(ClaveArchivo, archivo + ".mp4");
            Debug.Log("[TRAILER] Grabando en " + archivo + ".mp4");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            controlador = null;
        }
    }

    private static void Parar()
    {
        if (controlador == null)
            return;

        try
        {
            controlador.StopRecording();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        controlador = null;
    }
}
#endif
