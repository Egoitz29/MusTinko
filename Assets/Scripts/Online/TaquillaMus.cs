using System.Collections.Generic;
using UnityEngine;

// Catalogo de la taquilla y lo que el jugador ha comprado y lleva puesto.
// Se guarda en el dispositivo (PlayerPrefs), igual que las monedas.
public enum CategoriaTaquilla
{
    Traje,
    Reverso,
    ColorNombre,
    Frases
}

public sealed class ArticuloTaquilla
{
    public string Id;
    public CategoriaTaquilla Categoria;
    public string Nombre;
    public string Descripcion;
    public int Precio;
    public int Indice;
}

// Un traje cambia los complementos; el poncho mantiene el color del jugador (su identidad).
public sealed class TrajeMus
{
    public Color? Gorro, BordeGorro, Pompom, Manoplas, Botones, Piernas, Zapatos, Pelo;
}

public static class TaquillaMus
{
    public static readonly TrajeMus[] Trajes =
    {
        new(), // De la casa: los colores de siempre
        new() { Gorro = C(0.12f, 0.12f, 0.14f), BordeGorro = C(0.08f, 0.08f, 0.1f), Pompom = C(0.12f, 0.12f, 0.14f) },
        new() { Gorro = C(0.13f, 0.2f, 0.42f), BordeGorro = C(0.96f, 0.96f, 0.96f), Pompom = C(0.85f, 0.15f, 0.15f), Piernas = C(0.95f, 0.95f, 0.95f), Botones = C(0.95f, 0.76f, 0.28f) },
        new() { Gorro = C(0.45f, 0.3f, 0.18f), BordeGorro = C(0.35f, 0.22f, 0.13f), Pompom = C(0.85f, 0.82f, 0.75f), Manoplas = C(0.7f, 0.7f, 0.68f), Piernas = C(0.4f, 0.27f, 0.16f), Zapatos = C(0.12f, 0.1f, 0.09f) },
        new() { Gorro = C(0.85f, 0.1f, 0.12f), BordeGorro = C(0.98f, 0.98f, 0.98f), Pompom = C(0.85f, 0.1f, 0.12f), Manoplas = C(0.98f, 0.98f, 0.98f), Piernas = C(0.98f, 0.98f, 0.98f), Botones = C(0.85f, 0.1f, 0.12f) },
        new() { Gorro = C(0.55f, 0.55f, 0.56f), BordeGorro = C(0.4f, 0.4f, 0.42f), Pompom = C(0.9f, 0.9f, 0.9f), Pelo = C(0.85f, 0.85f, 0.85f), Piernas = C(0.3f, 0.3f, 0.33f) },
        new() { Gorro = C(0.06f, 0.06f, 0.07f), BordeGorro = C(0.95f, 0.76f, 0.28f), Pompom = C(0.95f, 0.76f, 0.28f), Botones = C(1f, 0.82f, 0.3f), Piernas = C(0.06f, 0.06f, 0.07f), Zapatos = C(0.05f, 0.05f, 0.05f), Manoplas = C(0.97f, 0.97f, 0.97f) },
    };

    public static readonly string[] Reversos = { "Baraja/Reverso_Clasico", "Baraja/Reverso_Tinko", "Baraja/Reverso_Verde", "Baraja/Reverso_Negro" };

    public static readonly string[] ColoresNombre = { "", "#FFD24D", "#FF6B5E", "#6EC6FF", "#7CD67C", "#FF8AD8" };

    public static readonly string[][] PacksFrases =
    {
        new[] { "¡Buena!", "¡Qué suerte!", "¡Vamos!", "Bien jugado", "Uff...", "¡Ja, ja!" },
        new[] { "¡Órdago!", "Eso no lo quieres", "¡Mus visto!", "Hoy no es tu día", "Paciencia...", "¡Toma ya!" },
        new[] { "¿Miedo o qué?", "Ni con esas cartas", "¡A la saca!", "Vuelve a casa", "Aprende, chaval", "Llora a tu pueblo" },
    };

