
using System.Text;
using TMPro;
using UnityEngine;

public class MarcadorMus : MonoBehaviour
{
    private enum TipoResultado
    {
        Paso,
        EnviteAceptado,
        EnviteRechazado
    }

    private class RegistroLance
    {
        public bool registrado;
        public TipoResultado tipo;
        public int tantos;
        public int parejaNegada;
    }
    // La victoria por tantos y la victoria por ordago son diferentes.
    public bool JuegoTerminado => juegoTerminado;
    public bool JuegoTerminadoPorOrdago => juegoTerminado && victoriaPorOrdago;
    public int ParejaGanadoraJuego => parejaGanadoraJuego;
    public string MotivoVictoria => motivoVictoria;


    [Header("Prioridad de desempate")]
    [Range(0, 3)]
    [SerializeField] private int jugadorMano = 0;
    public int JugadorMano => jugadorMano;

    public void EstablecerJugadorMano(int nuevoJugador)
    {
        if (nuevoJugador < 0 || nuevoJugador > 3)
        {
            Debug.LogError("Jugador mano no valido: " + nuevoJugador);
            return;
        }

        jugadorMano = nuevoJugador;
    }
    [Header("Referencias")]
    [SerializeField] private RepartoPrueba reparto;
    [SerializeField] private TMP_Text textoMarcador;

    [Header("Reglas de victoria")]
    [Min(1)]
    [SerializeField] private int limiteTantos = 40;
    public int LimiteTantos => limiteTantos;

    [Header("Marcador")]
    [SerializeField] private int puntos02;
    [SerializeField] private int puntos13;

    // Negadas ya cobradas durante la mano actual.
    // Nos servirán para el resumen final.
    private int negadasMano02;
    private int negadasMano13;

    public int PuntosPareja02 => puntos02;
    public int PuntosPareja13 => puntos13;

    private bool manoContabilizada;
    private bool juegoTerminado;
    private int parejaGanadoraOrdago = -1;
    private int parejaGanadoraJuego = -1;
    private bool victoriaPorOrdago;
    private string motivoVictoria = "";

    // Grande, Chica, Pares, Juego/Punto
    private readonly RegistroLance[] registros =
    {
        new RegistroLance(),
        new RegistroLance(),
        new RegistroLance(),
        new RegistroLance()
    };

    private void Start()
    {
        ActualizarMarcador();
    }

    // =========================================
    // GUARDAR RESULTADO DE CADA ENVITE
    // =========================================

    public void RegistrarLance(
        GestorFasesMus.Fase fase,
        MotorEnvitesMus motor)
    {
        // Si alguien ya ha ganado, no se aceptan nuevos lances.
        if (juegoTerminado || manoContabilizada)
            return;

        int indice = ObtenerIndice(fase);

        if (indice < 0 || motor == null)
            return;

        if (registros[indice].registrado)
        {
            Debug.LogWarning(
                "El lance " + fase + " ya estaba registrado."
            );
            return;
        }

        RegistroLance registro = registros[indice];

        switch (motor.Estado)
        {
            case MotorEnvitesMus.EstadoEnvite.SinApuesta:
                registro.tipo = TipoResultado.Paso;
                registro.tantos = 0;
                break;

            case MotorEnvitesMus.EstadoEnvite.Aceptado:
                registro.tipo = TipoResultado.EnviteAceptado;
                registro.tantos = motor.TantosApostados;
                break;


            case MotorEnvitesMus.EstadoEnvite.Rechazado:

                registro.tipo = TipoResultado.EnviteRechazado;
                registro.tantos = motor.TantosPorNegada;
                registro.parejaNegada =
                    motor.ParejaGanadoraNegada;

                // Cobramos inmediatamente la negada.
                if (registro.parejaNegada == 0)
                {
                    puntos02 += registro.tantos;
                    negadasMano02 += registro.tantos;
                }
                else if (registro.parejaNegada == 1)
                {
                    puntos13 += registro.tantos;
                    negadasMano13 += registro.tantos;
                }
                else
                {
                    Debug.LogError("Pareja de negada incorrecta.");
                    return;
                }

                ActualizarMarcador();

                Debug.Log(
                    "NEGADA COBRADA INMEDIATAMENTE: +" +
                    registro.tantos +
                    " para pareja " + registro.parejaNegada
                );

                // Un deje o una negada pueden dar la victoria
                // antes de completar los cuatro lances.
                ComprobarVictoriaPorPuntos(0, 0, "Negada en " + fase);

                break;


            default:
                Debug.LogWarning(
                    "El envite todavía no está resuelto."
                );
                return;
        }

        registro.registrado = true;

        Debug.Log(
            "RESULTADO GUARDADO: " + fase +
            " | " + registro.tipo +
            " | Tantos: " + registro.tantos
        );
    }

