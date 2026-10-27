using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Control de interfaz y bots. Las reglas de turnos estan en MotorTurnosIndividualesMus.
public class GestorEnvitesMus : MonoBehaviour
{
    private enum Situacion
    {
        Inactivo,
        Anunciando,
        Jugando,
        Cerrado
    }

    [Header("Referencias")]
    [SerializeField] private GestorFasesMus gestorFases;
    [SerializeField] private GameObject panelEnvites;
    [SerializeField] private RepartoPrueba reparto;

    // Indices: 0=TÚ, 1=RIVAL DERECHO, 2=COMPAÑERO, 3=RIVAL IZQUIERDO.
    // En esta prueba solo conectamos el elemento 1.
    [Header("Bocadillos 3D por jugador")]
    [SerializeField]
    private BocadilloMus3D[] bocadillosJugadores =
        new BocadilloMus3D[4];

    [Header("Anuncios de Pares y Juego")]
    [SerializeField] private TMP_Text textoAnuncios;
    [Min(0f)][SerializeField] private float tiempoEntreAnuncios = 1f;

    [Header("Botones de apuestas")]
    [SerializeField] private Button botonPasar;
    [SerializeField] private Button botonEnvidar;
    [SerializeField] private Button botonQuiero;
    [SerializeField] private Button botonNoQuiero;
    [SerializeField] private Button botonMas;
    [SerializeField] private Button botonOrdago;

    [Header("Indicador de apuestas")]
    [SerializeField] private TMP_Text textoEstadoApuestas;
    [SerializeField] private MarcadorMus marcador;

    [Header("Control de avance")]
    [SerializeField] private Button botonSiguienteLance;

    [Header("Bots provisionales")]
    [Min(0f)][SerializeField] private float tiempoRespuestaBot = 0.8f;
    [Range(0f, 1f)][SerializeField] private float probabilidadBotEnvida = 0.5f;
    [Range(0f, 1f)][SerializeField] private float probabilidadBotQuiere = 0.7f;
    [Range(0f, 1f)][SerializeField] private float probabilidadBotReenvida = 0.10f;

    private readonly MotorTurnosIndividualesMus turnos =
        new MotorTurnosIndividualesMus();

    private Situacion situacion = Situacion.Inactivo;
    private GestorFasesMus.Fase faseAnterior = (GestorFasesMus.Fase)(-1);

    private Coroutine rutinaBot;
    private Coroutine rutinaAnuncios;

    private readonly string[] nombresJugadores =
    {
        "TÚ", "RIVAL DERECHO", "COMPAÑERO", "RIVAL IZQUIERDO"
    };

    // Se mantiene el contrato utilizado por GestorFasesMus.
    public bool LanceCerrado =>
        gestorFases != null &&
        EsLance(gestorFases.FaseActual) &&
        faseAnterior == gestorFases.FaseActual &&
        situacion == Situacion.Cerrado;

    private void Start()
    {
        if (panelEnvites != null) panelEnvites.SetActive(false);
        if (textoAnuncios != null) textoAnuncios.gameObject.SetActive(false);
        situacion = Situacion.Inactivo;
        ActualizarBotones();
    }

    private void Update()
    {
        if (gestorFases == null) return;
        if (gestorFases.FaseActual != faseAnterior)
            CambiarFase(gestorFases.FaseActual);
    }

    private static bool EsLance(GestorFasesMus.Fase fase)
    {
        return fase == GestorFasesMus.Fase.Grande ||
               fase == GestorFasesMus.Fase.Chica ||
               fase == GestorFasesMus.Fase.Pares ||
               fase == GestorFasesMus.Fase.JuegoPunto;
    }

    private void CambiarFase(GestorFasesMus.Fase fase)
    {
        DetenerBot();
        DetenerAnuncios();

        // Nuevo lance: borramos los mensajes del lance anterior.
        // No se borran automaticamente al cerrar el lance.
        OcultarTodosLosBocadillos();

        faseAnterior = fase;
        turnos.Reiniciar();
        situacion = Situacion.Inactivo;

        if (panelEnvites != null) panelEnvites.SetActive(false);
        if (textoAnuncios != null) textoAnuncios.gameObject.SetActive(false);
        ActualizarBotones();

        if (!EsLance(fase)) return;

        if (fase == GestorFasesMus.Fase.Pares ||
            fase == GestorFasesMus.Fase.JuegoPunto)
        {
            situacion = Situacion.Anunciando;
            ActualizarBotones();
            rutinaAnuncios = StartCoroutine(AnunciarLanceEspecial(fase));
        }
        else
        {
            AbrirApuestas(fase, null);
        }
    }

