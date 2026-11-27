using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// MENU PRINCIPAL (como la maqueta): barra a la izquierda y una pantalla por apartado.
//   INICIO        saludo con nivel, los 4 modos de juego, amigos conectados y reto de la semana
//   SALAS         salas por Internet (filtros, entrar, crear sala con opciones, entrar con codigo)
//   PERFIL        tu jugador: codigo de amigo, titulo, nivel, estadisticas, logros y ultimas partidas
//   AMIGOS        lista con su estado, solicitudes, anadir por codigo, jugasteis hace poco
//   CLASIFICACION esta semana / global / amigos
//   TAQUILLA      (la de siempre)
//   MISMA WIFI    (desde Inicio) crear partida en la WiFi, IP, buscar y modo demo
// Todo se coloca en un marco de 1880x1000 que se escala para caber en cualquier pantalla.
public partial class VistaMesaOnlineMus
{
    private enum PantallaMenu { Inicio, Salas, Perfil, Amigos, Clasificacion, Wifi }

    private const float AnchoMarco = 1880f, AltoMarco = 1000f;
    private RectTransform marcoMenu, contenidoMenu;
    private PantallaMenu pantallaMenu = PantallaMenu.Inicio;
    private readonly Dictionary<PantallaMenu, GameObject> pantallasMenu = new();
    private readonly Dictionary<PantallaMenu, Button> botonesNav = new();
    private float siguienteRefrescoMenu;
    private GameObject insigniaAmigos;
    private TextMeshProUGUI txtInsigniaAmigos;

    private static readonly Color CremaApagada = new(0.85f, 0.77f, 0.63f);
    private static readonly Color Verde = new(0.18f, 0.52f, 0.25f);
    private static readonly Color RojoOscuro = new(0.7f, 0.23f, 0.19f);

    // ------------------------------------------------------------------ piezas

    private RectTransform Caja(string nombre, Transform padre, float x, float y, float w, float h, string sprite = null, Color? color = null)
    {
        GameObject go = sprite == null ? new GameObject(nombre, typeof(RectTransform)) : new GameObject(nombre, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);

        if (sprite != null)
        {
            Image img = go.GetComponent<Image>();
            img.sprite = SpriteUI(sprite, sprite.StartsWith("panel") ? 30 : 12);
            img.type = Image.Type.Sliced;
            img.color = color ?? Color.white;
            img.raycastTarget = false;
        }

        return rt;
    }

    private TextMeshProUGUI Letra(Transform padre, float x, float y, float w, float h, string texto, float tam, Color color,
        bool titulo = false, TextAlignmentOptions alineacion = TextAlignmentOptions.TopLeft, bool contorno = true)
    {
        RectTransform rt = Caja("Texto", padre, x, y, w, h);
        TextMeshProUGUI t = Texto(rt, texto, tam, color, titulo, alineacion, contorno);
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        return t;
    }

    private Button BotonEn(Transform padre, float x, float y, float w, float h, string color, string texto, UnityAction accion, float tam = 28)
    {
        Button b = Boton(padre, color, texto, new Vector2(w, h), accion, tam);
        RectTransform rt = b.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return b;
    }

    // Circulo con borde oscuro y una letra (la cara del jugador en las listas).
    private sealed class Cara
    {
        public RectTransform Raiz;
        public Image Fondo;
        public TextMeshProUGUI Letra;
    }

    private Cara Circulo(Transform padre, float x, float y, float tam)
    {
        Cara c = new();
        c.Raiz = Caja("Cara", padre, x, y, tam, tam);
        Image borde = Imagen("Borde", c.Raiz, "circulo", tam, new Color(0.07f, 0.04f, 0.02f, 1f));
        borde.rectTransform.anchorMin = borde.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        c.Fondo = Imagen("Fondo", c.Raiz, "circulo", tam - Mathf.Max(6f, tam * 0.08f), Color.white);
        c.Fondo.rectTransform.anchorMin = c.Fondo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        c.Letra = Texto(c.Raiz, "", tam * 0.55f, Tinta, true, TextAlignmentOptions.Center, false);
        c.Letra.rectTransform.offsetMin = c.Letra.rectTransform.offsetMax = Vector2.zero;
        return c;
    }

    private static Color ColorDeNombre(string nombre)
    {
        if (string.IsNullOrEmpty(nombre))
            return new Color(0.55f, 0.48f, 0.4f);

        int h = 0;
        foreach (char ch in nombre) h = h * 31 + ch;
        return ColoresJugadoresMus.Colores[Mathf.Abs(h) % ColoresJugadoresMus.Colores.Length];
    }

    private static string Inicial(string nombre) => string.IsNullOrEmpty(nombre) ? "?" : nombre.Substring(0, 1).ToUpperInvariant();

    // Barra de progreso (fondo oscuro y relleno).
    private Image Barra(Transform padre, float x, float y, float w, float h, string relleno = "barra_nosotros")
    {
        RectTransform fondo = Caja("Barra", padre, x, y, w, h, "barra_fondo");
        GameObject lleno = new("Relleno", typeof(RectTransform), typeof(Image));
        RectTransform rt = lleno.GetComponent<RectTransform>();
        rt.SetParent(fondo, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 0f);
        Image img = lleno.GetComponent<Image>();
        img.sprite = SpriteUI(relleno, 8);
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        return img;
    }

    private static void Llenar(Image relleno, float fraccion)
    {
        RectTransform fondo = (RectTransform)relleno.transform.parent;
        relleno.rectTransform.sizeDelta = new Vector2(fondo.sizeDelta.x * Mathf.Clamp01(fraccion), 0f);
    }

    private Button[] Chips(Transform padre, float x, float y, float ancho, float alto, float hueco, string[] textos, Action<int> alElegir, float tam = 22)
    {
        Button[] b = new Button[textos.Length];

        for (int i = 0; i < textos.Length; i++)
        {
            int indice = i;
            b[i] = BotonEn(padre, x + i * (ancho + hueco), y, ancho, alto, "gris", textos[i], () => alElegir(indice), tam);
        }

        return b;
    }

    private void PintarChips(Button[] chips, int elegido)
    {
        for (int i = 0; i < chips.Length; i++)
            PintarPestana(chips[i], i == elegido);
    }

    // ------------------------------------------------------------------ crear

    private void CrearMenuNuevo()
    {
        marcoMenu = new GameObject("MarcoMenu", typeof(RectTransform)).GetComponent<RectTransform>();
        marcoMenu.SetParent(panelMenuUI.transform, false);
        marcoMenu.anchorMin = marcoMenu.anchorMax = marcoMenu.pivot = new Vector2(0.5f, 0.5f);
        marcoMenu.sizeDelta = new Vector2(AnchoMarco, AltoMarco);

        CrearNavegacion();
        contenidoMenu = Caja("Contenido", marcoMenu, 320, 0, 1560, 1000);

        pantallasMenu[PantallaMenu.Inicio] = CrearPantallaInicio();
        pantallasMenu[PantallaMenu.Salas] = CrearPantallaSalas();
        pantallasMenu[PantallaMenu.Perfil] = CrearPantallaPerfil();
        pantallasMenu[PantallaMenu.Amigos] = CrearPantallaAmigos();
        pantallasMenu[PantallaMenu.Clasificacion] = CrearPantallaClasificacion();
        pantallasMenu[PantallaMenu.Wifi] = CrearPantallaWifi();
        CrearAvisoMenu();
        IrA(PantallaMenu.Inicio);
    }

    private void IrA(PantallaMenu p)
    {
        pantallaMenu = p;
        siguienteRefrescoMenu = 0f;

        foreach (KeyValuePair<PantallaMenu, GameObject> par in pantallasMenu)
            par.Value.SetActive(par.Key == p);

        foreach (KeyValuePair<PantallaMenu, Button> par in botonesNav)
            PintarPestana(par.Value, par.Key == p || (p == PantallaMenu.Wifi && par.Key == PantallaMenu.Inicio));

#if !MUS_TINKO_SIN_INTERNET
        if (p == PantallaMenu.Salas)
            siguienteBusquedaSalas = 0f;

        // Los servicios de Unity no se arrancan desde Awake (el menu se crea ahi): se espera al primer fotograma.
        if (!menuEnMarcha)
            return;

        if (p == PantallaMenu.Inicio || p == PantallaMenu.Amigos)
            _ = SocialMus.Instancia.PrepararAmigos();

        if (p == PantallaMenu.Clasificacion)
            SocialMus.Instancia.CargarClasificacion(TipoClasificacion);
#endif
    }

