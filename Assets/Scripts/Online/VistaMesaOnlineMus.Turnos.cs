using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// ENVITES LIBRES Y RELOJ DE CADA TURNO
//  - ENVIDO y SUBIR abren un selector: eliges cuantos tantos (de 2 hasta el limite) con - / + o con los atajos.
//  - Cada persona tiene un tiempo para decidir (lo lleva el servidor). Si se acaba, pasa sola.
//    Tu reloj sale encima de tus botones; el de los demas, un aro junto a su cabeza.
public partial class VistaMesaOnlineMus
{
    private GameObject panelEnviteUI;
    private TextMeshProUGUI txtTituloEnvite, txtCantidadEnvite;
    private Button botonConfirmarEnvite;
    private TextMeshProUGUI txtConfirmarEnvite;
    private bool enviteEsSubida;
    private int cantidadEnvite = 2;

    private GameObject relojPropio;
    private Image barraRelojPropio;
    private TextMeshProUGUI txtRelojPropio;
    private readonly Image[] aros = new Image[4];
    private readonly TextMeshProUGUI[] txtAros = new TextMeshProUGUI[4];

    // ---------- Selector de envite ----------

    private void CrearSelectorEnvite()
    {
        panelEnviteUI = Panel("Envite", raizUI, "panel_madera", new Vector2(0.5f, 0f), new Vector2(0, 172), new Vector2(760, 250));
        panelEnviteUI.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);

        GameObject titulo = new("Titulo", typeof(RectTransform));
        Rect(titulo, panelEnviteUI.transform, new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(700, 56));
        txtTituloEnvite = Texto(titulo.transform, "", 40, Oro, true);

        Button menos = Boton(panelEnviteUI.transform, "gris", "-", new Vector2(90, 80), () => CambiarCantidad(-1), 48);
        Rect(menos.gameObject, panelEnviteUI.transform, new Vector2(0.5f, 1f), new Vector2(-170, -72), new Vector2(90, 80));
        GameObject cantidad = new("Cantidad", typeof(RectTransform));
        Rect(cantidad, panelEnviteUI.transform, new Vector2(0.5f, 1f), new Vector2(0, -72), new Vector2(200, 80));
        txtCantidadEnvite = Texto(cantidad.transform, "2", 64, Crema, true);
        Button mas = Boton(panelEnviteUI.transform, "gris", "+", new Vector2(90, 80), () => CambiarCantidad(1), 48);
        Rect(mas.gameObject, panelEnviteUI.transform, new Vector2(0.5f, 1f), new Vector2(170, -72), new Vector2(90, 80));

        // Atajos
        int[] atajos = { 2, 3, 5, 10 };
        for (int i = 0; i < atajos.Length; i++)
        {
            int valor = atajos[i];
            Button b = Boton(panelEnviteUI.transform, "naranja", valor.ToString(), new Vector2(76, 56), () => PonerCantidad(valor), 30);
            Rect(b.gameObject, panelEnviteUI.transform, new Vector2(0f, 0f), new Vector2(24 + i * 84, 18), new Vector2(76, 56));
        }

        botonConfirmarEnvite = Boton(panelEnviteUI.transform, "verde", "ENVIDO", new Vector2(260, 70), ConfirmarEnvite, 30);
        Rect(botonConfirmarEnvite.gameObject, panelEnviteUI.transform, new Vector2(1f, 0f), new Vector2(-96, 14), new Vector2(260, 70));
        txtConfirmarEnvite = botonConfirmarEnvite.GetComponentInChildren<TextMeshProUGUI>();

        Button cerrar = Boton(panelEnviteUI.transform, "rojo", "X", new Vector2(56, 56), () => panelEnviteUI.SetActive(false), 30);
        Rect(cerrar.gameObject, panelEnviteUI.transform, new Vector2(1f, 1f), new Vector2(-14, -14), new Vector2(56, 56));

