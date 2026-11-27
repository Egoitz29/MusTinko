using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TAQUILLA: eliges tu personaje y le compras trajes, complementos, reversos y color de nombre.
// Con la taquilla abierta la camara se pone delante de TU personaje (probador): lo que tocas, te lo pruebas.
public partial class VistaMesaOnlineMus
{
    private GameObject panelTaquilla;
    private Transform rejillaTaquilla;
    private TextMeshProUGUI txtSaldoTaquilla, txtMensajeTaquilla;
    private readonly Dictionary<CategoriaTaquilla, Button> pestanasTaquilla = new();
    private CategoriaTaquilla categoriaTaquilla = CategoriaTaquilla.Traje;
    private bool taquillaAbierta;
    private int saldoTaquillaMostrado = -1;

    public void AbrirTaquilla()
    {
        if (creadorAbierto)
            CerrarCreador();

        if (panelTaquilla == null)
            CrearTaquilla();

        taquillaAbierta = true;
        panelTaquilla.SetActive(true);
        panelTaquilla.transform.SetAsLastSibling();
        MostrarCategoria(categoriaTaquilla);
    }

    public void CerrarTaquilla()
    {
        taquillaAbierta = false;
        trajeProbador = -1;
        complementoProbador = -1;

        if (panelTaquilla != null)
            panelTaquilla.SetActive(false);
    }