    private void CrearNavegacion()
    {
        RectTransform nav = Caja("Navegacion", marcoMenu, 0, 0, 300, 1000, "panel_madera_oscura");
        // Logo en dos lineas: con la letra de cartel "Mus TINKO" no cabe en una y pisaba el boton INICIO.
        TextMeshProUGUI logo = Letra(nav, 14, 10, 272, 120, "<size=70%>Mus</size>\nTINKO", 64, Oro, true, TextAlignmentOptions.Center);
        logo.lineSpacing = -28f;

        (PantallaMenu, string)[] items =
        {
            (PantallaMenu.Inicio, "INICIO"), (PantallaMenu.Salas, "SALAS"), (PantallaMenu.Perfil, "PERFIL"),
            (PantallaMenu.Amigos, "AMIGOS"), (PantallaMenu.Clasificacion, "CLASIFICACION")
        };

        for (int i = 0; i < items.Length; i++)
        {
            PantallaMenu destino = items[i].Item1;
            botonesNav[destino] = BotonEn(nav, 22, 140 + i * 88, 256, 74, "gris", items[i].Item2, () => IrA(destino), 26);
        }

        BotonEn(nav, 22, 140 + items.Length * 88, 256, 74, "morado", "TAQUILLA", AbrirTaquilla, 26);

        // Aviso de solicitudes de amistad sobre el boton AMIGOS.
        RectTransform insignia = Caja("Insignia", botonesNav[PantallaMenu.Amigos].transform, 214, -8, 46, 46);
        Imagen("Fondo", insignia, "circulo", 46, new Color(0.75f, 0.2f, 0.16f)).rectTransform.anchorMin = Vector2.one * 0.5f;
        txtInsigniaAmigos = Texto(insignia, "", 24, Color.white, true, TextAlignmentOptions.Center, false);
        txtInsigniaAmigos.rectTransform.offsetMin = txtInsigniaAmigos.rectTransform.offsetMax = Vector2.zero;
        insigniaAmigos = insignia.gameObject;
        insigniaAmigos.SetActive(false);

        // Monedas y regalo diario.
        RectTransform caja = Caja("Monedas", nav, 18, 742, 264, 236, "panel_madera");
        Image moneda = Imagen("Moneda", caja, "moneda", 70, Color.white);
        Rect(moneda.gameObject, caja, new Vector2(0f, 1f), new Vector2(18, -18), new Vector2(70, 70));
        txtSaldoMenu = Letra(caja, 98, 18, 154, 70, "", 54, Oro, true, TextAlignmentOptions.MidlineLeft);
        txtEstadisticasMenu = Letra(caja, 12, 96, 240, 36, "", 20, Crema, false, TextAlignmentOptions.Center);
        txtEstadisticasMenu.enableWordWrapping = false;
        botonRegaloMenu = BotonEn(caja, 16, 140, 232, 76, "dorado", "REGALO +" + MonederoMus.RegaloDiario, () =>
        {
            if (MonederoMus.ReclamarRegalo())
                MostrarAvisoMonedas("+" + MonederoMus.RegaloDiario + " monedas de regalo. ¡Vuelve manana!");
        }, 24);
    }

    // ------------------------------------------------------------------ aviso / invitaciones

    private GameObject avisoMenu;
    private TextMeshProUGUI txtAvisoMenu;
    private Button botonAvisoSi, botonAvisoNo;

    private void CrearAvisoMenu()
    {
        RectTransform caja = Caja("AvisoMenu", contenidoMenu, 300, 0, 960, 112, "panel_pergamino");
        txtAvisoMenu = Letra(caja, 28, 14, 560, 84, "", 28, Tinta, false, TextAlignmentOptions.MidlineLeft, false);
        botonAvisoSi = BotonEn(caja, 610, 20, 190, 72, "verde", "UNIRSE", AceptarInvitacion, 26);
        botonAvisoNo = BotonEn(caja, 814, 20, 120, 72, "rojo", "NO", RechazarInvitacion, 26);
        avisoMenu = caja.gameObject;
        avisoMenu.SetActive(false);
    }

    private void AceptarInvitacion()
    {
#if !MUS_TINKO_SIN_INTERNET
        SocialMus.Invitacion inv = SocialMus.Instancia.InvitacionPendiente;
        SocialMus.Instancia.InvitacionPendiente = null;

        if (inv != null)
        {
            GuardarNombre(campoNombreUI.text);
            SalaInternet()?.Unirse(inv.SalaId, NombreLocal);
        }
#endif
    }

    private void RechazarInvitacion()
    {
#if !MUS_TINKO_SIN_INTERNET
        SocialMus.Instancia.InvitacionPendiente = null;
#endif
    }

    private void ActualizarAvisoMenu()
    {
#if !MUS_TINKO_SIN_INTERNET
        SocialMus s = SocialMus.Instancia;

        if (s.InvitacionPendiente != null)
        {
            avisoMenu.SetActive(true);
            avisoMenu.transform.SetAsLastSibling();
            txtAvisoMenu.text = "<b>" + s.InvitacionPendiente.De + "</b> te invita a su mesa\n<size=80%>" + s.InvitacionPendiente.SalaNombre + "</size>";
            botonAvisoSi.gameObject.SetActive(true);
            botonAvisoNo.gameObject.SetActive(true);
            return;
        }

        if (s.AvisoAmigos.Length > 0)
        {
            avisoMenu.SetActive(true);
            avisoMenu.transform.SetAsLastSibling();
            txtAvisoMenu.text = s.AvisoAmigos;
            botonAvisoSi.gameObject.SetActive(false);
            botonAvisoNo.gameObject.SetActive(false);
            return;
        }
#endif
        avisoMenu.SetActive(false);
    }

    // ------------------------------------------------------------------ INICIO

    private Cara caraInicio;
    private TextMeshProUGUI txtSaludo, txtNivelInicio, txtXpInicio, txtSalasAbiertas, txtReto, txtNombreFalta, txtAmigosConectados, txtInfoAmigosInicio;
    private Image barraXpInicio, barraReto;
    private Button botonReto;
    private FilaAmigo[] filasAmigosInicio;

    private sealed class FilaAmigo
    {
        public GameObject Raiz;
        public Cara Cara;
        public TextMeshProUGUI Nombre, Estado, TxtAccion, TxtAccion2;
        public Button Accion, Accion2;
        public Action AlPulsar, AlPulsar2;
    }

    private GameObject CrearPantallaInicio()
    {
        RectTransform p = Caja("Inicio", contenidoMenu, 0, 0, 1560, 1000);

        caraInicio = Circulo(p, 0, 0, 112);
        txtSaludo = Letra(p, 134, 0, 980, 74, "", 66, Crema, true, TextAlignmentOptions.MidlineLeft);
        RectTransform chip = Caja("Nivel", p, 134, 78, 150, 42, "placa", Oro);
        txtNivelInicio = Letra(chip, 0, 0, 150, 42, "", 24, Tinta, false, TextAlignmentOptions.Center, false);
        txtNivelInicio.fontStyle = FontStyles.Bold;
        barraXpInicio = Barra(p, 300, 89, 300, 20);
        txtXpInicio = Letra(p, 616, 80, 420, 38, "", 22, CremaApagada, false, TextAlignmentOptions.MidlineLeft);

        Letra(p, 0, 132, 400, 60, "Jugar", 56, Oro, true);

        CrearTarjetaModo(p, 0, 196, "Contra bots", "Practica sin esperar a nadie. Los bots han aprendido con millones de partidas.",
            "dorado", "JUGAR YA", () =>
            {
                GuardarNombre(campoNombreUI.text);
                conexion?.CrearPartida();
            }, out _);
        CrearTarjetaModo(p, 565, 196, "Salas por Internet", "Entra en una mesa abierta o crea la tuya. Minimo 2 personas; el resto, bots.",
            "rojo", "VER SALAS", () => IrA(PantallaMenu.Salas), out txtSalasAbiertas);
        CrearTarjetaModo(p, 0, 466, "Con amigos", "Invita a tus amigos a tu mesa o unete a la suya.",
            "morado", "AMIGOS", () => IrA(PantallaMenu.Amigos), out _);
        CrearTarjetaModo(p, 565, 466, "Misma WiFi", "Para jugar con quien tienes al lado, aunque no haya Internet.",
            "azul", "ABRIR", () => IrA(PantallaMenu.Wifi), out _);

        RectTransform reto = Caja("Reto", p, 0, 736, 1110, 112, "panel_pergamino");
        Letra(reto, 28, 14, 760, 40, "Reto de la semana: gana " + PerfilMus.RetoVictorias + " partidas", 28, Tinta, false, TextAlignmentOptions.MidlineLeft, false)
            .fontStyle = FontStyles.Bold;
        barraReto = Barra(reto, 28, 64, 700, 22, "barra_ellos");
        txtReto = Letra(reto, 744, 52, 340, 44, "", 24, Tinta, false, TextAlignmentOptions.MidlineRight, false);
        botonReto = BotonEn(reto, 864, 20, 220, 72, "verde", "RECLAMAR", () =>
        {
            if (PerfilMus.ReclamarReto())
                MostrarAvisoMonedas("¡Reto cumplido! +" + PerfilMus.PremioReto + " monedas");
        }, 26);

        txtNombreFalta = Letra(p, 0, 870, 1110, 44, "<color=#FFD24D>Ponte un nombre en tu PERFIL para que te reconozcan.</color>", 24, Crema);

        RectTransform col = Caja("Amigos", p, 1130, 132, 430, 716, "panel_madera_oscura");
        Letra(col, 26, 18, 380, 54, "Amigos", 46, Oro, true);
        txtAmigosConectados = Letra(col, 200, 28, 206, 40, "", 22, CremaApagada, false, TextAlignmentOptions.MidlineRight);
        filasAmigosInicio = new FilaAmigo[5];

        for (int i = 0; i < filasAmigosInicio.Length; i++)
            filasAmigosInicio[i] = CrearFilaAmigo(col, 22, 86 + i * 104, 386, 66, 22, 120);

        txtInfoAmigosInicio = Letra(col, 26, 100, 380, 400, "", 24, CremaApagada, false, TextAlignmentOptions.Top);
        BotonEn(col, 26, 716 - 92, 378, 70, "gris", "VER TODOS", () => IrA(PantallaMenu.Amigos), 24);
        return p.gameObject;
    }

