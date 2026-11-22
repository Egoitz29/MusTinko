using System;
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

    // Que personaje (0 LILA, 1 MOSTAZA, 2 MENTA, 3 CORAL) hay en cada silla.
    // Cada jugador lleva SU personaje a la silla donde se sienta; los bots se quedan los que sobran.
    private static readonly int[] personajeDePuesto = { 0, 1, 2, 3 };
    // Nombre que se ve de cada silla: el apodo si es una persona, el del personaje si es un bot.
    public static readonly string[] NombresPorPuesto = { "LILA", "MOSTAZA", "MENTA", "CORAL" };
    private static readonly string[] apodos = { "", "", "", "" };

    public static void PonerApodos(string a0, string a1, string a2, string a3)
    {
        apodos[0] = a0 ?? "";
        apodos[1] = a1 ?? "";
        apodos[2] = a2 ?? "";
        apodos[3] = a3 ?? "";
        RecalcularNombres();
    }

    private static void RecalcularNombres()
    {
        for (int i = 0; i < 4; i++)
            NombresPorPuesto[i] = apodos[i].Length > 0 ? apodos[i] : Nombres[personajeDePuesto[i]];
    }
    public static int Version { get; private set; }

    public static int PersonajeDe(int puesto) => puesto >= 0 && puesto < 4 ? personajeDePuesto[puesto] : 0;
    public static string HexDe(int puesto) => Hex[PersonajeDe(puesto)];
    public static Color ColorDe(int puesto) => Colores[PersonajeDe(puesto)];

    public static void PonerPersonajes(int p0, int p1, int p2, int p3)
    {
        int[] nuevos = { p0, p1, p2, p3 };
        bool igual = true;

        for (int i = 0; i < 4; i++)
        {
            nuevos[i] = Mathf.Clamp(nuevos[i], 0, 3);
            igual &= nuevos[i] == personajeDePuesto[i];
        }

        if (igual)
            return;

        for (int i = 0; i < 4; i++)
            personajeDePuesto[i] = nuevos[i];

        RecalcularNombres();
        Version++;
    }

    // Reparte personajes: primero los que se piden (si no estan cogidos), luego los que sobran.
    public static int[] Repartir(int[] pedidos)
    {
        int[] r = { -1, -1, -1, -1 };
        bool[] usado = new bool[4];

        for (int p = 0; p < 4; p++)
        {
            int k = pedidos[p];

            if (k >= 0 && k < 4 && !usado[k])
            {
                r[p] = k;
                usado[k] = true;
            }
        }

        for (int p = 0; p < 4; p++)
        {
            if (r[p] >= 0)
                continue;

            int k = !usado[p] ? p : Array.IndexOf(usado, false);
            r[p] = k;
            usado[k] = true;
        }

        return r;
    }

    public static string Nombre(int puesto)
    {
        if (puesto < 0 || puesto > 3)
            return "-";

        return "<color=" + Hex[PersonajeDe(puesto)] + "><b>" + NombresPorPuesto[puesto] + "</b></color>";
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

    // "personaje" es 0 LILA, 1 MOSTAZA, 2 MENTA, 3 CORAL (sus colores de ropa y pelo).
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
        if (pieza == "Gorro_ParteAlta" || pieza.StartsWith("Txapela")) return t.Gorro;
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
        // Complementos (bigote y barba van como "Pelo_..." y cogen el color del pelo).
        if (pieza.StartsWith("Gafas")) return new Color(0.1f, 0.09f, 0.09f);
        if (pieza == "Puro") return new Color(0.42f, 0.24f, 0.12f);
        if (pieza == "Brasa") return new Color(1f, 0.45f, 0.1f);
        if (pieza == "Ceniza") return new Color(0.75f, 0.73f, 0.7f);
        if (pieza.StartsWith("Pajarita") || pieza == "Cinta") return new Color(0.75f, 0.1f, 0.14f);
        if (pieza.StartsWith("Txapela")) return new Color(0.12f, 0.12f, 0.14f);
        if (pieza.StartsWith("Sombrero")) return new Color(0.08f, 0.08f, 0.09f);
        if (pieza.StartsWith("Bufanda")) return ropaOscura;
        if (pieza == "Raya") return Crema;
        if (pieza == "Pendiente") return Oro;
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
