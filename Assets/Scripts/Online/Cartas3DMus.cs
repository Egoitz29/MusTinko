using System;
using UnityEngine;
using UnityEngine.Rendering;

// Cartas 3D de Mus TINKO. La malla se hace en Blender (Herramientas/GeneradorCartas/carta3d.py)
// y se guarda en Resources/Modelos/Carta_Tinko_Malla.json ya en el sistema de Unity:
//   - mide 1 de alto (se escala al crearla)
//   - el anverso (dibujo) mira hacia -Z, como un Quad; el reverso hacia +Z
public static class Cartas3DMus
{
    public const string RutaMalla = "Modelos/Carta_Tinko_Malla";
    public const float Proporcion = 486f / 762f; // ancho / alto, como los dibujos de la baraja

    [Serializable]
    private class DatosMalla
    {
        public float[] v;
        public float[] n;
        public float[] uv;
        public int[] anverso;
        public int[] reverso;
        public int[] canto;
    }

    // Cuanto reflejan la luz el carton y el reverso (1 = normal). En tu taberna la luz es tenue y salian apagadas.
    public static float Brillo { get; private set; } = 0.8f;

    public static void PonerBrillo(float brillo)
    {
        Brillo = Mathf.Max(0.2f, brillo);

        if (matAnverso == null)
            return;

        Color c = new(Brillo, Brillo, Brillo, 1f);
        PonerColor(matAnverso, c);
        PonerColor(matReverso, c);
        PonerColor(matAnversoLiso, new Color(0.97f * Brillo, 0.95f * Brillo, 0.9f * Brillo, 1f));
    }

    private static void PonerColor(Material m, Color c)
    {
        m.color = c;

        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
    }

    private static Mesh malla;
    private static bool intentado;
    private static Material matAnverso, matAnversoLiso, matReverso, matCanto;
    private static string reversoCargado;
    private static MaterialPropertyBlock bloque;

    public static bool Disponible => Malla != null && Materiales();

    public static Mesh Malla
    {
        get
        {
            if (malla != null || intentado)
                return malla;

            intentado = true;
            TextAsset texto = Resources.Load<TextAsset>(RutaMalla);

            if (texto == null)
            {
                Debug.LogWarning("[CARTAS 3D] No se encontro Resources/" + RutaMalla + ": se usan las cartas de siempre.");
                return null;
            }

            DatosMalla d = JsonUtility.FromJson<DatosMalla>(texto.text);
            int cuantos = d.v.Length / 3;
            Vector3[] vertices = new Vector3[cuantos];
            Vector3[] normales = new Vector3[cuantos];
            Vector2[] uvs = new Vector2[cuantos];

            for (int i = 0; i < cuantos; i++)
            {
                vertices[i] = new Vector3(d.v[i * 3], d.v[i * 3 + 1], d.v[i * 3 + 2]);
                normales[i] = new Vector3(d.n[i * 3], d.n[i * 3 + 1], d.n[i * 3 + 2]);
                uvs[i] = new Vector2(d.uv[i * 2], d.uv[i * 2 + 1]);
            }

            malla = new Mesh { name = "Carta_Tinko" };
            malla.vertices = vertices;
            malla.normals = normales;
            malla.uv = uvs;
            malla.subMeshCount = 3;
            malla.SetTriangles(d.anverso, 0);
            malla.SetTriangles(d.reverso, 1);
            malla.SetTriangles(d.canto, 2);
            malla.RecalculateBounds();
            return malla;
        }
    }

    private static bool Materiales()
    {
        if (matAnverso != null)
            return true;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            return false;

        matAnverso = Nuevo(shader, Color.white, 0.35f);
        matAnversoLiso = Nuevo(shader, new Color(0.97f, 0.95f, 0.9f), 0.3f);
        matReverso = Nuevo(shader, Color.white, 0.4f);
        matCanto = Nuevo(shader, new Color(0.93f, 0.9f, 0.83f), 0.2f);
        bloque = new MaterialPropertyBlock();
        PonerBrillo(Brillo);

        // El dibujo sale de la baraja (todas las cartas estan en una sola imagen).
        Sprite cualquiera = BarajaSpritesMus.Obtener(CartasMus.Texto(0));

        if (cualquiera != null)
            PonerTextura(matAnverso, cualquiera.texture);

        return true;
    }

    private static Material Nuevo(Shader shader, Color color, float brillo)
    {
        Material m = new(shader);
        m.color = color;

        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", brillo);

        return m;
    }

    private static void PonerTextura(Material m, Texture t)
    {
        m.mainTexture = t;

        if (m.HasProperty("_BaseMap"))
            m.SetTexture("_BaseMap", t);
    }

    // Reverso que lleva puesto el jugador en la taquilla.
    private static void ActualizarReverso()
    {
        // Ojo: puede llamarse antes de crear los materiales (antes fallaba y el reverso salia en blanco para siempre).
        if (!Materiales())
            return;

        string ruta = TaquillaMus.RutaReverso;

        if (ruta == reversoCargado)
            return;

        Texture2D t = Resources.Load<Texture2D>(ruta);

        if (t == null)
            return;

        reversoCargado = ruta;
        PonerTextura(matReverso, t);
    }

    // Crea una carta de "alto" de altura. carta = texto de la carta ("1\nOROS"...) o null si no se sabe (cara lisa).
    public static GameObject Crear(string nombre, Transform padre, float alto, string carta)
    {
        if (!Disponible)
            return null;

        ActualizarReverso();

        GameObject go = new(nombre, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(padre, false);
        go.transform.localScale = Vector3.one * alto;
        go.GetComponent<MeshFilter>().sharedMesh = Malla;
        MeshRenderer r = go.GetComponent<MeshRenderer>();
        r.shadowCastingMode = ShadowCastingMode.Off;
        PonerCara(go, carta);
        return go;
    }

    public static void PonerCara(GameObject go, string carta)
    {
        if (go == null || !Disponible)
            return;

        MeshRenderer r = go.GetComponent<MeshRenderer>();
        Sprite dibujo = string.IsNullOrEmpty(carta) ? null : BarajaSpritesMus.Obtener(carta);
        r.sharedMaterials = new[] { dibujo != null ? matAnverso : matAnversoLiso, matReverso, matCanto };

        if (dibujo == null)
        {
            r.SetPropertyBlock(null, 0);
            return;
        }

        // Solo el trozo de la imagen de esta carta.
        Rect rect = dibujo.textureRect;
        Texture tex = dibujo.texture;
        Vector4 st = new(rect.width / tex.width, rect.height / tex.height, rect.x / tex.width, rect.y / tex.height);
        bloque.Clear();
        bloque.SetVector("_BaseMap_ST", st);
        bloque.SetVector("_MainTex_ST", st);
        r.SetPropertyBlock(bloque, 0);
    }

    // Para refrescar el reverso de las cartas que ya existen (al cambiarlo en la taquilla).
    public static void RefrescarReverso() => ActualizarReverso();
}