    private void CrearTaquilla()
    {
        panelTaquilla = Panel("Taquilla", raizUI, "panel_madera", new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(1180, 1020));
        panelTaquilla.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

        GameObject titulo = new("Titulo", typeof(RectTransform));
        Rect(titulo, panelTaquilla.transform, new Vector2(0f, 1f), new Vector2(40, -14), new Vector2(500, 100));
        Texto(titulo.transform, "TAQUILLA", 84, Oro, true, TextAlignmentOptions.Left);

        // Saldo pegado a la derecha y la moneda detras (antes la moneda caia encima de las cifras).
        GameObject saldo = new("Saldo", typeof(RectTransform));
        Rect(saldo, panelTaquilla.transform, new Vector2(1f, 1f), new Vector2(-376, -22), new Vector2(210, 80));
        txtSaldoTaquilla = Texto(saldo.transform, "", 48, Oro, true, TextAlignmentOptions.Right);
        Icono(panelTaquilla.transform, new Vector2(1f, 1f), new Vector2(-292, -26), 70);

        Button cerrar = Boton(panelTaquilla.transform, "rojo", "CERRAR", new Vector2(160, 66), CerrarTaquilla, 26);
        Rect(cerrar.gameObject, panelTaquilla.transform, new Vector2(1f, 1f), new Vector2(-110, -30), new Vector2(160, 66));

        GameObject pestanas = new("Pestanas", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        Rect(pestanas, panelTaquilla.transform, new Vector2(0.5f, 1f), new Vector2(0, -126), new Vector2(1108, 70));
        HorizontalLayoutGroup h = pestanas.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 12;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;

        // Ya no hay personajes que comprar: MI PERSONAJE abre el creador (colores gratis, lo tuyo).
        Boton(pestanas.transform, "verde", "MI PERSONAJE", new Vector2(240, 66), () =>
        {
            CerrarTaquilla();
            AbrirCreador(false);
        }, 26);
        AnadirPestana(pestanas.transform, CategoriaTaquilla.Traje, "TRAJES");
        AnadirPestana(pestanas.transform, CategoriaTaquilla.Complemento, "EXTRAS");
        AnadirPestana(pestanas.transform, CategoriaTaquilla.Reverso, "CARTAS");
        AnadirPestana(pestanas.transform, CategoriaTaquilla.ColorNombre, "NOMBRE");

        // Zona con scroll para los articulos.
        GameObject vista = new("Vista", typeof(RectTransform), typeof(RectMask2D));
        RectTransform rv = vista.GetComponent<RectTransform>();
        rv.SetParent(panelTaquilla.transform, false);
        rv.anchorMin = new Vector2(0, 0);
        rv.anchorMax = new Vector2(1, 1);
        rv.offsetMin = new Vector2(36, 80);
        rv.offsetMax = new Vector2(-36, -218);

        GameObject contenido = new("Contenido", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        RectTransform rc = contenido.GetComponent<RectTransform>();
        rc.SetParent(vista.transform, false);
        rc.anchorMin = new Vector2(0, 1);
        rc.anchorMax = new Vector2(1, 1);
        rc.pivot = new Vector2(0.5f, 1f);
        rc.anchoredPosition = Vector2.zero;
        rc.sizeDelta = Vector2.zero;
        GridLayoutGroup g = contenido.GetComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(356, 340);
        g.spacing = new Vector2(18, 18);
        g.childAlignment = TextAnchor.UpperCenter;
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 3;
        contenido.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rejillaTaquilla = contenido.transform;

        ScrollRect scroll = panelTaquilla.AddComponent<ScrollRect>();
        scroll.viewport = rv;
        scroll.content = rc;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject mensaje = new("Mensaje", typeof(RectTransform));
        Rect(mensaje, panelTaquilla.transform, new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(1100, 56));
        txtMensajeTaquilla = Texto(mensaje.transform, "", 30, Crema);

        panelTaquilla.SetActive(false);
    }

    private void AnadirPestana(Transform padre, CategoriaTaquilla cat, string texto)
    {
        Button b = Boton(padre, "azul", texto, new Vector2(196, 66), () => MostrarCategoria(cat), 28);
        pestanasTaquilla[cat] = b;
    }

    private void MostrarCategoria(CategoriaTaquilla cat)
    {
        categoriaTaquilla = cat;
        trajeProbador = -1;
        complementoProbador = -1;

        foreach (KeyValuePair<CategoriaTaquilla, Button> par in pestanasTaquilla)
        {
            bool activa = par.Key == cat;
            par.Value.transform.localScale = Vector3.one * (activa ? 1.06f : 0.94f);
            par.Value.image.color = activa ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }

        for (int i = rejillaTaquilla.childCount - 1; i >= 0; i--)
            Destroy(rejillaTaquilla.GetChild(i).gameObject);

        foreach (ArticuloTaquilla a in TaquillaMus.Catalogo)
            if (a.Categoria == cat)
                CrearTarjeta(a);

        if (cat == CategoriaTaquilla.Traje || cat == CategoriaTaquilla.Complemento)
            MensajeTaquilla("Toca una tarjeta para probartelo: miralo en tu personaje.", false);
        else if (cat == CategoriaTaquilla.Reverso)
            MensajeTaquilla("Es la parte de atras de tus cartas: mira las que tiene en la mano.", false);
        else
            MensajeTaquilla("Tu nombre sale con este color en la sala y en el chat.", false);
    }

    private void CrearTarjeta(ArticuloTaquilla a)
    {
        GameObject tarjeta = new("Articulo_" + a.Id, typeof(RectTransform), typeof(Image), typeof(Button));
        tarjeta.transform.SetParent(rejillaTaquilla, false);
        Image fondo = tarjeta.GetComponent<Image>();
        fondo.sprite = SpriteUI("panel_pergamino", 30);
        fondo.type = Image.Type.Sliced;
        bool puesto = TaquillaMus.EstaEquipado(a);

        if (puesto)
            fondo.color = new Color(1f, 0.93f, 0.7f);

        tarjeta.GetComponent<Button>().onClick.AddListener(() => Probar(a));

        GameObject nombre = new("Nombre", typeof(RectTransform));
        Rect(nombre, tarjeta.transform, new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(300, 60));
        Texto(nombre.transform, a.Nombre, 40, Tinta, true, TextAlignmentOptions.Center, false);

        // Sello en la esquina: PUESTO (lo llevas) o TUYO (comprado).
        if (puesto || TaquillaMus.Tiene(a))
        {
            GameObject sello = new("Sello", typeof(RectTransform), typeof(Image));
            Rect(sello, tarjeta.transform, new Vector2(1f, 1f), new Vector2(8, 10), new Vector2(112, 36));
            sello.transform.localEulerAngles = new Vector3(0f, 0f, -12f);
            Image fondoSello = sello.GetComponent<Image>();
            fondoSello.sprite = SpriteUI("placa", 12);
            fondoSello.type = Image.Type.Sliced;
            fondoSello.color = puesto ? new Color(0.89f, 0.6f, 0.15f) : new Color(0.18f, 0.52f, 0.25f);
            fondoSello.raycastTarget = false;
            Texto(sello.transform, puesto ? "PUESTO" : "TUYO", 18, Color.white, true, TextAlignmentOptions.Center, false);
        }

        GameObject muestra = new("Muestra", typeof(RectTransform));
        Rect(muestra, tarjeta.transform, new Vector2(0.5f, 1f), new Vector2(0, -74), new Vector2(326, 110));
        CrearMuestra(a, muestra.transform);

        GameObject descripcion = new("Descripcion", typeof(RectTransform));
        Rect(descripcion, tarjeta.transform, new Vector2(0.5f, 1f), new Vector2(0, -186), new Vector2(330, 66));
        Texto(descripcion.transform, a.Descripcion, 24, Tinta, false, TextAlignmentOptions.Center, false);

        bool tiene = TaquillaMus.Tiene(a);
        bool quitable = puesto && a.Categoria == CategoriaTaquilla.Complemento;
        string color = quitable ? "rojo" : puesto ? "gris" : tiene ? "verde" : "dorado";
        string texto = quitable ? "QUITAR" : puesto ? "LO LLEVAS" : tiene ? "PONER" : "      " + MonederoMus.Formato(a.Precio);

        Button boton = Boton(tarjeta.transform, color, texto, new Vector2(310, 66), () => PulsarArticulo(a), 30);
        Rect(boton.gameObject, tarjeta.transform, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(310, 66));
        boton.interactable = !puesto || quitable;

        // Precio con su moneda (y en rojo si no te llega).
        if (!tiene)
        {
            Image moneda = Icono(boton.transform, new Vector2(0.5f, 0.5f), Vector2.zero, 40);
            RectTransform rm = moneda.rectTransform;
            rm.pivot = new Vector2(0.5f, 0.5f);
            rm.anchoredPosition = new Vector2(-24f - 9f * MonederoMus.Formato(a.Precio).Length, 2f);

            if (!MonederoMus.PuedePagar(a.Precio))
                boton.GetComponentInChildren<TextMeshProUGUI>(true).color = new Color(1f, 0.72f, 0.66f);
        }
    }

    private void Probar(ArticuloTaquilla a)
    {
        if (a.Categoria == CategoriaTaquilla.Traje)
            trajeProbador = a.Indice;
        else if (a.Categoria == CategoriaTaquilla.Complemento)
            complementoProbador = a.Indice;
        else
            return;

        MensajeTaquilla("Te estas probando: <b>" + a.Nombre + "</b>", false);
    }

    private void CrearMuestra(ArticuloTaquilla a, Transform padre)
    {
        switch (a.Categoria)
        {
            case CategoriaTaquilla.Personaje:
            {
                GameObject m = new("Color", typeof(RectTransform), typeof(Image));
                Rect(m, padre, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96)).pivot = new Vector2(0.5f, 0.5f);
                Image img = m.GetComponent<Image>();
                img.sprite = SpriteUI("moneda", 0);
                img.color = ColoresJugadoresMus.Colores[a.Indice];
                img.raycastTarget = false;
                break;
            }
            case CategoriaTaquilla.Complemento:
                Texto(padre, "Toca para probartelo", 26, new Color(0.45f, 0.3f, 0.15f), false, TextAlignmentOptions.Center, false);
                break;
            case CategoriaTaquilla.Traje:
            {
                TrajeMus t = TaquillaMus.Trajes[a.Indice];
                Color[] colores =
                {
                    t.Gorro ?? new Color(0.75f, 0.7f, 0.85f), t.Pompom ?? new Color(0.97f, 0.94f, 0.86f),
                    t.Manoplas ?? new Color(0.9f, 0.88f, 0.92f), t.Botones ?? new Color(0.95f, 0.76f, 0.28f),
                    t.Piernas ?? new Color(0.26f, 0.23f, 0.3f), t.Pelo ?? new Color(0.36f, 0.22f, 0.13f)
                };

                for (int i = 0; i < colores.Length; i++)
                {
                    GameObject m = new("Color", typeof(RectTransform), typeof(Image));
                    Rect(m, padre, new Vector2(0f, 0.5f), new Vector2(10 + i * 52, 22), new Vector2(46, 46));
                    Image img = m.GetComponent<Image>();
                    img.color = colores[i];
                    img.raycastTarget = false;
                }
                break;
            }
            case CategoriaTaquilla.Reverso:
            {
                Texture2D tex = Resources.Load<Texture2D>(TaquillaMus.Reversos[a.Indice]);
                GameObject m = new("Reverso", typeof(RectTransform), typeof(Image));
                Rect(m, padre, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 120)).pivot = new Vector2(0.5f, 0.5f);
                Image img = m.GetComponent<Image>();
                img.raycastTarget = false;
                img.preserveAspect = true;

                if (tex != null)
                    img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                break;
            }
            case CategoriaTaquilla.ColorNombre:
            {
                string hex = TaquillaMus.ColoresNombre[a.Indice];
                string nombre = NombreLocal;
                Texto(padre, hex.Length == 0 ? "<b>" + nombre + "</b>" : "<b><color=" + hex + ">" + nombre + "</color></b>", 44, Color.white);
                break;
            }

        }
    }

