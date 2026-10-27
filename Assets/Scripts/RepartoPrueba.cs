
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RepartoPrueba : MonoBehaviour
{
    [Header("Cartas visibles del jugador local")]
    [SerializeField] private CartaSeleccionable[] cartas;

    private const int NumeroJugadores = 4;
    private const int CartasPorJugador = 4;

    private readonly string[] valores =
    {
        "AS", "2", "3", "4", "5",
        "6", "7", "SOTA", "CABALLO", "REY"
    };

    private readonly string[] palos =
    {
        "OROS", "COPAS", "ESPADAS", "BASTOS"
    };

    private readonly List<string> mazo = new();
    private readonly List<string> descartadas = new();

    // Jugadores:
    // 0 = Nosotros
    // 1 = Rival derecha
    // 2 = Compañero enfrente
    // 3 = Rival izquierda
    private readonly string[,] manos =
        new string[NumeroJugadores, CartasPorJugador];

    private void Start()
    {
        RepartirMano();
    }

    public void RepartirMano()
    {
        if (!ValidarCartas())
            return;

        mazo.Clear();
        descartadas.Clear();

        // Creamos la baraja española de 40 cartas.
        foreach (string palo in palos)
        {
            foreach (string valor in valores)
            {
                mazo.Add(valor + "\n" + palo);
            }
        }

        BarajarMazo();

        // Repartimos cuatro cartas a cada jugador.
        for (int jugador = 0;
             jugador < NumeroJugadores;
             jugador++)
        {
            for (int posicion = 0;
                 posicion < CartasPorJugador;
                 posicion++)
            {
                manos[jugador, posicion] = SacarCarta();
            }
        }

        // Solo mostramos las cartas del jugador 0.
        ActualizarManoLocal();

        VerificarReparto();

        Debug.Log(
            "Reparto completo: 4 jugadores, " +
            "16 cartas repartidas. " +
            "Quedan " + mazo.Count + " en el mazo."
        );
    }

    public int DescartarSeleccionadas()
    {
        if (!ValidarCartas())
            return 0;

        List<int> posiciones = new();

        for (int i = 0; i < CartasPorJugador; i++)
        {
            if (cartas[i].EstaSeleccionada)
                posiciones.Add(i);
        }

        // Conservar las cuatro cartas es válido.
        if (posiciones.Count == 0)
        {
            Debug.Log("El jugador conserva sus cuatro cartas.");
            return 0;
        }

        // Retiramos únicamente nuestras cartas elegidas.
        foreach (int posicion in posiciones)
        {
            descartadas.Add(manos[0, posicion]);
        }

        // Sacamos cartas nuevas del mazo compartido.
        foreach (int posicion in posiciones)
        {
            manos[0, posicion] = SacarCarta();
        }

        ActualizarManoLocal();
        VerificarReparto();

        Debug.Log(
            "Descarte de " + posiciones.Count +
            " cartas. Quedan " + mazo.Count +
            " en el mazo."
        );

        return posiciones.Count;
    }

    private string SacarCarta()
    {
        if (mazo.Count == 0)
        {
            // Provisional para pruebas de múltiples descartes.
            mazo.AddRange(descartadas);
            descartadas.Clear();
            BarajarMazo();
        }

        if (mazo.Count == 0)
        {
            Debug.LogError("No quedan cartas disponibles.");
            return "";
        }

        int indice = mazo.Count - 1;
        string carta = mazo[indice];

        mazo.RemoveAt(indice);

        return carta;
    }

    private void BarajarMazo()
    {
        for (int i = mazo.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            (mazo[i], mazo[j]) = (mazo[j], mazo[i]);
        }
    }


    private void ActualizarManoLocal()
    {
        // Actualizar las cuatro cartas visibles
        for (int i = 0; i < CartasPorJugador; i++)
        {
            TMP_Text texto =
                cartas[i].GetComponentInChildren<TMP_Text>();

            if (texto != null)
                texto.text = manos[0, i];

            cartas[i].Deseleccionar();
        }

        // Obtener nuestras cuatro cartas
        string[] manoLocal = new string[4];

        for (int i = 0; i < 4; i++)
        {
            manoLocal[i] = manos[0, i];
        }

        // Evaluar la mano según las reglas del Mus
        EvaluadorManoMus.Resultado resultado =
            EvaluadorManoMus.Evaluar(manoLocal);

        // Mostrar el resultado en la consola
        Debug.Log("EVALUACIÓN DE NUESTRA MANO:\n" + resultado);
    }


    private void VerificarReparto()
    {
        HashSet<string> cartasEnJuego = new();

        for (int jugador = 0;
             jugador < NumeroJugadores;
             jugador++)
        {
            for (int i = 0; i < CartasPorJugador; i++)
            {
                string carta = manos[jugador, i];

                if (!cartasEnJuego.Add(carta))
                {
                    Debug.LogError(
                        "Carta duplicada detectada: " + carta
                    );
                }
            }
        }
    }


#if UNITY_EDITOR
    [ContextMenu("Prueba - Ganador de Pares")]
    private void ProbarGanadorPares()
    {
        if (string.IsNullOrEmpty(manos[0, 0]))
        {
            Debug.LogWarning("Primero inicia la partida.");
            return;
        }

        var resultados =
            new EvaluadorManoMus.Resultado[NumeroJugadores];

        for (int jugador = 0; jugador < NumeroJugadores; jugador++)
        {
            resultados[jugador] = EvaluarJugador(jugador);
        }

        int ganador =
            ComparadorLancesMus.GanadorPares(resultados, 0);

        if (ganador == -1)
        {
            Debug.Log("PARES: Ningún jugador tiene pares.");
            return;
        }

        Debug.Log(
            "GANADOR PARES: Jugador " + ganador
        );

        Debug.Log(
            "Combinación ganadora: " +
            resultados[ganador].DetallePares
        );

        Debug.Log(
            "Pareja ganadora Pares: " +
            (ganador % 2 == 0 ? "0 y 2" : "1 y 3")
        );
    }
#endif

#if UNITY_EDITOR
    [ContextMenu("Prueba - Resolver todos los lances")]
    private void ResolverTodosLosLances()
    {
        if (string.IsNullOrEmpty(manos[0, 0]))
        {
            Debug.LogWarning("Primero inicia la partida.");
            return;
        }

        // Evaluamos las cuatro manos.
        var resultados =
            new EvaluadorManoMus.Resultado[NumeroJugadores];

        for (int jugador = 0; jugador < NumeroJugadores; jugador++)
        {
            resultados[jugador] = EvaluarJugador(jugador);
        }

        // Por ahora el jugador 0 es mano.
        int jugadorMano = 0;

        // Calculamos los ganadores.
        int ganadorGrande =
            ComparadorLancesMus.GanadorGrande(
                resultados, jugadorMano);

        int ganadorChica =
            ComparadorLancesMus.GanadorChica(
                resultados, jugadorMano);

        int ganadorPares =
            ComparadorLancesMus.GanadorPares(
                resultados, jugadorMano);

        bool hayJuego;

        int ganadorJuegoPunto =
            ComparadorLancesMus.GanadorJuegoPunto(
                resultados,
                out hayJuego,
                jugadorMano);

        // Mostramos el resumen completo.
        Debug.Log("========== RESUMEN DE MANO ==========");

        MostrarGanador("GRANDE", ganadorGrande);
        MostrarGanador("CHICA", ganadorChica);
        MostrarGanador("PARES", ganadorPares);

        MostrarGanador(
            hayJuego ? "JUEGO" : "PUNTO",
            ganadorJuegoPunto
        );

        Debug.Log("====================================");
    }

    private void MostrarGanador(string lance, int ganador)
    {
        if (ganador == -1)
        {
            Debug.Log(lance + ": No se disputa.");
            return;
        }

        string pareja = ganador % 2 == 0
            ? "Pareja 0 y 2"
            : "Pareja 1 y 3";

        Debug.Log(
            lance +
            " | Ganador: Jugador " + ganador +
            " | " + pareja
        );
    }
#endif


    private bool ValidarCartas()
    {
        if (cartas == null ||
            cartas.Length != CartasPorJugador)
        {
            Debug.LogError(
                "Asigna las cuatro cartas de la interfaz."
            );
            return false;
        }

        foreach (CartaSeleccionable carta in cartas)
        {
            if (carta == null)
            {
                Debug.LogError("Falta una carta asignada.");
                return false;
            }
        }

        return true;
    }


    public int DescartarBot(int jugador, int cantidad)
    {
        // El jugador 0 somos nosotros.
        // Los jugadores 1, 2 y 3 son bots.
        if (jugador < 1 || jugador >= NumeroJugadores)
        {
            Debug.LogError("Identificador de bot incorrecto.");
            return 0;
        }

        cantidad = Mathf.Clamp(cantidad, 0, CartasPorJugador);

        // Posiciones de las cuatro cartas del bot.
        List<int> posiciones = new List<int> { 0, 1, 2, 3 };

        // Elegimos posiciones diferentes al azar.
        for (int i = 0; i < cantidad; i++)
        {
            int aleatorio = Random.Range(i, posiciones.Count);

            (posiciones[i], posiciones[aleatorio]) =
                (posiciones[aleatorio], posiciones[i]);
        }

        // Retiramos todas las cartas elegidas.
        for (int i = 0; i < cantidad; i++)
        {
            int posicion = posiciones[i];

            descartadas.Add(manos[jugador, posicion]);
        }

        // Repartimos las cartas nuevas.
        for (int i = 0; i < cantidad; i++)
        {
            int posicion = posiciones[i];

            manos[jugador, posicion] = SacarCarta();
        }

        VerificarReparto();

        Debug.Log(
            "BOT " + jugador +
            " descarta " + cantidad +
            " cartas. Quedan " + mazo.Count +
            " en el mazo."
        );

        return cantidad;
    }


    public EvaluadorManoMus.Resultado EvaluarJugador(int jugador)
    {
        if (jugador < 0 || jugador >= NumeroJugadores)
        {
            Debug.LogError("Jugador inválido: " + jugador);
            return null;
        }

        string[] mano = new string[CartasPorJugador];

        for (int i = 0; i < CartasPorJugador; i++)
        {
            mano[i] = manos[jugador, i];
        }

        return EvaluadorManoMus.Evaluar(mano);
    }

#if UNITY_EDITOR
    [ContextMenu("Prueba - Evaluar las cuatro manos")]
    private void EvaluarTodasLasManos()
    {
        if (string.IsNullOrEmpty(manos[0, 0]))
        {
            Debug.LogWarning("Primero inicia la partida.");
            return;
        }

        for (int jugador = 0; jugador < NumeroJugadores; jugador++)
        {
            var resultado = EvaluarJugador(jugador);

            Debug.Log(
                "JUGADOR " + jugador + "\n" + resultado
            );
        }
    }
#endif



#if UNITY_EDITOR
    [ContextMenu("Prueba - Ganadores de Grande y Chica")]
    private void ProbarGanadores()
    {
        if (string.IsNullOrEmpty(manos[0, 0]))
        {
            Debug.LogWarning("Primero inicia la partida.");
            return;
        }

        var resultados =
            new EvaluadorManoMus.Resultado[NumeroJugadores];

        for (int jugador = 0; jugador < NumeroJugadores; jugador++)
        {
            resultados[jugador] = EvaluarJugador(jugador);
        }

        // De momento el jugador 0 es mano.
        int ganadorGrande =
            ComparadorLancesMus.GanadorGrande(resultados, 0);

        int ganadorChica =
            ComparadorLancesMus.GanadorChica(resultados, 0);

        Debug.Log(
            "GANADOR GRANDE: Jugador " + ganadorGrande
        );

        Debug.Log(
            "GANADOR CHICA: Jugador " + ganadorChica
        );

        Debug.Log(
            "Pareja ganadora Grande: " +
            (ganadorGrande % 2 == 0 ? "0 y 2" : "1 y 3")
        );

        Debug.Log(
            "Pareja ganadora Chica: " +
            (ganadorChica % 2 == 0 ? "0 y 2" : "1 y 3")
        );
    }
#endif


#if UNITY_EDITOR
    [ContextMenu("Prueba - Ganador de Juego o Punto")]
    private void ProbarGanadorJuegoPunto()
    {
        if (string.IsNullOrEmpty(manos[0, 0]))
        {
            Debug.LogWarning("Primero inicia la partida.");
            return;
        }

        var resultados =
            new EvaluadorManoMus.Resultado[NumeroJugadores];

        for (int jugador = 0; jugador < NumeroJugadores; jugador++)
        {
            resultados[jugador] = EvaluarJugador(jugador);
        }

        bool hayJuego;

        int ganador = ComparadorLancesMus.GanadorJuegoPunto(
            resultados,
            out hayJuego,
            0
        );

        string nombreLance = hayJuego ? "JUEGO" : "PUNTO";

        Debug.Log(
            "LANCE ACTIVO: " + nombreLance
        );

        Debug.Log(
            "GANADOR " + nombreLance +
            ": Jugador " + ganador
        );

        Debug.Log(
            "Puntos del ganador: " +
            resultados[ganador].PuntosJuego
        );

        Debug.Log(
            "Pareja ganadora: " +
            (ganador % 2 == 0 ? "0 y 2" : "1 y 3")
        );
    }
#endif

    public string ObtenerCartaJugador(int jugador, int posicion)
    {
        if (jugador < 0 || jugador >= NumeroJugadores ||
            posicion < 0 || posicion >= CartasPorJugador)
        {
            Debug.LogError("Jugador o posicion de carta incorrecta.");
            return null;
        }

        return manos[jugador, posicion];
    }

    [ContextMenu("Prueba - Solo cargar manos fijas")]
    public void PrepararManosFijasPrueba()
    {
        if (!Application.isPlaying)
            return;

        string[,] cartasPrueba =
        {
        {
            "REY\nOROS", "REY\nCOPAS",
            "7\nESPADAS", "7\nBASTOS"
        },
        {
            "AS\nOROS", "AS\nCOPAS",
            "4\nESPADAS", "5\nBASTOS"
        },
        {
            "3\nESPADAS", "SOTA\nCOPAS",
            "CABALLO\nBASTOS", "AS\nESPADAS"
        },
        {
"REY\nESPADAS", "6\nCOPAS",
"6\nBASTOS", "SOTA\nOROS"
        }
    };

        for (int jugador = 0; jugador < 4; jugador++)
        {
            for (int posicion = 0; posicion < 4; posicion++)
            {
                manos[jugador, posicion] =
                    cartasPrueba[jugador, posicion];
            }
        }

        ActualizarManoLocal();
        VerificarReparto();

        Debug.Log("PRUEBA: cuatro manos fijas preparadas.");
    }



}
