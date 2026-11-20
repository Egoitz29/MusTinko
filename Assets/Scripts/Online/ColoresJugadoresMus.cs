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