    private void CrearTarjetaModo(Transform padre, float x, float y, string titulo, string texto, string color, string boton, UnityAction accion,
        out TextMeshProUGUI extra)
    {
        // Cada modo es un naipe de papel, un poco girado, como una mano de cuatro cartas.
        RectTransform c = Caja("Modo_" + titulo, padre, x, y, 545, 250, "panel_pergamino");
        float giro = (x < 100f ? -1.2f : 1.2f) * (y < 300f ? 1f : -1f);
        c.localRotation = Quaternion.Euler(0f, 0f, giro);
        Color tintaSuave = new(0.35f, 0.27f, 0.2f);
        Letra(c, 28, 20, 489, 60, titulo, 44, Tinta, true, TextAlignmentOptions.TopLeft, false);
        Letra(c, 28, 80, 489, 72, texto, 24, tintaSuave, false, TextAlignmentOptions.TopLeft, false);
        BotonEn(c, 28, 158, 290, 70, color, boton, accion, 28);
        extra = Letra(c, 330, 168, 190, 50, "", 22, Tinta, false, TextAlignmentOptions.MidlineLeft, false);

        // Esquina del naipe: una pinta del color del boton (oros, copas, espadas o bastos).
        Image pinta = Imagen("Pinta", c, "circulo", 34, ColorPinta(color));
        Rect(pinta.gameObject, c, new Vector2(1f, 1f), new Vector2(-24, -24), new Vector2(34, 34));
    }

    private static Color ColorPinta(string color) => color switch
    {
        "dorado" => new Color(0.89f, 0.70f, 0.24f),
        "rojo" => new Color(0.72f, 0.2f, 0.16f),
        "azul" => new Color(0.17f, 0.44f, 0.65f),
        "morado" => new Color(0.18f, 0.42f, 0.24f),
        _ => new Color(0.17f, 0.11f, 0.08f)
    };

    private FilaAmigo CrearFilaAmigo(Transform padre, float x, float y, float ancho, float tamCara, float tamTexto, float anchoBoton)
    {
        FilaAmigo f = new();
        RectTransform raiz = Caja("Fila", padre, x, y, ancho, tamCara + 20);
        f.Raiz = raiz.gameObject;
        f.Cara = Circulo(raiz, 0, 0, tamCara);
        float xt = tamCara + 14;
        float ancho1 = ancho - xt - anchoBoton - 10;
        f.Nombre = Letra(raiz, xt, 0, ancho1, tamCara * 0.52f, "", tamTexto + 4, Crema, false, TextAlignmentOptions.MidlineLeft);
        f.Nombre.fontStyle = FontStyles.Bold;
        f.Nombre.enableWordWrapping = false;
        f.Estado = Letra(raiz, xt, tamCara * 0.52f, ancho1, tamCara * 0.48f, "", tamTexto - 2, CremaApagada, false, TextAlignmentOptions.MidlineLeft);
        f.Estado.enableWordWrapping = false;
        f.Accion = BotonEn(raiz, ancho - anchoBoton, (tamCara - 54) * 0.5f, anchoBoton, 54, "verde", "", () => f.AlPulsar?.Invoke(), 20);
        f.TxtAccion = f.Accion.GetComponentInChildren<TextMeshProUGUI>();
        return f;
    }

    private void PintarCara(Cara c, string nombre, bool apagada)
    {
        Color col = ColorDeNombre(nombre);
        c.Fondo.color = apagada ? new Color(0.5f, 0.45f, 0.38f) : col;
        c.Letra.text = Inicial(nombre);
    }

    private void ActualizarInicio()
    {
        string nombre = string.IsNullOrWhiteSpace(NombreLocal) ? "jugador" : NombreLocal;
        txtSaludo.text = "¡Kaixo, " + nombre + "!";
        caraInicio.Fondo.color = ColoresJugadoresMus.Colores[Mathf.Clamp(TaquillaMus.PersonajeMio, 0, 3)];
        caraInicio.Letra.text = Inicial(nombre);
        txtNivelInicio.text = "Nivel " + PerfilMus.Nivel;
        Llenar(barraXpInicio, (float)PerfilMus.XpEnNivel / PerfilMus.XpParaSubir);
        txtXpInicio.text = PerfilMus.XpEnNivel + " / " + PerfilMus.XpParaSubir + " de experiencia";
        txtNombreFalta.gameObject.SetActive(string.IsNullOrWhiteSpace(NombreLocal));

        int ganadas = PerfilMus.GanadasSemana;
        Llenar(barraReto, (float)ganadas / PerfilMus.RetoVictorias);
        bool reclamable = PerfilMus.RetoCumplido && !PerfilMus.RetoReclamado;
        botonReto.gameObject.SetActive(reclamable);
        txtReto.gameObject.SetActive(!reclamable);
        txtReto.text = PerfilMus.RetoReclamado ? "¡Conseguido!" : Mathf.Min(ganadas, PerfilMus.RetoVictorias) + " / " + PerfilMus.RetoVictorias +
                                                                  "  ·  premio " + PerfilMus.PremioReto;

#if !MUS_TINKO_SIN_INTERNET
        SalaInternetMus sala = SalaInternet();
        int abiertas = sala != null ? sala.Salas.Count : 0;
        txtSalasAbiertas.text = sala == null || sala.Busquedas == 0 ? "" : abiertas == 1 ? "1 sala abierta" : abiertas + " salas abiertas";

        // De paso, la lista de salas se refresca de vez en cuando para el contador.
        if (sala != null && !sala.Buscando && !sala.Ocupado && Time.unscaledTime >= siguienteBusquedaSalas)
        {
            siguienteBusquedaSalas = Time.unscaledTime + 15f;
            sala.BuscarSalas();
        }

        PintarListaAmigos(filasAmigosInicio, txtInfoAmigosInicio, true);
        int conectados = 0;
        foreach (SocialMus.Amigo a in SocialMus.Instancia.Amigos) if (a.Conectado) conectados++;
        txtAmigosConectados.text = SocialMus.Instancia.AmigosListos ? conectados + " conectados" : "";
#else
        txtSalasAbiertas.text = "";
        foreach (FilaAmigo f in filasAmigosInicio) f.Raiz.SetActive(false);
        txtInfoAmigosInicio.text = "Amigos no disponible en esta version.";
        txtAmigosConectados.text = "";
#endif
    }

#if !MUS_TINKO_SIN_INTERNET
    // Rellena filas de amigos (Inicio: solo conectados primero; Amigos: todos).
    private void PintarListaAmigos(FilaAmigo[] filas, TextMeshProUGUI info, bool resumen)
    {
        SocialMus s = SocialMus.Instancia;
        List<SocialMus.Amigo> lista = s.Amigos;
        int n = s.AmigosListos ? Mathf.Min(filas.Length, lista.Count) : 0;

        for (int i = 0; i < filas.Length; i++)
        {
            FilaAmigo f = filas[i];
            f.Raiz.SetActive(i < n);

            if (i >= n)
                continue;

            SocialMus.Amigo a = lista[i];
            PintarCara(f.Cara, a.Nombre, !a.Conectado);
            f.Nombre.text = a.Nombre;
            f.Estado.text = s.TextoEstado(a);
            ConfigurarAccionAmigo(f, a);
        }

        if (!s.AmigosListos)
            info.text = string.IsNullOrEmpty(s.ErrorAmigos) ? "Cargando amigos..." : "<size=85%>" + s.ErrorAmigos + "</size>";
        else if (lista.Count == 0)
            info.text = resumen ? "Aun no tienes amigos.\n<size=85%>Anadelos en AMIGOS con su codigo.</size>" : "";
        else
            info.text = "";
    }

    private void ConfigurarAccionAmigo(FilaAmigo f, SocialMus.Amigo a)
    {
        SalaInternetMus sala = SalaInternet();

        if (a.PuedeUnirse)
        {
            MostrarAccion(f.Accion, f.TxtAccion, "verde", "UNIRSE");
            f.AlPulsar = () =>
            {
                GuardarNombre(campoNombreUI.text);
                sala?.Unirse(a.Actividad.SalaId, NombreLocal);
            };
        }
        else if (a.Conectado)
        {
            MostrarAccion(f.Accion, f.TxtAccion, "morado", "INVITAR");
            f.AlPulsar = () =>
            {
                // Desde el menu: se crea una sala con codigo y se le invita en cuanto exista.
                GuardarNombre(campoNombreUI.text);
                SocialMus.Instancia.InvitarCuandoHayaSala(a.Id);
                sala?.CrearSala(NombreLocal, new SalaInternetMus.Opciones { Privada = true, Apuesta = 0, Tantos = 40, Segundos = 25f });
            };
        }
        else
        {
            f.Accion.gameObject.SetActive(false);
            f.AlPulsar = null;
        }
    }
#endif

    private void MostrarAccion(Button b, TextMeshProUGUI t, string color, string texto)
    {
        b.gameObject.SetActive(true);
        Image img = b.GetComponent<Image>();
        img.sprite = SpriteUI("boton_" + color, 22);
        b.spriteState = new SpriteState { pressedSprite = SpriteUI("boton_" + color + "_pulsado", 22) };
        AjustarTextoBoton(b, color);
        t.text = texto;
    }

    // ------------------------------------------------------------------ SALAS

