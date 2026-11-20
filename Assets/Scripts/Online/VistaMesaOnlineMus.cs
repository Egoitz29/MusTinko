using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VistaMesaOnlineMus : MonoBehaviour
{
    private static readonly string[] NombresPuesto = { "LILA", "MOSTAZA", "MENTA", "CORAL" };

    [Header("Partida online")]
    [SerializeField] private PartidaOnlineMus partida;

    [Header("Configuracion automatica")]
    [Tooltip("Desactiva GestorFasesMus, GestorEnvitesMus, MarcadorMus y los bots locales, y copia sus referencias.")]
    [SerializeField] private bool usarMesaLocalExistente = true;

    [Header("Mesa (se rellenan solas si se dejan vacias)")]
    [SerializeField] private RepartoPrueba reparto;
    [SerializeField] private CartaSeleccionable[] cartas = new CartaSeleccionable[4];
    [SerializeField] private ReveladorManosMus revelador;

    [Header("Por sitio de la mesa: 0 tu, 1 derecha, 2 enfrente, 3 izquierda")]
    [SerializeField] private BocadilloMus3D[] bocadillos = new BocadilloMus3D[4];
    [SerializeField] private ControlSenalesMus[] senales = new ControlSenalesMus[4];

    [Header("Botones")]
    [SerializeField] private Button botonMus;
    [SerializeField] private Button botonNoHayMus;
    [SerializeField] private Button botonDescartar;
    [SerializeField] private Button botonPasar;
    [SerializeField] private Button botonEnvidar;
    [SerializeField] private Button botonMas;
    [SerializeField] private Button botonQuiero;
    [SerializeField] private Button botonNoQuiero;
    [SerializeField] private Button botonOrdago;
    [SerializeField] private GameObject panelEnvites;
    [SerializeField] private Button[] botonesSenas = new Button[4];
    [SerializeField] private GameObject[] ocultarEnOnline = new GameObject[0];

    [Header("Textos")]
    [SerializeField] private TMP_Text textoFase;
    [SerializeField] private TMP_Text textoMarcador;
    [SerializeField] private TMP_Text textoAnuncios;
    [SerializeField] private TMP_Text textoEnvite;

    private bool limpiarAlSiguienteEvento;
    private int[] cartasLocalesMostradas = { -1, -1, -1, -1 };
    private bool manosEnMesa;

    private void Awake()
    {
        if (usarMesaLocalExistente)
            AdaptarMesaLocal();

        if (partida == null)
            partida = FindFirstObjectByType<PartidaOnlineMus>();

        if (partida == null || reparto == null)
        {
            Debug.LogError("VistaMesaOnlineMus: falta PartidaOnlineMus o RepartoPrueba en la escena.", this);
            enabled = false;
            return;
        }

        reparto.DesactivarRepartoInicial();

        Conectar(botonMus, () => Pedir(AccionMus.Mus));
        Conectar(botonNoHayMus, () => Pedir(AccionMus.NoHayMus));
        Conectar(botonDescartar, Descartar);
        Conectar(botonPasar, () => Pedir(AccionMus.Pasar));
        Conectar(botonEnvidar, () => Pedir(AccionMus.Envidar(partida.Estado.TantosEnvite)));
        Conectar(botonMas, () => Pedir(AccionMus.Reenvidar(partida.Estado.TantosEnvite)));
        Conectar(botonQuiero, () => Pedir(AccionMus.Quiero));
        Conectar(botonNoQuiero, () => Pedir(AccionMus.NoQuiero));
        Conectar(botonOrdago, () => Pedir(AccionMus.Ordago));

        for (int i = 0; i < botonesSenas.Length && i < 4; i++)
        {
            TipoSenalMus tipo = (TipoSenalMus)i;
            Conectar(botonesSenas[i], () => partida.SolicitarSenal(tipo));
        }

        foreach (GameObject objeto in ocultarEnOnline)
            if (objeto != null)
                objeto.SetActive(false);
    }

    private void OnEnable()
    {
        if (partida == null)
            return;

        partida.EventoRecibido += AlEvento;
        partida.SenalRecibida += AlSenal;
        partida.ManoPrivadaActualizada += AlRecibirMano;
        partida.ManosReveladas += AlRevelar;
    }

    private void OnDisable()
    {
        if (partida == null)
            return;

        partida.EventoRecibido -= AlEvento;
        partida.SenalRecibida -= AlSenal;
        partida.ManoPrivadaActualizada -= AlRecibirMano;
        partida.ManosReveladas -= AlRevelar;
    }

    private void AdaptarMesaLocal()
    {
        GestorFasesMus fases = FindFirstObjectByType<GestorFasesMus>();
        GestorEnvitesMus envites = FindFirstObjectByType<GestorEnvitesMus>();
        MarcadorMus marcador = FindFirstObjectByType<MarcadorMus>();

        if (fases != null)
        {
            if (reparto == null) reparto = Leer<RepartoPrueba>(fases, "reparto");
            if (revelador == null) revelador = Leer<ReveladorManosMus>(fases, "reveladorManos");
            if (botonMus == null) botonMus = Leer<Button>(fases, "botonMus");
            if (botonNoHayMus == null) botonNoHayMus = Leer<Button>(fases, "botonNoHayMus");
            if (botonDescartar == null) botonDescartar = Leer<Button>(fases, "botonConfirmarDescarte");
            if (textoFase == null) textoFase = Leer<TMP_Text>(fases, "textoFase");

            if (cartas == null || cartas.Length != 4 || cartas[0] == null)
                cartas = Leer<CartaSeleccionable[]>(fases, "cartas");

            OcultarAdemas(Leer<Button>(fases, "botonSiguienteLance"));
            fases.enabled = false;
        }

        if (envites != null)
        {
            if (botonPasar == null) botonPasar = Leer<Button>(envites, "botonPasar");
            if (botonEnvidar == null) botonEnvidar = Leer<Button>(envites, "botonEnvidar");
            if (botonMas == null) botonMas = Leer<Button>(envites, "botonMas");
            if (botonQuiero == null) botonQuiero = Leer<Button>(envites, "botonQuiero");
            if (botonNoQuiero == null) botonNoQuiero = Leer<Button>(envites, "botonNoQuiero");
            if (botonOrdago == null) botonOrdago = Leer<Button>(envites, "botonOrdago");
            if (panelEnvites == null) panelEnvites = Leer<GameObject>(envites, "panelEnvites");
            if (textoAnuncios == null) textoAnuncios = Leer<TMP_Text>(envites, "textoAnuncios");
            if (textoEnvite == null) textoEnvite = Leer<TMP_Text>(envites, "textoEstadoApuestas");

            if (bocadillos == null || bocadillos.Length != 4 || Vacio(bocadillos))
                bocadillos = Leer<BocadilloMus3D[]>(envites, "bocadillosJugadores") ?? new BocadilloMus3D[4];

            OcultarAdemas(Leer<Button>(envites, "botonSiguienteLance"));
            envites.enabled = false;
        }

        if (marcador != null)
        {
            if (textoMarcador == null) textoMarcador = Leer<TMP_Text>(marcador, "textoMarcador");
            marcador.enabled = false;
        }

        if (reparto == null) reparto = FindFirstObjectByType<RepartoPrueba>();
        if (revelador == null) revelador = FindFirstObjectByType<ReveladorManosMus>();

        if (senales == null || senales.Length != 4)
            senales = new ControlSenalesMus[4];

        if (Vacio(senales))
        {
            foreach (SenalesPorCartasMus s in FindObjectsByType<SenalesPorCartasMus>(FindObjectsSortMode.None))
            {
                ControlSenalesMus control = s.GetComponent<ControlSenalesMus>();
                if (control != null && s.IndiceJugador >= 0 && s.IndiceJugador < 4)
                    senales[s.IndiceJugador] = control;
            }
        }

        foreach (DecisionSenalesBotMus bot in FindObjectsByType<DecisionSenalesBotMus>(FindObjectsSortMode.None))
            bot.enabled = false;

        foreach (PantallaVictoriaMus pantalla in FindObjectsByType<PantallaVictoriaMus>(FindObjectsSortMode.None))
            pantalla.enabled = false;

        foreach (VistaFinalManoMus vista in FindObjectsByType<VistaFinalManoMus>(FindObjectsSortMode.None))
            vista.enabled = false;
    }

    private void OcultarAdemas(Button boton)
    {
        if (boton != null)
            boton.gameObject.SetActive(false);
    }

    private static T Leer<T>(object origen, string campo) where T : class
    {
        FieldInfo info = origen.GetType().GetField(campo, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return info?.GetValue(origen) as T;
    }

    private static bool Vacio(UnityEngine.Object[] objetos)
    {
        foreach (UnityEngine.Object o in objetos)
            if (o != null)
                return false;

        return true;
    }

    private static void Conectar(Button boton, UnityAction accion)
    {
        if (boton == null)
            return;

        for (int i = 0; i < boton.onClick.GetPersistentEventCount(); i++)
            boton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(accion);
    }

    private void Pedir(AccionMus accion)
    {
        partida.SolicitarAccion(accion);
    }

    private void Descartar()
    {
        int mascara = MascaraSeleccion();

        if (mascara == 0)
        {
            Anunciar("Elige al menos una carta para descartar.");
            return;
        }

        partida.SolicitarAccion(AccionMus.Descartar(mascara));
    }

    private int MascaraSeleccion()
    {
        int mascara = 0;

        for (int i = 0; i < 4 && cartas != null && i < cartas.Length; i++)
            if (cartas[i] != null && cartas[i].EstaSeleccionada)
                mascara |= 1 << i;

        return mascara;
    }

    private int Relativo(int puesto)
    {
        int local = partida.PuestoLocal;
        return local < 0 ? puesto : (puesto - local + 4) % 4;
    }

    private string Nombre(int puesto) => puesto >= 0 && puesto < 4 ? NombresPuesto[puesto] : "-";

    private void Update()
    {
        if (!partida.IsSpawned)
        {
            MostrarBotones(false);
            return;
        }

        EstadoPublicoMus e = partida.Estado;
        int local = partida.PuestoLocal;
        bool empezada = partida.PartidaEmpezada;
        bool miTurno = e.EnCurso && local >= 0 && e.JugadorActual == local;
        FaseMus fase = e.FaseActual;

        Activar(botonMus, miTurno && e.Legal(TipoAccionMus.Mus));
        Activar(botonNoHayMus, miTurno && e.Legal(TipoAccionMus.NoHayMus));
        Activar(botonDescartar, miTurno && e.Legal(TipoAccionMus.Descartar));
        Activar(botonPasar, miTurno && e.Legal(TipoAccionMus.Pasar));
        Activar(botonEnvidar, miTurno && e.Legal(TipoAccionMus.Envidar));
        Activar(botonMas, miTurno && e.Legal(TipoAccionMus.Reenvidar));
        Activar(botonQuiero, miTurno && e.Legal(TipoAccionMus.Quiero));
        Activar(botonNoQuiero, miTurno && e.Legal(TipoAccionMus.NoQuiero));
        Activar(botonOrdago, miTurno && e.Legal(TipoAccionMus.Ordago));

        if (panelEnvites != null)
            panelEnvites.SetActive(miTurno && PartidaMus.EsLance(fase));

        bool puedoDescartar = miTurno && fase == FaseMus.Descarte;

        if (cartas != null)
            foreach (CartaSeleccionable carta in cartas)
                if (carta != null)
                {
                    Button b = carta.GetComponent<Button>();
                    if (b != null) b.interactable = puedoDescartar;
                }

        ActualizarBotonesSenas(e, local);

        if (textoFase != null)
            textoFase.text = !empezada ? "ESPERANDO PARTIDA ONLINE" : TextoFase(e, miTurno);

        if (textoMarcador != null && empezada)
        {
            int pareja = local >= 0 ? local % 2 : 0;
            int nuestros = pareja == 0 ? e.Puntos0 : e.Puntos1;
            int suyos = pareja == 0 ? e.Puntos1 : e.Puntos0;
            textoMarcador.text = "NOSOTROS " + nuestros + "  -  ELLOS " + suyos;
        }

        if (textoEnvite != null)
        {
            bool hayEnvite = PartidaMus.EsLance(fase) && (e.TantosApostados > 0 ||
                e.EstadoEnvite == (int)MotorEnvitesMus.EstadoEnvite.OrdagoPendiente);
            textoEnvite.text = hayEnvite
                ? (e.EstadoEnvite == (int)MotorEnvitesMus.EstadoEnvite.OrdagoPendiente ? "ORDAGO EN JUEGO" : "ENVITE: " + e.TantosApostados)
                : "";
        }
    }

    private string TextoFase(EstadoPublicoMus e, bool miTurno)
    {
        if (!e.EnCurso)
        {
            int local = partida.PuestoLocal;
            bool ganamos = local >= 0 && e.ParejaGanadora == local % 2;
            return (ganamos ? "HABEIS GANADO" : "HABEIS PERDIDO") + (e.PorOrdago ? " POR ORDAGO" : "");
        }

        string fase;

        switch (e.FaseActual)
        {
            case FaseMus.Mus: fase = "MUS"; break;
            case FaseMus.Descarte: fase = "DESCARTE"; break;
            case FaseMus.Grande: fase = "GRANDE"; break;
            case FaseMus.Chica: fase = "CHICA"; break;
            case FaseMus.Pares: fase = "PARES"; break;
            case FaseMus.JuegoPunto: fase = "JUEGO / PUNTO"; break;
            default: return "FIN DE LA MANO";
        }

        string turno = miTurno ? "TE TOCA" : "TURNO DE " + Nombre(e.JugadorActual);

        if (miTurno && e.FaseActual == FaseMus.Descarte)
            turno = "TE TOCA: ELIGE CARTAS Y DESCARTA";

        return fase + "  |  " + turno;
    }

    private void ActualizarBotonesSenas(EstadoPublicoMus e, int local)
    {
        int[] mias = cartasLocalesMostradas;
        bool hayMano = local >= 0 && mias[0] >= 0 && e.EnCurso && e.FaseActual != FaseMus.FinMano;
        EvaluadorManoMus.Resultado r = hayMano ? CartasMus.Evaluar(mias) : null;

        for (int i = 0; i < botonesSenas.Length && i < 4; i++)
            Activar(botonesSenas[i], hayMano && PartidaMus.CumpleSenal(mias, r, (TipoSenalMus)i));
    }

    private static void Activar(Button boton, bool activo)
    {
        if (boton != null && boton.gameObject.activeSelf != activo)
            boton.gameObject.SetActive(activo);
    }

    private void MostrarBotones(bool activo)
    {
        Activar(botonMus, activo);
        Activar(botonNoHayMus, activo);
        Activar(botonDescartar, activo);
        Activar(botonPasar, activo);
        Activar(botonEnvidar, activo);
        Activar(botonMas, activo);
        Activar(botonQuiero, activo);
        Activar(botonNoQuiero, activo);
        Activar(botonOrdago, activo);

        if (panelEnvites != null)
            panelEnvites.SetActive(activo);
    }

    private void AlRecibirMano()
    {
        int[] nuevas = new int[4];

        for (int i = 0; i < 4; i++)
            nuevas[i] = partida.CartasLocales[i];

        bool manoNueva = cartasLocalesMostradas[0] < 0 || manosEnMesa;
        cartasLocalesMostradas = nuevas;
        reparto.EstablecerManoExterna(0, CartasMus.ATexto(nuevas));

        if (manoNueva)
        {
            for (int sitio = 1; sitio < 4; sitio++)
                reparto.EstablecerManoExterna(sitio, new[] { "", "", "", "" });

            if (revelador != null)
                revelador.OcultarManos();

            manosEnMesa = false;
            OcultarBocadillos();
        }
    }

    private void AlRevelar(int[] todas)
    {
        for (int puesto = 0; puesto < 4; puesto++)
        {
            string[] textos = new string[4];

            for (int c = 0; c < 4; c++)
                textos[c] = CartasMus.Texto(todas[puesto * 4 + c]);

            reparto.EstablecerManoExterna(Relativo(puesto), textos);
        }

        if (revelador != null)
            revelador.MostrarManos();

        manosEnMesa = true;
    }

    private void AlEvento(EventoMus e)
    {
        if (e.Tipo == TipoEventoMus.LanceCerrado)
        {
            limpiarAlSiguienteEvento = true;
            Anunciar(e.Fase.ToString().ToUpper() + ": " + TextoCierre((MotorEnvitesMus.EstadoEnvite)e.Dato));
            return;
        }

        if (e.Tipo == TipoEventoMus.FinMano)
        {
            Anunciar("FIN DE LA MANO");
            return;
        }

        if (limpiarAlSiguienteEvento)
        {
            OcultarBocadillos();
            limpiarAlSiguienteEvento = false;
        }

        if (e.Tipo == TipoEventoMus.Declaracion)
        {
            string texto = (e.Fase == FaseMus.Pares ? "PARES " : "JUEGO ") + (e.Dato == 1 ? "SI" : "NO");
            MostrarBocadillo(e.Jugador, texto, true);
            Anunciar(Nombre(e.Jugador) + ": " + texto);
            return;
        }

        TipoAccionMus tipo = e.Accion.Tipo;

        if (tipo == TipoAccionMus.Envidar || tipo == TipoAccionMus.Reenvidar || tipo == TipoAccionMus.Ordago)
            OcultarBocadillos();

        if (tipo == TipoAccionMus.Descartar)
        {
            OcultarBocadillos();
            MostrarBocadillo(e.Jugador, "ME DESCARTO DE " + AccionMus.ContarBits(e.Accion.Valor), false);
            Anunciar(Nombre(e.Jugador) + " se descarta de " + AccionMus.ContarBits(e.Accion.Valor));
            return;
        }

        MostrarBocadillo(e.Jugador, Frase(e.Accion), true);
        Anunciar(Nombre(e.Jugador) + ": " + Frase(e.Accion));

        if (tipo == TipoAccionMus.NoHayMus)
            limpiarAlSiguienteEvento = true;
    }

    private static string Frase(AccionMus a)
    {
        switch (a.Tipo)
        {
            case TipoAccionMus.Envidar: return "ENVIDO " + a.Valor + "!";
            case TipoAccionMus.Reenvidar: return a.Valor + " MAS!";
            case TipoAccionMus.Ordago: return "ORDAGO!";
            default: return a.ToString();
        }
    }

    private static string TextoCierre(MotorEnvitesMus.EstadoEnvite estado)
    {
        switch (estado)
        {
            case MotorEnvitesMus.EstadoEnvite.Aceptado: return "QUERIDO";
            case MotorEnvitesMus.EstadoEnvite.Rechazado: return "NO QUERIDO";
            case MotorEnvitesMus.EstadoEnvite.OrdagoAceptado: return "ORDAGO QUERIDO";
            default: return "EN PASO";
        }
    }

    private void AlSenal(int puesto, TipoSenalMus tipo)
    {
        int sitio = Relativo(puesto);

        if (senales != null && sitio < senales.Length && senales[sitio] != null)
            senales[sitio].EjecutarSenal((ControlSenalesMus.TipoSenal)(int)tipo);
    }

    private void MostrarBocadillo(int puesto, string texto, bool fijo)
    {
        int sitio = Relativo(puesto);

        if (bocadillos == null || sitio >= bocadillos.Length || bocadillos[sitio] == null)
            return;

        if (fijo) bocadillos[sitio].MostrarFijo(texto);
        else bocadillos[sitio].Mostrar(texto);
    }

    private void OcultarBocadillos()
    {
        if (bocadillos == null)
            return;

        foreach (BocadilloMus3D b in bocadillos)
            if (b != null)
                b.Ocultar();
    }

    private void Anunciar(string texto)
    {
        if (textoAnuncios == null)
            return;

        textoAnuncios.gameObject.SetActive(true);
        textoAnuncios.text = texto;
    }
}
