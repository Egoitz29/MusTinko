
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PrepararPersonajesMusEditor
{
    private static readonly string[] Personajes =
    {
        "MOSTAZA", "CORAL", "LILA"
    };

    private static readonly string[] Clips =
    {
        "Reposo", "Sena_31", "Sena_Duples",
        "Sena_DosReyes", "Sena_DosAses"
    };

    private static readonly float[] Inicios =
    {
        1, 1, 50, 85, 125
    };

    private static readonly float[] Finales =
    {
        2, 40, 75, 115, 160
    };

    private const string CarpetaModelos =
        "Assets/Models/Personajes";

    private const string CarpetaAnimaciones =
        "Assets/Animations";

    private const string ControladorBase =
        "Assets/Animations/TINKO_MENTA_Controller_Fijo.controller";

    [MenuItem("Tools/Mus TINKO/Preparar tres personajes")]
    private static void Ejecutar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Deten Play antes de continuar.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControladorBase) == null)
        {
            Debug.LogError(
                "Falta el controlador corregido de MENTA."
            );
            return;
        }

        string documentos = Environment.GetFolderPath(
            Environment.SpecialFolder.MyDocuments
        );

        string origen = EditorUtility.OpenFolderPanel(
            "Selecciona ExportacionesUnity",
            documentos,
            ""
        );

        if (string.IsNullOrEmpty(origen))
            return;

        foreach (string personaje in Personajes)
        {
            string archivo =
                "TINKO_" + personaje + "_Animaciones.fbx";

            if (!File.Exists(Path.Combine(origen, archivo)))
            {
                Debug.LogError("Falta " + archivo);
                return;
            }
        }

        if (!AssetDatabase.IsValidFolder(CarpetaModelos) ||
            !AssetDatabase.IsValidFolder(CarpetaAnimaciones))
        {
            Debug.LogError(
                "Faltan Assets/Models/Personajes o Assets/Animations."
            );
            return;
        }

        foreach (string personaje in Personajes)
        {
            string archivo =
                "TINKO_" + personaje + "_Animaciones.fbx";

            string destino = CarpetaModelos + "/" + archivo;

            if (!File.Exists(Path.GetFullPath(destino)))
            {
                File.Copy(
                    Path.Combine(origen, archivo),
                    Path.GetFullPath(destino)
                );
            }
        }

        AssetDatabase.Refresh();

        foreach (string personaje in Personajes)
        {
            try
            {
                Preparar(personaje);
            }
            catch (Exception error)
            {
                Debug.LogError(
                    "[MUS TINKO] " + personaje +
                    ": " + error.Message
                );
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[MUS TINKO] Proceso finalizado. Revisa los resultados anteriores.");
    }

    private static void Preparar(string personaje)
    {
        string nombre = "TINKO_" + personaje;

        string rutaFbx =
            CarpetaModelos + "/" +
            nombre + "_Animaciones.fbx";

        var importer = AssetImporter.GetAtPath(rutaFbx)
            as ModelImporter;

        if (importer == null)
            throw new Exception("No se encontro el importador FBX.");

        var predeterminados = importer.defaultClipAnimations;

        if (predeterminados == null ||
            predeterminados.Length == 0)
        {
            throw new Exception("El FBX no contiene una toma animada.");
        }

        string toma = predeterminados[0].takeName;

        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;

        var configurados = new ModelImporterClipAnimation[Clips.Length];

        for (int i = 0; i < Clips.Length; i++)
        {
            configurados[i] = new ModelImporterClipAnimation
            {
                name = Clips[i],
                takeName = toma,
                firstFrame = Inicios[i],
                lastFrame = Finales[i],
                loopTime = i == 0
            };
        }

        importer.clipAnimations = configurados;
        importer.SaveAndReimport();

        string rutaController =
            CarpetaAnimaciones + "/" +
            nombre + "_Controller_Fijo.controller";

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(
                rutaController) != null)
        {
            Debug.LogWarning(
                nombre + ": ya existe su Controller. No se sobrescribe."
            );
            return;
        }

        string carpetaLimpia =
            CarpetaAnimaciones + "/" + nombre + "_Limpias";

        if (!AssetDatabase.IsValidFolder(carpetaLimpia))
        {
            AssetDatabase.CreateFolder(
                CarpetaAnimaciones,
                nombre + "_Limpias"
            );
        }

        var importados = AssetDatabase
            .LoadAllAssetsAtPath(rutaFbx)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .ToArray();

        var limpios = new Dictionary<string, AnimationClip>();

        foreach (string nombreClip in Clips)
        {
            AnimationClip original = importados.FirstOrDefault(
                c => c.name == nombreClip
            );

            if (original == null)
                throw new Exception("No se importo el clip " + nombreClip);

            AnimationClip copia = UnityEngine.Object.Instantiate(original);
            copia.name = nombreClip + "_Fijo";

            foreach (var binding in AnimationUtility.GetCurveBindings(copia))
            {
                if (!string.IsNullOrEmpty(binding.path))
                    continue;

                bool raizTransform =
                    binding.type == typeof(Transform) &&
                    (
                        binding.propertyName.StartsWith("m_LocalPosition") ||
                        binding.propertyName.StartsWith("m_LocalRotation") ||
                        binding.propertyName.StartsWith("localEulerAngles") ||
                        binding.propertyName.StartsWith("m_LocalScale")
                    );

                bool raizAnimator =
                    binding.type == typeof(Animator) &&
                    (
                        binding.propertyName.StartsWith("RootT") ||
                        binding.propertyName.StartsWith("RootQ")
                    );

                if (raizTransform || raizAnimator)
                    AnimationUtility.SetEditorCurve(copia, binding, null);
            }

            AssetDatabase.CreateAsset(
                copia,
                carpetaLimpia + "/" + copia.name + ".anim"
            );

            limpios.Add(nombreClip, copia);
        }

        if (!AssetDatabase.CopyAsset(
                ControladorBase, rutaController))
        {
            throw new Exception("No se pudo crear el Animator Controller.");
        }

        var controlador =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                rutaController
            );

        foreach (var capa in controlador.layers)
            Reemplazar(capa.stateMachine, limpios);

        EditorUtility.SetDirty(controlador);

        Debug.Log(
            "[MUS TINKO] " + personaje +
            ": Rig Generic, 5 clips y Controller Fijo creados."
        );
    }

    private static void Reemplazar(
        AnimatorStateMachine maquina,
        Dictionary<string, AnimationClip> nuevos)
    {
        foreach (var entrada in maquina.states)
        {
            var estado = entrada.state;

            if (estado.motion is AnimationClip anterior)
            {
                string nombre = anterior.name.Replace("_Fijo", "");

                if (nuevos.TryGetValue(nombre, out var nuevo))
                {
                    estado.motion = nuevo;
                    EditorUtility.SetDirty(estado);
                }
            }
        }

        foreach (var sub in maquina.stateMachines)
            Reemplazar(sub.stateMachine, nuevos);
    }
}
