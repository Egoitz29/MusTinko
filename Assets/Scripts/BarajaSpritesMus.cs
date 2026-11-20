using UnityEngine;

public static class BarajaSpritesMus
{
    public const string RutaPorDefecto = "Baraja/Cartas_Tinko";

    private static Sprite[] sprites;
    private static string rutaCargada;

    public static Sprite Obtener(string carta, string ruta = RutaPorDefecto)
    {
        if (!Cargar(ruta) || !Indice(carta, out int indice))
            return null;

        return sprites[indice];
    }

    public static bool Cargar(string ruta)
    {
        if (sprites != null && rutaCargada == ruta)
            return true;

        Sprite[] encontrados = Resources.LoadAll<Sprite>(ruta);
        Sprite[] porIndice = new Sprite[40];
        int total = 0;

        foreach (Sprite sprite in encontrados)
        {
            int separador = sprite.name.LastIndexOf('_');

            if (separador < 0 ||
                !int.TryParse(sprite.name.Substring(separador + 1), out int i) ||
                i < 0 || i >= 40 || porIndice[i] != null)
                continue;

            porIndice[i] = sprite;
            total++;
        }

        if (total != 40)
        {
            Debug.LogWarning("BarajaSpritesMus: encontradas " + total + "/40 cartas en Resources/" + ruta);
            return false;
        }

        sprites = porIndice;
        rutaCargada = ruta;
        return true;
    }

    public static bool Indice(string carta, out int indice)
    {
        indice = -1;

        if (string.IsNullOrEmpty(carta))
            return false;

        string[] partes = carta.Trim().ToUpperInvariant().Split('\n');

        if (partes.Length != 2)
            return false;

        int columna = System.Array.IndexOf(CartasMus.Valores, partes[0].Trim());
        int fila;

        switch (partes[1].Trim())
        {
            case "ESPADAS": fila = 0; break;
            case "COPAS": fila = 1; break;
            case "OROS": fila = 2; break;
            case "BASTOS": fila = 3; break;
            default: return false;
        }

        if (columna < 0)
            return false;

        indice = fila * 10 + columna;
        return true;
    }
}