    private TextMeshProUGUI txtInfoSalasMenu, txtMasSalas, txtActualizarSalasMenu;
    private Button botonActualizarSalasMenu, botonCrearSalaMenu;
    private Button[] chipsFiltroSalas, chipsTipoSala, chipsEntradaSala, chipsTantosSala, chipsTiempoSala;
    private int filtroSalas, tipoSala, entradaSala, tantosSala = 1, tiempoSala = 1;
    private TMP_InputField campoNombreSala, campoCodigoSala;
    private TarjetaSala[] tarjetasSalas;
    private float siguienteBusquedaSalas;

    private static readonly int[] EntradasSala = { 0, 50, 200, 1000 };
    private static readonly int[] TantosSala = { 30, 40 };
    private static readonly float[] TiemposSala = { 15f, 25f, 40f };

    private sealed class TarjetaSala
    {
        public GameObject Raiz;
        public TextMeshProUGUI Nombre, Info, TxtBoton;
        public Image[] Sillas;
        public GameObject Amigo;
        public Button Boton;
        public string Id;
    }

    private GameObject CrearPantallaSalas()
    {
        RectTransform p = Caja("Salas", contenidoMenu, 0, 0, 1560, 1000);
        Letra(p, 0, 0, 700, 74, "Salas por Internet", 62, Oro, true);
        botonActualizarSalasMenu = BotonEn(p, 860, 8, 250, 62, "gris", "ACTUALIZAR", () => siguienteBusquedaSalas = 0f, 24);
        txtActualizarSalasMenu = botonActualizarSalasMenu.GetComponentInChildren<TextMeshProUGUI>();
        chipsFiltroSalas = Chips(p, 0, 88, 200, 56, 14, new[] { "TODAS", "SIN APUESTA", "DE AMIGOS" }, i => { filtroSalas = i; siguienteRefrescoMenu = 0f; });

        tarjetasSalas = new TarjetaSala[6];

        for (int i = 0; i < tarjetasSalas.Length; i++)
            tarjetasSalas[i] = CrearTarjetaSala(p, (i % 3) * 377, 162 + (i / 3) * 322);

        txtInfoSalasMenu = Letra(p, 0, 200, 1110, 300, "", 30, Crema, false, TextAlignmentOptions.Top);
        txtMasSalas = Letra(p, 0, 812, 1110, 40, "", 22, CremaApagada, false, TextAlignmentOptions.Center);

        // Crear sala
        RectTransform f = Caja("CrearSala", p, 1130, 0, 430, 740, "panel_pergamino");
        Letra(f, 28, 18, 374, 58, "Crear sala", 52, Tinta, true, TextAlignmentOptions.TopLeft, false);
        Letra(f, 28, 84, 374, 30, "Nombre", 22, Tinta, false, TextAlignmentOptions.TopLeft, false).fontStyle = FontStyles.Bold;
        campoNombreSala = Campo(f, new Vector2(0, -116), new Vector2(374, 64), "Mesa de...", "");
        Letra(f, 28, 192, 374, 30, "Quien puede entrar", 22, Tinta, false, TextAlignmentOptions.TopLeft, false).fontStyle = FontStyles.Bold;
        chipsTipoSala = Chips(f, 28, 224, 182, 54, 10, new[] { "PUBLICA", "CON CODIGO" }, i => { tipoSala = i; siguienteRefrescoMenu = 0f; });
        Letra(f, 28, 292, 374, 30, "Entrada (monedas)", 22, Tinta, false, TextAlignmentOptions.TopLeft, false).fontStyle = FontStyles.Bold;
        chipsEntradaSala = Chips(f, 28, 324, 86, 54, 10, new[] { "0", "50", "200", "1000" }, i => { entradaSala = i; siguienteRefrescoMenu = 0f; });
        Letra(f, 28, 392, 374, 30, "Partida a", 22, Tinta, false, TextAlignmentOptions.TopLeft, false).fontStyle = FontStyles.Bold;
        chipsTantosSala = Chips(f, 28, 424, 182, 54, 10, new[] { "30 TANTOS", "40 TANTOS" }, i => { tantosSala = i; siguienteRefrescoMenu = 0f; });
        Letra(f, 28, 492, 374, 30, "Tiempo por turno", 22, Tinta, false, TextAlignmentOptions.TopLeft, false).fontStyle = FontStyles.Bold;
        chipsTiempoSala = Chips(f, 28, 524, 118, 54, 10, new[] { "15 S", "25 S", "40 S" }, i => { tiempoSala = i; siguienteRefrescoMenu = 0f; });
        Letra(f, 28, 590, 374, 50, "Minimo 2 personas para empezar. Las sillas libres las ocupan bots.", 19, Tinta, false, TextAlignmentOptions.TopLeft, false);
        botonCrearSalaMenu = BotonEn(f, 28, 650, 374, 70, "verde", "CREAR SALA", CrearSalaDesdeMenu, 30);

        // Entrar con codigo
        RectTransform c = Caja("Codigo", p, 1130, 756, 430, 244, "panel_madera");
        Letra(c, 28, 16, 374, 50, "¿Tienes un codigo?", 38, Crema, true);
        campoCodigoSala = Campo(c, new Vector2(0, -76), new Vector2(374, 64), "Codigo de la sala", "");
        BotonEn(c, 28, 158, 374, 64, "azul", "ENTRAR CON CODIGO", () =>
        {
            GuardarNombre(campoNombreUI.text);
#if !MUS_TINKO_SIN_INTERNET
            SalaInternet()?.UnirseConCodigo(campoCodigoSala.text, NombreLocal);
#endif
        }, 24);

        return p.gameObject;
    }

    private TarjetaSala CrearTarjetaSala(Transform padre, float x, float y)
    {
        TarjetaSala t = new();
        RectTransform c = Caja("Sala", padre, x, y, 357, 302, "panel_madera");
        t.Raiz = c.gameObject;
        t.Nombre = Letra(c, 20, 14, 317, 50, "", 36, Crema, true);
        t.Nombre.enableAutoSizing = true;
        t.Nombre.fontSizeMin = 20;
        t.Nombre.fontSizeMax = 36;
        t.Nombre.enableWordWrapping = false;

        // Mini mesa con las 4 sillas.
        RectTransform mapa = Caja("Mapa", c, 20, 74, 112, 112);
        Imagen("Mesa", mapa, "circulo", 58, new Color(0.6f, 0.38f, 0.21f)).rectTransform.anchoredPosition = Vector2.zero;
        t.Sillas = new Image[4];
        Vector2[] sitios = { new(0, -38), new(38, 0), new(0, 38), new(-38, 0) };

        for (int i = 0; i < 4; i++)
        {
            t.Sillas[i] = Imagen("Silla", mapa, "circulo", 32, Color.white);
            t.Sillas[i].rectTransform.anchoredPosition = sitios[i];
        }

        foreach (Transform hijo in mapa)
            ((RectTransform)hijo).anchorMin = ((RectTransform)hijo).anchorMax = new Vector2(0.5f, 0.5f);

        t.Info = Letra(c, 146, 78, 196, 110, "", 23, CremaApagada);
        RectTransform amigo = Caja("Amigo", c, 236, 196, 102, 34, "placa", new Color(0.5f, 0.3f, 0.78f));
        Letra(amigo, 0, 0, 102, 34, "AMIGO", 18, Color.white, false, TextAlignmentOptions.Center, false).fontStyle = FontStyles.Bold;
        t.Amigo = amigo.gameObject;
        t.Boton = BotonEn(c, 20, 214, 317, 70, "verde", "ENTRAR", () =>
        {
            GuardarNombre(campoNombreUI.text);
#if !MUS_TINKO_SIN_INTERNET
            SalaInternet()?.Unirse(t.Id, NombreLocal);
#endif
        }, 28);
        t.TxtBoton = t.Boton.GetComponentInChildren<TextMeshProUGUI>();
        // La etiqueta AMIGO va encima del boton.
        amigo.SetAsLastSibling();
        amigo.anchoredPosition = new Vector2(236, -40);
        return t;
    }

    private void CrearSalaDesdeMenu()
    {
        GuardarNombre(campoNombreUI.text);
#if !MUS_TINKO_SIN_INTERNET
        SalaInternet()?.CrearSala(NombreLocal, new SalaInternetMus.Opciones
        {
            Nombre = campoNombreSala.text,
            Privada = tipoSala == 1,
            Apuesta = EntradasSala[entradaSala],
            Tantos = TantosSala[tantosSala],
            Segundos = TiemposSala[tiempoSala]
        });
#endif
    }

