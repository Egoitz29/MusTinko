using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// VISION DE VERDAD
//  - Mirada libre: arrastras para mirar (sin los 4 saltos fijos) y al soltar se "imanta" al jugador mas cercano.
//    Tambien puedes TOCAR a un jugador para mirarle. Teclas 1-4 como antes (izquierda, frente, derecha, cartas).
//  - Vision periferica: lo que miras se ve nitido; lo demas, borroso y algo oscuro (notas que se mueven, pero
//    para saber que sena hacen tienes que mirarles).
//  - Mirada fija: si sigues mirando a alguien, la camara se acerca un poco a su cara.
//  - Un ojito encima de quien te esta mirando (verde tu companero, rojo un rival).
//  - Ojos de los personajes mas pequenos (ajustable).
public partial class VistaMesaOnlineMus
{
    [Header("Vision: mirada libre")]
    [SerializeField] private bool miradaLibre = true;
    [Tooltip("Grados que gira la vista al arrastrar todo el ancho de la pantalla.")]
    [SerializeField] private float gradosPorPantalla = 120f;
    [SerializeField] private float giroMaximoMirada = 75f;
    [Tooltip("Al soltar, si un jugador esta a menos de estos grados, la vista se va hacia el.")]
    [SerializeField] private float gradosIman = 22f;
    [SerializeField] private float inclinacionCartasMirada = 55f;
    [SerializeField] private float suavidadMirada = 10f;

    [Header("Vision: mirada fija (zoom)")]
    [Tooltip("Solo cuando eliges a quien mirar (tocandole, arrastrando o con 1-2-3), no en la vista normal.")]
    [UnityEngine.Range(0.5f, 1f)][SerializeField] private float zoomMiradaFija = 0.8f;
    [SerializeField] private float segundosParaZoom = 0.7f;

    [Header("Vision periferica (borroso alrededor)")]
    [Tooltip("Desenfoca lo que no miras. De momento apagado (no convencio); se puede probar marcandolo.")]
    [SerializeField] private bool usarVisionPeriferica = false;
    [SerializeField] private bool desenfoqueEnMovil = true;
    [UnityEngine.Range(1f, 16f)][SerializeField] private float aperturaDesenfoque = 2.2f;
    [UnityEngine.Range(20f, 150f)][SerializeField] private float focalDesenfoque = 70f;
    [UnityEngine.Range(0f, 0.6f)][SerializeField] private float oscurecerBordes = 0.38f;

    [Header("Ojos de los personajes")]
    [UnityEngine.Range(0.5f, 1f)][SerializeField] private float tamanoOjos = 0.8f;

    // Estado de la mirada
    private bool miradaIniciada;
    private float yawBase, pitchBase, fovBase;
    private float yawActual, pitchActual, yawObjetivo, pitchObjetivo;
    private bool arrastrandoMirada, posibleToque;
    private Vector2 inicioToque, ultimoPuntero;
    private float momentoToque;
    private int sitioMirado = -1;           // a quien tienes en el centro (silla 0-3, -1 nadie, 4 las cartas)
    private float desdeCuandoMiras;
    private bool miradaElegida;             // has elegido tu a quien mirar (si no, no hay zoom)
    private readonly List<RaycastResult> resultadosUIMirada = new();

    // Vision periferica
    private Volume volumenVision;
    private DepthOfField desenfoque;
    private Vignette vineta;
    private float distanciaFoco = 2.5f;

    // Ojitos de "te esta mirando"
    private readonly Image[] ojitos = new Image[4];

    // ---------- Mirada libre (se llama en LateUpdate) ----------

    private bool MiradaLibreActiva => miradaLibre && !DemoActiva && !taquillaAbierta && !enRevision;