    private IEnumerator AnunciarLanceEspecial(GestorFasesMus.Fase fase)
    {
        if (reparto == null)
        {
            ErrorAnuncios("Falta asignar GestorReparto.");
            yield break;
        }

        EvaluadorManoMus.Resultado[] manos = new EvaluadorManoMus.Resultado[4];
        ReglasLancesEspecialesMus.Decision decision;
        try
        {
            for (int i = 0; i < 4; i++)
                manos[i] = reparto.EvaluarJugador(i);
            decision = ReglasLancesEspecialesMus.Evaluar(manos, fase);
        }
        catch (Exception e)
        {
            ErrorAnuncios("Error evaluando manos: " + e.Message);
            yield break;
        }

        string lance = fase == GestorFasesMus.Fase.Pares ? "PARES" : "JUEGO";
        for (int paso = 0; paso < 4; paso++)
        {
            if (!EsFaseActual(fase)) yield break;
            int jugador = (gestorFases.JugadorMano + paso) % 4;
            bool tiene = fase == GestorFasesMus.Fase.Pares
                ? manos[jugador].Pares != EvaluadorManoMus.TipoPares.SinPares
                : manos[jugador].TieneJuego;

            string mensaje = nombresJugadores[jugador] + ": " + lance +
                             (tiene ? " SÍ" : " NO");
            MostrarAnuncio(mensaje);
            // Muestra PARES SÍ/NO y JUEGO SÍ/NO encima de quien habla.
            MostrarBocadillo(jugador, lance + (tiene ? " SÍ" : " NO"));
            Debug.Log(mensaje);
            yield return new WaitForSeconds(tiempoEntreAnuncios);
        }

        if (!EsFaseActual(fase)) yield break;
        MostrarAnuncio(decision.Mensaje);
        Debug.Log(decision.Mensaje);
        yield return new WaitForSeconds(tiempoEntreAnuncios);
        if (!EsFaseActual(fase)) yield break;

        rutinaAnuncios = null;
        if (!decision.PermiteEnvites)
        {
            CerrarLance(decision.Mensaje);
            yield break;
        }

        AbrirApuestas(fase, manos);
    }

    private void AbrirApuestas(
        GestorFasesMus.Fase fase,
        EvaluadorManoMus.Resultado[] manos)
    {
        if (!EsFaseActual(fase)) return;

        // Los anuncios de PARES/JUEGO son temporales.
        // Al empezar las apuestas se limpian para dejar espacio
        // a PASO / ENVIDO / QUIERO / etc.
        OcultarTodosLosBocadillos();

        bool[] puedenHablar = { true, true, true, true };
        if (fase == GestorFasesMus.Fase.Pares)
        {
            for (int i = 0; i < 4; i++)
                puedenHablar[i] = manos[i].Pares !=
                    EvaluadorManoMus.TipoPares.SinPares;
        }
        else if (fase == GestorFasesMus.Fase.JuegoPunto)
        {
            bool hayJuego = false;
            for (int i = 0; i < 4; i++)
                hayJuego |= manos[i].TieneJuego;

            if (hayJuego)
                for (int i = 0; i < 4; i++)
                    puedenHablar[i] = manos[i].TieneJuego;
        }

        try
        {
            turnos.Iniciar(gestorFases.JugadorMano, puedenHablar);
        }
        catch (Exception e)
        {
            ErrorAnuncios("No se pueden iniciar las apuestas: " + e.Message);
            return;
        }

        if (turnos.Cerrado)
        {
            CerrarLance("Nadie puede apostar en este lance.");
            return;
        }

        Debug.Log("Comienza " + fase + ". Mano: jugador " +
                  gestorFases.JugadorMano + ". Turnos individuales.");
        ProcesarSiguienteTurno();
    }