    private void ActualizarSalasMenu()
    {
        PintarChips(chipsFiltroSalas, filtroSalas);
        PintarChips(chipsTipoSala, tipoSala);
        PintarChips(chipsEntradaSala, entradaSala);
        PintarChips(chipsTantosSala, tantosSala);
        PintarChips(chipsTiempoSala, tiempoSala);

        if (string.IsNullOrEmpty(campoNombreSala.text) && !campoNombreSala.isFocused)
            campoNombreSala.text = "Mesa de " + (string.IsNullOrWhiteSpace(NombreLocal) ? "Mus" : NombreLocal);

#if !MUS_TINKO_SIN_INTERNET
        SalaInternetMus s = SalaInternet();

        if (s == null)
            return;

        bool ocupado = s.Ocupado;
        botonCrearSalaMenu.interactable = !ocupado && !s.EnSala && MonederoMus.PuedePagar(EntradasSala[entradaSala]);
        botonActualizarSalasMenu.interactable = !ocupado && !s.Buscando;
        txtActualizarSalasMenu.text = s.Buscando ? "BUSCANDO..." : "ACTUALIZAR";

        if (!ocupado && !s.EnSala && !s.Buscando && Time.unscaledTime >= siguienteBusquedaSalas)
        {
            siguienteBusquedaSalas = Time.unscaledTime + 6f;
            s.BuscarSalas();
        }

        // Salas de amigos: las que salen en su estado.
        HashSet<string> deAmigos = new();
        foreach (SocialMus.Amigo a in SocialMus.Instancia.Amigos)
            if (a.PuedeUnirse)
                deAmigos.Add(a.Actividad.SalaId);

        List<SalaInternetMus.SalaPublica> lista = new();

        foreach (SalaInternetMus.SalaPublica sala in s.Salas)
        {
            if (filtroSalas == 1 && sala.Apuesta > 0) continue;
            if (filtroSalas == 2 && !deAmigos.Contains(sala.Id)) continue;
            lista.Add(sala);
        }

        int n = ocupado || s.Busquedas == 0 ? 0 : Mathf.Min(tarjetasSalas.Length, lista.Count);
        Color[] colores = ColoresJugadoresMus.Colores;

        for (int i = 0; i < tarjetasSalas.Length; i++)
        {
            TarjetaSala t = tarjetasSalas[i];
            t.Raiz.SetActive(i < n);

            if (i >= n)
                continue;

            SalaInternetMus.SalaPublica sala = lista[i];
            t.Id = sala.Id;
            t.Nombre.text = sala.Nombre;
            t.Info.text = sala.Jugadores + "/" + SalaInternetMus.MaxJugadores + " jugadores\n" +
                          (sala.Apuesta > 0 ? "Entrada " + MonederoMus.Formato(sala.Apuesta) : "Sin apuesta") + "\nA " + sala.Tantos + " tantos";
            t.Amigo.SetActive(deAmigos.Contains(sala.Id));

            for (int k = 0; k < 4; k++)
                t.Sillas[k].color = k < sala.Jugadores ? colores[k % colores.Length] : new Color(1f, 1f, 1f, 0.18f);

            bool puede = MonederoMus.PuedePagar(sala.Apuesta);
            t.Boton.interactable = puede;
            t.TxtBoton.text = puede ? "ENTRAR" : "SIN MONEDAS";
        }

        txtMasSalas.text = lista.Count > tarjetasSalas.Length ? "y " + (lista.Count - tarjetasSalas.Length) + " salas mas" : "";

        if (ocupado)
            txtInfoSalasMenu.text = s.Estado;
        else if (n > 0)
            txtInfoSalasMenu.text = "";
        else if (!string.IsNullOrEmpty(s.Estado))
            txtInfoSalasMenu.text = "<color=#FFB080>" + s.Estado + "</color>";
        else if (s.Busquedas == 0 || s.Buscando)
            txtInfoSalasMenu.text = "Buscando salas...";
        else if (filtroSalas != 0 && s.Salas.Count > 0)
            txtInfoSalasMenu.text = "Ninguna sala con este filtro.";
        else
            txtInfoSalasMenu.text = "No hay salas abiertas ahora mismo.\n<size=80%>¡Crea la tuya! Saldra aqui para que entren los demas.</size>";
#else
        foreach (TarjetaSala t in tarjetasSalas) t.Raiz.SetActive(false);
        txtInfoSalasMenu.text = "Las salas por Internet estan desactivadas.";
#endif
    }

    // ------------------------------------------------------------------ PERFIL

    private Cara caraPerfil;
    private TextMeshProUGUI txtNombrePerfil, txtCodigoPerfil, txtTituloPerfil, txtNivelPerfil, txtXpPerfil, txtParejaPerfil, txtLogrosCuenta, txtSinPartidas;
    private Image barraXpPerfil;
    private TextMeshProUGUI[] valoresStats, textosStats;
    private (Image Medalla, Image Fondo, TextMeshProUGUI Nombre, TextMeshProUGUI Detalle)[] tilesLogros;
    private (GameObject Raiz, Image Chip, TextMeshProUGUI Resultado, TextMeshProUGUI Con, TextMeshProUGUI Contra, TextMeshProUGUI Marcador, TextMeshProUGUI Cuando)[] filasPartidas;
    private int versionPerfilPintada = -1;
    private int versionPerfil;

    private GameObject CrearPantallaPerfil()
    {
        PerfilMus.Cambio += () => versionPerfil++;
        RectTransform p = Caja("Perfil", contenidoMenu, 0, 0, 1560, 1000);

        RectTransform card = Caja("Jugador", p, 0, 0, 430, 1000, "panel_madera");
        caraPerfil = Circulo(card, 105, 28, 220);
        txtNombrePerfil = Letra(card, 20, 262, 390, 70, "", 60, Crema, true, TextAlignmentOptions.Center);
        txtNombrePerfil.enableAutoSizing = true;
        txtNombrePerfil.fontSizeMin = 30;
        txtNombrePerfil.fontSizeMax = 60;
        txtCodigoPerfil = Letra(card, 20, 334, 390, 34, "", 22, CremaApagada, false, TextAlignmentOptions.Center);
        BotonEn(card, 125, 372, 180, 48, "gris", "COPIAR", () =>
        {
#if !MUS_TINKO_SIN_INTERNET
            if (ServiciosUnityMus.CodigoAmigo.Length > 0)
                GUIUtility.systemCopyBuffer = ServiciosUnityMus.CodigoAmigo;
#endif
        }, 20);
        RectTransform chip = Caja("Titulo", card, 55, 434, 320, 46, "placa", new Color(0.95f, 0.89f, 0.75f));
        txtTituloPerfil = Letra(chip, 0, 0, 320, 46, "", 22, Tinta, false, TextAlignmentOptions.Center, false);
        txtTituloPerfil.fontStyle = FontStyles.Bold;
        txtNivelPerfil = Letra(card, 28, 496, 200, 36, "", 28, Crema, false, TextAlignmentOptions.MidlineLeft);
        txtNivelPerfil.fontStyle = FontStyles.Bold;
        txtXpPerfil = Letra(card, 200, 496, 202, 36, "", 20, CremaApagada, false, TextAlignmentOptions.MidlineRight);
        barraXpPerfil = Barra(card, 28, 538, 374, 22);
        txtParejaPerfil = Letra(card, 28, 574, 374, 72, "", 22, CremaApagada);
        BotonEn(card, 28, 652, 374, 70, "morado", "CAMBIAR ASPECTO", AbrirTaquilla, 28);
        Letra(card, 28, 738, 374, 32, "Tu nombre", 22, Crema).fontStyle = FontStyles.Bold;
        campoNombreUI = Campo(card, new Vector2(0, -774), new Vector2(374, 66), "Escribe tu apodo", NombreLocal);
        campoNombreUI.characterLimit = PartidaOnlineMus.LargoMaximoNombre;
        campoNombreUI.onEndEdit.AddListener(GuardarNombre);
        BotonEn(card, 28, 856, 374, 66, "verde", "GUARDAR NOMBRE", () =>
        {
            GuardarNombre(campoNombreUI.text);
            MostrarAvisoMonedas("Nombre guardado: " + NombreLocal);
        }, 26);

        Letra(p, 450, 0, 600, 74, "Perfil", 62, Oro, true);

        valoresStats = new TextMeshProUGUI[6];
        textosStats = new TextMeshProUGUI[6];

        for (int i = 0; i < 6; i++)
        {
            RectTransform t = Caja("Dato", p, 450 + (i % 3) * 375, 84 + (i / 3) * 136, 355, 122, "panel_madera_oscura");
            valoresStats[i] = Letra(t, 22, 10, 311, 62, "", 52, Oro, true);
            textosStats[i] = Letra(t, 22, 72, 311, 36, "", 22, CremaApagada);
        }

        RectTransform logros = Caja("Logros", p, 450, 362, 650, 638, "panel_madera");
        Letra(logros, 24, 16, 400, 56, "Logros", 46, Crema, true);
        txtLogrosCuenta = Letra(logros, 424, 24, 200, 40, "", 22, CremaApagada, false, TextAlignmentOptions.MidlineRight);
        tilesLogros = new (Image, Image, TextMeshProUGUI, TextMeshProUGUI)[8];

        for (int i = 0; i < 8; i++)
        {
            RectTransform t = Caja("Logro", logros, 24 + (i % 4) * 152, 84 + (i / 4) * 272, 142, 258, "placa", Color.white);
            Image fondo = t.GetComponent<Image>();
            Image medalla = Imagen("Medalla", t, "circulo", 76, Oro);
            Rect(medalla.gameObject, t, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(76, 76));
            Image anillo = Imagen("Anillo", medalla.transform, "anillo", 76, new Color(0.45f, 0.28f, 0.08f));
            anillo.rectTransform.anchorMin = anillo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            TextMeshProUGUI nombre = Letra(t, 8, 100, 126, 70, "", 21, Tinta, false, TextAlignmentOptions.Center, false);
            nombre.fontStyle = FontStyles.Bold;
            TextMeshProUGUI detalle = Letra(t, 8, 172, 126, 80, "", 17, Tinta, false, TextAlignmentOptions.Top, false);
            tilesLogros[i] = (medalla, fondo, nombre, detalle);
        }

        RectTransform ultimas = Caja("Ultimas", p, 1120, 362, 440, 638, "panel_madera_oscura");
        Letra(ultimas, 24, 16, 392, 56, "Ultimas partidas", 42, Crema, true);
        txtSinPartidas = Letra(ultimas, 24, 90, 392, 100, "Aun no has jugado partidas.", 24, CremaApagada, false, TextAlignmentOptions.Top);
        filasPartidas = new (GameObject, Image, TextMeshProUGUI, TextMeshProUGUI, TextMeshProUGUI, TextMeshProUGUI, TextMeshProUGUI)[6];

        for (int i = 0; i < filasPartidas.Length; i++)
        {
            RectTransform f = Caja("Partida", ultimas, 20, 80 + i * 92, 400, 84);
            RectTransform chipR = Caja("Resultado", f, 0, 18, 112, 44, "placa", Verde);
            TextMeshProUGUI res = Letra(chipR, 0, 0, 112, 44, "", 18, Color.white, false, TextAlignmentOptions.Center, false);
            res.fontStyle = FontStyles.Bold;
            TextMeshProUGUI con = Letra(f, 124, 4, 190, 38, "", 22, Crema, false, TextAlignmentOptions.MidlineLeft);
            con.fontStyle = FontStyles.Bold;
            con.enableWordWrapping = false;
            TextMeshProUGUI contra = Letra(f, 124, 42, 190, 34, "", 17, CremaApagada, false, TextAlignmentOptions.MidlineLeft);
            contra.enableWordWrapping = false;
            TextMeshProUGUI marcador = Letra(f, 300, 2, 100, 42, "", 30, Crema, true, TextAlignmentOptions.MidlineRight);
            TextMeshProUGUI cuando = Letra(f, 280, 44, 120, 32, "", 16, CremaApagada, false, TextAlignmentOptions.MidlineRight);
            filasPartidas[i] = (f.gameObject, chipR.GetComponent<Image>(), res, con, contra, marcador, cuando);
        }

        return p.gameObject;
    }

