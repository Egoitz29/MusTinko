using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public struct EstadoPublicoMus : INetworkSerializable
{
    public bool EnCurso;
    public int Fase;
    public int Mano;
    public int JugadorActual;
    public int Puntos0;
    public int Puntos1;
    public int Limite;
    public int RondaMus;
    public int EstadoEnvite;
    public int TantosApostados;
    public int ParejaEnvidante;
    public int ParejaGanadora;
    public bool PorOrdago;
    public bool MusCorrido;
    public int TantosEnvite;
    public int MascaraBots;
    public int MascaraLegales;
    public int MascaraHablan;
    public ulong Cliente0;
    public ulong Cliente1;
    public ulong Cliente2;
    public ulong Cliente3;
    public int Apuesta;
    public int CodigoPartida;
    public double FinTurno;        // hora del servidor en que se acaba el tiempo del que habla (0 = sin reloj)
    public float DuracionTurno;    // segundos totales de ese turno (para dibujar cuanto queda)

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref EnCurso);
        s.SerializeValue(ref Fase);
        s.SerializeValue(ref Mano);
        s.SerializeValue(ref JugadorActual);
        s.SerializeValue(ref Puntos0);
        s.SerializeValue(ref Puntos1);
        s.SerializeValue(ref Limite);
        s.SerializeValue(ref RondaMus);
        s.SerializeValue(ref EstadoEnvite);
        s.SerializeValue(ref TantosApostados);
        s.SerializeValue(ref ParejaEnvidante);
        s.SerializeValue(ref ParejaGanadora);
        s.SerializeValue(ref PorOrdago);
        s.SerializeValue(ref MusCorrido);
        s.SerializeValue(ref TantosEnvite);
        s.SerializeValue(ref MascaraBots);
        s.SerializeValue(ref MascaraLegales);
        s.SerializeValue(ref MascaraHablan);
        s.SerializeValue(ref Cliente0);
        s.SerializeValue(ref Cliente1);
        s.SerializeValue(ref Cliente2);
        s.SerializeValue(ref Cliente3);
        s.SerializeValue(ref Apuesta);
        s.SerializeValue(ref CodigoPartida);
        s.SerializeValue(ref FinTurno);
        s.SerializeValue(ref DuracionTurno);
    }

    public ulong Cliente(int puesto)
    {
        switch (puesto)
        {
            case 0: return Cliente0;
            case 1: return Cliente1;
            case 2: return Cliente2;
            case 3: return Cliente3;
            default: return ulong.MaxValue;
        }
    }

    public bool EsBot(int puesto) => (MascaraBots & (1 << puesto)) != 0;
    public bool Legal(TipoAccionMus tipo) => (MascaraLegales & (1 << (int)tipo)) != 0;
    public FaseMus FaseActual => (FaseMus)Fase;
}

public struct EventoRedMus : INetworkSerializable
{
    public int Tipo;
    public int Fase;
    public int Jugador;
    public int AccionTipo;
    public int AccionValor;
    public int Dato;

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref Tipo);
        s.SerializeValue(ref Fase);
        s.SerializeValue(ref Jugador);
        s.SerializeValue(ref AccionTipo);
        s.SerializeValue(ref AccionValor);
        s.SerializeValue(ref Dato);
    }

    public EventoMus AEvento()
    {
        return new EventoMus((TipoEventoMus)Tipo, (FaseMus)Fase, Jugador,
            new AccionMus((TipoAccionMus)AccionTipo, AccionValor), Dato);
    }
}

public struct ManosReveladasMus : INetworkSerializable
{
    public int[] Cartas;
    public int[] Desglose;

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref Cartas);
        s.SerializeValue(ref Desglose);
    }

    public static int[] Empaquetar(IReadOnlyList<ConceptoTanteoMus> conceptos)
    {
        int[] datos = new int[conceptos.Count * 5];

        for (int i = 0; i < conceptos.Count; i++)
        {
            datos[i * 5] = conceptos[i].Lance;
            datos[i * 5 + 1] = (int)conceptos[i].Tipo;
            datos[i * 5 + 2] = conceptos[i].Pareja;
            datos[i * 5 + 3] = conceptos[i].Tantos;
            datos[i * 5 + 4] = conceptos[i].Jugador;
        }

        return datos;
    }

    public static ConceptoTanteoMus[] Desempaquetar(int[] datos)
    {
        if (datos == null)
            return new ConceptoTanteoMus[0];

        ConceptoTanteoMus[] conceptos = new ConceptoTanteoMus[datos.Length / 5];

        for (int i = 0; i < conceptos.Length; i++)
            conceptos[i] = new ConceptoTanteoMus(datos[i * 5], (TipoConceptoMus)datos[i * 5 + 1],
                datos[i * 5 + 2], datos[i * 5 + 3], datos[i * 5 + 4]);

        return conceptos;
    }
}

public class PartidaOnlineMus : NetworkBehaviour
{
    public const ulong SinCliente = ulong.MaxValue;
    // Nombre del personaje sentado en cada silla (cambia segun quien se siente).
    private static string[] NombresPuesto => ColoresJugadoresMus.NombresPorPuesto;

    [Header("Partida")]
    [SerializeField] private int limiteTantos = 40;
    [SerializeField] private int maxRondasMus = 0;

    [Header("Bots")]
    [SerializeField] private float esperaBot = 1.5f;
    [SerializeField] private float esperaFinMano = 8f;
    [SerializeField] private bool siguienteManoManual = true;
    [SerializeField] private TextAsset parametrosBotEntrenado;
    [Tooltip("Bot nuevo: decide con las probabilidades de 132 millones de repartos y envida cantidades libres. Si se apaga, se usa el bot de reglas de antes.")]
    [SerializeField] private bool usarBotProbabilista = true;

    [Header("Interfaz de prueba (desactivar en la mesa 3D)")]
    [SerializeField] private bool mostrarPanelPrueba = true;
    [SerializeField] private bool mostrarHistorial = true;

    [Header("Registro para entrenamiento")]
    [SerializeField] private bool registrarDecisiones = true;

    public event Action<EventoMus> EventoRecibido;
    public event Action<int, TipoSenalMus> SenalRecibida;
    public event Action ManoPrivadaActualizada;
    public event Action<int[]> ManosReveladas;

    public EstadoPublicoMus Estado => estado;
    public int PuestoLocal { get; private set; } = -1;
    public IReadOnlyList<int> CartasLocales => cartasLocales;
    public ConceptoTanteoMus[] UltimoDesglose { get; private set; } = new ConceptoTanteoMus[0];

    private PartidaMus partida;
    private readonly ulong[] clientes = { SinCliente, SinCliente, SinCliente, SinCliente };
    private readonly IBotMus[] bots = new IBotMus[4];
    private readonly string[] firmaManoEnviada = new string[4];
    private readonly System.Random azarBots = new();
    private float momentoAccionBot;
    private float momentoSiguienteMano;
    private bool pausado;
    private int manoConSenalesBots = -1;
    private RegistroExperienciasMus registro;
    private int idPartida;
    private int numeroMano;

    private EstadoPublicoMus estado = EstadoInicial();

    // Sin esto ParejaGanadora vale 0 y parece que la partida ya ha terminado (y se esconde la sala).
    private static EstadoPublicoMus EstadoInicial() => new EstadoPublicoMus
    {
        ParejaGanadora = -1,
        JugadorActual = -1,
        Cliente0 = SinCliente,
        Cliente1 = SinCliente,
        Cliente2 = SinCliente,
        Cliente3 = SinCliente
    };
    private readonly int[] cartasLocales = { -1, -1, -1, -1 };
    private readonly string[] bocadillos = new string[4];
    private int[] manosReveladas;
    private int mascaraDescarte;
    private readonly List<string> ultimosEventos = new();
    private bool reiniciarHistorial = true;
    private int manoHistorial;
    private FaseMus faseHistorial = FaseMus.FinPartida;
    private int declaracionesJuegoHistorial;
    private bool juegoEnHistorial;
    private Vector2 scrollHistorial;
    private GUIStyle estiloHistorial;
    private bool historialAbierto = !Application.isMobilePlatform;

