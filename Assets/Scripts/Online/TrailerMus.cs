using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// TRAILER DE MUS TINKO (unos 50 segundos, listo para grabar y mandar a tus amigos)
//
//   1. Titulo: "Mus TINKO" con la camara girando sobre la taberna.
//   2. Los cuatro personajes en primer plano, con su nombre.
//   3. Una partida de verdad (4 bots en modo demo): camara de cine y vista de jugador, con frases encima.
//   4. Cierre con el logo.
//
// Como lanzarlo:
//   - Menu de Unity "Mus TINKO > Trailer > Grabar trailer en MP4" (lo graba solo con Unity Recorder).
//   - Menu "Mus TINKO > Trailer > Ver el trailer (sin grabar)", o la tecla F9 en el juego
//     (por ejemplo para grabarlo con Win+Alt+R).
// Los textos se pueden cambiar en el Inspector del objeto "TrailerMus" mientras se juega.
[DefaultExecutionOrder(10000)]   // despues de VistaMesaOnlineMus: en los planos de cine manda en la camara
public class TrailerMus : MonoBehaviour
{
    public const string ClavePedido = "MusTinko_Trailer";   // 1 = verlo, 2 = verlo y salir de Play al acabar (grabando)

    [Header("Textos")]
    [SerializeField] private string titulo = "Mus TINKO";
    [SerializeField] private string subtitulo = "El mus de toda la vida, ahora en tu móvil";
    [SerializeField] private string[] frasesPartida =
    {
        "Juega con tus amigos... o contra bots que han jugado millones de partidas",
        "Pásale señas a tu compañero sin que te pillen",
        "Envida lo que quieras... ¡o echa un órdago!",
        "Crea tu mesa por Internet y que entren tus amigos"
    };
    [SerializeField] private string[] lemasPersonajes = { "La lista de la mesa", "No pasa ni una", "Siempre tranquila", "El rey del órdago" };   // LILA, MOSTAZA, MENTA, CORAL
    [SerializeField] private string cierre = "Muy pronto en tu móvil";

    [Header("Ritmo (segundos)")]
    [SerializeField] private float segundosTitulo = 6f;
    [SerializeField] private float segundosPorPersonaje = 2.4f;
    [SerializeField] private float segundosPartida = 32f;
    [SerializeField] private float segundosCierre = 5f;

    private enum Plano { Ninguno, Orbita, Cara, Juego, Alto }

    private VistaMesaOnlineMus vista;
    private bool enCurso;
    private bool salirAlAcabar;
    private Plano plano = Plano.Ninguno;
    private int puestoPlano;
    private float inicioPlano;
    private float anguloOrbita;

    private Canvas capa;
    private CanvasGroup negro, grupoTitulo, grupoFrase, grupoNombre;
    private TextMeshProUGUI txtTitulo, txtSubtitulo, txtFrase, txtNombre, txtLema;

    public bool EnCurso => enCurso;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Preparar()
    {
        if (FindFirstObjectByType<TrailerMus>() == null)
            new GameObject("TrailerMus").AddComponent<TrailerMus>();
    }

    private void Start()
    {
        vista = FindFirstObjectByType<VistaMesaOnlineMus>();

        if (vista == null)
        {
            Destroy(gameObject);   // otra escena: aqui no hay mesa
            return;
        }

        int pedido = 0;

        try
        {
            pedido = PlayerPrefs.GetInt(ClavePedido, 0);

            if (pedido != 0)
            {
                PlayerPrefs.DeleteKey(ClavePedido);
                PlayerPrefs.Save();
            }
        }
        catch
        {
        }

        if (pedido != 0)
            Rodar(pedido == 2);
    }

    private void Update()
    {
        Keyboard teclado = Keyboard.current;

        if (teclado != null && teclado.f9Key.wasPressedThisFrame && !enCurso)
            Rodar(false);
    }

    public void Rodar(bool salirDePlayAlAcabar)
    {
        if (enCurso || vista == null)
            return;

        salirAlAcabar = salirDePlayAlAcabar;
        StartCoroutine(Guion());
    }

    // ------------------------------------------------------------------ guion

