using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Monedas: saldo y regalo diario en el menu, mesa con entrada en la sala,
// se paga la entrada al empezar y la pareja ganadora cobra el doble.
public partial class VistaMesaOnlineMus
{
    private TextMeshProUGUI txtSaldoMenu, txtEstadisticasMenu;
    private Button botonRegaloMenu;
    private int saldoMostrado = -1;

    private readonly List<Button> botonesMesa = new();
    private readonly List<int> entradasBotones = new();
    private TextMeshProUGUI txtInfoMesa;

    private int codigoPagado, codigoCobrado, apuestaPagada, parejaPagada;

    private GameObject avisoMonedas;
    private TextMeshProUGUI txtAvisoMonedas;
    private float finAvisoMonedas;

    // ---------- Menu ----------

    private void CrearMonedasMenu()
    {
        GameObject caja = Panel("Monedas", panelMenuUI.transform, "panel_madera", new Vector2(1f, 1f), new Vector2(-40, -40), new Vector2(380, 230));
        caja.GetComponent<RectTransform>().pivot = new Vector2(1f, 1f);

        Icono(caja.transform, new Vector2(0f, 1f), new Vector2(30, -22), 78);

        GameObject saldo = new("Saldo", typeof(RectTransform));
        Rect(saldo, caja.transform, new Vector2(0f, 1f), new Vector2(120, -20), new Vector2(240, 80));
        txtSaldoMenu = Texto(saldo.transform, "", 54, Oro, true, TextAlignmentOptions.Left);

        GameObject stats = new("Estadisticas", typeof(RectTransform));
        Rect(stats, caja.transform, new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(340, 36));
        txtEstadisticasMenu = Texto(stats.transform, "", 20, Crema);

        botonRegaloMenu = Boton(caja.transform, "dorado", "REGALO DIARIO +" + MonederoMus.RegaloDiario, new Vector2(330, 66), () =>
        {
            if (MonederoMus.ReclamarRegalo())
                MostrarAvisoMonedas("+" + MonederoMus.RegaloDiario + " monedas de regalo. ¡Vuelve manana!");
        }, 24);
        Rect(botonRegaloMenu.gameObject, caja.transform, new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(330, 66));

        Button taquilla = Boton(panelMenuUI.transform, "morado", "TAQUILLA", new Vector2(380, 84), AbrirTaquilla, 34);
        Rect(taquilla.gameObject, panelMenuUI.transform, new Vector2(1f, 1f), new Vector2(-230, -330), new Vector2(380, 84));
    }

