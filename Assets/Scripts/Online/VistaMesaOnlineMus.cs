using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public partial class VistaMesaOnlineMus : MonoBehaviour
{
    private static string[] NombresPuesto => ColoresJugadoresMus.NombresPorPuesto;

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

    [Header("Fin de mano: camara de la mesa y tabla de tantos")]
    [SerializeField] private Camera camaraJugador;
    [SerializeField] private Camera camaraRevision;
    [SerializeField] private CamaraMus controlCamaraJugador;
    [SerializeField] private GameObject panelCartas;
    [SerializeField] private Button botonSiguienteMano;
    [SerializeField] private bool mostrarTablaTanteo = true;
    [Tooltip("Si esta desmarcado, se mueve la camara del jugador encima de la mesa (mas fiable).")]
    [SerializeField] private bool usarCamaraRevisionDeLaEscena = false;
    [SerializeField] private float alturaVistaMesa = 1.8f;
    [SerializeField] private float retrocesoVistaMesa = 0.45f;

    [Header("Ritmo visual (segundos que se ve cada cosa)")]
    [SerializeField] private float pausaAccion = 1.1f;
    [SerializeField] private float pausaDeclaracion = 0.8f;
    [SerializeField] private float pausaCierreLance = 1.5f;

    [Header("Colores de los personajes (segun su nombre)")]
    [SerializeField] private bool colorearPersonajes = true;
    [Tooltip("0 = sin color, 1 = color puro. Valores bajos dejan los personajes claros como antes.")]
    [UnityEngine.Range(0f, 1f)][SerializeField] private float intensidadColorSuave = 0.4f;
    [Tooltip("Pinta cada pieza (piel, pelo, ropa del color del jugador, zapatos...).")]
    [SerializeField] private bool aspectoPorPiezas = true;
    [UnityEngine.Range(0f, 1f)][SerializeField] private float saturacionRopa = 0.85f;
    [UnityEngine.Range(0f, 1f)][SerializeField] private float brilloMaterial = 0.2f;

    [Header("Gestos (senas) mas claros, con GestosMus")]
    [SerializeField] private bool usarGestosClaros = true;
    [SerializeField] private bool mostrarBotonesSenas = true;

    [Header("Vida de los personajes (respirar, parpadear, mirar, reaccionar)")]
    [SerializeField] private bool darVida = true;
    [SerializeField] private float alturaCabezas = 1.1f;
    [Tooltip("Angulo maximo entre tu camara y la cara de alguien para contar que le estas mirando.")]
    [SerializeField] private float anguloMirada = 25f;

    [Header("Marcador y avisos en pantalla")]
    [SerializeField] private bool mostrarHud = true;
    [Tooltip("Con el marcador nuevo, los anuncios de texto sobran: ya estan los bocadillos y el historial.")]
    [SerializeField] private bool ocultarAnunciosConHud = true;

    [Header("Textos")]
    [SerializeField] private TMP_Text textoFase;
    [SerializeField] private TMP_Text textoMarcador;
    [SerializeField] private TMP_Text textoAnuncios;
    [SerializeField] private TMP_Text textoEnvite;

    private bool limpiarAlSiguienteEvento;
    private int[] cartasLocalesMostradas = { -1, -1, -1, -1 };
    private bool manosEnMesa;
    private bool enRevision;
    private bool tablaVisible;
    private TablaTanteoMus.Tabla tabla;
    private int[] manosVistas;
    private int puestoColoreado = -2;
    private int versionSalaColoreada = -1;
    private int trajeProbador = -1;
    private int probadorColoreado = -1;
    private int declaracionesJuego;
    private bool algunoTieneJuego;
    private string modoUltimoLance;
    private FaseMus faseMostrada = FaseMus.Mus;
    private bool corridoVisual;
    private int ultimoTurnoMirado = -1;
    private readonly GestosMus[] gestos = new GestosMus[4];
    private readonly HashSet<TipoSenalMus> senasHechas = new();
    private string avisoSena;
    private float finAvisoSena;
    private GUIStyle estiloSena;
    private string avisoTemporal;
    private string avisoAyuda;
    private float finAvisoTemporal;
    private Vector3 posicionCamaraGuardada;
    private Quaternion rotacionCamaraGuardada;
    private bool camaraMovida;
    private readonly List<GameObject> textosOcultosEnRevision = new();
    private readonly Queue<KeyValuePair<Action, float>> cola = new();
    private float proximoVisual;
    private readonly string[] resultadosLance = new string[4];
    private Texture2D texFondo;
    private Texture2D texCelda;
    private Texture2D texCeldaActiva;
    private Texture2D texNos;
    private Texture2D texEllos;
    private Texture2D texBarra;
    private GUIStyle estiloTitulo;
    private GUIStyle estiloPeque;
    private GUIStyle estiloBanner;
    private GUIStyle estiloAyuda;
    private GUIStyle estiloTexto;
    private GUIStyle estiloCabecera;

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
        partida.HistorialAlRitmoDeLaMesa = true;

        Conectar(botonMus, () => Pedir(AccionMus.Mus));
        Conectar(botonNoHayMus, () => Pedir(AccionMus.NoHayMus));
        Conectar(botonDescartar, Descartar);
        Conectar(botonPasar, () => Pedir(AccionMus.Pasar));
        Conectar(botonEnvidar, () => Pedir(AccionMus.Envidar(partida.Estado.TantosEnvite)));
        Conectar(botonMas, () => Pedir(AccionMus.Reenvidar(partida.Estado.TantosEnvite)));
        Conectar(botonQuiero, () => Pedir(AccionMus.Quiero));
        Conectar(botonNoQuiero, () => Pedir(AccionMus.NoQuiero));
        Conectar(botonOrdago, () => Pedir(AccionMus.Ordago));
        Conectar(botonSiguienteMano, () => partida.PedirSiguienteMano());

        for (int i = 0; i < botonesSenas.Length && i < 4; i++)
        {
            TipoSenalMus tipo = (TipoSenalMus)i;
            Conectar(botonesSenas[i], () => partida.SolicitarSenal(tipo));
        }

        foreach (GameObject objeto in ocultarEnOnline)
            if (objeto != null)
                objeto.SetActive(false);

        if (mostrarHud)
        {
            if (textoFase != null) textoFase.gameObject.SetActive(false);
            if (textoMarcador != null) textoMarcador.gameObject.SetActive(false);
            if (textoEnvite != null) textoEnvite.gameObject.SetActive(false);
        }

        ActivarVistaJugador(false);

        if (usarInterfazNueva)
            CrearInterfaz();
    }

    private void ColorearPersonajes()
    {
        puestoColoreado = partida.PuestoLocal;
        versionSalaColoreada = partida.VersionSala;
        probadorColoreado = trajeProbador;
        necesitaColorear = false;
        int local = Mathf.Max(0, puestoColoreado);
        int mio = MiPuesto();

        for (int sitio = 0; sitio < 4; sitio++)
        {
            GameObject posicion = ModeloEnSitio(sitio);
            int puesto = (local + sitio) % 4;
            int traje = TaquillaMus.TrajeDe(AspectoDePuesto(puesto));

            // Tu personaje lleva siempre lo que tienes puesto (y en el probador, lo que te estas probando).
            if (puesto == mio)
                traje = trajeProbador >= 0 ? trajeProbador : TaquillaMus.Equipado(CategoriaTaquilla.Traje);

            if (aspectoPorPiezas)
                ColoresJugadoresMus.AplicarAspecto(posicion, ColoresJugadoresMus.PersonajeDe(puesto), saturacionRopa, brilloMaterial, traje);
            else
                ColoresJugadoresMus.Colorear(posicion, ColoresJugadoresMus.ColorDe(puesto), intensidadColorSuave);
        }
    }

    private GameObject[] modelosPorSitio;

    private GameObject ModeloEnSitio(int sitio)
    {
        if (modelosPorSitio == null)
        {
            Transform camara = camaraJugador != null ? camaraJugador.transform : Camera.main != null ? Camera.main.transform : null;
            Vector3 referencia = camara != null && !camaraMovida ? camara.position : camara != null ? posicionCamaraGuardada : new Vector3(0f, 0f, -3f);
            modelosPorSitio = SitiosMesaMus.Asignar(Vector3.zero, referencia);
        }

        return modelosPorSitio[sitio];
    }

    private void PrepararGestos()
    {
        for (int sitio = 0; sitio < 4; sitio++)
        {
            if (gestos[sitio] != null)
                continue;

            GameObject posicion = ModeloEnSitio(sitio);

            if (posicion == null)
                continue;

            Animator animador = posicion.GetComponentInChildren<Animator>(true);
            GameObject modelo = animador != null ? animador.gameObject : posicion;
            gestos[sitio] = modelo.GetComponent<GestosMus>();

            if (gestos[sitio] == null)
                gestos[sitio] = modelo.AddComponent<GestosMus>();
        }
    }

    private void Avisar(string texto, float segundos, string ayuda = null)
    {
        avisoAyuda = ayuda;
        avisoTemporal = texto;
        finAvisoTemporal = Time.time + segundos;
    }

    private void Encolar(Action accion, float pausa)
    {
        cola.Enqueue(new KeyValuePair<Action, float>(accion, pausa));
    }

    private void ProcesarCola()
    {
        while (cola.Count > 0 && Time.time >= proximoVisual)
        {
            KeyValuePair<Action, float> siguiente = cola.Dequeue();
            siguiente.Key();
            proximoVisual = Time.time + siguiente.Value;
        }
    }

    private float Pausa(EventoMus e) => PausaBase(e) * (DemoActiva ? ritmoVisualDemo : 1f);

    private float PausaBase(EventoMus e)
    {
        switch (e.Tipo)
        {
            case TipoEventoMus.Accion: return e.Accion.Tipo == TipoAccionMus.Descartar ? pausaDeclaracion : pausaAccion;
            case TipoEventoMus.Declaracion: return pausaDeclaracion;
            case TipoEventoMus.LanceCerrado: return pausaCierreLance;
            default: return 0.3f;
        }
    }

    private void AlEvento(EventoMus e) => Encolar(() => ProcesarEvento(e), Pausa(e));

    private void AlSenal(int puesto, TipoSenalMus tipo) => Encolar(() => ProcesarSenal(puesto, tipo), DemoActiva ? pausaSenaDemo : 0.4f);

    private void AlRecibirMano()
    {
        int[] recibidas = new int[4];

        for (int i = 0; i < 4; i++)
            recibidas[i] = partida.CartasLocales[i];

        Encolar(() => ProcesarMano(recibidas), 0.1f);
    }

    private void AlRevelar(int[] todas)
    {
        int[] copia = (int[])todas.Clone();
        Encolar(() => ProcesarRevelado(copia), 0.5f);
    }

    private void ActivarVistaJugador(bool recentrar)
    {
        enRevision = false;
        tablaVisible = false;

        if (camaraJugador != null)
        {
            if (camaraMovida)
            {
                camaraJugador.transform.SetPositionAndRotation(posicionCamaraGuardada, rotacionCamaraGuardada);
                camaraMovida = false;
            }

            camaraJugador.enabled = true;
        }

        if (camaraRevision != null) camaraRevision.enabled = false;

        if (controlCamaraJugador != null)
        {
            controlCamaraJugador.enabled = true;
            if (recentrar) controlCamaraJugador.RecentrarVista();
        }

        if (panelCartas != null) panelCartas.SetActive(true);

        foreach (GameObject objeto in textosOcultosEnRevision)
            if (objeto != null)
                objeto.SetActive(true);

        textosOcultosEnRevision.Clear();

        if (partida != null)
            partida.ModoRevision = false;
    }

    private void ActivarVistaRevision()
    {
        enRevision = true;
        tablaVisible = mostrarTablaTanteo;

        if (controlCamaraJugador != null) controlCamaraJugador.enabled = false;

        if (usarCamaraRevisionDeLaEscena && camaraRevision != null)
        {
            camaraRevision.gameObject.SetActive(true);
            camaraRevision.enabled = true;
            if (camaraJugador != null) camaraJugador.enabled = false;
        }
        else if (camaraJugador != null)
        {
            if (!camaraMovida)
            {
                posicionCamaraGuardada = camaraJugador.transform.position;
                rotacionCamaraGuardada = camaraJugador.transform.rotation;
                camaraMovida = true;
            }

            Vector3 centro = CentroMesa();
            Vector3 posicion = centro + new Vector3(0f, alturaVistaMesa, -retrocesoVistaMesa);
            camaraJugador.transform.SetPositionAndRotation(posicion, Quaternion.LookRotation(centro - posicion, Vector3.forward));
        }

        if (panelCartas != null) panelCartas.SetActive(false);

        OcultarEnRevision(textoFase);
        OcultarEnRevision(textoMarcador);
        OcultarEnRevision(textoAnuncios);
        OcultarEnRevision(textoEnvite);

        if (botonSiguienteMano != null)
            OcultarEnRevision(botonSiguienteMano.gameObject);

        OcultarBocadillos();
        partida.ModoRevision = true;
    }

    private void OcultarEnRevision(Component componente)
    {
        if (componente != null)
            OcultarEnRevision(componente.gameObject);
    }

    private void OcultarEnRevision(GameObject objeto)
    {
        if (objeto == null || !objeto.activeSelf)
            return;

        objeto.SetActive(false);
        textosOcultosEnRevision.Add(objeto);
    }

    private Vector3 CentroMesa()
    {
        float altura = 0.865f;

        if (revelador != null)
        {
            FieldInfo campo = revelador.GetType().GetField("alturaCartas", BindingFlags.Instance | BindingFlags.NonPublic);

            if (campo != null && campo.GetValue(revelador) is float valor)
                altura = valor;
        }

        return new Vector3(0f, altura, 0f);
    }

    private void OnEnable()
    {
        if (partida == null)
            return;

        partida.EventoRecibido += AlEvento;
        partida.SenalRecibida += AlSenal;
        partida.ManoPrivadaActualizada += AlRecibirMano;
        partida.ManosReveladas += AlRevelar;
        partida.SenalPercibida += AlPercibirSena;
        partida.TeHanPillado += AlTePillan;
        partida.ResultadoSena += AlResultadoSena;
    }

    private void OnDisable()
    {
        if (partida == null)
            return;

        partida.EventoRecibido -= AlEvento;
        partida.SenalRecibida -= AlSenal;
        partida.ManoPrivadaActualizada -= AlRecibirMano;
        partida.ManosReveladas -= AlRevelar;
        partida.SenalPercibida -= AlPercibirSena;
        partida.TeHanPillado -= AlTePillan;
        partida.ResultadoSena -= AlResultadoSena;
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
        {
            if (camaraJugador == null) camaraJugador = Leer<Camera>(vista, "camaraJugador");
            if (camaraRevision == null) camaraRevision = Leer<Camera>(vista, "camaraRevision");
            if (controlCamaraJugador == null) controlCamaraJugador = Leer<CamaraMus>(vista, "controlCamaraJugador");
            if (panelCartas == null) panelCartas = Leer<GameObject>(vista, "panelCartas");

            GameObject siguiente = Leer<GameObject>(vista, "botonSiguienteMano");
            if (botonSiguienteMano == null && siguiente != null) botonSiguienteMano = siguiente.GetComponent<Button>();

            GameObject repartir = Leer<GameObject>(vista, "botonRepartirPrueba");
            if (repartir != null) repartir.SetActive(false);

            vista.enabled = false;
        }
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
        ProcesarCola();
        ActualizarDemo();
        ActualizarCartasEnMano();

        if (lienzoUI != null)
            ActualizarInterfaz();

        ActualizarPersonajes();

        if (colorearPersonajes && (partida.PuestoLocal != puestoColoreado || partida.VersionSala != versionSalaColoreada ||
                                   trajeProbador != probadorColoreado || necesitaColorear))
            ColorearPersonajes();

        partida.EsperandoAnimaciones = cola.Count > 0;

        ActualizarMiMirada();
        AplicarMiradasDeLosDemas();

        if (darVida && !partida.SenasConMirada)
        {
            PrepararGestos();
            int turno = partida.Estado.EnCurso ? partida.Estado.JugadorActual : -1;

            if (cola.Count == 0 && turno != ultimoTurnoMirado)
            {
                ultimoTurnoMirado = turno;
                TodosMiranA(turno, 3f);
            }
        }

        if (!partida.IsSpawned)
        {
            MostrarBotones(false);
            return;
        }

        EstadoPublicoMus e = partida.Estado;
        int local = partida.PuestoLocal;
        bool empezada = partida.PartidaEmpezada;
        bool miTurno = e.EnCurso && local >= 0 && e.JugadorActual == local && cola.Count == 0;
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

        if (botonSiguienteMano != null && !enRevision)
            Activar(botonSiguienteMano, false);

        if (textoFase != null && !mostrarHud)
            textoFase.text = !empezada ? "ESPERANDO PARTIDA ONLINE" : TextoFase(e, miTurno);

        if (textoMarcador != null && empezada && !mostrarHud)
        {
            int pareja = local >= 0 ? local % 2 : 0;
            int nuestros = pareja == 0 ? e.Puntos0 : e.Puntos1;
            int suyos = pareja == 0 ? e.Puntos1 : e.Puntos0;
            textoMarcador.text = "NOSOTROS " + nuestros + "  -  ELLOS " + suyos;
        }

        if (textoEnvite != null && !mostrarHud)
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

    private void ProcesarMano(int[] recibidas)
    {
        int[] nuevas = new int[4];

        bool iguales = true;

        for (int i = 0; i < 4; i++)
        {
            nuevas[i] = recibidas[i];
            iguales &= nuevas[i] == cartasLocalesMostradas[i];
        }

        if (iguales && enRevision)
            return;

        bool manoNueva = cartasLocalesMostradas[0] < 0 || manosEnMesa || enRevision;
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

            for (int i = 0; i < resultadosLance.Length; i++)
                resultadosLance[i] = null;

            declaracionesJuego = 0;
            algunoTieneJuego = false;
            modoUltimoLance = null;
            senasHechas.Clear();

            if (enRevision)
                ActivarVistaJugador(true);
        }
    }

    private void ProcesarRevelado(int[] todas)
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

        int pareja = partida.PuestoLocal >= 0 ? partida.PuestoLocal % 2 : 0;
        tabla = TablaTanteoMus.Construir(partida.UltimoDesglose, todas, pareja, NombresPuesto);
        manosVistas = todas;
        ReaccionarAlTanteo();
        ActivarVistaRevision();
    }

    private GestosMus GestosDePuesto(int puesto)
    {
        PrepararGestos();
        int sitio = Relativo(puesto);
        return sitio >= 0 && sitio < 4 ? gestos[sitio] : null;
    }

    private Vector3 CabezaDePuesto(int puesto)
    {
        int sitio = Relativo(puesto);

        if (sitio == 0)
        {
            Camera cam = camaraJugador != null ? camaraJugador : Camera.main;
            if (cam != null) return cam.transform.position;
        }

        GameObject modelo = ModeloEnSitio(sitio);
        return modelo != null ? modelo.transform.position + Vector3.up * alturaCabezas : Vector3.up * alturaCabezas;
    }

    private void TodosMiranA(int puesto, float segundos)
    {
        if (!darVida || puesto < 0)
            return;

        Vector3 objetivo = CabezaDePuesto(puesto);

        for (int p = 0; p < 4; p++)
        {
            if (p == puesto)
                continue;

            GestosMus g = GestosDePuesto(p);
            if (g != null) g.MirarA(objetivo, segundos);
        }
    }

    private void ReaccionarA(EventoMus e)
    {
        if (!darVida || e.Jugador < 0)
            return;

        GestosMus g = GestosDePuesto(e.Jugador);

        if (e.Tipo == TipoEventoMus.Declaracion)
        {
            if (e.Dato == 1 && g != null) g.Reaccionar(ReaccionMus.Asentir);
            return;
        }

        if (e.Tipo != TipoEventoMus.Accion)
            return;

        TodosMiranA(e.Jugador, 2.5f);

        if (g == null)
            return;

        switch (e.Accion.Tipo)
        {
            case TipoAccionMus.Mus:
            case TipoAccionMus.Quiero: g.Reaccionar(ReaccionMus.Asentir); break;
            case TipoAccionMus.NoHayMus:
            case TipoAccionMus.NoQuiero: g.Reaccionar(ReaccionMus.Negar); break;
            case TipoAccionMus.Descartar: g.Reaccionar(ReaccionMus.MirarCartas); break;
            case TipoAccionMus.Pasar: g.Reaccionar(ReaccionMus.Encoger); break;
            case TipoAccionMus.Envidar:
            case TipoAccionMus.Reenvidar: g.Reaccionar(ReaccionMus.Animarse); break;
            case TipoAccionMus.Ordago: g.Reaccionar(ReaccionMus.Ordago); break;
        }
    }

    private void ReaccionarAlTanteo()
    {
        if (!darVida || tabla == null || tabla.TotalNosotros == tabla.TotalEllos)
            return;

        int local = Mathf.Max(0, partida.PuestoLocal);
        bool ganamos = tabla.TotalNosotros > tabla.TotalEllos;

        for (int p = 0; p < 4; p++)
        {
            GestosMus g = GestosDePuesto(p);
            bool nuestro = p % 2 == local % 2;
            if (g != null) g.Reaccionar(nuestro == ganamos ? ReaccionMus.Celebrar : ReaccionMus.Triste);
        }
    }

    private void ProcesarEvento(EventoMus e)
    {
        RotuloDemoEvento(e);
        partida.AnadirAlHistorial(e);
        ReaccionarA(e);
        faseMostrada = e.Tipo == TipoEventoMus.FinMano ? FaseMus.FinMano : e.Fase;

        if (e.Tipo == TipoEventoMus.LanceCerrado)
        {
            limpiarAlSiguienteEvento = true;
            string resultado = TextoCierre(e);
            int indiceLance = IndiceLance(e.Fase);

            if (indiceLance >= 0)
                resultadosLance[indiceLance] = resultado;

            Anunciar(e.Fase.ToString().ToUpper() + ": " + resultado);
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
            MostrarBocadillo(e.Jugador, texto, true, e.Dato == 1 ? ColorSi : ColorNo);
            Anunciar(Nombre(e.Jugador) + ": " + texto);

            if (e.Fase == FaseMus.JuegoPunto)
            {
                declaracionesJuego++;
                algunoTieneJuego |= e.Dato == 1;

                if (declaracionesJuego == 4)
                {
                    modoUltimoLance = algunoTieneJuego ? "JUEGO" : "PUNTO";
                    Avisar(algunoTieneJuego
                        ? "HAY JUEGO: se juega a JUEGO"
                        : "NADIE TIENE JUEGO: se juega a PUNTO", 3.5f,
                        algunoTieneJuego
                        ? "JUEGO: 31 es la mejor, luego 32, 40, 37, 36, 35, 34 y 33"
                        : "PUNTO: gana quien mas se acerca a 30");
                }
            }

            return;
        }

        TipoAccionMus tipo = e.Accion.Tipo;

        if (tipo == TipoAccionMus.Mus || tipo == TipoAccionMus.NoHayMus)
            corridoVisual = e.Dato == 1 && tipo == TipoAccionMus.Mus;

        if (tipo == TipoAccionMus.NoHayMus && e.Dato == 1)
            Avisar(ColoresJugadoresMus.Nombre(e.Jugador) + " CORTA: EMPIEZA SIENDO MANO", 3.5f,
                "En las siguientes manos, la mano va pasando al siguiente jugador");

        if (tipo == TipoAccionMus.Envidar || tipo == TipoAccionMus.Reenvidar || tipo == TipoAccionMus.Ordago)
            OcultarBocadillos();

        if (tipo == TipoAccionMus.Descartar)
        {
            OcultarBocadillos();
            MostrarBocadillo(e.Jugador, "ME DESCARTO DE " + AccionMus.ContarBits(e.Accion.Valor), false, ColorNo);
            Anunciar(Nombre(e.Jugador) + " se descarta de " + AccionMus.ContarBits(e.Accion.Valor));
            return;
        }

        MostrarBocadillo(e.Jugador, Frase(e.Accion), true, ColorAccion(e.Accion.Tipo));
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

    private static readonly Color ColorSi = new(0.15f, 0.45f, 0.85f);
    private static readonly Color ColorNo = new(0.35f, 0.35f, 0.35f);

    private static Color ColorAccion(TipoAccionMus tipo)
    {
        switch (tipo)
        {
            case TipoAccionMus.Mus: return new Color(0.1f, 0.55f, 0.2f);
            case TipoAccionMus.NoHayMus: return new Color(0.7f, 0.1f, 0.1f);
            case TipoAccionMus.Envidar:
            case TipoAccionMus.Reenvidar: return new Color(0.9f, 0.45f, 0.0f);
            case TipoAccionMus.Quiero: return new Color(0.1f, 0.55f, 0.2f);
            case TipoAccionMus.Ordago: return new Color(0.85f, 0.0f, 0.0f);
            default: return ColorNo;
        }
    }

    private static int IndiceLance(FaseMus fase)
    {
        switch (fase)
        {
            case FaseMus.Grande: return 0;
            case FaseMus.Chica: return 1;
            case FaseMus.Pares: return 2;
            case FaseMus.JuegoPunto: return 3;
            default: return -1;
        }
    }

    private string Pareja(int pareja)
    {
        int local = partida.PuestoLocal;
        return local < 0 ? (pareja == 0 ? "LILA-MENTA" : "MOSTAZA-CORAL") : (pareja == local % 2 ? "NOSOTROS" : "ELLOS");
    }

    private string TextoCierre(EventoMus e)
    {
        switch ((MotorEnvitesMus.EstadoEnvite)e.Dato)
        {
            case MotorEnvitesMus.EstadoEnvite.Aceptado: return "Querido a " + e.Accion.Valor;
            case MotorEnvitesMus.EstadoEnvite.Rechazado: return "No querido: +" + e.Accion.Valor + " " + Pareja(e.Jugador);
            case MotorEnvitesMus.EstadoEnvite.OrdagoAceptado: return "ORDAGO QUERIDO";
            default: return "En paso";
        }
    }

    private void AvisarSena(string texto, float segundos)
    {
        avisoSena = texto;
        finAvisoSena = Time.time + segundos;
    }

    private void AlPercibirSena(int emisor, TipoSenalMus tipo, bool pillada)
    {
        AvisarSena(pillada
            ? "<color=#FFB347>Has pillado a </color>" + ColoresJugadoresMus.Nombre(emisor) + "<color=#FFB347>: <b>" + SenalesMus.Nombre(tipo) + "</b></color>"
            : ColoresJugadoresMus.Nombre(emisor) + " te hace una sena: <b>" + SenalesMus.Nombre(tipo) + "</b>  <size=80%>(" + SenalesMus.Gesto(tipo) + ")</size>", 4f);
    }

    private void AlTePillan(int rival, TipoSenalMus tipo)
    {
        AvisarSena("<color=#FF6B5E>Te ha pillado </color>" + ColoresJugadoresMus.Nombre(rival) + "<color=#FF6B5E>: ya sabe tu " + SenalesMus.Nombre(tipo) + "</color>", 4f);
    }

    private void AlResultadoSena(TipoSenalMus tipo, bool vista)
    {
        if (avisoSena != null && Time.time < finAvisoSena && avisoSena.Contains("pillado"))
            return;

        AvisarSena(vista ? "Tu companero ha visto tu sena" : "<color=#FFB347>Tu companero no te miraba: no la ha visto</color>", 3f);
    }

    private int miMiradaEnviada = -2;

    private void ActualizarMiMirada()
    {
        if (!partida.SenasConMirada || partida.PuestoLocal < 0 || enRevision)
            return;

        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;

        if (cam == null)
            return;

        int mejor = PartidaMus.MiraCartas;
        float mejorAngulo = anguloMirada;

        for (int sitio = 1; sitio < 4; sitio++)
        {
            GameObject modelo = ModeloEnSitio(sitio);

            if (modelo == null)
                continue;

            Vector3 cabeza = modelo.transform.position + Vector3.up * alturaCabezas;
            float angulo = Vector3.Angle(cam.transform.forward, cabeza - cam.transform.position);

            if (angulo < mejorAngulo)
            {
                mejorAngulo = angulo;
                mejor = (partida.PuestoLocal + sitio) % 4;
            }
        }

        if (mejor == miMiradaEnviada)
            return;

        miMiradaEnviada = mejor;
        partida.EnviarMirada(mejor);
    }

    private void AplicarMiradasDeLosDemas()
    {
        if (!darVida || !partida.SenasConMirada)
            return;

        for (int p = 0; p < 4; p++)
        {
            if (p == partida.PuestoLocal)
                continue;

            GestosMus g = GestosDePuesto(p);

            if (g == null)
                continue;

            int objetivo = partida.MiradaDe(p);
            bool cartas = objetivo < 0 && partida.Estado.EnCurso && !enRevision;

            if (cartas && !g.MirandoCartas)
                RotuloDemoMiraCartas(p);

            g.MirandoCartas = cartas;

            if (objetivo >= 0)
                g.MirarA(CabezaDePuesto(objetivo), 1f);
        }
    }

    public string QuienMeMira()
    {
        int local = partida.PuestoLocal;

        if (!partida.SenasConMirada || local < 0)
            return "";

        int companero = (local + 2) % 4;
        string rivales = "";

        for (int p = 0; p < 4; p++)
        {
            if (p == local || p == companero || partida.MiradaDe(p) != local)
                continue;

            rivales += (rivales.Length > 0 ? ", " : "") + ColoresJugadoresMus.Nombre(p);
        }

        string texto = partida.MiradaDe(companero) == local
            ? "<color=#7CD67C>Tu companero te mira</color>"
            : "<color=#BBBBBB>Tu companero no te mira</color>";

        if (rivales.Length > 0)
            texto += "\n<color=#FF6B5E>Te vigilan: </color>" + rivales;
        else if (partida.MiradaDe((local + 1) % 4) < 0 && partida.MiradaDe((local + 3) % 4) < 0)
            texto += "\n<color=#FFD24D>Los rivales miran sus cartas: buen momento</color>";

        return texto;
    }

    private void ProcesarSenal(int puesto, TipoSenalMus tipo)
    {
        RotuloDemoSena(puesto, tipo);
        partida.AnadirSenalAlHistorial(puesto, tipo);

        int sitio = Relativo(puesto);
        int local = partida.PuestoLocal;

        if (usarGestosClaros)
        {
            PrepararGestos();

            if (gestos[sitio] != null && gestos[sitio].Preparado)
                gestos[sitio].Hacer(tipo);
        }
        else if ((int)tipo < 4 && senales != null && sitio < senales.Length && senales[sitio] != null)
        {
            senales[sitio].EjecutarSenal((ControlSenalesMus.TipoSenal)(int)tipo);
        }

        if (partida.SenasConMirada)
            return;

        if (local >= 0 && puesto == (local + 2) % 4)
        {
            avisoSena = ColoresJugadoresMus.Nombre(puesto) + " te hace una sena: <b>" + SenalesMus.Nombre(tipo) +
                        "</b>  <size=13>(" + SenalesMus.Gesto(tipo) + ")</size>";
            finAvisoSena = Time.time + 3.5f;
        }
        else if (puesto == local)
        {
            avisoSena = "Has hecho la sena: <b>" + SenalesMus.Nombre(tipo) + "</b>";
            finAvisoSena = Time.time + 2f;
        }
    }

    private void MostrarBocadillo(int puesto, string texto, bool fijo, Color color)
    {
        int sitio = Relativo(puesto);

        if (InterfazNuevaActiva && lienzoUI != null)
        {
            MostrarBocadilloUI(sitio, texto, color, fijo);
            return;
        }

        if (bocadillos == null || sitio >= bocadillos.Length || bocadillos[sitio] == null)
            return;

        if (fijo) bocadillos[sitio].MostrarFijo(texto, color);
        else bocadillos[sitio].Mostrar(texto, color);
    }

    private void OcultarBocadillos()
    {
        if (lienzoUI != null)
            OcultarBocadillosUI();

        if (bocadillos == null)
            return;

        foreach (BocadilloMus3D b in bocadillos)
            if (b != null)
                b.Ocultar();
    }

    private void OnGUI()
    {
        if (InterfazNuevaActiva)
            return;

        EscalaGUIMus.Aplicar();

        if (mostrarHud && !enRevision)
            DibujarHud();

        if (!enRevision || tabla == null)
            return;

        PrepararHud();

        if (estiloTexto == null)
        {
            estiloTexto = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, richText = true };
            estiloTexto.normal.textColor = Color.white;
            estiloCabecera = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, richText = true };
            estiloCabecera.normal.textColor = Color.white;
        }

        if (!tablaVisible)
        {
            Rect zonaBotones = new(EscalaGUIMus.Ancho / 2f - 200, EscalaGUIMus.Alto - 70, 400, 50);
            GUILayout.BeginArea(zonaBotones);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("VER TANTEO", GUILayout.Height(44)))
                tablaVisible = true;

            if (partida.PuedePasarDeMano && GUILayout.Button("SIGUIENTE MANO", GUILayout.Height(44)))
                partida.PedirSiguienteMano();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }

        float ancho = Mathf.Min(1150f, EscalaGUIMus.Ancho - 40f);
        float alto = Mathf.Min(660f, EscalaGUIMus.Alto - 40f);
        Rect zona = new(EscalaGUIMus.Ancho / 2f - ancho / 2f, EscalaGUIMus.Alto / 2f - alto / 2f, ancho, alto);
        GUI.DrawTexture(zona, texFondo);
        GUI.DrawTexture(zona, texFondo);

        float anchoManos = Mathf.Min(330f, ancho * 0.34f);
        DibujarManos(new Rect(zona.x + 12, zona.y + 12, anchoManos, alto - 80));

        Rect zonaTanteo = new(zona.x + anchoManos + 30, zona.y + 12, ancho - anchoManos - 42, alto - 80);
        GUILayout.BeginArea(zonaTanteo);
        float columna = (zonaTanteo.width - 120f) / 2f;

        GUILayout.Label("<size=20>TANTEO DE LA MANO</size>", estiloCabecera);
        GUILayout.BeginHorizontal();
        GUILayout.Label("", estiloCabecera, GUILayout.Width(110));
        GUILayout.Label("<color=#7CD67C>NOSOTROS</color>", estiloCabecera, GUILayout.Width(columna));
        GUILayout.Label("<color=#FF7A6E>ELLOS</color>", estiloCabecera, GUILayout.Width(columna));
        GUILayout.EndHorizontal();

        string[] colores = { "#FFC94D", "#6EC6FF", "#FF8AD8", "#FF9B5E" };

        for (int i = 0; i < tabla.Filas.Length; i++)
        {
            TablaTanteoMus.Fila fila = tabla.Filas[i];
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("<color=" + colores[i] + ">" + fila.Lance + "</color>", estiloCabecera, GUILayout.Width(110));
            GUILayout.Label(fila.Nosotros.Count == 0 ? "-" : string.Join("\n", fila.Nosotros), estiloTexto, GUILayout.Width(columna));
            GUILayout.Label(fila.Ellos.Count == 0 ? "-" : string.Join("\n", fila.Ellos), estiloTexto, GUILayout.Width(columna));
            GUILayout.EndHorizontal();
        }

        EstadoPublicoMus e = partida.Estado;
        int parejaLocal = partida.PuestoLocal >= 0 ? partida.PuestoLocal % 2 : 0;
        int nuestros = parejaLocal == 0 ? e.Puntos0 : e.Puntos1;
        int suyos = parejaLocal == 0 ? e.Puntos1 : e.Puntos0;

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.Label("ESTA MANO", estiloCabecera, GUILayout.Width(110));
        GUILayout.Label("+" + tabla.TotalNosotros, estiloCabecera, GUILayout.Width(columna));
        GUILayout.Label("+" + tabla.TotalEllos, estiloCabecera, GUILayout.Width(columna));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("TOTAL", estiloCabecera, GUILayout.Width(110));
        GUILayout.Label("<size=22>" + nuestros + "</size> / " + e.Limite, estiloCabecera, GUILayout.Width(columna));
        GUILayout.Label("<size=22>" + suyos + "</size> / " + e.Limite, estiloCabecera, GUILayout.Width(columna));
        GUILayout.EndHorizontal();

        if (tabla.CobradoAntesNosotros + tabla.CobradoAntesEllos > 0)
            GUILayout.Label("<i>(ya sumado) = los no queridos se cobran en el momento.</i>", estiloTexto);

        if (!e.EnCurso)
            GUILayout.Label("<size=24><color=#FFC94D>" + TextoFase(e, false) + "</color></size>", estiloCabecera);

        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(zona.x + 12, zona.yMax - 60, ancho - 24, 50));
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("VER LAS CARTAS EN LA MESA", GUILayout.Height(44)))
            tablaVisible = false;

        if (partida.PuedePasarDeMano && GUILayout.Button("SIGUIENTE MANO", GUILayout.Height(44)))
            partida.PedirSiguienteMano();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DibujarManos(Rect zona)
    {
        if (manosVistas == null)
            return;

        int local = Mathf.Max(0, partida.PuestoLocal);
        int[] orden = { local, (local + 2) % 4, (local + 1) % 4, (local + 3) % 4 };
        float alturaBloque = zona.height / 4f;
        float anchoCarta = Mathf.Min((zona.width - 20f) / 4f, (alturaBloque - 30f) / 1.57f);
        float altoCarta = anchoCarta * 1.57f;

        for (int b = 0; b < 4; b++)
        {
            int puesto = orden[b];
            float y = zona.y + b * alturaBloque;
            bool nuestro = puesto % 2 == local % 2;
            int[] mano = { manosVistas[puesto * 4], manosVistas[puesto * 4 + 1], manosVistas[puesto * 4 + 2], manosVistas[puesto * 4 + 3] };
            EvaluadorManoMus.Resultado r = CartasMus.Evaluar(mano);
            string resumen = r.DetallePares + ", " + (r.TieneJuego ? "juego " : "punto ") + r.PuntosJuego;
            string titulo = ColoresJugadoresMus.Nombre(puesto) + (puesto == local ? " (tu)" : "") +
                            "  <size=12><color=" + (nuestro ? "#7CD67C" : "#FF7A6E") + ">" + (nuestro ? "nosotros" : "ellos") + "</color>  " + resumen + "</size>";

            GUI.Label(new Rect(zona.x, y, zona.width, 24), titulo, estiloCabecera);

            for (int c = 0; c < 4; c++)
            {
                Rect rc = new(zona.x + c * (anchoCarta + 4), y + 26, anchoCarta, altoCarta);
                Sprite sprite = BarajaSpritesMus.Obtener(CartasMus.Texto(mano[c]));

                if (sprite != null)
                {
                    Texture2D tex = sprite.texture;
                    Rect tr = sprite.textureRect;
                    GUI.DrawTextureWithTexCoords(rc, tex, new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height));
                }
                else
                {
                    GUI.Box(rc, CartasMus.TextoCorto(mano[c]));
                }
            }
        }
    }


    private static Texture2D Textura(Color color)
    {
        Texture2D t = new(1, 1);
        t.SetPixel(0, 0, color);
        t.Apply();
        return t;
    }

    private void PrepararHud()
    {
        if (estiloBanner != null)
            return;

        texFondo = Textura(new Color(0.08f, 0.06f, 0.05f, 0.85f));
        texCelda = Textura(new Color(0.25f, 0.2f, 0.16f, 0.9f));
        texCeldaActiva = Textura(new Color(0.85f, 0.6f, 0.1f, 0.95f));
        texNos = Textura(new Color(0.2f, 0.65f, 0.3f, 1f));
        texEllos = Textura(new Color(0.8f, 0.25f, 0.2f, 1f));
        texBarra = Textura(new Color(1f, 1f, 1f, 0.15f));

        estiloTitulo = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        estiloTitulo.normal.textColor = Color.white;
        estiloPeque = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.UpperCenter, wordWrap = true };
        estiloPeque.normal.textColor = new Color(1f, 0.95f, 0.8f);
        estiloBanner = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
        estiloBanner.normal.textColor = Color.white;
        estiloBanner.normal.background = texFondo;
        estiloAyuda = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        estiloAyuda.normal.textColor = new Color(1f, 0.92f, 0.7f);
        estiloAyuda.normal.background = texFondo;
    }

    private void DibujarHud()
    {
        if (!partida.IsSpawned || !partida.PartidaEmpezada)
            return;

        PrepararHud();

        EstadoPublicoMus e = partida.Estado;
        int local = partida.PuestoLocal;
        int pareja = local >= 0 ? local % 2 : 0;
        int nuestros = pareja == 0 ? e.Puntos0 : e.Puntos1;
        int suyos = pareja == 0 ? e.Puntos1 : e.Puntos0;
        int limite = Mathf.Max(1, e.Limite);

        float ancho = Mathf.Min(700f, EscalaGUIMus.Ancho - 40f);
        float x = EscalaGUIMus.Ancho / 2f - ancho / 2f;
        GUI.DrawTexture(new Rect(x, 8, ancho, 96), texFondo);

        float wm = 160f;
        GUI.Label(new Rect(x + 8, 12, wm, 22), "NOSOTROS  " + nuestros, estiloTitulo);
        Barra(new Rect(x + 16, 36, wm - 16, 8), nuestros / (float)limite, texNos);
        GUI.Label(new Rect(x + 8, 52, wm, 22), "ELLOS  " + suyos, estiloTitulo);
        Barra(new Rect(x + 16, 76, wm - 16, 8), suyos / (float)limite, texEllos);
        GUI.Label(new Rect(x + 8, 84, wm, 18), "a " + e.Limite + " tantos", estiloPeque);

        string[] titulos = { "MUS", "GRANDE", "CHICA", "PARES", modoUltimoLance ?? "JUEGO/PUNTO" };
        FaseMus fase = cola.Count > 0 ? faseMostrada : e.FaseActual;
        int activa = fase == FaseMus.Mus || fase == FaseMus.Descarte ? 0 : IndiceLance(fase) + 1;
        float wc = (ancho - wm - 16f) / 5f;

        for (int i = 0; i < 5; i++)
        {
            Rect r = new(x + wm + 8 + i * wc, 12, wc - 6, 88);
            GUI.DrawTexture(r, i == activa && e.EnCurso && fase != FaseMus.FinMano ? texCeldaActiva : texCelda);
            int tamanoTitulo = estiloTitulo.fontSize;
            if (titulos[i].Length > 8) estiloTitulo.fontSize = 12;
            GUI.Label(new Rect(r.x, r.y + 4, r.width, 22), titulos[i], estiloTitulo);
            estiloTitulo.fontSize = tamanoTitulo;

            string detalle = i == 0
                ? ((cola.Count > 0 ? corridoVisual : e.MusCorrido) ? "MUS CORRIDO\nel que corte\nsera mano" : activa == 0 ? "Ronda " + e.RondaMus + "\nMano: " + Nombre(e.Mano) : "Mano: " + Nombre(e.Mano))
                : resultadosLance[i - 1] ?? "";

            GUI.Label(new Rect(r.x + 3, r.y + 30, r.width - 6, 56), detalle, estiloPeque);
        }

        string banner = TextoBanner(e, local, out string ayuda);

        if (avisoTemporal != null && Time.time < finAvisoTemporal)
        {
            banner = avisoTemporal;
            ayuda = avisoAyuda;
        }

        if (!string.IsNullOrEmpty(banner))
            GUI.Label(new Rect(EscalaGUIMus.Ancho / 2f - 260, 112, 520, 42), banner, estiloBanner);

        if (!string.IsNullOrEmpty(ayuda))
            GUI.Label(new Rect(EscalaGUIMus.Ancho / 2f - 260, 156, 520, 28), ayuda, estiloAyuda);

        if (estiloSena == null)
        {
            estiloSena = new GUIStyle(estiloAyuda) { richText = true, fontSize = 17 };
            estiloSena.normal.textColor = new Color(0.85f, 0.75f, 1f);
        }

        if (avisoSena != null && Time.time < finAvisoSena)
            GUI.Label(new Rect(EscalaGUIMus.Ancho / 2f - 300, 190, 600, 34), avisoSena, estiloSena);

        if (mostrarBotonesSenas)
            DibujarBotonesSenas(e, local);
    }

    private void DibujarBotonesSenas(EstadoPublicoMus e, int local)
    {
        if (local < 0 || cartasLocalesMostradas[0] < 0 || !e.EnCurso || e.FaseActual == FaseMus.FinMano)
            return;

        if (e.MusCorrido)
        {
            Rect aviso = new(20, EscalaGUIMus.Alto / 2f - 40, 230, 80);
            GUI.DrawTexture(aviso, texFondo);
            GUI.Label(new Rect(aviso.x, aviso.y + 4, aviso.width, 24), "SENAS A TU COMPANERO", estiloTitulo);
            GUI.Label(new Rect(aviso.x + 6, aviso.y + 30, aviso.width - 12, 44), "En el mus corrido no se hacen senas", estiloPeque);
            return;
        }

        EvaluadorManoMus.Resultado r = CartasMus.Evaluar(cartasLocalesMostradas);
        List<TipoSenalMus> posibles = new();

        foreach (TipoSenalMus tipo in (TipoSenalMus[])Enum.GetValues(typeof(TipoSenalMus)))
            if (PartidaMus.CumpleSenal(cartasLocalesMostradas, r, tipo))
                posibles.Add(tipo);

        float alto = 34 + posibles.Count * 34;
        Rect zona = new(20, EscalaGUIMus.Alto / 2f - alto / 2f, 230, Mathf.Max(alto, 60));
        GUI.DrawTexture(zona, texFondo);
        GUI.Label(new Rect(zona.x, zona.y + 4, zona.width, 24), "SENAS A TU COMPANERO", estiloTitulo);

        if (posibles.Count == 0)
        {
            GUI.Label(new Rect(zona.x + 6, zona.y + 30, zona.width - 12, 24), "Ninguna con estas cartas", estiloPeque);
            return;
        }

        for (int i = 0; i < posibles.Count; i++)
        {
            TipoSenalMus tipo = posibles[i];
            bool hecha = senasHechas.Contains(tipo);
            GUI.enabled = !hecha;

            if (GUI.Button(new Rect(zona.x + 8, zona.y + 32 + i * 34, zona.width - 16, 30),
                    SenalesMus.Nombre(tipo) + (hecha ? "  (hecha)" : "")))
            {
                senasHechas.Add(tipo);
                partida.SolicitarSenal(tipo);
            }

            GUI.enabled = true;
        }
    }

    private void Barra(Rect r, float valor, Texture2D relleno)
    {
        GUI.DrawTexture(r, texBarra);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(valor), r.height), relleno);
    }

    private string TextoBanner(EstadoPublicoMus e, int local, out string ayuda)
    {
        ayuda = null;

        if (cola.Count > 0)
            return null;

        if (!e.EnCurso)
            return TextoFase(e, false);

        if (e.FaseActual == FaseMus.FinMano)
            return "FIN DE LA MANO";

        bool miTurno = local >= 0 && e.JugadorActual == local;
        bool hayEnvite = e.EstadoEnvite == (int)MotorEnvitesMus.EstadoEnvite.PendienteRespuesta ||
                         e.EstadoEnvite == (int)MotorEnvitesMus.EstadoEnvite.OrdagoPendiente;

        if (hayEnvite && PartidaMus.EsLance(e.FaseActual))
        {
            ayuda = (e.FaseActual == FaseMus.JuegoPunto && modoUltimoLance != null ? "[" + modoUltimoLance + "]  " : "") +
                (e.EstadoEnvite == (int)MotorEnvitesMus.EstadoEnvite.OrdagoPendiente
                ? "ORDAGO de " + Pareja(e.ParejaEnvidante) + ": si se quiere, se acaba la partida"
                : "En juego: " + e.TantosApostados + " tantos (envida " + Pareja(e.ParejaEnvidante) + ")");
        }

        if (!miTurno)
        {
            int j = e.JugadorActual;
            string quien = j < 0 ? "" : ColoresJugadoresMus.Nombre(j) + (local >= 0 && j == (local + 2) % 4 ? " (tu companero)" : local >= 0 ? " (rival)" : "");
            return "Turno de " + quien;
        }

        switch (e.FaseActual)
        {
            case FaseMus.Mus:
                ayuda ??= e.MusCorrido
                    ? "MUS CORRIDO: si dices NO HAY MUS, empiezas siendo mano"
                    : "MUS para cambiar cartas, NO HAY MUS para jugar con estas";
                return "TE TOCA: MUS?";
            case FaseMus.Descarte:
                ayuda ??= "Toca las cartas que quieres cambiar y pulsa descartar";
                return "TE TOCA: DESCARTE";
            default:
                if (hayEnvite)
                    return "TE TOCA RESPONDER";
                if (e.FaseActual == FaseMus.JuegoPunto && modoUltimoLance != null)
                    ayuda ??= "Se juega a " + modoUltimoLance + ": PASO, ENVIDO u ORDAGO";
                ayuda ??= "PASO, ENVIDO u ORDAGO";
                return "TE TOCA HABLAR";
        }
    }

    private void Anunciar(string texto)
    {
        if (textoAnuncios == null || enRevision)
            return;

        if (mostrarHud && ocultarAnunciosConHud)
        {
            textoAnuncios.gameObject.SetActive(false);
            return;
        }

        textoAnuncios.gameObject.SetActive(true);
        textoAnuncios.text = texto;
    }
}