    // =========================================
    // CONTABILIZAR MANO COMPLETA
    // =========================================

    public void ContabilizarManoConEnvites()
    {
        if (manoContabilizada || juegoTerminado)
            return;

        if (reparto == null)
        {
            Debug.LogError("Falta GestorReparto.");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (!registros[i].registrado)
            {
                Debug.LogError(
                    "Falta el resultado del lance " + i
                );
                return;
            }
        }

        EvaluadorManoMus.Resultado[] manos =
            ObtenerResultados();

        if (manos == null)
            return;

        int ganancia02 = 0;
        int ganancia13 = 0;

        StringBuilder detalle = new StringBuilder();

        // Calculamos siguiendo el orden del Mus.
        for (int lance = 0; lance < 4; lance++)
        {
            // El tanteo se detiene en el PRIMER tanto decisivo.
            if (juegoTerminado)
                break;

            bool hayJuego;

            int ganador = ObtenerGanador(
                lance, manos, out hayJuego
            );

            RegistroLance registro = registros[lance];

            string nombre = lance switch
            {
                0 => "GRANDE",
                1 => "CHICA",
                2 => "PARES",
                _ => hayJuego ? "JUEGO" : "PUNTO"
            };

            detalle.AppendLine("----- " + nombre + " -----");

            int parejaGanadora = ganador >= 0
                ? ganador % 2
                : -1;

            // Si se rechaza una apuesta, gana el
            // lance la pareja que realizó el
            // último envite.


            if (registro.tipo ==
                TipoResultado.EnviteRechazado)
            {
                // La negada la cobra quien hizo el envite,
                // pero los tantos propios de Pares y Juego
                // corresponden a quien tenga mejores cartas.
                int parejaQueCobroNegada =
                    registro.parejaNegada;

                detalle.AppendLine(
                    nombre + " - Negada: +" +
                    registro.tantos +
                    " para pareja " +
                    (parejaQueCobroNegada == 0 ? "0-2" : "1-3") +
                    " (cobrada anteriormente)"
                );
            }


            else if (registro.tipo ==
                     TipoResultado.EnviteAceptado)
            {
                // Un envite aceptado lo gana
                // la pareja con mejores cartas.
                if (parejaGanadora >= 0)
                {
                    Sumar(
                        parejaGanadora,
                        registro.tantos,
                        nombre + " - Envite querido",
                        ref ganancia02,
                        ref ganancia13,
                        detalle
                    );
                }
            }
            else
            {
                // Grande y Chica en paso: 1 tanto.
                if (lance <= 1 && parejaGanadora >= 0)
                {
                    Sumar(
                        parejaGanadora,
                        1,
                        nombre + " - En paso",
                        ref ganancia02,
                        ref ganancia13,
                        detalle
                    );
                }
            }

            // Los tantos propios de Pares y Juego
            // se calculan independientemente del
            // importe de los envites.

            if (lance == 2 && parejaGanadora >= 0)
            {
                for (int jugador = 0; jugador < 4; jugador++)
                {
                    if (jugador % 2 != parejaGanadora)
                        continue;

                    int valor = manos[jugador].Pares switch
                    {
                        EvaluadorManoMus.TipoPares.Par => 1,
                        EvaluadorManoMus.TipoPares.Medias => 2,
                        EvaluadorManoMus.TipoPares.Duples => 3,
                        _ => 0
                    };

                    if (valor > 0)
                    {
                        Sumar(
                            parejaGanadora,
                            valor,
                            "Pares del jugador " + jugador,
                            ref ganancia02,
                            ref ganancia13,
                            detalle
                        );
                    }
                }
            }

            if (lance == 3 && parejaGanadora >= 0)
            {
                if (hayJuego)
                {
                    for (int jugador = 0;
                         jugador < 4;
                         jugador++)
                    {
                        if (jugador % 2 != parejaGanadora)
                            continue;

                        if (!manos[jugador].TieneJuego)
                            continue;

                        int valor =
                            manos[jugador].PuntosJuego == 31
                            ? 3 : 2;

                        Sumar(
                            parejaGanadora,
                            valor,
                            "Juego del jugador " + jugador,
                            ref ganancia02,
                            ref ganancia13,
                            detalle
                        );
                    }
                }
                else
                {
                    // Punto concede un tanto propio.
                    Sumar(
                        parejaGanadora,
                        1,
                        "Tanto de Punto",
                        ref ganancia02,
                        ref ganancia13,
                        detalle
                    );
                }
            }
        }

        puntos02 += ganancia02;
        puntos13 += ganancia13;

        manoContabilizada = true;

        Debug.Log(
            "========== TANTEO REAL DE LA MANO ==========\n" +
            detalle +
            "\nGANANCIA EN ESTA MANO:\n" +
"Pareja 0-2: +" + (ganancia02 + negadasMano02) + "\n" +
"Pareja 1-3: +" + (ganancia13 + negadasMano13) +
            "\nMARCADOR ACUMULADO:\n" +
            "Pareja 0-2: " + puntos02 + "\n" +
            "Pareja 1-3: " + puntos13
        );

        ActualizarMarcador();
    }