    private void DirigirMiradaLibre()
    {
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;

        if (cam == null)
            return;

        if (controlCamaraJugador != null && controlCamaraJugador.enabled)
            controlCamaraJugador.enabled = false;

        if (!miradaIniciada)
        {
            miradaIniciada = true;
            Vector3 e = cam.transform.rotation.eulerAngles;
            yawBase = yawActual = yawObjetivo = e.y;
            pitchBase = pitchActual = pitchObjetivo = Mathf.DeltaAngle(0f, e.x);
            fovBase = cam.fieldOfView;
        }

        LeerEntradaMirada(cam);

        float k = 1f - Mathf.Exp(-Time.deltaTime * (arrastrandoMirada ? suavidadMirada * 2.5f : suavidadMirada));
        yawActual = Mathf.LerpAngle(yawActual, yawObjetivo, k);
        pitchActual = Mathf.Lerp(pitchActual, pitchObjetivo, k);
        cam.transform.rotation = Quaternion.Euler(pitchActual, yawActual, 0f);

        // Mirada fija: si llevas un rato mirando a alguien, la camara se acerca un poco.
        int mirado = QuienEstaEnElCentro(cam);

        if (mirado != sitioMirado)
        {
            sitioMirado = mirado;
            desdeCuandoMiras = Time.time;
        }

        bool fija = miradaElegida && !arrastrandoMirada && sitioMirado >= 0 && sitioMirado <= 3 && Time.time - desdeCuandoMiras > segundosParaZoom;
        float fovObjetivo = fija ? fovBase * zoomMiradaFija : fovBase;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fovObjetivo, 1f - Mathf.Exp(-Time.deltaTime * 3f));
    }

    private void SoltarMiradaLibre()
    {
        if (!miradaIniciada)
            return;

        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;

        if (cam != null)
            cam.fieldOfView = fovBase;

        sitioMirado = -1;
    }

    private void LeerEntradaMirada(Camera cam)
    {
        Keyboard teclado = Keyboard.current;

        if (teclado != null)
        {
            // Izquierda, enfrente y derecha de TU silla.
            if (teclado.digit1Key.wasPressedThisFrame) MirarSitio(cam, (SillaCamara + 3) % 4);
            if (teclado.digit2Key.wasPressedThisFrame) MirarSitio(cam, (SillaCamara + 2) % 4);
            if (teclado.digit3Key.wasPressedThisFrame) MirarSitio(cam, (SillaCamara + 1) % 4);
            if (teclado.digit4Key.wasPressedThisFrame) MirarCartasMirada();
        }

        bool pulsado = false, empieza = false, suelta = false, tactil = false;
        Vector2 puntero = Vector2.zero;

        if (Touchscreen.current != null && (Touchscreen.current.primaryTouch.press.isPressed || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
        {
            var dedo = Touchscreen.current.primaryTouch;
            puntero = dedo.position.ReadValue();
            pulsado = dedo.press.isPressed;
            empieza = dedo.press.wasPressedThisFrame;
            suelta = dedo.press.wasReleasedThisFrame;
            tactil = true;
        }
        else if (Mouse.current != null)
        {
            var raton = Mouse.current;
            puntero = raton.position.ReadValue();
            pulsado = raton.leftButton.isPressed;
            empieza = raton.leftButton.wasPressedThisFrame;
            suelta = raton.leftButton.wasReleasedThisFrame;
        }

        if (empieza)
        {
            posibleToque = true;
            arrastrandoMirada = !SobreLaInterfaz(puntero);

            if (arrastrandoMirada)
                miradaElegida = false;
            inicioToque = ultimoPuntero = puntero;
            momentoToque = Time.time;

            if (!arrastrandoMirada)
                posibleToque = false;
        }

        // En el movil arrastrar libre era impreciso: el dedo hace GESTOS (deslizar a un lado = mirar a ese
        // jugador, hacia abajo = tus cartas, hacia arriba = de frente) y la vista salta sola, sin girar a mano.
        if (tactil && arrastrandoMirada)
        {
            if (pulsado && (puntero - inicioToque).magnitude > Screen.height * 0.02f)
                posibleToque = false;

            if (suelta)
            {
                arrastrandoMirada = false;
                Vector2 gesto = puntero - inicioToque;

                if (posibleToque && Time.time - momentoToque < 0.35f)
                    TocarParaMirar(cam, puntero);
                else if (gesto.magnitude > Screen.height * 0.06f)
                    GestoMirada(gesto);
            }

            return;
        }

        if (arrastrandoMirada && pulsado)
        {
            Vector2 delta = puntero - ultimoPuntero;
            ultimoPuntero = puntero;
            float gradosPorPixel = gradosPorPantalla / Mathf.Max(1f, Screen.width);
            yawObjetivo += delta.x * gradosPorPixel;
            pitchObjetivo -= delta.y * gradosPorPixel;
            yawObjetivo = yawBase + Mathf.Clamp(Mathf.DeltaAngle(yawBase, yawObjetivo), -giroMaximoMirada, giroMaximoMirada);
            pitchObjetivo = Mathf.Clamp(pitchObjetivo, pitchBase - 20f, pitchBase + inclinacionCartasMirada + 12f);

            if ((puntero - inicioToque).magnitude > Screen.height * 0.02f)
                posibleToque = false;
        }

        if (suelta && arrastrandoMirada)
        {
            arrastrandoMirada = false;

            if (posibleToque && Time.time - momentoToque < 0.35f)
                TocarParaMirar(cam, puntero);
            else
                Imantar(cam);
        }
    }

    private bool SobreLaInterfaz(Vector2 posicion)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData datos = new(EventSystem.current) { position = posicion };
        resultadosUIMirada.Clear();
        EventSystem.current.RaycastAll(datos, resultadosUIMirada);
        return resultadosUIMirada.Count > 0;
    }

    // Toque corto encima de un jugador: le miras.
    private void TocarParaMirar(Camera cam, Vector2 puntero)
    {
        int mejor = -1;
        float mejorDistancia = Screen.height * 0.16f;

        for (int sitio = 0; sitio < 4; sitio++)
        {
            GameObject m = sitio == SillaCamara ? null : ModeloEnSitio(sitio);

            if (m == null)
                continue;

            Vector3 p = cam.WorldToScreenPoint(CabezaDe(m));

            if (p.z <= 0f)
                continue;

            float d = (new Vector2(p.x, p.y) - puntero).magnitude;

            if (d < mejorDistancia)
            {
                mejorDistancia = d;
                mejor = sitio;
            }
        }

        if (mejor >= 0)
            MirarSitio(cam, mejor);
    }

    // ---------- Mando de mirada (movil): izquierda, frente, derecha y tus cartas ----------

    private const int MirarIzquierda = 0, MirarFrente = 1, MirarDerecha = 2, MirarCartas = 3;
    private GameObject mandoMirada;
    private readonly Image[] fondosMando = new Image[4];
    private int posicionMirada = MirarFrente;

    private void CrearMandoMirada()
    {
        mandoMirada = new GameObject("MandoMirada", typeof(RectTransform));
        RectTransform r = Rect(mandoMirada, raizUI, new Vector2(1f, 0.5f), new Vector2(-24f, 30f), new Vector2(310, 310));
        Imagen("Fondo", r, "circulo", 310, new Color(0.1f, 0.06f, 0.03f, 0.45f));

        Vector2[] sitios = { new(-100, 0), new(0, 100), new(100, 0), new(0, -100) };
        float[] giros = { 90f, 0f, -90f, 180f };
        string[] textos = { "IZQ", "FRENTE", "DCHA", "CARTAS" };

        for (int i = 0; i < 4; i++)
        {
            int indice = i;
            GameObject b = new("Mirar_" + textos[i], typeof(RectTransform), typeof(Image), typeof(Button));
            Rect(b, r, new Vector2(0.5f, 0.5f), sitios[i], new Vector2(108, 108));
            Image fondo = b.GetComponent<Image>();
            fondo.sprite = SpriteUI("circulo", 0);
            b.GetComponent<Button>().onClick.AddListener(() => MirarPosicion(indice));
            fondosMando[i] = fondo;

            Image flecha = Imagen("Flecha", b.transform, "flecha", 46, Color.white);
            flecha.rectTransform.anchoredPosition = new Vector2(0f, 14f);
            flecha.rectTransform.localEulerAngles = new Vector3(0f, 0f, giros[i]);

            GameObject etiqueta = new("Texto", typeof(RectTransform));
            Rect(etiqueta, b.transform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(100, 34));
            Texto(etiqueta.transform, textos[i], 20, Crema, false, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
        }

        mandoMirada.SetActive(false);
    }

    private void ActualizarMandoMirada(bool enJuego)
    {
        if (mandoMirada == null)
            return;

        bool tapado = taquillaAbierta || (panelTanteoUI != null && panelTanteoUI.activeSelf);
        bool ver = enJuego && MiradaLibreActiva && !tapado && SillaCamara >= 0;
        mandoMirada.SetActive(ver);

        if (!ver)
            return;

        // Se ilumina a donde estas mirando de verdad.
        int actual = sitioMirado == 4 ? MirarCartas : PosicionDeSitio(sitioMirado);

        for (int i = 0; i < 4; i++)
            fondosMando[i].color = i == actual ? new Color(0.89f, 0.66f, 0.2f, 0.95f) : new Color(0.24f, 0.15f, 0.09f, 0.85f);
    }

    private int SitioDePosicion(int posicion) => posicion switch
    {
        MirarIzquierda => (SillaCamara + 3) % 4,
        MirarFrente => (SillaCamara + 2) % 4,
        MirarDerecha => (SillaCamara + 1) % 4,
        _ => -1
    };

    private int PosicionDeSitio(int sitio)
    {
        for (int p = MirarIzquierda; p <= MirarDerecha; p++)
            if (sitio >= 0 && SitioDePosicion(p) == sitio)
                return p;

        return -1;
    }

    private void MirarPosicion(int posicion)
    {
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;

        if (cam == null)
            return;

        posicionMirada = posicion;

        if (posicion == MirarCartas)
            MirarCartasMirada();
        else
            MirarSitio(cam, SitioDePosicion(posicion));
    }

    // Deslizar: a los lados pasa al jugador de al lado; abajo, tus cartas; arriba, de frente.
    private void GestoMirada(Vector2 gesto)
    {
        if (Mathf.Abs(gesto.y) > Mathf.Abs(gesto.x))
        {
            MirarPosicion(gesto.y < 0f ? MirarCartas : MirarFrente);
            return;
        }

        int derecha = gesto.x > 0f ? 1 : -1;

        if (posicionMirada == MirarCartas)
            MirarPosicion(derecha > 0 ? MirarDerecha : MirarIzquierda);
        else
            MirarPosicion(Mathf.Clamp(posicionMirada + derecha, MirarIzquierda, MirarDerecha));
    }

    // Al soltar: si hay alguien cerca del centro, la vista se va hacia el; si miras muy abajo, a las cartas.
    private void Imantar(Camera cam)
    {
        if (pitchObjetivo > pitchBase + inclinacionCartasMirada * 0.5f)
        {
            MirarCartasMirada();
            return;
        }

        Vector3 adelante = Quaternion.Euler(pitchObjetivo, yawObjetivo, 0f) * Vector3.forward;
        int mejor = -1;
        float mejorAngulo = gradosIman;

        for (int sitio = 0; sitio < 4; sitio++)
        {
            GameObject m = sitio == SillaCamara ? null : ModeloEnSitio(sitio);

            if (m == null)
                continue;

            float a = Vector3.Angle(adelante, CabezaDe(m) - cam.transform.position);

            if (a < mejorAngulo)
            {
                mejorAngulo = a;
                mejor = sitio;
            }
        }

        if (mejor >= 0)
            MirarSitio(cam, mejor);
        else
            pitchObjetivo = Mathf.Clamp(pitchObjetivo, pitchBase - 20f, pitchBase + 25f);
    }

    private void MirarSitio(Camera cam, int sitio)
    {
        GameObject m = ModeloEnSitio(sitio);

        if (m == null)
            return;

        // A la cara, un pelin por debajo del centro de la cabeza (ahi estan los ojos y la boca).
        float alto = Mathf.Max(0.2f, CabezaDe(m).y - CuerpoDe(m).y);
        Vector3 dir = CabezaDe(m) - Vector3.up * (alto * 0.08f) - cam.transform.position;
        Quaternion q = Quaternion.LookRotation(dir, Vector3.up);
        Vector3 e = q.eulerAngles;
        int posicion = PosicionDeSitio(sitio);
        if (posicion >= 0) posicionMirada = posicion;
        yawObjetivo = yawBase + Mathf.Clamp(Mathf.DeltaAngle(yawBase, e.y), -giroMaximoMirada, giroMaximoMirada);
        pitchObjetivo = Mathf.DeltaAngle(0f, e.x);
        miradaElegida = true;
    }

    private void RecentrarMirada()
    {
        yawObjetivo = yawBase;
        pitchObjetivo = pitchBase;
        miradaElegida = false;
        posicionMirada = MirarFrente;
    }

    private void MirarCartasMirada()
    {
        posicionMirada = MirarCartas;
        miradaElegida = false;
        yawObjetivo = yawBase;
        pitchObjetivo = pitchBase + inclinacionCartasMirada;

        // Si estan tus cartas 3D, se mira justo a ellas.
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;

        if (manoJugador != null && cam != null)
        {
            Vector3 e = Quaternion.LookRotation(manoJugador.position - cam.transform.position, Vector3.up).eulerAngles;
            yawObjetivo = e.y;
            pitchObjetivo = Mathf.DeltaAngle(0f, e.x);
        }
    }

    // Quien queda en el centro de la vista (1..3 sitios de los demas, 4 = mirando tus cartas, -1 nadie).
    private int QuienEstaEnElCentro(Camera cam)
    {
        if (pitchActual > pitchBase + inclinacionCartasMirada * 0.5f)
            return 4;

        int mejor = -1;
        float mejorAngulo = anguloMirada;

        for (int sitio = 0; sitio < 4; sitio++)
        {
            GameObject m = sitio == SillaCamara ? null : ModeloEnSitio(sitio);

            if (m == null)
                continue;

            float a = Vector3.Angle(cam.transform.forward, CabezaDe(m) - cam.transform.position);

            if (a < mejorAngulo)
            {
                mejorAngulo = a;
                mejor = sitio;
            }
        }

        return mejor;
    }

    // ---------- Vision periferica (borroso alrededor de lo que miras) ----------

    private void ActualizarVisionPeriferica(bool enJuego)
    {
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;
        bool activa = usarVisionPeriferica && (desenfoqueEnMovil || !Application.isMobilePlatform) &&
                      enJuego && !enRevision && !taquillaAbierta && !DemoActiva && miradaLibre && cam != null;

        if (volumenVision == null)
        {
            if (!activa)
                return;

            CrearVolumenVision(cam);
        }

        float pesoObjetivo = activa ? 1f : 0f;
        volumenVision.weight = Mathf.MoveTowards(volumenVision.weight, pesoObjetivo, Time.deltaTime * 2.5f);

        if (!activa)
            return;

        // Se enfoca a quien miras; si no miras a nadie, al centro de la mesa; si miras tus cartas, de cerca.
        float distancia;

        if (sitioMirado >= 0 && sitioMirado <= 3 && ModeloEnSitio(sitioMirado) != null)
            distancia = Vector3.Distance(cam.transform.position, CabezaDe(ModeloEnSitio(sitioMirado)));
        else if (sitioMirado == 4)
            distancia = 0.6f;
        else
            distancia = Vector3.Distance(cam.transform.position, CentroMesa());

        distanciaFoco = Mathf.Lerp(distanciaFoco, distancia, 1f - Mathf.Exp(-Time.deltaTime * 6f));
        desenfoque.focusDistance.value = distanciaFoco;
        desenfoque.aperture.value = aperturaDesenfoque;
        desenfoque.focalLength.value = focalDesenfoque;
        vineta.intensity.value = oscurecerBordes;
    }

    private void CrearVolumenVision(Camera cam)
    {
        UniversalAdditionalCameraData datos = cam.GetUniversalAdditionalCameraData();

        if (datos != null)
            datos.renderPostProcessing = true;

        VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        desenfoque = perfil.Add<DepthOfField>(true);
        desenfoque.mode.value = DepthOfFieldMode.Bokeh;
        desenfoque.focusDistance.value = distanciaFoco;
        desenfoque.aperture.value = aperturaDesenfoque;
        desenfoque.focalLength.value = focalDesenfoque;

        vineta = perfil.Add<Vignette>(true);
        vineta.intensity.value = oscurecerBordes;
        vineta.smoothness.value = 0.5f;
        vineta.color.value = Color.black;

        GameObject go = new("VisionPeriferica");
        volumenVision = go.AddComponent<Volume>();
        volumenVision.isGlobal = true;
        volumenVision.priority = 50;
        volumenVision.sharedProfile = perfil;
        volumenVision.weight = 0f;
    }

    // ---------- Ojitos: quien te esta mirando ----------

    private void ActualizarOjitos(bool enJuego)
    {
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;
        int local = partida.PuestoLocal;
        bool mostrar = enJuego && !enRevision && partida.SenasConMirada && local >= 0 && cam != null;

        for (int sitio = 0; sitio < 4; sitio++)
        {
            if (sitio == SillaCamara)
                continue;

            if (ojitos[sitio] == null)
            {
                if (!mostrar)
                    continue;

                GameObject go = new("Ojito_" + sitio, typeof(RectTransform), typeof(Image));
                Rect(go, raizUI, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 48)).pivot = new Vector2(0.5f, 0.5f);
                ojitos[sitio] = go.GetComponent<Image>();
                ojitos[sitio].sprite = SpriteUI("ojo", 0);
                ojitos[sitio].raycastTarget = false;
                ojitos[sitio].preserveAspect = true;
            }

            Image ojo = ojitos[sitio];
            int puesto = sitio;
            GameObject m = ModeloEnSitio(sitio);
            bool teMira = mostrar && m != null && partida.MiradaDe(puesto) == local;

            if (!teMira)
            {
                ojo.gameObject.SetActive(false);
                continue;
            }

            // Al lado de la cabeza, un poco por encima (para no tapar los bocadillos).
            float alto = Mathf.Max(0.2f, CabezaDe(m).y - CuerpoDe(m).y);
            Vector3 punto = CabezaDe(m) + Vector3.up * (alto * 0.42f) + cam.transform.right * (alto * 0.42f);
            Vector3 pantalla = cam.WorldToScreenPoint(punto);

            if (pantalla.z <= 0f)
            {
                ojo.gameObject.SetActive(false);
                continue;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(raizUI, pantalla, null, out Vector2 pos);
            ojo.gameObject.SetActive(true);
            ojo.rectTransform.anchoredPosition = pos;
            bool companero = puesto == (local + 2) % 4;
            ojo.color = companero ? new Color(0.45f, 0.95f, 0.5f) : new Color(1f, 0.42f, 0.36f);
            ojo.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.time * 6f));
        }
    }

    // ---------- Ojos mas pequenos ----------

    private void ActualizarTamanoOjos()
    {
        PrepararGestos();

        for (int sitio = 0; sitio < 4; sitio++)
        {
            if (gestos[sitio] != null)
                gestos[sitio].EscalaOjos = tamanoOjos;
        }
    }
}
