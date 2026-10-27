
using System;

public static class ComparadorLancesMus
{
    // Determina quién gana Grande.
    public static int GanadorGrande(
        EvaluadorManoMus.Resultado[] resultados,
        int jugadorMano = 0)
    {
        return BuscarGanador(
            resultados, jugadorMano, CompararGrande
        );
    }

    // Determina quién gana Chica.
    public static int GanadorChica(
        EvaluadorManoMus.Resultado[] resultados,
        int jugadorMano = 0)
    {
        return BuscarGanador(
            resultados, jugadorMano, CompararChica
        );
    }

    private static int CompararGrande(
        EvaluadorManoMus.Resultado a,
        EvaluadorManoMus.Resultado b)
    {
        // En Grande gana la carta de mayor valor.
        // En caso de igualdad, comparamos la siguiente.
        for (int i = 3; i >= 0; i--)
        {
            int diferencia =
                a.RangosOrdenados[i].CompareTo(
                    b.RangosOrdenados[i]);

            if (diferencia != 0)
                return diferencia;
        }

        return 0;
    }

    private static int CompararChica(
        EvaluadorManoMus.Resultado a,
        EvaluadorManoMus.Resultado b)
    {
        // En Chica gana el valor más pequeño.
        for (int i = 0; i < 4; i++)
        {
            int valorA = a.RangosOrdenados[i];
            int valorB = b.RangosOrdenados[i];

            if (valorA < valorB)
                return 1;

            if (valorA > valorB)
                return -1;
        }

        return 0;
    }

    private static int BuscarGanador(
        EvaluadorManoMus.Resultado[] resultados,
        int jugadorMano,
        Func<
            EvaluadorManoMus.Resultado,
            EvaluadorManoMus.Resultado,
            int
        > comparar)
    {
        if (resultados == null || resultados.Length != 4)
            throw new ArgumentException(
                "Se necesitan las evaluaciones de 4 jugadores."
            );

        if (jugadorMano < 0 || jugadorMano >= 4)
            throw new ArgumentException(
                "El jugador mano debe estar entre 0 y 3."
            );

        foreach (var resultado in resultados)
        {
            if (resultado == null)
                throw new ArgumentException(
                    "Falta una evaluación de jugador."
                );
        }

        int ganador = jugadorMano;

        // Recorremos los jugadores en orden desde la mano.
        for (int paso = 1; paso < 4; paso++)
        {
            int jugador = (jugadorMano + paso) % 4;

            if (comparar(
                resultados[jugador],
                resultados[ganador]) > 0)
            {
                ganador = jugador;
            }
        }

        // En caso de empate, conserva prioridad
        // quien está antes en el orden de mano.
        return ganador;
    }


    public static int GanadorPares(
        EvaluadorManoMus.Resultado[] resultados,
        int jugadorMano = 0)
    {
        int ganador = BuscarGanador(
            resultados,
            jugadorMano,
            CompararPares
        );

        // Si nadie tiene pares, no hay ganador.
        if (resultados[ganador].Pares ==
            EvaluadorManoMus.TipoPares.SinPares)
        {
            return -1;
        }

        return ganador;
    }

    private static int CompararPares(
        EvaluadorManoMus.Resultado a,
        EvaluadorManoMus.Resultado b)
    {
        // Primero comparamos la categoría:
        // Duples > Medias > Par > Sin pares.
        int diferencia = a.Pares.CompareTo(b.Pares);

        if (diferencia != 0)
            return diferencia;

        switch (a.Pares)
        {
            case EvaluadorManoMus.TipoPares.Duples:

                // Comparamos primero el par mayor.
                diferencia = a.RangosOrdenados[3].CompareTo(
                    b.RangosOrdenados[3]
                );

                if (diferencia != 0)
                    return diferencia;

                // Si coinciden, comparamos el par menor.
                return a.RangosOrdenados[0].CompareTo(
                    b.RangosOrdenados[0]
                );

            case EvaluadorManoMus.TipoPares.Medias:

                // En unas medias el rango de la
                // posición 1 siempre pertenece al trío.
                return a.RangosOrdenados[1].CompareTo(
                    b.RangosOrdenados[1]
                );

            case EvaluadorManoMus.TipoPares.Par:

                return ObtenerRangoPar(a).CompareTo(
                    ObtenerRangoPar(b)
                );

            default:
                return 0;
        }
    }

    private static int ObtenerRangoPar(
        EvaluadorManoMus.Resultado resultado)
    {
        int[] rangos = resultado.RangosOrdenados;

        for (int i = 1; i < rangos.Length; i++)
        {
            if (rangos[i] == rangos[i - 1])
                return rangos[i];
        }

        return 0;
    }



    public static int GanadorJuegoPunto(
        EvaluadorManoMus.Resultado[] resultados,
        out bool hayJuego,
        int jugadorMano = 0)
    {
        if (resultados == null || resultados.Length != 4)
            throw new ArgumentException(
                "Se necesitan cuatro evaluaciones."
            );

        hayJuego = false;

        // Comprobamos si al menos un jugador tiene Juego.
        foreach (var resultado in resultados)
        {
            if (resultado == null)
                throw new ArgumentException(
                    "Falta una evaluación de jugador."
                );

            if (resultado.TieneJuego)
                hayJuego = true;
        }

        if (hayJuego)
        {
            // En Juego utilizamos el orden especial:
            // 31 > 32 > 40 > 37 > 36 > 35 > 34 > 33.
            return BuscarGanador(
                resultados,
                jugadorMano,
                (a, b) => PrioridadJuego(a.PuntosJuego)
                    .CompareTo(PrioridadJuego(b.PuntosJuego))
            );
        }

        // Si nadie tiene Juego, gana el Punto más alto.
        return BuscarGanador(
            resultados,
            jugadorMano,
            (a, b) => a.PuntosJuego.CompareTo(b.PuntosJuego)
        );
    }

    private static int PrioridadJuego(int puntos)
    {
        switch (puntos)
        {
            case 31: return 8;
            case 32: return 7;
            case 40: return 6;
            case 37: return 5;
            case 36: return 4;
            case 35: return 3;
            case 34: return 2;
            case 33: return 1;

            default: return 0;
        }
    }

}