    private IEnumerator Guion()
    {
        enCurso = true;
        CrearCapa();
        vista.ModoTrailer = true;
        negro.alpha = 1f;
        Mostrar(grupoTitulo, 0f);
        Mostrar(grupoFrase, 0f);
        Mostrar(grupoNombre, 0f);

        // Los bots empiezan a jugar ya: cuando llegue la partida, la mesa estara en marcha.
        vista.EmpezarDemoParaTrailer();
        yield return new WaitForSeconds(0.6f);

        // 1. Titulo
        CambiarPlano(Plano.Orbita, 0);
        txtTitulo.text = titulo;
        txtSubtitulo.text = subtitulo;
        StartCoroutine(Fundir(negro, 0f, 1.4f));
        yield return new WaitForSeconds(0.8f);
        StartCoroutine(Fundir(grupoTitulo, 1f, 0.9f));
        yield return new WaitForSeconds(Mathf.Max(1f, segundosTitulo - 2.2f));
        StartCoroutine(Fundir(grupoTitulo, 0f, 0.6f));
        yield return new WaitForSeconds(0.6f);

        // 2. Personajes
        for (int p = 0; p < 4; p++)
        {
            CambiarPlano(Plano.Cara, p);
            Color color = ColoresJugadoresMus.ColorDe(p);
            txtNombre.text = ColoresJugadoresMus.NombresPorPuesto[p];
            txtNombre.color = color;
            int personaje = ColoresJugadoresMus.PersonajeDe(p);
            txtLema.text = personaje < lemasPersonajes.Length ? lemasPersonajes[personaje] : "";
            StartCoroutine(Fundir(grupoNombre, 1f, 0.3f));
            yield return new WaitForSeconds(Mathf.Max(0.8f, segundosPorPersonaje - 0.3f));
            StartCoroutine(Fundir(grupoNombre, 0f, 0.25f));
            yield return new WaitForSeconds(0.3f);
        }

        // 3. Partida (la camara la lleva la demo: de cine o desde la silla de LILA)
        CambiarPlano(Plano.Juego, 0);
        int n = Mathf.Max(1, frasesPartida.Length);
        float porFrase = segundosPartida / n;

        for (int i = 0; i < n; i++)
        {
            vista.VistaDemoJugador(i % 2 == 1);
            txtFrase.text = i < frasesPartida.Length ? frasesPartida[i] : "";
            StartCoroutine(Fundir(grupoFrase, 1f, 0.5f));
            yield return new WaitForSeconds(Mathf.Max(1f, porFrase - 0.9f));
            StartCoroutine(Fundir(grupoFrase, 0f, 0.4f));
            yield return new WaitForSeconds(0.45f);
        }

        // 4. Cierre
        CambiarPlano(Plano.Alto, 0);
        StartCoroutine(Fundir(negro, 0.55f, 0.8f));
        txtTitulo.text = titulo;
        txtSubtitulo.text = cierre;
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(Fundir(grupoTitulo, 1f, 0.8f));
        yield return new WaitForSeconds(Mathf.Max(1f, segundosCierre - 1.5f));
        StartCoroutine(Fundir(negro, 1f, 1f));
        yield return new WaitForSeconds(1.1f);

        Terminar();
    }

    private void Terminar()
    {
        plano = Plano.Ninguno;
        vista.ModoTrailer = false;
        enCurso = false;

        if (capa != null)
            Destroy(capa.gameObject);

#if UNITY_EDITOR
        if (salirAlAcabar)
        {
            UnityEditor.EditorApplication.ExitPlaymode();
            return;
        }
#endif
        vista.SalirDemoDelTrailer();
    }

    // ------------------------------------------------------------------ camara

    private void CambiarPlano(Plano nuevo, int puesto)
    {
        plano = nuevo;
        puestoPlano = puesto;
        inicioPlano = Time.time;
    }

