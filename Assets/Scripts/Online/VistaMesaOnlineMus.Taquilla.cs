using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TAQUILLA: tienda de trajes, reversos, colores de nombre y frases, pagando con monedas.
// Mientras miras un traje, el personaje de tu derecha se lo prueba (probador).
public partial class VistaMesaOnlineMus
{
    private const int SitioProbador = 1;

    private GameObject panelTaquilla;
    private Transform rejillaTaquilla;
    private TextMeshProUGUI txtSaldoTaquilla, txtMensajeTaquilla;
    private readonly Dictionary<CategoriaTaquilla, Button> pestanasTaquilla = new();
    private CategoriaTaquilla categoriaTaquilla = CategoriaTaquilla.Traje;
    private bool taquillaAbierta;
    private int saldoTaquillaMostrado = -1;

    public void AbrirTaquilla()
    {
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

        if (panelTaquilla != null)
            panelTaquilla.SetActive(false);
    }

    private void CrearTaquilla()
    {
        panelTaquilla = Panel("Taquilla", raizUI, "panel_madera", new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(1160, 1000));
        panelTaquilla.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

        GameObject titulo = new("Titulo", typeof(RectTransform));
        Rect(titulo, panelTaquilla.transform, new Vector2(0f, 1f), new Vector2(40, -14), new Vector2(520, 100));
        Texto(titulo.transform, "TAQUILLA", 84, Oro, true, TextAlignmentOptions.Left);

        Icono(panelTaquilla.transform, new Vector2(1f, 1f), new Vector2(-470, -26), 70);
        GameObject saldo = new("Saldo", typeof(RectTransform));
        Rect(saldo, panelTaquilla.transform, new Vector2(1f, 1f), new Vector2(-390, -22), new Vector2(220, 80));
        txtSaldoTaquilla = Texto(saldo.transform, "", 48, Oro, true, TextAlignmentOptions.Left);

        Button cerrar = Boton(panelTaquilla.transform, "rojo", "CERRAR", new Vector2(160, 66), CerrarTaquilla, 26);
        Rect(cerrar.gameObject, panelTaquilla.transform, new Vector2(1f, 1f), new Vector2(-110, -30), new Vector2(160, 66));

        GameObject pestanas = new("Pestanas", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        Rect(pestanas, panelTaquilla.transform, new Vector2(0.5f, 1f), new Vector2(0, -126), new Vector2(1088, 66));
        HorizontalLayoutGroup h = pestanas.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 12;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;

        AnadirPestana(pestanas.transform, CategoriaTaquilla.Traje, "TRAJES");
        AnadirPestana(pestanas.transform, CategoriaTaquilla.Reverso, "REVERSOS");
        AnadirPestana(pestanas.transform, CategoriaTaquilla.ColorNombre, "COLOR NOMBRE");
        AnadirPestana(pestanas.transform, CategoriaTaquilla.Frases, "FRASES");

        // Zona con scroll para los articulos.
        GameObject vista = new("Vista", typeof(RectTransform), typeof(RectMask2D));
        RectTransform rv = vista.GetComponent<RectTransform>();
        rv.SetParent(panelTaquilla.transform, false);
        rv.anchorMin = new Vector2(0, 0);
        rv.anchorMax = new Vector2(1, 1);
        rv.offsetMin = new Vector2(36, 80);
        rv.offsetMax = new Vector2(-36, -214);

        GameObject contenido = new("Contenido", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        RectTransform rc = contenido.GetComponent<RectTransform>();
        rc.SetParent(vista.transform, false);
        rc.anchorMin = new Vector2(0, 1);
        rc.anchorMax = new Vector2(1, 1);
        rc.pivot = new Vector2(0.5f, 1f);
        rc.anchoredPosition = Vector2.zero;
        rc.sizeDelta = Vector2.zero;
        GridLayoutGroup g = contenido.GetComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(350, 330);
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
        Rect(mensaje, panelTaquilla.transform, new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(1080, 50));
        txtMensajeTaquilla = Texto(mensaje.transform, "", 26, Crema);

        panelTaquilla.SetActive(false);
    }

    private void AnadirPestana(Transform padre, CategoriaTaquilla cat, string texto)
    {
        Button b = Boton(padre, "azul", texto, new Vector2(262, 62), () => MostrarCategoria(cat), 26);
        pestanasTaquilla[cat] = b;
    }

    private void MostrarCategoria(CategoriaTaquilla cat)
    {
        categoriaTaquilla = cat;

        if (cat != CategoriaTaquilla.Traje)
            trajeProbador = -1;

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

        if (cat == CategoriaTaquilla.Traje)
            MensajeTaquilla("Toca un traje para probartelo: lo lleva el personaje de tu derecha.", false);
        else if (cat == CategoriaTaquilla.Reverso)
            MensajeTaquilla("El reverso es lo que ves por detras de las cartas de los demas.", false);
        else if (cat == CategoriaTaquilla.ColorNombre)
            MensajeTaquilla("Tu nombre sale con este color en la sala y en el chat.", false);
        else
            MensajeTaquilla("Las frases rapidas del chat cambian por las del pack que lleves.", false);
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

        if (a.Categoria == CategoriaTaquilla.Traje)
            tarjeta.GetComponent<Button>().onClick.AddListener(() =>
            {
                trajeProbador = a.Indice;
                MensajeTaquilla("Probando: " + a.Nombre + " (mira al personaje de tu derecha)", false);
            });

        GameObject nombre = new("Nombre", typeof(RectTransform));
        Rect(nombre, tarjeta.transform, new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(330, 56));
        Texto(nombre.transform, a.Nombre, 40, Tinta, true, TextAlignmentOptions.Center, false);

        GameObject muestra = new("Muestra", typeof(RectTransform));
        Rect(muestra, tarjeta.transform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(320, 120));
        CrearMuestra(a, muestra.transform);

        GameObject descripcion = new("Descripcion", typeof(RectTransform));
        Rect(descripcion, tarjeta.transform, new Vector2(0.5f, 1f), new Vector2(0, -192), new Vector2(320, 54));
        Texto(descripcion.transform, a.Descripcion, 19, Tinta, false, TextAlignmentOptions.Center, false);

        bool tiene = TaquillaMus.Tiene(a);
        string color = puesto ? "gris" : tiene ? "verde" : "dorado";
        string texto = puesto ? "LO LLEVAS" : tiene ? "PONER" : a.Precio + " MONEDAS";

        Button boton = Boton(tarjeta.transform, color, texto, new Vector2(300, 62), () => PulsarArticulo(a), 24);
        Rect(boton.gameObject, tarjeta.transform, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(300, 62));
        boton.interactable = !puesto;
    }

    private void CrearMuestra(ArticuloTaquilla a, Transform padre)
    {
        switch (a.Categoria)
        {
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
                    Rect(m, padre, new Vector2(0f, 0.5f), new Vector2(14 + i * 50, 20), new Vector2(42, 42));
                    Image img = m.GetComponent<Image>();
                    img.color = colores[i];
                    img.raycastTarget = false;
                }

                GameObject leyenda = new("Leyenda", typeof(RectTransform));
                Rect(leyenda, padre, new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(320, 34));
                Texto(leyenda.transform, "gorro · pompon · manos · botones · pantalon · pelo", 14, Tinta, false, TextAlignmentOptions.Center, false);
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
            default:
                Texto(padre, string.Join("   ", TaquillaMus.PacksFrases[a.Indice]), 20, Tinta, false, TextAlignmentOptions.Center, false);
                break;
        }
    }

    private void PulsarArticulo(ArticuloTaquilla a)
    {
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

        if (a.Categoria == CategoriaTaquilla.Traje)
            trajeProbador = -1;

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
