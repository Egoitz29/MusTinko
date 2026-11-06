
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class RepararAnimacionesMenta
{
    [MenuItem("Tools/Mus TINKO/Reparar movimiento MENTA")]
    private static void Ejecutar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Deten Play antes de reparar las animaciones.");
            return;
        }

        string origen = "Assets/Animations/TINKO_MENTA_Controller.controller";
        string destino = "Assets/Animations/TINKO_MENTA_Controller_Fijo.controller";
        string carpeta = "Assets/Animations/MENTA_Limpias";

        var original = AssetDatabase.LoadAssetAtPath<AnimatorController>(origen);

        if (original == null)
        {
            Debug.LogError("No encuentro TINKO_MENTA_Controller.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(destino) != null)
        {
            Debug.LogWarning("El Controller corregido ya existe. No se ha sobrescrito.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(carpeta))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "MENTA_Limpias");
        }

        if (!AssetDatabase.CopyAsset(origen, destino))
        {
            Debug.LogError("No se pudo copiar el Controller.");
            return;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(destino);
        var reemplazos = new Dictionary<AnimationClip, AnimationClip>();

        var nombres = new HashSet<string>
        {
            "Reposo",
            "Sena_31",
            "Sena_Duples",
            "Sena_DosReyes",
            "Sena_DosAses"
        };

        int curvasEliminadas = 0;
        int clipsProcesados = 0;

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip == null || !nombres.Contains(clip.name))
                continue;

            AnimationClip copia = Object.Instantiate(clip);
            copia.name = clip.name + "_Fijo";

            int eliminadasClip = 0;

            foreach (var binding in AnimationUtility.GetCurveBindings(copia))
            {
                if (!string.IsNullOrEmpty(binding.path))
                    continue;

                bool transformRaiz =
                    binding.type == typeof(Transform) &&
                    (
                        binding.propertyName.StartsWith("m_LocalPosition") ||
                        binding.propertyName.StartsWith("m_LocalRotation") ||
                        binding.propertyName.StartsWith("localEulerAngles") ||
                        binding.propertyName.StartsWith("m_LocalScale")
                    );

                bool movimientoRaiz =
                    binding.type == typeof(Animator) &&
                    (
                        binding.propertyName.StartsWith("RootT") ||
                        binding.propertyName.StartsWith("RootQ")
                    );

                if (!transformRaiz && !movimientoRaiz)
                    continue;

                AnimationUtility.SetEditorCurve(copia, binding, null);
                eliminadasClip++;
            }

            string ruta = AssetDatabase.GenerateUniqueAssetPath(
                carpeta + "/" + copia.name + ".anim"
            );

            AssetDatabase.CreateAsset(copia, ruta);
            reemplazos.Add(clip, copia);

            curvasEliminadas += eliminadasClip;
            clipsProcesados++;

            Debug.Log(
                "[MENTA] " + clip.name +
                ": curvas de raiz eliminadas = " +
                eliminadasClip
            );
        }

        foreach (var layer in controller.layers)
        {
            ReemplazarClips(layer.stateMachine, reemplazos);
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[MENTA] Reparacion terminada. Clips: " +
            clipsProcesados +
            ". Curvas eliminadas: " +
            curvasEliminadas
        );

        if (curvasEliminadas == 0)
        {
            Debug.LogWarning(
                "[MENTA] No se encontraron curvas directas de raiz. " +
                "Hay que revisar los bindings antes de considerar corregido el desplazamiento."
            );
        }
    }

    private static void ReemplazarClips(
        AnimatorStateMachine maquina,
        Dictionary<AnimationClip, AnimationClip> reemplazos)
    {
        foreach (var entrada in maquina.states)
        {
            AnimatorState estado = entrada.state;

            if (estado.motion is AnimationClip clip &&
                reemplazos.TryGetValue(clip, out AnimationClip nuevo))
            {
                estado.motion = nuevo;
                EditorUtility.SetDirty(estado);
            }
        }

        foreach (var submaquina in maquina.stateMachines)
        {
            ReemplazarClips(submaquina.stateMachine, reemplazos);
        }
    }
}
