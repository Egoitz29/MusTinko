using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// MINIMAPA DE LA MESA Y AVISO DE TURNO
//
// Minimapa (abajo a la izquierda): la mesa vista desde arriba con la cara EN DIRECTO de cada jugador.
//   - Tu siempre abajo (como estas sentado); a los lados y enfrente, los demas.
//   - Ficha dorada "M" en el que es mano.
//   - El que tiene que hablar: brillo detras, aro con el tiempo que le queda y la flecha de la mesa apuntandole.
//   - Los que no juegan el lance (sin pares o sin juego) salen apagados.
//   - Placa del nombre azul = tu pareja, roja = los rivales. Abajo: quien habla y cuantos segundos le quedan.
//
// En la mesa 3D:
//   - Un foco de luz calida encima del que tiene que hablar (va pasando de uno a otro). Si se le acaba el tiempo, late en rojo.
//   - Un arco de luz en el borde de la mesa, delante de el, que se va gastando con su tiempo (verde -> amarillo -> rojo).
//   - Un cartelito "MANO" delante del que es mano.
public partial class VistaMesaOnlineMus
{
    [Header("Minimapa de la mesa")]
    [SerializeField] private bool mostrarMinimapa = true;
    [Tooltip("Caras en directo: una camarita pequena va sacando el retrato de cada jugador.")]
    [SerializeField] private bool retratosEnVivo = true;
    [Tooltip("Cuanto se ve alrededor de la cabeza (mas pequeno = mas cerca).")]
    [SerializeField] private float encuadreRetrato = 0.8f;
    [Tooltip("Sube o baja el centro del retrato (para que quepan txapelas y sombreros).")]
    [SerializeField] private float subirRetrato = 0.08f;
    [Tooltip("1 = cada retrato se refresca 1 de cada 4 fotogramas. 2 = la mitad (mejor en moviles).")]
    [SerializeField] private int saltoRetratos = 1;

    [Header("Aviso de turno en la mesa 3D")]
    [SerializeField] private bool focoTurno = true;
    [SerializeField] private float intensidadFoco = 3f;
    [SerializeField] private float anguloFoco = 42f;
    [Tooltip("Altura del foco sobre la cabeza del que habla.")]
    [SerializeField] private float alturaFoco = 1.5f;
    [SerializeField] private Color colorFoco = new(1f, 0.85f, 0.6f);
    [SerializeField] private bool arcoTiempoMesa = true;
    [Tooltip("Donde va el arco del tiempo (fraccion del radio de la mesa).")]
    [SerializeField] private float radioArco = 0.82f;
    [SerializeField] private float anchoArco = 0.03f;
    [SerializeField] private bool cartelMano = true;

    // ------------------------------------------------------------------ minimapa (UI)

    private sealed class SitioMapa
    {
        public RectTransform Raiz;
        public Image Halo, Aro, Borde, Placa;
        public RawImage Cara;
        public TextMeshProUGUI Inicial, Nombre;
        public GameObject Mano;
        public RenderTexture Textura;
        public bool ConRetrato, Colocado;
        public float Escala = 1f;
    }

