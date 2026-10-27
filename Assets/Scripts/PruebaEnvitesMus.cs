
using UnityEngine;

public class PruebaEnvitesMus : MonoBehaviour
{
    // ==========================================
    // PRUEBA 1 - ENVITE ACEPTADO
    // ==========================================

    [ContextMenu("Prueba - Envite aceptado")]
    private void ProbarEnviteAceptado()
    {
        MotorEnvitesMus motor = new MotorEnvitesMus();

        motor.Envidar(0, 2);
        motor.Quiero(1);

        Debug.Log(
            "ENVITE ACEPTADO\n" +
            "Estado: " + motor.Estado + "\n" +
            "Tantos pendientes: " + motor.TantosApostados
        );
    }

    // ==========================================
    // PRUEBA 2 - ENVITE RECHAZADO
    // ==========================================

    [ContextMenu("Prueba - Envite rechazado")]
    private void ProbarEnviteRechazado()
    {
        MotorEnvitesMus motor = new MotorEnvitesMus();

        motor.Envidar(0, 2);
        motor.NoQuiero(1);

        Debug.Log(
            "ENVITE RECHAZADO\n" +
            "Estado: " + motor.Estado + "\n" +
            "Pareja ganadora: " + motor.ParejaGanadoraNegada + "\n" +
            "Tantos ganados: " + motor.TantosPorNegada
        );
    }

    // ==========================================
    // PRUEBA 3 - REENVIDOS
    // ==========================================

    [ContextMenu("Prueba - Reenvidos")]
    private void ProbarReenvidos()
    {
        // Reenvido aceptado
        MotorEnvitesMus motor1 = new MotorEnvitesMus();

        bool correcto1 =
            motor1.Envidar(0, 2) &&
            motor1.Reenvidar(1, 2) &&
            motor1.Quiero(0);

        Debug.Log(
            "REENVIDO ACEPTADO\n" +
            "Estado: " + motor1.Estado + "\n" +
            "Apuesta total: " + motor1.TantosApostados
        );

        // Reenvido rechazado
        MotorEnvitesMus motor2 = new MotorEnvitesMus();

        bool correcto2 =
            motor2.Envidar(0, 2) &&
            motor2.Reenvidar(1, 2) &&
            motor2.NoQuiero(0);

        Debug.Log(
            "REENVIDO RECHAZADO\n" +
            "Estado: " + motor2.Estado + "\n" +
            "Pareja ganadora: " +
            motor2.ParejaGanadoraNegada + "\n" +
            "Tantos ganados: " +
            motor2.TantosPorNegada
        );

        if (!correcto1 || !correcto2 ||
            motor1.TantosApostados != 4 ||
            motor2.ParejaGanadoraNegada != 1 ||
            motor2.TantosPorNegada != 2)
        {
            Debug.LogError("ERROR en la prueba de reenvidos.");
        }
    }

    // ==========================================
    // PRUEBA 4 - ÓRDAGO ACEPTADO
    // ==========================================

    [ContextMenu("Prueba - Ordago aceptado")]
    private void ProbarOrdagoAceptado()
    {
        MotorEnvitesMus motor = new MotorEnvitesMus();

        bool correcto =
            motor.LanzarOrdago(0) &&
            motor.Quiero(1);

        Debug.Log(
            "ÓRDAGO ACEPTADO\n" +
            "Estado: " + motor.Estado + "\n" +
            "Resolver inmediatamente el lance."
        );

        if (!correcto ||
            motor.Estado !=
            MotorEnvitesMus.EstadoEnvite.OrdagoAceptado)
        {
            Debug.LogError("ERROR: Órdago aceptado.");
        }
    }

    // ==========================================
    // PRUEBA 5 - ÓRDAGO RECHAZADO
    // ==========================================

    [ContextMenu("Prueba - Ordago rechazado")]
    private void ProbarOrdagoRechazado()
    {
        MotorEnvitesMus motor = new MotorEnvitesMus();

        bool correcto =
            motor.LanzarOrdago(0) &&
            motor.NoQuiero(1);

        Debug.Log(
            "ÓRDAGO RECHAZADO\n" +
            "Estado: " + motor.Estado + "\n" +
            "Pareja ganadora: " +
            motor.ParejaGanadoraNegada + "\n" +
            "Tantos ganados: " +
            motor.TantosPorNegada
        );

        if (!correcto || motor.TantosPorNegada != 1)
        {
            Debug.LogError("ERROR: Órdago rechazado.");
        }
    }

    // ==========================================
    // PRUEBA 6 - ÓRDAGO TRAS REENVIDO
    // ==========================================

    [ContextMenu("Prueba - Ordago tras reenvido")]
    private void ProbarOrdagoTrasReenvido()
    {
        MotorEnvitesMus motor = new MotorEnvitesMus();

        bool correcto =
            motor.Envidar(0, 2) &&
            motor.Reenvidar(1, 2) &&
            motor.LanzarOrdago(0) &&
            motor.NoQuiero(1);

        Debug.Log(
            "ÓRDAGO TRAS REENVIDO RECHAZADO\n" +
            "Estado: " + motor.Estado + "\n" +
            "Pareja ganadora: " +
            motor.ParejaGanadoraNegada + "\n" +
            "Tantos ganados: " +
            motor.TantosPorNegada
        );

        if (!correcto || motor.TantosPorNegada != 4)
        {
            Debug.LogError("ERROR: Órdago tras reenvido.");
        }
    }
}