    public static readonly ArticuloTaquilla[] Catalogo =
    {
        A("traje_casa", CategoriaTaquilla.Traje, "De la casa", "Los colores de siempre.", 0, 0),
        A("traje_txapela", CategoriaTaquilla.Traje, "Txapela negra", "Gorro negro, el de toda la vida.", 150, 1),
        A("traje_marinero", CategoriaTaquilla.Traje, "Marinero", "Azul marino, pompon rojo y pantalon blanco.", 300, 2),
        A("traje_pastor", CategoriaTaquilla.Traje, "Pastor", "Lana gris y tonos de monte.", 400, 3),
        A("traje_fiestas", CategoriaTaquilla.Traje, "De fiestas", "Rojo y blanco, listo para las fiestas.", 600, 4),
        A("traje_abuelo", CategoriaTaquilla.Traje, "Abuelo de taberna", "Pelo blanco y gorro gris. Sabe mucho.", 800, 5),
        A("traje_gala", CategoriaTaquilla.Traje, "De gala", "Negro y oro. Para las mesas grandes.", 1500, 6),

        A("reverso_clasico", CategoriaTaquilla.Reverso, "Clasico", "El de la baraja de siempre.", 0, 0),
        A("reverso_tinko", CategoriaTaquilla.Reverso, "TINKO", "Granate con el sello de la casa.", 300, 1),
        A("reverso_verde", CategoriaTaquilla.Reverso, "Tapete", "Verde como el tapete del casino.", 500, 2),
        A("reverso_negro", CategoriaTaquilla.Reverso, "Negro y oro", "Elegante. Para presumir.", 1000, 3),

        A("nombre_blanco", CategoriaTaquilla.ColorNombre, "Blanco", "Tu nombre normal.", 0, 0),
        A("nombre_oro", CategoriaTaquilla.ColorNombre, "Oro", "Tu nombre en dorado.", 250, 1),
        A("nombre_rojo", CategoriaTaquilla.ColorNombre, "Rojo", "Tu nombre en rojo.", 200, 2),
        A("nombre_azul", CategoriaTaquilla.ColorNombre, "Azul", "Tu nombre en azul.", 200, 3),
        A("nombre_verde", CategoriaTaquilla.ColorNombre, "Verde", "Tu nombre en verde.", 200, 4),
        A("nombre_rosa", CategoriaTaquilla.ColorNombre, "Rosa", "Tu nombre en rosa.", 200, 5),

        A("frases_basicas", CategoriaTaquilla.Frases, "Basicas", "Las de siempre.", 0, 0),
        A("frases_taberna", CategoriaTaquilla.Frases, "De taberna", "¡Órdago!, ¡Mus visto!, Hoy no es tu dia...", 150, 1),
        A("frases_picantes", CategoriaTaquilla.Frases, "Picantes", "Para picar a los rivales (con carino).", 300, 2),
    };

    private const string ClaveComprados = "MusTinko_Comprados";
    private const string ClaveEquipado = "MusTinko_Equipado_";

    public static event System.Action Cambio;

    private static HashSet<string> comprados;

    private static Color C(float r, float g, float b) => new(r, g, b);

    private static ArticuloTaquilla A(string id, CategoriaTaquilla cat, string nombre, string descripcion, int precio, int indice) =>
        new() { Id = id, Categoria = cat, Nombre = nombre, Descripcion = descripcion, Precio = precio, Indice = indice };

    private static HashSet<string> Comprados
    {
        get
        {
            if (comprados != null)
                return comprados;

            comprados = new HashSet<string>();
            string guardado = "";

            try { guardado = PlayerPrefs.GetString(ClaveComprados, ""); }
            catch { }

            foreach (string id in guardado.Split(','))
                if (id.Length > 0)
                    comprados.Add(id);

            return comprados;
        }
    }

    public static bool Tiene(ArticuloTaquilla a) => a.Precio <= 0 || Comprados.Contains(a.Id);

    public static bool Comprar(ArticuloTaquilla a)
    {
        if (Tiene(a))
            return true;

        if (!MonederoMus.Pagar(a.Precio))
            return false;

        Comprados.Add(a.Id);

        try
        {
            PlayerPrefs.SetString(ClaveComprados, string.Join(",", Comprados));
            PlayerPrefs.Save();
        }
        catch
        {
        }

        Equipar(a);
        return true;
    }

    public static int Equipado(CategoriaTaquilla cat)
    {
        try { return PlayerPrefs.GetInt(ClaveEquipado + cat, 0); }
        catch { return 0; }
    }

    public static bool EstaEquipado(ArticuloTaquilla a) => Equipado(a.Categoria) == a.Indice;

    public static void Equipar(ArticuloTaquilla a)
    {
        if (!Tiene(a))
            return;

        try
        {
            PlayerPrefs.SetInt(ClaveEquipado + a.Categoria, a.Indice);
            PlayerPrefs.Save();
        }
        catch
        {
        }

        Cambio?.Invoke();
    }

    // Lo que ven los demas: traje y color del nombre en un solo numero.
    public static int AspectoPropio => Mathf.Clamp(Equipado(CategoriaTaquilla.Traje), 0, Trajes.Length - 1) * 100 +
                                       Mathf.Clamp(Equipado(CategoriaTaquilla.ColorNombre), 0, ColoresNombre.Length - 1);

    public static int TrajeDe(int aspecto) => Mathf.Clamp(aspecto / 100, 0, Trajes.Length - 1);
    public static int ColorNombreDe(int aspecto) => Mathf.Clamp(aspecto % 100, 0, ColoresNombre.Length - 1);

    public static string RutaReverso => Reversos[Mathf.Clamp(Equipado(CategoriaTaquilla.Reverso), 0, Reversos.Length - 1)];
    public static string[] Frases => PacksFrases[Mathf.Clamp(Equipado(CategoriaTaquilla.Frases), 0, PacksFrases.Length - 1)];

    // Nombre coloreado si ha comprado un color; si no, el color que se pase (normalmente el del personaje).
    public static string NombreConColor(string nombre, int aspecto, string colorPorDefecto)
    {
        string hex = ColoresNombre[ColorNombreDe(aspecto)];

        if (hex.Length == 0)
            hex = colorPorDefecto;

        return string.IsNullOrEmpty(hex) ? "<b>" + nombre + "</b>" : "<b><color=" + hex + ">" + nombre + "</color></b>";
    }
}
