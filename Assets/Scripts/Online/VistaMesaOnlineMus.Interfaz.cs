using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public partial class VistaMesaOnlineMus
{
    [Header("Interfaz nueva (Canvas, estilo taberna)")]
    [SerializeField] private bool usarInterfazNueva = true;

    public static bool InterfazNuevaActiva { get; private set; }

    private static readonly Color Crema = new(1f, 0.95f, 0.84f);
    private static readonly Color Tinta = new(0.22f, 0.14f, 0.08f);
    private static readonly Color Oro = new(1f, 0.8f, 0.3f);

    private Canvas lienzoUI;
    private RectTransform raizUI;
    private TMP_FontAsset fuenteTitulos;
    private readonly Dictionary<string, Sprite> spritesUI = new();

    private GameObject grupoHud;
    private TextMeshProUGUI txtNos, txtEllos, txtBanner, txtAyuda, txtAvisoSena;
    private GameObject cajaBanner, cajaAyuda;
    private Image barraNos, barraEllos;
    private readonly Image[] celdas = new Image[5];
    private readonly TextMeshProUGUI[] titulosCelda = new TextMeshProUGUI[5];
    private readonly TextMeshProUGUI[] detallesCelda = new TextMeshProUGUI[5];

    private GameObject barraAcciones;
    private readonly Dictionary<TipoAccionMus, Button> botonesAccion = new();

    private GameObject panelSenasUI;
    private TextMeshProUGUI txtSenasInfo;
    private readonly Dictionary<TipoSenalMus, Button> botonesSenaUI = new();

    private GameObject panelHistorialUI;
    private Button botonHistorialUI;
    private TextMeshProUGUI txtHistorialUI;
    private ScrollRect scrollHistorialUI;
    private int versionHistorialMostrada = -1;
    private bool historialAbiertoUI = !Application.isMobilePlatform;

    private GameObject panelHostUI;
    private TextMeshProUGUI txtHostUI;
    private Button botonEmpezarUI, botonPausaUI;

    private GameObject panelMenuUI;
    private TextMeshProUGUI txtEstadoMenuUI;
    private TMP_InputField campoIpUI, campoCodigoUI;
    private bool empezarAlConectar;
    private PruebaConexionMus conexion;

    private GameObject panelTanteoUI, botonesMesaUI;
    private Button botonSiguienteUI, botonSiguienteMesaUI;
    private TablaTanteoMus.Tabla tablaMostrada;

    private void CrearInterfaz()
    {
        conexion = FindFirstObjectByType<PruebaConexionMus>();
        fuenteTitulos = CargarFuente("UI_Tinko/Fuentes/PirataOne-Regular");

        GameObject go = new("InterfazTinko", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        lienzoUI = go.GetComponent<Canvas>();
        lienzoUI.renderMode = RenderMode.ScreenSpaceOverlay;
        lienzoUI.sortingOrder = 50;
        CanvasScaler escala = go.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = Application.isMobilePlatform ? new Vector2(1920, 1080) : new Vector2(2560, 1440);
        escala.matchWidthOrHeight = 0.5f;
        raizUI = go.GetComponent<RectTransform>();

        OcultarBotonesDeLaEscena();
        CrearHud();
        CrearAcciones();
        CrearPanelSenas();
        CrearHistorial();
        CrearPanelHost();
        CrearSala();
        CrearTanteo();
        CrearMenu();

        InterfazNuevaActiva = true;
    }

    private void OnDestroy()
    {
        if (lienzoUI != null)
            InterfazNuevaActiva = false;
    }

    private void OcultarBotonesDeLaEscena()
    {
        foreach (Button b in new[] { botonMus, botonNoHayMus, botonDescartar, botonPasar, botonEnvidar, botonMas, botonQuiero, botonNoQuiero, botonOrdago, botonSiguienteMano })
            if (b != null)
                b.gameObject.SetActive(false);

        if (panelEnvites != null)
            panelEnvites.SetActive(false);

        botonMus = botonNoHayMus = botonDescartar = botonPasar = botonEnvidar = botonMas = botonQuiero = botonNoQuiero = botonOrdago = botonSiguienteMano = null;
        panelEnvites = null;

        for (int i = 0; i < botonesSenas.Length; i++)
        {
            if (botonesSenas[i] != null)
                botonesSenas[i].gameObject.SetActive(false);
            botonesSenas[i] = null;
        }

        foreach (TMP_Text t in new[] { textoFase, textoMarcador, textoEnvite, textoAnuncios })
            if (t != null)
                t.gameObject.SetActive(false);
    }

    private static TMP_FontAsset CargarFuente(string ruta)
    {
        Font fuente = Resources.Load<Font>(ruta);

        if (fuente == null)
            return null;

        try
        {
            return TMP_FontAsset.CreateFontAsset(fuente);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[INTERFAZ] No se pudo crear la fuente de titulos: " + e.Message);
            return null;
        }
    }

    private Sprite SpriteUI(string nombre, float borde)
    {
        if (spritesUI.TryGetValue(nombre, out Sprite cacheado))
            return cacheado;

        Texture2D tex = Resources.Load<Texture2D>("UI_Tinko/" + nombre);
        Sprite sprite = null;

        if (tex != null)
        {
            float b = Mathf.Min(borde, tex.width / 2f - 1, tex.height / 2f - 1);
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }
        else
        {
            Debug.LogWarning("[INTERFAZ] Falta el grafico UI_Tinko/" + nombre);
        }

        spritesUI[nombre] = sprite;
        return sprite;
    }

    private static RectTransform Rect(GameObject go, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tamano)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamano;
        return rt;
    }

    private static void Estirar(RectTransform rt, float margen)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    private GameObject Panel(string nombre, Transform padre, string sprite, Vector2 ancla, Vector2 pos, Vector2 tamano)
    {
        GameObject go = new(nombre, typeof(RectTransform), typeof(Image));
        Rect(go, padre, ancla, pos, tamano);
        Image img = go.GetComponent<Image>();
        img.sprite = SpriteUI(sprite, sprite.StartsWith("panel") ? 30 : 20);
        img.type = Image.Type.Sliced;
        return go;
    }

    private TextMeshProUGUI Texto(Transform padre, string texto, float tamano, Color color, bool titulo = false,
        TextAlignmentOptions alineacion = TextAlignmentOptions.Center, bool contorno = true)
    {
        GameObject go = new("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        Estirar(rt, 6);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();

        if (titulo && fuenteTitulos != null)
            t.font = fuenteTitulos;

        t.text = texto;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = alineacion;
        t.richText = true;
        t.enableWordWrapping = true;
        t.raycastTarget = false;

        if (contorno)
        {
            t.outlineWidth = 0.18f;
            t.outlineColor = new Color32(30, 18, 10, 255);
        }

        return t;
    }

    private Button Boton(Transform padre, string color, string texto, Vector2 tamano, UnityAction alPulsar, float tamanoTexto = 30)
    {
        GameObject go = new("Boton_" + texto, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.sizeDelta = tamano;
        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredWidth = tamano.x;
        le.preferredHeight = tamano.y;

        Image img = go.GetComponent<Image>();
        img.sprite = SpriteUI("boton_" + color, 22);
        img.type = Image.Type.Sliced;

        Button b = go.GetComponent<Button>();
        b.transition = Selectable.Transition.SpriteSwap;
        SpriteState estados = new() { pressedSprite = SpriteUI("boton_" + color + "_pulsado", 22) };
        b.spriteState = estados;
        b.onClick.AddListener(alPulsar);

        TextMeshProUGUI t = Texto(go.transform, texto, tamanoTexto, Color.white);
        t.rectTransform.offsetMin = new Vector2(8, 10);
        t.rectTransform.offsetMax = new Vector2(-8, -4);
        t.fontStyle = FontStyles.Bold;
        t.enableAutoSizing = true;
        t.fontSizeMin = 14;
        t.fontSizeMax = tamanoTexto;
        return b;
    }

    private void CrearHud()
    {
        grupoHud = new GameObject("HUD", typeof(RectTransform));
        Rect(grupoHud, raizUI, new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(1102, 150));

        GameObject marcador = Panel("Marcador", grupoHud.transform, "panel_madera_oscura", new Vector2(0f, 1f), Vector2.zero, new Vector2(260, 150));
        GameObject filaNos = new("Nosotros", typeof(RectTransform));
        Rect(filaNos, marcador.transform, new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(230, 40));
        txtNos = Texto(filaNos.transform, "NOSOTROS 0", 28, Crema, true);
        barraNos = Barra(marcador.transform, new Vector2(0, -58), "barra_nosotros");
        GameObject filaEllos = new("Ellos", typeof(RectTransform));
        Rect(filaEllos, marcador.transform, new Vector2(0.5f, 1f), new Vector2(0, -76), new Vector2(230, 40));
        txtEllos = Texto(filaEllos.transform, "ELLOS 0", 28, Crema, true);
        barraEllos = Barra(marcador.transform, new Vector2(0, -120), "barra_ellos");

        string[] nombres = { "MUS", "GRANDE", "CHICA", "PARES", "JUEGO" };

        for (int i = 0; i < 5; i++)
        {
            GameObject celda = Panel("Lance_" + nombres[i], grupoHud.transform, "panel_madera", new Vector2(0f, 1f), new Vector2(270 + i * 168, 0), new Vector2(160, 150));
            celdas[i] = celda.GetComponent<Image>();
            GameObject cab = new("Titulo", typeof(RectTransform));
            Rect(cab, celda.transform, new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(150, 44));
            titulosCelda[i] = Texto(cab.transform, nombres[i], 32, Crema, true);
            titulosCelda[i].enableAutoSizing = true;
            titulosCelda[i].fontSizeMin = 18;
            titulosCelda[i].fontSizeMax = 32;
            GameObject det = new("Detalle", typeof(RectTransform));
            Rect(det, celda.transform, new Vector2(0.5f, 1f), new Vector2(0, -56), new Vector2(146, 84));
            detallesCelda[i] = Texto(det.transform, "", 19, Crema);
        }

        cajaBanner = Panel("Banner", raizUI, "panel_pergamino", new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(680, 70));
        txtBanner = Texto(cajaBanner.transform, "", 40, Tinta, true, TextAlignmentOptions.Center, false);
        cajaAyuda = Panel("Ayuda", raizUI, "panel_madera_oscura", new Vector2(0.5f, 1f), new Vector2(0, -246), new Vector2(680, 46));
        txtAyuda = Texto(cajaAyuda.transform, "", 21, Crema);
        GameObject sena = new("AvisoSena", typeof(RectTransform));
        Rect(sena, raizUI, new Vector2(0.5f, 1f), new Vector2(0, -298), new Vector2(900, 44));
        txtAvisoSena = Texto(sena.transform, "", 28, new Color(0.88f, 0.78f, 1f));
    }

    private Image Barra(Transform padre, Vector2 pos, string relleno)
    {
        GameObject fondo = new("BarraFondo", typeof(RectTransform), typeof(Image));
        Rect(fondo, padre, new Vector2(0.5f, 1f), pos, new Vector2(210, 18));
        Image f = fondo.GetComponent<Image>();
        f.sprite = SpriteUI("barra_fondo", 8);
        f.type = Image.Type.Sliced;

        GameObject lleno = new("BarraRelleno", typeof(RectTransform), typeof(Image));
        RectTransform rt = lleno.GetComponent<RectTransform>();
        rt.SetParent(fondo.transform, false);
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 0);
        Image img = lleno.GetComponent<Image>();
        img.sprite = SpriteUI(relleno, 8);
        img.type = Image.Type.Sliced;
        return img;
    }

    private void CrearAcciones()
    {
        barraAcciones = new GameObject("Acciones", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        Rect(barraAcciones, raizUI, new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(1500, 110));
        HorizontalLayoutGroup h = barraAcciones.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 14;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;

        Vector2 t = new(200, 88);
        botonesAccion[TipoAccionMus.Mus] = Boton(barraAcciones.transform, "verde", "MUS", t, () => Pedir(AccionMus.Mus));
        botonesAccion[TipoAccionMus.NoHayMus] = Boton(barraAcciones.transform, "rojo", "NO HAY MUS", t, () => Pedir(AccionMus.NoHayMus));
        botonesAccion[TipoAccionMus.Descartar] = Boton(barraAcciones.transform, "azul", "DESCARTAR", t, Descartar);
        botonesAccion[TipoAccionMus.Pasar] = Boton(barraAcciones.transform, "gris", "PASO", t, () => Pedir(AccionMus.Pasar));
        botonesAccion[TipoAccionMus.Envidar] = Boton(barraAcciones.transform, "naranja", "ENVIDO", t, () => Pedir(AccionMus.Envidar(partida.Estado.TantosEnvite)));
        botonesAccion[TipoAccionMus.Reenvidar] = Boton(barraAcciones.transform, "naranja", "2 MAS", t, () => Pedir(AccionMus.Reenvidar(partida.Estado.TantosEnvite)));
        botonesAccion[TipoAccionMus.Quiero] = Boton(barraAcciones.transform, "verde", "QUIERO", t, () => Pedir(AccionMus.Quiero));
        botonesAccion[TipoAccionMus.NoQuiero] = Boton(barraAcciones.transform, "rojo", "NO QUIERO", t, () => Pedir(AccionMus.NoQuiero));
        botonesAccion[TipoAccionMus.Ordago] = Boton(barraAcciones.transform, "dorado", "ORDAGO", t, () => Pedir(AccionMus.Ordago));
    }

    private void CrearPanelSenas()
    {
        panelSenasUI = Panel("Senas", raizUI, "panel_madera_oscura", new Vector2(0f, 0.5f), new Vector2(20, 0), new Vector2(270, 200));
        panelSenasUI.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
        VerticalLayoutGroup v = panelSenasUI.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(18, 18, 14, 18);
        v.spacing = 8;
        panelSenasUI.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        GameObject titulo = new("Titulo", typeof(RectTransform), typeof(LayoutElement));
        titulo.transform.SetParent(panelSenasUI.transform, false);
        titulo.GetComponent<LayoutElement>().preferredHeight = 36;
        Texto(titulo.transform, "SENAS", 30, Oro, true);

        GameObject info = new("Info", typeof(RectTransform), typeof(LayoutElement));
        info.transform.SetParent(panelSenasUI.transform, false);
        info.GetComponent<LayoutElement>().preferredHeight = 44;
        txtSenasInfo = Texto(info.transform, "a tu companero", 18, Crema);

        foreach (TipoSenalMus tipo in (TipoSenalMus[])Enum.GetValues(typeof(TipoSenalMus)))
        {
            TipoSenalMus t = tipo;
            botonesSenaUI[t] = Boton(panelSenasUI.transform, "morado", SenalesMus.Nombre(t), new Vector2(234, 50), () =>
            {
                senasHechas.Add(t);
                partida.SolicitarSenal(t);
            }, 22);
        }
    }

    private void CrearHistorial()
    {
        botonHistorialUI = Boton(raizUI, "gris", "HISTORIAL", new Vector2(190, 60), () => historialAbiertoUI = !historialAbiertoUI, 24);
        Rect(botonHistorialUI.gameObject, raizUI, new Vector2(1f, 1f), new Vector2(-20, -20), new Vector2(190, 60));

        panelHistorialUI = Panel("Historial", raizUI, "panel_madera_oscura", new Vector2(1f, 1f), new Vector2(-20, -170), new Vector2(400, 460));
        GameObject vista = new("Vista", typeof(RectTransform), typeof(RectMask2D));
        RectTransform rv = vista.GetComponent<RectTransform>();
        rv.SetParent(panelHistorialUI.transform, false);
        Estirar(rv, 22);

        GameObject contenido = new("Contenido", typeof(RectTransform), typeof(ContentSizeFitter));
        RectTransform rc = contenido.GetComponent<RectTransform>();
        rc.SetParent(vista.transform, false);
        rc.anchorMin = new Vector2(0, 1);
        rc.anchorMax = new Vector2(1, 1);
        rc.pivot = new Vector2(0.5f, 1);
        rc.anchoredPosition = Vector2.zero;
        rc.sizeDelta = Vector2.zero;
        contenido.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        txtHistorialUI = contenido.AddComponent<TextMeshProUGUI>();
        txtHistorialUI.fontSize = 20;
        txtHistorialUI.color = Crema;
        txtHistorialUI.richText = true;
        txtHistorialUI.alignment = TextAlignmentOptions.TopLeft;
        txtHistorialUI.raycastTarget = false;

        scrollHistorialUI = panelHistorialUI.AddComponent<ScrollRect>();
        scrollHistorialUI.viewport = rv;
        scrollHistorialUI.content = rc;
        scrollHistorialUI.horizontal = false;
        scrollHistorialUI.movementType = ScrollRect.MovementType.Clamped;
    }

    private void CrearPanelHost()
    {
        panelHostUI = Panel("Host", raizUI, "panel_madera_oscura", new Vector2(0f, 1f), new Vector2(20, -20), new Vector2(340, 140));
        GameObject info = new("Info", typeof(RectTransform));
        Rect(info, panelHostUI.transform, new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(316, 60));
        txtHostUI = Texto(info.transform, "", 19, Crema);
        botonEmpezarUI = Boton(panelHostUI.transform, "verde", "EMPEZAR PARTIDA", new Vector2(290, 60), () => partida.EmpezarPartidaServidor(), 26);
        Rect(botonEmpezarUI.gameObject, panelHostUI.transform, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(290, 60));
        botonPausaUI = Boton(panelHostUI.transform, "gris", "PAUSA", new Vector2(180, 52), () => partida.Pausado = !partida.Pausado, 24);
        Rect(botonPausaUI.gameObject, panelHostUI.transform, new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(180, 52));
    }

    private GameObject panelSalaUI;
    private TextMeshProUGUI txtSalaUI;
    private readonly Button[] botonesSitio = new Button[4];
    private readonly Image[] imagenesSitio = new Image[4];
    private readonly TextMeshProUGUI[] textosSitio = new TextMeshProUGUI[4];

    private void CrearSala()
    {
        panelSalaUI = Panel("Sala", raizUI, "panel_madera", new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(760, 560));
        panelSalaUI.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

        GameObject titulo = new("Titulo", typeof(RectTransform));
        Rect(titulo, panelSalaUI.transform, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(700, 70));
        Texto(titulo.transform, "ELIGE TU SITIO", 54, Oro, true);

        GameObject mesa = new("Mesa", typeof(RectTransform), typeof(Image));
        Rect(mesa, panelSalaUI.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(170, 170));
        mesa.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        Image imgMesa = mesa.GetComponent<Image>();
        imgMesa.sprite = SpriteUI("panel_madera_oscura", 30);
        imgMesa.type = Image.Type.Sliced;

        Vector2[] posiciones = { new(0, -175), new(235, -10), new(0, 155), new(-235, -10) };

        for (int p = 0; p < 4; p++)
        {
            int puesto = p;
            Button b = Boton(panelSalaUI.transform, puesto % 2 == 0 ? "azul" : "rojo", ColoresJugadoresMus.Nombres[puesto], new Vector2(250, 92), () => partida.ElegirSitio(puesto), 24);
            RectTransform rb = Rect(b.gameObject, panelSalaUI.transform, new Vector2(0.5f, 0.5f), posiciones[p], new Vector2(250, 92));
            rb.pivot = new Vector2(0.5f, 0.5f);
            botonesSitio[p] = b;
            imagenesSitio[p] = b.GetComponent<Image>();
            textosSitio[p] = b.GetComponentInChildren<TextMeshProUGUI>();
        }

        GameObject pie = new("Pie", typeof(RectTransform));
        Rect(pie, panelSalaUI.transform, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(700, 50));
        txtSalaUI = Texto(pie.transform, "", 20, Crema);
    }

    private void ActualizarSala(bool enMesa, bool empezada)
    {
        bool visible = enMesa && !empezada && !empezarAlConectar;
        panelSalaUI.SetActive(visible);

        if (!visible)
            return;

        ulong yo = NetworkManager.Singleton.LocalClientId;

        for (int p = 0; p < 4; p++)
        {
            ulong c = partida.ClienteEnSitio(p);
            bool mio = c == yo;
            string estadoSitio = mio ? "TU" : c == PartidaOnlineMus.SinCliente ? "libre (bot)" : "jugador";
            string pareja = p % 2 == 0 ? "pareja A" : "pareja B";
            textosSitio[p].text = ColoresJugadoresMus.Nombres[p] + "  <size=60%>" + pareja + "</size>\n<size=75%>" + estadoSitio + "</size>";
            imagenesSitio[p].sprite = SpriteUI(mio ? "boton_dorado" : p % 2 == 0 ? "boton_azul" : "boton_rojo", 22);
            botonesSitio[p].interactable = mio || c == PartidaOnlineMus.SinCliente;
        }

        txtSalaUI.text = partida.IsServer
            ? "Toca un sitio libre para cambiarte. Tu companero se sienta enfrente. Luego, EMPEZAR PARTIDA."
            : "Toca un sitio libre para cambiarte. Esperando a que el host empiece...";
    }

    private void CrearTanteo()
    {
        panelTanteoUI = new GameObject("Tanteo", typeof(RectTransform), typeof(Image));
        RectTransform rt = panelTanteoUI.GetComponent<RectTransform>();
        rt.SetParent(raizUI, false);
        Estirar(rt, 0);
        panelTanteoUI.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);

        botonesMesaUI = new GameObject("BotonesMesa", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        Rect(botonesMesaUI, raizUI, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(760, 100));
        HorizontalLayoutGroup h = botonesMesaUI.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 20;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        Boton(botonesMesaUI.transform, "dorado", "VER TANTEO", new Vector2(340, 96), () => tablaVisible = true);
        botonSiguienteMesaUI = Boton(botonesMesaUI.transform, "verde", "SIGUIENTE MANO", new Vector2(340, 96), () => partida.PedirSiguienteMano());

        panelTanteoUI.SetActive(false);
        botonesMesaUI.SetActive(false);
    }

    private void ConstruirContenidoTanteo()
    {
        for (int i = panelTanteoUI.transform.childCount - 1; i >= 0; i--)
            Destroy(panelTanteoUI.transform.GetChild(i).gameObject);

        tablaMostrada = tabla;

        if (tabla == null)
            return;

        GameObject hoja = Panel("Hoja", panelTanteoUI.transform, "panel_pergamino", new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1560, 880));
        hoja.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

        GameObject titulo = new("Titulo", typeof(RectTransform));
        Rect(titulo, hoja.transform, new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(1400, 70));
        EstadoPublicoMus e = partida.Estado;
        Texto(titulo.transform, e.EnCurso ? "TANTEO DE LA MANO" : TextoFase(e, false), 56, Tinta, true, TextAlignmentOptions.Center, false);

        int local = Mathf.Max(0, partida.PuestoLocal);
        int[] orden = { local, (local + 2) % 4, (local + 1) % 4, (local + 3) % 4 };

        for (int b = 0; b < 4; b++)
        {
            int puesto = orden[b];
            bool nuestro = puesto % 2 == local % 2;
            float y = -110 - b * 180;
            GameObject fila = new("Mano_" + puesto, typeof(RectTransform));
            Rect(fila, hoja.transform, new Vector2(0f, 1f), new Vector2(50, y), new Vector2(560, 36));
            int[] mano = manosVistas == null ? new[] { -1, -1, -1, -1 } :
                new[] { manosVistas[puesto * 4], manosVistas[puesto * 4 + 1], manosVistas[puesto * 4 + 2], manosVistas[puesto * 4 + 3] };
            string resumen = "";

            if (mano[0] >= 0)
            {
                EvaluadorManoMus.Resultado r = CartasMus.Evaluar(mano);
                resumen = r.DetallePares + ", " + (r.TieneJuego ? "juego " : "punto ") + r.PuntosJuego;
            }

            Texto(fila.transform, "<b>" + Nombre(puesto) + (puesto == local ? " (tu)" : "") + "</b>  <size=20><color=" +
                (nuestro ? "#2E7D32" : "#B23A2E") + ">" + (nuestro ? "nosotros" : "ellos") + "</color>  " + resumen + "</size>",
                26, Tinta, false, TextAlignmentOptions.Left, false);

            for (int c = 0; c < 4 && mano[0] >= 0; c++)
            {
                Sprite sprite = BarajaSpritesMus.Obtener(CartasMus.Texto(mano[c]));
                GameObject carta = new("Carta", typeof(RectTransform), typeof(Image));
                Rect(carta, hoja.transform, new Vector2(0f, 1f), new Vector2(50 + c * 92, y - 40), new Vector2(86, 135));
                Image img = carta.GetComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
            }
        }

        string[] colores = { "#9A6B00", "#1F5F99", "#A0306E", "#B3541E" };
        float x0 = 680, ancho = 820, columna = (ancho - 170) / 2f;
        Fila(hoja.transform, x0, -110, "", "<color=#2E7D32>NOSOTROS</color>", "<color=#B23A2E>ELLOS</color>", columna, 34);

        for (int i = 0; i < tabla.Filas.Length; i++)
        {
            TablaTanteoMus.Fila f = tabla.Filas[i];
            Fila(hoja.transform, x0, -170 - i * 120, "<color=" + colores[i] + ">" + f.Lance + "</color>",
                f.Nosotros.Count == 0 ? "-" : string.Join("\n", f.Nosotros),
                f.Ellos.Count == 0 ? "-" : string.Join("\n", f.Ellos), columna, 22);
        }

        int parejaLocal = local % 2;
        int nuestros = parejaLocal == 0 ? e.Puntos0 : e.Puntos1;
        int suyos = parejaLocal == 0 ? e.Puntos1 : e.Puntos0;
        Fila(hoja.transform, x0, -660, "ESTA MANO", "+" + tabla.TotalNosotros, "+" + tabla.TotalEllos, columna, 32);
        Fila(hoja.transform, x0, -720, "TOTAL", nuestros + " / " + e.Limite, suyos + " / " + e.Limite, columna, 36);

        GameObject botones = new("Botones", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        Rect(botones, hoja.transform, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(900, 96));
        HorizontalLayoutGroup h = botones.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 24;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        Boton(botones.transform, "azul", "VER LAS CARTAS EN LA MESA", new Vector2(420, 90), () => tablaVisible = false, 26);
        botonSiguienteUI = Boton(botones.transform, "verde", "SIGUIENTE MANO", new Vector2(360, 90), () => partida.PedirSiguienteMano(), 28);
    }

    private void Fila(Transform padre, float x, float y, string lance, string nos, string ellos, float columna, float tamano)
    {
        GameObject a = new("Lance", typeof(RectTransform));
        Rect(a, padre, new Vector2(0f, 1f), new Vector2(x, y), new Vector2(170, 110));
        Texto(a.transform, "<b>" + lance + "</b>", 30, Tinta, true, TextAlignmentOptions.TopLeft, false);
        GameObject b = new("Nos", typeof(RectTransform));
        Rect(b, padre, new Vector2(0f, 1f), new Vector2(x + 170, y), new Vector2(columna, 110));
        Texto(b.transform, nos, tamano, Tinta, false, TextAlignmentOptions.TopLeft, false);
        GameObject c = new("Ellos", typeof(RectTransform));
        Rect(c, padre, new Vector2(0f, 1f), new Vector2(x + 170 + columna, y), new Vector2(columna, 110));
        Texto(c.transform, ellos, tamano, Tinta, false, TextAlignmentOptions.TopLeft, false);
    }

    private readonly Button[] botonesSitioMenu = new Button[4];
    private TextMeshProUGUI txtParejasMenuUI;
    private const string ClaveSitio = "MusTinko_SitioPreferido";
    private int sitioPreferido = -1;
    private bool sitioPedido;

    private int SitioPreferido
    {
        get
        {
            if (sitioPreferido < 0)
            {
                try { sitioPreferido = Mathf.Clamp(PlayerPrefs.GetInt(ClaveSitio, 0), 0, 3); }
                catch { sitioPreferido = 0; }
            }

            return sitioPreferido;
        }
    }

    private void ElegirSitioPreferido(int puesto)
    {
        sitioPreferido = puesto;

        try
        {
            PlayerPrefs.SetInt(ClaveSitio, puesto);
            PlayerPrefs.Save();
        }
        catch
        {
        }
    }

    private void ActualizarMenuSitios()
    {
        int elegido = SitioPreferido;

        for (int p = 0; p < 4; p++)
            botonesSitioMenu[p].GetComponent<Image>().sprite = SpriteUI(p == elegido ? "boton_dorado" : p % 2 == 0 ? "boton_azul" : "boton_rojo", 22);

        int companero = (elegido + 2) % 4;
        txtParejasMenuUI.text = "Juegas con " + ColoresJugadoresMus.Nombre(companero) + " contra " +
                                ColoresJugadoresMus.Nombre((elegido + 1) % 4) + " y " + ColoresJugadoresMus.Nombre((elegido + 3) % 4);
    }

    private void PedirSitioAlEntrar(bool enMesa)
    {
        if (!enMesa)
        {
            sitioPedido = false;
            return;
        }

        if (sitioPedido || partida.PartidaEnCurso)
            return;

        ulong yo = NetworkManager.Singleton.LocalClientId;
        int elegido = SitioPreferido;

        if (partida.ClienteEnSitio(elegido) == yo)
        {
            sitioPedido = true;
            return;
        }

        bool hayAlguien = false;
        for (int p = 0; p < 4; p++)
            hayAlguien |= partida.ClienteEnSitio(p) == yo;

        if (!hayAlguien)
            return;

        if (partida.ClienteEnSitio(elegido) == PartidaOnlineMus.SinCliente)
            partida.ElegirSitio(elegido);
        else
            sitioPedido = true;
    }

    private void CrearMenu()
    {
        panelMenuUI = new GameObject("Menu", typeof(RectTransform), typeof(Image));
        RectTransform rt = panelMenuUI.GetComponent<RectTransform>();
        rt.SetParent(raizUI, false);
        Estirar(rt, 0);
        panelMenuUI.GetComponent<Image>().color = new Color(0.05f, 0.03f, 0.02f, 0.7f);

        GameObject caja = Panel("Caja", panelMenuUI.transform, "panel_madera", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 1020));
        caja.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

        GameObject titulo = new("Titulo", typeof(RectTransform));
        Rect(titulo, caja.transform, new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(700, 140));
        Texto(titulo.transform, "Mus TINKO", 120, Oro, true);

        GameObject etiqueta = new("EtiquetaSitio", typeof(RectTransform));
        Rect(etiqueta, caja.transform, new Vector2(0.5f, 1f), new Vector2(0, -172), new Vector2(680, 36));
        Texto(etiqueta.transform, "ELIGE TU SITIO", 28, Oro, true);

        for (int p = 0; p < 4; p++)
        {
            int puesto = p;
            Button b = Boton(caja.transform, puesto % 2 == 0 ? "azul" : "rojo", ColoresJugadoresMus.Nombres[puesto], new Vector2(160, 70), () => ElegirSitioPreferido(puesto), 24);
            Rect(b.gameObject, caja.transform, new Vector2(0.5f, 1f), new Vector2(-255 + puesto * 170, -214), new Vector2(160, 70));
            botonesSitioMenu[puesto] = b;
        }

        GameObject parejas = new("Parejas", typeof(RectTransform));
        Rect(parejas, caja.transform, new Vector2(0.5f, 1f), new Vector2(0, -288), new Vector2(680, 30));
        txtParejasMenuUI = Texto(parejas.transform, "", 18, Crema);

        float y = -330;
        Button jugar = Boton(caja.transform, "dorado", "JUGAR CONTRA BOTS", new Vector2(600, 96), () =>
        {
            empezarAlConectar = true;
            conexion?.CrearPartida();
        }, 34);
        Rect(jugar.gameObject, caja.transform, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(600, 96));

        y -= 110;
        Button crear = Boton(caja.transform, "azul", "CREAR PARTIDA (MISMA WIFI)", new Vector2(600, 86), () => conexion?.CrearPartida(), 28);
        Rect(crear.gameObject, caja.transform, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(600, 86));

        y -= 104;
        campoIpUI = Campo(caja.transform, new Vector2(-110, y), new Vector2(380, 76), "IP del host", conexion != null ? conexion.IpGuardada : "127.0.0.1");
        Button unirse = Boton(caja.transform, "verde", "UNIRSE", new Vector2(200, 76), () => conexion?.Unirse(campoIpUI.text), 28);
        Rect(unirse.gameObject, caja.transform, new Vector2(0.5f, 1f), new Vector2(200, y), new Vector2(200, 76));

#if MUS_TINKO_INTERNET
        y -= 110;
        Button sala = Boton(caja.transform, "morado", "CREAR SALA POR INTERNET", new Vector2(600, 86), () => SalaInternet()?.CrearSala(), 28);
        Rect(sala.gameObject, caja.transform, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(600, 86));
        y -= 100;
        campoCodigoUI = Campo(caja.transform, new Vector2(-110, y), new Vector2(380, 76), "Codigo de sala", "");
        Button unirseCodigo = Boton(caja.transform, "morado", "UNIRSE", new Vector2(200, 76), () => SalaInternet()?.UnirseConCodigo(campoCodigoUI.text), 28);
        Rect(unirseCodigo.gameObject, caja.transform, new Vector2(0.5f, 1f), new Vector2(200, y), new Vector2(200, 76));
#endif

        GameObject estado = new("Estado", typeof(RectTransform));
        Rect(estado, caja.transform, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(680, 110));
        txtEstadoMenuUI = Texto(estado.transform, "", 24, Crema);
    }

#if MUS_TINKO_INTERNET
    private SalaInternetMus salaInternet;

    private SalaInternetMus SalaInternet()
    {
        if (salaInternet == null)
            salaInternet = FindFirstObjectByType<SalaInternetMus>();
        return salaInternet;
    }
#endif

    private TMP_InputField Campo(Transform padre, Vector2 pos, Vector2 tamano, string marcador, string valor)
    {
        GameObject go = new("Campo_" + marcador, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        Rect(go, padre, new Vector2(0.5f, 1f), pos, tamano);
        Image fondo = go.GetComponent<Image>();
        fondo.sprite = SpriteUI("panel_pergamino", 20);
        fondo.type = Image.Type.Sliced;

        GameObject area = new("Area", typeof(RectTransform), typeof(RectMask2D));
        RectTransform ra = area.GetComponent<RectTransform>();
        ra.SetParent(go.transform, false);
        Estirar(ra, 14);

        TextMeshProUGUI texto = Texto(area.transform, "", 30, Tinta, false, TextAlignmentOptions.MidlineLeft, false);
        texto.enableWordWrapping = false;
        TextMeshProUGUI pista = Texto(area.transform, marcador, 30, new Color(Tinta.r, Tinta.g, Tinta.b, 0.45f), false, TextAlignmentOptions.MidlineLeft, false);
        pista.fontStyle = FontStyles.Italic;

        TMP_InputField campo = go.GetComponent<TMP_InputField>();
        campo.textViewport = ra;
        campo.textComponent = texto;
        campo.placeholder = pista;
        campo.text = valor;
        return campo;
    }

    private void ActualizarInterfaz()
    {
        NetworkManager red = NetworkManager.Singleton;
        bool conectado = red != null && red.IsListening;
        bool enMesa = conectado && partida.IsSpawned;
        bool empezada = enMesa && partida.PartidaEmpezada;

        panelMenuUI.SetActive(!conectado);

        if (!conectado)
        {
            string estadoMenu = conexion != null ? conexion.Estado : "";
#if MUS_TINKO_INTERNET
            if (SalaInternet() != null && !string.IsNullOrEmpty(SalaInternet().Estado))
                estadoMenu = SalaInternet().Estado;
#endif
            if (conexion != null && !string.IsNullOrEmpty(conexion.IpLocal))
                estadoMenu += "\n<size=20>Tu IP en esta red: " + conexion.IpLocal + "</size>";

            txtEstadoMenuUI.text = estadoMenu;
            ActualizarMenuSitios();
        }

        PedirSitioAlEntrar(enMesa);

        if (empezarAlConectar && enMesa && partida.IsServer && !partida.PartidaEmpezada && sitioPedido)
        {
            empezarAlConectar = false;
            partida.EmpezarPartidaServidor();
        }

        bool revision = enRevision && empezada;
        grupoHud.SetActive(empezada && !revision);
        ActualizarHost(enMesa, empezada, revision);
        ActualizarSala(enMesa, empezada);

        if (!empezada)
        {
            cajaBanner.SetActive(false);
            cajaAyuda.SetActive(false);
            txtAvisoSena.text = "";
            barraAcciones.SetActive(false);
            panelSenasUI.SetActive(false);
            panelHistorialUI.SetActive(false);
            botonHistorialUI.gameObject.SetActive(false);
            panelTanteoUI.SetActive(false);
            botonesMesaUI.SetActive(false);
            return;
        }

        ActualizarHud(revision);
        ActualizarAcciones(revision);
        ActualizarSenasUI(revision);
        ActualizarHistorialUI(revision);
        ActualizarTanteoUI(revision);
    }

    private void ActualizarHost(bool enMesa, bool empezada, bool revision)
    {
        bool esHost = enMesa && partida.IsServer;
        panelHostUI.SetActive(esHost && !revision);

        if (!esHost)
            return;

        int conectados = NetworkManager.Singleton.ConnectedClientsIds.Count;
        string texto = "Conectados: " + conectados + " (el resto, bots)";

        if (conexion != null && !string.IsNullOrEmpty(conexion.IpLocal))
            texto += "\n<size=18>IP: " + conexion.IpLocal + "</size>";

#if MUS_TINKO_INTERNET
        if (SalaInternet() != null && !string.IsNullOrEmpty(SalaInternet().Codigo))
            texto += "\n<size=22><color=#FFD24D>CODIGO: " + SalaInternet().Codigo + "</color></size>";
#endif

        txtHostUI.text = texto;
        botonEmpezarUI.gameObject.SetActive(!empezada || !partida.Estado.EnCurso);
        botonPausaUI.gameObject.SetActive(empezada && partida.Estado.EnCurso);

        if (botonPausaUI.gameObject.activeSelf)
            botonPausaUI.GetComponentInChildren<TextMeshProUGUI>().text = partida.Pausado ? "REANUDAR" : "PAUSA";
    }

    private void ActualizarHud(bool revision)
    {
        if (revision)
        {
            cajaBanner.SetActive(false);
            cajaAyuda.SetActive(false);
            txtAvisoSena.text = "";
            return;
        }

        EstadoPublicoMus e = partida.Estado;
        int local = partida.PuestoLocal;
        int pareja = local >= 0 ? local % 2 : 0;
        int nuestros = pareja == 0 ? e.Puntos0 : e.Puntos1;
        int suyos = pareja == 0 ? e.Puntos1 : e.Puntos0;
        int limite = Mathf.Max(1, e.Limite);

        txtNos.text = "NOSOTROS  " + nuestros;
        txtEllos.text = "ELLOS  " + suyos;
        barraNos.rectTransform.sizeDelta = new Vector2(210 * Mathf.Clamp01(nuestros / (float)limite), 0);
        barraEllos.rectTransform.sizeDelta = new Vector2(210 * Mathf.Clamp01(suyos / (float)limite), 0);

        FaseMus fase = cola.Count > 0 ? faseMostrada : e.FaseActual;
        int activa = fase == FaseMus.Mus || fase == FaseMus.Descarte ? 0 : IndiceLance(fase) + 1;
        titulosCelda[4].text = modoUltimoLance ?? "JUEGO";
        bool corrido = cola.Count > 0 ? corridoVisual : e.MusCorrido;

        for (int i = 0; i < 5; i++)
        {
            bool esActiva = i == activa && e.EnCurso && fase != FaseMus.FinMano;
            celdas[i].sprite = SpriteUI(esActiva ? "boton_dorado" : "panel_madera", esActiva ? 22 : 30);
            detallesCelda[i].text = i == 0
                ? (corrido ? "MUS CORRIDO\nel que corte\nsera mano" : activa == 0 ? "Ronda " + e.RondaMus + "\nMano: " + Nombre(e.Mano) : "Mano:\n" + Nombre(e.Mano))
                : resultadosLance[i - 1] ?? "";
        }

        string banner = TextoBanner(e, local, out string ayuda);

        if (avisoTemporal != null && Time.time < finAvisoTemporal)
        {
            banner = avisoTemporal;
            ayuda = avisoAyuda;
        }

        cajaBanner.SetActive(!string.IsNullOrEmpty(banner));
        txtBanner.text = banner ?? "";
        cajaAyuda.SetActive(!string.IsNullOrEmpty(ayuda));
        txtAyuda.text = ayuda ?? "";
        txtAvisoSena.text = avisoSena != null && Time.time < finAvisoSena ? avisoSena : "";
    }

    private void ActualizarAcciones(bool revision)
    {
        EstadoPublicoMus e = partida.Estado;
        int local = partida.PuestoLocal;
        bool miTurno = !revision && e.EnCurso && local >= 0 && e.JugadorActual == local && cola.Count == 0;
        bool alguno = false;

        foreach (KeyValuePair<TipoAccionMus, Button> par in botonesAccion)
        {
            bool visible = miTurno && e.Legal(par.Key);
            if (par.Value.gameObject.activeSelf != visible)
                par.Value.gameObject.SetActive(visible);
            alguno |= visible;
        }

        if (botonesAccion.TryGetValue(TipoAccionMus.Reenvidar, out Button mas))
            mas.GetComponentInChildren<TextMeshProUGUI>().text = e.TantosEnvite + " MAS";

        if (botonesAccion.TryGetValue(TipoAccionMus.Descartar, out Button descartar))
            descartar.interactable = MascaraSeleccion() != 0;

        barraAcciones.SetActive(alguno);
    }

    private void ActualizarSenasUI(bool revision)
    {
        EstadoPublicoMus e = partida.Estado;
        int local = partida.PuestoLocal;
        bool hayMano = !revision && local >= 0 && cartasLocalesMostradas[0] >= 0 && e.EnCurso && e.FaseActual != FaseMus.FinMano;
        panelSenasUI.SetActive(hayMano && mostrarBotonesSenas);

        if (!panelSenasUI.activeSelf)
            return;

        EvaluadorManoMus.Resultado r = CartasMus.Evaluar(cartasLocalesMostradas);
        int posibles = 0;

        foreach (KeyValuePair<TipoSenalMus, Button> par in botonesSenaUI)
        {
            bool puede = !e.MusCorrido && PartidaMus.CumpleSenal(cartasLocalesMostradas, r, par.Key);
            par.Value.gameObject.SetActive(puede);
            par.Value.interactable = !senasHechas.Contains(par.Key);
            posibles += puede ? 1 : 0;
        }

        txtSenasInfo.text = e.MusCorrido ? "En el mus corrido no se hacen senas"
            : posibles == 0 ? "Ninguna con estas cartas" : "a tu companero";
    }

    private void ActualizarHistorialUI(bool revision)
    {
        botonHistorialUI.gameObject.SetActive(!revision);
        panelHistorialUI.SetActive(!revision && historialAbiertoUI);
        botonHistorialUI.GetComponentInChildren<TextMeshProUGUI>().text = historialAbiertoUI ? "OCULTAR" : "HISTORIAL";

        if (!panelHistorialUI.activeSelf || partida.VersionHistorial == versionHistorialMostrada)
            return;

        versionHistorialMostrada = partida.VersionHistorial;
        txtHistorialUI.text = string.Join("\n", partida.LineasHistorial);
        Canvas.ForceUpdateCanvases();
        scrollHistorialUI.verticalNormalizedPosition = 0f;
    }

    private void ActualizarTanteoUI(bool revision)
    {
        if (!revision)
        {
            panelTanteoUI.SetActive(false);
            botonesMesaUI.SetActive(false);
            return;
        }

        if (tablaMostrada != tabla)
            ConstruirContenidoTanteo();

        panelTanteoUI.SetActive(tablaVisible);
        botonesMesaUI.SetActive(!tablaVisible);

        if (botonSiguienteUI != null)
            botonSiguienteUI.gameObject.SetActive(partida.PuedePasarDeMano);

        if (botonSiguienteMesaUI != null)
            botonSiguienteMesaUI.gameObject.SetActive(partida.PuedePasarDeMano);
    }
}
