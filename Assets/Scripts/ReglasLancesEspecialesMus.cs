
using System;

public static class ReglasLancesEspecialesMus
{
    public class Decision
    {
        public bool PermiteEnvites { get; }
        public bool EsPunto { get; }
        public string Mensaje { get; }

        public Decision(
            bool permiteEnvites,
            bool esPunto,
            string mensaje)
        {
            PermiteEnvites = permiteEnvites;
            EsPunto = esPunto;
            Mensaje = mensaje;
        }
    }

    public static Decision Evaluar(
        EvaluadorManoMus.Resultado[] manos,
        GestorFasesMus.Fase fase)
    {
        if (manos == null || manos.Length != 4)
            throw new ArgumentException(
                "Se necesitan cuatro manos."
            );

        foreach (var mano in manos)
        {
            if (mano == null)
                throw new ArgumentException(
                    "Falta la evaluacion de un jugador."
                );
        }

        if (fase == GestorFasesMus.Fase.Pares)
        {
            bool pares02 =
                TienePares(manos[0]) ||
                TienePares(manos[2]);

            bool pares13 =
                TienePares(manos[1]) ||
                TienePares(manos[3]);

            if (!pares02 && !pares13)
                return new Decision(
                    false, false,
                    "NADIE TIENE PARES."
                );

            if (pares02 && !pares13)
                return new Decision(
                    false, false,
                    "SOLO TU PAREJA TIENE PARES."
                );

            if (!pares02 && pares13)
                return new Decision(
                    false, false,
                    "SOLO LOS RIVALES TIENEN PARES."
                );

            return new Decision(
                true, false,
                "AMBAS PAREJAS TIENEN PARES."
            );
        }

        if (fase == GestorFasesMus.Fase.JuegoPunto)
        {
            bool juego02 =
                manos[0].TieneJuego ||
                manos[2].TieneJuego;

            bool juego13 =
                manos[1].TieneJuego ||
                manos[3].TieneJuego;

            if (!juego02 && !juego13)
                return new Decision(
                    true, true,
                    "NADIE TIENE JUEGO. SE DISPUTA PUNTO."
                );

            if (juego02 && !juego13)
                return new Decision(
                    false, false,
                    "SOLO TU PAREJA TIENE JUEGO."
                );

            if (!juego02 && juego13)
                return new Decision(
                    false, false,
                    "SOLO LOS RIVALES TIENEN JUEGO."
                );

            return new Decision(
                true, false,
                "AMBAS PAREJAS TIENEN JUEGO."
            );
        }

        return new Decision(
            true, false,
            "Lance sin restricciones especiales."
        );
    }

    private static bool TienePares(
        EvaluadorManoMus.Resultado mano)
    {
        return mano.Pares !=
            EvaluadorManoMus.TipoPares.SinPares;
    }
}