    private void ActualizarPerfil()
    {
        string nombre = string.IsNullOrWhiteSpace(NombreLocal) ? "Sin nombre" : NombreLocal;
        caraPerfil.Fondo.color = ColoresJugadoresMus.Colores[Mathf.Clamp(TaquillaMus.PersonajeMio, 0, 3)];
        caraPerfil.Letra.text = Inicial(nombre);
        txtNombrePerfil.text = nombre;
#if !MUS_TINKO_SIN_INTERNET
        string codigo = ServiciosUnityMus.CodigoAmigo;
        txtCodigoPerfil.text = codigo.Length > 0 ? "Codigo de amigo: <b><color=#FFF2D6>" + codigo + "</color></b>" : "Codigo de amigo: (conectando...)";
        if (codigo.Length == 0) _ = ServiciosUnityMus.Preparar();
#else
        txtCodigoPerfil.text = "";
#endif

        if (versionPerfilPintada == versionPerfil)
            return;

        versionPerfilPintada = versionPerfil;
        txtTituloPerfil.text = PerfilMus.Titulo;
        txtNivelPerfil.text = "Nivel " + PerfilMus.Nivel;
        txtXpPerfil.text = PerfilMus.XpEnNivel + " / " + PerfilMus.XpParaSubir + " XP";
        Llenar(barraXpPerfil, (float)PerfilMus.XpEnNivel / PerfilMus.XpParaSubir);
        PerfilMus.Pareja pareja = PerfilMus.ParejaFavorita;
        txtParejaPerfil.text = pareja == null ? "Pareja favorita: aun ninguna" :
            "Pareja favorita: <b><color=#FFF2D6>" + pareja.Nombre + "</color></b> · " + pareja.Partidas + " partidas juntos, " + pareja.Ganadas + " ganadas";

        string[] valores =
        {
            PerfilMus.Partidas.ToString(), PerfilMus.PorcentajeVictorias + " %", PerfilMus.OrdagosGanados.ToString(),
            PerfilMus.MejorRacha.ToString(), PerfilMus.LanceFavorito, MonederoMus.Formato(PerfilMus.MonedasGanadas)
        };
        string[] textos =
        {
            "partidas jugadas", "victorias (" + PerfilMus.Ganadas + ")", "ordagos ganados",
            "mejor racha", "el lance que mas ganas", "monedas ganadas"
        };

        for (int i = 0; i < 6; i++)
        {
            valoresStats[i].text = valores[i];
            textosStats[i].text = textos[i];
        }

        List<PerfilMus.Logro> logros = PerfilMus.Logros();
        int hechos = 0;

        for (int i = 0; i < tilesLogros.Length && i < logros.Count; i++)
        {
            PerfilMus.Logro l = logros[i];
            if (l.Hecho) hechos++;
            tilesLogros[i].Fondo.color = l.Hecho ? new Color(0.96f, 0.9f, 0.76f) : new Color(0.42f, 0.32f, 0.24f);
            tilesLogros[i].Medalla.color = l.Hecho ? Oro : new Color(0.3f, 0.23f, 0.17f);
            tilesLogros[i].Nombre.text = l.Nombre;
            tilesLogros[i].Nombre.color = l.Hecho ? Tinta : CremaApagada;
            tilesLogros[i].Detalle.text = l.Detalle + (l.Hecho || string.IsNullOrEmpty(l.Progreso) ? "" : "\n<b>" + l.Progreso + "</b>");
            tilesLogros[i].Detalle.color = l.Hecho ? Tinta : CremaApagada;
        }

        txtLogrosCuenta.text = hechos + " de " + logros.Count;

        IReadOnlyList<PerfilMus.PartidaGuardada> ultimas = PerfilMus.Ultimas;
        txtSinPartidas.gameObject.SetActive(ultimas.Count == 0);

        for (int i = 0; i < filasPartidas.Length; i++)
        {
            bool hay = i < ultimas.Count;
            filasPartidas[i].Raiz.SetActive(hay);

            if (!hay)
                continue;

            PerfilMus.PartidaGuardada g = ultimas[i];
            filasPartidas[i].Chip.color = g.Ganada ? Verde : RojoOscuro;
            filasPartidas[i].Resultado.text = g.Ganada ? "GANADA" : "PERDIDA";
            filasPartidas[i].Con.text = g.Con;
            filasPartidas[i].Contra.text = g.Contra;
            filasPartidas[i].Marcador.text = g.Ordago ? "Ordago" : g.Nos + "-" + g.Ellos;
            filasPartidas[i].Cuando.text = PerfilMus.Hace(g.Fecha);
        }
    }

    // ------------------------------------------------------------------ AMIGOS

    private bool pestanaSolicitudes;
    private Button botonPestanaAmigosLista, botonPestanaSolicitudes;
    private TextMeshProUGUI txtPestanaAmigosLista, txtPestanaSolicitudes, txtInfoAmigos, txtCodigoAmigoGrande;
    private FilaAmigo[] filasAmigos;
    private TMP_InputField campoCodigoAmigo;
    private (GameObject Raiz, Cara Cara, TextMeshProUGUI Nombre, Button Boton)[] filasRecientes;
    private int versionAmigosPintada = -1;

    private GameObject CrearPantallaAmigos()
    {
        RectTransform p = Caja("Amigos", contenidoMenu, 0, 0, 1560, 1000);
        Letra(p, 0, 0, 400, 74, "Amigos", 62, Oro, true);
        botonPestanaAmigosLista = BotonEn(p, 330, 8, 300, 62, "gris", "AMIGOS", () => { pestanaSolicitudes = false; versionAmigosPintada = -1; }, 24);
        botonPestanaSolicitudes = BotonEn(p, 646, 8, 330, 62, "gris", "SOLICITUDES", () => { pestanaSolicitudes = true; versionAmigosPintada = -1; }, 24);
        txtPestanaAmigosLista = botonPestanaAmigosLista.GetComponentInChildren<TextMeshProUGUI>();
        txtPestanaSolicitudes = botonPestanaSolicitudes.GetComponentInChildren<TextMeshProUGUI>();

        RectTransform lista = Caja("Lista", p, 0, 90, 1110, 910, "panel_madera_oscura");
        filasAmigos = new FilaAmigo[8];

        for (int i = 0; i < filasAmigos.Length; i++)
        {
            FilaAmigo f = CrearFilaAmigo(lista, 26, 22 + i * 108, 1058, 78, 26, 170);
            // Segunda accion (rechazar) a la izquierda de la primera.
            f.Accion2 = BotonEn(f.Raiz.transform, 1058 - 170 - 180, 12, 170, 54, "rojo", "RECHAZAR", () => f.AlPulsar2?.Invoke(), 20);
            f.TxtAccion2 = f.Accion2.GetComponentInChildren<TextMeshProUGUI>();
            filasAmigos[i] = f;
        }

        txtInfoAmigos = Letra(lista, 40, 40, 1030, 400, "", 28, CremaApagada, false, TextAlignmentOptions.Top);

        RectTransform codigo = Caja("TuCodigo", p, 1130, 0, 430, 294, "panel_pergamino");
        Letra(codigo, 28, 18, 374, 50, "Tu codigo de amigo", 40, Tinta, true, TextAlignmentOptions.TopLeft, false);
        txtCodigoAmigoGrande = Letra(codigo, 20, 78, 390, 76, "", 50, Tinta, true, TextAlignmentOptions.Center, false);
        txtCodigoAmigoGrande.enableAutoSizing = true;
        txtCodigoAmigoGrande.fontSizeMin = 26;
        txtCodigoAmigoGrande.fontSizeMax = 50;
        BotonEn(codigo, 28, 168, 374, 64, "gris", "COPIAR CODIGO", () =>
        {
#if !MUS_TINKO_SIN_INTERNET
            if (ServiciosUnityMus.CodigoAmigo.Length > 0)
                GUIUtility.systemCopyBuffer = ServiciosUnityMus.CodigoAmigo;
#endif
        }, 24);
        Letra(codigo, 28, 240, 374, 44, "Pasaselo a tus amigos para que te anadan.", 19, Tinta, false, TextAlignmentOptions.TopLeft, false);

        RectTransform anadir = Caja("Anadir", p, 1130, 312, 430, 300, "panel_madera");
        Letra(anadir, 28, 16, 374, 50, "Anadir amigo", 40, Crema, true);
        Letra(anadir, 28, 70, 374, 32, "Su codigo (Nombre#1234)", 20, CremaApagada);
        campoCodigoAmigo = Campo(anadir, new Vector2(0, -106), new Vector2(374, 66), "Nombre#1234", "");
        BotonEn(anadir, 28, 196, 374, 66, "verde", "ENVIAR SOLICITUD", () =>
        {
#if !MUS_TINKO_SIN_INTERNET
            SocialMus.Instancia.AnadirPorCodigo(campoCodigoAmigo.text);
            campoCodigoAmigo.text = "";
#endif
        }, 24);

        RectTransform recientes = Caja("Recientes", p, 1130, 630, 430, 370, "panel_madera_oscura");
        Letra(recientes, 28, 16, 374, 48, "Jugasteis hace poco", 36, Crema, true);
        filasRecientes = new (GameObject, Cara, TextMeshProUGUI, Button)[3];

        for (int i = 0; i < filasRecientes.Length; i++)
        {
            int indice = i;
            RectTransform f = Caja("Reciente", recientes, 24, 80 + i * 92, 382, 76);
            Cara cara = Circulo(f, 0, 6, 62);
            TextMeshProUGUI nombre = Letra(f, 76, 6, 170, 62, "", 24, Crema, false, TextAlignmentOptions.MidlineLeft);
            nombre.enableWordWrapping = false;
            Button b = BotonEn(f, 256, 10, 126, 54, "morado", "ANADIR", () => AnadirReciente(indice), 20);
            filasRecientes[i] = (f.gameObject, cara, nombre, b);
        }

        return p.gameObject;
    }

