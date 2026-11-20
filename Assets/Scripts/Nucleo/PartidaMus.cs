using System;
using System.Collections.Generic;

public class PartidaMus
{
    public const int Jugadores = 4;
    public const int CartasPorJugador = 4;

    private enum TipoRegistro
    {
        Paso,
        Aceptado,
        Rechazado
    }

    private struct RegistroLance
    {
        public bool Registrado;
        public TipoRegistro Tipo;
        public int Tantos;
    }

    private readonly Random azar;
    private readonly BarajaMus baraja;
    private readonly int[,] manos = new int[Jugadores, CartasPorJugador];
    private readonly EvaluadorManoMus.Resultado[] evaluaciones = new EvaluadorManoMus.Resultado[Jugadores];
    private readonly MotorTurnosIndividualesMus turnos = new();
    private readonly bool[] puedeHablar = new bool[Jugadores];
    private readonly bool[] yaDescarto = new bool[Jugadores];
    private readonly int[] cartasDescartadas = new int[Jugadores];
    private readonly RegistroLance[] registros = new RegistroLance[4];
    private readonly int[] puntos = new int[2];
    private readonly int[] puntosInicioMano = new int[2];
    private readonly List<EventoMus> historial = new();
    private readonly List<ConceptoTanteoMus> desglose = new();
    private readonly List<TipoSenalMus>[] senalesRecibidas =
    {
        new List<TipoSenalMus>(), new List<TipoSenalMus>(),
        new List<TipoSenalMus>(), new List<TipoSenalMus>()
    };

    private int turnoMus;
    private int musSeguidos;
    private int turnoDescarte;
    private int descartesHechos;

    public int Limite { get; }
    public int MaxRondasMus { get; }
    public int TantosEnvite { get; set; } = 2;

    public FaseMus Fase { get; private set; } = FaseMus.FinPartida;
    public int Mano { get; private set; }
    public bool MusCorridoPendiente { get; private set; }
    public int RondaMus { get; private set; }
    public int ManosJugadas { get; private set; }
    public int ParejaGanadora { get; private set; } = -1;
    public bool VictoriaPorOrdago { get; private set; }
    public string MotivoVictoria { get; private set; } = "";
    public bool PartidaTerminada => Fase == FaseMus.FinPartida;
    public IReadOnlyList<EventoMus> Historial => historial;
    public IReadOnlyList<ConceptoTanteoMus> Desglose => desglose;
    public MotorEnvitesMus Envite => turnos.Motor;

    public event Action<EventoMus> EventoRegistrado;
    public event Action<int, TipoSenalMus> SenalRealizada;

    public PartidaMus(int semilla, int limite = 40, int maxRondasMus = 0)
    {
        azar = new Random(semilla);
        baraja = new BarajaMus(azar);
        Limite = Math.Max(1, limite);
        MaxRondasMus = Math.Max(0, maxRondasMus);
    }

    public int Puntos(int pareja) => puntos[pareja];
    public int PuntosGanadosEnMano(int pareja) => puntos[pareja] - puntosInicioMano[pareja];
    public int Carta(int jugador, int posicion) => manos[jugador, posicion];
    public EvaluadorManoMus.Resultado Evaluacion(int jugador) => evaluaciones[jugador];
    public bool PuedeHablarEnLance(int jugador) => EsLance(Fase) && puedeHablar[jugador];

    public int[] CartasDe(int jugador)
    {
        int[] cartas = new int[CartasPorJugador];

        for (int i = 0; i < CartasPorJugador; i++)
            cartas[i] = manos[jugador, i];

        return cartas;
    }

    public int JugadorActual
    {
        get
        {
            switch (Fase)
            {
                case FaseMus.Mus: return turnoMus;
                case FaseMus.Descarte: return turnoDescarte;
                case FaseMus.Grande:
                case FaseMus.Chica:
                case FaseMus.Pares:
                case FaseMus.JuegoPunto:
                    return turnos.JugadorActual;
                default:
                    return -1;
            }
        }
    }

    public void IniciarPartida()
    {
        puntos[0] = 0;
        puntos[1] = 0;
        Mano = 0;
        MusCorridoPendiente = true;
        ManosJugadas = 0;
        ParejaGanadora = -1;
        VictoriaPorOrdago = false;
        MotivoVictoria = "";
        EmpezarMano();
    }

