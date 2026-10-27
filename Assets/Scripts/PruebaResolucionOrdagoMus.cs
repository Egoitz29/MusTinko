
using UnityEngine;

public class PruebaResolucionOrdagoMus : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RepartoPrueba reparto;

    [Header("Configuración de prueba")]
    [Range(0, 3)]
    [SerializeField] private int jugadorMano = 0;

    [ContextMenu("Prueba - Ordago a Grande")]
    private void ProbarGrande()
    {
        ResolverOrdago(GestorFasesMus.Fase.Grande);
    }

    [ContextMenu("Prueba - Ordago a Chica")]
    private void ProbarChica()
    {
        ResolverOrdago(GestorFasesMus.Fase.Chica);
    }

    [ContextMenu("Prueba - Ordago a Pares")]
    private void ProbarPares()
    {
        ResolverOrdago(GestorFasesMus.Fase.Pares);
    }

    [ContextMenu("Prueba - Ordago a Juego o Punto")]
    private void ProbarJuegoPunto()
    {
        ResolverOrdago(GestorFasesMus.Fase.JuegoPunto);
    }

    private void ResolverOrdago(GestorFasesMus.Fase fase)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "Inicia Play antes de probar el órdago."
            );
            return;
        }

        if (reparto == null)
        {
            Debug.LogError(
                "Asigna GestorReparto en el Inspector."
            );
            return;
        }

        // Simulamos un órdago de la pareja 0
        // aceptado por la pareja 1.
        MotorEnvitesMus motor = new MotorEnvitesMus();

        bool aceptado =
            motor.LanzarOrdago(0) &&
            motor.Quiero(1);

        if (!aceptado ||
            motor.Estado !=
            MotorEnvitesMus.EstadoEnvite.OrdagoAceptado)
        {
            Debug.LogError("No se pudo aceptar el órdago.");
            return;
        }

        // Evaluamos las cuatro manos actuales.
        EvaluadorManoMus.Resultado[] resultados =
            new EvaluadorManoMus.Resultado[4];

        for (int i = 0; i < 4; i++)
        {
            resultados[i] = reparto.EvaluarJugador(i);

            if (resultados[i] == null)
            {
                Debug.LogError(
                    "No se pudo evaluar el jugador " + i
                );
                return;
            }
        }

        int ganador = -1;
        string nombreLance = fase.ToString();

        switch (fase)
        {
            case GestorFasesMus.Fase.Grande:
                ganador = ComparadorLancesMus.GanadorGrande(
                    resultados, jugadorMano
                );
                nombreLance = "GRANDE";
                break;

            case GestorFasesMus.Fase.Chica:
                ganador = ComparadorLancesMus.GanadorChica(
                    resultados, jugadorMano
                );
                nombreLance = "CHICA";
                break;

            case GestorFasesMus.Fase.Pares:
                ganador = ComparadorLancesMus.GanadorPares(
                    resultados, jugadorMano
                );
                nombreLance = "PARES";
                break;

            case GestorFasesMus.Fase.JuegoPunto:
                bool hayJuego;

                ganador =
                    ComparadorLancesMus.GanadorJuegoPunto(
                        resultados,
                        out hayJuego,
                        jugadorMano
                    );

                nombreLance = hayJuego ? "JUEGO" : "PUNTO";
                break;

            default:
                Debug.LogError("Lance no válido.");
                return;
        }

        if (ganador == -1)
        {
            Debug.Log(
                "ÓRDAGO A " + nombreLance +
                "\nNo hay ganador válido para este lance."
            );
            return;
        }

        int parejaGanadora = ganador % 2;

        string nombrePareja = parejaGanadora == 0
            ? "TU PAREJA (0 Y 2)"
            : "RIVALES (1 Y 3)";

        Debug.Log(
            "========== ÓRDAGO ACEPTADO ==========\n" +
            "LANCE: " + nombreLance + "\n" +
            "JUGADOR GANADOR: " + ganador + "\n" +
            "PAREJA GANADORA: " + nombrePareja + "\n" +
            "===================================="
        );
    }
}
