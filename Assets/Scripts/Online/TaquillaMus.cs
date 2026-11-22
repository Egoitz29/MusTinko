using System.Collections.Generic;
using UnityEngine;

// Catalogo de la taquilla y lo que el jugador ha comprado y lleva puesto.
// Se guarda en el dispositivo (PlayerPrefs), igual que las monedas.
public enum CategoriaTaquilla
{
    Personaje,
    Traje,
    Complemento,
    Reverso,
    ColorNombre
}

// Donde va cada complemento: solo uno por hueco (no dos sombreros a la vez).
public enum HuecoComplemento
{
    Cabeza,
    Ojos,
    Pelo,
    Boca,
    Cuello,
    Oreja
}

public sealed class ComplementoMus
{
    public string Nombre;           // como se llama en el FBX: ACC_<Nombre>_...
    public HuecoComplemento Hueco;
    public bool TapaCabeza;         // esconde el gorro o el pelo de arriba
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

    // Frases rapidas del chat: las mismas para todos.
    public static readonly string[] Frases = { "¡Buena!", "¡Qué suerte!", "¡Vamos!", "Bien jugado", "Uff...", "¡Órdago!" };

    public static readonly ComplementoMus[] Complementos =
    {
        new() { Nombre = "Txapela", Hueco = HuecoComplemento.Cabeza, TapaCabeza = true },
        new() { Nombre = "SombreroCopa", Hueco = HuecoComplemento.Cabeza, TapaCabeza = true },
        new() { Nombre = "Gafas", Hueco = HuecoComplemento.Ojos },
        new() { Nombre = "Bigote", Hueco = HuecoComplemento.Pelo },
        new() { Nombre = "Barba", Hueco = HuecoComplemento.Pelo },
        new() { Nombre = "Puro", Hueco = HuecoComplemento.Boca },
        new() { Nombre = "Pajarita", Hueco = HuecoComplemento.Cuello },
        new() { Nombre = "Bufanda", Hueco = HuecoComplemento.Cuello },
        new() { Nombre = "Pendiente", Hueco = HuecoComplemento.Oreja },
    };

    // Lo que lleva cada personaje por defecto (y los bots): asi se distinguen de un vistazo.
    public static readonly int[] ComplementosPorDefecto =
    {
        Bits(2, 8),       // LILA: gafas y pendiente
        Bits(0, 3, 5),    // MOSTAZA: txapela, bigote y puro
        Bits(4, 7),       // MENTA: barba y bufanda
        Bits(1, 6),       // CORAL: sombrero de copa y pajarita
    };

    private static int Bits(params int[] indices)
    {
        int m = 0;
        foreach (int i in indices) m |= 1 << i;
        return m;
    }

