using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// PANTALLA DE CARGA: es la primera escena de la build (Carga.unity, muy ligera, sale enseguida).
// Ensena el logo, una barra y consejos mientras se carga la mesa por detras; asi no se queda la pantalla en negro.
// Al terminar se queda unos instantes encima de la mesa y se funde.
public class PantallaCargaMus : MonoBehaviour
{
    [SerializeField] private string escenaJuego = "MesamusOnline";
    [SerializeField] private float tiempoMinimo = 1.6f;

    private static readonly Color Fondo = new(0.16f, 0.10f, 0.06f);
    private static readonly Color Oro = new(1f, 0.78f, 0.25f);
    private static readonly Color Crema = new(0.96f, 0.9f, 0.78f);

    private static readonly string[] Consejos =
    {
        "Las senas solo las ve tu companero si te esta mirando.",
        "Un buen ordago a tiempo gana partidas.",
        "Grande, chica, pares y juego: piensa en los cuatro lances.",
        "Con 31 de juego tienes la mejor jugada posible.",
        "Toca a un jugador o desliza el dedo para mirarle.",
        "Gana partidas con entrada para conseguir monedas.",
        "En la TAQUILLA puedes cambiar tu ropa y tus accesorios."
    };

    private CanvasGroup grupo;
    private RectTransform relleno;
    private TextMeshProUGUI txtEstado, txtConsejo;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        CrearInterfaz();
        StartCoroutine(Cargar());
    }

    private IEnumerator Cargar()
    {
        float inicio = Time.unscaledTime;
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync(escenaJuego);

        if (op == null)
        {
            txtEstado.text = "No se encuentra la escena " + escenaJuego;
            yield break;
        }

        op.allowSceneActivation = false;
        float mostrado = 0f;
        int consejo = Random.Range(0, Consejos.Length);
        float cambioConsejo = Time.unscaledTime + 3f;
        txtConsejo.text = Consejos[consejo];

        // Unity llega hasta 0.9 cargando; el ultimo 10 % es al activar la escena.
        while (op.progress < 0.9f || Time.unscaledTime - inicio < tiempoMinimo)
        {
            float real = Mathf.Clamp01(op.progress / 0.9f) * 0.9f;
            float porTiempo = Mathf.Clamp01((Time.unscaledTime - inicio) / tiempoMinimo) * 0.9f;
            mostrado = Mathf.MoveTowards(mostrado, Mathf.Min(real, porTiempo), Time.unscaledDeltaTime * 0.8f);
            Pintar(mostrado);

            if (Time.unscaledTime > cambioConsejo)
            {
                consejo = (consejo + 1) % Consejos.Length;
                txtConsejo.text = Consejos[consejo];
                cambioConsejo = Time.unscaledTime + 3f;
            }

            yield return null;
        }

        Pintar(0.9f);
        txtEstado.text = "Preparando la mesa...";
        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;

        // La mesa tarda un par de fotogramas en montar la interfaz: se espera tapandola.
        for (int i = 0; i < 4; i++)
        {
            Pintar(0.9f + 0.025f * (i + 1));
            yield return null;
        }

        Pintar(1f);
        yield return new WaitForSecondsRealtime(0.15f);

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
        {
            grupo.alpha = 1f - t;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Pintar(float fraccion)
    {
        relleno.anchorMax = new Vector2(Mathf.Clamp01(fraccion), 1f);
        txtEstado.text = fraccion < 0.9f ? "Barajando las cartas... " + Mathf.RoundToInt(fraccion * 100f) + " %" : txtEstado.text;
    }

    // ------------------------------------------------------------------ interfaz

    private void CrearInterfaz()
    {
        Canvas lienzo = gameObject.AddComponent<Canvas>();
        lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
        lienzo.sortingOrder = 1000;
        CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();   // tapa los toques mientras carga
        grupo = gameObject.AddComponent<CanvasGroup>();
        RectTransform raiz = (RectTransform)transform;

        TMP_FontAsset titulos = Fuente("UI_Tinko/Fuentes/AlfaSlabOne-Regular");
        TMP_FontAsset textos = Fuente("UI_Tinko/Fuentes/Bitter-Regular");

        Image fondo = Bloque("Fondo", raiz, Vector2.zero, Vector2.one, Fondo);
        fondo.raycastTarget = true;
        Image degradado = Bloque("Sombra", raiz, Vector2.zero, new Vector2(1f, 0.45f), new Color(0f, 0f, 0f, 0.35f));
        Texture2D tex = Resources.Load<Texture2D>("UI_Tinko/degradado_inferior");
        if (tex != null)
            degradado.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

        // Cuatro naipes de fondo, abiertos en abanico, con los colores de las pintas.
        Color[] pintas = { new(0.89f, 0.70f, 0.24f), new(0.72f, 0.2f, 0.16f), new(0.17f, 0.44f, 0.65f), new(0.18f, 0.42f, 0.24f) };

        for (int i = 0; i < 4; i++)
        {
            Image naipe = Bloque("Naipe", raiz, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.95f, 0.89f, 0.75f));
            RectTransform rt = naipe.rectTransform;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(150, 230);
            rt.anchoredPosition = new Vector2(0f, 20f);
            rt.localEulerAngles = new Vector3(0f, 0f, 24f - i * 16f);
            Image pinta = Bloque("Pinta", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pintas[i]);
            pinta.rectTransform.sizeDelta = new Vector2(70, 70);
            pinta.rectTransform.anchoredPosition = new Vector2(0f, 30f);
            Texture2D circulo = Resources.Load<Texture2D>("UI_Tinko/circulo");
            if (circulo != null)
                pinta.sprite = Sprite.Create(circulo, new Rect(0, 0, circulo.width, circulo.height), new Vector2(0.5f, 0.5f));
        }

        TextMeshProUGUI logo = Texto(raiz, titulos, "<size=70%>Mus</size> TINKO", 120, Oro, new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(1200, 150));
        logo.outlineWidth = 0.2f;
        logo.outlineColor = new Color32(30, 18, 10, 255);

        // Barra de carga
        Image marco = Bloque("Barra", raiz, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.07f, 0.04f, 0.02f, 0.9f));
        marco.rectTransform.sizeDelta = new Vector2(820, 30);
        marco.rectTransform.anchoredPosition = new Vector2(0f, -220f);
        Image barra = Bloque("Relleno", marco.rectTransform, Vector2.zero, new Vector2(0f, 1f), Oro);
        relleno = barra.rectTransform;
        relleno.offsetMin = new Vector2(4f, 4f);
        relleno.offsetMax = new Vector2(-4f, -4f);

        txtEstado = Texto(raiz, textos, "Barajando las cartas...", 30, Crema, new Vector2(0.5f, 0.5f), new Vector2(0f, -275f), new Vector2(1000, 50));
        txtConsejo = Texto(raiz, textos, "", 28, new Color(0.85f, 0.77f, 0.63f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1500, 90));
        txtConsejo.fontStyle = FontStyles.Normal;
    }

    private static Image Bloque(string nombre, RectTransform padre, Vector2 min, Vector2 max, Color color)
    {
        GameObject go = new(nombre, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI Texto(RectTransform padre, TMP_FontAsset fuente, string texto, float tam, Color color, Vector2 ancla, Vector2 pos, Vector2 tamano)
    {
        GameObject go = new("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamano;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = texto;
        t.fontSize = tam;
        t.enableAutoSizing = true;
        t.fontSizeMax = tam;
        t.fontSizeMin = tam * 0.5f;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    private static readonly Dictionary<string, TMP_FontAsset> fuentes = new();

    private static TMP_FontAsset Fuente(string ruta)
    {
        if (fuentes.TryGetValue(ruta, out TMP_FontAsset f))
            return f;

        Font fuente = Resources.Load<Font>(ruta);
        TMP_FontAsset asset = null;

        try
        {
            if (fuente != null)
                asset = TMP_FontAsset.CreateFontAsset(fuente);
        }
        catch
        {
            asset = null;
        }

        fuentes[ruta] = asset;
        return asset;
    }
}
