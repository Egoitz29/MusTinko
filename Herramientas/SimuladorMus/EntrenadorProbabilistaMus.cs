using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Entrena el bot probabilista con millones de partidas (estrategia evolutiva).
// En cada generacion:
//   1. Se crean varios candidatos moviendo un poco los parametros del bot actual.
//   2. Cada candidato juega contra todos los rivales del grupo con las MISMAS semillas (repartos iguales para todos,
//      asi la comparacion es justa) y se mide su porcentaje de victorias.
//   3. El nuevo bot es la media de los mejores candidatos. El paso se va haciendo mas pequeno.
// Los rivales: el bot de reglas ya entrenado, los tres tipicos y el propio bot probabilista actual.
public class EntrenadorProbabilistaMus
{
    public int Candidatos = 16;
    public int Mejores = 4;
    public int PartidasPorRival = 800;
    public float SigmaInicial = 0.08f;
    public float SigmaFinal = 0.02f;

    public long PartidasJugadas => Interlocked.Read(ref partidasJugadas);
    private long partidasJugadas;

    public class Rival
    {
        public string Nombre;
        public Func<IBotMus> Crear;
    }

    public ParametrosProbabilistaMus Entrenar(ParametrosProbabilistaMus inicial, List<Rival> fijos, int generaciones, int semilla, Action<string> log)
    {
        Random azar = new(semilla);
        float[] media = inicial.AVector();
        int n = media.Length;
        Stopwatch reloj = Stopwatch.StartNew();
        double mejorFitness = -1;
        float[] mejorVector = (float[])media.Clone();

        for (int g = 1; g <= generaciones; g++)
        {
            float t = generaciones <= 1 ? 1f : (g - 1f) / (generaciones - 1f);
            float sigma = SigmaInicial * (float)Math.Pow(SigmaFinal / SigmaInicial, t);

            // Rivales de esta generacion: los fijos + el bot actual (asi tambien aprende a ganarse a si mismo).
            ParametrosProbabilistaMus actual = ParametrosProbabilistaMus.DesdeVector(media);
            List<Rival> rivales = new(fijos) { new Rival { Nombre = "Probabilista actual", Crear = () => new BotProbabilistaMus("Actual", actual) } };

            float[][] vectores = new float[Candidatos][];

            // Muestreo en espejo (+ruido / -ruido): reduce mucho el azar al comparar.
            for (int k = 0; k < Candidatos; k += 2)
            {
                float[] ruido = new float[n];
                for (int i = 0; i < n; i++) ruido[i] = (float)(Normal(azar) * sigma);

                vectores[k] = Sumar(media, ruido, 1f);
                if (k + 1 < Candidatos) vectores[k + 1] = Sumar(media, ruido, -1f);
            }

            int semillaGen = semilla + g * 1000003;
            double[] fitness = new double[Candidatos];

            Parallel.For(0, Candidatos, k =>
            {
                ParametrosProbabilistaMus c = ParametrosProbabilistaMus.DesdeVector(vectores[k]);
                fitness[k] = Evaluar(() => new BotProbabilistaMus("Candidato", c), rivales, PartidasPorRival, semillaGen);
            });

            int[] orden = Enumerable.Range(0, Candidatos).OrderByDescending(k => fitness[k]).ToArray();

            // Media ponderada de los mejores (pesa mas el primero).
            float[] nueva = new float[n];
            double sumaPesos = 0;

            for (int r = 0; r < Mejores; r++)
            {
                double peso = Math.Log(Mejores + 0.5) - Math.Log(r + 1);
                sumaPesos += peso;

                for (int i = 0; i < n; i++)
                    nueva[i] += (float)(peso * vectores[orden[r]][i]);
            }

            for (int i = 0; i < n; i++)
                media[i] = Math.Max(0f, Math.Min(1f, (float)(nueva[i] / sumaPesos)));

            if (fitness[orden[0]] > mejorFitness)
            {
                mejorFitness = fitness[orden[0]];
                mejorVector = (float[])vectores[orden[0]].Clone();
            }

            log?.Invoke("Gen " + g.ToString().PadLeft(3) + "  paso " + sigma.ToString("0.000", CultureInfo.InvariantCulture) +
                        "  mejor " + (fitness[orden[0]] * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%" +
                        "  media " + (fitness.Average() * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%" +
                        "  partidas " + PartidasJugadas.ToString("N0", CultureInfo.InvariantCulture) +
                        "  (" + reloj.Elapsed.TotalSeconds.ToString("0", CultureInfo.InvariantCulture) + " s)");
        }

        // Al final: la media o el mejor candidato visto, el que gane mas con partidas NUEVAS.
        ParametrosProbabilistaMus pm = ParametrosProbabilistaMus.DesdeVector(media);
        ParametrosProbabilistaMus pb = ParametrosProbabilistaMus.DesdeVector(mejorVector);
        double fm = Evaluar(() => new BotProbabilistaMus("Media", pm), fijos, PartidasPorRival * 3, semilla + 77777);
        double fb = Evaluar(() => new BotProbabilistaMus("Mejor", pb), fijos, PartidasPorRival * 3, semilla + 77777);
        log?.Invoke("Eleccion final: media " + (fm * 100).ToString("0.0", CultureInfo.InvariantCulture) + "% / mejor visto " +
                    (fb * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%");
        return fm >= fb ? pm : pb;
    }

    // Porcentaje medio de victorias contra todos los rivales (cambiando de sitio la mitad de las partidas).
    public double Evaluar(Func<IBotMus> crear, List<Rival> rivales, int partidasPorRival, int semilla)
    {
        double suma = 0;

        foreach (Rival r in rivales)
        {
            ComparacionBotsMus c = ComparacionBotsMus.Comparar(crear, r.Crear, partidasPorRival, semilla);
            Interlocked.Add(ref partidasJugadas, c.Partidas);
            suma += c.TasaA;
        }

        return suma / rivales.Count;
    }

    private static float[] Sumar(float[] a, float[] b, float signo)
    {
        float[] r = new float[a.Length];
        for (int i = 0; i < a.Length; i++) r[i] = Math.Max(0f, Math.Min(1f, a[i] + signo * b[i]));
        return r;
    }

    private static double Normal(Random azar)
    {
        double u1 = 1.0 - azar.NextDouble();
        double u2 = azar.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