    public bool ModoRevision { get; set; }
    public bool HistorialAlRitmoDeLaMesa { get; set; }
    public bool EsperandoAnimaciones { get; set; }
    private Vector2 scroll;

    public override void OnNetworkSpawn()
    {
        estado = EstadoInicial();

        if (!IsServer)
            return;

        NetworkManager.OnClientConnectedCallback += ClienteConectado;
        NetworkManager.OnClientDisconnectCallback += ClienteDesconectado;
        PrepararSala();
    }

    public override void OnNetworkDespawn()
    {
        ModoDemo = false;
        estado = EstadoInicial();

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= ClienteConectado;
            NetworkManager.OnClientDisconnectCallback -= ClienteDesconectado;
        }

        CerrarRegistro();
    }

    public override void OnDestroy()
    {
        CerrarRegistro();
        base.OnDestroy();
    }

    private readonly ulong[] sitiosSala = { SinCliente, SinCliente, SinCliente, SinCliente };
    private readonly ulong[] sitiosSalaVistos = { SinCliente, SinCliente, SinCliente, SinCliente };
    private static readonly int[] OrdenSitiosLibres = { 2, 1, 3, 0 };

    public bool PartidaEnCurso => partida != null && !partida.PartidaTerminada;
    public ulong ClienteEnSitio(int puesto) => puesto >= 0 && puesto < 4 ? sitiosSalaVistos[puesto] : SinCliente;

    public void ElegirSitio(int puesto) => ElegirSitioRpc(puesto);

    private void PrepararSala()
    {
        if (!IsServer)
            return;

        for (int p = 0; p < 4; p++)
            if (sitiosSala[p] != SinCliente && !EstaConectado(sitiosSala[p]))
                sitiosSala[p] = SinCliente;

        DifundirSala();
    }

    private readonly Dictionary<ulong, string> nombresClientes = new();
    private readonly string[] nombresSalaVistos = { "", "", "", "" };
    public string SinSitioVisto { get; private set; } = "";
    public const int LargoMaximoNombre = 16;

    public string NombreEnSitio(int puesto) => puesto >= 0 && puesto < 4 ? nombresSalaVistos[puesto] : "";

    public void EnviarNombre(string nombre) => NombreRpc(nombre ?? "");

    // ---------- Aspecto (traje y color del nombre comprados en la taquilla) ----------

    private readonly Dictionary<ulong, int> aspectosClientes = new();
    private readonly int[] aspectosVistos = new int[4];
    public int VersionSala { get; private set; }
    public int AspectoEnSitio(int puesto) => puesto >= 0 && puesto < 4 && sitiosSalaVistos[puesto] != SinCliente ? aspectosVistos[puesto] : 0;

    public void EnviarAspecto(int aspecto) => AspectoRpc(aspecto);

    [Rpc(SendTo.Server)]
    private void AspectoRpc(int aspecto, RpcParams parametros = default)
    {
        aspectosClientes[parametros.Receive.SenderClientId] = Mathf.Max(0, aspecto);
        DifundirSala();
    }

    private int AspectoDe(ulong id) => id != SinCliente && aspectosClientes.TryGetValue(id, out int a) ? a : 0;

    public static string LimpiarNombre(string nombre)
    {
        nombre = (nombre ?? "").Trim().Replace("<", "").Replace(">", "").Replace("\n", " ");
        return nombre.Length > LargoMaximoNombre ? nombre.Substring(0, LargoMaximoNombre) : nombre;
    }

    private string NombreDe(ulong id)
    {
        if (nombresClientes.TryGetValue(id, out string n) && n.Length > 0)
            return n;

        return id == Unity.Netcode.NetworkManager.ServerClientId ? "Host" : "Jugador " + id;
    }

    [Rpc(SendTo.Server)]
    private void NombreRpc(string nombre, RpcParams parametros = default)
    {
        nombresClientes[parametros.Receive.SenderClientId] = LimpiarNombre(nombre);
        DifundirSala();
    }

    [Header("Mesas con entrada (monedas)")]
    [SerializeField] private int[] entradasMesa = { 0, 50, 200, 1000 };
    private int apuestaSala;
    private int apuestaPartida;
    private int codigoPartida;

    public IReadOnlyList<int> EntradasMesa => entradasMesa;
    public int ApuestaSalaVista { get; private set; }

    public void CambiarApuesta(int entrada)
    {
        if (!IsServer || PartidaEnCurso || entrada < 0)
            return;

        apuestaSala = entrada;
        DifundirSala();
    }

    private void DifundirSala()
    {
        if (!IsServer)
            return;

        string[] n = new string[4];

        for (int p = 0; p < 4; p++)
            n[p] = sitiosSala[p] == SinCliente ? "" : NombreDe(sitiosSala[p]);

        List<string> sinSitio = new();

        foreach (ulong id in NetworkManager.ConnectedClientsIds)
            if (Array.IndexOf(sitiosSala, id) < 0)
                sinSitio.Add(NombreDe(id));

        int[] pedidos = new int[4];

        for (int p = 0; p < 4; p++)
            pedidos[p] = sitiosSala[p] == SinCliente ? -1 : TaquillaMus.PersonajeDe(AspectoDe(sitiosSala[p]));

        int[] pj = ColoresJugadoresMus.Repartir(pedidos);

        SalaRpc(sitiosSala[0], sitiosSala[1], sitiosSala[2], sitiosSala[3], n[0], n[1], n[2], n[3], string.Join(", ", sinSitio), apuestaSala,
            AspectoDe(sitiosSala[0]), AspectoDe(sitiosSala[1]), AspectoDe(sitiosSala[2]), AspectoDe(sitiosSala[3]),
            pj[0] * 1000 + pj[1] * 100 + pj[2] * 10 + pj[3]);
    }

    private bool EstaConectado(ulong id)
    {
        foreach (ulong c in NetworkManager.ConnectedClientsIds)
            if (c == id)
                return true;

        return false;
    }

    private void SentarEnLibre(ulong id, bool esHost)
    {
        if (esHost && sitiosSala[0] == SinCliente)
        {
            sitiosSala[0] = id;
            return;
        }

        foreach (int p in OrdenSitiosLibres)
        {
            if (sitiosSala[p] != SinCliente)
                continue;

            sitiosSala[p] = id;
            return;
        }
    }

    [Rpc(SendTo.Server)]
    private void ElegirSitioRpc(int puesto, RpcParams parametros = default)
    {
        if (PartidaEnCurso || puesto < 0 || puesto > 3)
            return;

        ulong quien = parametros.Receive.SenderClientId;

        if (sitiosSala[puesto] != SinCliente && sitiosSala[puesto] != quien)
            return;

        for (int p = 0; p < 4; p++)
            if (sitiosSala[p] == quien)
                sitiosSala[p] = SinCliente;

        sitiosSala[puesto] = quien;
        DifundirSala();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SalaRpc(ulong c0, ulong c1, ulong c2, ulong c3, string n0, string n1, string n2, string n3, string sinSitio, int apuesta, int a0, int a1, int a2, int a3, int personajes)
    {
        ColoresJugadoresMus.PonerPersonajes(personajes / 1000 % 10, personajes / 100 % 10, personajes / 10 % 10, personajes % 10);
        ColoresJugadoresMus.PonerApodos(c0 == SinCliente ? "" : n0, c1 == SinCliente ? "" : n1, c2 == SinCliente ? "" : n2, c3 == SinCliente ? "" : n3);
        ApuestaSalaVista = apuesta;
        aspectosVistos[0] = a0;
        aspectosVistos[1] = a1;
        aspectosVistos[2] = a2;
        aspectosVistos[3] = a3;
        VersionSala++;
        nombresSalaVistos[0] = n0;
        nombresSalaVistos[1] = n1;
        nombresSalaVistos[2] = n2;
        nombresSalaVistos[3] = n3;
        SinSitioVisto = sinSitio;
        sitiosSalaVistos[0] = c0;
        sitiosSalaVistos[1] = c1;
        sitiosSalaVistos[2] = c2;
        sitiosSalaVistos[3] = c3;
    }

    private void ClienteConectado(ulong id)
    {
        if (partida != null && !partida.PartidaTerminada)
            EstadoParaClienteRpc(estado, RpcTarget.Single(id, RpcTargetUse.Temp));
        else
            PrepararSala();

        DifundirSala();
    }

    private void ClienteDesconectado(ulong id)
    {
        nombresClientes.Remove(id);
        aspectosClientes.Remove(id);

        for (int p = 0; p < 4; p++)
            if (sitiosSala[p] == id && !PartidaEnCurso)
                sitiosSala[p] = SinCliente;

        if (!PartidaEnCurso)
            DifundirSala();

        if (partida == null)
            return;

        for (int p = 0; p < 4; p++)
        {
            if (clientes[p] != id)
                continue;

            clientes[p] = SinCliente;
            bots[p] = CrearBot(p);
            Debug.LogWarning("[MUS ONLINE] Jugador " + p + " desconectado. Lo sustituye un bot.");
            momentoAccionBot = Time.time + EsperaBot;
        }

        Difundir();
    }

    [Header("Modo demo (4 bots solos, para grabar videos)")]
    [Tooltip("Cuanto mas lentos van los bots en la demo (1 = normal).")]
    [SerializeField] private float ritmoBotsDemo = 1.6f;

    public bool ModoDemo { get; private set; }
    private float EsperaBot => ModoDemo ? esperaBot * ritmoBotsDemo : esperaBot;

    // Solo el host en modo demo puede ver todas las manos (para ensenar a jugar).
    public int[] CartasDemo(int puesto)
    {
        if (!IsServer || !ModoDemo || partida == null || puesto < 0 || puesto > 3)
            return null;

        return partida.CartasDe(puesto);
    }

    public void EmpezarDemoServidor()
    {
        if (!IsServer)
            return;

        ModoDemo = true;

        for (int p = 0; p < 4; p++)
            sitiosSala[p] = SinCliente;

        Empezar(false);
    }

    // Salas por Internet: hacen falta al menos 2 personas para empezar (las sillas libres las ocupan bots).
    // Jugando solo contra bots o en la WiFi basta con 1.
    public int MinimoHumanos { get; set; } = 1;
    public int HumanosConectados => IsServer && NetworkManager != null ? NetworkManager.ConnectedClientsIds.Count : 0;

    public void EmpezarPartidaServidor()
    {
        if (!IsServer)
            return;

        if (HumanosConectados < MinimoHumanos)
        {
            Debug.Log("[MUS ONLINE] Faltan jugadores: hay " + HumanosConectados + " y hacen falta " + MinimoHumanos + ".");
            return;
        }

        ModoDemo = false;
        Empezar(true);
    }

    private void Empezar(bool sentarHumanos)
    {
        PrepararSala();

        if (sentarHumanos)
            foreach (ulong id in NetworkManager.ConnectedClientsIds)
                if (Array.IndexOf(sitiosSala, id) < 0)
                    SentarEnLibre(id, id == NetworkManager.ServerClientId);

        DifundirSala();

        for (int p = 0; p < 4; p++)
            clientes[p] = sitiosSala[p];

        for (int p = 0; p < 4; p++)
        {
            bots[p] = clientes[p] == SinCliente ? CrearBot(p) : null;
            firmaManoEnviada[p] = null;
        }

        CerrarRegistro();
        AbrirRegistro();

        partida = new PartidaMus(Environment.TickCount, limiteTantos, maxRondasMus);
        partida.EventoRegistrado += EventoServidor;
        partida.SenalRealizada += SenalServidor;
        partida.SenalVista += SenalVistaServidor;
        partida.SenasRequierenMirada = senasConMirada;
        senasPendientesBots.Clear();
        manoConSenalesBots = -1;
        idPartida++;
        apuestaPartida = ModoDemo ? 0 : apuestaSala;
        codigoPartida = azarBots.Next(1, int.MaxValue);
        registro?.EmpezarPartida(idPartida);
        partida.IniciarPartida();
        numeroMano++;
        momentoAccionBot = Time.time + EsperaBot;

        Debug.Log("[MUS ONLINE] Partida iniciada. Humanos: " + (4 - ContarBots()) + ", bots: " + ContarBots());
        Difundir();
    }

    private IBotMus CrearBot(int puesto)
    {
        string nombre = "Bot " + NombresPuesto[puesto];

        // Bot probabilista (entrenado con millones de partidas). Necesita las tablas de probabilidad.
        if (usarBotProbabilista && PrepararBotProbabilista())
            return new BotProbabilistaMus(nombre, parametrosProbabilista);

        ParametrosBotMus parametros = ParametrosBotMus.Equilibrado();

        if (parametrosBotEntrenado == null)
            parametrosBotEntrenado = Resources.Load<TextAsset>("BotEntrenadoMus");

        if (parametrosBotEntrenado != null)
        {
            try
            {
                parametros = EntrenadorBotsMus.Cargar(parametrosBotEntrenado.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MUS ONLINE] Parametros de bot no validos: " + e.Message);
            }
        }

        return new BotReglasMus(nombre, parametros);
    }

    private static ParametrosProbabilistaMus parametrosProbabilista;
    private static bool probabilistaIntentado;

    private static bool PrepararBotProbabilista()
    {
        if (probabilistaIntentado)
            return TablasProbabilidadMus.Cargadas;

        probabilistaIntentado = true;

        try
        {
            TextAsset tablas = Resources.Load<TextAsset>("Bots/ProbabilidadesMus");

            if (tablas == null || !TablasProbabilidadMus.Cargar(tablas.text))
            {
                Debug.LogWarning("[MUS ONLINE] No hay tablas de probabilidad (Resources/Bots/ProbabilidadesMus): se usa el bot de reglas.");
                return false;
            }

            TextAsset texto = Resources.Load<TextAsset>("Bots/BotProbabilistaMus");
            parametrosProbabilista = (texto != null ? ParametrosProbabilistaMus.Cargar(texto.text) : null) ?? new ParametrosProbabilistaMus();
            Debug.Log("[MUS ONLINE] Bot probabilista listo" + (texto != null ? " (entrenado): " : " (sin entrenar): ") + parametrosProbabilista);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MUS ONLINE] No se pudo cargar el bot probabilista: " + e.Message);
            return false;
        }
    }

    private int ContarBots()
    {
        int n = 0;

        for (int p = 0; p < 4; p++)
            if (bots[p] != null)
                n++;

        return n;
    }

    private void Update()
    {
        if (!IsServer || partida == null || partida.PartidaTerminada)
            return;

        if (pausado)
        {
            momentoAccionBot = Time.time + EsperaBot;
            momentoSiguienteMano = Time.time + esperaFinMano;

            // En pausa el reloj del turno no corre.
            if (finTurnoServidor > 0)
            {
                finTurnoServidor += Time.deltaTime;
                relojMovido += Time.deltaTime;

                if (relojMovido > 1f)
                {
                    relojMovido = 0f;
                    Difundir();
                }
            }

            return;
        }

        ComprobarTiempoAgotado();

        if (partida.Fase == FaseMus.FinMano)
        {
            if (EsperandoAnimaciones)
                momentoSiguienteMano = Mathf.Max(momentoSiguienteMano, Time.time + esperaFinMano);

            if (!siguienteManoManual && Time.time >= momentoSiguienteMano)
                SiguienteManoServidor();
            return;
        }

        int momentoSenales = partida.ManosJugadas * 100 + partida.RondaMus * 2 + (PartidaMus.EsLance(partida.Fase) ? 1 : 0);

        if ((partida.Fase == FaseMus.Mus || PartidaMus.EsLance(partida.Fase)) && !partida.MusCorridoPendiente && manoConSenalesBots != momentoSenales)
        {
            manoConSenalesBots = momentoSenales;

            for (int p = 0; p < 4; p++)
                if (bots[p] != null)
                    foreach (TipoSenalMus senal in bots[p].ElegirSenales(partida.Observar(p), azarBots))
                        if (senasConMirada)
                            senasPendientesBots.Add(new SenaPendiente { Bot = p, Tipo = senal, Limite = Time.time + 9f });
                        else
                            partida.HacerSenal(p, senal);
        }

        if (senasConMirada)
        {
            ActualizarMiradasBots();
            ProcesarSenasPendientesBots();
        }

        int actual = partida.JugadorActual;

        // En la demo los bots esperan a que la mesa termine de ensenar lo anterior.
        if (ModoDemo && EsperandoAnimaciones)
            momentoAccionBot = Mathf.Max(momentoAccionBot, Time.time + 0.5f);

        if (actual < 0 || bots[actual] == null || Time.time < momentoAccionBot)
            return;

        List<AccionMus> legales = partida.AccionesLegales(actual);

        if (legales.Count == 0)
            return;

        ObservacionMus obs = partida.Observar(actual);
        AccionMus accion = bots[actual].Decidir(obs, legales, azarBots);

        // EsLegal acepta los envites libres (ENVIDO 5, 3 MAS...).
        if (!partida.EsLegal(actual, accion))
            accion = SimuladorMus.AccionSegura(legales);

        EjecutarAccion(actual, accion, obs, legales, bots[actual].Nombre);
    }

    // ---------- Reloj de cada turno ----------

    [Header("Tiempo para responder")]
    [Tooltip("Segundos que tiene cada persona para decidir. Si se acaban, pasa sola (los bots no lo necesitan).")]
    [SerializeField] private float segundosParaResponder = 25f;
    [Tooltip("Segundos de mas por las animaciones (la mesa va un poco por detras del servidor).")]
    [SerializeField] private float margenAnimaciones = 3f;
    [Tooltip("Para probar: ensena tambien el reloj de los bots (lo que tardan en pensar).")]
    [SerializeField] private bool mostrarRelojBots = false;

    private int decisiones;
    private int decisionConPlazo = -1;
    private double finTurnoServidor;
    private float duracionTurnoServidor;
    private float relojMovido;

    private double AhoraServidor => NetworkManager != null ? NetworkManager.ServerTime.Time : Time.timeAsDouble;

    // Cada vez que hay una decision nueva se pone el reloj al que le toca.
    private double PlazoDelTurno(out float duracion)
    {
        int actual = partida != null ? partida.JugadorActual : -1;
        bool hayDecision = partida != null && !partida.PartidaTerminada && actual >= 0 && partida.Fase != FaseMus.FinMano;

        if (!hayDecision)
        {
            finTurnoServidor = 0;
            duracionTurnoServidor = 0;
            decisionConPlazo = -1;
        }
        else if (decisionConPlazo != decisiones)
        {
            decisionConPlazo = decisiones;
            bool esBot = bots[actual] != null;

            if (!esBot && segundosParaResponder > 0f)
            {
                duracionTurnoServidor = segundosParaResponder + margenAnimaciones;
                finTurnoServidor = AhoraServidor + duracionTurnoServidor;
            }
            else if (esBot && mostrarRelojBots)
            {
                duracionTurnoServidor = Mathf.Max(0.5f, EsperaBot);
                finTurnoServidor = AhoraServidor + duracionTurnoServidor;
            }
            else
            {
                finTurnoServidor = 0;
                duracionTurnoServidor = 0;
            }
        }

        duracion = duracionTurnoServidor;
        return finTurnoServidor;
    }

    // Si a una persona se le acaba el tiempo, "se le pasa": paso, no quiero, mus... lo mas prudente.
    private void ComprobarTiempoAgotado()
    {
        if (partida == null || partida.PartidaTerminada || finTurnoServidor <= 0 || AhoraServidor < finTurnoServidor)
            return;

        int actual = partida.JugadorActual;

        if (actual < 0 || bots[actual] != null)
            return;

        List<AccionMus> legales = partida.AccionesLegales(actual);

        if (legales.Count == 0)
            return;

        AccionMus accion = default;
        bool elegida = false;

        foreach (TipoAccionMus tipo in new[] { TipoAccionMus.Pasar, TipoAccionMus.NoQuiero, TipoAccionMus.Mus })
        {
            foreach (AccionMus a in legales)
                if (a.Tipo == tipo)
                {
                    accion = a;
                    elegida = true;
                    break;
                }

            if (elegida)
                break;
        }

        if (!elegida)
            accion = SimuladorMus.AccionSegura(legales);

        finTurnoServidor = 0;
        AnadirLinea("<color=#FF9B5E>" + ColoresJugadoresMus.Nombre(actual) + " no ha contestado a tiempo: " + accion + "</color>");
        EjecutarAccion(actual, accion, partida.Observar(actual), legales, "TIEMPO_" + actual);
    }

    private void SiguienteManoServidor()
    {
        if (partida == null || partida.Fase != FaseMus.FinMano)
            return;

        partida.SiguienteMano();
        numeroMano++;
        decisiones++;
        manosReveladas = null;
        momentoAccionBot = Time.time + EsperaBot;
        Difundir();
    }

    [Rpc(SendTo.Server)]
    private void AccionRpc(int tipo, int valor, RpcParams parametros = default)
    {
        if (partida == null || partida.PartidaTerminada)
            return;

        int puesto = PuestoDeCliente(parametros.Receive.SenderClientId);

        if (puesto < 0 || bots[puesto] != null)
        {
            Debug.LogWarning("[MUS ONLINE] Accion rechazada: el emisor no ocupa un puesto humano.");
            return;
        }

        if (puesto != partida.JugadorActual)
        {
            Debug.LogWarning("[MUS ONLINE] Accion rechazada: no es el turno del jugador " + puesto + ".");
            return;
        }

        AccionMus accion = new((TipoAccionMus)tipo, valor);
        List<AccionMus> legales = partida.AccionesLegales(puesto);

        // Envites libres: cualquier cantidad valida, no solo la de la lista.
        if (!partida.EsLegal(puesto, accion))
        {
            Debug.LogWarning("[MUS ONLINE] Accion rechazada: " + accion + " no es legal ahora.");
            return;
        }

        EjecutarAccion(puesto, accion, partida.Observar(puesto), legales, "HUMANO_" + puesto);
    }

    [Rpc(SendTo.Server)]
    private void SenalRpc(int tipo, RpcParams parametros = default)
    {
        if (partida == null)
            return;

        int puesto = PuestoDeCliente(parametros.Receive.SenderClientId);

        if (puesto < 0 || bots[puesto] != null || !partida.HacerSenal(puesto, (TipoSenalMus)tipo))
            Debug.LogWarning("[MUS ONLINE] Sena rechazada para el emisor " + parametros.Receive.SenderClientId);
        else
            InformarResultadoSena(puesto, (TipoSenalMus)tipo);
    }

    private void EjecutarAccion(int puesto, AccionMus accion, ObservacionMus obs, List<AccionMus> legales, string autor)
    {
        int manoAntes = partida.ManosJugadas;

        if (!partida.Aplicar(puesto, accion))
        {
            Debug.LogWarning("[MUS ONLINE] Las reglas rechazaron " + accion + " del jugador " + puesto);
            return;
        }

        registro?.RegistrarDecision(obs, autor, legales, accion, manoAntes);
        momentoAccionBot = Time.time + EsperaBot;
        decisiones++;

        if (partida.Fase == FaseMus.FinMano || partida.PartidaTerminada)
        {
            registro?.TerminarMano(partida);
            momentoSiguienteMano = Time.time + esperaFinMano;
            RevelarManos();
        }

        if (partida.PartidaTerminada)
        {
            registro?.TerminarPartida(partida.ParejaGanadora);
            Debug.Log("[MUS ONLINE] Fin de partida. Gana la pareja " + (partida.ParejaGanadora == 0 ? "0-2" : "1-3") +
                      ". " + partida.MotivoVictoria);
        }

        Difundir();
    }

    private void EventoServidor(EventoMus e)
    {
        EventoRpc(new EventoRedMus
        {
            Tipo = (int)e.Tipo,
            Fase = (int)e.Fase,
            Jugador = e.Jugador,
            AccionTipo = (int)e.Accion.Tipo,
            AccionValor = e.Accion.Valor,
            Dato = e.Dato
        });
    }

    private void SenalServidor(int jugador, TipoSenalMus tipo)
    {
        SenalVisibleRpc(jugador, (int)tipo);
    }

    private void SenalVistaServidor(int receptor, int emisor, TipoSenalMus tipo, bool pillada)
    {
        if (clientes[receptor] != SinCliente && bots[receptor] == null)
            SenalVistaRpc(emisor, (int)tipo, pillada, RpcTarget.Single(clientes[receptor], RpcTargetUse.Temp));

        if (pillada && clientes[emisor] != SinCliente && bots[emisor] == null)
            TeHanPilladoRpc(receptor, (int)tipo, RpcTarget.Single(clientes[emisor], RpcTargetUse.Temp));
    }

    private void InformarResultadoSena(int emisor, TipoSenalMus tipo)
    {
        if (clientes[emisor] == SinCliente || bots[emisor] != null)
            return;

        bool vista = partida.Mirando((emisor + 2) % 4) == emisor || !partida.SenasRequierenMirada;
        ResultadoSenaRpc((int)tipo, vista, RpcTarget.Single(clientes[emisor], RpcTargetUse.Temp));
    }

    [Header("Senas con mirada")]
    [Tooltip("Las senas solo las ve quien mira al que la hace; un rival que mira, la pilla.")]
    [SerializeField] private bool senasConMirada = true;

    private struct SenaPendiente
    {
        public int Bot;
        public TipoSenalMus Tipo;
        public float Limite;
    }

    private readonly List<SenaPendiente> senasPendientesBots = new();
    private readonly float[] proximoCambioMirada = new float[4];
    private readonly int[] miradasEnviadas = { -2, -2, -2, -2 };

    public event Action<int, TipoSenalMus, bool> SenalPercibida;
    public event Action<int, TipoSenalMus> TeHanPillado;
    public event Action<TipoSenalMus, bool> ResultadoSena;
    public event Action MiradasCambiadas;

    private readonly int[] miradasVistas = { -1, -1, -1, -1 };
    public int MiradaDe(int puesto) => puesto >= 0 && puesto < 4 ? miradasVistas[puesto] : -1;
    public bool SenasConMirada => senasConMirada;

    public void EnviarMirada(int objetivo) => MiradaRpc(objetivo);

    public const int LargoMaximoChat = 120;

    public struct MensajeChat
    {
        public int Puesto;
        public string Nombre;
        public string Texto;
        public float Hora;
    }

    private readonly List<MensajeChat> chat = new();
    private readonly Dictionary<ulong, float> ultimoMensajeChat = new();

    public IReadOnlyList<MensajeChat> Chat => chat;
    public int VersionChat { get; private set; }
    public event Action<MensajeChat> MensajeChatRecibido;

    public void EnviarChat(string texto)
    {
        texto = LimpiarChat(texto);

        if (texto.Length > 0)
            ChatRpc(texto);
    }

    private static string LimpiarChat(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return "";

        texto = texto.Trim().Replace("<", "(").Replace(">", ")").Replace("\n", " ").Replace("\r", " ");
        return texto.Length > LargoMaximoChat ? texto.Substring(0, LargoMaximoChat) : texto;
    }

    [Rpc(SendTo.Server)]
    private void ChatRpc(string texto, RpcParams parametros = default)
    {
        ulong quien = parametros.Receive.SenderClientId;

        if (ultimoMensajeChat.TryGetValue(quien, out float ultimo) && Time.time - ultimo < 0.8f)
            return;

        texto = LimpiarChat(texto);

        if (texto.Length == 0)
            return;

        ultimoMensajeChat[quien] = Time.time;
        int puesto = partida != null ? PuestoDeCliente(quien) : -1;

        if (puesto < 0)
            puesto = Array.IndexOf(sitiosSala, quien);

        ChatDifundirRpc(puesto, NombreDe(quien), texto);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ChatDifundirRpc(int puesto, string nombre, string texto)
    {
        MensajeChat m = new() { Puesto = puesto, Nombre = nombre, Texto = texto, Hora = Time.time };
        chat.Add(m);

        while (chat.Count > 60)
            chat.RemoveAt(0);

        VersionChat++;
        MensajeChatRecibido?.Invoke(m);
    }

    [Rpc(SendTo.Server)]
    private void MiradaRpc(int objetivo, RpcParams parametros = default)
    {
        if (partida == null)
            return;

        int puesto = PuestoDeCliente(parametros.Receive.SenderClientId);

        if (puesto < 0 || bots[puesto] != null)
            return;

        partida.Mirar(puesto, objetivo);
        DifundirMiradas();
    }

    private int momentoCartasNuevas = -1;

    private void ActualizarMiradasBots()
    {
        bool cambio = false;

        // Cuando llegan cartas nuevas (reparto o descarte), los bots se quedan un rato mirandolas.
        int momentoCartas = partida.ManosJugadas * 100 + partida.RondaMus;

        if (momentoCartas != momentoCartasNuevas)
        {
            momentoCartasNuevas = momentoCartas;

            for (int p = 0; p < 4; p++)
            {
                if (bots[p] == null)
                    continue;

                partida.Mirar(p, PartidaMus.MiraCartas);
                proximoCambioMirada[p] = Time.time + 1.5f + (float)azarBots.NextDouble() * 2.5f;
                cambio = true;
            }
        }

        for (int p = 0; p < 4; p++)
        {
            if (bots[p] == null || Time.time < proximoCambioMirada[p])
                continue;

            int companero = (p + 2) % 4;
            bool companeroQuiereSenar = false;

            foreach (SenaPendiente sp in senasPendientesBots)
                companeroQuiereSenar |= sp.Bot == companero;

            int actual = partida.JugadorActual;
            double r = azarBots.NextDouble();
            int objetivo;

            if (companeroQuiereSenar && r < 0.6) objetivo = companero;
            else if (actual >= 0 && actual != p && r < 0.6) objetivo = actual;
            else if (r < 0.74) objetivo = companero;
            else if (r < 0.84) objetivo = (p + 1 + 2 * azarBots.Next(2)) % 4;
            else objetivo = PartidaMus.MiraCartas;

            partida.Mirar(p, objetivo);
            float dura = objetivo == PartidaMus.MiraCartas ? 1.5f + (float)azarBots.NextDouble() * 2f : 1.2f + (float)azarBots.NextDouble() * 2.5f;
            proximoCambioMirada[p] = Time.time + dura;
            cambio = true;
        }

        if (cambio)
            DifundirMiradas();
    }

    private void ProcesarSenasPendientesBots()
    {
        for (int i = senasPendientesBots.Count - 1; i >= 0; i--)
        {
            SenaPendiente sp = senasPendientesBots[i];
            int companero = (sp.Bot + 2) % 4;
            bool companeroMira = partida.Mirando(companero) == sp.Bot;
            bool rivalMira = partida.Mirando((sp.Bot + 1) % 4) == sp.Bot || partida.Mirando((sp.Bot + 3) % 4) == sp.Bot;
            bool seAcaba = Time.time >= sp.Limite;

            if (!(companeroMira && !rivalMira) && !(seAcaba && companeroMira))
            {
                if (seAcaba)
                    senasPendientesBots.RemoveAt(i);
                continue;
            }

            senasPendientesBots.RemoveAt(i);

            if (partida.PuedeHacerSenal(sp.Bot, sp.Tipo))
                partida.HacerSenal(sp.Bot, sp.Tipo);
        }
    }

    private void DifundirMiradas()
    {
        bool igual = true;

        for (int p = 0; p < 4; p++)
            igual &= miradasEnviadas[p] == partida.Mirando(p);

        if (igual)
            return;

        for (int p = 0; p < 4; p++)
            miradasEnviadas[p] = partida.Mirando(p);

        MiradasRpc(miradasEnviadas[0], miradasEnviadas[1], miradasEnviadas[2], miradasEnviadas[3]);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void MiradasRpc(int m0, int m1, int m2, int m3)
    {
        miradasVistas[0] = m0;
        miradasVistas[1] = m1;
        miradasVistas[2] = m2;
        miradasVistas[3] = m3;
        MiradasCambiadas?.Invoke();
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SenalVistaRpc(int emisor, int tipo, bool pillada, RpcParams parametros)
    {
        TipoSenalMus t = (TipoSenalMus)tipo;
        string linea = pillada
            ? "<color=#FFB347>>> Has pillado a " + ColoresJugadoresMus.Nombre(emisor) + "<color=#FFB347>: " + SenalesMus.Nombre(t) + "</color></color>"
            : "<color=#C9A0FF>>> " + ColoresJugadoresMus.Nombre(emisor) + "<color=#C9A0FF> (tu companero) te hace la sena " + SenalesMus.Nombre(t) + "</color></color>";
        AnadirLinea(linea);
        SenalPercibida?.Invoke(emisor, t, pillada);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void TeHanPilladoRpc(int rival, int tipo, RpcParams parametros)
    {
        AnadirLinea("<color=#FF6B5E>>> Te ha pillado " + ColoresJugadoresMus.Nombre(rival) + "<color=#FF6B5E> la sena " + SenalesMus.Nombre((TipoSenalMus)tipo) + "</color></color>");
        TeHanPillado?.Invoke(rival, (TipoSenalMus)tipo);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ResultadoSenaRpc(int tipo, bool vista, RpcParams parametros)
    {
        AnadirLinea("<color=#C9A0FF>>> Haces la sena " + SenalesMus.Nombre((TipoSenalMus)tipo) + ": " +
                    (vista ? "tu companero la ha visto" : "tu companero no te miraba") + "</color>");
        ResultadoSena?.Invoke((TipoSenalMus)tipo, vista);
    }

    private void RevelarManos()
    {
        int[] todas = new int[16];

        for (int p = 0; p < 4; p++)
            for (int c = 0; c < 4; c++)
                todas[p * 4 + c] = partida.Carta(p, c);

        ManosReveladasRpc(new ManosReveladasMus
        {
            Cartas = todas,
            Desglose = ManosReveladasMus.Empaquetar(partida.Desglose)
        });
    }

    private void Difundir()
    {
        if (partida == null)
            return;

        EstadoPublicoMus e = new()
        {
            EnCurso = !partida.PartidaTerminada,
            Fase = (int)partida.Fase,
            Mano = partida.Mano,
            JugadorActual = partida.JugadorActual,
            Puntos0 = partida.Puntos(0),
            Puntos1 = partida.Puntos(1),
            Limite = partida.Limite,
            RondaMus = partida.RondaMus,
            EstadoEnvite = (int)partida.Envite.Estado,
            TantosApostados = partida.Envite.TantosApostados,
            ParejaEnvidante = partida.Envite.ParejaEnvidante,
            ParejaGanadora = partida.ParejaGanadora,
            PorOrdago = partida.VictoriaPorOrdago,
            MusCorrido = partida.MusCorridoPendiente,
            FinTurno = PlazoDelTurno(out float duracion),
            DuracionTurno = duracion,
            Apuesta = apuestaPartida,
            CodigoPartida = codigoPartida,
            TantosEnvite = partida.TantosEnvite,
            Cliente0 = clientes[0],
            Cliente1 = clientes[1],
            Cliente2 = clientes[2],
            Cliente3 = clientes[3]
        };

        for (int p = 0; p < 4; p++)
        {
            if (bots[p] != null) e.MascaraBots |= 1 << p;
            if (partida.PuedeHablarEnLance(p)) e.MascaraHablan |= 1 << p;
        }

        if (e.JugadorActual >= 0)
            foreach (AccionMus a in partida.AccionesLegales(e.JugadorActual))
                e.MascaraLegales |= 1 << (int)a.Tipo;

        estado = e;
        EstadoRpc(e);

        for (int p = 0; p < 4; p++)
        {
            if (clientes[p] == SinCliente)
                continue;

            int[] cartas = partida.CartasDe(p);
            string firma = numeroMano + ":" + string.Join(",", cartas);

            if (firma == firmaManoEnviada[p])
                continue;

            firmaManoEnviada[p] = firma;
            ManoPrivadaRpc(p, cartas[0], cartas[1], cartas[2], cartas[3], RpcTarget.Single(clientes[p], RpcTargetUse.Temp));
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void EstadoParaClienteRpc(EstadoPublicoMus nuevo, RpcParams parametros)
    {
        AplicarEstado(nuevo);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void EstadoRpc(EstadoPublicoMus nuevo)
    {
        AplicarEstado(nuevo);
    }

    private void AplicarEstado(EstadoPublicoMus nuevo)
    {
        estado = nuevo;
        PuestoLocal = -1;

        for (int p = 0; p < 4; p++)
            if (nuevo.Cliente(p) == NetworkManager.LocalClientId && !nuevo.EsBot(p))
                PuestoLocal = p;
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ManoPrivadaRpc(int puesto, int c0, int c1, int c2, int c3, RpcParams parametros)
    {
        cartasLocales[0] = c0;
        cartasLocales[1] = c1;
        cartasLocales[2] = c2;
        cartasLocales[3] = c3;
        mascaraDescarte = 0;
        Debug.Log("[MUS ONLINE] Mano recibida (puesto " + puesto + "): " +
                  string.Join(" ", Array.ConvertAll(cartasLocales, CartasMus.TextoCorto)));
        ManoPrivadaActualizada?.Invoke();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void EventoRpc(EventoRedMus red)
    {
        EventoMus e = red.AEvento();

        switch (e.Tipo)
        {
            case TipoEventoMus.Accion:
                if (e.Accion.Tipo == TipoAccionMus.Envidar || e.Accion.Tipo == TipoAccionMus.Ordago)
                    LimpiarBocadillos();
                bocadillos[e.Jugador] = e.Accion.ToString();
                break;
            case TipoEventoMus.Declaracion:
                bocadillos[e.Jugador] = (e.Fase == FaseMus.Pares ? "PARES " : "JUEGO ") + (e.Dato == 1 ? "SI" : "NO");
                break;
            case TipoEventoMus.LanceCerrado:
                LimpiarBocadillos();
                break;
        }

        if (!HistorialAlRitmoDeLaMesa)
            AnadirAlHistorial(e);


        EventoRecibido?.Invoke(e);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SenalVisibleRpc(int jugador, int tipo)
    {
        if (!senasConMirada && !HistorialAlRitmoDeLaMesa)
            AnadirSenalAlHistorial(jugador, (TipoSenalMus)tipo);

        SenalRecibida?.Invoke(jugador, (TipoSenalMus)tipo);
    }

    public void AnadirSenalAlHistorial(int jugador, TipoSenalMus tipo)
    {
        if (senasConMirada)
            return;

        bool esDeMiPareja = PuestoLocal >= 0 && (jugador == PuestoLocal || jugador == (PuestoLocal + 2) % 4);

        if (esDeMiPareja)
        {
            string texto = jugador == PuestoLocal
                ? "Haces la sena " + (TipoSenalMus)tipo
                : NombresPuesto[jugador] + " (tu companero) te hace la sena " + (TipoSenalMus)tipo;

            AnadirLinea("<color=#C9A0FF>>> " + texto.Replace(NombresPuesto[jugador], ColoresJugadoresMus.Nombre(jugador)) + "</color>");
            Debug.Log("[MUS ONLINE] " + texto);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ManosReveladasRpc(ManosReveladasMus manos)
    {
        manosReveladas = manos.Cartas;
        UltimoDesglose = ManosReveladasMus.Desempaquetar(manos.Desglose);
        ManosReveladas?.Invoke(manos.Cartas);
    }

    public void AnadirAlHistorial(EventoMus e)
    {
        if (reiniciarHistorial && e.Tipo != TipoEventoMus.FinMano)
        {
            ultimosEventos.Clear();
            VersionHistorial++;
            manoHistorial++;
            reiniciarHistorial = false;
            faseHistorial = FaseMus.FinPartida;
            declaracionesJuegoHistorial = 0;
            juegoEnHistorial = false;
            bool corrido = e.Tipo == TipoEventoMus.Accion &&
                           (e.Accion.Tipo == TipoAccionMus.Mus || e.Accion.Tipo == TipoAccionMus.NoHayMus) && e.Dato == 1;
            AnadirLinea("<b>MANO " + manoHistorial + "</b>   " + (corrido
                ? "<color=#FFC94D>(MUS CORRIDO: el primero que corte sera mano)</color>"
                : "(mano: " + ColoresJugadoresMus.Nombre(estado.Mano) + ")"));
        }

        if (e.Tipo != TipoEventoMus.FinMano && e.Fase != faseHistorial)
        {
            faseHistorial = e.Fase;
            AnadirLinea("<b><color=" + ColorFase(e.Fase) + ">== " + NombreFase(e.Fase) + " ==</color></b>");
        }

        string color = ColorFase(e.Fase);

        switch (e.Tipo)
        {
            case TipoEventoMus.Accion:
                AnadirLinea("   " + ColoresJugadoresMus.Nombre(e.Jugador) + "<color=" + color + ">: " + e.Accion + "</color>");

                if (e.Accion.Tipo == TipoAccionMus.NoHayMus && e.Dato == 1)
                    AnadirLinea("<b><color=#FFC94D>   -> " + ColoresJugadoresMus.Nombre(e.Jugador) +
                                "<color=#FFC94D> corta el mus corrido: empieza siendo MANO</color></color></b>");
                break;
            case TipoEventoMus.Declaracion:
                AnadirLinea("   " + ColoresJugadoresMus.Nombre(e.Jugador) + "<color=" + color + ">: " +
                            (e.Fase == FaseMus.Pares ? "pares " : "juego ") + (e.Dato == 1 ? "SI" : "no") + "</color>");

                if (e.Fase == FaseMus.JuegoPunto)
                {
                    declaracionesJuegoHistorial++;
                    juegoEnHistorial |= e.Dato == 1;

                    if (declaracionesJuegoHistorial == 4)
                        AnadirLinea("<b><color=" + color + ">   " + (juegoEnHistorial
                            ? "Hay juego: se juega a JUEGO"
                            : "Nadie tiene juego: se juega a PUNTO") + "</color></b>");
                }
                break;
            case TipoEventoMus.LanceCerrado:
                AnadirLinea("<b><color=" + color + ">   -> " + ResultadoLance(e) + "</color></b>");
                break;
            default:
                AnadirLinea("<b>FIN DE LA MANO</b>");
                reiniciarHistorial = true;
                break;
        }
    }

    private string ResultadoLance(EventoMus e)
    {
        string pareja = PuestoLocal < 0
            ? (e.Jugador == 0 ? "pareja 0-2" : "pareja 1-3")
            : (e.Jugador == PuestoLocal % 2 ? "NOSOTROS" : "ELLOS");

        switch ((MotorEnvitesMus.EstadoEnvite)e.Dato)
        {
            case MotorEnvitesMus.EstadoEnvite.Aceptado: return "Querido a " + e.Accion.Valor + " (se cuenta al final)";
            case MotorEnvitesMus.EstadoEnvite.Rechazado: return "No querido: +" + e.Accion.Valor + " " + pareja;
            case MotorEnvitesMus.EstadoEnvite.OrdagoAceptado: return "ORDAGO QUERIDO";
            default: return "En paso (se cuenta al final)";
        }
    }

    public IReadOnlyList<string> LineasHistorial => ultimosEventos;
    public int VersionHistorial { get; private set; }

    public bool Pausado
    {
        get => pausado;
        set => pausado = value;
    }

    private void AnadirLinea(string linea)
    {
        ultimosEventos.Add(linea);
        VersionHistorial++;

        while (ultimosEventos.Count > 60)
            ultimosEventos.RemoveAt(0);

        scrollHistorial.y = float.MaxValue;
    }

    private static string ColorFase(FaseMus fase)
    {
        switch (fase)
        {
            case FaseMus.Mus: return "#7CD67C";
            case FaseMus.Descarte: return "#BBBBBB";
            case FaseMus.Grande: return "#FFC94D";
            case FaseMus.Chica: return "#6EC6FF";
            case FaseMus.Pares: return "#FF8AD8";
            case FaseMus.JuegoPunto: return "#FF9B5E";
            default: return "#FFFFFF";
        }
    }

    private static string NombreFase(FaseMus fase)
    {
        switch (fase)
        {
            case FaseMus.JuegoPunto: return "JUEGO / PUNTO";
            default: return fase.ToString().ToUpper();
        }
    }

    private static string NombreCorto(int p) => p >= 0 && p < 4 ? NombresPuesto[p] : "-";

    private void LimpiarBocadillos()
    {
        for (int p = 0; p < 4; p++)
            bocadillos[p] = null;
    }

    private int PuestoDeCliente(ulong id)
    {
        for (int p = 0; p < 4; p++)
            if (clientes[p] == id)
                return p;

        return -1;
    }

    public void SolicitarAccion(AccionMus accion)
    {
        AccionRpc((int)accion.Tipo, accion.Valor);
    }

    public void SolicitarSenal(TipoSenalMus tipo)
    {
        SenalRpc((int)tipo);
    }

    public bool PuedePasarDeMano => IsServer && estado.EnCurso && estado.FaseActual == FaseMus.FinMano;

    public void PedirSiguienteMano()
    {
        if (PuedePasarDeMano)
            SiguienteManoServidor();
    }

    private void AbrirRegistro()
    {
        if (!registrarDecisiones)
            return;

        try
        {
            string ruta = Path.Combine(Application.persistentDataPath, "decisiones_online.jsonl");
            registro = new RegistroExperienciasMus(new StreamWriter(ruta, true));
            Debug.Log("[MUS ONLINE] Registrando decisiones en " + ruta);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MUS ONLINE] No se pudo abrir el registro: " + e.Message);
            registro = null;
        }
    }

    private void CerrarRegistro()
    {
        registro?.Dispose();
        registro = null;
    }

    private static string Nombre(int p) => p >= 0 && p < 4 ? p + " " + NombresPuesto[p] : "-";

    public bool PartidaEmpezada => estado.EnCurso || estado.ParejaGanadora >= 0;

    private void OnGUI()
    {
        if (VistaMesaOnlineMus.InterfazNuevaActiva)
            return;

        EscalaGUIMus.Aplicar();

        if (!IsSpawned)
            return;

        if (!mostrarPanelPrueba)
        {
            if (ModoRevision)
                return;

            DibujarControlesCompactos();

            if (mostrarHistorial && PartidaEmpezada)
                DibujarHistorial();

            return;
        }

        float ancho = Mathf.Min(560f, EscalaGUIMus.Ancho - 40f);
        GUILayout.BeginArea(new Rect(20, 240, ancho, EscalaGUIMus.Alto - 260), "MUS TINKO - PARTIDA ONLINE", GUI.skin.window);
        scroll = GUILayout.BeginScrollView(scroll);

        if (!estado.EnCurso && estado.ParejaGanadora < 0)
        {
            GUILayout.Label("Conectados: " + NetworkManager.ConnectedClientsIds.Count + ". Los puestos vacios los ocupan bots.");

            if (IsServer && GUILayout.Button("EMPEZAR PARTIDA", GUILayout.Height(34)))
                EmpezarPartidaServidor();
            else if (!IsServer)
                GUILayout.Label("Esperando a que el host empiece la partida...");

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }

        GUILayout.Label("Tu puesto: " + (PuestoLocal >= 0 ? Nombre(PuestoLocal) + " | Pareja " + (PuestoLocal % 2 == 0 ? "0-2" : "1-3") : "espectador"));
        GUILayout.Label("Tantos  0-2: " + estado.Puntos0 + "   1-3: " + estado.Puntos1 + "   (a " + estado.Limite + ")");
        GUILayout.Label("Fase: " + estado.FaseActual + (estado.FaseActual == FaseMus.Mus ? " (ronda " + estado.RondaMus + ")" : "") +
                        " | Mano: " + (estado.MusCorrido ? "mus corrido" : Nombre(estado.Mano)) + " | Turno: " + Nombre(estado.JugadorActual));

        if (estado.TantosApostados > 0 || estado.EstadoEnvite == (int)MotorEnvitesMus.EstadoEnvite.OrdagoPendiente)
            GUILayout.Label("Envite: " + (MotorEnvitesMus.EstadoEnvite)estado.EstadoEnvite + " | " + estado.TantosApostados + " tantos");

        GUILayout.Space(4);

        for (int p = 0; p < 4; p++)
        {
            string quien = estado.EsBot(p) ? "BOT" : "HUMANO";
            string texto = (p == estado.JugadorActual ? "> " : "   ") + Nombre(p) + " [" + quien + "]  " + (bocadillos[p] ?? "");
            GUILayout.Label(texto);
        }

        GUILayout.Space(4);
        DibujarManoLocal();

        if (manosReveladas != null)
        {
            for (int p = 0; p < 4; p++)
            {
                string[] cartas = new string[4];
                for (int c = 0; c < 4; c++) cartas[c] = CartasMus.TextoCorto(manosReveladas[p * 4 + c]);
                GUILayout.Label("Mano de " + Nombre(p) + ": " + string.Join(" ", cartas));
            }

            foreach (ConceptoTanteoMus c in UltimoDesglose)
                GUILayout.Label("  " + TablaTanteoMus.NombreLance(c.Lance, true) + ": " + TablaTanteoMus.TextoConcepto(c, manosReveladas, NombresPuesto) +
                                " -> pareja " + (c.Pareja == 0 ? "0-2" : "1-3"));
        }

        if (!estado.EnCurso)
        {
            GUILayout.Label("FIN DE PARTIDA. Gana la pareja " + (estado.ParejaGanadora == 0 ? "0-2" : "1-3") +
                            (estado.PorOrdago ? " por ORDAGO." : " por tantos."));

            if (IsServer && GUILayout.Button("NUEVA PARTIDA", GUILayout.Height(30)))
                EmpezarPartidaServidor();
        }
        else if (estado.FaseActual == FaseMus.FinMano)
        {
            if (IsServer && siguienteManoManual)
            {
                if (GUILayout.Button("SIGUIENTE MANO", GUILayout.Height(30)))
                    SiguienteManoServidor();
            }
            else
            {
                GUILayout.Label(IsServer ? "Fin de la mano. La siguiente empieza en unos segundos..." : "Fin de la mano. Esperando al host...");
            }
        }
        else
        {
            DibujarAcciones();
        }

        if (IsServer)
            DibujarControlesHost();

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        DibujarHistorial();
    }

    private void DibujarHistorial()
    {
        if (estiloHistorial == null)
            estiloHistorial = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 };

        float ancho = Mathf.Min(380f, EscalaGUIMus.Ancho * 0.3f);
        float x = EscalaGUIMus.Ancho - ancho - 20;

        if (!historialAbierto)
        {
            if (GUI.Button(new Rect(EscalaGUIMus.Ancho - 150, 20, 130, 40), "HISTORIAL"))
                historialAbierto = true;
            return;
        }

        if (GUI.Button(new Rect(x, 20, ancho, 30), "OCULTAR HISTORIAL"))
        {
            historialAbierto = false;
            return;
        }

        GUILayout.BeginArea(new Rect(x, 52, ancho, EscalaGUIMus.Alto * 0.58f), "HISTORIAL DE LA MANO", GUI.skin.window);
        scrollHistorial = GUILayout.BeginScrollView(scrollHistorial);

        foreach (string e in ultimosEventos)
            GUILayout.Label(e, estiloHistorial);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DibujarControlesCompactos()
    {
        if (!IsServer)
        {
            if (!PartidaEmpezada)
            {
                GUILayout.BeginArea(new Rect(20, 20, 360, 60), GUI.skin.window);
                GUILayout.Label("Esperando a que el host empiece la partida...");
                GUILayout.EndArea();
            }

            return;
        }

        GUILayout.BeginArea(new Rect(20, 20, 360, 150), "HOST", GUI.skin.window);

        if (!PartidaEmpezada)
        {
            GUILayout.Label("Conectados: " + NetworkManager.ConnectedClientsIds.Count + " (el resto, bots)");

            if (GUILayout.Button("EMPEZAR PARTIDA", GUILayout.Height(32)))
                EmpezarPartidaServidor();
        }
        else if (!estado.EnCurso)
        {
            if (GUILayout.Button("NUEVA PARTIDA", GUILayout.Height(32)))
                EmpezarPartidaServidor();
        }
        else
        {
            if (estado.FaseActual == FaseMus.FinMano && siguienteManoManual && !HistorialAlRitmoDeLaMesa &&
                GUILayout.Button("SIGUIENTE MANO", GUILayout.Height(32)))
                SiguienteManoServidor();

            DibujarControlesHost();
        }

        GUILayout.EndArea();
    }

    private void DibujarControlesHost()
    {
        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Velocidad bots: " + esperaBot.ToString("0.0") + " s", GUILayout.Width(170));
        esperaBot = GUILayout.HorizontalSlider(esperaBot, 0.2f, 5f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        siguienteManoManual = GUILayout.Toggle(siguienteManoManual, " Pasar de mano con boton");

        if (GUILayout.Button(pausado ? "REANUDAR" : "PAUSA", GUILayout.Height(26)))
            pausado = !pausado;

        GUILayout.EndHorizontal();

        if (pausado)
            GUILayout.Label("PARTIDA EN PAUSA (los bots esperan)");
    }

    private void DibujarManoLocal()
    {
        if (PuestoLocal < 0 || cartasLocales[0] < 0)
            return;

        bool descartando = estado.FaseActual == FaseMus.Descarte && estado.JugadorActual == PuestoLocal;

        GUILayout.BeginHorizontal();

        for (int c = 0; c < 4; c++)
        {
            bool marcada = (mascaraDescarte & (1 << c)) != 0;
            string texto = CartasMus.TextoCorto(cartasLocales[c]) + (marcada ? " [X]" : "");

            if (GUILayout.Button(texto, GUILayout.Height(32)) && descartando)
                mascaraDescarte ^= 1 << c;
        }

        GUILayout.EndHorizontal();

        EvaluadorManoMus.Resultado r = CartasMus.Evaluar(cartasLocales);
        GUILayout.Label(r.DetallePares + " | " + (r.TieneJuego ? "Juego " : "Punto ") + r.PuntosJuego);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Senas:", GUILayout.Width(50));

        foreach (TipoSenalMus s in (TipoSenalMus[])Enum.GetValues(typeof(TipoSenalMus)))
            if (!estado.MusCorrido && PartidaMus.CumpleSenal(cartasLocales, r, s) && GUILayout.Button(s.ToString()))
                SolicitarSenal(s);

        GUILayout.EndHorizontal();
    }

    private void DibujarAcciones()
    {
        if (PuestoLocal < 0 || estado.JugadorActual != PuestoLocal)
        {
            GUILayout.Label("Esperando a " + Nombre(estado.JugadorActual) + "...");
            return;
        }

        GUILayout.Label("TE TOCA");
        GUILayout.BeginHorizontal();

        if (estado.Legal(TipoAccionMus.Mus) && GUILayout.Button("MUS", GUILayout.Height(32)))
            SolicitarAccion(AccionMus.Mus);

        if (estado.Legal(TipoAccionMus.NoHayMus) && GUILayout.Button("NO HAY MUS", GUILayout.Height(32)))
            SolicitarAccion(AccionMus.NoHayMus);

        if (estado.Legal(TipoAccionMus.Descartar))
        {
            GUI.enabled = mascaraDescarte != 0;
            if (GUILayout.Button("DESCARTAR " + AccionMus.ContarBits(mascaraDescarte), GUILayout.Height(32)))
                SolicitarAccion(AccionMus.Descartar(mascaraDescarte));
            GUI.enabled = true;
        }

        if (estado.Legal(TipoAccionMus.Pasar) && GUILayout.Button("PASO", GUILayout.Height(32)))
            SolicitarAccion(AccionMus.Pasar);

        if (estado.Legal(TipoAccionMus.Envidar) && GUILayout.Button("ENVIDO", GUILayout.Height(32)))
            SolicitarAccion(AccionMus.Envidar(estado.TantosEnvite));

        if (estado.Legal(TipoAccionMus.Reenvidar) && GUILayout.Button(estado.TantosEnvite + " MAS", GUILayout.Height(32)))
            SolicitarAccion(AccionMus.Reenvidar(estado.TantosEnvite));

        if (estado.Legal(TipoAccionMus.Quiero) && GUILayout.Button("QUIERO", GUILayout.Height(32)))
            SolicitarAccion(AccionMus.Quiero);

        if (estado.Legal(TipoAccionMus.NoQuiero) && GUILayout.Button("NO QUIERO", GUILayout.Height(32)))
            SolicitarAccion(AccionMus.NoQuiero);

        if (estado.Legal(TipoAccionMus.Ordago) && GUILayout.Button("ORDAGO", GUILayout.Height(32)))
            SolicitarAccion(AccionMus.Ordago);

        GUILayout.EndHorizontal();

        if (estado.FaseActual == FaseMus.Descarte)
            GUILayout.Label("Pulsa las cartas que quieres cambiar y luego DESCARTAR.");
    }
}