    public bool SiguienteMano()
    {
        if (Fase != FaseMus.FinMano)
            return false;

        Mano = (Mano + 1) % Jugadores;
        EmpezarMano();
        return true;
    }

    private void EmpezarMano()
    {
        baraja.Reiniciar();

        for (int j = 0; j < Jugadores; j++)
            for (int c = 0; c < CartasPorJugador; c++)
                manos[j, c] = baraja.Sacar();

        for (int i = 0; i < registros.Length; i++)
            registros[i] = default;

        for (int j = 0; j < Jugadores; j++)
        {
            senalesRecibidas[j].Clear();
            cartasDescartadas[j] = 0;
        }

        historial.Clear();
        desglose.Clear();
        puntosInicioMano[0] = puntos[0];
        puntosInicioMano[1] = puntos[1];
        turnos.Reiniciar();
        Array.Clear(puedeHablar, 0, puedeHablar.Length);

        RondaMus = 1;
        EvaluarManos();
        EmpezarRondaMus();
    }

    private int PrimeroEnHablarMus => MusCorridoPendiente ? 0 : Mano;

    private void EmpezarRondaMus()
    {
        musSeguidos = 0;
        turnoMus = PrimeroEnHablarMus;
        Fase = FaseMus.Mus;
    }

    private void EvaluarManos()
    {
        for (int j = 0; j < Jugadores; j++)
            evaluaciones[j] = CartasMus.Evaluar(CartasDe(j));
    }

    public bool MusPermitido => MaxRondasMus == 0 || RondaMus <= MaxRondasMus;

    public List<AccionMus> AccionesLegales(int jugador)
    {
        List<AccionMus> legales = new();

        if (jugador < 0 || jugador >= Jugadores || jugador != JugadorActual)
            return legales;

        switch (Fase)
        {
            case FaseMus.Mus:
                if (MusPermitido)
                    legales.Add(AccionMus.Mus);
                legales.Add(AccionMus.NoHayMus);
                break;

            case FaseMus.Descarte:
                for (int mascara = 1; mascara < 16; mascara++)
                    legales.Add(AccionMus.Descartar(mascara));
                break;

            case FaseMus.Grande:
            case FaseMus.Chica:
            case FaseMus.Pares:
            case FaseMus.JuegoPunto:
                AnadirAccionesLance(legales);
                break;
        }

        return legales;
    }

    private void AnadirAccionesLance(List<AccionMus> legales)
    {
        MotorEnvitesMus motor = turnos.Motor;

        if (!turnos.HayEnvitePendiente)
        {
            legales.Add(AccionMus.Pasar);
            legales.Add(AccionMus.Envidar(TantosEnvite));
            legales.Add(AccionMus.Ordago);
            return;
        }

        legales.Add(AccionMus.Quiero);
        legales.Add(AccionMus.NoQuiero);

        if (motor.Estado == MotorEnvitesMus.EstadoEnvite.PendienteRespuesta)
        {
            if (PuedeReenvidar(TantosEnvite))
                legales.Add(AccionMus.Reenvidar(TantosEnvite));

            legales.Add(AccionMus.Ordago);
        }
    }

    private bool PuedeReenvidar(int tantos)
    {
        return turnos.Motor.TantosApostados + tantos < Limite;
    }

    public bool Aplicar(int jugador, AccionMus accion)
    {
        if (jugador < 0 || jugador >= Jugadores || jugador != JugadorActual)
            return false;

        switch (Fase)
        {
            case FaseMus.Mus: return AplicarMus(jugador, accion);
            case FaseMus.Descarte: return AplicarDescarte(jugador, accion);
            case FaseMus.Grande:
            case FaseMus.Chica:
            case FaseMus.Pares:
            case FaseMus.JuegoPunto:
                return AplicarLance(jugador, accion);
            default:
                return false;
        }
    }

