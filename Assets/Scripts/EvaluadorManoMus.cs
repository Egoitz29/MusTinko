
using System;

public static class EvaluadorManoMus
{
    public enum TipoPares
    {
        SinPares,
        Par,
        Medias,
        Duples
    }

    private static readonly string[] nombres =
    {
        "", "AS", "4", "5", "6", "7",
        "SOTA", "CABALLO", "REY"
    };

    private static readonly string[] nombresPares =
    {
        "", "ases", "cuatros", "cincos", "seises",
        "sietes", "sotas", "caballos", "reyes"
    };

    public class Resultado
    {
        public int[] RangosOrdenados { get; }
        public TipoPares Pares { get; }
        public string DetallePares { get; }
        public int PuntosJuego { get; }

        public bool TieneJuego => PuntosJuego >= 31;

        public Resultado(
            int[] rangos,
            TipoPares pares,
            string detalle,
            int puntos)
        {
            RangosOrdenados = rangos;
            Pares = pares;
            DetallePares = detalle;
            PuntosJuego = puntos;
        }

        public override string ToString()
        {
            string[] grande = new string[4];
            string[] chica = new string[4];

            for (int i = 0; i < 4; i++)
            {
                grande[i] = nombres[RangosOrdenados[3 - i]];
                chica[i] = nombres[RangosOrdenados[i]];
            }

            return
                "GRANDE: " + string.Join(", ", grande) +
                "\nCHICA: " + string.Join(", ", chica) +
                "\nPARES: " + DetallePares +
                "\n" + (TieneJuego ? "JUEGO: " : "PUNTO: ") +
                PuntosJuego;
        }
    }

    public static Resultado Evaluar(string[] mano)
    {
        if (mano == null || mano.Length != 4)
            throw new ArgumentException(
                "Una mano debe tener exactamente 4 cartas."
            );

        int[] rangos = new int[4];
        int[] cantidades = new int[9];
        int puntos = 0;

        for (int i = 0; i < 4; i++)
        {
            if (string.IsNullOrWhiteSpace(mano[i]))
                throw new ArgumentException("Carta vacía.");

            // Formato actual: "REY\nOROS"
            string valor = mano[i]
                .Split('\n')[0]
                .Trim()
                .ToUpperInvariant();

            int rango = ObtenerRango(valor);

            rangos[i] = rango;
            cantidades[rango]++;

            // As y 2 valen 1.
            // Las figuras y el 3 valen 10.
            if (rango == 1)
                puntos += 1;
            else if (rango <= 5)
                puntos += rango + 2;
            else
                puntos += 10;
        }

        Array.Sort(rangos);

        int primerPar = 0;
        int segundoPar = 0;
        int trio = 0;
        int cuatroIguales = 0;

        for (int rango = 1; rango <= 8; rango++)
        {
            switch (cantidades[rango])
            {
                case 2:
                    if (primerPar == 0)
                        primerPar = rango;
                    else
                        segundoPar = rango;
                    break;

                case 3:
                    trio = rango;
                    break;

                case 4:
                    cuatroIguales = rango;
                    break;
            }
        }

        TipoPares tipo;
        string detalle;

        if (cuatroIguales != 0)
        {
            tipo = TipoPares.Duples;
            detalle = "Duples (4 " +
                nombresPares[cuatroIguales] + ")";
        }
        else if (segundoPar != 0)
        {
            tipo = TipoPares.Duples;
            detalle = "Duples de " +
                nombresPares[primerPar] + " y " +
                nombresPares[segundoPar];
        }
        else if (trio != 0)
        {
            tipo = TipoPares.Medias;
            detalle = "Medias de " + nombresPares[trio];
        }
        else if (primerPar != 0)
        {
            tipo = TipoPares.Par;
            detalle = "Par de " + nombresPares[primerPar];
        }
        else
        {
            tipo = TipoPares.SinPares;
            detalle = "Sin pares";
        }

        return new Resultado(rangos, tipo, detalle, puntos);
    }

    private static int ObtenerRango(string valor)
    {
        switch (valor)
        {
            case "AS":
            case "2":
                return 1;

            case "4": return 2;
            case "5": return 3;
            case "6": return 4;
            case "7": return 5;

            case "SOTA": return 6;
            case "CABALLO": return 7;

            case "REY":
            case "3":
                return 8;

            default:
                throw new ArgumentException(
                    "Valor de carta desconocido: " + valor
                );
        }
    }
}
