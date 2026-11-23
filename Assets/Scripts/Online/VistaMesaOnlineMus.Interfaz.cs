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
    private float ultimaSena = -10f;
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
        CrearSelectorEnvite();
        CrearRelojes();
        CrearMinimapa();
        CrearPanelSenas();
        CrearHistorial();
        CrearPanelHost();
        CrearSala();
        CrearChat();
        CrearBocadillosUI();
        CrearTanteo();
        CrearMenu();

        InterfazNuevaActiva = true;
    }

    private void OnDestroy()
    {
        LimpiarMinimapa();

        if (lienzoUI != null)
            InterfazNuevaActiva = false;
    }

    private sealed class BocadilloUI
    {
        public RectTransform Raiz;
        public RectTransform Cola;
        public TextMeshProUGUI Texto;
        public float Inicio;
        public float Hasta;
        public bool Visible;
    }

    private readonly BocadilloUI[] bocadillosUI = new BocadilloUI[4];

    [Header("Bocadillos en pantalla")]
    [SerializeField] private float alturaBocadillos = 0.5f;
    [SerializeField] private float margenSuperiorBocadillos = 330f;

    private void CrearBocadillosUI()
    {
        foreach (BocadilloMus3D b in bocadillos)
            if (b != null)
                b.Ocultar();

        for (int sitio = 0; sitio < 4; sitio++)
        {
            GameObject go = new("Bocadillo_" + sitio, typeof(RectTransform), typeof(Image));
            RectTransform rt = Rect(go, raizUI, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 80));
            rt.pivot = new Vector2(0.5f, 0f);
            Image cuerpo = go.GetComponent<Image>();
            cuerpo.sprite = SpriteUI("bocadillo", 34);
            cuerpo.type = Image.Type.Sliced;
            cuerpo.raycastTarget = false;

            GameObject cola = new("Cola", typeof(RectTransform), typeof(Image));
            RectTransform rc = cola.GetComponent<RectTransform>();
            rc.SetParent(rt, false);
            rc.anchorMin = rc.anchorMax = new Vector2(0.35f, 0f);
            rc.pivot = new Vector2(0.3f, 1f);
            rc.anchoredPosition = new Vector2(0, 8);
            rc.sizeDelta = new Vector2(44, 30);
            Image ic = cola.GetComponent<Image>();
            ic.sprite = SpriteUI("bocadillo_cola", 0);
            ic.raycastTarget = false;

            TextMeshProUGUI t = Texto(go.transform, "", 30, Tinta, false, TextAlignmentOptions.Center, false);
            t.fontStyle = FontStyles.Bold;
            t.enableWordWrapping = false;
            t.rectTransform.offsetMin = new Vector2(18, 14);
            t.rectTransform.offsetMax = new Vector2(-18, -12);

            go.SetActive(false);
            bocadillosUI[sitio] = new BocadilloUI { Raiz = rt, Cola = rc, Texto = t };
        }
    }

    private void MostrarBocadilloUI(int sitio, string texto, Color color, bool fijo)
    {
        if (sitio < 0 || sitio > 3 || bocadillosUI[sitio] == null)
            return;

        BocadilloUI b = bocadillosUI[sitio];
        b.Texto.text = texto;
        b.Texto.color = Color.Lerp(color, Tinta, 0.25f);
        float ancho = Mathf.Clamp(b.Texto.GetPreferredValues(texto).x + 56f, 150f, 460f);
        b.Raiz.sizeDelta = new Vector2(ancho, 80);
        b.Inicio = Time.time;
        b.Hasta = fijo ? float.MaxValue : Time.time + 2.5f;
        b.Visible = true;
    }

    private void OcultarBocadillosUI()
    {
        foreach (BocadilloUI b in bocadillosUI)
            if (b != null)
            {
                b.Visible = false;
                b.Raiz.gameObject.SetActive(false);
            }
    }

    private void ActualizarBocadillosUI(bool mostrar)
    {
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;
        Vector2 mitad = raizUI.rect.size * 0.5f;

        for (int sitio = 0; sitio < 4; sitio++)
        {
            BocadilloUI b = bocadillosUI[sitio];

            if (b == null)
                continue;

            if (Time.time > b.Hasta)
                b.Visible = false;

            bool visible = mostrar && b.Visible;
            Vector2 pos;
            bool colaIzquierda = true;

            if (sitio == SillaCamara && PrimeraPersona)
            {
                pos = new Vector2(0f, -mitad.y + 150f);
            }
            else
            {
                GameObject modelo = ModeloEnSitio(sitio);

                if (modelo == null || cam == null)
                {
                    b.Raiz.gameObject.SetActive(false);
                    continue;
                }

                // Encima de la cabeza de verdad (la raiz del modelo puede estar en otro sitio al cambiar de silla).
                Vector3 punto = CabezaDe(modelo) + Vector3.up * alturaBocadillos;
                Vector3 pantalla = cam.WorldToScreenPoint(punto);

                if (pantalla.z <= 0f)
                    visible = false;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(raizUI, pantalla, null, out pos);
                colaIzquierda = pos.x <= 0f;
            }

            float ancho = b.Raiz.sizeDelta.x;
            float techo = mitad.y - margenSuperiorBocadillos - 80f;

            // Si no cabe encima de la cabeza (chocaria con la barra de arriba), va al lado y no encima de la cara.
            if (pos.y > techo + 30f && !(sitio == SillaCamara && PrimeraPersona))
            {
                bool aLaDerecha = pos.x >= 0f;
                pos.x += (aLaDerecha ? 1f : -1f) * (ancho * 0.5f + 90f);
                pos.y = Mathf.Min(pos.y - 60f, techo);
                colaIzquierda = aLaDerecha;
            }

            pos.x = Mathf.Clamp(pos.x, -mitad.x + ancho * 0.5f + 20f, mitad.x - ancho * 0.5f - 20f);
            pos.y = Mathf.Clamp(pos.y, -mitad.y + 140f, techo);

            if (b.Raiz.gameObject.activeSelf != visible)
                b.Raiz.gameObject.SetActive(visible);

            if (!visible)
                continue;

            b.Raiz.anchoredPosition = pos;
            float k = Mathf.Clamp01((Time.time - b.Inicio) / 0.22f);
            float escala = Mathf.Lerp(0.5f, 1f, k) + 0.12f * Mathf.Sin(k * Mathf.PI);
            b.Raiz.localScale = Vector3.one * escala;
            b.Cola.anchorMin = b.Cola.anchorMax = new Vector2(colaIzquierda ? 0.3f : 0.7f, 0f);
            b.Cola.localScale = new Vector3(colaIzquierda ? 1f : -1f, 1f, 1f);
        }
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
        // Envites libres: abren un selector para elegir cuantos tantos.
        botonesAccion[TipoAccionMus.Envidar] = Boton(barraAcciones.transform, "naranja", "ENVIDO...", t, () => AbrirSelectorEnvite(false));
        botonesAccion[TipoAccionMus.Reenvidar] = Boton(barraAcciones.transform, "naranja", "SUBIR...", t, () => AbrirSelectorEnvite(true));
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
        info.GetComponent<LayoutElement>().preferredHeight = 58;
        txtSenasInfo = Texto(info.transform, "a tu companero", 18, Crema);

        foreach (TipoSenalMus tipo in (TipoSenalMus[])Enum.GetValues(typeof(TipoSenalMus)))
        {
            TipoSenalMus t = tipo;
            botonesSenaUI[t] = Boton(panelSenasUI.transform, "morado", SenalesMus.Nombre(t), new Vector2(234, 50), () =>
            {
                senasHechas.Add(t);
                ultimaSena = Time.time;
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

    private readonly List<TextMeshProUGUI> textosFrasesRapidas = new();
    private string[] frasesMostradas;

    private Button botonChatUI;
    private TextMeshProUGUI txtBotonChat, txtChatUI, txtAvisoChat;
    private GameObject panelChatUI, avisoChatUI;
    private ScrollRect scrollChatUI;
    private TMP_InputField campoChatUI;
    private bool chatAbierto;
    private int versionChatMostrada = -1;
    private int versionChatLeida;
    private float finAvisoChat;

    private void CrearChat()
    {
        botonChatUI = Boton(raizUI, "azul", "CHAT", new Vector2(170, 60), () =>
        {
            chatAbierto = !chatAbierto;
            versionChatLeida = partida.VersionChat;
        }, 24);
        Rect(botonChatUI.gameObject, raizUI, new Vector2(1f, 0f), new Vector2(-20, 20), new Vector2(170, 60));
        txtBotonChat = botonChatUI.GetComponentInChildren<TextMeshProUGUI>();

        avisoChatUI = Panel("AvisoChat", raizUI, "panel_madera_oscura", new Vector2(1f, 0f), new Vector2(-20, 90), new Vector2(520, 60));
        txtAvisoChat = Texto(avisoChatUI.transform, "", 20, Crema, false, TextAlignmentOptions.MidlineLeft);
        txtAvisoChat.rectTransform.offsetMin = new Vector2(16, 6);

        panelChatUI = Panel("Chat", raizUI, "panel_madera_oscura", new Vector2(1f, 0f), new Vector2(-20, 90), new Vector2(520, 340));

        GameObject vista = new("Vista", typeof(RectTransform), typeof(RectMask2D));
        RectTransform rv = vista.GetComponent<RectTransform>();
        rv.SetParent(panelChatUI.transform, false);
        rv.anchorMin = new Vector2(0, 0);
        rv.anchorMax = new Vector2(1, 1);
        rv.offsetMin = new Vector2(18, 140);
        rv.offsetMax = new Vector2(-18, -16);

        GameObject contenido = new("Contenido", typeof(RectTransform), typeof(ContentSizeFitter));
        RectTransform rc = contenido.GetComponent<RectTransform>();
        rc.SetParent(vista.transform, false);
        rc.anchorMin = new Vector2(0, 1);
        rc.anchorMax = new Vector2(1, 1);
        rc.pivot = new Vector2(0.5f, 1);
        rc.anchoredPosition = Vector2.zero;
        rc.sizeDelta = Vector2.zero;
        contenido.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        txtChatUI = contenido.AddComponent<TextMeshProUGUI>();
        txtChatUI.fontSize = 20;
        txtChatUI.color = Crema;
        txtChatUI.richText = true;
        txtChatUI.alignment = TextAlignmentOptions.TopLeft;
        txtChatUI.raycastTarget = false;

        scrollChatUI = panelChatUI.AddComponent<ScrollRect>();
        scrollChatUI.viewport = rv;
        scrollChatUI.content = rc;
        scrollChatUI.horizontal = false;
        scrollChatUI.movementType = ScrollRect.MovementType.Clamped;

        string[] frases = TaquillaMus.Frases;

        for (int i = 0; i < frases.Length; i++)
        {
            int indice = i;
            Button b = Boton(panelChatUI.transform, "gris", frases[i], new Vector2(156, 40), () =>
            {
                string[] actuales = TaquillaMus.Frases;
                if (indice < actuales.Length) partida.EnviarChat(actuales[indice]);
            }, 18);
            textosFrasesRapidas.Add(b.GetComponentInChildren<TextMeshProUGUI>());
            Rect(b.gameObject, panelChatUI.transform, new Vector2(0f, 0f), new Vector2(18 + (i % 3) * 164, 92 - (i / 3) * 44), new Vector2(156, 40));
        }

        campoChatUI = Campo(panelChatUI.transform, new Vector2(0, 0), new Vector2(360, 50), "Escribe un mensaje...", "");
        Rect(campoChatUI.gameObject, panelChatUI.transform, new Vector2(0f, 0f), new Vector2(18, 12), new Vector2(360, 50));
        campoChatUI.characterLimit = PartidaOnlineMus.LargoMaximoChat;
        campoChatUI.onSubmit.AddListener(_ => EnviarMensajeChat());
        Button enviar = Boton(panelChatUI.transform, "verde", "ENVIAR", new Vector2(120, 50), EnviarMensajeChat, 22);
        Rect(enviar.gameObject, panelChatUI.transform, new Vector2(1f, 0f), new Vector2(-18, 12), new Vector2(120, 50));

        panelChatUI.SetActive(false);
        avisoChatUI.SetActive(false);
    }

    private void EnviarMensajeChat()
    {
        if (campoChatUI == null || string.IsNullOrWhiteSpace(campoChatUI.text))
            return;

        partida.EnviarChat(campoChatUI.text);
        campoChatUI.text = "";
        campoChatUI.ActivateInputField();
    }

    private string LineaChat(PartidaOnlineMus.MensajeChat m)
    {
        string nombre = string.IsNullOrEmpty(m.Nombre) ? "?" : m.Nombre;
        string quien = m.Puesto >= 0
            ? TaquillaMus.NombreConColor(nombre, partida.AspectoEnSitio(m.Puesto), ColoresJugadoresMus.HexDe(m.Puesto))
            : "<b>" + nombre + "</b>";
        return quien + ": " + m.Texto;
    }

    private void ActualizarChat(bool enMesa)
    {
        botonChatUI.gameObject.SetActive(enMesa);

        // Si ha cambiado el pack de frases en la taquilla, se cambian los botones.
        if (frasesMostradas != TaquillaMus.Frases)
        {
            frasesMostradas = TaquillaMus.Frases;

            for (int i = 0; i < textosFrasesRapidas.Count && i < frasesMostradas.Length; i++)
                textosFrasesRapidas[i].text = frasesMostradas[i];
        }

        if (!enMesa)
        {
            panelChatUI.SetActive(false);
            avisoChatUI.SetActive(false);
            return;
        }

        int version = partida.VersionChat;

        if (chatAbierto)
            versionChatLeida = version;

        int sinLeer = version - versionChatLeida;
        txtBotonChat.text = sinLeer > 0 && !chatAbierto ? "CHAT (" + sinLeer + ")" : chatAbierto ? "CERRAR" : "CHAT";
        panelChatUI.SetActive(chatAbierto);

        if (version != versionChatMostrada)
        {
            versionChatMostrada = version;
            List<string> lineas = new();

            foreach (PartidaOnlineMus.MensajeChat m in partida.Chat)
                lineas.Add(LineaChat(m));

            txtChatUI.text = string.Join("\n", lineas);
            Canvas.ForceUpdateCanvases();
            scrollChatUI.verticalNormalizedPosition = 0f;

            if (partida.Chat.Count > 0 && !chatAbierto)
            {
                txtAvisoChat.text = LineaChat(partida.Chat[partida.Chat.Count - 1]);
                finAvisoChat = Time.time + 4f;
            }
        }

        avisoChatUI.SetActive(!chatAbierto && Time.time < finAvisoChat);
    }

    private GameObject panelSalaUI;
    private TextMeshProUGUI txtSalaUI;

    private sealed class DiagramaMesa
    {
        public readonly Image[] Sillas = new Image[4];
        public readonly TextMeshProUGUI[] Etiquetas = new TextMeshProUGUI[4];
        public readonly Button[] Botones = new Button[4];
    }

    private DiagramaMesa diagramaSala;

    private DiagramaMesa CrearDiagrama(Transform padre, Vector2 ancla, Vector2 centro, float tam, Action<int> alElegir)
    {
        DiagramaMesa d = new();
        GameObject mesa = new("MesaArriba", typeof(RectTransform), typeof(Image));
        RectTransform rm = Rect(mesa, padre, ancla, centro, new Vector2(tam, tam));
        rm.pivot = new Vector2(0.5f, 0.5f);
        Image imgMesa = mesa.GetComponent<Image>();
        imgMesa.sprite = SpriteUI("mesa_arriba", 0);
        imgMesa.preserveAspect = true;
        imgMesa.raycastTarget = false;

        float distancia = tam * 0.64f;
        float silla = tam * 0.4f;
        Vector2[] direccion = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };
        float[] giro = { 0f, 90f, 180f, -90f };

        for (int p = 0; p < 4; p++)
        {
            int puesto = p;
            GameObject go = new("Silla_" + p, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rs = Rect(go, padre, ancla, centro + direccion[p] * distancia, new Vector2(silla, silla));
            rs.pivot = new Vector2(0.5f, 0.5f);
            rs.localRotation = Quaternion.Euler(0f, 0f, giro[p]);
            Image img = go.GetComponent<Image>();
            img.sprite = SpriteUI("silla_arriba", 0);
            Button b = go.GetComponent<Button>();
            b.onClick.AddListener(() => alElegir(puesto));
            d.Sillas[p] = img;
            d.Botones[p] = b;

            float hueco = p % 2 == 0 ? silla * 0.5f + 50f : silla * 0.5f + 140f;
            GameObject et = new("Etiqueta_" + p, typeof(RectTransform));
            RectTransform re = Rect(et, padre, ancla, centro + direccion[p] * (distancia + hueco), new Vector2(260, 90));
            re.pivot = new Vector2(0.5f, 0.5f);
            d.Etiquetas[p] = Texto(et.transform, "", 36, Crema, true);
        }

        return d;
    }

    private void PintarSitio(DiagramaMesa d, int puesto, string rol, bool mio, bool elegible)
    {
        Color baseColor = mio ? new Color(1f, 0.82f, 0.3f) : puesto % 2 == 0 ? new Color(0.45f, 0.62f, 0.95f) : new Color(0.9f, 0.4f, 0.35f);
        d.Sillas[puesto].color = elegible || mio ? baseColor : baseColor * 0.55f;
        d.Sillas[puesto].rectTransform.localScale = Vector3.one * (mio ? 1.12f : 1f);
        d.Botones[puesto].interactable = elegible || mio;
        d.Etiquetas[puesto].text = ColoresJugadoresMus.Nombre(puesto) + "\n<size=65%>" + rol + "</size>";
    }

    private static readonly string[] NombresSillas = { "silla de abajo", "silla derecha", "silla de arriba", "silla izquierda" };
    private Button botonEmpezarSala;
    private TextMeshProUGUI txtSinSitioUI;

    private void CrearSala()
    {
        panelSalaUI = Panel("Sala", raizUI, "panel_madera", new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1060, 1000));
        panelSalaUI.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

        GameObject titulo = new("Titulo", typeof(RectTransform));
        Rect(titulo, panelSalaUI.transform, new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(1000, 80));
        Texto(titulo.transform, "SALA: ELIGE TU SILLA", 60, Oro, true);

        GameObject info = new("Info", typeof(RectTransform));
        Rect(info, panelSalaUI.transform, new Vector2(0.5f, 1f), new Vector2(0, -94), new Vector2(1000, 44));
        txtSalaUI = Texto(info.transform, "", 26, Crema);

        CrearSelectorMesa(panelSalaUI.transform, -146);
        diagramaSala = CrearDiagrama(panelSalaUI.transform, new Vector2(0.5f, 1f), new Vector2(0, -566), 230, puesto => partida.ElegirSitio(puesto));

        GameObject sinSitio = new("SinSitio", typeof(RectTransform));
        Rect(sinSitio, panelSalaUI.transform, new Vector2(0.5f, 0f), new Vector2(0, 108), new Vector2(1000, 46));
        txtSinSitioUI = Texto(sinSitio.transform, "", 25, Crema);

        Button taquillaSala = Boton(panelSalaUI.transform, "morado", "TAQUILLA", new Vector2(260, 80), AbrirTaquilla, 32);
        Rect(taquillaSala.gameObject, panelSalaUI.transform, new Vector2(0.5f, 0f), new Vector2(-260, 22), new Vector2(260, 80));

        botonEmpezarSala = Boton(panelSalaUI.transform, "verde", "EMPEZAR PARTIDA", new Vector2(420, 80), () => partida.EmpezarPartidaServidor(), 34);
        Rect(botonEmpezarSala.gameObject, panelSalaUI.transform, new Vector2(0.5f, 0f), new Vector2(150, 22), new Vector2(420, 80));
    }

    private void ActualizarSala(bool enMesa, bool empezada)
    {
        bool visible = enMesa && !empezada && !taquillaAbierta;
        panelSalaUI.SetActive(visible);

        if (!visible)
            return;

        ulong yo = NetworkManager.Singleton.LocalClientId;
        int miSitio = -1;

        for (int p = 0; p < 4; p++)
            if (partida.ClienteEnSitio(p) == yo)
                miSitio = p;

        for (int p = 0; p < 4; p++)
        {
            ulong c = partida.ClienteEnSitio(p);
            bool mio = c == yo;
            bool libre = c == PartidaOnlineMus.SinCliente;
            string nombre = libre ? "LIBRE" : partida.NombreEnSitio(p);
            string relacion = miSitio < 0 ? (p % 2 == 0 ? "pareja A" : "pareja B") : mio ? "TU" : p == (miSitio + 2) % 4 ? "companero" : "rival";
            // Las sillas son sitios fijos de la mesa: se nombran por donde estan (en el dibujo, vista desde arriba).
            string texto = "<size=62%>" + NombresSillas[p] + " · " + relacion + "</size>";
            PintarSitio(diagramaSala, p, texto, mio, libre);
            diagramaSala.Etiquetas[p].text = (libre ? "<color=#BBBBBB>LIBRE</color>" : TaquillaMus.NombreConColor(nombre, partida.AspectoEnSitio(p), "")) + "\n" + texto;
        }

        string conexionTexto = "";

        if (conexion != null && !string.IsNullOrEmpty(conexion.IpLocal) && partida.IsServer)
            conexionTexto = "Tu IP: " + conexion.IpLocal;

#if MUS_TINKO_INTERNET
        if (SalaInternet() != null && !string.IsNullOrEmpty(SalaInternet().Codigo))
            conexionTexto = "<color=#FFD24D>CODIGO DE LA SALA: " + SalaInternet().Codigo + "</color>";
#endif

        string instruccion = miSitio < 0 ? "Toca una silla libre para sentarte." : "Puedes cambiarte a otra silla libre.";
        txtSalaUI.text = instruccion + (conexionTexto.Length > 0 ? "   " + conexionTexto : "");

        string sinSitio = partida.SinSitioVisto;
        txtSinSitioUI.text = string.IsNullOrEmpty(sinSitio) ? "Las sillas que queden libres las ocupan bots."
            : "En la sala sin silla: " + sinSitio;

        botonEmpezarSala.gameObject.SetActive(partida.IsServer);
        botonEmpezarSala.interactable = miSitio >= 0 && MonederoMus.PuedePagar(partida.ApuestaSalaVista);
        ActualizarSelectorMesa(miSitio);

        if (!partida.IsServer)
            txtSinSitioUI.text += "\nEsperando a que el host empiece...";
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

        GameObject hoja = Panel("Hoja", panelTanteoUI.transform, "panel_pergamino", new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(1720, 920));
        hoja.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        Transform h = hoja.transform;

        EstadoPublicoMus e = partida.Estado;
        int local = Mathf.Max(0, partida.PuestoLocal);
        int parejaLocal = local % 2;
        int nuestros = parejaLocal == 0 ? e.Puntos0 : e.Puntos1;
        int suyos = parejaLocal == 0 ? e.Puntos1 : e.Puntos0;
        const string Verde = "#2E7D32", Rojo = "#B23A2E";

        Etiqueta(h, new Vector2(0, -18), new Vector2(1600, 72), e.EnCurso ? "TANTEO DE LA MANO" : TextoFase(e, false), 58, true, TextAlignmentOptions.Center, new Vector2(0.5f, 1f));
        Etiqueta(h, new Vector2(0, -90), new Vector2(1600, 40),
            "Esta mano:  <color=" + Verde + "><b>NOSOTROS +" + tabla.TotalNosotros + "</b></color>   ·   <color=" + Rojo + "><b>ELLOS +" + tabla.TotalEllos + "</b></color>",
            30, false, TextAlignmentOptions.Center, new Vector2(0.5f, 1f));

        // ---------- Izquierda: las cartas, por parejas ----------
        int[][] parejas = { new[] { local, (local + 2) % 4 }, new[] { (local + 1) % 4, (local + 3) % 4 } };
        float y = -140;

        for (int pa = 0; pa < 2; pa++)
        {
            bool nuestra = pa == 0;
            Etiqueta(h, new Vector2(40, y), new Vector2(800, 44), "<color=" + (nuestra ? Verde : Rojo) + ">" + (nuestra ? "NOSOTROS" : "ELLOS") + "</color>",
                38, true, TextAlignmentOptions.Left, new Vector2(0f, 1f));
            y -= 46;

            foreach (int puesto in parejas[pa])
            {
                int[] mano = manosVistas == null ? new[] { -1, -1, -1, -1 } :
                    new[] { manosVistas[puesto * 4], manosVistas[puesto * 4 + 1], manosVistas[puesto * 4 + 2], manosVistas[puesto * 4 + 3] };

                for (int c = 0; c < 4 && mano[0] >= 0; c++)
                {
                    GameObject carta = new("Carta", typeof(RectTransform), typeof(Image));
                    Rect(carta, h, new Vector2(0f, 1f), new Vector2(40 + c * 100, y), new Vector2(92, 144));
                    Image img = carta.GetComponent<Image>();
                    img.sprite = BarajaSpritesMus.Obtener(CartasMus.Texto(mano[c]));
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                }

                string resumen = "";

                if (mano[0] >= 0)
                {
                    EvaluadorManoMus.Resultado r = CartasMus.Evaluar(mano);
                    resumen = r.DetallePares + "\n" + (r.TieneJuego ? "Juego " : "Punto ") + r.PuntosJuego;
                }

                Etiqueta(h, new Vector2(450, y - 4), new Vector2(390, 150), NombreEnTanteo(puesto, local) + "\n<size=78%>" + resumen + "</size>",
                    32, false, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
                y -= 156;
            }
        }

        // ---------- Derecha: los lances ----------
        string[] colores = { "#9A6B00", "#1F5F99", "#A0306E", "#B3541E" };
        const float X = 880, AnchoLance = 220, Columna = 290;
        Etiqueta(h, new Vector2(X + AnchoLance, -140), new Vector2(Columna, 44), "<color=" + Verde + ">NOSOTROS</color>", 36, true, TextAlignmentOptions.Left, new Vector2(0f, 1f));
        Etiqueta(h, new Vector2(X + AnchoLance + Columna, -140), new Vector2(Columna, 44), "<color=" + Rojo + ">ELLOS</color>", 36, true, TextAlignmentOptions.Left, new Vector2(0f, 1f));

        for (int i = 0; i < tabla.Filas.Length; i++)
        {
            TablaTanteoMus.Fila f = tabla.Filas[i];
            float yf = -192 - i * 118;

            // La fila se tine del color de quien gana el lance con las cartas.
            if (f.Ganador >= 0)
            {
                GameObject fondo = new("Ganador", typeof(RectTransform), typeof(Image));
                Rect(fondo, h, new Vector2(0f, 1f), new Vector2(X - 12, yf + 6), new Vector2(AnchoLance + Columna * 2 + 12, 112));
                Image img = fondo.GetComponent<Image>();
                img.color = f.Ganador == 0 ? new Color(0.18f, 0.5f, 0.2f, 0.16f) : new Color(0.7f, 0.22f, 0.18f, 0.16f);
                img.raycastTarget = false;
            }

            string gana = f.Ganador < 0 ? "" : "\n<size=60%><color=" + (f.Ganador == 0 ? Verde : Rojo) + ">gana " + (f.Ganador == 0 ? "NOSOTROS" : "ELLOS") + "</color></size>";
            Etiqueta(h, new Vector2(X, yf), new Vector2(AnchoLance, 110), "<color=" + colores[i] + "><b>" + f.Lance + "</b></color>" + gana, 36, true, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
            Etiqueta(h, new Vector2(X + AnchoLance, yf), new Vector2(Columna - 10, 110), f.Nosotros.Count == 0 ? "-" : string.Join("\n", f.Nosotros), 24, false, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
            Etiqueta(h, new Vector2(X + AnchoLance + Columna, yf), new Vector2(Columna - 10, 110), f.Ellos.Count == 0 ? "-" : string.Join("\n", f.Ellos), 24, false, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
        }

        Etiqueta(h, new Vector2(X, -670), new Vector2(AnchoLance, 50), "ESTA MANO", 34, true, TextAlignmentOptions.Left, new Vector2(0f, 1f));
        Etiqueta(h, new Vector2(X + AnchoLance, -670), new Vector2(Columna, 50), "<color=" + Verde + ">+" + tabla.TotalNosotros + "</color>", 36, true, TextAlignmentOptions.Left, new Vector2(0f, 1f));
        Etiqueta(h, new Vector2(X + AnchoLance + Columna, -670), new Vector2(Columna, 50), "<color=" + Rojo + ">+" + tabla.TotalEllos + "</color>", 36, true, TextAlignmentOptions.Left, new Vector2(0f, 1f));
        Etiqueta(h, new Vector2(X, -724), new Vector2(AnchoLance, 56), "TOTAL", 40, true, TextAlignmentOptions.Left, new Vector2(0f, 1f));
        Etiqueta(h, new Vector2(X + AnchoLance, -724), new Vector2(Columna, 56), nuestros + " / " + e.Limite, 40, true, TextAlignmentOptions.Left, new Vector2(0f, 1f));
        Etiqueta(h, new Vector2(X + AnchoLance + Columna, -724), new Vector2(Columna, 56), suyos + " / " + e.Limite, 40, true, TextAlignmentOptions.Left, new Vector2(0f, 1f));

        // Botones abajo a la derecha (ya no tapan las cartas).
        GameObject botones = new("Botones", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        Rect(botones, h, new Vector2(0f, 0f), new Vector2(X - 12, 26), new Vector2(812, 96));
        HorizontalLayoutGroup hl = botones.GetComponent<HorizontalLayoutGroup>();
        hl.spacing = 20;
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = hl.childControlHeight = true;
        hl.childForceExpandWidth = hl.childForceExpandHeight = false;
        Boton(botones.transform, "azul", "VER LA MESA", new Vector2(340, 90), () => tablaVisible = false, 30);
        botonSiguienteUI = Boton(botones.transform, "verde", "SIGUIENTE MANO", new Vector2(400, 90), () => partida.PedirSiguienteMano(), 32);
    }

    // Personaje (con su color, oscurecido para que se lea en el pergamino) y el apodo si es una persona.
    private string NombreEnTanteo(int puesto, int local)
    {
        Color c = ColoresJugadoresMus.ColorDe(puesto) * 0.62f;
        string hex = "#" + ColorUtility.ToHtmlStringRGB(c);
        string apodo = partida.ClienteEnSitio(puesto) != PartidaOnlineMus.SinCliente ? partida.NombreEnSitio(puesto) : "bot";
        return "<b><color=" + hex + ">" + ColoresJugadoresMus.Nombres[ColoresJugadoresMus.PersonajeDe(puesto)] + "</color></b>  <size=72%>" +
               (puesto == local && partida.PuestoLocal >= 0 ? "(tu) " : "") + apodo + "</size>";
    }

    private void Etiqueta(Transform padre, Vector2 pos, Vector2 tamano, string texto, float letra, bool titulo, TextAlignmentOptions alineacion, Vector2 ancla)
    {
        GameObject go = new("Texto", typeof(RectTransform));
        Rect(go, padre, ancla, pos, tamano);
        Texto(go.transform, texto, letra, Tinta, titulo, alineacion, false);
    }

    private const string ClaveNombre = "MusTinko_Nombre";
    private TMP_InputField campoNombreUI;
    private string nombreLocal;
    private bool nombreEnviado;
    private int aspectoEnviado = -1;

    private string NombreLocal
    {
        get
        {
            if (nombreLocal == null)
            {
                try { nombreLocal = PlayerPrefs.GetString(ClaveNombre, ""); }
                catch { nombreLocal = ""; }

                if (string.IsNullOrWhiteSpace(nombreLocal))
                    nombreLocal = "Jugador" + UnityEngine.Random.Range(10, 100);
            }

            return nombreLocal;
        }
    }

    private void GuardarNombre(string nombre)
    {
        nombre = PartidaOnlineMus.LimpiarNombre(nombre);

        if (nombre.Length == 0)
            return;

        nombreLocal = nombre;
        nombreEnviado = false;

        try
        {
            PlayerPrefs.SetString(ClaveNombre, nombre);
            PlayerPrefs.Save();
        }
        catch
        {
        }
    }

    private void EnviarNombreAlEntrar(bool enMesa)
    {
        if (!enMesa)
        {
            nombreEnviado = false;
            return;
        }

        if (nombreEnviado)
        {
            // Si cambias de traje o de color en la taquilla estando en la sala, los demas lo ven.
            if (aspectoEnviado != TaquillaMus.AspectoPropio)
            {
                aspectoEnviado = TaquillaMus.AspectoPropio;
                partida.EnviarAspecto(aspectoEnviado);
            }

            return;
        }

        nombreEnviado = true;
        partida.EnviarNombre(NombreLocal);
        partida.EnviarAspecto(TaquillaMus.AspectoPropio);
        aspectoEnviado = TaquillaMus.AspectoPropio;
    }

    private void CrearMenu()
    {
        panelMenuUI = new GameObject("Menu", typeof(RectTransform), typeof(Image));
        RectTransform rt = panelMenuUI.GetComponent<RectTransform>();
        rt.SetParent(raizUI, false);
        Estirar(rt, 0);
        panelMenuUI.GetComponent<Image>().color = new Color(0.05f, 0.03f, 0.02f, 0.7f);

        GameObject caja = Panel("Caja", panelMenuUI.transform, "panel_madera", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 990));
        caja.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

        GameObject titulo = new("Titulo", typeof(RectTransform));
        Rect(titulo, caja.transform, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(700, 130));
        Texto(titulo.transform, "Mus TINKO", 120, Oro, true);

        GameObject etiquetaNombre = new("EtiquetaNombre", typeof(RectTransform));
        Rect(etiquetaNombre, caja.transform, new Vector2(0.5f, 1f), new Vector2(-150, -170), new Vector2(260, 60));
        Texto(etiquetaNombre.transform, "TU NOMBRE", 30, Oro, true);
        campoNombreUI = Campo(caja.transform, new Vector2(110, -168), new Vector2(340, 64), "Escribe tu apodo", NombreLocal);
        campoNombreUI.characterLimit = PartidaOnlineMus.LargoMaximoNombre;
        campoNombreUI.onEndEdit.AddListener(GuardarNombre);

        float x = 0;
        float y = -270;
        Button jugar = Boton(caja.transform, "dorado", "JUGAR CONTRA BOTS", new Vector2(560, 96), () =>
        {
            GuardarNombre(campoNombreUI.text);
            conexion?.CrearPartida();
        }, 34);
        Rect(jugar.gameObject, caja.transform, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(560, 96));

        y -= 96;
        Button demo = Boton(caja.transform, "naranja", "MODO DEMO (PARA GRABAR)", new Vector2(560, 70), EmpezarDemoDesdeMenu, 26);
        Rect(demo.gameObject, caja.transform, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(560, 70));

        y -= 96;
        Button crear = Boton(caja.transform, "azul", "CREAR PARTIDA (MISMA WIFI)", new Vector2(560, 84), () =>
        {
            GuardarNombre(campoNombreUI.text);
            conexion?.CrearPartida();
        }, 28);
        Rect(crear.gameObject, caja.transform, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(560, 84));

        y -= 100;
        campoIpUI = Campo(caja.transform, new Vector2(x - 95, y), new Vector2(360, 74), "IP del host", conexion != null ? conexion.IpGuardada : "127.0.0.1");
        Button unirse = Boton(caja.transform, "verde", "UNIRSE", new Vector2(190, 74), () =>
        {
            GuardarNombre(campoNombreUI.text);
            conexion?.Unirse(campoIpUI.text);
        }, 28);
        Rect(unirse.gameObject, caja.transform, new Vector2(0.5f, 1f), new Vector2(x + 185, y), new Vector2(190, 74));

#if MUS_TINKO_INTERNET
        y -= 104;
        Button sala = Boton(caja.transform, "morado", "CREAR SALA POR INTERNET", new Vector2(560, 84), () => SalaInternet()?.CrearSala(), 28);
        Rect(sala.gameObject, caja.transform, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(560, 84));
        y -= 98;
        campoCodigoUI = Campo(caja.transform, new Vector2(x - 95, y), new Vector2(360, 74), "Codigo de sala", "");
        Button unirseCodigo = Boton(caja.transform, "morado", "UNIRSE", new Vector2(190, 74), () => SalaInternet()?.UnirseConCodigo(campoCodigoUI.text), 28);
        Rect(unirseCodigo.gameObject, caja.transform, new Vector2(0.5f, 1f), new Vector2(x + 185, y), new Vector2(190, 74));
#endif

        GameObject estado = new("Estado", typeof(RectTransform));
        Rect(estado, caja.transform, new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(680, 110));
        txtEstadoMenuUI = Texto(estado.transform, "", 24, Crema);

        CrearMonedasMenu();
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

        panelMenuUI.SetActive(!conectado && !taquillaAbierta);
        ActualizarTaquilla();

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
        }

        EnviarNombreAlEntrar(enMesa);
        ActualizarMonedas(conectado, enMesa);

        bool revision = enRevision && empezada;
        ActualizarChat(enMesa);
        ActualizarBocadillosUI(empezada && !revision);
        grupoHud.SetActive(empezada && !revision);
        ActualizarHost(enMesa, empezada, revision);
        ActualizarSala(enMesa, empezada);
        AjustarInterfazDemo(enMesa, empezada, revision);
        ActualizarMinimapa(empezada, revision);

        if (!empezada)
        {
            cajaBanner.SetActive(false);
            cajaAyuda.SetActive(false);
            txtAvisoSena.text = "";
            barraAcciones.SetActive(false);
            panelSenasUI.SetActive(false);

            if (relojPropio != null) relojPropio.SetActive(false);
            if (panelEnviteUI != null) panelEnviteUI.SetActive(false);
            panelHistorialUI.SetActive(false);
            botonHistorialUI.gameObject.SetActive(false);
            panelTanteoUI.SetActive(false);
            botonesMesaUI.SetActive(false);
            return;
        }

        ActualizarHud(revision);
        ActualizarAcciones(revision);
        ActualizarRelojes(empezada, revision);
        ActualizarSenasUI(revision);
        ActualizarHistorialUI(revision);
        ActualizarTanteoUI(revision);
    }

    private void ActualizarHost(bool enMesa, bool empezada, bool revision)
    {
        bool esHost = enMesa && partida.IsServer;
        panelHostUI.SetActive(esHost && !revision && empezada);

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

    private int puntosVistos0, puntosVistos1;

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

        // El marcador va al ritmo de la mesa: los tantos se ven cuando la mesa ya ha ensenado la jugada
        // (si no, suben antes de tiempo y al final de la mano saltan los de las dos parejas de golpe).
        if (cola.Count == 0 || !e.EnCurso)
        {
            puntosVistos0 = e.Puntos0;
            puntosVistos1 = e.Puntos1;
        }

        int nuestros = pareja == 0 ? puntosVistos0 : puntosVistos1;
        int suyos = pareja == 0 ? puntosVistos1 : puntosVistos0;
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

        ActualizarSelectorEnvite(miTurno);

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
            par.Value.interactable = partida.SenasConMirada ? Time.time - ultimaSena > 1.5f : !senasHechas.Contains(par.Key);
            posibles += puede ? 1 : 0;
        }

        string mirada = QuienMeMira();
        txtSenasInfo.text = e.MusCorrido ? "En el mus corrido no se hacen senas"
            : posibles == 0 ? "Ninguna con estas cartas" + (mirada.Length > 0 ? "\n" + mirada : "")
            : mirada.Length > 0 ? mirada : "a tu companero";
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