    private bool AplicarMus(int jugador, AccionMus accion)
    {
        if (accion.Tipo == TipoAccionMus.Mus)
        {
            if (!MusPermitido)
                return false;

            Registrar(TipoEventoMus.Accion, jugador, accion, MusCorridoPendiente ? 1 : 0);
            musSeguidos++;

            if (musSeguidos >= Jugadores)
            {
                Fase = FaseMus.Descarte;
                Array.Clear(yaDescarto, 0, yaDescarto.Length);
                descartesHechos = 0;
                turnoDescarte = PrimeroEnHablarMus;
            }
            else
            {
                turnoMus = (turnoMus + 1) % Jugadores;
            }

            return true;
        }

        if (accion.Tipo != TipoAccionMus.NoHayMus)
            return false;

        Registrar(TipoEventoMus.Accion, jugador, accion, MusCorridoPendiente ? 1 : 0);

        if (MusCorridoPendiente)
        {
            Mano = jugador;
            MusCorridoPendiente = false;
        }

        IniciarLance(FaseMus.Grande);
        return true;
    }

    private bool AplicarDescarte(int jugador, AccionMus accion)
    {
        if (accion.Tipo != TipoAccionMus.Descartar || yaDescarto[jugador])
            return false;

        int mascara = accion.Valor;

        if (mascara < 1 || mascara > 15)
            return false;

        for (int c = 0; c < CartasPorJugador; c++)
        {
            if ((mascara & (1 << c)) == 0)
                continue;

            baraja.Descartar(manos[jugador, c]);
            manos[jugador, c] = baraja.Sacar();
        }

        yaDescarto[jugador] = true;
        cartasDescartadas[jugador] = AccionMus.ContarBits(mascara);
        evaluaciones[jugador] = CartasMus.Evaluar(CartasDe(jugador));
        Registrar(TipoEventoMus.Accion, jugador, accion);
        descartesHechos++;

        if (descartesHechos >= Jugadores)
        {
            RondaMus++;
            EmpezarRondaMus();
        }
        else
        {
            turnoDescarte = (turnoDescarte + 1) % Jugadores;
        }

        return true;
    }

    private bool AplicarLance(int jugador, AccionMus accion)
    {
        bool valido;

        switch (accion.Tipo)
        {
            case TipoAccionMus.Pasar: valido = turnos.Pasar(jugador); break;
            case TipoAccionMus.Envidar: valido = accion.Valor >= 2 && turnos.Envidar(jugador, accion.Valor); break;
            case TipoAccionMus.Reenvidar: valido = accion.Valor >= 1 && PuedeReenvidar(accion.Valor) && turnos.Reenvidar(jugador, accion.Valor); break;
            case TipoAccionMus.Quiero: valido = turnos.Quiero(jugador); break;
            case TipoAccionMus.NoQuiero: valido = turnos.NoQuiero(jugador); break;
            case TipoAccionMus.Ordago: valido = turnos.LanzarOrdago(jugador); break;
            default: valido = false; break;
        }

        if (!valido)
            return false;

        Registrar(TipoEventoMus.Accion, jugador, accion);

        if (turnos.Cerrado)
            CerrarLance();

        return true;
    }

    private void IniciarLance(FaseMus fase)
    {
        Fase = fase;
        turnos.Reiniciar();

        bool permite = true;

        for (int j = 0; j < Jugadores; j++)
            puedeHablar[j] = true;

        if (fase == FaseMus.Pares)
        {
            bool pareja0 = false;
            bool pareja1 = false;

            for (int paso = 0; paso < Jugadores; paso++)
            {
                int j = (Mano + paso) % Jugadores;
                bool tiene = evaluaciones[j].Pares != EvaluadorManoMus.TipoPares.SinPares;
                puedeHablar[j] = tiene;
                Registrar(TipoEventoMus.Declaracion, j, default, tiene ? 1 : 0);

                if (tiene)
                {
                    if (j % 2 == 0) pareja0 = true;
                    else pareja1 = true;
                }
            }

            permite = pareja0 && pareja1;
        }
        else if (fase == FaseMus.JuegoPunto)
        {
            bool hayJuego = false;
            bool pareja0 = false;
            bool pareja1 = false;

            for (int paso = 0; paso < Jugadores; paso++)
            {
                int j = (Mano + paso) % Jugadores;
                bool tiene = evaluaciones[j].TieneJuego;
                Registrar(TipoEventoMus.Declaracion, j, default, tiene ? 1 : 0);

                if (tiene)
                {
                    hayJuego = true;
                    if (j % 2 == 0) pareja0 = true;
                    else pareja1 = true;
                }
            }

            if (hayJuego)
            {
                for (int j = 0; j < Jugadores; j++)
                    puedeHablar[j] = evaluaciones[j].TieneJuego;

                permite = pareja0 && pareja1;
            }
        }

        if (!permite)
        {
            Array.Clear(puedeHablar, 0, puedeHablar.Length);
            CerrarLance();
            return;
        }

        turnos.Iniciar(Mano, puedeHablar);

        if (turnos.Cerrado)
            CerrarLance();
    }

