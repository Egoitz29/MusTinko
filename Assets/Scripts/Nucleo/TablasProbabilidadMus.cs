using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

// Probabilidad de que TU PAREJA gane cada lance con tu mano, calculada simulando millones de repartos.
//
// En el Mus el palo no importa y el 3 cuenta como rey y el 2 como as, asi que solo hay 330 manos distintas
// (combinaciones de 4 cartas entre 8 rangos). Para cada una y para cada posicion respecto a la mano
// (0 = eres mano ... 3 = eres postre) se reparten muchas veces las otras tres manos con las cartas que quedan
// y se cuenta cuantas veces gana tu pareja (los empates los gana quien esta mas cerca de la mano).
//
// Solo se apuesta a pares o a juego si las dos parejas tienen, asi que esos lances se cuentan solo en los repartos
// donde algun rival tambien tiene, y por separado segun si tu companero tiene o no (eso lo sabes al declarar):
//   0 Grande, 1 Chica,
//   2 Pares (tu companero sin pares), 3 Pares con companero (los dos con pares),
//   4 Juego (tu companero sin juego), 5 Juego con companero,
//   6 Punto (nadie tiene juego).
public static class TablasProbabilidadMus
{
    public const int Manos = 330;
    public const int Posiciones = 4;
    public const int Lances = 7;
    public const int Grande = 0, Chica = 1, Pares = 2, ParesConCompanero = 3, Juego = 4, JuegoConCompanero = 5, Punto = 6;

    private static readonly int[] claveAIndice = new int[8 * 8 * 8 * 8];
    private static readonly int[][] rangosDeIndice = new int[Manos][];
    private static readonly EvaluadorManoMus.Resultado[] evaluacionDeIndice = new EvaluadorManoMus.Resultado[Manos];
    private static readonly List<int>[] cartasDeRango = new List<int>[9];
    private static readonly float[] probabilidad = new float[Manos * Posiciones * Lances];
    private static bool preparado;

    public static bool Cargadas { get; private set; }
    public static long RepartosSimulados { get; private set; }

    private static void Preparar()
    {
        if (preparado)
            return;

        for (int r = 1; r <= 8; r++)
            cartasDeRango[r] = new List<int>();

        for (int c = 0; c < CartasMus.TotalCartas; c++)
            cartasDeRango[CartasMus.Rango(c)].Add(c);

        for (int i = 0; i < claveAIndice.Length; i++)
            claveAIndice[i] = -1;

        int n = 0;

        for (int a = 1; a <= 8; a++)
            for (int b = a; b <= 8; b++)
                for (int c = b; c <= 8; c++)
                    for (int d = c; d <= 8; d++)
                    {
                        int[] r = { a, b, c, d };
                        claveAIndice[Clave(r)] = n;
                        rangosDeIndice[n] = r;

                        // Una mano cualquiera con esos rangos (el palo no importa).
                        int[] usadas = new int[9];
                        int[] cartas = new int[4];

                        for (int k = 0; k < 4; k++)
                            cartas[k] = cartasDeRango[r[k]][usadas[r[k]]++];

                        evaluacionDeIndice[n] = CartasMus.Evaluar(cartas);
                        n++;
                    }

        preparado = true;
    }

    private static int Clave(int[] rangosOrdenados) =>
        (rangosOrdenados[0] - 1) * 512 + (rangosOrdenados[1] - 1) * 64 + (rangosOrdenados[2] - 1) * 8 + (rangosOrdenados[3] - 1);

    public static int Indice(IReadOnlyList<int> cartas)
    {
        Preparar();
        int[] r = { CartasMus.Rango(cartas[0]), CartasMus.Rango(cartas[1]), CartasMus.Rango(cartas[2]), CartasMus.Rango(cartas[3]) };
        Array.Sort(r);
        return claveAIndice[Clave(r)];
    }

    public static float Probabilidad(IReadOnlyList<int> cartas, int posicion, int lance)
    {
        if (!Cargadas)
            return 0.5f;

        return probabilidad[(Indice(cartas) * Posiciones + Math.Max(0, Math.Min(3, posicion))) * Lances + lance];
    }

    // Para las pruebas: vuelve a dejar el bot sin tablas.
    public static void Olvidar() => Cargadas = false;

    public static bool TienePares(int indice) { Preparar(); return evaluacionDeIndice[indice].Pares != EvaluadorManoMus.TipoPares.SinPares; }
    public static bool TieneJuego(int indice) { Preparar(); return evaluacionDeIndice[indice].TieneJuego; }

    // ---------------------------------------------------------------- generar (simulando repartos)

