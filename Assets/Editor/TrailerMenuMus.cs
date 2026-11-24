using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Ver el trailer sin grabarlo (por ejemplo para grabarlo con Win+Alt+R o con OBS).
// Para grabarlo directamente en MP4: "Mus TINKO > Trailer > Grabar trailer en MP4" (necesita Unity Recorder).
public static class TrailerMenuMus
{
    private const string Escena = "Assets/Scenes/MesamusOnline.unity";

    [MenuItem("Mus TINKO/Trailer/Ver el trailer (sin grabar)")]
    public static void Ver()
    {
        if (EditorApplication.isPlaying)
        {
            TrailerMus director = Object.FindFirstObjectByType<TrailerMus>();

            if (director != null)
                director.Rodar(false);
            return;
        }

        if (SceneManager.GetActiveScene().path != Escena)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(Escena);
        }

        PlayerPrefs.SetInt(TrailerMus.ClavePedido, 1);
        PlayerPrefs.Save();
        EditorApplication.EnterPlaymode();
    }
}
