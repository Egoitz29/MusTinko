using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GestorFasesMus : MonoBehaviour
{
    public enum Fase
    {
        Mus,
        Descarte,
        Grande,
        Chica,
        Pares,
        JuegoPunto,
        FinMano
    }

    public Fase FaseActual => faseActual;

    // Jugador que es mano durante toda la mano actual.
    // Se rota una sola vez al repartir la siguiente mano.
    public int JugadorMano => jugadorMano;

    [Header("Rotacion del jugador mano")]
    [Range(0, 3)]
    [SerializeField] private int jugadorMano = 0;

    // Al arrancar una partida, el primer jugador que corta
    // en el mus corrido queda designado como primer mano.
    // Se conserva durante los descartes iniciales y se desactiva
    // solo cuando alguien dice NO HAY MUS.
    private bool musCorridoInicialPendiente = true;

    // Cuantos jugadores han dicho MUS en esta ronda.
    private int jugadoresQueDijeronMus;

    [Header("Estado de partida")]
    [SerializeField] private Fase faseActual = Fase.Mus;

    [Header("Referencias")]
    [SerializeField] private RepartoPrueba reparto;
    [SerializeField] private CartaSeleccionable[] cartas;

    [Header("Puntuación")]
    [SerializeField] private MarcadorMus marcador;

    [Header("Revelado de cartas")]
    [SerializeField] private ReveladorManosMus reveladorManos;

    [Header("Sistema de envites")]
    [SerializeField] private GestorEnvitesMus gestorEnvites;

    [Header("Botones")]
    [SerializeField] private Button botonMus;
    [SerializeField] private Button botonNoHayMus;
    [SerializeField] private Button botonConfirmarDescarte;
    [SerializeField] private Button botonSiguienteLance;

    [Header("Interfaz")]
    [SerializeField] private TMP_Text textoFase;

    [Header("IA MUS de los bots")]
    [Min(0f)]
    [SerializeField] private float tiempoDecisionBot = 0.8f;

    [Range(0, 100)]
    [Tooltip("Cuanto mas alto, mejores cartas exige el bot para cortar Mus.")]
    [SerializeField] private int exigenciaParaCortar = 55;

    [Min(1)]
    [Tooltip("Si nadie corta, en esta ronda los bots cortaran para evitar un Mus interminable.")]
    [SerializeField] private int maximoRondasMus = 4;

    // Rondas de Mus jugadas desde el ultimo reparto de 16 cartas.
    // NO se reinicia al descartar: sigue siendo la misma mano.
    private int rondasMusMano;

    // Indice del jugador al que corresponde decidir MUS ahora.
    private int turnoMus;

    private Coroutine rutinaBot;

    private readonly string[] nombresJugadores =
    {
        "TÚ",
        "RIVAL DERECHO",
        "COMPAÑERO",
        "RIVAL IZQUIERDO"
    };

    // =====================================================
    // INICIO
    // =====================================================

    private void Start()
    {
        // Cada nueva partida arranca con el mus corrido inicial.
        // El 0 es el primero en hablar, pero todavia NO es mano.
        musCorridoInicialPendiente = true;
        jugadorMano = 0;
        rondasMusMano = 0;

        IniciarRondaMus();
    }

    private void IniciarRondaMus()
    {
        DetenerBot();

        faseActual = Fase.Mus;
        jugadoresQueDijeronMus = 0;
        rondasMusMano++;

        // En el mus corrido inicial siempre iniciamos la ronda
        // por el jugador 0 hasta que alguien corte.
        // En las manos normales comienza quien es mano.
        turnoMus = musCorridoInicialPendiente ? 0 : jugadorMano;

        // Antes del primer corte no hay jugador mano definitivo.
        if (!musCorridoInicialPendiente && marcador != null)
            marcador.EstablecerJugadorMano(jugadorMano);

        ActualizarInterfaz();

        if (musCorridoInicialPendiente)
        {
            Debug.Log(
                "MUS CORRIDO INICIAL: aun no hay jugador MANO. " +
                "Empieza " + nombresJugadores[turnoMus] + "."
            );
        }
        else
        {
            Debug.Log(
                "Nueva ronda de Mus. MANO: jugador " +
                jugadorMano + " (" + nombresJugadores[jugadorMano] + ")."
            );
        }

        // Si comienza un bot, dejamos que decida.
        if (turnoMus != 0)
            rutinaBot = StartCoroutine(DecisionBot());
    }

    // =====================================================
    // DECISIONES DEL JUGADOR LOCAL
    // =====================================================

    public void ElegirMus()
    {
        if (!PuedeDecidirJugadorLocal())
            return;

        Debug.Log("TÚ dices MUS.");

        // Solo presentación: la lógica del Mus sigue separada.
        if (gestorEnvites != null)
            gestorEnvites.MostrarBocadillo(0, "MUS");

        RegistrarMus();
    }

    public void CortarMus()
    {
        if (!PuedeDecidirJugadorLocal())
            return;

        Debug.Log("TÚ dices NO HAY MUS.");

        if (gestorEnvites != null)
            gestorEnvites.MostrarBocadillo(0, "NO HAY MUS");

        CortarMusJugador(0);
    }

    // Comun para los cortes del jugador local y de los bots.
    private void CortarMusJugador(int jugadorQueCorta)
    {
        if (faseActual != Fase.Mus ||
            jugadorQueCorta != turnoMus)
            return;

        DetenerBot();

        if (musCorridoInicialPendiente)
        {
            // SOLO durante el primer mus corrido quien corta
            // se convierte en el primer jugador mano.
            jugadorMano = jugadorQueCorta;
            musCorridoInicialPendiente = false;

            if (marcador != null)
                marcador.EstablecerJugadorMano(jugadorMano);

            Debug.Log(
                "MUS CORRIDO TERMINADO. PRIMER MANO: jugador " +
                jugadorMano + " (" + nombresJugadores[jugadorMano] + ")."
            );
        }
        else
        {
            // En el mus normal el corte NO cambia al mano.
            Debug.Log(
                "Mus cortado por " + nombresJugadores[jugadorQueCorta] +
                ". Se mantiene MANO: jugador " + jugadorMano + "."
            );
        }

        faseActual = Fase.Grande;
        ActualizarInterfaz();
    }

    private bool PuedeDecidirJugadorLocal()
    {
        return faseActual == Fase.Mus && turnoMus == 0;
    }

    // =====================================================
    // REGISTRO DE MUS
    // =====================================================

    private void RegistrarMus()
    {
        if (faseActual != Fase.Mus)
            return;

        jugadoresQueDijeronMus++;

        // Los cuatro jugadores han aceptado Mus.
        if (jugadoresQueDijeronMus >= 4)
        {
            faseActual = Fase.Descarte;

            Debug.Log(
                "Los cuatro jugadores dicen MUS. " +
                "Comienza la fase de descarte."
            );

            ActualizarInterfaz();
            return;
        }

        // Pasamos al siguiente jugador en sentido de la mesa.
        turnoMus = (turnoMus + 1) % 4;
        ActualizarInterfaz();

        // Solo los jugadores 1, 2 y 3 son bots provisionales.
        if (turnoMus != 0)
            rutinaBot = StartCoroutine(DecisionBot());
    }

    // =====================================================
    // DECISIONES DE LOS BOTS
    // =====================================================

    private IEnumerator DecisionBot()
    {
        yield return new WaitForSeconds(tiempoDecisionBot);

        rutinaBot = null;

        if (faseActual != Fase.Mus ||
            turnoMus < 1 ||
            turnoMus > 3)
        {
            yield break;
        }

        int jugador = turnoMus;

        // Cada bot solo examina sus propias cuatro cartas.
        int fuerza = CalcularFuerzaMus(jugador);
        bool porCalidad = fuerza >= exigenciaParaCortar;
        bool porLimite = rondasMusMano >= maximoRondasMus;
        bool cortarMus = porCalidad || porLimite;

        Debug.Log(
            "IA MUS - " + nombresJugadores[jugador] +
            " | Fuerza: " + fuerza + "/100" +
            " | Umbral: " + exigenciaParaCortar +
            " | Ronda: " + rondasMusMano + "/" + maximoRondasMus +
            " | Decision: " + (cortarMus ? "NO HAY MUS" : "MUS") +
            (porLimite && !porCalidad ? " (limite de rondas)" : "")
        );

        if (cortarMus)
        {
            Debug.Log(
                nombresJugadores[jugador] +
                " dice NO HAY MUS."
            );

            // Mostramos la declaración en el bocadillo del bot.
            if (gestorEnvites != null)
                gestorEnvites.MostrarBocadillo(jugador, "NO HAY MUS");

            // El primer corte del mus corrido designa al mano.
            // En partidas posteriores solo termina el Mus.
            CortarMusJugador(jugador);
            yield break;
        }

        Debug.Log(
            nombresJugadores[jugador] +
            " dice MUS."
        );

        if (gestorEnvites != null)
            gestorEnvites.MostrarBocadillo(jugador, "MUS");

        RegistrarMus();
    }

    // =====================================================
    // IA: FUERZA DE LA MANO PARA DECIDIR MUS
    // =====================================================

    // Heuristica provisional: no ve las cartas del contrario,
    // ni modifica reparto, envites o puntuaciones.
    // La fuerza va de 0 a 100 y no pretende ser una probabilidad.
    private int CalcularFuerzaMus(int jugador)
    {
        if (reparto == null)
        {
            Debug.LogError("IA MUS: falta la referencia al reparto.");
            return 0;
        }

        EvaluadorManoMus.Resultado mano =
            reparto.EvaluarJugador(jugador);

        if (mano == null)
        {
            Debug.LogError("IA MUS: no se pudo evaluar al jugador " + jugador);
            return 0;
        }

        int fuerza = 0;

        // Los pares son una buena razon para conservar la mano.
        switch (mano.Pares)
        {
            case EvaluadorManoMus.TipoPares.Duples:
                fuerza += 80;
                break;
            case EvaluadorManoMus.TipoPares.Medias:
                fuerza += 75;
                break;
            case EvaluadorManoMus.TipoPares.Par:
                fuerza += 30;
                break;
        }

        // El valor de Juego suma atractivo a la mano.
        // Importa el orden MUS: 31 > 32 > 40 > 37 > ... > 33.
        if (mano.TieneJuego)
        {
            switch (mano.PuntosJuego)
            {
                case 31: fuerza += 75; break;
                case 32: fuerza += 65; break;
                case 40: fuerza += 55; break;
                case 37: fuerza += 35; break;
                case 36: fuerza += 30; break;
                case 35: fuerza += 25; break;
                case 34: fuerza += 20; break;
                case 33: fuerza += 15; break;
            }
        }

        // En Mus hay 8 reyes y 8 ases: el 3 cuenta como REY
        // y el 2 cuenta como AS. Sirve para valorar Grande/Chica.
        int reyes = 0;
        int ases = 0;

        for (int posicion = 0; posicion < 4; posicion++)
        {
            string carta = reparto.ObtenerCartaJugador(jugador, posicion);
            if (string.IsNullOrEmpty(carta))
                continue;

            string valor = carta.Split('\n')[0].Trim().ToUpperInvariant();

            if (valor == "REY" || valor == "3")
                reyes++;
            else if (valor == "AS" || valor == "2")
                ases++;
        }

        if (reyes >= 2)
            fuerza += 28;
        else if (reyes == 1)
            fuerza += 8;

        if (ases >= 2)
            fuerza += 28;
        else if (ases == 1)
            fuerza += 8;

        return Mathf.Clamp(fuerza, 0, 100);
    }

    // Diagnostico no destructivo desde el Inspector durante Play.
    [ContextMenu("IA MUS - Ver decisiones de los tres bots")]
    private void DiagnosticarDecisionesBots()
    {
        if (!Application.isPlaying || reparto == null)
        {
            Debug.LogWarning("IA MUS: inicia Play y revisa GestorReparto.");
            return;
        }

        for (int jugador = 1; jugador <= 3; jugador++)
        {
            int fuerza = CalcularFuerzaMus(jugador);
            bool cortar = fuerza >= exigenciaParaCortar ||
                          rondasMusMano >= maximoRondasMus;

            Debug.Log(
                "PRUEBA IA - " + nombresJugadores[jugador] +
                " | Fuerza " + fuerza + "/100" +
                " | " + (cortar ? "NO HAY MUS" : "MUS")
            );
        }
    }

    // =====================================================
    // DESCARTE DE LOS CUATRO JUGADORES
    // =====================================================

    public void ConfirmarDescarte()
    {
        if (faseActual != Fase.Descarte || reparto == null)
            return;

        // Primero descarta nuestro jugador.
        int cantidadLocal = reparto.DescartarSeleccionadas();

        Debug.Log(
            "Jugador local descarta " +
            cantidadLocal + " cartas."
        );

        // Después descartan los tres bots.
        for (int jugador = 1; jugador <= 3; jugador++)
        {
            // Entre cero y cuatro cartas.
            int cantidadBot = Random.Range(0, 5);

            reparto.DescartarBot(jugador, cantidadBot);
        }

        Debug.Log(
            "Los cuatro jugadores han terminado el descarte."
        );

        // Se inicia otra ronda de Mus.
        IniciarRondaMus();
    }

    // =====================================================
    // CONTROL DE LOS LANCES
    // =====================================================

    public void SiguienteLancePrueba()
    {
        if (gestorEnvites != null &&
    !gestorEnvites.LanceCerrado)
        {
            Debug.LogWarning(
                "Debes terminar las apuestas antes de avanzar."
            );
            return;
        }
        switch (faseActual)
        {
            case Fase.Grande:
                faseActual = Fase.Chica;
                break;

            case Fase.Chica:
                faseActual = Fase.Pares;
                break;

            case Fase.Pares:
                faseActual = Fase.JuegoPunto;
                break;

            case Fase.JuegoPunto:

                // Terminamos los cuatro lances.
                faseActual = Fase.FinMano;

                // NUEVO:
                // Contabilizamos los tantos de la mano.
                if (marcador != null)
                {
                    marcador.ContabilizarManoConEnvites();
                }
                else
                {
                    Debug.LogWarning(
                        "No se ha asignado MarcadorMus " +
                        "en el GestorFases."
                    );
                }
                if (reveladorManos != null)
                {
                    reveladorManos.MostrarManos();
                }
                break;

            default:
                return;
        }

        ActualizarInterfaz();
    }

    // =====================================================
    // NUEVA MANO
    // =====================================================

    public void NuevaManoPrueba()
    {
        if (reparto == null)
        {
            Debug.LogError(
                "No se ha asignado GestorReparto."
            );
            return;
        }

        if (faseActual != Fase.FinMano)
        {
            Debug.LogWarning(
                "Siguiente mano solo puede pulsarse al terminar la actual."
            );
            return;
        }

        if (marcador != null && marcador.JuegoTerminado)
        {
            Debug.LogWarning(
                "El juego ha terminado. Pulsa NUEVA PARTIDA."
            );
            return;
        }

        DetenerBot();

        if (reveladorManos != null)
        {
            reveladorManos.OcultarManos();
        }

        // Tras el mus corrido inicial ya conocemos al primer mano.
        // La rotacion posterior se hace solo al empezar mano nueva,
        // nunca durante los descartes de una misma mano.
        jugadorMano = (jugadorMano + 1) % 4;
        Debug.Log("ROTACION: ahora es MANO el jugador " + jugadorMano);

        // Reiniciamos el limite de rondas: comienza otra mano.
        rondasMusMano = 0;

        // Repartimos 16 cartas nuevas.
        reparto.RepartirMano();

        // NUEVO:
        // Permitimos contabilizar la siguiente mano.
        // El marcador acumulado NO se reinicia.
        if (marcador != null)
        {
            marcador.PrepararNuevaMano();
        }

        // Comenzamos otra ronda de Mus.
        IniciarRondaMus();
    }

    // =====================================================
    // CONTROL DE CORRUTINAS
    // =====================================================

    private void DetenerBot()
    {
        if (rutinaBot != null)
        {
            StopCoroutine(rutinaBot);
            rutinaBot = null;
        }
    }

    private void OnDisable()
    {
        DetenerBot();
    }

    // =====================================================
    // ACTUALIZACIÓN DE INTERFAZ
    // =====================================================

    private void ActualizarInterfaz()
    {
        bool enMus = faseActual == Fase.Mus;

        bool turnoLocal =
            enMus && turnoMus == 0;

        bool enDescarte =
            faseActual == Fase.Descarte;

        bool enLance =
            faseActual == Fase.Grande ||
            faseActual == Fase.Chica ||
            faseActual == Fase.Pares ||
            faseActual == Fase.JuegoPunto;

        // -------------------------------------------------
        // BOTONES DE MUS
        // -------------------------------------------------

        if (botonMus != null)
            botonMus.gameObject.SetActive(turnoLocal);

        if (botonNoHayMus != null)
            botonNoHayMus.gameObject.SetActive(turnoLocal);

        // -------------------------------------------------
        // BOTÓN DE DESCARTE
        // -------------------------------------------------

        if (botonConfirmarDescarte != null)
        {
            botonConfirmarDescarte.gameObject.SetActive(
                enDescarte
            );
        }

        // -------------------------------------------------
        // BOTÓN DE LANCES (PROVISIONAL)
        // -------------------------------------------------

        if (botonSiguienteLance != null)
        {
            botonSiguienteLance.gameObject.SetActive(
                enLance
            );
        }

        // -------------------------------------------------
        // SELECCIÓN DE CARTAS
        // -------------------------------------------------

        if (cartas != null)
        {
            foreach (CartaSeleccionable carta in cartas)
            {
                if (carta == null)
                    continue;

                Button boton = carta.GetComponent<Button>();

                if (boton != null)
                {
                    boton.interactable = enDescarte;
                }

                // Fuera del descarte no permitimos
                // mantener cartas seleccionadas.
                if (!enDescarte)
                {
                    carta.Deseleccionar();
                }
            }
        }

        // -------------------------------------------------
        // TEXTO DE LA FASE ACTUAL
        // -------------------------------------------------

        if (textoFase == null)
            return;

        switch (faseActual)
        {
            case Fase.Mus:

                string prefijoMus = musCorridoInicialPendiente
                    ? "MUS CORRIDO - " : "";

                if (turnoMus == 0)
                {
                    textoFase.text = prefijoMus +
                        "TU TURNO: ¿MUS O NO HAY MUS?";
                }
                else
                {
                    textoFase.text = prefijoMus +
                        nombresJugadores[turnoMus] +
                        " DECIDE...";
                }

                break;

            case Fase.Descarte:
                textoFase.text = "FASE DE DESCARTE";
                break;

            case Fase.Grande:
                textoFase.text = "LANCE: GRANDE";
                break;

            case Fase.Chica:
                textoFase.text = "LANCE: CHICA";
                break;

            case Fase.Pares:
                textoFase.text = "LANCE: PARES";
                break;

            case Fase.JuegoPunto:
                textoFase.text = "LANCE: JUEGO / PUNTO";
                break;

            case Fase.FinMano:
                textoFase.text = "FIN DE LA MANO";
                break;
        }
    }


    public void RegistrarResultadoEnvite(
        Fase fase,
        MotorEnvitesMus motor)
    {
        if (marcador == null || motor == null)
        {
            Debug.LogError(
                "Falta el marcador o el motor de envites."
            );
            return;
        }

        // Si se acepta un órdago,
        // el juego termina inmediatamente.
        if (motor.Estado ==
            MotorEnvitesMus.EstadoEnvite.OrdagoAceptado)
        {
            int parejaGanadora =
                marcador.ResolverOrdagoAceptado(fase);


            if (parejaGanadora >= 0)
            {
                DetenerBot();

                if (reveladorManos != null)
                {
                    reveladorManos.MostrarManos();
                }

                faseActual = Fase.FinMano;
                ActualizarInterfaz();
            }


            return;
        }

        // Envites normales: guardamos el resultado
        // y los contamos al terminar la mano.
        marcador.RegistrarLance(fase, motor);

        // Una negada puede llegar al limite de 40 durante
        // Grande, Chica, Pares o Juego. Acabamos al instante.
        if (marcador.JuegoTerminado && faseActual != Fase.FinMano)
        {
            DetenerBot();

            if (reveladorManos != null)
                reveladorManos.MostrarManos();

            faseActual = Fase.FinMano;
            ActualizarInterfaz();
        }
    }

    // Reinicia una partida completa sin recargar la escena:
    // restablece el mus corrido, la baraja y el marcador.
    public void ReiniciarPartidaCompleta()
    {
        if (reparto == null || marcador == null)
        {
            Debug.LogError(
                "Falta GestorReparto o GestorPuntuacion."
            );
            return;
        }

        DetenerBot();

        if (reveladorManos != null)
            reveladorManos.OcultarManos();

        marcador.ReiniciarPartidaCompleta();
        musCorridoInicialPendiente = true;
        jugadorMano = 0;
        rondasMusMano = 0;

        reparto.RepartirMano();
        IniciarRondaMus();

        Debug.Log("NUEVA PARTIDA: comienza el mus corrido inicial.");
    }

}