    private void CerrarLance()
    {
        int indice = IndiceLance(Fase);
        MotorEnvitesMus motor = turnos.Motor;
        RegistroLance registro = new() { Registrado = true };

        switch (motor.Estado)
        {
            case MotorEnvitesMus.EstadoEnvite.Aceptado:
                registro.Tipo = TipoRegistro.Aceptado;
                registro.Tantos = motor.TantosApostados;
                break;

            case MotorEnvitesMus.EstadoEnvite.Rechazado:
                registro.Tipo = TipoRegistro.Rechazado;
                registro.Tantos = motor.TantosPorNegada;
                puntos[motor.ParejaGanadoraNegada] += motor.TantosPorNegada;
                desglose.Add(new ConceptoTanteoMus(indice, TipoConceptoMus.NoQuerido,
                    motor.ParejaGanadoraNegada, motor.TantosPorNegada));
                break;

            case MotorEnvitesMus.EstadoEnvite.OrdagoAceptado:
                ResolverOrdago(indice);
                return;

            default:
                registro.Tipo = TipoRegistro.Paso;
                break;
        }

        registros[indice] = registro;
        Registrar(TipoEventoMus.LanceCerrado,
            registro.Tipo == TipoRegistro.Rechazado ? motor.ParejaGanadoraNegada : -1,
            new AccionMus(TipoAccionMus.Envidar, registro.Tantos), (int)motor.Estado);

        if (ComprobarVictoria(0, 0, "Negada en " + Fase))
            return;

        switch (Fase)
        {
            case FaseMus.Grande: IniciarLance(FaseMus.Chica); break;
            case FaseMus.Chica: IniciarLance(FaseMus.Pares); break;
            case FaseMus.Pares: IniciarLance(FaseMus.JuegoPunto); break;
            default: Tantear(); break;
        }
    }

    private void ResolverOrdago(int indice)
    {
        int ganador = GanadorLance(indice, out _);

        if (ganador < 0)
            ganador = Mano;

        desglose.Add(new ConceptoTanteoMus(indice, TipoConceptoMus.Ordago, ganador % 2, 0, ganador));

        Registrar(TipoEventoMus.LanceCerrado, ganador % 2, default, (int)MotorEnvitesMus.EstadoEnvite.OrdagoAceptado);
        TerminarPartida(ganador % 2, true, "Ordago aceptado en " + Fase);
    }

