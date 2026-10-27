using UnityEngine;

// Pruebas de la logica SIN escena, cartas ni marcador.
// GameObject vacio -> Add Component -> menu (tres puntos) -> ejecutar.
public class PruebasTurnosIndividualesMus : MonoBehaviour
{
    private int correctas;
    private int fallidas;

    [ContextMenu("Prueba - Diez escenarios de envites individuales")]
    private void EjecutarPruebas()
    {
        correctas = 0;
        fallidas = 0;
        bool[] todos = { true, true, true, true };
        MotorTurnosIndividualesMus t;

        // 1. Todos pasan, empezando por 0.
        t = new MotorTurnosIndividualesMus();
        t.Iniciar(0, todos);
        bool p1 = t.JugadorActual == 0 && t.Pasar(0) &&
                  t.JugadorActual == 1 && t.Pasar(1) &&
                  t.JugadorActual == 2 && t.Pasar(2) &&
                  t.JugadorActual == 3 && t.Pasar(3) &&
                  t.Cerrado &&
                  t.Motor.Estado == MotorEnvitesMus.EstadoEnvite.SinApuesta;
        Comprobar("0: pasan los cuatro", p1);

        // 2. Rotacion: mano 1 -> 2 -> 3 -> 0.
        t = new MotorTurnosIndividualesMus();
        t.Iniciar(1, todos);
        bool p2 = t.JugadorActual == 1 && t.Pasar(1) &&
                  t.JugadorActual == 2 && t.Pasar(2) &&
                  t.JugadorActual == 3 && t.Pasar(3) &&
                  t.JugadorActual == 0 && t.Pasar(0) && t.Cerrado;
        Comprobar("1: la mano 1 abre el lance", p2);

        // 3. No quiero de uno NO impide que su pareja diga quiero.
        t = new MotorTurnosIndividualesMus();
        t.Iniciar(0, todos);
        bool p3 = t.Envidar(0) && t.JugadorActual == 1 &&
                  t.NoQuiero(1) && !t.Cerrado &&
                  t.JugadorActual == 3 && t.Quiero(3) && t.Cerrado &&
                  t.Motor.Estado == MotorEnvitesMus.EstadoEnvite.Aceptado &&
                  t.Motor.TantosApostados == 2;
        Comprobar("2: rival no quiere, su pareja si", p3);

        // 4. Ambos rivales rechazan -> se cobra 1 negada.
        t = new MotorTurnosIndividualesMus();
        t.Iniciar(0, todos);
        bool p4 = t.Envidar(0) && t.NoQuiero(1) &&
                  t.NoQuiero(3) && t.Cerrado &&
                  t.Motor.Estado == MotorEnvitesMus.EstadoEnvite.Rechazado &&
                  t.Motor.TantosPorNegada == 1 &&
                  t.Motor.ParejaGanadoraNegada == 0;
        Comprobar("3: doble rechazo y negada", p4);

        // 5. Reenvido: respuesta pasa de la pareja 1 a la 0.
        t = new MotorTurnosIndividualesMus();
        t.Iniciar(0, todos);
        bool p5 = t.Envidar(0) && t.Reenvidar(1) &&
                  t.JugadorActual == 2 && t.NoQuiero(2) &&
                  t.JugadorActual == 0 && t.Quiero(0) && t.Cerrado &&
                  t.Motor.TantosApostados == 4 &&
                  t.Motor.Estado == MotorEnvitesMus.EstadoEnvite.Aceptado;
        Comprobar("4: reenvido y replica del companero", p5);

        // 6. Pares: solo pueden apostar los jugadores 1 y 2.
        t = new MotorTurnosIndividualesMus();
        bool[] soloPares = { false, true, true, false };
        t.Iniciar(0, soloPares);
        bool p6 = t.JugadorActual == 1 && t.Pasar(1) &&
                  t.JugadorActual == 2 && t.Envidar(2) &&
                  t.JugadorActual == 1 && t.Quiero(1) && t.Cerrado;
        Comprobar("5: jugadores sin pares no intervienen", p6);

        // 7. Juego: si solo un jugador rival puede hablar, su
        // NO QUIERO basta para rechazar el envite.
        t = new MotorTurnosIndividualesMus();
        bool[] soloJuego = { true, false, false, true };
        t.Iniciar(1, soloJuego);
        bool p7 = t.JugadorActual == 3 && t.Envidar(3) &&
                  t.JugadorActual == 0 && t.NoQuiero(0) &&
                  t.Cerrado &&
                  t.Motor.Estado == MotorEnvitesMus.EstadoEnvite.Rechazado;
        Comprobar("6: juego con un respondedor habilitado", p7);

        // 8. Órdago aceptado.
        t = new MotorTurnosIndividualesMus();
        t.Iniciar(0, todos);
        bool p8 = t.LanzarOrdago(0) && t.Quiero(1) && t.Cerrado &&
                  t.Motor.Estado == MotorEnvitesMus.EstadoEnvite.OrdagoAceptado;
        Comprobar("7: ordago aceptado", p8);

        // 9. No permite actuar fuera de turno.
        t = new MotorTurnosIndividualesMus();
        t.Iniciar(1, todos);
        bool p9 = !t.Envidar(0) && !t.Pasar(2) &&
                  t.JugadorActual == 1 &&
                  t.Motor.Estado == MotorEnvitesMus.EstadoEnvite.SinApuesta;
        Comprobar("8: impedir accion fuera de turno", p9);

        // 10. Rechazar un reenvido cobra la oferta anterior.
        t = new MotorTurnosIndividualesMus();
        t.Iniciar(0, todos);
        bool p10 = t.Envidar(0) && t.Reenvidar(1) &&
                   t.NoQuiero(2) && t.NoQuiero(0) && t.Cerrado &&
                   t.Motor.Estado == MotorEnvitesMus.EstadoEnvite.Rechazado &&
                   t.Motor.TantosPorNegada == 2 &&
                   t.Motor.ParejaGanadoraNegada == 1;
        Comprobar("9: doble rechazo de un reenvido", p10);

        if (fallidas == 0)
            Debug.Log("TURNOS INDIVIDUALES: 10/10 PRUEBAS SUPERADAS.");
        else
            Debug.LogError("TURNOS INDIVIDUALES: " + correctas +
                           "/10 correctas, " + fallidas + " fallidas.");
    }

    private void Comprobar(string nombre, bool supera)
    {
        if (supera)
        {
            correctas++;
            Debug.Log("CORRECTO - " + nombre);
        }
        else
        {
            fallidas++;
            Debug.LogError("FALLO - " + nombre);
        }
    }
}
