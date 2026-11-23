using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// MENU: SALAS POR INTERNET
//  - CREAR SALA: tu sala sale en la lista de todos los jugadores.
//  - La lista se actualiza sola cada pocos segundos; ENTRAR te mete en la sala y eliges silla.
//  - Para empezar hacen falta al menos 2 personas; las sillas libres las ocupan bots.
// La otra pestana ("WIFI Y MAS") tiene la partida en la misma WiFi, la IP a mano y el modo demo.
public partial class VistaMesaOnlineMus
{
    private GameObject pestanaInternetUI, pestanaMasUI;
    private Button botonPestanaInternet, botonPestanaMas;
#if !MUS_TINKO_SIN_INTERNET
    private bool pestanaInternetActiva = true;
#else
    private bool pestanaInternetActiva;
#endif

    private GameObject Contenedor(string nombre, Transform padre)
    {
        GameObject go = new(nombre, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        Estirar(rt, 0);
        return go;
    }

    private void PintarPestana(Button b, bool activa)
    {
        string color = activa ? "naranja" : "gris";
        Image img = b.GetComponent<Image>();
        img.sprite = SpriteUI("boton_" + color, 22);
        b.spriteState = new SpriteState { pressedSprite = SpriteUI("boton_" + color + "_pulsado", 22) };
        b.transform.localScale = activa ? Vector3.one : new Vector3(0.94f, 0.94f, 1f);
    }

    // Se llama cada fotograma mientras se ve el menu.
    private void ActualizarMenuPestanas()
    {
        if (pestanaInternetUI == null)
            return;

        pestanaInternetUI.SetActive(pestanaInternetActiva);
        pestanaMasUI.SetActive(!pestanaInternetActiva);
        PintarPestana(botonPestanaInternet, pestanaInternetActiva);
        PintarPestana(botonPestanaMas, !pestanaInternetActiva);

        if (pestanaInternetActiva)
            ActualizarPestanaInternet();
        else
            ActualizarBusquedaLan();
    }

    // Boton SALIR de la sala (antes de empezar la partida).
    private void SalirDeLaSala()
    {
#if !MUS_TINKO_SIN_INTERNET
        if (SalaInternet() != null && SalaInternet().EnSala)
        {
            SalaInternet().Salir();
            return;
        }
#endif
        conexion?.Salir();
    }

    private bool EnSalaPorInternet
    {
        get
        {
#if !MUS_TINKO_SIN_INTERNET
            return SalaInternet() != null && SalaInternet().EnSala;
#else
            return false;
#endif
        }
    }

#if !MUS_TINKO_SIN_INTERNET
    private const int FilasSalas = 4;
    private readonly GameObject[] filasSalas = new GameObject[FilasSalas];
    private readonly TextMeshProUGUI[] txtFilasSalas = new TextMeshProUGUI[FilasSalas];
    private readonly Button[] botonesFilasSalas = new Button[FilasSalas];
    private readonly TextMeshProUGUI[] txtBotonesFilasSalas = new TextMeshProUGUI[FilasSalas];
    private readonly string[] idsFilasSalas = new string[FilasSalas];
    private Button botonCrearSalaUI, botonActualizarSalas;
    private TextMeshProUGUI txtInfoSalas, txtActualizarSalas;
    private float siguienteBusquedaSalas;
#endif

    private void CrearPestanaInternet(Transform padre, float y)
    {
#if !MUS_TINKO_SIN_INTERNET
        botonCrearSalaUI = Boton(padre, "verde", "CREAR SALA", new Vector2(560, 70), () =>
        {
            GuardarNombre(campoNombreUI.text);
            SalaInternet()?.CrearSala(NombreLocal, 0);
        }, 30);
        Rect(botonCrearSalaUI.gameObject, padre, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(560, 70));

        y -= 82;
        GameObject cabecera = new("CabeceraSalas", typeof(RectTransform));
        Rect(cabecera, padre, new Vector2(0.5f, 1f), new Vector2(-100, y), new Vector2(420, 44));
        TextMeshProUGUI tc = Texto(cabecera.transform, "SALAS ABIERTAS  <size=60%><color=#FFF2D6>(minimo 2 jugadores)</color></size>", 32, Oro, true,
            TextAlignmentOptions.MidlineLeft);
        tc.enableWordWrapping = false;

        botonActualizarSalas = Boton(padre, "gris", "ACTUALIZAR", new Vector2(190, 44), () => siguienteBusquedaSalas = 0f, 20);
        Rect(botonActualizarSalas.gameObject, padre, new Vector2(0.5f, 1f), new Vector2(205, y), new Vector2(190, 44));
        txtActualizarSalas = botonActualizarSalas.GetComponentInChildren<TextMeshProUGUI>();

        y -= 54;
        GameObject info = new("InfoSalas", typeof(RectTransform));
        Rect(info, padre, new Vector2(0.5f, 1f), new Vector2(0, y - 20), new Vector2(620, 150));
        txtInfoSalas = Texto(info.transform, "", 24, Crema);

        for (int i = 0; i < FilasSalas; i++)
        {
            int indice = i;
            GameObject fila = new("Sala_" + i, typeof(RectTransform), typeof(Image));
            Rect(fila, padre, new Vector2(0.5f, 1f), new Vector2(0, y - i * 64), new Vector2(640, 58));
            Image fondo = fila.GetComponent<Image>();
            fondo.sprite = SpriteUI("barra_fondo", 8);
            fondo.type = Image.Type.Sliced;
            fondo.raycastTarget = false;

            GameObject texto = new("Texto", typeof(RectTransform));
            Rect(texto, fila.transform, new Vector2(0f, 0.5f), new Vector2(16, 0), new Vector2(450, 56)).pivot = new Vector2(0f, 0.5f);
            txtFilasSalas[i] = Texto(texto.transform, "", 22, Crema, false, TextAlignmentOptions.MidlineLeft);
            txtFilasSalas[i].rectTransform.offsetMin = txtFilasSalas[i].rectTransform.offsetMax = Vector2.zero;
            txtFilasSalas[i].enableAutoSizing = true;
            txtFilasSalas[i].fontSizeMin = 14;
            txtFilasSalas[i].fontSizeMax = 22;

            botonesFilasSalas[i] = Boton(fila.transform, "verde", "ENTRAR", new Vector2(160, 48), () => EntrarEnSala(indice), 22);
            Rect(botonesFilasSalas[i].gameObject, fila.transform, new Vector2(1f, 0.5f), new Vector2(-8, 0), new Vector2(160, 48));
            txtBotonesFilasSalas[i] = botonesFilasSalas[i].GetComponentInChildren<TextMeshProUGUI>();
            filasSalas[i] = fila;
            fila.SetActive(false);
        }
#else
        GameObject info = new("InfoSalas", typeof(RectTransform));
        Rect(info, padre, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(620, 160));
        Texto(info.transform, "Las salas por Internet estan desactivadas.\n<size=80%>(Mus TINKO > Internet > Activar salas por Internet)</size>", 24, Crema);
#endif
    }

    private void ActualizarPestanaInternet()
    {
#if !MUS_TINKO_SIN_INTERNET
        SalaInternetMus s = SalaInternet();

        if (botonCrearSalaUI == null)
            return;

        if (s == null)
        {
            txtInfoSalas.text = "Falta SalaInternetMus en la escena.";
            return;
        }

        bool ocupado = s.Ocupado;
        botonCrearSalaUI.interactable = !ocupado && !s.EnSala;
        botonActualizarSalas.interactable = !ocupado && !s.Buscando;
        txtActualizarSalas.text = s.Buscando ? "BUSCANDO..." : "ACTUALIZAR";

        // La lista se refresca sola.
        if (!ocupado && !s.EnSala && !s.Buscando && Time.unscaledTime >= siguienteBusquedaSalas)
        {
            siguienteBusquedaSalas = Time.unscaledTime + 6f;
            s.BuscarSalas();
        }

        IReadOnlyList<SalaInternetMus.SalaPublica> lista = s.Salas;
        int filas = ocupado || s.Busquedas == 0 ? 0 : System.Math.Min(FilasSalas, lista.Count);

        for (int i = 0; i < FilasSalas; i++)
        {
            bool hay = i < filas;
            filasSalas[i].SetActive(hay);
            idsFilasSalas[i] = hay ? lista[i].Id : null;

            if (!hay)
                continue;

            SalaInternetMus.SalaPublica sala = lista[i];
            string entrada = sala.Apuesta > 0 ? "entrada " + sala.Apuesta + " monedas" : "sin apuesta";
            txtFilasSalas[i].text = "<b>" + sala.Nombre + "</b>\n<size=80%>" + sala.Jugadores + "/" + SalaInternetMus.MaxJugadores +
                                    " jugadores  ·  " + entrada + "</size>";
            bool puede = MonederoMus.PuedePagar(sala.Apuesta);
            botonesFilasSalas[i].interactable = puede;
            txtBotonesFilasSalas[i].text = puede ? "ENTRAR" : "SIN MONEDAS";
        }

        if (ocupado)
            txtInfoSalas.text = s.Estado;
        else if (filas > 0)
            txtInfoSalas.text = "";
        else if (!string.IsNullOrEmpty(s.Estado))
            txtInfoSalas.text = "<color=#FFB080>" + s.Estado + "</color>";
        else if (s.Busquedas == 0 || s.Buscando)
            txtInfoSalas.text = "Buscando salas...";
        else
            txtInfoSalas.text = "No hay salas abiertas ahora mismo.\n<size=85%>¡Crea la tuya! Saldra aqui para que entren los demas.</size>";
#endif
    }

#if !MUS_TINKO_SIN_INTERNET
    private void EntrarEnSala(int indice)
    {
        string id = idsFilasSalas[indice];

        if (string.IsNullOrEmpty(id))
            return;

        GuardarNombre(campoNombreUI.text);
        SalaInternet()?.Unirse(id);
    }
#endif
}
