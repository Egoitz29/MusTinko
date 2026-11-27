using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// BIENVENIDA Y CREADOR DE PERSONAJE
//  - La primera vez que se abre el juego sale una bienvenida y despues el creador: nombre, color de la ropa,
//    piel, pelo, traje y complementos. En ese momento TODO es gratis; lo que elijas se queda como tuyo.
//  - Despues, desde PERFIL o la TAQUILLA (MI PERSONAJE) se puede cambiar: los colores siguen siendo gratis,
//    los trajes y complementos solo los que tengas (el resto se compran en la taquilla).
//  - Mientras esta abierto, la camara se pone delante de tu personaje (la misma del probador de la taquilla).
public partial class VistaMesaOnlineMus
{
    private GameObject panelBienvenida, panelCreador;
    private bool creadorAbierto, creadorDeBienvenida, bienvenidaRevisada;
    private TextMeshProUGUI txtTituloCreador, txtSubtituloCreador, txtAvisoCreador, txtBotonListo;
    private TMP_InputField campoNombreCreador;
    private Button botonTiendaCreador;
    private readonly List<(Image Marco, int Indice)> muestrasRopa = new(), muestrasPiel = new(), muestrasPelo = new();
    private readonly List<(Button Boton, TextMeshProUGUI Texto, int Indice)> chipsTrajes = new(), chipsComplementos = new();

    private static readonly Color MarcoElegido = new(1f, 0.8f, 0.3f);
    private static readonly Color MarcoNormal = new(0.1f, 0.06f, 0.03f);

    // ------------------------------------------------------------------ bienvenida

    // Se llama desde ActualizarInterfaz con el menu a la vista.
    private void ComprobarBienvenida()
    {
        if (bienvenidaRevisada || TaquillaMus.PersonajeCreado || DemoActiva)
            return;

        bienvenidaRevisada = true;
        MostrarBienvenida();
    }

    private void MostrarBienvenida()
    {
        if (panelBienvenida == null)
            CrearBienvenida();

        panelBienvenida.SetActive(true);
        panelBienvenida.transform.SetAsLastSibling();
    }

    private void CrearBienvenida()
    {
        panelBienvenida = new GameObject("Bienvenida", typeof(RectTransform), typeof(Image));
        RectTransform raiz = panelBienvenida.GetComponent<RectTransform>();
        raiz.SetParent(raizUI, false);
        Estirar(raiz, 0);
        panelBienvenida.GetComponent<Image>().color = new Color(0.06f, 0.03f, 0.02f, 0.88f);

        GameObject hoja = Panel("Hoja", raiz, "panel_pergamino", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 820));
        hoja.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        Transform h = hoja.transform;

        Etiqueta(h, new Vector2(0, -36), new Vector2(1060, 120), "<size=70%>Mus</size> TINKO", 110, true, TextAlignmentOptions.Center, new Vector2(0.5f, 1f));
        Etiqueta(h, new Vector2(0, -160), new Vector2(1060, 70), "¡Kaixo! Bienvenido a la mesa", 46, true, TextAlignmentOptions.Center, new Vector2(0.5f, 1f));
        Etiqueta(h, new Vector2(0, -236), new Vector2(980, 80),
            "Mus de verdad, con senas, envites y ordagos. Antes de sentarte, vamos a crear a tu personaje.",
            28, false, TextAlignmentOptions.Center, new Vector2(0.5f, 1f));

        (string Titulo, string Texto, Color Pinta)[] puntos =
        {
            ("Crea tu personaje", "Tu color, tu piel, tu pelo, tu traje y tus complementos. Hoy todo es gratis.", new Color(0.89f, 0.70f, 0.24f)),
            ("Juega con quien quieras", "Contra bots para practicar o en salas con tus amigos y tu familia.", new Color(0.72f, 0.2f, 0.16f)),
            ("Gana monedas", "Con ellas compras trajes y complementos nuevos en la TAQUILLA.", new Color(0.18f, 0.42f, 0.24f)),
        };