    public static readonly ArticuloTaquilla[] Catalogo =
    {
        A("pj_lila", CategoriaTaquilla.Personaje, "LILA", "Gafas redondas y pendiente. Lista y tranquila.", 0, 0),
        A("pj_mostaza", CategoriaTaquilla.Personaje, "MOSTAZA", "Txapela, bigotazo y puro. El de toda la vida.", 0, 1),
        A("pj_menta", CategoriaTaquilla.Personaje, "MENTA", "Barba y bufanda. No se le escapa una.", 0, 2),
        A("pj_coral", CategoriaTaquilla.Personaje, "CORAL", "Sombrero de copa y pajarita. Muy fino.", 0, 3),

        A("cp_txapela", CategoriaTaquilla.Complemento, "Txapela grande", "La boina de siempre, bien ancha.", 200, 0),
        A("cp_copa", CategoriaTaquilla.Complemento, "Sombrero de copa", "Para las mesas de 1.000.", 600, 1),
        A("cp_gafas", CategoriaTaquilla.Complemento, "Gafas redondas", "Para ver bien las senas.", 250, 2),
        A("cp_bigote", CategoriaTaquilla.Complemento, "Bigotazo", "Un bigote con las puntas para arriba.", 300, 3),
        A("cp_barba", CategoriaTaquilla.Complemento, "Barba", "Barba, perilla y patillas.", 350, 4),
        A("cp_puro", CategoriaTaquilla.Complemento, "Puro", "Encendido y todo.", 400, 5),
        A("cp_pajarita", CategoriaTaquilla.Complemento, "Pajarita", "Roja, muy elegante.", 200, 6),
        A("cp_bufanda", CategoriaTaquilla.Complemento, "Bufanda", "Del color de tu ropa.", 250, 7),
        A("cp_pendiente", CategoriaTaquilla.Complemento, "Pendiente", "Un aro de oro.", 150, 8),

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

    // Los complementos de tu personaje por defecto son gratis.
    public static bool Tiene(ArticuloTaquilla a) => a.Precio <= 0 || Comprados.Contains(a.Id) ||
        (a.Categoria == CategoriaTaquilla.Complemento && (ComplementosPorDefecto[PersonajeMio] & (1 << a.Indice)) != 0);

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

    public static bool EstaEquipado(ArticuloTaquilla a) => a.Categoria == CategoriaTaquilla.Complemento
        ? (ComplementosMios & (1 << a.Indice)) != 0
        : a.Categoria == CategoriaTaquilla.Personaje ? PersonajeMio == a.Indice : Equipado(a.Categoria) == a.Indice;

    // ---------- Mi personaje y mis complementos ----------

    private const string ClavePersonaje = "MusTinko_Personaje";
    private const string ClaveComplementos = "MusTinko_Complementos";

    public static int PersonajeMio
    {
        get
        {
            try { return Mathf.Clamp(PlayerPrefs.GetInt(ClavePersonaje, 0), 0, 3); }
            catch { return 0; }
        }
    }

    // Si nunca los has tocado, llevas los de tu personaje.
    public static int ComplementosMios
    {
        get
        {
            try { return PlayerPrefs.GetInt(ClaveComplementos, -1) is int m && m >= 0 ? m : ComplementosPorDefecto[PersonajeMio]; }
            catch { return ComplementosPorDefecto[PersonajeMio]; }
        }
    }

    public static void QuitarComplemento(ArticuloTaquilla a)
    {
        GuardarComplementos(ComplementosMios & ~(1 << a.Indice));
    }

    private static void GuardarComplementos(int mascara)
    {
        try
        {
            PlayerPrefs.SetInt(ClaveComplementos, mascara);
            PlayerPrefs.Save();
        }
        catch
        {
        }

        Cambio?.Invoke();
    }

    public static void Equipar(ArticuloTaquilla a)
    {
        if (!Tiene(a))
            return;

        if (a.Categoria == CategoriaTaquilla.Personaje)
        {
            try
            {
                PlayerPrefs.SetInt(ClavePersonaje, a.Indice);
                // Al cambiar de personaje te pones sus complementos.
                PlayerPrefs.DeleteKey(ClaveComplementos);
                PlayerPrefs.Save();
            }
            catch
            {
            }

            Cambio?.Invoke();
            return;
        }

        if (a.Categoria == CategoriaTaquilla.Complemento)
        {
            int mascara = ComplementosMios;
            HuecoComplemento hueco = Complementos[a.Indice].Hueco;

            for (int i = 0; i < Complementos.Length; i++)
                if (Complementos[i].Hueco == hueco)
                    mascara &= ~(1 << i);

            GuardarComplementos(mascara | (1 << a.Indice));
            return;
        }

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

    // Lo que ven los demas, en un solo numero:
    //   bits 0-2 personaje+1 (0 = sin elegir), 3-6 traje, 7-10 color del nombre, 11-22 complementos, 23 "complementos elegidos".
    public static int AspectoPropio =>
        (PersonajeMio + 1) |
        (Mathf.Clamp(Equipado(CategoriaTaquilla.Traje), 0, 15) << 3) |
        (Mathf.Clamp(Equipado(CategoriaTaquilla.ColorNombre), 0, 15) << 7) |
        ((ComplementosMios & 0xFFF) << 11) | (1 << 23);

    public static int PersonajeDe(int aspecto) => (aspecto & 7) - 1;
    public static int TrajeDe(int aspecto) => Mathf.Clamp((aspecto >> 3) & 15, 0, Trajes.Length - 1);
    public static int ColorNombreDe(int aspecto) => Mathf.Clamp((aspecto >> 7) & 15, 0, ColoresNombre.Length - 1);

    // Complementos que lleva quien tiene este aspecto (un bot, o alguien que no ha elegido, lleva los de su personaje).
    public static int ComplementosDe(int aspecto, int personaje) =>
        (aspecto & (1 << 23)) != 0 ? (aspecto >> 11) & 0xFFF : ComplementosPorDefecto[Mathf.Clamp(personaje, 0, 3)];

    public static string RutaReverso => Reversos[Mathf.Clamp(Equipado(CategoriaTaquilla.Reverso), 0, Reversos.Length - 1)];

    // Nombre coloreado si ha comprado un color; si no, el color que se pase (normalmente el del personaje).
    public static string NombreConColor(string nombre, int aspecto, string colorPorDefecto)
    {
        string hex = ColoresNombre[ColorNombreDe(aspecto)];

        if (hex.Length == 0)
            hex = colorPorDefecto;

        return string.IsNullOrEmpty(hex) ? "<b>" + nombre + "</b>" : "<b><color=" + hex + ">" + nombre + "</color></b>";
    }
}