    private void PulsarArticulo(ArticuloTaquilla a)
    {
        if (a.Categoria == CategoriaTaquilla.Complemento && TaquillaMus.EstaEquipado(a))
        {
            TaquillaMus.QuitarComplemento(a);
            complementoProbador = -1;
            MensajeTaquilla("Te has quitado: " + a.Nombre, true);
            MostrarCategoria(categoriaTaquilla);
            return;
        }

        if (TaquillaMus.Tiene(a))
        {
            TaquillaMus.Equipar(a);
            MensajeTaquilla("Te has puesto: " + a.Nombre, true);
        }
        else if (TaquillaMus.Comprar(a))
        {
            MensajeTaquilla("¡Comprado! " + a.Nombre + " (-" + MonederoMus.Formato(a.Precio) + ")", true);
        }
        else
        {
            MensajeTaquilla("<color=#FF6B5E>No tienes monedas suficientes. ¡Gana partidas con entrada!</color>", true);
            return;
        }

        MostrarCategoria(categoriaTaquilla);
    }

    private void MensajeTaquilla(string texto, bool destacado)
    {
        if (txtMensajeTaquilla == null)
            return;

        txtMensajeTaquilla.text = texto;
        txtMensajeTaquilla.fontStyle = destacado ? FontStyles.Bold : FontStyles.Normal;
    }

    private void ActualizarTaquilla()
    {
        if (!taquillaAbierta || panelTaquilla == null)
            return;

        int saldo = MonederoMus.Saldo;

        if (saldo != saldoTaquillaMostrado)
        {
            saldoTaquillaMostrado = saldo;
            txtSaldoTaquilla.text = MonederoMus.Formato(saldo);
        }

        // Si la partida empieza con la taquilla abierta, se cierra.
        if (partida.IsSpawned && partida.PartidaEmpezada)
            CerrarTaquilla();
    }
}