    private void AnadirReciente(int indice)
    {
#if !MUS_TINKO_SIN_INTERNET
        if (indice < SalaInternetMus.Recientes.Count)
            SocialMus.Instancia.AnadirPorId(SalaInternetMus.Recientes[indice].Id, SalaInternetMus.Recientes[indice].Nombre);
#endif
    }

    private void ActualizarAmigosMenu()
    {
        PintarPestana(botonPestanaAmigosLista, !pestanaSolicitudes);
        PintarPestana(botonPestanaSolicitudes, pestanaSolicitudes);

#if !MUS_TINKO_SIN_INTERNET
        SocialMus s = SocialMus.Instancia;
        txtCodigoAmigoGrande.text = ServiciosUnityMus.CodigoAmigo.Length > 0 ? ServiciosUnityMus.CodigoAmigo : "...";
        txtPestanaAmigosLista.text = "AMIGOS · " + s.Amigos.Count;
        txtPestanaSolicitudes.text = "SOLICITUDES · " + s.Solicitudes.Count;

        for (int i = 0; i < filasRecientes.Length; i++)
        {
            bool hay = i < SalaInternetMus.Recientes.Count;
            filasRecientes[i].Raiz.SetActive(hay);

            if (!hay)
                continue;

            SalaInternetMus.JugadorReciente r = SalaInternetMus.Recientes[i];
            PintarCara(filasRecientes[i].Cara, r.Nombre, false);
            filasRecientes[i].Nombre.text = r.Nombre;
            bool yaAmigo = s.Amigos.Exists(a => a.Id == r.Id) || s.Enviadas.Exists(a => a.Id == r.Id);
            filasRecientes[i].Boton.gameObject.SetActive(!yaAmigo);
        }

        if (!pestanaSolicitudes)
        {
            foreach (FilaAmigo f in filasAmigos)
                f.Accion2.gameObject.SetActive(false);

            PintarListaAmigos(filasAmigos, txtInfoAmigos, false);

            if (s.AmigosListos && s.Amigos.Count == 0)
                txtInfoAmigos.text = "Aun no tienes amigos.\n<size=80%>Pasa tu codigo o anade el de un amigo a la derecha.</size>";
            return;
        }

        // Solicitudes recibidas (aceptar / rechazar) y enviadas (pendientes).
        List<(SocialMus.Amigo a, bool recibida)> todas = new();
        foreach (SocialMus.Amigo a in s.Solicitudes) todas.Add((a, true));
        foreach (SocialMus.Amigo a in s.Enviadas) todas.Add((a, false));

        for (int i = 0; i < filasAmigos.Length; i++)
        {
            FilaAmigo f = filasAmigos[i];
            bool hay = i < todas.Count;
            f.Raiz.SetActive(hay);

            if (!hay)
                continue;

            SocialMus.Amigo a = todas[i].a;
            bool recibida = todas[i].recibida;
            PintarCara(f.Cara, a.Nombre, false);
            f.Nombre.text = a.Nombre;
            f.Estado.text = recibida ? "Quiere ser tu amigo" : "Solicitud enviada, esperando respuesta";

            if (recibida)
            {
                MostrarAccion(f.Accion, f.TxtAccion, "verde", "ACEPTAR");
                f.AlPulsar = () => s.Aceptar(a);
                f.Accion2.gameObject.SetActive(true);
                f.AlPulsar2 = () => s.Rechazar(a);
            }
            else
            {
                f.Accion.gameObject.SetActive(false);
                f.Accion2.gameObject.SetActive(false);
            }
        }

        txtInfoAmigos.text = !s.AmigosListos ? (string.IsNullOrEmpty(s.ErrorAmigos) ? "Cargando..." : s.ErrorAmigos)
            : todas.Count == 0 ? "No tienes solicitudes." : "";
#else
        foreach (FilaAmigo f in filasAmigos) f.Raiz.SetActive(false);
        foreach (var r in filasRecientes) r.Raiz.SetActive(false);
        txtInfoAmigos.text = "Amigos no disponible en esta version.";
#endif
    }

    // ------------------------------------------------------------------ CLASIFICACION

    private int pestanaClasificacion;
    private Button[] chipsClasificacion;
    private TextMeshProUGUI txtInfoClasif, txtMisPuntos, txtReinicio;
    private (GameObject Raiz, Cara Cara, TextMeshProUGUI Nombre, RectTransform Peana, TextMeshProUGUI Puesto, TextMeshProUGUI Puntos)[] podio;
    private (GameObject Raiz, Image Resalte, TextMeshProUGUI Puesto, Cara Cara, TextMeshProUGUI Nombre, TextMeshProUGUI Puntos)[] filasClasif;
    private int versionClasifPintada = -1;

    private string TipoClasificacion => pestanaClasificacion == 0 ? "semanal" : pestanaClasificacion == 1 ? "global" : "amigos";