    // =========================================
    // RESOLVER ÓRDAGO ACEPTADO
    // =========================================

    public int ResolverOrdagoAceptado(
        GestorFasesMus.Fase fase)
    {
        EvaluadorManoMus.Resultado[] manos =
            ObtenerResultados();

        if (manos == null)
            return -1;

        int indice = ObtenerIndice(fase);

        if (indice < 0)
            return -1;

        bool hayJuego;

        int ganador = ObtenerGanador(
            indice, manos, out hayJuego
        );

        if (ganador < 0)
        {
            Debug.LogError(
                "No hay ganador válido para este órdago."
            );
            return -1;
        }

        parejaGanadoraOrdago = ganador % 2;
        parejaGanadoraJuego = parejaGanadoraOrdago;
        juegoTerminado = true;
        victoriaPorOrdago = true;
        motivoVictoria = "Ordago aceptado en " + fase;
        manoContabilizada = true;

        Debug.Log(
            "¡¡ÓRDAGO ACEPTADO!!\n" +
            "Lance: " + fase + "\n" +
            "Jugador ganador: " + ganador + "\n" +
            "Pareja ganadora: " +
            parejaGanadoraOrdago + "\n" +
            "FIN DEL JUEGO"
        );

        ActualizarMarcador();

        return parejaGanadoraOrdago;
    }

    // =========================================
    // PREPARAR OTRA MANO
    // =========================================

    public void PrepararNuevaMano()
    {
        if (juegoTerminado)
        {
            Debug.LogWarning(
                "El juego ha terminado. Usa NUEVA PARTIDA."
            );
            return;
        }

        manoContabilizada = false;
        parejaGanadoraOrdago = -1;

        // No arrastrar negadas de la mano anterior al nuevo resumen.
        negadasMano02 = 0;
        negadasMano13 = 0;

        foreach (RegistroLance registro in registros)
        {
            registro.registrado = false;
            registro.tantos = 0;
            registro.parejaNegada = -1;
        }

        ActualizarMarcador();
    }

    // Reinicia el marcador y todos los registros de apuestas.
    // GestorFasesMus se ocupa de repartir y activar mus corrido.
    public void ReiniciarPartidaCompleta()
    {
        puntos02 = 0;
        puntos13 = 0;
        juegoTerminado = false;
        victoriaPorOrdago = false;
        parejaGanadoraJuego = -1;
        parejaGanadoraOrdago = -1;
        motivoVictoria = "";
        jugadorMano = 0;
        PrepararNuevaMano();
        Debug.Log("MARCADOR REINICIADO: 0 - 0.");
    }

    // =========================================
    // MÉTODOS AUXILIARES
    // =========================================

    private EvaluadorManoMus.Resultado[] ObtenerResultados()
    {
        if (reparto == null)
            return null;

        EvaluadorManoMus.Resultado[] manos =
            new EvaluadorManoMus.Resultado[4];

        for (int i = 0; i < 4; i++)
        {
            manos[i] = reparto.EvaluarJugador(i);

            if (manos[i] == null)
                return null;
        }

        return manos;
    }