    private void Tantear()
    {
        int ganancia0 = 0;
        int ganancia1 = 0;

        for (int lance = 0; lance < 4; lance++)
        {
            if (PartidaTerminada)
                return;

            int ganador = GanadorLance(lance, out bool hayJuego);
            int pareja = ganador >= 0 ? ganador % 2 : -1;
            RegistroLance registro = registros[lance];

            if (registro.Tipo == TipoRegistro.Aceptado)
            {
                if (pareja >= 0)
                    Sumar(new ConceptoTanteoMus(lance, TipoConceptoMus.Querido, pareja, registro.Tantos), ref ganancia0, ref ganancia1);
            }
            else if (registro.Tipo == TipoRegistro.Paso && lance <= 1 && pareja >= 0)
            {
                Sumar(new ConceptoTanteoMus(lance, TipoConceptoMus.EnPaso, pareja, 1), ref ganancia0, ref ganancia1);
            }

            if (lance == 2 && pareja >= 0)
            {
                for (int j = 0; j < Jugadores; j++)
                {
                    if (j % 2 != pareja)
                        continue;

                    int valor = ValorPares(evaluaciones[j].Pares);

                    if (valor > 0)
                        Sumar(new ConceptoTanteoMus(lance, TipoConceptoMus.Pares, pareja, valor, j), ref ganancia0, ref ganancia1);
                }
            }

            if (lance == 3 && pareja >= 0)
            {
                if (hayJuego)
                {
                    for (int j = 0; j < Jugadores; j++)
                    {
                        if (j % 2 != pareja || !evaluaciones[j].TieneJuego)
                            continue;

                        Sumar(new ConceptoTanteoMus(lance, TipoConceptoMus.Juego, pareja,
                            evaluaciones[j].PuntosJuego == 31 ? 3 : 2, j), ref ganancia0, ref ganancia1);
                    }
                }
                else
                {
                    Sumar(new ConceptoTanteoMus(lance, TipoConceptoMus.Punto, pareja, 1), ref ganancia0, ref ganancia1);
                }
            }
        }

        if (PartidaTerminada)
            return;

        puntos[0] += ganancia0;
        puntos[1] += ganancia1;
        ManosJugadas++;
        Fase = FaseMus.FinMano;
        Registrar(TipoEventoMus.FinMano, -1, default);
    }

    private void Sumar(ConceptoTanteoMus concepto, ref int ganancia0, ref int ganancia1)
    {
        if (PartidaTerminada || concepto.Tantos <= 0)
            return;

        if (concepto.Pareja == 0) ganancia0 += concepto.Tantos;
        else ganancia1 += concepto.Tantos;

        desglose.Add(concepto);
        ComprobarVictoria(ganancia0, ganancia1, concepto.Tipo + " en " + NombreLance(concepto.Lance, true));
    }

    private bool ComprobarVictoria(int pendientes0, int pendientes1, string motivo)
    {
        if (PartidaTerminada)
            return true;

        int total0 = puntos[0] + pendientes0;
        int total1 = puntos[1] + pendientes1;

        if (total0 < Limite && total1 < Limite)
            return false;

        puntos[0] = total0;
        puntos[1] = total1;
        TerminarPartida(total0 >= Limite ? 0 : 1, false, "Victoria por tantos: " + motivo);
        return true;
    }

    private void TerminarPartida(int pareja, bool porOrdago, string motivo)
    {
        ParejaGanadora = pareja;
        VictoriaPorOrdago = porOrdago;
        MotivoVictoria = motivo;
        ManosJugadas++;
        Fase = FaseMus.FinPartida;
        Registrar(TipoEventoMus.FinMano, -1, default);
    }

    private int GanadorLance(int lance, out bool hayJuego)
    {
        hayJuego = false;

        switch (lance)
        {
            case 0: return ComparadorLancesMus.GanadorGrande(evaluaciones, Mano);
            case 1: return ComparadorLancesMus.GanadorChica(evaluaciones, Mano);
            case 2: return ComparadorLancesMus.GanadorPares(evaluaciones, Mano);
            case 3: return ComparadorLancesMus.GanadorJuegoPunto(evaluaciones, out hayJuego, Mano);
            default: return -1;
        }
    }

    public bool PuedeHacerSenal(int jugador, TipoSenalMus tipo)
    {
        if (jugador < 0 || jugador >= Jugadores || Fase == FaseMus.FinMano || Fase == FaseMus.FinPartida)
            return false;

        if (MusCorridoPendiente)
            return false;

        return CumpleSenal(CartasDe(jugador), evaluaciones[jugador], tipo);
    }

    public static bool CumpleSenal(int[] cartas, EvaluadorManoMus.Resultado evaluacion, TipoSenalMus tipo)
    {
        switch (tipo)
        {
            case TipoSenalMus.TreintaYUna:
                return evaluacion.PuntosJuego == 31;
            case TipoSenalMus.Duples:
                return evaluacion.Pares == EvaluadorManoMus.TipoPares.Duples;
            case TipoSenalMus.DosReyes:
                return ContarRango(cartas, 8) >= 2;
            case TipoSenalMus.DosAses:
                return ContarRango(cartas, 1) >= 2;
            case TipoSenalMus.TresReyes:
                return ContarRango(cartas, 8) >= 3;
            case TipoSenalMus.TresAses:
                return ContarRango(cartas, 1) >= 3;
            case TipoSenalMus.Ciego:
                return evaluacion.Pares == EvaluadorManoMus.TipoPares.SinPares && !evaluacion.TieneJuego &&
                       ContarRango(cartas, 8) == 0 && ContarRango(cartas, 1) <= 1;
            default:
                return false;
        }
    }