    private Image Icono(Transform padre, Vector2 ancla, Vector2 pos, float tam)
    {
        GameObject go = new("Moneda", typeof(RectTransform), typeof(Image));
        Rect(go, padre, ancla, pos, new Vector2(tam, tam));
        Image img = go.GetComponent<Image>();
        img.sprite = SpriteUI("moneda", 0);
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    // ---------- Sala: elegir mesa ----------

    private void CrearSelectorMesa(Transform padre, float y)
    {
        GameObject fila = new("Mesas", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        Rect(fila, padre, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(980, 70));
        HorizontalLayoutGroup h = fila.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 14;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;

        GameObject etiqueta = new("Etiqueta", typeof(RectTransform), typeof(LayoutElement));
        etiqueta.transform.SetParent(fila.transform, false);
        LayoutElement le = etiqueta.GetComponent<LayoutElement>();
        le.preferredWidth = 160;
        le.preferredHeight = 66;
        Texto(etiqueta.transform, "MESA:", 40, Oro, true);

        IReadOnlyList<int> entradas = partida.EntradasMesa;

        for (int i = 0; i < entradas.Count; i++)
        {
            int entrada = entradas[i];
            string texto = entrada == 0 ? "GRATIS" : MonederoMus.Formato(entrada);
            Button b = Boton(fila.transform, entrada == 0 ? "gris" : "dorado", texto, new Vector2(165, 66), () => partida.CambiarApuesta(entrada), 30);
            botonesMesa.Add(b);
            entradasBotones.Add(entrada);
        }

        GameObject info = new("InfoMesa", typeof(RectTransform));
        Rect(info, padre, new Vector2(0.5f, 1f), new Vector2(0, y - 76), new Vector2(1000, 46));
        txtInfoMesa = Texto(info.transform, "", 25, Crema);
    }

    private void ActualizarSelectorMesa(int miSitio)
    {
        int apuesta = partida.ApuestaSalaVista;

        for (int i = 0; i < botonesMesa.Count; i++)
        {
            bool elegida = entradasBotones[i] == apuesta;
            botonesMesa[i].interactable = partida.IsServer || elegida;
            botonesMesa[i].transform.localScale = Vector3.one * (elegida ? 1.12f : 0.92f);
            botonesMesa[i].image.color = elegida ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }

        int saldo = MonederoMus.Saldo;
        string texto;

        if (apuesta <= 0)
            texto = "Mesa gratis: sin monedas en juego.   Tienes <color=#FFD24D>" + MonederoMus.Formato(saldo) + "</color>";
        else
            texto = "Entrada " + MonederoMus.Formato(apuesta) + "  ·  Si ganais cobras <color=#7CD67C>" +
                    MonederoMus.Formato(apuesta * 2) + "</color>  ·  Tienes <color=#FFD24D>" + MonederoMus.Formato(saldo) + "</color>";

        if (!MonederoMus.PuedePagar(apuesta))
            texto += partida.IsServer
                ? "\n<color=#FF6B5E>No te llega para esta mesa: elige una mas barata</color>"
                : "\n<color=#FF6B5E>No te llega: jugaras sin apostar</color>";
        else if (!partida.IsServer)
            texto += "  <size=80%>(la mesa la elige el host)</size>";

        txtInfoMesa.text = texto;
    }

    // ---------- Pagar y cobrar ----------

    private void ActualizarMonedas(bool conectado, bool enMesa)
    {
        ActualizarAvisoMonedas();

        if (!conectado)
        {
            int saldo = MonederoMus.Saldo;

            if (saldo != saldoMostrado)
            {
                saldoMostrado = saldo;
                txtSaldoMenu.text = MonederoMus.Formato(saldo);
            }

            int jugadas = MonederoMus.PartidasJugadas;
            txtEstadisticasMenu.text = jugadas == 0 ? "Aun no has jugado partidas"
                : "Ganadas " + MonederoMus.PartidasGanadas + " de " + jugadas;
            botonRegaloMenu.gameObject.SetActive(MonederoMus.RegaloDisponible);
            codigoPagado = 0;
            return;
        }

        if (!enMesa || DemoActiva)
            return;

        EstadoPublicoMus e = partida.Estado;
        int local = partida.PuestoLocal;

        // Al empezar una partida en la que estoy sentado, pago la entrada.
        if (e.EnCurso && local >= 0 && e.CodigoPartida != 0 && e.CodigoPartida != codigoPagado)
        {
            codigoPagado = e.CodigoPartida;
            parejaPagada = local % 2;
            apuestaPagada = MonederoMus.Pagar(e.Apuesta) ? e.Apuesta : 0;

            if (e.Apuesta > 0)
                MostrarAvisoMonedas(apuestaPagada > 0
                    ? "Entrada pagada: -" + MonederoMus.Formato(apuestaPagada) + " monedas. ¡Bote en juego!"
                    : "No te llegan las monedas: juegas sin apostar");
        }

        // Al terminar (y cuando la mesa ya ha ensenado el final), cobro si ha ganado mi pareja.
        if (!e.EnCurso && e.ParejaGanadora >= 0 && e.CodigoPartida == codigoPagado && codigoPagado != 0 &&
            codigoCobrado != codigoPagado && cola.Count == 0)
        {
            codigoCobrado = codigoPagado;
            bool ganamos = e.ParejaGanadora == parejaPagada;
            MonederoMus.ContarPartida(ganamos);

            if (apuestaPagada > 0)
            {
                if (ganamos)
                {
                    MonederoMus.Cobrar(apuestaPagada * 2);
                    MostrarAvisoMonedas("¡GANAIS! +" + MonederoMus.Formato(apuestaPagada * 2) + " monedas");
                }
                else
                {
                    MostrarAvisoMonedas("Habeis perdido la entrada (" + MonederoMus.Formato(apuestaPagada) + " monedas)");
                }
            }
        }
    }

    // ---------- Aviso con la moneda ----------

    private void MostrarAvisoMonedas(string texto)
    {
        if (avisoMonedas == null)
        {
            avisoMonedas = Panel("AvisoMonedas", raizUI, "panel_madera", new Vector2(0.5f, 1f), new Vector2(0, -300), new Vector2(980, 110));
            avisoMonedas.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            Icono(avisoMonedas.transform, new Vector2(0f, 0.5f), new Vector2(24, 36), 76);

            GameObject t = new("Texto", typeof(RectTransform));
            Rect(t, avisoMonedas.transform, new Vector2(0.5f, 0.5f), new Vector2(40, 0), new Vector2(840, 90))
                .pivot = new Vector2(0.5f, 0.5f);
            txtAvisoMonedas = Texto(t.transform, "", 36, Oro, true);
        }

        avisoMonedas.transform.SetAsLastSibling();
        txtAvisoMonedas.text = texto;
        finAvisoMonedas = Time.time + 4.5f;
        avisoMonedas.SetActive(true);
        avisoMonedas.transform.localScale = Vector3.one * 0.6f;
    }

    private void ActualizarAvisoMonedas()
    {
        if (avisoMonedas == null || !avisoMonedas.activeSelf)
            return;

        if (Time.time > finAvisoMonedas)
        {
            avisoMonedas.SetActive(false);
            return;
        }

        float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f);
        avisoMonedas.transform.localScale = Vector3.Lerp(avisoMonedas.transform.localScale, Vector3.one, k);
    }
}