    private const float TamMapa = 360f;
    private const float AltoPieMapa = 48f;
    private const float AltoCabeceraMapa = 42f;
    private const float RadioSitiosMapa = 118f;
    private const float TamCaraMapa = 92f;
    private static readonly Vector2[] DireccionMapa = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };

    private GameObject panelMapa;
    private RectTransform centroMapa, giroFlechaMapa;
    private Image flechaMapa;
    private TextMeshProUGUI txtLanceMapa, txtPieMapa;
    private readonly SitioMapa[] sitiosMapa = new SitioMapa[4];
    private float anguloFlechaMapa;
    private bool flechaMapaColocada;

    private void CrearMinimapa()
    {
        panelMapa = Panel("Minimapa", raizUI, "panel_madera_oscura", new Vector2(0f, 0f), new Vector2(20, 20),
            new Vector2(TamMapa, AltoCabeceraMapa + TamMapa + AltoPieMapa));

        // En el movil la pantalla es mas pequena: un poco mas chico para no pisar las senas.
        if (Application.isMobilePlatform)
            panelMapa.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        // Cabecera: el lance y lo que hay en juego.
        GameObject lance = new("Lance", typeof(RectTransform));
        Rect(lance, panelMapa.transform, new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(TamMapa - 36, AltoCabeceraMapa));
        txtLanceMapa = Texto(lance.transform, "", 32, Oro, true);
        txtLanceMapa.enableAutoSizing = true;
        txtLanceMapa.fontSizeMin = 16;
        txtLanceMapa.fontSizeMax = 32;
        txtLanceMapa.enableWordWrapping = false;
        txtLanceMapa.rectTransform.offsetMin = txtLanceMapa.rectTransform.offsetMax = Vector2.zero;

        GameObject c = new("Centro", typeof(RectTransform));
        centroMapa = Rect(c, panelMapa.transform, new Vector2(0.5f, 1f), new Vector2(0, -AltoCabeceraMapa - TamMapa * 0.5f), Vector2.zero);
        centroMapa.pivot = new Vector2(0.5f, 0.5f);

        GameObject mesa = new("Mesa", typeof(RectTransform), typeof(Image));
        Rect(mesa, centroMapa, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(176, 176));
        Image imgMesa = mesa.GetComponent<Image>();
        imgMesa.sprite = SpriteUI("mesa_arriba", 0);
        imgMesa.preserveAspect = true;
        imgMesa.raycastTarget = false;

        // Flecha en el centro de la mesa que gira hacia el que habla (como una botella).
        GameObject giro = new("GiroFlecha", typeof(RectTransform));
        giroFlechaMapa = Rect(giro, centroMapa, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        GameObject flecha = new("Flecha", typeof(RectTransform), typeof(Image));
        Rect(flecha, giroFlechaMapa, new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(66, 66));
        flechaMapa = flecha.GetComponent<Image>();
        flechaMapa.sprite = SpriteUI("flecha", 0);
        flechaMapa.raycastTarget = false;

        for (int p = 0; p < 4; p++)
            sitiosMapa[p] = CrearSitioMapa(p);

        GameObject pie = new("Pie", typeof(RectTransform));
        Rect(pie, panelMapa.transform, new Vector2(0.5f, 0f), new Vector2(0, 8), new Vector2(TamMapa - 28, AltoPieMapa - 8));
        txtPieMapa = Texto(pie.transform, "", 22, Crema);
        txtPieMapa.enableAutoSizing = true;
        txtPieMapa.fontSizeMin = 14;
        txtPieMapa.fontSizeMax = 22;

        panelMapa.SetActive(false);
    }

    private SitioMapa CrearSitioMapa(int puesto)
    {
        SitioMapa s = new();
        GameObject raiz = new("Sitio_" + puesto, typeof(RectTransform));
        s.Raiz = Rect(raiz, centroMapa, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TamCaraMapa, TamCaraMapa));

        s.Halo = Imagen("Halo", s.Raiz, "brillo", TamCaraMapa * 1.9f, new Color(1f, 0.82f, 0.35f, 0f));
        s.Aro = Imagen("Aro", s.Raiz, "anillo", TamCaraMapa + 26f, Color.white);
        s.Aro.type = Image.Type.Filled;
        s.Aro.fillMethod = Image.FillMethod.Radial360;
        s.Aro.fillOrigin = (int)Image.Origin360.Top;
        s.Aro.fillClockwise = false;
        s.Borde = Imagen("Borde", s.Raiz, "circulo", TamCaraMapa + 8f, Color.white);

        // La cara va recortada en circulo.
        Image mascara = Imagen("Mascara", s.Raiz, "circulo", TamCaraMapa, new Color(0.17f, 0.11f, 0.07f, 1f));
        mascara.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        GameObject inicial = new("Inicial", typeof(RectTransform));
        Rect(inicial, mascara.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TamCaraMapa, TamCaraMapa));
        s.Inicial = Texto(inicial.transform, "", 46, Crema, true);

        GameObject cara = new("Cara", typeof(RectTransform), typeof(RawImage));
        Rect(cara, mascara.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TamCaraMapa, TamCaraMapa));
        s.Cara = cara.GetComponent<RawImage>();
        s.Cara.raycastTarget = false;
        s.Cara.enabled = false;

        // Placa con el nombre (azul = tu pareja, roja = rivales), montada sobre la parte de abajo del retrato.
        GameObject placa = new("Placa", typeof(RectTransform), typeof(Image));
        Rect(placa, s.Raiz, new Vector2(0.5f, 0.5f), new Vector2(0, -TamCaraMapa * 0.5f - 2f), new Vector2(112, 30));
        s.Placa = placa.GetComponent<Image>();
        s.Placa.sprite = SpriteUI("barra_fondo", 8);
        s.Placa.type = Image.Type.Sliced;
        s.Placa.raycastTarget = false;
        s.Nombre = Texto(placa.transform, "", 19, Color.white);
        s.Nombre.rectTransform.offsetMin = new Vector2(6, 1);
        s.Nombre.rectTransform.offsetMax = new Vector2(-6, -1);
        s.Nombre.enableAutoSizing = true;
        s.Nombre.fontSizeMin = 11;
        s.Nombre.fontSizeMax = 19;
        s.Nombre.fontStyle = FontStyles.Bold;
        s.Nombre.enableWordWrapping = false;

        // Ficha de mano.
        Image ficha = Imagen("FichaMano", s.Raiz, "circulo", 40f, new Color(0.3f, 0.17f, 0.06f, 1f));
        ficha.rectTransform.anchoredPosition = new Vector2(TamCaraMapa * 0.38f, TamCaraMapa * 0.38f);
        Image oro = Imagen("Oro", ficha.rectTransform, "circulo", 34f, Oro);
        s.Mano = ficha.gameObject;
        TextMeshProUGUI m = Texto(oro.transform, "M", 28, Tinta, true, TextAlignmentOptions.Center, false);
        m.rectTransform.offsetMin = m.rectTransform.offsetMax = Vector2.zero;
        s.Mano.SetActive(false);
        return s;
    }

    private Image Imagen(string nombre, Transform padre, string sprite, float tam, Color color)
    {
        GameObject go = new(nombre, typeof(RectTransform), typeof(Image));
        Rect(go, padre, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(tam, tam));
        Image img = go.GetComponent<Image>();
        img.sprite = SpriteUI(sprite, 0);
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // Quien tiene que hablar AHORA en la mesa (cuando ya se han visto las animaciones). -1 = nadie.
    private int TurnoVisible()
    {
        if (!partida.IsSpawned)
            return -1;

        EstadoPublicoMus e = partida.Estado;

        if (!e.EnCurso || cola.Count > 0 || e.FaseActual == FaseMus.FinMano || e.FaseActual == FaseMus.FinPartida)
            return -1;

        return e.JugadorActual >= 0 && e.JugadorActual < 4 ? e.JugadorActual : -1;
    }

    private static string NombreLanceMapa(FaseMus fase)
    {
        switch (fase)
        {
            case FaseMus.Mus: return "MUS";
            case FaseMus.Descarte: return "DESCARTE";
            case FaseMus.Grande: return "GRANDE";
            case FaseMus.Chica: return "CHICA";
            case FaseMus.Pares: return "PARES";
            case FaseMus.JuegoPunto: return "JUEGO";
            default: return "";
        }
    }

    private void ActualizarMinimapa(bool empezada, bool revision)
    {
        if (panelMapa == null)
            return;

        bool tapado = taquillaAbierta || (panelTanteoUI != null && panelTanteoUI.activeSelf) ||
                      (panelCartasDemo != null && panelCartasDemo.activeSelf);
        bool ver = mostrarMinimapa && empezada && !revision && !tapado;
        panelMapa.SetActive(ver);
        ActualizarCamaraRetratos(ver);

        if (!ver)
            return;

        EstadoPublicoMus e = partida.Estado;
        int local = partida.PuestoLocal;
        int silla = SillaCamara;
        int turno = TurnoVisible();
        float quedan = SegundosQueQuedan(out float fraccion);
        bool conReloj = turno >= 0 && quedan >= 0f;
        bool prisa = conReloj && quedan <= 5f;
        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * (prisa ? 10f : 4f));
        float suave = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
        FaseMus fase = e.FaseActual;
        bool enLance = e.EnCurso && PartidaMus.EsLance(fase);

        for (int p = 0; p < 4; p++)
        {
            SitioMapa s = sitiosMapa[p];
            Vector2 destino = DireccionMapa[(p - silla + 4) % 4] * RadioSitiosMapa;
            s.Raiz.anchoredPosition = s.Colocado ? Vector2.Lerp(s.Raiz.anchoredPosition, destino, suave) : destino;
            s.Colocado = true;

            bool habla = p == turno;
            bool fuera = enLance && (e.MascaraHablan & (1 << p)) == 0;
            s.Escala = Mathf.Lerp(s.Escala, habla ? 1.14f : 1f, suave);
            s.Raiz.localScale = new Vector3(s.Escala, s.Escala, 1f);

            if (habla)
                s.Raiz.SetAsLastSibling();

            Color personaje = ColoresJugadoresMus.ColorDe(p);
            Color borde = habla ? Color.Lerp(personaje, Oro, 0.3f + 0.4f * pulso) : personaje * (fuera ? 0.45f : 0.85f);
            borde.a = 1f;
            s.Borde.color = borde;
            s.Halo.color = new Color(1f, prisa ? 0.45f : 0.82f, prisa ? 0.3f : 0.35f, habla ? 0.45f + 0.4f * pulso : 0f);

            s.Aro.enabled = habla;

            if (habla)
            {
                s.Aro.fillAmount = conReloj ? fraccion : 1f;
                s.Aro.color = conReloj ? ColorReloj(fraccion) : new Color(1f, 0.8f, 0.3f, 0.55f + 0.45f * pulso);
            }

            float luz = habla ? 1f : fuera ? 0.38f : 0.75f;
            s.Cara.color = new Color(luz, luz, luz, 1f);
            s.Cara.enabled = s.ConRetrato;
            s.Inicial.enabled = !s.ConRetrato;
            s.Inicial.text = NombreCorto(p, 1);
            s.Inicial.color = personaje;
            s.Mano.SetActive(e.EnCurso && e.Mano == p);

            bool nuestra = local >= 0 ? p % 2 == local % 2 : p % 2 == 0;
            Color placa = nuestra ? new Color(0.26f, 0.45f, 0.82f) : new Color(0.78f, 0.28f, 0.22f);
            s.Placa.color = fuera ? placa * 0.6f : placa;
            s.Nombre.text = p == local ? "TÚ" : NombreCorto(p, 10);
        }

        // Flecha hacia el que habla.
        flechaMapa.enabled = turno >= 0;

        if (turno >= 0)
        {
            Vector2 d = DireccionMapa[(turno - silla + 4) % 4];
            float objetivo = Mathf.Atan2(-d.x, d.y) * Mathf.Rad2Deg;
            anguloFlechaMapa = flechaMapaColocada ? Mathf.LerpAngle(anguloFlechaMapa, objetivo, suave) : objetivo;
            flechaMapaColocada = true;
            giroFlechaMapa.localRotation = Quaternion.Euler(0f, 0f, anguloFlechaMapa);
            flechaMapa.color = conReloj ? Color.Lerp(Color.white, ColorReloj(fraccion), 0.6f) : Color.white;
            float escala = 1f + 0.12f * pulso;
            flechaMapa.rectTransform.localScale = new Vector3(escala, escala, 1f);
        }

        // Cabecera: el lance y lo que hay en juego.
        string lance = NombreLanceMapa(fase);

        if (enLance && e.EstadoEnvite == (int)MotorEnvitesMus.EstadoEnvite.OrdagoPendiente)
            lance += "  <color=#FF6A4D>ORDAGO</color>";
        else if (enLance && e.TantosApostados > 0)
            lance += "  <size=75%><color=#FFF2D6>" + e.TantosApostados + (e.TantosApostados == 1 ? " tanto" : " tantos") + "</color></size>";

        txtLanceMapa.text = lance;

        // Pie: quien habla y cuanto le queda.
        string pie;

        if (turno >= 0 && turno == local)
            pie = "<color=#FFD24D><b>¡TE TOCA!</b></color>" + (conReloj ? "  " + Mathf.CeilToInt(quedan) + " s" : "");
        else if (turno >= 0)
            pie = "Habla " + ColoresJugadoresMus.Nombre(turno) +
                  (conReloj ? "  " + Mathf.CeilToInt(quedan) + " s" : e.EsBot(turno) ? "  <size=80%>(pensando)</size>" : "");
        else if (e.EnCurso)
            pie = "Mano: " + ColoresJugadoresMus.Nombre(e.Mano);
        else
            pie = "";

        txtPieMapa.text = pie;
    }

    private static string NombreCorto(int puesto, int largo)
    {
        string n = ColoresJugadoresMus.NombresPorPuesto[puesto] ?? "";

        if (n.Length == 0)
            return "?";

        return n.Length > largo ? n.Substring(0, largo) : n;
    }

    // ------------------------------------------------------------------ retratos en directo

    private Camera camaraRetratos;
    private int retratoActual = -1;
    private int contadorRetratos;
    private readonly List<Renderer> encendidosParaRetrato = new();

    private void ActualizarCamaraRetratos(bool ver)
    {
        if (!ver || !retratosEnVivo)
        {
            if (camaraRetratos != null)
                camaraRetratos.enabled = false;
            return;
        }

        if (camaraRetratos == null)
            CrearCamaraRetratos();

        contadorRetratos++;

        if (saltoRetratos > 1 && contadorRetratos % saltoRetratos != 0)
        {
            camaraRetratos.enabled = false;
            return;
        }

        // Un retrato por fotograma, por turnos.
        for (int i = 0; i < 4; i++)
        {
            int p = (retratoActual + 1 + i) % 4;
            GameObject modelo = ModeloEnSitio(Relativo(p));

            if (modelo == null)
            {
                sitiosMapa[p].ConRetrato = false;
                continue;
            }

            retratoActual = p;
            EncuadrarRetrato(modelo, sitiosMapa[p]);
            camaraRetratos.enabled = true;
            return;
        }

        camaraRetratos.enabled = false;
    }

    private void CrearCamaraRetratos()
    {
        GameObject go = new("CamaraRetratos");
        camaraRetratos = go.AddComponent<Camera>();
        camaraRetratos.clearFlags = CameraClearFlags.SolidColor;
        camaraRetratos.backgroundColor = new Color(0.17f, 0.11f, 0.07f, 1f);
        camaraRetratos.fieldOfView = 26f;
        camaraRetratos.allowHDR = false;
        camaraRetratos.allowMSAA = false;
        camaraRetratos.depth = -20f;
        camaraRetratos.enabled = false;

        Camera principal = camaraJugador != null ? camaraJugador : Camera.main;

        if (principal != null)
            camaraRetratos.cullingMask = principal.cullingMask;

        UniversalAdditionalCameraData datos = camaraRetratos.GetUniversalAdditionalCameraData();

        if (datos != null)
        {
            datos.renderShadows = false;
            datos.renderPostProcessing = false;
        }

        for (int p = 0; p < 4; p++)
        {
            sitiosMapa[p].Textura = new RenderTexture(192, 192, 16) { name = "Retrato_" + p };
            sitiosMapa[p].Cara.texture = sitiosMapa[p].Textura;
        }

        // Tu personaje esta escondido en primera persona: solo se enciende mientras se saca tu retrato.
        RenderPipelineManager.beginCameraRendering += AntesDeRetrato;
        RenderPipelineManager.endCameraRendering += DespuesDeRetrato;
    }

    private void EncuadrarRetrato(GameObject modelo, SitioMapa s)
    {
        Vector3 cabeza = CabezaDe(modelo);
        float alto = Mathf.Clamp(cabeza.y - CuerpoDe(modelo).y, 0.2f, 2f);
        Vector3 haciaMesa = Plano(CentroMesa() - cabeza);
        Vector3 punto = cabeza + Vector3.up * (alto * subirRetrato);
        float visible = Mathf.Max(0.1f, alto * encuadreRetrato);
        float distancia = visible * 0.5f / Mathf.Tan(camaraRetratos.fieldOfView * 0.5f * Mathf.Deg2Rad);

        Transform t = camaraRetratos.transform;
        t.position = punto + haciaMesa * distancia + Vector3.up * (distancia * 0.12f);
        t.rotation = Quaternion.LookRotation(punto - t.position, Vector3.up);
        // Solo se ve la cabeza: lo de detras (paredes, otros jugadores) queda fuera.
        camaraRetratos.nearClipPlane = Mathf.Max(0.02f, distancia * 0.25f);
        camaraRetratos.farClipPlane = distancia + alto * 0.7f;
        camaraRetratos.targetTexture = s.Textura;
        s.ConRetrato = true;
    }

    private void AntesDeRetrato(ScriptableRenderContext contexto, Camera camara)
    {
        if (camara != camaraRetratos || modeloOculto == null || retratoActual < 0 ||
            ModeloEnSitio(Relativo(retratoActual)) != modeloOculto)
            return;

        foreach (Renderer r in rendersOcultos)
        {
            if (r == null || r.enabled || !r.transform.IsChildOf(modeloOculto.transform))
                continue;

            r.enabled = true;
            encendidosParaRetrato.Add(r);
        }
    }

    private void DespuesDeRetrato(ScriptableRenderContext contexto, Camera camara)
    {
        if (camara != camaraRetratos)
            return;

        foreach (Renderer r in encendidosParaRetrato)
            if (r != null)
                r.enabled = false;

        encendidosParaRetrato.Clear();
    }

    private void LimpiarMinimapa()
    {
        RenderPipelineManager.beginCameraRendering -= AntesDeRetrato;
        RenderPipelineManager.endCameraRendering -= DespuesDeRetrato;

        foreach (SitioMapa s in sitiosMapa)
            if (s != null && s.Textura != null)
                s.Textura.Release();

        if (camaraRetratos != null)
            Destroy(camaraRetratos.gameObject);
    }

    // ------------------------------------------------------------------ aviso de turno en 3D

    private Light focoLuz;
    private Vector3 focoPosicion;
    private bool focoColocado;
    private float focoNivel;

    private Mesh mallaArco, mallaPista;
    private MeshRenderer renderArco, renderPista;
    private float radioMesaMedido = -1f;
    private int turnoArco = -1;
    private float giroArco;

    private GameObject cartelManoGo;
    private TextMeshPro txtCartelMano;
    private Vector3 cartelPosicion;
    private bool cartelColocado;

    private void ActualizarAvisoTurno()
    {
        bool hayPartida = partida.IsSpawned && partida.Estado.EnCurso && !enRevision && !taquillaAbierta;
        int turno = hayPartida ? TurnoVisible() : -1;
        ActualizarFoco(turno);
        ActualizarArcoMesa(turno);
        ActualizarCartelMano(hayPartida);
    }

    private void ActualizarFoco(int turno)
    {
        GameObject modelo = turno >= 0 ? ModeloEnSitio(Relativo(turno)) : null;
        bool quiere = focoTurno && modelo != null;

        if (focoLuz == null)
        {
            if (!quiere)
                return;

            GameObject go = new("FocoTurno");
            focoLuz = go.AddComponent<Light>();
            focoLuz.type = LightType.Spot;
            focoLuz.shadows = LightShadows.None;
            focoLuz.intensity = 0f;
        }

        if (modelo != null)
        {
            Vector3 cabeza = CabezaDe(modelo);
            Vector3 haciaMesa = Plano(CentroMesa() - cabeza);
            Vector3 destino = cabeza + Vector3.up * alturaFoco + haciaMesa * (alturaFoco * 0.45f);
            focoPosicion = focoColocado ? Vector3.Lerp(focoPosicion, destino, 1f - Mathf.Exp(-6f * Time.deltaTime)) : destino;
            focoColocado = true;
            Vector3 mirar = Vector3.Lerp(CaderaDe(modelo), cabeza, 0.4f);
            focoLuz.transform.SetPositionAndRotation(focoPosicion, Quaternion.LookRotation(mirar - focoPosicion));
        }

        float quedan = SegundosQueQuedan(out _);
        bool prisa = turno >= 0 && quedan >= 0f && quedan <= 5f;
        // Si eres tu (en primera persona) se baja un poco, para no deslumbrar tus cartas.
        float objetivo = quiere ? (turno == partida.PuestoLocal && PrimeraPersona ? 0.5f : 1f) : 0f;
        focoNivel = Mathf.MoveTowards(focoNivel, objetivo, Time.deltaTime * 2.5f);
        float latido = prisa ? 0.75f + 0.25f * Mathf.Sin(Time.time * 10f) : 1f;

        focoLuz.enabled = focoNivel > 0.001f;
        focoLuz.intensity = intensidadFoco * focoNivel * latido;
        focoLuz.color = prisa ? Color.Lerp(colorFoco, new Color(1f, 0.4f, 0.3f), 0.55f) : colorFoco;
        focoLuz.spotAngle = anguloFoco;
        focoLuz.innerSpotAngle = anguloFoco * 0.55f;
        focoLuz.range = alturaFoco * 2.2f + 1f;
    }

    private float RadioMesa()
    {
        if (radioMesaMedido > 0f)
            return radioMesaMedido;

        radioMesaMedido = 0.75f;
        GameObject mesa = GameObject.Find("MesaRedondaMus");

        if (mesa != null)
        {
            Renderer[] renders = mesa.GetComponentsInChildren<Renderer>();

            if (renders.Length > 0)
            {
                Bounds b = renders[0].bounds;

                foreach (Renderer r in renders)
                    b.Encapsulate(r.bounds);

                radioMesaMedido = Mathf.Clamp(Mathf.Min(b.extents.x, b.extents.z), 0.3f, 2f);
            }
        }

        return radioMesaMedido;
    }

    private void ActualizarArcoMesa(int turno)
    {
        bool ver = arcoTiempoMesa && turno >= 0 && ModeloEnSitio(Relativo(turno)) != null;

        if (!ver)
        {
            if (renderArco != null)
                renderArco.enabled = renderPista.enabled = false;
            turnoArco = -1;
            return;
        }

        if (renderArco == null)
        {
            renderPista = CrearMallaLuz("PistaTiempo", out mallaPista);
            renderArco = CrearMallaLuz("ArcoTiempo", out mallaArco);
        }

        float quedan = SegundosQueQuedan(out float fraccion);
        bool conReloj = quedan >= 0f;
        bool prisa = conReloj && quedan <= 5f;
        float radio = RadioMesa() * radioArco;
        Vector3 centro = CentroMesa() + Vector3.up * 0.004f;

        // El arco gira suave de un jugador al siguiente.
        Vector3 dir = DireccionSilla(Relativo(turno));
        float objetivo = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        giroArco = turnoArco < 0 ? objetivo : Mathf.LerpAngle(giroArco, objetivo, 1f - Mathf.Exp(-8f * Time.deltaTime));
        turnoArco = turno;
        Vector3 haciaJugador = Quaternion.Euler(0f, giroArco, 0f) * Vector3.forward;

        const float medio = 38f;
        float pulso = 0.7f + 0.3f * Mathf.Sin(Time.time * (prisa ? 10f : 3f));
        Color color = conReloj ? ColorReloj(fraccion) : new Color(1f, 0.8f, 0.3f);
        color = new Color(Mathf.Min(1f, color.r * 1.15f), Mathf.Min(1f, color.g * 1.15f), Mathf.Min(1f, color.b * 1.15f), pulso);

        ConstruirArco(mallaPista, centro, haciaJugador, radio, anchoArco * 1.8f, medio, new Color(0.08f, 0.05f, 0.03f, 0.55f));
        ConstruirArco(mallaArco, centro + Vector3.up * 0.002f, haciaJugador, radio, anchoArco * 2f,
            medio * (conReloj ? Mathf.Max(0.03f, fraccion) : 1f), color);
        renderArco.enabled = renderPista.enabled = true;
    }

    private static MeshRenderer CrearMallaLuz(string nombre, out Mesh malla)
    {
        GameObject go = new(nombre, typeof(MeshFilter), typeof(MeshRenderer));
        malla = new Mesh { name = nombre };
        malla.MarkDynamic();
        go.GetComponent<MeshFilter>().sharedMesh = malla;
        MeshRenderer r = go.GetComponent<MeshRenderer>();
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;

        // Sin luces y con transparencia (usa el color de cada vertice). Viene siempre incluido en el juego.
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        r.sharedMaterial = new Material(shader);
        return r;
    }

    // Arco plano sobre la mesa, con los bordes difuminados (parece una tira de luz).
    private static void ConstruirArco(Mesh malla, Vector3 centro, Vector3 hacia, float radio, float ancho, float medioAngulo, Color color)
    {
        const int pasos = 40;
        Vector3[] v = new Vector3[(pasos + 1) * 3];
        Color[] c = new Color[v.Length];
        int[] t = new int[pasos * 12];
        Color transparente = new(color.r, color.g, color.b, 0f);

        for (int i = 0; i <= pasos; i++)
        {
            float u = (float)i / pasos;
            Vector3 d = Quaternion.AngleAxis(Mathf.Lerp(-medioAngulo, medioAngulo, u), Vector3.up) * hacia;
            // Las puntas se apagan poco a poco.
            float punta = Mathf.Clamp01(Mathf.Min(u, 1f - u) * 8f);
            v[i * 3] = centro + d * (radio - ancho * 0.5f);
            v[i * 3 + 1] = centro + d * radio;
            v[i * 3 + 2] = centro + d * (radio + ancho * 0.5f);
            c[i * 3] = transparente;
            c[i * 3 + 1] = new Color(color.r, color.g, color.b, color.a * punta);
            c[i * 3 + 2] = transparente;
        }

        int k = 0;

        for (int i = 0; i < pasos; i++)
        {
            int a = i * 3, b = (i + 1) * 3;

            for (int f = 0; f < 2; f++)
            {
                // Vistos desde arriba, en el sentido de las agujas del reloj (cara de delante hacia arriba).
                t[k++] = a + f; t[k++] = a + f + 1; t[k++] = b + f;
                t[k++] = a + f + 1; t[k++] = b + f + 1; t[k++] = b + f;
            }
        }

        malla.Clear();
        malla.vertices = v;
        malla.colors = c;
        malla.triangles = t;
        malla.RecalculateBounds();
    }

    private void ActualizarCartelMano(bool hayPartida)
    {
        EstadoPublicoMus e = partida.Estado;
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;
        int mano = e.Mano;
        bool ver = cartelMano && hayPartida && cam != null && mano >= 0 && mano < 4 && !manosEnMesa &&
                   e.FaseActual != FaseMus.FinMano && ModeloEnSitio(Relativo(mano)) != null;

        if (!ver)
        {
            if (cartelManoGo != null)
                cartelManoGo.SetActive(false);
            cartelColocado = false;
            return;
        }

        if (cartelManoGo == null)
            CrearCartelMano();

        cartelManoGo.SetActive(true);

        // Delante del que es mano, un poco hacia su derecha para no tapar el centro de la mesa.
        Vector3 dir = Quaternion.Euler(0f, 28f, 0f) * DireccionSilla(Relativo(mano));
        Vector3 destino = CentroMesa() + dir * (RadioMesa() * 0.6f);
        cartelPosicion = cartelColocado ? Vector3.Lerp(cartelPosicion, destino, 1f - Mathf.Exp(-5f * Time.deltaTime)) : destino;
        cartelColocado = true;

        // Siempre mirando hacia ti, de pie sobre la mesa.
        Vector3 haciaFuera = Plano(cartelPosicion - cam.transform.position);
        cartelManoGo.transform.SetPositionAndRotation(cartelPosicion, Quaternion.LookRotation(haciaFuera, Vector3.up));
    }

    private void CrearCartelMano()
    {
        cartelManoGo = new GameObject("CartelMano");

        GameObject tabla = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tabla.name = "Tabla";
        Destroy(tabla.GetComponent<Collider>());
        tabla.transform.SetParent(cartelManoGo.transform, false);
        tabla.transform.localPosition = new Vector3(0f, 0.032f, 0.004f);
        tabla.transform.localScale = new Vector3(0.15f, 0.06f, 0.008f);
        MeshRenderer r = tabla.GetComponent<MeshRenderer>();
        r.shadowCastingMode = ShadowCastingMode.Off;
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");

        if (lit != null)
        {
            Material m = new(lit);
            Color madera = new(0.2f, 0.11f, 0.05f);
            m.color = madera;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", madera);
            r.sharedMaterial = m;
        }

        GameObject texto = new("Texto", typeof(RectTransform));
        texto.transform.SetParent(cartelManoGo.transform, false);
        texto.transform.localPosition = new Vector3(0f, 0.032f, -0.0005f);
        txtCartelMano = texto.AddComponent<TextMeshPro>();
        txtCartelMano.rectTransform.sizeDelta = new Vector2(0.14f, 0.05f);

        if (fuenteTitulos != null)
            txtCartelMano.font = fuenteTitulos;

        txtCartelMano.text = "MANO";
        txtCartelMano.alignment = TextAlignmentOptions.Center;
        txtCartelMano.enableAutoSizing = true;
        txtCartelMano.fontSizeMin = 0.05f;
        txtCartelMano.fontSizeMax = 5f;
        txtCartelMano.enableWordWrapping = false;
        txtCartelMano.color = Oro;
    }
}
