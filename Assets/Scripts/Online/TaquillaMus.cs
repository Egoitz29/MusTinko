using System.Collections.Generic;
using UnityEngine;

// Catalogo de la taquilla, tu personaje (colores que eliges) y lo que has comprado y llevas puesto.
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
        // Nuevos
        new() { Gorro = C(0.97f, 0.97f, 0.97f), BordeGorro = C(0.8f, 0.1f, 0.12f), Pompom = C(0.8f, 0.1f, 0.12f), Manoplas = C(0.97f, 0.97f, 0.97f), Piernas = C(0.97f, 0.97f, 0.97f), Zapatos = C(0.95f, 0.95f, 0.95f) },          // Pelotari
        new() { Gorro = C(0.1f, 0.45f, 0.25f), BordeGorro = C(0.97f, 0.97f, 0.97f), Pompom = C(0.97f, 0.97f, 0.97f), Piernas = C(0.1f, 0.3f, 0.18f), Botones = C(0.97f, 0.97f, 0.97f) },                                       // Remero
        new() { Gorro = C(0.95f, 0.45f, 0.1f), BordeGorro = C(0.2f, 0.35f, 0.2f), Pompom = C(0.2f, 0.35f, 0.2f), Manoplas = C(0.95f, 0.45f, 0.1f), Piernas = C(0.3f, 0.32f, 0.22f), Zapatos = C(0.35f, 0.22f, 0.12f) },  // Montanero
        new() { Gorro = C(0.98f, 0.82f, 0.1f), BordeGorro = C(0.9f, 0.7f, 0.05f), Pompom = C(0.98f, 0.82f, 0.1f), Manoplas = C(0.98f, 0.82f, 0.1f), Piernas = C(0.15f, 0.2f, 0.35f), Zapatos = C(0.1f, 0.1f, 0.1f) },     // Arrantzale
        new() { Gorro = C(0.55f, 0.75f, 0.95f), BordeGorro = C(0.97f, 0.97f, 0.97f), Pompom = C(0.97f, 0.97f, 0.97f), Manoplas = C(0.97f, 0.97f, 0.97f), Piernas = C(0.2f, 0.3f, 0.5f) },                                   // Invierno
        new() { Gorro = C(0.95f, 0.76f, 0.2f), BordeGorro = C(0.7f, 0.1f, 0.12f), Pompom = C(0.7f, 0.1f, 0.12f), Botones = C(0.95f, 0.76f, 0.2f), Manoplas = C(0.95f, 0.9f, 0.8f), Piernas = C(0.55f, 0.08f, 0.1f), Zapatos = C(0.95f, 0.76f, 0.2f) }, // Rey de oros
    };

    // ---------- Mi personaje: colores que eliges tu (gratis, se cambian cuando quieras) ----------

    // Color de la ropa: es tu color en la mesa (el de tu nombre, tu silla en el minimapa...).
    // Los cuatro primeros son los de los personajes de antes (los bots los siguen llevando).
    public static readonly Color[] ColoresRopa =
    {
        C(0.72f, 0.55f, 0.94f), C(0.85f, 0.68f, 0.12f), C(0.45f, 0.88f, 0.65f), C(1.0f, 0.5f, 0.42f),
        C(0.85f, 0.18f, 0.18f), C(0.2f, 0.45f, 0.85f), C(0.15f, 0.6f, 0.3f), C(0.95f, 0.55f, 0.15f),
        C(0.95f, 0.5f, 0.75f), C(0.35f, 0.8f, 0.9f), C(0.55f, 0.3f, 0.65f), C(0.55f, 0.35f, 0.2f),
        C(0.25f, 0.25f, 0.3f), C(0.92f, 0.9f, 0.85f), C(0.6f, 0.75f, 0.25f), C(0.1f, 0.25f, 0.45f)
    };

    public static readonly string[] NombresRopa =
    {
        "Lila", "Mostaza", "Menta", "Coral", "Rojo", "Azul", "Verde", "Naranja",
        "Rosa", "Cielo", "Morado", "Marron", "Gris", "Blanco", "Pistacho", "Marino"
    };

    public static readonly Color[] TonosPiel =
    {
        C(1f, 0.85f, 0.72f), C(1f, 0.9f, 0.82f), C(0.93f, 0.75f, 0.6f), C(0.8f, 0.6f, 0.45f), C(0.62f, 0.43f, 0.3f), C(0.45f, 0.3f, 0.2f)
    };

    public static readonly Color[] ColoresPelo =
    {
        C(0.36f, 0.22f, 0.13f), C(0.80f, 0.58f, 0.25f), C(0.14f, 0.12f, 0.12f), C(0.58f, 0.24f, 0.12f),
        C(0.95f, 0.85f, 0.55f), C(0.85f, 0.85f, 0.85f), C(0.85f, 0.35f, 0.15f), C(0.3f, 0.45f, 0.85f)
    };

    public static readonly string[] Reversos = { "Baraja/Reverso_Clasico", "Baraja/Reverso_Tinko", "Baraja/Reverso_Verde", "Baraja/Reverso_Negro" };

    public static readonly string[] ColoresNombre = { "", "#FFD24D", "#FF6B5E", "#6EC6FF", "#7CD67C", "#FF8AD8", "#C9A2FF", "#FFA94D", "#5FE3D0" };

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

    // Lo que llevan los bots en cada silla: asi se distinguen de un vistazo.
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
        A("traje_pelotari", CategoriaTaquilla.Traje, "Pelotari", "Blanco impecable con el rojo del fronton.", 350, 7),
        A("traje_remero", CategoriaTaquilla.Traje, "Remero", "Verde y blanco, de trainera.", 350, 8),
        A("traje_montanero", CategoriaTaquilla.Traje, "Montanero", "Naranja para que te vean en el monte.", 450, 9),
        A("traje_arrantzale", CategoriaTaquilla.Traje, "Arrantzale", "Amarillo de chubasquero, recien llegado del puerto.", 500, 10),
        A("traje_invierno", CategoriaTaquilla.Traje, "Invierno", "Azul hielo y blanco nieve.", 550, 11),
        A("traje_rey", CategoriaTaquilla.Traje, "Rey de oros", "Oro y granate. El que manda en la mesa.", 2000, 12),

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
        A("nombre_lavanda", CategoriaTaquilla.ColorNombre, "Lavanda", "Tu nombre en lavanda.", 300, 6),
        A("nombre_naranja", CategoriaTaquilla.ColorNombre, "Naranja", "Tu nombre en naranja.", 300, 7),
        A("nombre_turquesa", CategoriaTaquilla.ColorNombre, "Turquesa", "Tu nombre en turquesa.", 300, 8),
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

        Regalar(a);
        Equipar(a);
        return true;
    }

    // Lo deja como tuyo sin pagar (lo que eliges al crear el personaje).
    private static void Regalar(ArticuloTaquilla a)
    {
        if (!Comprados.Add(a.Id))
            return;

        try
        {
            PlayerPrefs.SetString(ClaveComprados, string.Join(",", Comprados));
            PlayerPrefs.Save();
        }
        catch
        {
        }
    }

    public static ArticuloTaquilla Buscar(CategoriaTaquilla cat, int indice)
    {
        foreach (ArticuloTaquilla a in Catalogo)
            if (a.Categoria == cat && a.Indice == indice)
                return a;

        return null;
    }

    public static int Equipado(CategoriaTaquilla cat)
    {
        try { return PlayerPrefs.GetInt(ClaveEquipado + cat, 0); }
        catch { return 0; }
    }

    public static bool EstaEquipado(ArticuloTaquilla a) => a.Categoria == CategoriaTaquilla.Complemento
        ? (ComplementosMios & (1 << a.Indice)) != 0
        : Equipado(a.Categoria) == a.Indice;

    // ---------- Mi personaje: colores, complementos y si ya lo he creado ----------

    private const string ClaveComplementos = "MusTinko_Complementos";
    private const string ClaveRopa = "MusTinko_ColorRopa";
    private const string ClavePiel = "MusTinko_Piel";
    private const string ClavePelo = "MusTinko_ColorPelo";
    private const string ClaveCreado = "MusTinko_PersonajeCreado";

    public static int RopaMia { get => Leer(ClaveRopa, 0, ColoresRopa.Length); set => Escribir(ClaveRopa, value, ColoresRopa.Length); }
    public static int PielMia { get => Leer(ClavePiel, 0, TonosPiel.Length); set => Escribir(ClavePiel, value, TonosPiel.Length); }
    public static int PeloMio { get => Leer(ClavePelo, 0, ColoresPelo.Length); set => Escribir(ClavePelo, value, ColoresPelo.Length); }

    public static Color ColorRopaMio => ColoresRopa[RopaMia];

    // La primera vez que se entra al juego se crea el personaje (pantalla de bienvenida).
    public static bool PersonajeCreado
    {
        get
        {
            try { return PlayerPrefs.GetInt(ClaveCreado, 0) == 1; }
            catch { return true; }
        }
    }

    // Al terminar de crear el personaje, el traje y los complementos que llevas pasan a ser tuyos;
    // lo demas ya hay que comprarlo en la taquilla.
    public static void TerminarCreacion()
    {
        ArticuloTaquilla traje = Buscar(CategoriaTaquilla.Traje, Equipado(CategoriaTaquilla.Traje));
        if (traje != null) Regalar(traje);

        int mascara = ComplementosMios;

        for (int i = 0; i < Complementos.Length; i++)
            if ((mascara & (1 << i)) != 0 && Buscar(CategoriaTaquilla.Complemento, i) is ArticuloTaquilla c)
                Regalar(c);

        try
        {
            PlayerPrefs.SetInt(ClaveCreado, 1);
            PlayerPrefs.Save();
        }
        catch
        {
        }

        Cambio?.Invoke();
    }

    private static int Leer(string clave, int porDefecto, int cuantos)
    {
        try { return Mathf.Clamp(PlayerPrefs.GetInt(clave, porDefecto), 0, cuantos - 1); }
        catch { return porDefecto; }
    }

    private static void Escribir(string clave, int valor, int cuantos)
    {
        try
        {
            PlayerPrefs.SetInt(clave, Mathf.Clamp(valor, 0, cuantos - 1));
            PlayerPrefs.Save();
        }
        catch
        {
        }

        Cambio?.Invoke();
    }

    // Sin tocar, no llevas ningun complemento.
    public static int ComplementosMios
    {
        get
        {
            try { return PlayerPrefs.GetInt(ClaveComplementos, -1) is int m && m >= 0 ? m : 0; }
            catch { return 0; }
        }
    }

    public static void QuitarComplemento(ArticuloTaquilla a)
    {
        GuardarComplementos(ComplementosMios & ~(1 << a.Indice));
    }

    // Pone o quita un complemento sin mirar si es tuyo (el creador de personaje lo deja todo).
    public static void CambiarComplementoLibre(int indice, bool poner)
    {
        int mascara = ComplementosMios;

        if (poner)
        {
            HuecoComplemento hueco = Complementos[indice].Hueco;

            for (int i = 0; i < Complementos.Length; i++)
                if (Complementos[i].Hueco == hueco)
                    mascara &= ~(1 << i);

            mascara |= 1 << indice;
        }
        else
        {
            mascara &= ~(1 << indice);
        }

        GuardarComplementos(mascara);
    }

    public static void CambiarTrajeLibre(int indice)
    {
        try
        {
            PlayerPrefs.SetInt(ClaveEquipado + CategoriaTaquilla.Traje, Mathf.Clamp(indice, 0, Trajes.Length - 1));
            PlayerPrefs.Save();
        }
        catch
        {
        }

        Cambio?.Invoke();
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

    // Lo que ven los demas, en un solo numero (positivo):
    //   bits 0-3 traje, 4-7 color del nombre, 8-19 complementos, 20-23 color de ropa, 24-26 piel, 27-29 pelo,
    //   30 "es un personaje propio" (si no esta, es un bot o alguien de una version vieja: aspecto por silla).
    public static int AspectoPropio =>
        Mathf.Clamp(Equipado(CategoriaTaquilla.Traje), 0, 15) |
        (Mathf.Clamp(Equipado(CategoriaTaquilla.ColorNombre), 0, 15) << 4) |
        ((ComplementosMios & 0xFFF) << 8) |
        ((RopaMia & 15) << 20) | ((PielMia & 7) << 24) | ((PeloMio & 7) << 27) | (1 << 30);

    public static bool EsPropio(int aspecto) => (aspecto & (1 << 30)) != 0;
    public static int TrajeDe(int aspecto) => EsPropio(aspecto) ? Mathf.Clamp(aspecto & 15, 0, Trajes.Length - 1) : 0;
    public static int ColorNombreDe(int aspecto) => EsPropio(aspecto) ? Mathf.Clamp((aspecto >> 4) & 15, 0, ColoresNombre.Length - 1) : 0;

    // Un bot lleva lo de su silla (los colores y complementos de los personajes de antes).
    public static int ComplementosDe(int aspecto, int silla) =>
        EsPropio(aspecto) ? (aspecto >> 8) & 0xFFF : ComplementosPorDefecto[Mathf.Clamp(silla, 0, 3)];
    public static int RopaDe(int aspecto, int silla) =>
        EsPropio(aspecto) ? Mathf.Clamp((aspecto >> 20) & 15, 0, ColoresRopa.Length - 1) : Mathf.Clamp(silla, 0, 3);
    public static int PielDe(int aspecto) => EsPropio(aspecto) ? Mathf.Clamp((aspecto >> 24) & 7, 0, TonosPiel.Length - 1) : 0;
    public static int PeloDe(int aspecto, int silla) =>
        EsPropio(aspecto) ? Mathf.Clamp((aspecto >> 27) & 7, 0, ColoresPelo.Length - 1) : Mathf.Clamp(silla, 0, 3);

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
