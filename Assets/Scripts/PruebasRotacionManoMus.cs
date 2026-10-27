using UnityEngine;

// Prueba de desempates: los cuatro evaluados tienen
// los mismos valores. Solo debe decidir el jugador MANO.
// No es un reparto fisico: se prueban exclusivamente comparadores.
public class PruebasRotacionManoMus : MonoBehaviour
{
    [ContextMenu("Prueba - Empates con los 4 jugadores mano")]
    private void Ejecutar()
    {
        bool todoCorrecto = true;

        // Grande y Chica: iguales; sin pares y sin juego.
        var iguales = EvaluarCuatro(
            "REY\nOROS", "7\nCOPAS", "6\nBASTOS", "5\nESPADAS");

        // Pares: cuatro manos con el mismo par de reyes.
        var conPares = EvaluarCuatro(
            "REY\nOROS", "REY\nCOPAS", "7\nBASTOS", "5\nESPADAS");

        // Juego: 31 en las cuatro manos.
        var conJuego = EvaluarCuatro(
            "REY\nOROS", "SOTA\nCOPAS", "CABALLO\nBASTOS", "AS\nESPADAS");

        // Punto: sin juego y todos con la misma puntuacion.
        var conPunto = EvaluarCuatro(
            "AS\nOROS", "4\nCOPAS", "5\nBASTOS", "6\nESPADAS");

        for (int mano = 0; mano < 4; mano++)
        {
            bool hayJuego;
            bool correcto =
                ComparadorLancesMus.GanadorGrande(iguales, mano) == mano &&
                ComparadorLancesMus.GanadorChica(iguales, mano) == mano &&
                ComparadorLancesMus.GanadorPares(conPares, mano) == mano &&
                ComparadorLancesMus.GanadorJuegoPunto(
                    conJuego, out hayJuego, mano) == mano &&
                hayJuego;

            bool hayJuegoPunto;
            correcto &= ComparadorLancesMus.GanadorJuegoPunto(
                conPunto, out hayJuegoPunto, mano) == mano &&
                !hayJuegoPunto;

            if (correcto)
                Debug.Log("ROTACION CORRECTA: jugador MANO " + mano);
            else
            {
                Debug.LogError("ERROR DE DESEMPATE: jugador MANO " + mano);
                todoCorrecto = false;
            }
        }

        if (todoCorrecto)
            Debug.Log("ROTACION: 4/4 posiciones superadas.");
    }

    private static EvaluadorManoMus.Resultado[] EvaluarCuatro(
        string c0, string c1, string c2, string c3)
    {
        var resultados = new EvaluadorManoMus.Resultado[4];
        var cartas = new[] { c0, c1, c2, c3 };

        for (int i = 0; i < 4; i++)
            resultados[i] = EvaluadorManoMus.Evaluar(cartas);

        return resultados;
    }
}
