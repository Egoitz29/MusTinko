
using System;
using System.Text;

public static class TanteoEnPasoMus
{
    public class Resumen
    {
        public int Pareja02 { get; }
        public int Pareja13 { get; }
        public string Detalle { get; }

        public Resumen(int pareja02, int pareja13, string detalle)
        {
            Pareja02 = pareja02;
            Pareja13 = pareja13;
            Detalle = detalle;
        }
    }

    public static Resumen Calcular(
        EvaluadorManoMus.Resultado[] manos,
        int jugadorMano = 0)
    {
        if (manos == null || manos.Length != 4)
            throw new ArgumentException("Se requieren cuatro manos.");

        int tantos02 = 0;
        int tantos13 = 0;
        StringBuilder detalle = new StringBuilder();

        int grande = ComparadorLancesMus.GanadorGrande(
            manos, jugadorMano);

        int chica = ComparadorLancesMus.GanadorChica(
            manos, jugadorMano);

        int pares = ComparadorLancesMus.GanadorPares(
            manos, jugadorMano);

        bool hayJuego;
        int juegoPunto = ComparadorLancesMus.GanadorJuegoPunto(
            manos, out hayJuego, jugadorMano);

        // Grande y Chica en paso: un tanto cada una.
        Sumar(grande, 1, "Grande",
            ref tantos02, ref tantos13, detalle);

        Sumar(chica, 1, "Chica",
            ref tantos02, ref tantos13, detalle);

        // Pares: cuentan las jugadas de ambos
        // integrantes de la pareja ganadora.
        if (pares >= 0)
        {
            for (int jugador = 0; jugador < 4; jugador++)
            {
                if (jugador % 2 != pares % 2)
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
                    Sumar(pares, valor, "Pares jugador " + jugador,
                        ref tantos02, ref tantos13, detalle);
                }
            }
        }
        else
        {
            detalle.AppendLine("Pares: nadie tiene pares.");
        }

        // Juego: 31 vale tres; los demás juegos, dos.
        // Si no hay juego, Punto vale uno.
        if (hayJuego)
        {
            for (int jugador = 0; jugador < 4; jugador++)
            {
                if (jugador % 2 != juegoPunto % 2)
                    continue;

                if (!manos[jugador].TieneJuego)
                    continue;

                int valor = manos[jugador].PuntosJuego == 31 ? 3 : 2;

                Sumar(juegoPunto, valor, "Juego jugador " + jugador,
                    ref tantos02, ref tantos13, detalle);
            }
        }
        else
        {
            Sumar(juegoPunto, 1, "Punto",
                ref tantos02, ref tantos13, detalle);
        }

        return new Resumen(tantos02, tantos13, detalle.ToString());
    }

    private static void Sumar(
        int ganador,
        int puntos,
        string concepto,
        ref int tantos02,
        ref int tantos13,
        StringBuilder detalle)
    {
        if (ganador % 2 == 0)
            tantos02 += puntos;
        else
            tantos13 += puntos;

        detalle.AppendLine(
            concepto + ": +" + puntos +
            " para pareja " + (ganador % 2 == 0 ? "0-2" : "1-3")
        );
    }
}