    private GameObject CrearPantallaClasificacion()
    {
        RectTransform p = Caja("Clasificacion", contenidoMenu, 0, 0, 1560, 1000);
        Letra(p, 0, 0, 470, 74, "Clasificacion", 62, Oro, true);
        chipsClasificacion = Chips(p, 480, 8, 196, 62, 14, new[] { "ESTA SEMANA", "GLOBAL", "AMIGOS" }, i =>
        {
            pestanaClasificacion = i;
#if !MUS_TINKO_SIN_INTERNET
            SocialMus.Instancia.CargarClasificacion(TipoClasificacion);
#endif
        }, 22);

        // Podio: 2.o, 1.o, 3.o
        podio = new (GameObject, Cara, TextMeshProUGUI, RectTransform, TextMeshProUGUI, TextMeshProUGUI)[3];
        float[] xs = { 435, 165, 705 };   // 1.o en el centro
        float[] altos = { 200, 150, 116 };

        for (int i = 0; i < 3; i++)
        {
            RectTransform col = Caja("Podio", p, xs[i], 90, 240, 384);
            float tam = i == 0 ? 112 : 92;
            float peanaY = 384 - altos[i];
            Cara cara = Circulo(col, (240 - tam) * 0.5f, peanaY - tam - 52, tam);
            TextMeshProUGUI nombre = Letra(col, 0, peanaY - 48, 240, 44, "", 26, Crema, false, TextAlignmentOptions.Center);
            nombre.fontStyle = FontStyles.Bold;
            nombre.enableWordWrapping = false;
            Color[] medallas = { Oro, new(0.85f, 0.82f, 0.77f), new(0.85f, 0.58f, 0.35f) };
            RectTransform peana = Caja("Peana", col, 0, peanaY, 240, altos[i], "placa", medallas[i]);
            TextMeshProUGUI puesto = Letra(peana, 0, 10, 240, 64, (i + 1) + ".º", 56, Tinta, true, TextAlignmentOptions.Center, false);
            TextMeshProUGUI puntos = Letra(peana, 0, 74, 240, 34, "", 22, Tinta, false, TextAlignmentOptions.Center, false);
            podio[i] = (col.gameObject, cara, nombre, peana, puesto, puntos);
        }

        RectTransform tabla = Caja("Tabla", p, 0, 494, 1110, 506, "panel_madera_oscura");
        filasClasif = new (GameObject, Image, TextMeshProUGUI, Cara, TextMeshProUGUI, TextMeshProUGUI)[7];

        for (int i = 0; i < filasClasif.Length; i++)
        {
            RectTransform f = Caja("Fila", tabla, 16, 14 + i * 68, 1078, 64);
            Image resalte = f.gameObject.AddComponent<Image>();
            resalte.sprite = SpriteUI("placa", 12);
            resalte.type = Image.Type.Sliced;
            resalte.color = new Color(1f, 0.8f, 0.3f, 0.35f);
            resalte.raycastTarget = false;
            TextMeshProUGUI puesto = Letra(f, 12, 4, 96, 56, "", 38, Oro, true, TextAlignmentOptions.MidlineLeft);
            Cara cara = Circulo(f, 110, 6, 52);
            TextMeshProUGUI nombre = Letra(f, 176, 4, 640, 56, "", 28, Crema, false, TextAlignmentOptions.MidlineLeft);
            nombre.fontStyle = FontStyles.Bold;
            TextMeshProUGUI puntos = Letra(f, 830, 4, 230, 56, "", 38, Crema, true, TextAlignmentOptions.MidlineRight);
            filasClasif[i] = (f.gameObject, resalte, puesto, cara, nombre, puntos);
        }

        txtInfoClasif = Letra(p, 0, 200, 1110, 300, "", 28, CremaApagada, false, TextAlignmentOptions.Top);

        RectTransform info = Caja("ComoPuntua", p, 1130, 0, 430, 1000, "panel_pergamino");
        Letra(info, 28, 20, 374, 56, "Como se puntua", 46, Tinta, true, TextAlignmentOptions.TopLeft, false);
        Letra(info, 28, 86, 374, 200, "3 puntos por victoria y 1 por partida jugada.\n\nSolo cuentan las partidas con al menos 2 personas: contra bots no.",
            24, Tinta, false, TextAlignmentOptions.TopLeft, false);
        Letra(info, 28, 300, 374, 40, "Tus puntos", 30, Tinta, false, TextAlignmentOptions.TopLeft, false).fontStyle = FontStyles.Bold;
        txtMisPuntos = Letra(info, 28, 344, 374, 130, "", 24, Tinta, false, TextAlignmentOptions.TopLeft, false);
        RectTransform reinicio = Caja("Reinicio", info, 28, 494, 374, 126, "panel_madera_oscura");
        Letra(reinicio, 22, 14, 330, 32, "La semanal se reinicia en", 20, CremaApagada);
        txtReinicio = Letra(reinicio, 22, 50, 330, 60, "", 44, Oro, true);
        BotonEn(info, 28, 900, 374, 70, "gris", "ACTUALIZAR", () =>
        {
#if !MUS_TINKO_SIN_INTERNET
            SocialMus.Instancia.CargarClasificacion(TipoClasificacion);
#endif
        }, 26);

        return p.gameObject;
    }

    private void ActualizarClasificacionMenu()
    {
        PintarChips(chipsClasificacion, pestanaClasificacion);
        TimeSpan queda = PerfilMus.QuedaSemana;
        txtReinicio.text = queda.Days + " dias " + queda.Hours + " h";

#if !MUS_TINKO_SIN_INTERNET
        SocialMus s = SocialMus.Instancia;
        string puesto = s.MiPuesto != null && s.TipoCargado != "amigos" ? "\nTu puesto: " + s.MiPuesto.Rango + ".º" : "";
        txtMisPuntos.text = "Esta semana: " + PerfilMus.PuntosSemana + "\nEn total: " + PerfilMus.PuntosTotal + puesto;

        if (versionClasifPintada == s.Version)
            return;

        versionClasifPintada = s.Version;
        List<SocialMus.Puesto> lista = s.TipoCargado == TipoClasificacion ? s.Clasificacion : new List<SocialMus.Puesto>();

        for (int i = 0; i < 3; i++)
        {
            bool hay = i < lista.Count;
            podio[i].Raiz.SetActive(hay);

            if (!hay)
                continue;

            PintarCara(podio[i].Cara, lista[i].Nombre, false);
            podio[i].Nombre.text = lista[i].Nombre + (lista[i].Yo ? " (tu)" : "");
            podio[i].Puntos.text = lista[i].Puntos.ToString("0") + " puntos";
        }

        for (int i = 0; i < filasClasif.Length; i++)
        {
            int k = i + 3;
            bool hay = k < lista.Count;
            filasClasif[i].Raiz.SetActive(hay);

            if (!hay)
                continue;

            SocialMus.Puesto e = lista[k];
            filasClasif[i].Resalte.enabled = e.Yo;
            filasClasif[i].Puesto.text = e.Rango + ".º";
            PintarCara(filasClasif[i].Cara, e.Nombre, false);
            filasClasif[i].Nombre.text = e.Nombre + (e.Yo ? " (tu)" : "");
            filasClasif[i].Puntos.text = e.Puntos.ToString("0");
        }

        txtInfoClasif.text = s.CargandoClasificacion && lista.Count == 0 ? "Cargando..." :
            !string.IsNullOrEmpty(s.ErrorClasificacion) ? "<size=85%>" + s.ErrorClasificacion + "</size>" :
            lista.Count == 0 && s.TipoCargado == TipoClasificacion ? "Aun no hay nadie en esta clasificacion.\n<size=80%>¡Juega una partida con otra persona y seras el primero!</size>" : "";
#else
        foreach (var f in filasClasif) f.Raiz.SetActive(false);
        foreach (var c in podio) c.Raiz.SetActive(false);
        txtInfoClasif.text = "La clasificacion no esta disponible en esta version.";
        txtMisPuntos.text = "Esta semana: " + PerfilMus.PuntosSemana + "\nEn total: " + PerfilMus.PuntosTotal;
#endif
    }

    // ------------------------------------------------------------------ MISMA WIFI

    private GameObject CrearPantallaWifi()
    {
        RectTransform p = Caja("Wifi", contenidoMenu, 0, 0, 1560, 1000);
        Letra(p, 0, 0, 700, 74, "Misma WiFi", 62, Oro, true);
        BotonEn(p, 1310, 8, 250, 62, "gris", "VOLVER", () => IrA(PantallaMenu.Inicio), 24);

        RectTransform caja = Caja("Caja", p, 250, 96, 1060, 900, "panel_madera");
        Transform c = caja;

        Button crear = Boton(c, "azul", "CREAR PARTIDA (MISMA WIFI)", new Vector2(560, 70), () =>
        {
            GuardarNombre(campoNombreUI.text);
            conexion?.CrearPartida();
        }, 28);
        Rect(crear.gameObject, c, new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(560, 70));

        campoIpUI = Campo(c, new Vector2(-95, -126), new Vector2(360, 70), "IP del host", conexion != null ? conexion.IpGuardada : "127.0.0.1");
        Button unirse = Boton(c, "verde", "UNIRSE", new Vector2(190, 70), () =>
        {
            GuardarNombre(campoNombreUI.text);
            conexion?.Unirse(campoIpUI.text);
        }, 28);
        Rect(unirse.gameObject, c, new Vector2(0.5f, 1f), new Vector2(185, -126), new Vector2(190, 70));

        // Buscar la partida de otro movil en la misma WiFi (sin escribir la IP).
        CrearBusquedaLan(c, -222);

        Button demo = Boton(c, "naranja", "MODO DEMO (PARA GRABAR)", new Vector2(420, 56), EmpezarDemoDesdeMenu, 22);
        Rect(demo.gameObject, c, new Vector2(0.5f, 1f), new Vector2(0, -470), new Vector2(420, 56));

        GameObject estado = new("Estado", typeof(RectTransform));
        Rect(estado, c, new Vector2(0.5f, 1f), new Vector2(0, -560), new Vector2(940, 300));
        txtEstadoMenuUI = Texto(estado.transform, "", 26, Crema);
        return p.gameObject;
    }

    // ------------------------------------------------------------------ cada fotograma (con el menu a la vista)

    private bool menuEnMarcha;

    private void ActualizarMenuNuevo()
    {
        if (marcoMenu == null)
            return;

        if (!menuEnMarcha)
        {
            menuEnMarcha = true;
            IrA(pantallaMenu);
        }

        // El marco se escala para caber en la pantalla.
        Vector2 tam = ((RectTransform)panelMenuUI.transform).rect.size;
        float escala = Mathf.Clamp(Mathf.Min((tam.x - 40f) / AnchoMarco, (tam.y - 40f) / AltoMarco), 0.3f, 1.35f);
        marcoMenu.localScale = new Vector3(escala, escala, 1f);

        ActualizarAvisoMenu();

#if !MUS_TINKO_SIN_INTERNET
        int solicitudes = SocialMus.Instancia.Solicitudes.Count;
        insigniaAmigos.SetActive(solicitudes > 0);
        txtInsigniaAmigos.text = solicitudes.ToString();
#endif

        if (Time.unscaledTime < siguienteRefrescoMenu)
            return;

        siguienteRefrescoMenu = Time.unscaledTime + 0.25f;

        switch (pantallaMenu)
        {
            case PantallaMenu.Inicio: ActualizarInicio(); break;
            case PantallaMenu.Salas: ActualizarSalasMenu(); break;
            case PantallaMenu.Perfil: ActualizarPerfil(); break;
            case PantallaMenu.Amigos: ActualizarAmigosMenu(); break;
            case PantallaMenu.Clasificacion: ActualizarClasificacionMenu(); break;
            case PantallaMenu.Wifi: ActualizarBusquedaLan(); break;
        }
    }
}