    private int ObtenerGanador(
        int lance,
        EvaluadorManoMus.Resultado[] manos,
        out bool hayJuego)
    {
        hayJuego = false;

        switch (lance)
        {
            case 0:
                return ComparadorLancesMus.GanadorGrande(
                    manos, jugadorMano
                );

            case 1:
                return ComparadorLancesMus.GanadorChica(
                    manos, jugadorMano
                );

            case 2:
                return ComparadorLancesMus.GanadorPares(
                    manos, jugadorMano
                );

            case 3:
                return ComparadorLancesMus.GanadorJuegoPunto(
                    manos, out hayJuego, jugadorMano
                );

            default:
                return -1;
        }
    }

    private int ObtenerIndice(GestorFasesMus.Fase fase)
    {
        switch (fase)
        {
            case GestorFasesMus.Fase.Grande: return 0;
            case GestorFasesMus.Fase.Chica: return 1;
            case GestorFasesMus.Fase.Pares: return 2;
            case GestorFasesMus.Fase.JuegoPunto: return 3;
            default: return -1;
        }
    }

    private void Sumar(
        int pareja,
        int puntos,
        string concepto,
        ref int ganancia02,
        ref int ganancia13,
        StringBuilder detalle)
    {
        // No sumar mas premios, ni siquiera del mismo lance,
        // tras alcanzar el limite.
        if (juegoTerminado || puntos <= 0)
            return;

        if (pareja == 0)
            ganancia02 += puntos;
        else if (pareja == 1)
            ganancia13 += puntos;
        else
            return;

        detalle.AppendLine(
            concepto + ": +" + puntos +
            " para pareja " +
            (pareja == 0 ? "0-2" : "1-3")
        );

        // Los acumuladores se aplican al marcador al acabar
        // el tanteo, pero comprobamos el limite TRAS CADA premio.
        ComprobarVictoriaPorPuntos(
            ganancia02, ganancia13, concepto
        );
    }

    // Detecta la victoria usando puntos ya anotados y los
    // que se van sumando ahora, respetando el orden de tanteo.
    private bool ComprobarVictoriaPorPuntos(
        int pendientes02, int pendientes13, string motivo)
    {
        if (juegoTerminado)
            return true;

        int total02 = puntos02 + pendientes02;
        int total13 = puntos13 + pendientes13;

        if (total02 < limiteTantos && total13 < limiteTantos)
            return false;

        // Cada premio se concede a una sola pareja; se acaba
        // inmediatamente, sin seguir concediendo otros premios.
        parejaGanadoraJuego = total02 >= limiteTantos ? 0 : 1;
        juegoTerminado = true;
        victoriaPorOrdago = false;
        motivoVictoria = "Victoria por tantos: " + motivo;

        Debug.Log(
            "¡FIN DEL JUEGO POR TANTOS! Ganadora pareja " +
            (parejaGanadoraJuego == 0 ? "0-2" : "1-3") +
            ". Limite: " + limiteTantos +
            ". Motivo: " + motivo
        );

        ActualizarMarcador();
        return true;
    }

    private void ActualizarMarcador()
    {
        if (textoMarcador == null)
            return;

        if (juegoTerminado)
        {
            string ganador = parejaGanadoraJuego == 0
                ? "¡TU PAREJA GANA EL JUEGO!"
                : "¡LOS RIVALES GANAN EL JUEGO!";

            textoMarcador.text = ganador +
                (victoriaPorOrdago ? " (ÓRDAGO)" : "") +
                "\nTU PAREJA: " + puntos02 +
                "  |  RIVALES: " + puntos13;
            return;
        }

        textoMarcador.text =
            "TU PAREJA: " + puntos02 +
            "\nRIVALES: " + puntos13;
    }


    public string ObtenerEnvitesPendientes()
    {
        if (manoContabilizada || juegoTerminado)
            return "";

        string[] nombres =
        {
        "Grande",
        "Chica",
        "Pares",
        "Juego/Punto"
    };

        StringBuilder texto = new StringBuilder();

        for (int i = 0; i < registros.Length; i++)
        {
            RegistroLance registro = registros[i];

            if (!registro.registrado ||
                registro.tipo != TipoResultado.EnviteAceptado)
            {
                continue;
            }

            texto.AppendLine(
                nombres[i] + ": " +
                registro.tantos + " tantos"
            );
        }

        return texto.ToString().TrimEnd();
    }

}


