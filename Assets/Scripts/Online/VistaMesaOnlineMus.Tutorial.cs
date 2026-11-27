using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PARTIDA TUTORIAL: una partida contra bots que se para al principio para ensenar a mirar
// (tocar abajo, a la derecha, a la izquierda y en el medio), las senas y los botones. Despues se juega normal.
// Sale sola despues de crear el personaje la primera vez, y desde INICIO cuando quieras.
public partial class VistaMesaOnlineMus
{
    private const string ClaveTutorial = "MusTinko_TutorialHecho";

    private bool tutorialActivo, tutorialLanzado, tutorialEnMesa;
    private int pasoTutorial;
    private float momentoPasoTutorial;
    private GameObject panelTutorial;
    private TextMeshProUGUI txtTituloTutorial, txtTextoTutorial, txtPasoTutorial, txtSeguirTutorial;
    private Button botonSeguirTutorial;
    private RectTransform flechaTutorial;

    private enum EsperaTutorial { Boton, Cartas, Derecha, Izquierda, Centro }

    private static readonly (string Titulo, string Texto, EsperaTutorial Espera)[] PasosTutorial =
    {
        ("Asi se mira en la mesa",
            "En el mus no ves a todos a la vez: miras a quien quieras <b>tocando la pantalla</b>. Vamos a probarlo (la partida espera).",
            EsperaTutorial.Boton),
        ("Mira tus cartas", "Toca la parte de <b>ABAJO</b> de la pantalla.", EsperaTutorial.Cartas),
        ("Mira a tu derecha", "Toca el lado <b>DERECHO</b> de la pantalla. Ahi se sienta un rival.", EsperaTutorial.Derecha),
        ("Mira a tu izquierda", "Ahora toca el lado <b>IZQUIERDO</b>: el otro rival.", EsperaTutorial.Izquierda),
        ("Vuelve al centro", "Toca en el <b>MEDIO</b> de la pantalla. Enfrente tienes a tu companero.", EsperaTutorial.Centro),
        ("Tambien deslizando",
            "Si deslizas el dedo hacia un lado, hacia abajo o hacia arriba, la vista salta sola. Con <b>ZOOM</b> (arriba a la derecha) la camara se acerca a quien miras.",
            EsperaTutorial.Boton),
        ("Las senas",
            "Con tus cartas puedes hacer senas a tu companero (panel de la izquierda). Ojo: <b>solo las ve si te esta mirando</b>... y los rivales tambien pueden pillarlas.",
            EsperaTutorial.Boton),
        ("¡A jugar!",
            "Cuando te toque hablar saldran abajo los botones: Mus, Paso, Envido, Quiero, Ordago... El minimapa de abajo a la izquierda te dice a quien le toca.",
            EsperaTutorial.Boton),
    };

    public static bool TutorialHecho
    {
        get
        {
            try { return PlayerPrefs.GetInt(ClaveTutorial, 0) == 1; }
            catch { return true; }
        }
    }

    public void EmpezarTutorial()
    {
        if (tutorialActivo)
            return;

        tutorialActivo = true;
        tutorialLanzado = false;
        tutorialEnMesa = false;
        pasoTutorial = 0;
        GuardarNombre(NombreLocal);
        conexion?.CrearPartida();
    }

    private void TerminarTutorial()
    {
        tutorialActivo = false;

        if (partida != null && partida.IsSpawned && partida.IsServer)
            partida.Pausado = false;

        if (panelTutorial != null)
            panelTutorial.SetActive(false);

        try
        {
            PlayerPrefs.SetInt(ClaveTutorial, 1);
            PlayerPrefs.Save();
        }
        catch
        {
        }
    }

    // Cada frame desde ActualizarInterfaz.
    private void ActualizarTutorial(bool conectado)
    {
        if (!tutorialActivo)
            return;

        // Si se sale de la partida a medias, se acaba el tutorial.
        if (!conectado)
        {
            if (tutorialLanzado)
                TerminarTutorial();
            return;
        }

        if (!partida.IsSpawned)
            return;

        // La partida empieza sola, sin pasar por la sala.
        if (!tutorialLanzado && partida.IsServer && !partida.PartidaEmpezada)
        {
            tutorialLanzado = true;
            partida.EmpezarPartidaServidor();
            return;
        }

        if (!partida.PartidaEmpezada)
            return;

        if (panelTutorial == null)
            CrearPanelTutorial();

        if (!tutorialEnMesa)
        {
            tutorialEnMesa = true;
            MostrarPasoTutorial(0);
        }

        // Mientras se explica, los bots y el reloj esperan (el panel tapa el marcador, que aun no hace falta).
        if (partida.IsServer)
            partida.Pausado = true;

        panelTutorial.SetActive(!enRevision);
        panelTutorial.transform.SetAsLastSibling();

        EsperaTutorial espera = PasosTutorial[pasoTutorial].Espera;
        bool cumplido = espera switch
        {
            EsperaTutorial.Cartas => posicionMirada == MirarCartas,
            EsperaTutorial.Derecha => posicionMirada == MirarDerecha,
            EsperaTutorial.Izquierda => posicionMirada == MirarIzquierda,
            EsperaTutorial.Centro => posicionMirada == MirarFrente,
            _ => false
        };
        bool hecho = cumplido && Time.unscaledTime - momentoPasoTutorial > 0.6f;

        if (hecho)
            SiguientePasoTutorial();

        AnimarFlechaTutorial(espera);
    }