    private void ProcesarSiguienteTurno()
    {
        if (turnos.Cerrado)
        {
            CerrarLance("Lance resuelto.");
            return;
        }

        if (!EsFaseActual(faseAnterior)) return;

        situacion = Situacion.Jugando;
        int jugador = turnos.JugadorActual;
        bool respondiendo = turnos.HayEnvitePendiente;

        if (panelEnvites != null)
            panelEnvites.SetActive(jugador == 0);

        string mensaje = nombresJugadores[jugador] +
            (respondiendo ? ": RESPONDE AL ENVITE" : ": PUEDE PASAR O ENVIDAR");
        if (turnos.Motor.Estado == MotorEnvitesMus.EstadoEnvite.OrdagoPendiente)
            mensaje = nombresJugadores[jugador] + ": RESPONDE AL ÓRDAGO";

        MostrarAnuncio(mensaje);
        Debug.Log("TURNO DE " + nombresJugadores[jugador] +
                  " | " + faseAnterior +
                  (respondiendo ? " | RESPUESTA" : " | APUESTA"));
        ActualizarBotones();

        if (jugador != 0)
            rutinaBot = StartCoroutine(TurnoBot(jugador, faseAnterior));
    }

    private IEnumerator TurnoBot(int jugador, GestorFasesMus.Fase fase)
    {
        yield return new WaitForSeconds(tiempoRespuestaBot);
        rutinaBot = null;

        if (!EsFaseActual(fase) ||
            situacion != Situacion.Jugando ||
            turnos.JugadorActual != jugador)
            yield break;

        bool resultado;
        string accion;
        if (!turnos.HayEnvitePendiente)
        {
            if (UnityEngine.Random.value < probabilidadBotEnvida)
            {
                resultado = turnos.Envidar(jugador);
                accion = "ENVIDA 2";
            }
            else
            {
                resultado = turnos.Pasar(jugador);
                accion = "PASA";
            }
        }
        else if (turnos.Motor.Estado !=
                     MotorEnvitesMus.EstadoEnvite.OrdagoPendiente &&
                 UnityEngine.Random.value < probabilidadBotReenvida)
        {
            resultado = turnos.Reenvidar(jugador);
            accion = "REENVIDA 2 MÁS";
        }
        else if (UnityEngine.Random.value < probabilidadBotQuiere)
        {
            resultado = turnos.Quiero(jugador);
            accion = "DICE QUIERO";
        }
        else
        {
            resultado = turnos.NoQuiero(jugador);
            accion = "DICE NO QUIERO (POR MI PARTE)";
        }

        if (!resultado)
        {
            Debug.LogError("El bot " + jugador + " no pudo realizar: " + accion);
            situacion = Situacion.Inactivo;
            ActualizarBotones();
            yield break;
        }

        Debug.Log(nombresJugadores[jugador] + " " + accion);
        // Representacion local: persiste hasta la siguiente fase
        // o hasta que una apuesta nueva sustituya a la anterior.
        RegistrarAccionVisual(jugador, accion);
        ProcesarSiguienteTurno();
    }

    // Métodos públicos: mantener los OnClick existentes del Inspector.
    public void Pasar()
    {
        if (!EsTurnoHumano() || !turnos.Pasar(0)) return;
        Debug.Log("TÚ PASAS.");
        RegistrarAccionVisual(0, "PASA");
        ProcesarSiguienteTurno();
    }

    public void Envidar()
    {
        if (!EsTurnoHumano() || !turnos.Envidar(0)) return;
        Debug.Log("TÚ ENVIDAS 2.");
        RegistrarAccionVisual(0, "ENVIDA 2");
        ProcesarSiguienteTurno();
    }

    public void Quiero()
    {
        if (!EsTurnoHumano() || !turnos.Quiero(0)) return;
        Debug.Log("TÚ DICES QUIERO.");
        RegistrarAccionVisual(0, "DICE QUIERO");
        ProcesarSiguienteTurno();
    }

