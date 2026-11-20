using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public class MontarTabernaMus : EditorWindow
{
    private const string NombreRaiz = "Taberna_TINKO";

    private Vector3 centro = Vector3.zero;
    private float giroY;
    private float radioLibre = 3f;
    private bool incluirTecho = true;
    private bool incluirMuebles = true;
    private bool incluirLuces = true;
    private float multiplicadorLuces = 1.5f;
    private bool ajustarAmbiente = true;
    private float intensidadSol = 0.35f;

    [MenuItem("Mus TINKO/Montar taberna alrededor de la mesa")]
    public static void Abrir()
    {
        GetWindow<MontarTabernaMus>("Montar taberna");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Monta la taberna del pack Low Poly Tavern (la de su escena de ejemplo) alrededor de tu mesa de Mus. " +
            "Se crea dentro de un objeto '" + NombreRaiz + "' en la escena abierta. Se puede deshacer con Ctrl+Z o con 'Quitar taberna'.",
            MessageType.Info);

        centro = EditorGUILayout.Vector3Field("Centro (tu mesa)", centro);
        giroY = EditorGUILayout.Slider("Girar la taberna", giroY, -180f, 180f);
        radioLibre = EditorGUILayout.Slider("Espacio libre alrededor de la mesa", radioLibre, 0f, 8f);
        incluirTecho = EditorGUILayout.Toggle("Techo", incluirTecho);
        incluirMuebles = EditorGUILayout.Toggle("Muebles y objetos", incluirMuebles);
        incluirLuces = EditorGUILayout.Toggle("Luces de antorchas y velas", incluirLuces);
        multiplicadorLuces = EditorGUILayout.Slider("Fuerza de las luces", multiplicadorLuces, 0.2f, 4f);
        ajustarAmbiente = EditorGUILayout.Toggle("Ambiente calido de interior", ajustarAmbiente);
        intensidadSol = EditorGUILayout.Slider("Luz del sol (Directional)", intensidadSol, 0f, 1.5f);

        EditorGUILayout.Space();

        if (GUILayout.Button("MONTAR TABERNA", GUILayout.Height(36)))
            Montar();

        if (GUILayout.Button("Quitar taberna"))
            Quitar();
    }

    private void Quitar()
    {
        GameObject anterior = GameObject.Find(NombreRaiz);

        if (anterior != null)
            Undo.DestroyObjectImmediate(anterior);
    }

    private void Montar()
    {
        Quitar();

        GameObject raiz = new(NombreRaiz);
        Undo.RegisterCreatedObjectUndo(raiz, "Montar taberna");
        raiz.transform.SetPositionAndRotation(centro, Quaternion.Euler(0f, giroY, 0f));

        Transform edificio = Hijo(raiz.transform, "Edificio");
        Transform techo = Hijo(raiz.transform, "Techo");
        Transform muebles = Hijo(raiz.transform, "Muebles");
        Transform luces = Hijo(raiz.transform, "Luces");

        Dictionary<string, GameObject> cache = new();
        int creados = 0;
        int omitidos = 0;

        foreach (Pieza p in Piezas)
        {
            bool esTecho = p.Nombre.Contains("Roof");
            bool esEdificio = esTecho || p.Nombre.Contains("Wall") || p.Nombre == "Floor Tile" || p.Nombre == "Iron Torch Bracket";

            if (esTecho && !incluirTecho) continue;
            if (!esEdificio && !incluirMuebles) continue;

            if (!esEdificio && new Vector2(p.Posicion.x, p.Posicion.z).magnitude < radioLibre)
            {
                omitidos++;
                continue;
            }

            if (!cache.TryGetValue(p.Nombre, out GameObject prefab))
            {
                prefab = Rutas.TryGetValue(p.Nombre, out string ruta) ? AssetDatabase.LoadAssetAtPath<GameObject>(ruta) : null;
                cache[p.Nombre] = prefab;
            }

            if (prefab == null)
            {
                omitidos++;
                continue;
            }

            Transform padre = esTecho ? techo : esEdificio ? edificio : muebles;
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            obj.transform.localPosition = p.Posicion;
            obj.transform.localRotation = p.Rotacion;
            obj.transform.localScale = p.Escala;
            creados++;
        }

        if (incluirLuces)
        {
            int i = 0;

            foreach (LuzTaberna l in Luces)
            {
                GameObject go = new("Luz_" + (++i));
                go.transform.SetParent(luces, false);
                go.transform.localPosition = l.Posicion;
                Light luz = go.AddComponent<Light>();
                luz.type = LightType.Point;
                luz.color = l.Color;
                luz.intensity = l.Intensidad * multiplicadorLuces;
                luz.range = l.Alcance;
                luz.shadows = l.Alcance > 6f ? LightShadows.Soft : LightShadows.None;
            }
        }

        if (ajustarAmbiente)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.1458f, 0.1894f, 0.2642f);
            RenderSettings.ambientEquatorColor = new Color(0.1981f, 0.1100f, 0.0589f);
            RenderSettings.ambientGroundColor = new Color(0.1132f, 0.0701f, 0.0283f);
            RenderSettings.ambientIntensity = 1f;
        }

        foreach (Light sol in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (sol.type != LightType.Directional) continue;
            Undo.RecordObject(sol, "Luz del sol");
            sol.intensity = intensidadSol;
        }

        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        Debug.Log("[TABERNA] Montada: " + creados + " piezas, " + omitidos + " omitidas (cerca de la mesa o sin prefab). Guarda la escena para conservarla.");
    }

    private static Transform Hijo(Transform padre, string nombre)
    {
        GameObject go = new(nombre);
        go.transform.SetParent(padre, false);
        return go.transform;
    }

    private readonly struct Pieza
    {
        public readonly string Nombre;
        public readonly Vector3 Posicion;
        public readonly Quaternion Rotacion;
        public readonly Vector3 Escala;

        public Pieza(string nombre, float px, float py, float pz, float qx, float qy, float qz, float qw, float sx, float sy, float sz)
        {
            Nombre = nombre;
            Posicion = new Vector3(px, py, pz);
            Rotacion = new Quaternion(qx, qy, qz, qw);
            Escala = new Vector3(sx, sy, sz);
        }
    }

    private readonly struct LuzTaberna
    {
        public readonly Vector3 Posicion;
        public readonly Color Color;
        public readonly float Intensidad;
        public readonly float Alcance;

        public LuzTaberna(float px, float py, float pz, float r, float g, float b, float intensidad, float alcance)
        {
            Posicion = new Vector3(px, py, pz);
            Color = new Color(r, g, b);
            Intensidad = intensidad;
            Alcance = alcance;
        }
    }

    private static readonly Dictionary<string, string> Rutas = new()
    {
        { "Backing Chair", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Backing Chair.prefab" },
        { "Bar Counter", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Bar Counter.prefab" },
        { "Clay Goblet", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Clay Goblet.prefab" },
        { "Corner Line Roof", "Assets/Low Poly Tarvern Pack/Prefabs/Building/Corner Line Roof.prefab" },
        { "Corner Line Roof 2", "Assets/Low Poly Tarvern Pack/Prefabs/Building/Corner Line Roof 2.prefab" },
        { "Corner Side Roof 2", "Assets/Low Poly Tarvern Pack/Prefabs/Building/Corner Side Roof 2.prefab" },
        { "Corner side Roof", "Assets/Low Poly Tarvern Pack/Prefabs/Building/Corner side Roof.prefab" },
        { "Dining Table", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Dining Table.prefab" },
        { "Empty Foam Tankard", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Empty Foam Tankard.prefab" },
        { "Fat Mead Jug", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Fat Mead Jug.prefab" },
        { "Floor Tile", "Assets/Low Poly Tarvern Pack/Prefabs/Building/Floor Tile.prefab" },
        { "Full Foam Tankard", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Full Foam Tankard.prefab" },
        { "Horizontal Keg", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Horizontal Keg.prefab" },
        { "Iron Torch Bracket", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Iron Torch Bracket.prefab" },
        { "Line Roof", "Assets/Low Poly Tarvern Pack/Prefabs/Building/Line Roof.prefab" },
        { "Long Wooden Bench", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Long Wooden Bench.prefab" },
        { "Round Large Table", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Round Large Table.prefab" },
        { "Side Roof", "Assets/Low Poly Tarvern Pack/Prefabs/Building/Side Roof.prefab" },
        { "Single Candle 1", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Single Candle 1.prefab" },
        { "Single Candle 2", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Single Candle 2.prefab" },
        { "Single Candle 3", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Single Candle 3.prefab" },
        { "Single Candle 4", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Single Candle 4.prefab" },
        { "Spilled Tankard", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Spilled Tankard.prefab" },
        { "Square Table", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Square Table.prefab" },
        { "Standard Stool", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Standard Stool.prefab" },
        { "Standing Barrel", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Standing Barrel.prefab" },
        { "Straight Wall", "Assets/Low Poly Tarvern Pack/Prefabs/Building/Straight Wall.prefab" },
        { "Tall Stool", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Tall Stool.prefab" },
        { "Thin Wine Bottle 1", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Thin Wine Bottle 1.prefab" },
        { "Thin Wine Bottle 2", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Thin Wine Bottle 2.prefab" },
        { "Wall Support", "Assets/Low Poly Tarvern Pack/Prefabs/Building/Wall Support.prefab" },
        { "Wall with Doorway", "Assets/Low Poly Tarvern Pack/Prefabs/Building/Wall with Doorway.prefab" },
        { "Wall with Window", "Assets/Low Poly Tarvern Pack/Prefabs/Building/Wall with Window.prefab" },
        { "Wooden Chest", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Wooden Chest.prefab" },
        { "Wooden Serving Plate", "Assets/Low Poly Tarvern Pack/Prefabs/Probs/Wooden Serving Plate.prefab" },
    };

    private static readonly LuzTaberna[] Luces =
    {
        new(4.2000f, 2.8000f, -4.8000f, 0.7453f, 0.5750f, 0.3269f, 0.6000f, 8.5937f),
        new(3.2000f, 2.8000f, 3.8000f, 0.7453f, 0.5750f, 0.3269f, 0.6000f, 8.5937f),
        new(13.4600f, 5.0000f, 1.8900f, 1.0000f, 0.6528f, 0.4009f, 2.0400f, 8.2000f),
        new(2.9330f, 2.2610f, 5.5040f, 1.0000f, 0.6528f, 0.4009f, 0.5400f, 2.2200f),
        new(-9.6000f, 3.4000f, -3.2000f, 0.7453f, 0.5750f, 0.3269f, 0.3800f, 8.5937f),
        new(10.3410f, 2.8040f, -6.7000f, 1.0000f, 0.6528f, 0.4009f, 0.7200f, 3.0996f),
        new(8.4000f, 4.8200f, 7.4600f, 1.0000f, 0.6528f, 0.4009f, 2.0400f, 8.2000f),
        new(-0.1800f, 5.0000f, 7.4000f, 1.0000f, 0.6528f, 0.4009f, 2.0400f, 8.2000f),
        new(8.8000f, 3.0400f, 6.0000f, 1.0000f, 0.6528f, 0.4009f, 0.7200f, 3.0996f),
        new(-9.8100f, 2.1500f, -5.8100f, 1.0000f, 0.6528f, 0.4009f, 0.6100f, 3.0600f),
        new(1.7130f, 2.0750f, -4.7090f, 1.0000f, 0.6528f, 0.4009f, 0.4300f, 2.4100f),
        new(-8.4600f, 5.0000f, -7.3300f, 1.0000f, 0.6528f, 0.4009f, 2.0400f, 8.2000f),
        new(8.8000f, 3.2000f, -0.4000f, 1.0000f, 0.6528f, 0.4009f, 0.7200f, 3.0996f),
        new(0.0300f, 5.0000f, -7.3300f, 1.0000f, 0.6528f, 0.4009f, 2.0400f, 8.2000f),
        new(13.3800f, 5.0000f, -4.9700f, 1.0000f, 0.6528f, 0.4009f, 2.0400f, 8.2000f),
        new(-7.8000f, 5.0000f, 7.4000f, 1.0000f, 0.6528f, 0.4009f, 2.0400f, 8.2000f),
        new(-9.7820f, 2.1500f, -3.5500f, 1.0000f, 0.6528f, 0.4009f, 0.1800f, 1.0100f),
    };

    private static readonly Pieza[] Piezas =
    {
        new("Straight Wall", 14.0000f, -0.2000f, 4.0000f, 0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Full Foam Tankard", 0.7369f, 1.4724f, -4.3328f, 0.0000f, 0.6728f, 0.0000f, 0.7398f, 1.0000f, 1.0000f, 1.0000f),
        new("Thin Wine Bottle 2", 3.2737f, 1.4732f, -4.5753f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Spilled Tankard", 3.2000f, 1.4689f, -5.4000f, 0.0000f, -0.5369f, 0.0000f, 0.8437f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 6.0000f, -0.2000f, -4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", -6.0000f, -0.2000f, -8.0000f, 0.0000f, 1.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Fat Mead Jug", 0.5584f, 1.4656f, -4.9629f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Standard Stool", -13.3600f, -0.0003f, 5.6000f, 0.0000f, 0.3851f, 0.0000f, 0.9229f, 1.0000f, 1.0000f, 1.0000f),
        new("Corner side Roof", -14.4000f, 5.8000f, -8.4000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Wooden Serving Plate", 8.3094f, 2.1767f, 7.1388f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Standard Stool", -9.6779f, -0.0003f, 3.4411f, 0.0000f, 0.1511f, 0.0000f, 0.9885f, 1.0000f, 1.0000f, 1.0000f),
        new("Corner Line Roof", 14.4000f, 5.8000f, 4.0000f, -0.0000f, 0.7071f, 0.0000f, -0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Wooden Serving Plate", -10.1389f, 1.4724f, 5.2960f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Side Roof", -2.0000f, 5.8000f, -8.4000f, -0.0000f, 0.7071f, 0.0000f, -0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Side Roof", -6.0000f, 5.8000f, 8.4000f, 0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Wooden Chest", 7.4000f, -0.0001f, -7.2000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Side Roof", 2.0000f, 5.8000f, -8.4000f, -0.0000f, 0.7071f, 0.0000f, -0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Full Foam Tankard", 8.4000f, 2.1767f, 0.8000f, 0.0000f, 0.6202f, 0.0000f, 0.7845f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 4", 8.9838f, 2.1767f, -0.8122f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Wooden Serving Plate", -1.8289f, 1.5546f, 5.9211f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", 14.0000f, -0.2000f, -4.0000f, 0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Dining Table", 2.0000f, -0.0002f, -4.8000f, 0.0000f, -0.0473f, 0.0000f, 0.9989f, 1.0000f, 1.0000f, 1.0000f),
        new("Thin Wine Bottle 1", 3.1860f, 1.5490f, 5.1200f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Thin Wine Bottle 1", -9.3480f, 1.4732f, -5.7110f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Standing Barrel", 12.9602f, -0.0001f, -7.0245f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Side Roof", 10.0000f, 5.8000f, 8.4000f, 0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Wooden Serving Plate", -9.9400f, 1.4724f, -4.2900f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Wall with Window", 6.0000f, -0.2000f, 8.4000f, 0.0203f, -0.0000f, 0.0000f, 0.9998f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 10.0000f, -0.2000f, -8.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Side Roof", 2.0000f, 5.8000f, 8.4000f, 0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Long Wooden Bench", -8.0000f, 0.2400f, -5.2000f, -0.4740f, -0.5180f, -0.4362f, 0.5628f, 1.0000f, 1.0000f, 1.0000f),
        new("Line Roof", -2.0000f, 9.4000f, 4.0000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 10.0000f, -0.2000f, 0.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Thin Wine Bottle 2", -10.4240f, 1.4732f, 4.7172f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Backing Chair", 2.0000f, -0.0001f, 4.8000f, 0.0000f, 0.4088f, 0.0000f, 0.9126f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", -10.0000f, -0.2000f, -8.0000f, 0.0000f, 1.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Corner Line Roof 2", -14.3859f, 5.8000f, 3.9998f, -0.0000f, 0.7071f, 0.0000f, -0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Line Roof", 2.0000f, 9.4000f, 4.0000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Thin Wine Bottle 1", 8.8311f, 2.1999f, 7.1520f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Wall with Window", 2.0000f, -0.2000f, -8.4000f, 0.1318f, -0.0000f, -0.0000f, 0.9913f, 1.0000f, 1.0000f, 1.0000f),
        new("Thin Wine Bottle 2", -9.5678f, 1.4732f, -6.0848f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", 2.0000f, -0.2000f, -8.0000f, 0.0000f, 1.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Line Roof", 6.0000f, 9.4000f, 4.0000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Square Table", 8.2000f, -0.0001f, -4.8000f, 0.0000f, 0.0433f, 0.0000f, 0.9991f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 4", -9.8400f, 1.4724f, -3.6700f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", -14.4000f, -0.2000f, 8.0000f, 0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", 14.0000f, -0.2000f, 8.0000f, 0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Line Roof", 2.0000f, 9.4000f, 0.0000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Iron Torch Bracket", -0.0900f, 3.6381f, 8.1590f, 0.0000f, -1.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Wall Support", 14.4000f, -0.2000f, 8.0000f, -0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Empty Foam Tankard", 10.8000f, 2.1044f, -6.6000f, 0.0000f, 0.2466f, 0.0000f, 0.9691f, 1.0000f, 1.0000f, 1.0000f),
        new("Wall with Window", -14.4000f, -0.2000f, -4.0000f, 0.1533f, -0.0000f, -0.0000f, 0.9882f, 1.0000f, 1.0000f, 1.0000f),
        new("Iron Torch Bracket", 0.0000f, 3.6381f, -8.1730f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Thin Wine Bottle 1", 8.7887f, 2.1999f, 0.2130f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Full Foam Tankard", 3.6000f, 1.5570f, 5.6000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Backing Chair", -3.4000f, -0.0001f, 6.4000f, 0.0000f, 0.8363f, 0.0000f, 0.5483f, 1.0000f, 1.0000f, 1.0000f),
        new("Standard Stool", 10.0000f, -0.0003f, -5.0000f, 0.0000f, 0.3851f, 0.0000f, 0.9229f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", -2.0000f, -0.2000f, 8.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Empty Foam Tankard", 13.2000f, 2.1044f, -6.7462f, 0.0000f, -0.4942f, 0.0000f, 0.8694f, 1.0000f, 1.0000f, 1.0000f),
        new("Corner Line Roof", 14.4000f, 5.8000f, 0.0000f, -0.0000f, 0.7071f, 0.0000f, -0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Tall Stool", 6.4000f, -0.0001f, 1.6000f, 0.0000f, -0.0839f, 0.0000f, 0.9965f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", 14.0000f, -0.2000f, -8.0000f, 0.0000f, 1.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Empty Foam Tankard", 11.0349f, 2.1036f, -7.0891f, 0.0000f, -0.2856f, 0.0000f, 0.9583f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", -6.0000f, -0.2000f, 4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", 14.0000f, -0.2000f, 0.0000f, 0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", -6.0000f, -0.2000f, -8.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", -14.0000f, -0.2000f, 8.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Fat Mead Jug", -1.7378f, 1.5474f, 5.1295f, 0.0000f, -0.5652f, 0.0000f, 0.8250f, 1.0000f, 1.0000f, 1.0000f),
        new("Side Roof", -10.0000f, 5.8000f, -8.4000f, -0.0000f, 0.7071f, 0.0000f, -0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 2", 8.7650f, 2.1767f, 5.9351f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Full Foam Tankard", 8.2074f, 2.1767f, 6.4000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 1", -9.8000f, 1.4724f, -5.8000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 14.0000f, -0.2000f, -8.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Long Wooden Bench", -11.2000f, -0.0001f, -4.8000f, 0.0000f, -0.6936f, 0.0000f, 0.7203f, 1.0000f, 1.0000f, 1.0000f),
        new("Spilled Tankard", -8.8500f, 1.4732f, -5.7280f, 0.0000f, -0.1372f, 0.0000f, 0.9905f, 1.0000f, 1.0000f, 1.0000f),
        new("Long Wooden Bench", 1.9256f, -0.0001f, -2.5307f, 0.0000f, 0.0284f, 0.0000f, 0.9996f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 4", -2.2324f, 1.5468f, 5.5140f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Side Roof", 6.0000f, 5.8000f, 8.4000f, 0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Empty Foam Tankard", -1.2147f, 1.5536f, 5.6651f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Wall Support", 14.0000f, -0.2000f, -8.4000f, -0.0000f, 0.0000f, -0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", -2.0000f, -0.2000f, -8.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Empty Foam Tankard", 12.7784f, 2.1044f, -7.2223f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Horizontal Keg", 12.0000f, -0.0136f, -4.4000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Standing Barrel", 10.8000f, -0.0001f, -7.0000f, 0.0000f, -0.3827f, 0.0000f, 0.9239f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", -2.0000f, -0.2000f, 0.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Standard Stool", -13.2000f, -0.0003f, 6.8000f, 0.0000f, 0.3851f, 0.0000f, 0.9229f, 1.0000f, 1.0000f, 1.0000f),
        new("Tall Stool", 7.2000f, -0.0001f, -1.2000f, 0.0000f, 0.1922f, 0.0000f, 0.9814f, 1.0000f, 1.0000f, 1.0000f),
        new("Bar Counter", 8.4000f, -0.0001f, 5.4000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Side Roof", -6.0000f, 5.8000f, -8.4000f, -0.0000f, 0.7071f, 0.0000f, -0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Thin Wine Bottle 2", 8.9371f, 2.1999f, 1.2575f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Clay Goblet", -10.7678f, 1.4724f, 5.6637f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Tall Stool", 7.0000f, 0.1930f, 3.0000f, -0.1111f, -0.1217f, 0.6512f, 0.7408f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", -2.0000f, -0.2000f, 4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 6.0000f, -0.2000f, -8.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Dining Table", -9.6000f, -0.0002f, -4.8000f, 0.0000f, 0.7596f, 0.0000f, 0.6504f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", 6.0000f, -0.2000f, 8.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 3", -9.2300f, 1.4724f, 4.8966f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 4", 2.9282f, 1.5527f, 5.5117f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Tall Stool", 7.0000f, -0.0001f, 4.2000f, 0.0000f, 0.2696f, 0.0000f, 0.9630f, 1.0000f, 1.0000f, 1.0000f),
        new("Tall Stool", 7.4000f, -0.0001f, 0.6000f, 0.0000f, 0.0202f, 0.0000f, 0.9998f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 10.0000f, -0.2000f, 4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Corner Side Roof 2", 14.4000f, 5.8000f, -8.4000f, 0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Iron Torch Bracket", -8.5200f, 3.6381f, -8.1730f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 4", 1.7223f, 1.4724f, -4.6945f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Spilled Tankard", 8.2000f, 2.1767f, 2.2000f, 0.0000f, 0.9330f, 0.0000f, 0.3599f, 1.0000f, 1.0000f, 1.0000f),
        new("Standard Stool", 13.2000f, -0.0003f, 0.2000f, 0.0000f, 0.3851f, 0.0000f, 0.9229f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 4", 10.3579f, 2.1044f, -6.8265f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Iron Torch Bracket", 14.1720f, 3.6381f, 1.8900f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Backing Chair", -3.2000f, -0.0001f, 4.4000f, 0.0000f, 0.6019f, 0.0000f, 0.7986f, 1.0000f, 1.0000f, 1.0000f),
        new("Standard Stool", -0.8000f, -0.0003f, -6.2000f, 0.0000f, 0.3851f, 0.0000f, 0.9229f, 1.0000f, 1.0000f, 1.0000f),
        new("Line Roof", 10.0000f, 9.4000f, 4.0000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Standard Stool", -13.0000f, -0.0003f, 4.4000f, 0.0000f, 0.4880f, 0.0000f, 0.8728f, 1.0000f, 1.0000f, 1.0000f),
        new("Line Roof", 10.0000f, 9.4000f, 0.0000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Long Wooden Bench", 2.0000f, -0.0001f, -6.8000f, 0.0000f, -0.0098f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Round Large Table", 2.9707f, -0.0001f, 5.3546f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Empty Foam Tankard", 10.4972f, 2.1035f, -7.1906f, 0.0000f, 0.4289f, 0.0000f, 0.9033f, 1.0000f, 1.0000f, 1.0000f),
        new("Corner side Roof", 14.4000f, 5.8000f, 8.4000f, 0.0000f, -0.7071f, 0.0000f, -0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 14.0000f, -0.2000f, 4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Tall Stool", 6.8341f, -0.0001f, 6.3416f, 0.0000f, -0.0839f, 0.0000f, 0.9965f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", -6.0000f, -0.2000f, -4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Line Roof", -2.0000f, 9.4000f, 0.0000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Iron Torch Bracket", 8.4000f, 3.6381f, 8.1590f, 0.0000f, -1.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 2", -9.0484f, 1.4724f, 4.8716f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 2.0000f, -0.2000f, 0.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Standard Stool", -6.8000f, -0.0003f, 3.6000f, 0.0000f, 0.2960f, 0.0000f, 0.9552f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 6.0000f, -0.2000f, 4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Side Roof", -2.0000f, 5.8000f, 8.4000f, 0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 6.0000f, -0.2000f, 0.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Wall with Window", 14.0000f, -0.2000f, 8.4000f, 0.0000f, -1.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", -10.0000f, -0.2000f, -4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 1", 9.0978f, 2.1767f, -0.4335f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Iron Torch Bracket", -7.8000f, 3.6381f, 8.1590f, 0.0000f, -1.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Wooden Chest", 13.2000f, -0.0001f, -1.6000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", -10.0000f, -0.2000f, 4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", -10.0000f, -0.2000f, -8.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Fat Mead Jug", 8.7002f, 2.1767f, 3.6652f, 0.0000f, -0.1736f, 0.0000f, 0.9848f, 1.0000f, 1.0000f, 1.0000f),
        new("Line Roof", -6.0000f, 9.4000f, 4.0000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 2.0000f, -0.2000f, 4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 10.0000f, -0.2000f, -4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", -6.0000f, -0.2000f, 0.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Wooden Serving Plate", 8.4000f, 2.1767f, -0.4000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 4", 8.7190f, 2.1767f, 5.7144f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", -6.0000f, -0.2000f, 8.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Corner Side Roof 2", -14.4000f, 5.8000f, 8.4000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Wall with Window", -14.4000f, -0.2000f, 0.0000f, 0.2401f, -0.0000f, -0.0000f, 0.9707f, 1.0000f, 1.0000f, 1.0000f),
        new("Long Wooden Bench", -9.1133f, -0.0001f, 6.7855f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 14.0000f, -0.2000f, -4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Line Roof", 6.0000f, 9.4000f, 0.0000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", -2.0000f, -0.2000f, -8.0000f, 0.0000f, 1.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Wall Support", -14.4000f, -0.2000f, -8.0000f, -0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Line Roof", -6.0000f, 9.4000f, 0.0000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 2.0000f, -0.2000f, -8.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Bar Counter", 8.4000f, -0.0001f, 0.4000f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Side Roof", 6.0000f, 5.8000f, -8.4000f, -0.0000f, 0.7071f, 0.0000f, -0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Corner Line Roof 2", -14.3858f, 5.8000f, -0.0002f, -0.0000f, 0.7071f, 0.0000f, -0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Backing Chair", 4.0000f, -0.0001f, 6.0000f, -0.0000f, 0.8518f, 0.0000f, -0.5239f, 1.0000f, 1.0000f, 1.0000f),
        new("Clay Goblet", -10.2521f, 1.4724f, -6.1735f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Wall with Doorway", -14.4000f, -0.2000f, 4.0000f, 0.0000f, 0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Backing Chair", 0.0000f, -0.0001f, 6.4000f, -0.0000f, 0.9809f, 0.0000f, -0.1945f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 2.0000f, -0.2000f, -4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Single Candle 1", 8.5159f, 2.1767f, 5.8779f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", 10.0000f, -0.2000f, -8.0000f, 0.0000f, 1.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Wooden Serving Plate", -8.3206f, 1.4581f, 5.3495f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Empty Foam Tankard", 8.6000f, 2.1767f, -1.4000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", -10.0000f, -0.2000f, 0.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Dining Table", -9.0000f, -0.0002f, 5.0000f, 0.0000f, -0.0258f, 0.0000f, 0.9997f, 1.0000f, 1.0000f, 1.0000f),
        new("Straight Wall", -10.0000f, -0.2000f, 8.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Iron Torch Bracket", 14.1720f, 3.6381f, -5.0800f, 0.0000f, -0.7071f, 0.0000f, 0.7071f, 1.0000f, 1.0000f, 1.0000f),
        new("Fat Mead Jug", 8.6950f, 2.1767f, 4.2807f, 0.0000f, -0.2757f, 0.0000f, 0.9612f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", -2.0000f, -0.2000f, -4.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Floor Tile", 14.0000f, -0.2000f, 0.0000f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Round Large Table", -1.6000f, -0.0001f, 5.3546f, 0.0000f, 0.0000f, 0.0000f, 1.0000f, 1.0000f, 1.0000f, 1.0000f),
        new("Wall Support", -14.0000f, -0.2000f, 8.4000f, -0.0000f, 1.0000f, 0.0000f, -0.0000f, 1.0000f, 1.0000f, 1.0000f),
    };
}