    public static void Generar(int muestrasPorCaso, int semilla, Action<string> log)
    {
        Preparar();
        long total = 0;
        object cerrojo = new();

        Parallel.For(0, Manos, indice =>
        {
            Random azar = new(semilla + indice * 7919);
            int[] rangos = rangosDeIndice[indice];
            int[] usadas = new int[9];
            int[] mias = new int[4];

            for (int k = 0; k < 4; k++)
                mias[k] = cartasDeRango[rangos[k]][usadas[rangos[k]]++];

            List<int> resto = new();

            for (int c = 0; c < CartasMus.TotalCartas; c++)
                if (Array.IndexOf(mias, c) < 0)
                    resto.Add(c);

            int[] mazo = resto.ToArray();
            EvaluadorManoMus.Resultado mia = evaluacionDeIndice[indice];
            EvaluadorManoMus.Resultado[] ev = new EvaluadorManoMus.Resultado[4];
            int[] otra = new int[4];
            long local = 0;

            for (int pos = 0; pos < Posiciones; pos++)
            {
                int[] ganados = new int[Lances];
                int[] casos = new int[Lances];

                for (int m = 0; m < muestrasPorCaso; m++)
                {
                    // Se barajan solo las 12 cartas que hacen falta.
                    for (int k = 0; k < 12; k++)
                    {
                        int j = k + azar.Next(mazo.Length - k);
                        (mazo[k], mazo[j]) = (mazo[j], mazo[k]);
                    }

                    int siguiente = 0;

                    for (int jugador = 0; jugador < 4; jugador++)
                    {
                        if (jugador == pos)
                        {
                            ev[jugador] = mia;
                            continue;
                        }

                        for (int k = 0; k < 4; k++)
                            otra[k] = mazo[siguiente++];

                        ev[jugador] = evaluacionDeIndice[Indice(otra)];
                    }

                    int pareja = pos % 2;
                    int companero = (pos + 2) % 4;
                    // El jugador 0 es la mano (los empates van para el mas cercano a la mano).
                    Contar(ganados, casos, Grande, ComparadorLancesMus.GanadorGrande(ev, 0), pareja);
                    Contar(ganados, casos, Chica, ComparadorLancesMus.GanadorChica(ev, 0), pareja);

                    if (mia.Pares != EvaluadorManoMus.TipoPares.SinPares && RivalCon(ev, pareja, true))
                    {
                        bool conCompanero = ev[companero].Pares != EvaluadorManoMus.TipoPares.SinPares;
                        Contar(ganados, casos, conCompanero ? ParesConCompanero : Pares, ComparadorLancesMus.GanadorPares(ev, 0), pareja);
                    }

                    int ganadorJuego = ComparadorLancesMus.GanadorJuegoPunto(ev, out bool hayJuego, 0);

                    if (mia.TieneJuego)
                    {
                        if (RivalCon(ev, pareja, false))
                            Contar(ganados, casos, ev[companero].TieneJuego ? JuegoConCompanero : Juego, ganadorJuego, pareja);
                    }
                    else if (!hayJuego)
                        Contar(ganados, casos, Punto, ganadorJuego, pareja);
                }

                for (int l = 0; l < Lances; l++)
                    probabilidad[(indice * Posiciones + pos) * Lances + l] = casos[l] == 0 ? 0f : (float)ganados[l] / casos[l];

                local += muestrasPorCaso;
            }

            lock (cerrojo)
                total += local;
        });

        RepartosSimulados = total;
        Cargadas = true;
        log?.Invoke("Tablas de probabilidad: " + total.ToString("N0", CultureInfo.InvariantCulture) + " repartos simulados (" +
                    Manos + " manos x " + Posiciones + " posiciones).");
    }

    private static bool RivalCon(EvaluadorManoMus.Resultado[] ev, int pareja, bool pares)
    {
        for (int j = 0; j < 4; j++)
            if (j % 2 != pareja && (pares ? ev[j].Pares != EvaluadorManoMus.TipoPares.SinPares : ev[j].TieneJuego))
                return true;

        return false;
    }

    private static void Contar(int[] ganados, int[] casos, int lance, int ganador, int pareja)
    {
        if (ganador < 0)
            return;

        casos[lance]++;

        if (ganador % 2 == pareja)
            ganados[lance]++;
    }

    // ---------------------------------------------------------------- guardar / cargar (TextAsset en Unity)

    public static string Guardar()
    {
        Preparar();
        StringBuilder sb = new();
        sb.AppendLine("# Mus TINKO - probabilidad de que tu pareja gane cada lance. " + RepartosSimulados + " repartos.");
        sb.AppendLine("# rangos;posicion;grande;chica;pares;pares_con_companero;juego;juego_con_companero;punto");

        for (int i = 0; i < Manos; i++)
            for (int pos = 0; pos < Posiciones; pos++)
            {
                sb.Append(string.Join("", rangosDeIndice[i])).Append(';').Append(pos);

                for (int l = 0; l < Lances; l++)
                    sb.Append(';').Append(probabilidad[(i * Posiciones + pos) * Lances + l].ToString("0.0000", CultureInfo.InvariantCulture));

                sb.AppendLine();
            }

        return sb.ToString();
    }

    public static bool Cargar(string texto)
    {
        Preparar();

        if (string.IsNullOrEmpty(texto))
            return false;

        int leidas = 0;

        foreach (string linea in texto.Split('\n'))
        {
            string l = linea.Trim();

            if (l.Length == 0 || l[0] == '#')
                continue;

            string[] p = l.Split(';');

            if (p.Length != 2 + Lances || p[0].Length != 4)
                continue;

            int[] r = { p[0][0] - '0', p[0][1] - '0', p[0][2] - '0', p[0][3] - '0' };
            int indice = claveAIndice[Clave(r)];
            int pos = int.Parse(p[1], CultureInfo.InvariantCulture);

            for (int k = 0; k < Lances; k++)
                probabilidad[(indice * Posiciones + pos) * Lances + k] = float.Parse(p[2 + k], CultureInfo.InvariantCulture);

            leidas++;
        }

        Cargadas = leidas == Manos * Posiciones;
        return Cargadas;
    }
}
