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
    public int TantosEnvite;
    public int MascaraBots;
    public int MascaraLegales;
    public int MascaraHablan;
    public ulong Cliente0;
    public ulong Cliente1;
    public ulong Cliente2;
    public ulong Cliente3;

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
        s.SerializeValue(ref TantosEnvite);
        s.SerializeValue(ref MascaraBots);
        s.SerializeValue(ref MascaraLegales);
        s.SerializeValue(ref MascaraHablan);
        s.SerializeValue(ref Cliente0);
        s.SerializeValue(ref Cliente1);
        s.SerializeValue(ref Cliente2);
        s.SerializeValue(ref Cliente3);
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
    private static readonly string[] NombresPuesto = { "LILA", "MOSTAZA", "MENTA", "CORAL" };

    [Header("Partida")]
    [SerializeField] private int limiteTantos = 40;
    [SerializeField] private int maxRondasMus = 0;

    [Header("Bots")]
    [SerializeField] private float esperaBot = 1.5f;
    [SerializeField] private float esperaFinMano = 8f;
    [SerializeField] private bool siguienteManoManual = true;
    [SerializeField] private TextAsset parametrosBotEntrenado;

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

    private EstadoPublicoMus estado;
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

    public bool ModoRevision { get; set; }
    private Vector2 scroll;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        NetworkManager.OnClientConnectedCallback += ClienteConectado;
        NetworkManager.OnClientDisconnectCallback += ClienteDesconectado;
    }

    public override void OnNetworkDespawn()
    {
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

    private void ClienteConectado(ulong id)
    {
        if (partida != null && !partida.PartidaTerminada)
            EstadoParaClienteRpc(estado, RpcTarget.Single(id, RpcTargetUse.Temp));
    }

    private void ClienteDesconectado(ulong id)
    {
        if (partida == null)
            return;

        for (int p = 0; p < 4; p++)
        {
            if (clientes[p] != id)
                continue;

            clientes[p] = SinCliente;
            bots[p] = CrearBot(p);
            Debug.LogWarning("[MUS ONLINE] Jugador " + p + " desconectado. Lo sustituye un bot.");
            momentoAccionBot = Time.time + esperaBot;
        }

        Difundir();
    }

    public void EmpezarPartidaServidor()
    {
        if (!IsServer)
            return;

        List<ulong> conectados = new();

        foreach (ulong id in NetworkManager.ConnectedClientsIds)
            if (id != Unity.Netcode.NetworkManager.ServerClientId)
                conectados.Add(id);

        conectados.Sort();
        clientes[0] = Unity.Netcode.NetworkManager.ServerClientId;

        for (int p = 1; p < 4; p++)
            clientes[p] = p - 1 < conectados.Count ? conectados[p - 1] : SinCliente;

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
        manoConSenalesBots = -1;
        idPartida++;
        registro?.EmpezarPartida(idPartida);
        partida.IniciarPartida();
        numeroMano++;
        momentoAccionBot = Time.time + esperaBot;

        Debug.Log("[MUS ONLINE] Partida iniciada. Humanos: " + (4 - ContarBots()) + ", bots: " + ContarBots());
        Difundir();
    }

    private IBotMus CrearBot(int puesto)
    {
        ParametrosBotMus parametros = ParametrosBotMus.Equilibrado();

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

        return new BotReglasMus("Bot " + NombresPuesto[puesto], parametros);
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
            momentoAccionBot = Time.time + esperaBot;
            momentoSiguienteMano = Time.time + esperaFinMano;
            return;
        }

        if (partida.Fase == FaseMus.FinMano)
        {
            if (!siguienteManoManual && Time.time >= momentoSiguienteMano)
                SiguienteManoServidor();
            return;
        }

        if (PartidaMus.EsLance(partida.Fase) && manoConSenalesBots != partida.ManosJugadas)
        {
            manoConSenalesBots = partida.ManosJugadas;

            for (int p = 0; p < 4; p++)
                if (bots[p] != null)
                    foreach (TipoSenalMus senal in bots[p].ElegirSenales(partida.Observar(p), azarBots))
                        partida.HacerSenal(p, senal);
        }

        int actual = partida.JugadorActual;

        if (actual < 0 || bots[actual] == null || Time.time < momentoAccionBot)
            return;

        List<AccionMus> legales = partida.AccionesLegales(actual);

        if (legales.Count == 0)
            return;

        ObservacionMus obs = partida.Observar(actual);
        AccionMus accion = bots[actual].Decidir(obs, legales, azarBots);

        if (!legales.Contains(accion))
            accion = SimuladorMus.AccionSegura(legales);

        EjecutarAccion(actual, accion, obs, legales, bots[actual].Nombre);
    }

    private void SiguienteManoServidor()
    {
        if (partida == null || partida.Fase != FaseMus.FinMano)
            return;

        partida.SiguienteMano();
        numeroMano++;
        manosReveladas = null;
        momentoAccionBot = Time.time + esperaBot;
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

        if (!legales.Contains(accion))
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
        momentoAccionBot = Time.time + esperaBot;

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

        AnadirAlHistorial(e);


        EventoRecibido?.Invoke(e);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SenalVisibleRpc(int jugador, int tipo)
    {
        bool esDeMiPareja = PuestoLocal >= 0 && (jugador == PuestoLocal || jugador == (PuestoLocal + 2) % 4);

        if (esDeMiPareja)
        {
            string texto = jugador == PuestoLocal
                ? "Haces la sena " + (TipoSenalMus)tipo
                : NombresPuesto[jugador] + " (tu companero) te hace la sena " + (TipoSenalMus)tipo;

            AnadirLinea("<color=#C9A0FF>>> " + texto.Replace(NombresPuesto[jugador], ColoresJugadoresMus.Nombre(jugador)) + "</color>");
            Debug.Log("[MUS ONLINE] " + texto);
        }

        SenalRecibida?.Invoke(jugador, (TipoSenalMus)tipo);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ManosReveladasRpc(ManosReveladasMus manos)
    {
        manosReveladas = manos.Cartas;
        UltimoDesglose = ManosReveladasMus.Desempaquetar(manos.Desglose);
        ManosReveladas?.Invoke(manos.Cartas);
    }

    private void AnadirAlHistorial(EventoMus e)
    {
        if (reiniciarHistorial && e.Tipo != TipoEventoMus.FinMano)
        {
            ultimosEventos.Clear();
            manoHistorial++;
            reiniciarHistorial = false;
            faseHistorial = FaseMus.FinPartida;
            declaracionesJuegoHistorial = 0;
            juegoEnHistorial = false;
            AnadirLinea("<b>MANO " + manoHistorial + "</b>   (mano: " + ColoresJugadoresMus.Nombre(estado.Mano) + ")");
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

    private void AnadirLinea(string linea)
    {
        ultimosEventos.Add(linea);

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

        float ancho = Mathf.Min(560f, Screen.width - 40f);
        GUILayout.BeginArea(new Rect(20, 240, ancho, Screen.height - 260), "MUS TINKO - PARTIDA ONLINE", GUI.skin.window);
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
                        " | Mano: " + Nombre(estado.Mano) + " | Turno: " + Nombre(estado.JugadorActual));

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

        float ancho = Mathf.Min(380f, Screen.width * 0.3f);
        GUILayout.BeginArea(new Rect(Screen.width - ancho - 20, 20, ancho, Screen.height - 40), "HISTORIAL DE LA MANO", GUI.skin.window);
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
            if (estado.FaseActual == FaseMus.FinMano && siguienteManoManual &&
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
            if (PartidaMus.CumpleSenal(cartasLocales, r, s) && GUILayout.Button(s.ToString()))
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