    private void LateUpdate()
    {
        if (!enCurso || plano == Plano.Ninguno || plano == Plano.Juego || vista == null)
            return;

        Camera cam = vista.CamaraDelJuego;

        if (cam == null)
            return;

        Vector3 centro = vista.CentroDeLaMesa;
        float t = Time.time - inicioPlano;
        Vector3 pos;
        Vector3 mirar;

        switch (plano)
        {
            case Plano.Orbita:
            {
                // Giro lento alrededor de la mesa, alto, bajando poco a poco.
                anguloOrbita += 9f * Time.deltaTime;
                Vector3 dir = Quaternion.Euler(0f, anguloOrbita + 200f, 0f) * Vector3.forward;
                pos = centro + dir * 2.7f + Vector3.up * Mathf.Lerp(1.9f, 1.2f, Mathf.Clamp01(t / segundosTitulo));
                mirar = centro + Vector3.up * 0.25f;
                break;
            }
            case Plano.Cara:
            {
                // Primer plano desde la mesa hacia la cara, acercandose un poco.
                Vector3 cabeza = vista.CabezaDelPuesto(puestoPlano);
                Vector3 haciaMesa = centro - cabeza;
                haciaMesa.y = 0f;
                haciaMesa = haciaMesa.sqrMagnitude > 0.0001f ? haciaMesa.normalized : Vector3.forward;
                Vector3 lado = Vector3.Cross(Vector3.up, haciaMesa);
                float acercar = Mathf.Lerp(1.15f, 0.85f, Mathf.Clamp01(t / segundosPorPersonaje));
                pos = cabeza + haciaMesa * acercar + lado * 0.18f + Vector3.up * 0.06f;
                mirar = cabeza + Vector3.up * 0.04f;
                break;
            }
            default:
            {
                // Desde arriba, alejandose: se ve la taberna entera.
                anguloOrbita += 6f * Time.deltaTime;
                Vector3 dir = Quaternion.Euler(0f, anguloOrbita, 0f) * Vector3.forward;
                float alejar = Mathf.Lerp(2.2f, 3.4f, Mathf.Clamp01(t / segundosCierre));
                pos = centro + dir * alejar + Vector3.up * (1.6f + alejar * 0.35f);
                mirar = centro;
                break;
            }
        }

        cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mirar - pos, Vector3.up));
    }

    // ------------------------------------------------------------------ capa de textos

    private void CrearCapa()
    {
        GameObject go = new("CapaTrailer", typeof(Canvas), typeof(CanvasScaler));
        capa = go.GetComponent<Canvas>();
        capa.renderMode = RenderMode.ScreenSpaceOverlay;
        capa.sortingOrder = 300;
        CanvasScaler escala = go.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;
        RectTransform raiz = go.GetComponent<RectTransform>();

        TMP_FontAsset pirata = null;
        Font fuente = Resources.Load<Font>("UI_Tinko/Fuentes/PirataOne-Regular");

        if (fuente != null)
        {
            try
            {
                pirata = TMP_FontAsset.CreateFontAsset(fuente);
            }
            catch
            {
            }
        }

        // Bandas de cine arriba y abajo.
        Bloque("BandaArriba", raiz, new Vector2(0f, 1f), new Vector2(1f, 1f), 0f, 96f, Color.black);
        Bloque("BandaAbajo", raiz, new Vector2(0f, 0f), new Vector2(1f, 0f), 0f, 96f, Color.black);

        // Titulo
        grupoTitulo = Grupo("Titulo", raiz);
        txtTitulo = Letras(grupoTitulo.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(1600, 260), 210, new Color(1f, 0.8f, 0.3f), pirata);
        txtSubtitulo = Letras(grupoTitulo.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(1600, 90), 52, new Color(1f, 0.95f, 0.84f), null);

        // Frase de la partida, abajo con un degradado oscuro detras.
        grupoFrase = Grupo("Frase", raiz);
        Image fondo = Bloque("Degradado", grupoFrase.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), 0f, 330f, Color.white);
        Texture2D degradado = Resources.Load<Texture2D>("UI_Tinko/degradado_inferior");
        if (degradado != null)
            fondo.sprite = Sprite.Create(degradado, new Rect(0, 0, degradado.width, degradado.height), new Vector2(0.5f, 0.5f));
        else
            fondo.color = new Color(0f, 0f, 0f, 0.55f);
        txtFrase = Letras(grupoFrase.transform, new Vector2(0.5f, 0f), new Vector2(0, 140), new Vector2(1600, 140), 58, new Color(1f, 0.95f, 0.84f), null);

        // Nombre del personaje, abajo a la izquierda.
        grupoNombre = Grupo("Nombre", raiz);
        txtNombre = Letras(grupoNombre.transform, new Vector2(0f, 0f), new Vector2(120, 210), new Vector2(1000, 180), 150, Color.white, pirata);
        txtNombre.alignment = TextAlignmentOptions.BottomLeft;
        txtLema = Letras(grupoNombre.transform, new Vector2(0f, 0f), new Vector2(126, 150), new Vector2(1000, 60), 40, new Color(1f, 0.95f, 0.84f), null);
        txtLema.alignment = TextAlignmentOptions.TopLeft;

        // Fundido a negro (encima de todo).
        negro = Grupo("Negro", raiz);
        Image img = negro.gameObject.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
    }

    private static CanvasGroup Grupo(string nombre, Transform padre)
    {
        GameObject go = new(nombre, typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        CanvasGroup g = go.GetComponent<CanvasGroup>();
        g.blocksRaycasts = false;
        g.interactable = false;
        return g;
    }

    private static Image Bloque(string nombre, Transform padre, Vector2 anclaMin, Vector2 anclaMax, float y, float alto, Color color)
    {
        GameObject go = new(nombre, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.pivot = new Vector2(0.5f, anclaMin.y);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(0f, alto);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI Letras(Transform padre, Vector2 ancla, Vector2 pos, Vector2 tam, float tamLetra, Color color, TMP_FontAsset fuente)
    {
        GameObject go = new("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();

        if (fuente != null)
            t.font = fuente;

        t.fontSize = tamLetra;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
        t.raycastTarget = false;
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(20, 12, 6, 255);
        return t;
    }

    private static void Mostrar(CanvasGroup g, float alfa) => g.alpha = alfa;

    private static IEnumerator Fundir(CanvasGroup g, float hasta, float segundos)
    {
        float desde = g.alpha;
        float t = 0f;

        while (t < segundos)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / segundos));
            yield return null;
        }

        g.alpha = hasta;
    }
}