    public void NoQuiero()
    {
        if (!EsTurnoHumano() || !turnos.NoQuiero(0)) return;
        Debug.Log("TÚ DICES NO QUIERO (POR MI PARTE).");
        RegistrarAccionVisual(0, "DICE NO QUIERO (POR MI PARTE)");
        ProcesarSiguienteTurno();
    }

    public void Mas()
    {
        if (!EsTurnoHumano() || !turnos.Reenvidar(0)) return;
        Debug.Log("TÚ REENVIDAS 2 MÁS. TOTAL: " + turnos.Motor.TantosApostados);
        RegistrarAccionVisual(0, "REENVIDA 2 MÁS");
        ProcesarSiguienteTurno();
    }

    public void Ordago()
    {
        if (!EsTurnoHumano() || !turnos.LanzarOrdago(0)) return;
        Debug.Log("TÚ LANZAS ÓRDAGO.");
        RegistrarAccionVisual(0, "LANZA ÓRDAGO");
        ProcesarSiguienteTurno();
    }

    private bool EsTurnoHumano()
    {
        return situacion == Situacion.Jugando &&
               !turnos.Cerrado &&
               turnos.JugadorActual == 0 &&
               EsFaseActual(faseAnterior);
    }

    private bool EsFaseActual(GestorFasesMus.Fase fase)
    {
        return gestorFases != null && gestorFases.FaseActual == fase;
    }

    private void CerrarLance(string motivo)
    {
        if (situacion == Situacion.Cerrado) return;

        situacion = Situacion.Cerrado;
        Debug.Log(motivo);

        MotorEnvitesMus motor = turnos.Motor;
        switch (motor.Estado)
        {
            case MotorEnvitesMus.EstadoEnvite.SinApuesta:
                Debug.Log("LANCE EN PASO: " + faseAnterior);
                break;
            case MotorEnvitesMus.EstadoEnvite.Aceptado:
                Debug.Log("ENVITE ACEPTADO: " + motor.TantosApostados + " tantos.");
                break;
            case MotorEnvitesMus.EstadoEnvite.Rechazado:
                Debug.Log("ENVITE RECHAZADO: +" + motor.TantosPorNegada +
                          " pareja " + motor.ParejaGanadoraNegada + ".");
                break;
            case MotorEnvitesMus.EstadoEnvite.OrdagoAceptado:
                Debug.Log("ÓRDAGO ACEPTADO. Se resuelve el juego.");
                break;
        }

        MostrarAnuncio("LANCE " + faseAnterior.ToString().ToUpper() + " CERRADO");
        if (panelEnvites != null) panelEnvites.SetActive(false);

        if (gestorFases != null)
            gestorFases.RegistrarResultadoEnvite(faseAnterior, motor);

        ActualizarBotones();
    }

    private void ActualizarBotones()
    {
        bool humano = situacion == Situacion.Jugando &&
                      !turnos.Cerrado &&
                      turnos.JugadorActual == 0;
        bool responde = humano && turnos.HayEnvitePendiente;
        bool ordagoPendiente = responde &&
            turnos.Motor.Estado == MotorEnvitesMus.EstadoEnvite.OrdagoPendiente;

        if (botonPasar != null) botonPasar.gameObject.SetActive(humano && !responde);
        if (botonEnvidar != null) botonEnvidar.gameObject.SetActive(humano && !responde);
        if (botonQuiero != null) botonQuiero.gameObject.SetActive(responde);
        if (botonNoQuiero != null) botonNoQuiero.gameObject.SetActive(responde);
        if (botonMas != null) botonMas.gameObject.SetActive(responde && !ordagoPendiente);
        if (botonOrdago != null) botonOrdago.gameObject.SetActive(humano && !ordagoPendiente);

        if (botonSiguienteLance != null && EsLance(faseAnterior))
            botonSiguienteLance.interactable = situacion == Situacion.Cerrado;

        ActualizarAvisoApuestas();
    }