        for (int i = 0; i < puntos.Length; i++)
        {
            float y = -344 - i * 104;
            Image pinta = Imagen("Pinta", h, "circulo", 56, puntos[i].Pinta);
            Rect(pinta.gameObject, h, new Vector2(0f, 1f), new Vector2(150, y - 8), new Vector2(56, 56));
            Etiqueta(h, new Vector2(230, y), new Vector2(820, 44), puntos[i].Titulo, 32, true, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f));
            Etiqueta(h, new Vector2(230, y - 44), new Vector2(820, 40), puntos[i].Texto, 24, false, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f));
        }

        Button empezar = Boton(h, "verde", "CREAR MI PERSONAJE", new Vector2(520, 96), () =>
        {
            panelBienvenida.SetActive(false);
            AbrirCreador(true);
        }, 36);
        Rect(empezar.gameObject, h, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(520, 96));
    }

    // ------------------------------------------------------------------ creador

    public void AbrirCreador(bool deBienvenida)
    {
        if (panelCreador == null)
            CrearCreador();

        if (panelTaquilla != null)
            panelTaquilla.SetActive(false);

        creadorDeBienvenida = deBienvenida;
        creadorAbierto = true;
        // Se usa el mismo modo que la taquilla: camara de probador y menu escondido.
        taquillaAbierta = true;
        trajeProbador = -1;
        complementoProbador = -1;

        // La primera vez cada uno sale distinto: se empieza con colores al azar.
        if (deBienvenida)
            Azar(false);

        txtTituloCreador.text = deBienvenida ? "Crea tu personaje" : "Mi personaje";
        txtSubtituloCreador.text = deBienvenida
            ? "Elige como quieres ser. <b>Hoy todo es gratis</b>: lo que lleves puesto al terminar se queda contigo."
            : "Los colores son gratis. Trajes y complementos: los tuyos, el resto en la TAQUILLA.";
        txtBotonListo.text = deBienvenida ? "¡A JUGAR!" : "GUARDAR";
        botonTiendaCreador.gameObject.SetActive(!deBienvenida);
        campoNombreCreador.text = NombreLocal;
        txtAvisoCreador.text = "";

        panelCreador.SetActive(true);
        panelCreador.transform.SetAsLastSibling();
        PintarCreador();
    }

    private void CerrarCreador()
    {
        creadorAbierto = false;
        taquillaAbierta = false;
        trajeProbador = -1;
        complementoProbador = -1;

        if (panelCreador != null)
            panelCreador.SetActive(false);
    }

    private void ActualizarCreador()
    {
        if (!creadorAbierto)
            return;

        // Si empieza una partida con el creador abierto, se cierra (lo elegido ya esta guardado).
        if (partida.IsSpawned && partida.PartidaEmpezada)
        {
            if (creadorDeBienvenida)
            {
                GuardarNombre(campoNombreCreador.text);
                TaquillaMus.TerminarCreacion();
            }

            CerrarCreador();
        }
    }

    private void CrearCreador()
    {
        panelCreador = Panel("Creador", raizUI, "panel_madera", new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(1180, 1020));
        panelCreador.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
        Transform p = panelCreador.transform;

        txtTituloCreador = TextoEn(p, 40, 14, 1100, 86, "", 66, Oro, true, TextAlignmentOptions.MidlineLeft);
        txtSubtituloCreador = TextoEn(p, 40, 100, 1100, 44, "", 23, Crema, false, TextAlignmentOptions.MidlineLeft);

        // Columna izquierda: nombre y colores.
        TextoEn(p, 40, 156, 520, 36, "Tu nombre", 28, Crema, true, TextAlignmentOptions.MidlineLeft);
        campoNombreCreador = Campo(p, Vector2.zero, new Vector2(520, 68), "Como te llaman en la mesa", NombreLocal);
        Rect(campoNombreCreador.gameObject, p, new Vector2(0f, 1f), new Vector2(40, -196), new Vector2(520, 68));
        campoNombreCreador.characterLimit = PartidaOnlineMus.LargoMaximoNombre;

        TextoEn(p, 40, 284, 520, 36, "Color de la ropa", 28, Crema, true, TextAlignmentOptions.MidlineLeft);
        CrearMuestras(p, muestrasRopa, TaquillaMus.ColoresRopa, 40, 324, 8, i => { TaquillaMus.RopaMia = i; PintarCreador(); });

        TextoEn(p, 40, 470, 520, 36, "Piel", 28, Crema, true, TextAlignmentOptions.MidlineLeft);
        CrearMuestras(p, muestrasPiel, TaquillaMus.TonosPiel, 40, 510, 8, i => { TaquillaMus.PielMia = i; PintarCreador(); });

        TextoEn(p, 40, 590, 520, 36, "Pelo", 28, Crema, true, TextAlignmentOptions.MidlineLeft);
        CrearMuestras(p, muestrasPelo, TaquillaMus.ColoresPelo, 40, 630, 8, i => { TaquillaMus.PeloMio = i; PintarCreador(); });

        txtAvisoCreador = TextoEn(p, 40, 714, 520, 150, "", 24, new Color(1f, 0.85f, 0.45f), false, TextAlignmentOptions.TopLeft);

        // Columna derecha: trajes y complementos.
        TextoEn(p, 600, 156, 540, 36, "Traje", 28, Crema, true, TextAlignmentOptions.MidlineLeft);

        for (int i = 0; i < TaquillaMus.Trajes.Length; i++)
        {
            int indice = i;
            ArticuloTaquilla a = TaquillaMus.Buscar(CategoriaTaquilla.Traje, i);
            Button b = BotonAbs(p, 600 + (i % 2) * 274, 196 + (i / 2) * 58, 264, 50, "gris", a != null ? a.Nombre : "Traje " + i, () => ElegirTraje(indice), 20);
            chipsTrajes.Add((b, b.GetComponentInChildren<TextMeshProUGUI>(true), i));
        }

        float yComp = 196 + ((TaquillaMus.Trajes.Length + 1) / 2) * 58 + 16;
        TextoEn(p, 600, yComp, 540, 36, "Complementos", 28, Crema, true, TextAlignmentOptions.MidlineLeft);

        for (int i = 0; i < TaquillaMus.Complementos.Length; i++)
        {
            int indice = i;
            ArticuloTaquilla a = TaquillaMus.Buscar(CategoriaTaquilla.Complemento, i);
            Button b = BotonAbs(p, 600 + (i % 3) * 182, yComp + 40 + (i / 3) * 56, 174, 48, "gris", a != null ? a.Nombre : TaquillaMus.Complementos[i].Nombre,
                () => ElegirComplemento(indice), 18);
            chipsComplementos.Add((b, b.GetComponentInChildren<TextMeshProUGUI>(true), i));
        }

        // Abajo: al azar, ir a la tienda y terminar.
        BotonAbs(p, 40, 914, 250, 80, "gris", "AL AZAR", () => { Azar(creadorDeBienvenida); PintarCreador(); }, 28);
        botonTiendaCreador = BotonAbs(p, 306, 914, 250, 80, "morado", "TAQUILLA", () =>
        {
            GuardarNombre(campoNombreCreador.text);
            CerrarCreador();
            AbrirTaquilla();
        }, 28);
        Button listo = BotonAbs(p, 760, 904, 380, 96, "verde", "¡A JUGAR!", TerminarCreador, 36);
        txtBotonListo = listo.GetComponentInChildren<TextMeshProUGUI>(true);

        panelCreador.SetActive(false);
    }

    private void CrearMuestras(Transform padre, List<(Image, int)> lista, Color[] colores, float x, float y, int porFila, System.Action<int> alElegir)
    {
        for (int i = 0; i < colores.Length; i++)
        {
            int indice = i;
            GameObject go = new("Muestra", typeof(RectTransform), typeof(Image), typeof(Button));
            Rect(go, padre, new Vector2(0f, 1f), new Vector2(x + (i % porFila) * 65, -(y + (i / porFila) * 66)), new Vector2(58, 58));
            Image marco = go.GetComponent<Image>();
            marco.sprite = SpriteUI("circulo", 0);
            marco.color = MarcoNormal;
            go.GetComponent<Button>().onClick.AddListener(() => alElegir(indice));

            Image relleno = Imagen("Color", go.transform, "circulo", 46, colores[i]);
            relleno.rectTransform.anchoredPosition = Vector2.zero;
            lista.Add((marco, i));
        }
    }

    private TextMeshProUGUI TextoEn(Transform padre, float x, float y, float w, float h, string texto, float tam, Color color, bool titulo, TextAlignmentOptions alineacion)
    {
        GameObject go = new("Texto", typeof(RectTransform));
        Rect(go, padre, new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));
        TextMeshProUGUI t = Texto(go.transform, texto, tam, color, titulo, alineacion);
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        return t;
    }

    private Button BotonAbs(Transform padre, float x, float y, float w, float h, string color, string texto, UnityAction accion, float tam)
    {
        Button b = Boton(padre, color, texto, new Vector2(w, h), accion, tam);
        Rect(b.gameObject, padre, new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));
        return b;
    }

    // Lo que puedes ponerte: en la bienvenida, todo; despues, lo que tengas.
    private bool PuedePonerse(CategoriaTaquilla cat, int indice)
    {
        if (creadorDeBienvenida)
            return true;

        ArticuloTaquilla a = TaquillaMus.Buscar(cat, indice);
        return a == null || TaquillaMus.Tiene(a);
    }

    private void ElegirTraje(int indice)
    {
        if (!PuedePonerse(CategoriaTaquilla.Traje, indice))
        {
            // No lo tienes: te lo pruebas para verlo, pero no se guarda.
            trajeProbador = indice;
            ArticuloTaquilla a = TaquillaMus.Buscar(CategoriaTaquilla.Traje, indice);
            txtAvisoCreador.text = "Te estas probando <b>" + a.Nombre + "</b>. Lo tienes en la TAQUILLA por " + MonederoMus.Formato(a.Precio) + " monedas.";
            return;
        }

        trajeProbador = -1;
        TaquillaMus.CambiarTrajeLibre(indice);
        txtAvisoCreador.text = "";
        PintarCreador();
    }

    private void ElegirComplemento(int indice)
    {
        bool puesto = (TaquillaMus.ComplementosMios & (1 << indice)) != 0;

        if (!puesto && !PuedePonerse(CategoriaTaquilla.Complemento, indice))
        {
            complementoProbador = indice;
            ArticuloTaquilla a = TaquillaMus.Buscar(CategoriaTaquilla.Complemento, indice);
            txtAvisoCreador.text = "Te estas probando <b>" + a.Nombre + "</b>. Lo tienes en la TAQUILLA por " + MonederoMus.Formato(a.Precio) + " monedas.";
            return;
        }

        complementoProbador = -1;
        TaquillaMus.CambiarComplementoLibre(indice, !puesto);
        txtAvisoCreador.text = "";
        PintarCreador();
    }

    private void Azar(bool tambienRopa)
    {
        TaquillaMus.RopaMia = Random.Range(0, TaquillaMus.ColoresRopa.Length);
        TaquillaMus.PielMia = Random.Range(0, TaquillaMus.TonosPiel.Length);
        TaquillaMus.PeloMio = Random.Range(0, TaquillaMus.ColoresPelo.Length);

        if (!tambienRopa)
            return;

        // En la bienvenida tambien un traje y uno o dos complementos (luego cada uno lo cambia).
        TaquillaMus.CambiarTrajeLibre(Random.value < 0.4f ? 0 : Random.Range(1, TaquillaMus.Trajes.Length));
        for (int i = 0; i < TaquillaMus.Complementos.Length; i++)
            TaquillaMus.CambiarComplementoLibre(i, false);

        int cuantos = Random.Range(1, 3);

        for (int n = 0; n < cuantos; n++)
            TaquillaMus.CambiarComplementoLibre(Random.Range(0, TaquillaMus.Complementos.Length), true);
    }

    private void PintarCreador()
    {
        foreach ((Image marco, int i) in muestrasRopa)
            Marcar(marco, i == TaquillaMus.RopaMia);
        foreach ((Image marco, int i) in muestrasPiel)
            Marcar(marco, i == TaquillaMus.PielMia);
        foreach ((Image marco, int i) in muestrasPelo)
            Marcar(marco, i == TaquillaMus.PeloMio);

        int traje = TaquillaMus.Equipado(CategoriaTaquilla.Traje);

        foreach ((Button b, TextMeshProUGUI t, int i) in chipsTrajes)
        {
            ArticuloTaquilla a = TaquillaMus.Buscar(CategoriaTaquilla.Traje, i);
            bool tiene = PuedePonerse(CategoriaTaquilla.Traje, i);
            PintarPestana(b, i == traje);
            t.text = a == null ? "" : tiene ? a.Nombre : a.Nombre + " <size=75%>· " + MonederoMus.Formato(a.Precio) + "</size>";
            b.GetComponent<Image>().color = tiene ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }

        int mascara = TaquillaMus.ComplementosMios;

        foreach ((Button b, TextMeshProUGUI t, int i) in chipsComplementos)
        {
            ArticuloTaquilla a = TaquillaMus.Buscar(CategoriaTaquilla.Complemento, i);
            bool tiene = PuedePonerse(CategoriaTaquilla.Complemento, i);
            PintarPestana(b, (mascara & (1 << i)) != 0);
            t.text = a == null ? "" : tiene ? a.Nombre : a.Nombre + " <size=75%>· " + MonederoMus.Formato(a.Precio) + "</size>";
            b.GetComponent<Image>().color = tiene ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }
    }

    private static void Marcar(Image marco, bool elegido)
    {
        marco.color = elegido ? MarcoElegido : MarcoNormal;
        marco.rectTransform.localScale = Vector3.one * (elegido ? 1.12f : 1f);
    }

    private void TerminarCreador()
    {
        string nombre = PartidaOnlineMus.LimpiarNombre(campoNombreCreador.text);

        if (nombre.Length == 0)
        {
            txtAvisoCreador.text = "<color=#FF8A7A>Ponte un nombre para que te reconozcan en la mesa.</color>";
            return;
        }

        GuardarNombre(nombre);

        if (campoNombreUI != null)
            campoNombreUI.text = nombre;

        if (creadorDeBienvenida)
        {
            TaquillaMus.TerminarCreacion();
            CerrarCreador();
            MostrarAvisoMonedas("¡Kaixo, " + nombre + "! Tienes " + MonederoMus.Formato(MonederoMus.Saldo) + " monedas para empezar.");
            return;
        }

        CerrarCreador();
        MostrarAvisoMonedas("Personaje guardado.");
    }
}