        panelEnviteUI.SetActive(false);
    }

    private int MaximoEnvite()
    {
        EstadoPublicoMus e = partida.Estado;
        int limite = Mathf.Max(3, e.Limite);
        return enviteEsSubida ? Mathf.Max(2, limite - 1 - e.TantosApostados) : limite - 1;
    }

    private void AbrirSelectorEnvite(bool subir)
    {
        enviteEsSubida = subir;
        cantidadEnvite = 2;
        panelEnviteUI.SetActive(true);
        panelEnviteUI.transform.SetAsLastSibling();
        RefrescarSelectorEnvite();
    }

    private void CambiarCantidad(int delta) => PonerCantidad(cantidadEnvite + delta);

    private void PonerCantidad(int valor)
    {
        cantidadEnvite = Mathf.Clamp(valor, 2, MaximoEnvite());
        RefrescarSelectorEnvite();
    }

    private void RefrescarSelectorEnvite()
    {
        EstadoPublicoMus e = partida.Estado;
        txtTituloEnvite.text = enviteEsSubida ? "¿CUANTO SUBES?  <size=60%>(hay " + e.TantosApostados + ")</size>" : "¿CUANTOS ENVIDAS?";
        txtCantidadEnvite.text = cantidadEnvite.ToString();
        txtConfirmarEnvite.text = enviteEsSubida ? cantidadEnvite + " MAS" : "ENVIDO " + cantidadEnvite;
    }

    private void ConfirmarEnvite()
    {
        Pedir(enviteEsSubida ? AccionMus.Reenvidar(cantidadEnvite) : AccionMus.Envidar(cantidadEnvite));
        panelEnviteUI.SetActive(false);
    }

    private void ActualizarSelectorEnvite(bool miTurno)
    {
        if (panelEnviteUI == null || !panelEnviteUI.activeSelf)
            return;

        // Si ya no te toca (o ya no se puede), se cierra solo.
        TipoAccionMus tipo = enviteEsSubida ? TipoAccionMus.Reenvidar : TipoAccionMus.Envidar;

        if (!miTurno || !partida.Estado.Legal(tipo))
            panelEnviteUI.SetActive(false);
    }

    // ---------- Relojes ----------

    private void CrearRelojes()
    {
        relojPropio = Panel("TuReloj", raizUI, "panel_madera_oscura", new Vector2(0.5f, 0f), new Vector2(0, 128), new Vector2(560, 34));
        relojPropio.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);

        GameObject barra = new("Barra", typeof(RectTransform), typeof(Image));
        RectTransform rb = barra.GetComponent<RectTransform>();
        rb.SetParent(relojPropio.transform, false);
        rb.anchorMin = new Vector2(0f, 0f);
        rb.anchorMax = new Vector2(1f, 1f);
        rb.offsetMin = new Vector2(6, 6);
        rb.offsetMax = new Vector2(-6, -6);
        barraRelojPropio = barra.GetComponent<Image>();
        barraRelojPropio.sprite = SpriteUI("circulo", 0);
        barraRelojPropio.type = Image.Type.Filled;
        barraRelojPropio.fillMethod = Image.FillMethod.Horizontal;
        barraRelojPropio.raycastTarget = false;

        GameObject t = new("Segundos", typeof(RectTransform));
        Rect(t, relojPropio.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540, 34)).pivot = new Vector2(0.5f, 0.5f);
        txtRelojPropio = Texto(t.transform, "", 24, Color.white);
        relojPropio.SetActive(false);
    }

    private float SegundosQueQuedan(out float fraccion)
    {
        EstadoPublicoMus e = partida.Estado;
        fraccion = 0f;

        if (e.FinTurno <= 0 || e.DuracionTurno <= 0f || NetworkManager.Singleton == null)
            return -1f;

        float quedan = (float)(e.FinTurno - NetworkManager.Singleton.ServerTime.Time);
        fraccion = Mathf.Clamp01(quedan / e.DuracionTurno);
        return Mathf.Max(0f, quedan);
    }

    private static Color ColorReloj(float fraccion)
    {
        return fraccion > 0.5f ? Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(0.35f, 0.85f, 0.4f), (fraccion - 0.5f) * 2f)
            : Color.Lerp(new Color(0.95f, 0.25f, 0.2f), new Color(1f, 0.85f, 0.2f), fraccion * 2f);
    }

    private void ActualizarRelojes(bool enJuego, bool revision)
    {
        if (relojPropio == null)
            return;

        EstadoPublicoMus e = partida.Estado;
        int local = partida.PuestoLocal;
        float quedan = SegundosQueQuedan(out float fraccion);
        bool hayReloj = enJuego && !revision && quedan >= 0f && e.EnCurso && cola.Count == 0;
        int turno = e.JugadorActual;

        // El tuyo: barra encima de los botones.
        bool mio = hayReloj && local >= 0 && turno == local;
        relojPropio.SetActive(mio);

        if (mio)
        {
            barraRelojPropio.fillAmount = fraccion;
            Color c = ColorReloj(fraccion);
            barraRelojPropio.color = c;
            int s = Mathf.CeilToInt(quedan);
            txtRelojPropio.text = s <= 5 ? "<b>¡" + s + " s!</b>" : s + " s para decidir";
            float pulso = s <= 5 ? 1f + 0.06f * Mathf.Sin(Time.time * 10f) : 1f;
            relojPropio.transform.localScale = new Vector3(pulso, pulso, 1f);
        }

        // Los demas: un aro junto a la cabeza del que tiene el turno.
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;

        for (int p = 0; p < 4; p++)
        {
            bool ver = hayReloj && turno == p && p != local && cam != null && ModeloEnSitio(Relativo(p)) != null;

            if (aros[p] == null)
            {
                if (!ver)
                    continue;

                GameObject go = new("Reloj_" + p, typeof(RectTransform), typeof(Image));
                Rect(go, raizUI, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72, 72)).pivot = new Vector2(0.5f, 0.5f);
                aros[p] = go.GetComponent<Image>();
                aros[p].sprite = SpriteUI("anillo", 0);
                aros[p].type = Image.Type.Filled;
                aros[p].fillMethod = Image.FillMethod.Radial360;
                aros[p].fillOrigin = (int)Image.Origin360.Top;
                aros[p].fillClockwise = false;
                aros[p].raycastTarget = false;
                GameObject t = new("Segundos", typeof(RectTransform));
                Rect(t, go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72, 72)).pivot = new Vector2(0.5f, 0.5f);
                txtAros[p] = Texto(t.transform, "", 26, Color.white, true);
            }

            if (!ver)
            {
                aros[p].gameObject.SetActive(false);
                continue;
            }

            GameObject m = ModeloEnSitio(Relativo(p));
            float alto = Mathf.Max(0.2f, CabezaDe(m).y - CuerpoDe(m).y);
            Vector3 punto = CabezaDe(m) + Vector3.up * (alto * 0.45f) - cam.transform.right * (alto * 0.5f);
            Vector3 pantalla = cam.WorldToScreenPoint(punto);

            if (pantalla.z <= 0f)
            {
                aros[p].gameObject.SetActive(false);
                continue;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(raizUI, pantalla, null, out Vector2 pos);
            aros[p].gameObject.SetActive(true);
            aros[p].rectTransform.anchoredPosition = pos;
            aros[p].fillAmount = fraccion;
            aros[p].color = ColorReloj(fraccion);
            txtAros[p].text = Mathf.CeilToInt(quedan).ToString();
        }
    }
}