    private void ActualizarAvisoApuestas()
    {
        if (textoEstadoApuestas == null) return;
        string pendientes = marcador != null ? marcador.ObtenerEnvitesPendientes() : "";
        string apuesta = "";

        if (EsLance(faseAnterior))
        {
            if (turnos.Motor.Estado == MotorEnvitesMus.EstadoEnvite.PendienteRespuesta)
                apuesta = "ENVITE ACTUAL: " + turnos.Motor.TantosApostados + " tantos";
            else if (turnos.Motor.Estado == MotorEnvitesMus.EstadoEnvite.OrdagoPendiente)
                apuesta = "¡ÓRDAGO PENDIENTE!";
        }

        string contenido = string.IsNullOrEmpty(pendientes)
            ? "" : "ENVITES PENDIENTES\n" + pendientes;
        if (!string.IsNullOrEmpty(apuesta))
            contenido += (contenido.Length > 0 ? "\n" : "") + apuesta;

        textoEstadoApuestas.text = contenido;
    }

    // -----------------------------------------------------------------
    // BOCADILLOS DE APUESTAS (PERSISTENTES)
    // -----------------------------------------------------------------
    // Este metodo solo pinta acciones. La regla del lance se decide
    // exclusivamente en MotorTurnosIndividualesMus.
    // Asi servira tambien para acciones recibidas por red en el futuro.
    private void RegistrarAccionVisual(int jugador, string accion)
    {
        // Una nueva apuesta sustituye visualmente a la anterior:
        // borramos PASO, ENVIDO, QUIERO/NO QUIERO anteriores, etc.
        // El nuevo envite se queda visible mientras lo responden.
        if (accion == "ENVIDA 2" ||
            accion == "REENVIDA 2 MÁS" ||
            accion == "LANZA ÓRDAGO")
        {
            OcultarTodosLosBocadillos();
        }

        if (bocadillosJugadores == null ||
            jugador < 0 || jugador >= bocadillosJugadores.Length)
            return;

        BocadilloMus3D bocadillo = bocadillosJugadores[jugador];
        if (bocadillo != null)
            bocadillo.MostrarFijo(FraseBocadillo(accion));
    }

    // No se llama al cerrar la apuesta; se llama al comenzar la siguiente.
    private void OcultarTodosLosBocadillos()
    {
        if (bocadillosJugadores == null) return;

        foreach (BocadilloMus3D bocadillo in bocadillosJugadores)
        {
            if (bocadillo != null)
                bocadillo.Ocultar();
        }
    }

    // Presentación independiente: identificador del jugador + frase.
    // Los índices 0..3 serán los mismos cuando incorporemos el online.
    // MUS / NO HAY MUS y anuncios de PARES / JUEGO siguen temporales.
    public void MostrarBocadillo(int jugador, string mensaje)
    {
        if (bocadillosJugadores == null ||
            jugador < 0 || jugador >= bocadillosJugadores.Length)
            return;

        BocadilloMus3D bocadillo = bocadillosJugadores[jugador];
        if (bocadillo != null)
            bocadillo.Mostrar(mensaje);
    }

    private static string FraseBocadillo(string accion)
    {
        switch (accion)
        {
            case "PASA": return "PASO";
            case "ENVIDA 2": return "¡ENVIDO 2!";
            case "DICE QUIERO": return "QUIERO";
            case "DICE NO QUIERO (POR MI PARTE)": return "NO QUIERO";
            case "REENVIDA 2 MÁS": return "¡MÁS 2!";
            case "LANZA ÓRDAGO": return "¡ÓRDAGO!";
            default: return accion;
        }
    }

    private void MostrarAnuncio(string mensaje)
    {
        if (textoAnuncios == null) return;
        textoAnuncios.gameObject.SetActive(true);
        textoAnuncios.text = mensaje;
    }

    private void ErrorAnuncios(string mensaje)
    {
        Debug.LogError("ENVITES MUS: " + mensaje);
        MostrarAnuncio("ERROR: " + mensaje);
        situacion = Situacion.Inactivo;
        if (panelEnvites != null) panelEnvites.SetActive(false);
        ActualizarBotones();
    }

    private void DetenerBot()
    {
        if (rutinaBot == null) return;
        StopCoroutine(rutinaBot);
        rutinaBot = null;
    }

    private void DetenerAnuncios()
    {
        if (rutinaAnuncios == null) return;
        StopCoroutine(rutinaAnuncios);
        rutinaAnuncios = null;
    }

    private void OnDisable()
    {
        DetenerBot();
        DetenerAnuncios();
    }
}