    private void SiguientePasoTutorial()
    {
        if (pasoTutorial >= PasosTutorial.Length - 1)
        {
            TerminarTutorial();
            return;
        }

        MostrarPasoTutorial(pasoTutorial + 1);
    }

    private void MostrarPasoTutorial(int paso)
    {
        pasoTutorial = paso;
        momentoPasoTutorial = Time.unscaledTime;
        var p = PasosTutorial[paso];
        txtTituloTutorial.text = p.Titulo;
        txtTextoTutorial.text = p.Texto;
        txtPasoTutorial.text = (paso + 1) + " / " + PasosTutorial.Length;
        botonSeguirTutorial.gameObject.SetActive(p.Espera == EsperaTutorial.Boton);
        txtSeguirTutorial.text = paso == PasosTutorial.Length - 1 ? "¡A JUGAR!" : "SIGUIENTE";

        // Para el paso del centro, primero hay que estar mirando a otro lado (si no, ya estaria hecho).
        if (p.Espera == EsperaTutorial.Centro && posicionMirada == MirarFrente)
            MirarPosicion(MirarCartas);
    }

    private void CrearPanelTutorial()
    {
        panelTutorial = Panel("Tutorial", raizUI, "panel_pergamino", new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(1120, 250));
        Transform t = panelTutorial.transform;

        GameObject titulo = new("Titulo", typeof(RectTransform));
        Rect(titulo, t, new Vector2(0f, 1f), new Vector2(34, -18), new Vector2(700, 60));
        txtTituloTutorial = Texto(titulo.transform, "", 44, Tinta, true, TextAlignmentOptions.MidlineLeft, false);

        GameObject paso = new("Paso", typeof(RectTransform));
        Rect(paso, t, new Vector2(1f, 1f), new Vector2(-34, -22), new Vector2(160, 50));
        txtPasoTutorial = Texto(paso.transform, "", 24, new Color(0.45f, 0.35f, 0.25f), false, TextAlignmentOptions.MidlineRight, false);

        GameObject texto = new("Texto", typeof(RectTransform));
        Rect(texto, t, new Vector2(0f, 1f), new Vector2(34, -84), new Vector2(1050, 80));
        txtTextoTutorial = Texto(texto.transform, "", 27, Tinta, false, TextAlignmentOptions.TopLeft, false);

        botonSeguirTutorial = Boton(t, "verde", "SIGUIENTE", new Vector2(260, 64), SiguientePasoTutorial, 28);
        Rect(botonSeguirTutorial.gameObject, t, new Vector2(1f, 0f), new Vector2(-30, 18), new Vector2(260, 64));
        txtSeguirTutorial = botonSeguirTutorial.GetComponentInChildren<TextMeshProUGUI>(true);

        Button saltar = Boton(t, "gris", "SALTAR TUTORIAL", new Vector2(280, 56), TerminarTutorial, 22);
        Rect(saltar.gameObject, t, new Vector2(0f, 0f), new Vector2(30, 22), new Vector2(280, 56));

        // Flecha grande que senala la zona que hay que tocar.
        GameObject flecha = new("FlechaTutorial", typeof(RectTransform), typeof(Image));
        flechaTutorial = Rect(flecha, raizUI, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
        flechaTutorial.pivot = new Vector2(0.5f, 0.5f);
        Image img = flecha.GetComponent<Image>();
        img.sprite = SpriteUI("flecha", 0);
        img.raycastTarget = false;
        flecha.transform.SetParent(t, true);
    }

    private void AnimarFlechaTutorial(EsperaTutorial espera)
    {
        bool ver = espera != EsperaTutorial.Boton && panelTutorial.activeSelf;
        flechaTutorial.gameObject.SetActive(ver);

        if (!ver)
            return;

        // Las posiciones son respecto al centro de la pantalla (en la escala de la interfaz).
        Vector2 tam = raizUI.rect.size;
        float vaiven = Mathf.Sin(Time.unscaledTime * 6f) * 18f;
        Vector2 pos;
        float giro;

        switch (espera)
        {
            case EsperaTutorial.Cartas: pos = new Vector2(0f, -tam.y * 0.36f - vaiven); giro = 180f; break;
            case EsperaTutorial.Derecha: pos = new Vector2(tam.x * 0.38f + vaiven, -tam.y * 0.05f); giro = -90f; break;
            case EsperaTutorial.Izquierda: pos = new Vector2(-tam.x * 0.38f - vaiven, -tam.y * 0.05f); giro = 90f; break;
            default: pos = new Vector2(0f, -tam.y * 0.05f); giro = 0f; break;
        }

        flechaTutorial.position = raizUI.TransformPoint(pos);
        flechaTutorial.localEulerAngles = new Vector3(0f, 0f, giro);
        float escala = espera == EsperaTutorial.Centro ? 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
        flechaTutorial.localScale = Vector3.one * escala;
    }
}