    public bool HacerSenal(int jugador, TipoSenalMus tipo)
    {
        if (!PuedeHacerSenal(jugador, tipo))
            return false;

        List<TipoSenalMus> recibidas = senalesRecibidas[(jugador + 2) % Jugadores];

        if (recibidas.Contains(tipo))
            return false;

        recibidas.Add(tipo);
        SenalRealizada?.Invoke(jugador, tipo);
        return true;
    }

    public ObservacionMus Observar(int jugador)
    {
        ObservacionMus obs = new()
        {
            Jugador = jugador,
            Fase = Fase,
            Mano = Mano,
            JugadorActual = JugadorActual,
            MusCorrido = MusCorridoPendiente,
            RondaMus = RondaMus,
            Evaluacion = evaluaciones[jugador],
            PuntosPropios = puntos[jugador % 2],
            PuntosRivales = puntos[(jugador + 1) % 2],
            Limite = Limite,
            EstadoEnvite = turnos.Motor.Estado,
            ParejaEnvidante = turnos.Motor.ParejaEnvidante,
            TantosApostados = turnos.Motor.TantosApostados
        };

        for (int c = 0; c < CartasPorJugador; c++)
            obs.Cartas[c] = manos[jugador, c];

        for (int j = 0; j < Jugadores; j++)
        {
            obs.PuedeHablarLance[j] = EsLance(Fase) && puedeHablar[j];
            obs.CartasDescartadas[j] = cartasDescartadas[j];
        }

        obs.HistorialMano.AddRange(historial);
        obs.SenalesRecibidas.AddRange(senalesRecibidas[jugador]);
        return obs;
    }

    private void Registrar(TipoEventoMus tipo, int jugador, AccionMus accion, int dato = 0)
    {
        EventoMus evento = new(tipo, Fase, jugador, accion, dato);
        historial.Add(evento);
        EventoRegistrado?.Invoke(evento);
    }

    public static bool EsLance(FaseMus fase)
    {
        return fase == FaseMus.Grande || fase == FaseMus.Chica ||
               fase == FaseMus.Pares || fase == FaseMus.JuegoPunto;
    }

    private static int IndiceLance(FaseMus fase)
    {
        switch (fase)
        {
            case FaseMus.Grande: return 0;
            case FaseMus.Chica: return 1;
            case FaseMus.Pares: return 2;
            default: return 3;
        }
    }

    private static string NombreLance(int lance, bool hayJuego)
    {
        switch (lance)
        {
            case 0: return "Grande";
            case 1: return "Chica";
            case 2: return "Pares";
            default: return hayJuego ? "Juego" : "Punto";
        }
    }

    public static int ValorPares(EvaluadorManoMus.TipoPares pares)
    {
        switch (pares)
        {
            case EvaluadorManoMus.TipoPares.Par: return 1;
            case EvaluadorManoMus.TipoPares.Medias: return 2;
            case EvaluadorManoMus.TipoPares.Duples: return 3;
            default: return 0;
        }
    }

    private static int ContarRango(int[] cartas, int rango)
    {
        int cuenta = 0;

        foreach (int carta in cartas)
            if (CartasMus.Rango(carta) == rango)
                cuenta++;

        return cuenta;
    }

    public void PrepararManosFijas(int[][] cartas)
    {
        if (cartas == null || cartas.Length != Jugadores)
            throw new ArgumentException("Se necesitan cuatro manos.");

        HashSet<int> usadas = new();

        for (int j = 0; j < Jugadores; j++)
        {
            for (int c = 0; c < CartasPorJugador; c++)
            {
                if (!usadas.Add(cartas[j][c]))
                    throw new ArgumentException("Carta repetida en las manos fijas.");

                manos[j, c] = cartas[j][c];
            }
        }

        EvaluarManos();
    }
}
