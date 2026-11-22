using UnityEngine;

public static class ColoresJugadoresMus
{
    public static readonly string[] Nombres = { "LILA", "MOSTAZA", "MENTA", "CORAL" };

    public static readonly Color[] Colores =
    {
        new(0.72f, 0.55f, 0.94f),
        new(0.85f, 0.68f, 0.12f),
        new(0.45f, 0.88f, 0.65f),
        new(1.0f, 0.5f, 0.42f)
    };

    public static readonly string[] Hex = { "#B88CF0", "#D9AE1F", "#74E0A6", "#FF7F6B" };

    public static string Nombre(int puesto)
    {
        if (puesto < 0 || puesto > 3)
            return "-";

        return "<color=" + Hex[puesto] + "><b>" + Nombres[puesto] + "</b></color>";
    }

    private static readonly Color Piel = new(1f, 0.85f, 0.72f);
    private static readonly Color Crema = new(0.97f, 0.94f, 0.86f);
    private static readonly Color Oro = new(0.95f, 0.76f, 0.28f);

    private static readonly Color[] Pelo =
    {
        new(0.36f, 0.22f, 0.13f),
        new(0.80f, 0.58f, 0.25f),
        new(0.14f, 0.12f, 0.12f),
        new(0.58f, 0.24f, 0.12f)
    };

    public static void AplicarAspecto(GameObject raiz, int puesto, float saturacionRopa, float brillo, int traje = 0)
    {
        TrajeMus t = TaquillaMus.Trajes[Mathf.Clamp(traje, 0, TaquillaMus.Trajes.Length - 1)];
        if (raiz == null || puesto < 0 || puesto > 3)
            return;

        Color ropa = Color.Lerp(Color.white, Colores[puesto], Mathf.Clamp01(saturacionRopa));
        Color ropaOscura = ropa * 0.82f;
        ropaOscura.a = 1f;
        Color manoplas = Color.Lerp(ropa, Color.white, 0.55f);
        Color pelo = Pelo[puesto];
        MaterialPropertyBlock bloque = new();

        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))
                continue;

            if (r.GetComponentInParent<BocadilloMus3D>() != null)
                continue;

            string pieza = Pieza(r.name);
            Color? color = ColorDeTraje(pieza, t) ?? ColorDePieza(pieza, ropa, ropaOscura, manoplas, pelo);

            if (!color.HasValue)
                continue;

            for (int i = 0; i < r.sharedMaterials.Length; i++)
            {
                r.GetPropertyBlock(bloque, i);
                bloque.SetColor("_BaseColor", color.Value);
                bloque.SetColor("_Color", color.Value);
                bloque.SetFloat("_Smoothness", brillo);
                bloque.SetFloat("_Glossiness", brillo);
                r.SetPropertyBlock(bloque, i);
            }
        }
    }

    private static string Pieza(string nombre)
    {
        int primero = nombre.IndexOf('_');
        int segundo = primero >= 0 ? nombre.IndexOf('_', primero + 1) : -1;
        return segundo >= 0 ? nombre.Substring(segundo + 1) : nombre;
    }

    // Lo que cambia un traje de la taquilla (si no lo cambia, devuelve null y se usa lo de siempre).
    private static Color? ColorDeTraje(string pieza, TrajeMus t)
    {
        if (pieza == "Gorro_ParteAlta") return t.Gorro;
        if (pieza == "Gorro_Borde") return t.BordeGorro;
        if (pieza == "Gorro_Pompom") return t.Pompom;
        if (pieza.StartsWith("Manopla") || pieza.StartsWith("Pulgar")) return t.Manoplas;
        if (pieza.StartsWith("Boton") || pieza.StartsWith("Etiqueta")) return t.Botones;
        if (pieza.StartsWith("Pierna")) return t.Piernas;
        if (pieza.StartsWith("Zapato")) return t.Zapatos;
        if (pieza.StartsWith("CEJA") || pieza.StartsWith("Pelo") || pieza.StartsWith("Flequillo")) return t.Pelo;
        return null;
    }

    private static Color? ColorDePieza(string pieza, Color ropa, Color ropaOscura, Color manoplas, Color pelo)
    {
        if (pieza.StartsWith("Cabeza") || pieza.StartsWith("Oreja") || pieza == "Cuello" || pieza.StartsWith("Parpado"))
            return Piel;
        if (pieza.StartsWith("Nariz")) return new Color(1f, 0.76f, 0.66f);
        if (pieza.StartsWith("Mejilla")) return new Color(1f, 0.58f, 0.58f);
        if (pieza.StartsWith("BlancoOjo") || pieza.StartsWith("BrilloOjo")) return new Color(0.98f, 0.98f, 0.98f);
        if (pieza.StartsWith("Pupila")) return new Color(0.1f, 0.08f, 0.08f);
        if (pieza.StartsWith("CEJA") || pieza.StartsWith("Pelo") || pieza.StartsWith("Flequillo")) return pelo;
        if (pieza.StartsWith("Sonrisa")) return new Color(0.5f, 0.14f, 0.14f);
        if (pieza == "Boca_Mordisco_Labio") return new Color(0.82f, 0.32f, 0.32f);
        if (pieza == "Boca_Mordisco_Dientes") return new Color(0.98f, 0.98f, 0.95f);
        if (pieza == "Boca_Mordisco_Fondo") return new Color(0.25f, 0.06f, 0.06f);
        if (pieza.StartsWith("Lengua")) return new Color(0.95f, 0.45f, 0.5f);
        if (pieza.StartsWith("Poncho") || pieza == "Gorro_ParteAlta") return ropa;
        if (pieza.StartsWith("Manga") || pieza == "Gorro_Borde") return ropaOscura;
        if (pieza.StartsWith("Manopla") || pieza.StartsWith("Pulgar")) return manoplas;
        if (pieza == "Gorro_Pompom" || pieza == "Cuello_Color") return Crema;
        if (pieza.StartsWith("Boton") || pieza.StartsWith("Etiqueta")) return Oro;
        if (pieza.StartsWith("Pierna")) return new Color(0.26f, 0.23f, 0.3f);
        if (pieza.StartsWith("Zapato")) return new Color(0.32f, 0.19f, 0.11f);
        return null;
    }

    private static readonly string[] PartesSinColor = { "ojo", "eye", "pupil", "iris", "boca", "mouth", "diente", "teeth", "lengua", "tongue" };

    public static void Colorear(GameObject raiz, Color color, float intensidad)
    {
        if (raiz == null)
            return;

        Color tinte = Color.Lerp(Color.white, color, Mathf.Clamp01(intensidad));
        MaterialPropertyBlock bloque = new();

        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))
                continue;

            if (r.GetComponentInParent<BocadilloMus3D>() != null)
                continue;

            Material[] materiales = r.sharedMaterials;

            for (int i = 0; i < materiales.Length; i++)
            {
                if (materiales[i] == null || EsParteSinColor(materiales[i].name))
                    continue;

                r.GetPropertyBlock(bloque, i);
                bloque.SetColor("_BaseColor", tinte);
                bloque.SetColor("_Color", tinte);
                r.SetPropertyBlock(bloque, i);
            }
        }
    }

    private static bool EsParteSinColor(string nombre)
    {
        string n = nombre.ToLowerInvariant();

        foreach (string parte in PartesSinColor)
            if (n.Contains(parte))
                return true;

        return false;
    }
}
