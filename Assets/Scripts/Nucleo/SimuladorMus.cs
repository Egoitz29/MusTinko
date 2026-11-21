using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public class ResultadoPartidaMus
{
    public int ParejaGanadora = -1;
    public bool PorOrdago;
    public int Manos;
    public int Puntos0;
    public int Puntos1;
    public int Decisiones;
    public int AccionesInvalidas;
    public int Senales;
    public bool Abortada;
}

public class SimuladorMus
{
    public int Limite = 40;
    public int MaxRondasMus = 4;
    public int MaxDecisiones = 20000;
    public RegistroExperienciasMus Registro;

    public ResultadoPartidaMus JugarPartida(IBotMus[] bots, int semilla, int idPartida = 0)
    {
        if (bots == null || bots.Length != PartidaMus.Jugadores)
            throw new ArgumentException("Se necesitan cuatro bots.");

        Random azar = new(semilla ^ 0x5bd1e995);
        PartidaMus partida = new(semilla, Limite, MaxRondasMus);
        ResultadoPartidaMus resultado = new();
        int manoSenales = -1;

        Registro?.EmpezarPartida(idPartida);
        partida.IniciarPartida();

        while (!partida.PartidaTerminada)
        {
            if (partida.Fase == FaseMus.FinMano)
            {
                Registro?.TerminarMano(partida);
                partida.SiguienteMano();
                continue;
            }

            int momentoSenales = partida.ManosJugadas * 100 + partida.RondaMus * 2 + (PartidaMus.EsLance(partida.Fase) ? 1 : 0);

            if ((partida.Fase == FaseMus.Mus || PartidaMus.EsLance(partida.Fase)) && !partida.MusCorridoPendiente && manoSenales != momentoSenales)
            {
                manoSenales = momentoSenales;

                for (int j = 0; j < PartidaMus.Jugadores; j++)
                    foreach (TipoSenalMus senal in bots[j].ElegirSenales(partida.Observar(j), azar))
                        if (partida.HacerSenal(j, senal))
                            resultado.Senales++;
            }

            int jugador = partida.JugadorActual;
            List<AccionMus> legales = partida.AccionesLegales(jugador);

            if (jugador < 0 || legales.Count == 0)
            {
                resultado.Abortada = true;
                break;
            }

            ObservacionMus obs = partida.Observar(jugador);
            AccionMus accion = bots[jugador].Decidir(obs, legales, azar);

            if (!legales.Contains(accion))
            {
                resultado.AccionesInvalidas++;
                accion = AccionSegura(legales);
            }

            Registro?.RegistrarDecision(obs, bots[jugador].Nombre, legales, accion, partida.ManosJugadas);

            if (!partida.Aplicar(jugador, accion))
            {
                resultado.AccionesInvalidas++;
                AccionMus segura = AccionSegura(legales);

                if (segura.Equals(accion) || !partida.Aplicar(jugador, segura))
                {
                    resultado.Abortada = true;
                    break;
                }
            }

            resultado.Decisiones++;

            if (resultado.Decisiones >= MaxDecisiones)
            {
                resultado.Abortada = true;
                break;
            }
        }

        resultado.ParejaGanadora = partida.ParejaGanadora;
        resultado.PorOrdago = partida.VictoriaPorOrdago;
        resultado.Manos = partida.ManosJugadas;
        resultado.Puntos0 = partida.Puntos(0);
        resultado.Puntos1 = partida.Puntos(1);

        if (partida.PartidaTerminada)
            Registro?.TerminarMano(partida);

        Registro?.TerminarPartida(resultado.Abortada ? -1 : partida.ParejaGanadora);
        return resultado;
    }

    public static AccionMus AccionSegura(IReadOnlyList<AccionMus> legales)
    {
        TipoAccionMus[] preferencia =
        {
            TipoAccionMus.NoHayMus, TipoAccionMus.Pasar, TipoAccionMus.NoQuiero, TipoAccionMus.Descartar
        };

        foreach (TipoAccionMus tipo in preferencia)
            foreach (AccionMus a in legales)
                if (a.Tipo == tipo)
                    return a;

        return legales[0];
    }
}

public class ComparacionBotsMus
{
    public string NombreA;
    public string NombreB;
    public int Partidas;
    public int VictoriasA;
    public int VictoriasB;
    public int Abortadas;
    public int Ordagos;
    public int AccionesInvalidas;
    public long Manos;

    public double TasaA => Partidas - Abortadas == 0 ? 0 : (double)VictoriasA / (Partidas - Abortadas);
    public double MargenError95 => Partidas - Abortadas == 0 ? 0 : 1.96 * Math.Sqrt(TasaA * (1 - TasaA) / (Partidas - Abortadas));
    public bool ADiferenciaSignificativa => TasaA - MargenError95 > 0.5;

    public override string ToString()
    {
        CultureInfo c = CultureInfo.InvariantCulture;
        return NombreA + " vs " + NombreB + ": " +
               (TasaA * 100).ToString("0.0", c) + "% +/- " + (MargenError95 * 100).ToString("0.0", c) +
               "% (" + VictoriasA + "-" + VictoriasB + " en " + Partidas + " partidas" +
               ", manos/partida " + (Partidas == 0 ? 0 : (double)Manos / Partidas).ToString("0.0", c) +
               ", ordagos " + Ordagos + ", invalidas " + AccionesInvalidas + ", abortadas " + Abortadas + ")" +
               (ADiferenciaSignificativa ? " [A MEJOR, significativo]" :
                   TasaA + MargenError95 < 0.5 ? " [B MEJOR, significativo]" : " [sin diferencia clara]");
    }

    public static ComparacionBotsMus Comparar(
        Func<IBotMus> crearA, Func<IBotMus> crearB, int partidas, int semilla,
        SimuladorMus simulador = null)
    {
        simulador ??= new SimuladorMus();
        ComparacionBotsMus r = new() { NombreA = crearA().Nombre, NombreB = crearB().Nombre };

        for (int i = 0; i < partidas; i++)
        {
            bool aEnPares = i % 2 == 0;
            int semillaPartida = semilla + i / 2;
            IBotMus[] bots = aEnPares
                ? new[] { crearA(), crearB(), crearA(), crearB() }
                : new[] { crearB(), crearA(), crearB(), crearA() };

            ResultadoPartidaMus res = simulador.JugarPartida(bots, semillaPartida, i);
            int parejaA = aEnPares ? 0 : 1;

            r.Partidas++;
            r.Manos += res.Manos;
            r.AccionesInvalidas += res.AccionesInvalidas;

            if (res.PorOrdago) r.Ordagos++;

            if (res.Abortada || res.ParejaGanadora < 0) r.Abortadas++;
            else if (res.ParejaGanadora == parejaA) r.VictoriasA++;
            else r.VictoriasB++;
        }

        return r;
    }
}

public class EntrenadorBotsMus
{
    public int PartidasPorEvaluacion = 400;
    public float Sigma = 0.08f;
    public double TasaMinimaParaAceptar = 0.53;

    public ParametrosBotMus Entrenar(ParametrosBotMus inicial, int generaciones, int semilla, Action<string> log)
    {
        Random azar = new(semilla);
        ParametrosBotMus mejor = inicial.Copiar();
        int aceptadas = 0;

        for (int g = 1; g <= generaciones; g++)
        {
            float[] v = mejor.AVector();

            for (int i = 0; i < v.Length; i++)
                v[i] += (float)(Normal(azar) * Sigma);

            ParametrosBotMus candidato = ParametrosBotMus.DesdeVector(v, mejor.UsaSenales);
            ParametrosBotMus actual = mejor;

            ComparacionBotsMus c = ComparacionBotsMus.Comparar(
                () => new BotReglasMus("Candidato", candidato),
                () => new BotReglasMus("Actual", actual),
                PartidasPorEvaluacion, semilla + g * 100003);

            bool acepta = c.TasaA >= TasaMinimaParaAceptar;

            if (acepta)
            {
                mejor = candidato;
                aceptadas++;
            }

            log?.Invoke("Generacion " + g + ": " + c + (acepta ? " -> ACEPTADO" : " -> descartado"));
        }

        log?.Invoke("Mejoras aceptadas: " + aceptadas + "/" + generaciones);
        log?.Invoke("Parametros finales: " + mejor);
        return mejor;
    }

    private static double Normal(Random azar)
    {
        double u1 = 1.0 - azar.NextDouble();
        double u2 = azar.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    public static string Guardar(ParametrosBotMus p)
    {
        StringBuilder sb = new();
        float[] v = p.AVector();

        for (int i = 0; i < v.Length; i++)
        {
            if (i > 0) sb.Append(';');
            sb.Append(v[i].ToString("R", CultureInfo.InvariantCulture));
        }

        sb.Append(';').Append(p.UsaSenales ? "1" : "0");
        return sb.ToString();
    }

    public static ParametrosBotMus Cargar(string texto)
    {
        string[] partes = texto.Trim().Split(';');

        if (partes.Length < ParametrosBotMus.NumeroParametros)
            throw new FormatException("Parametros incompletos.");

        float[] v = new float[ParametrosBotMus.NumeroParametros];

        for (int i = 0; i < v.Length; i++)
            v[i] = float.Parse(partes[i], CultureInfo.InvariantCulture);

        bool senales = partes.Length <= v.Length || partes[v.Length] == "1";
        return ParametrosBotMus.DesdeVector(v, senales);
    }
}

public class RegistroExperienciasMus : IDisposable
{
    private class Decision
    {
        public StringBuilder Json;
        public int Pareja;
        public int Mano;
        public int RecompensaMano;
    }

    private readonly TextWriter salida;
    private readonly List<Decision> partidaActual = new();
    private int idPartida;

    public int LineasEscritas { get; private set; }

    public RegistroExperienciasMus(TextWriter salida)
    {
        this.salida = salida;
    }

    public void EmpezarPartida(int id)
    {
        idPartida = id;
        partidaActual.Clear();
    }

    public void RegistrarDecision(ObservacionMus obs, string bot, IReadOnlyList<AccionMus> legales, AccionMus accion, int mano)
    {
        StringBuilder sb = new();
        sb.Append('{');
        Campo(sb, "partida", idPartida);
        Campo(sb, "mano", mano);
        Campo(sb, "jugador", obs.Jugador);
        Campo(sb, "bot", bot);
        Campo(sb, "pareja", obs.Pareja);
        Campo(sb, "fase", obs.Fase.ToString());
        Campo(sb, "posicion", obs.PosicionRespectoMano);
        Campo(sb, "musCorrido", obs.MusCorrido);
        Campo(sb, "rondaMus", obs.RondaMus);
        Lista(sb, "cartas", obs.Cartas);
        Campo(sb, "cartasTexto", string.Join(" ", Array.ConvertAll(obs.Cartas, CartasMus.TextoCorto)));
        Lista(sb, "rangos", obs.Evaluacion.RangosOrdenados);
        Campo(sb, "pares", obs.Evaluacion.Pares.ToString());
        Campo(sb, "puntosJuego", obs.Evaluacion.PuntosJuego);
        Campo(sb, "puntosPropios", obs.PuntosPropios);
        Campo(sb, "puntosRivales", obs.PuntosRivales);
        Campo(sb, "estadoEnvite", obs.EstadoEnvite.ToString());
        Campo(sb, "tantosApostados", obs.TantosApostados);
        Campo(sb, "envidaPropia", obs.ParejaEnvidante < 0 ? -1 : obs.ParejaEnvidante == obs.Pareja ? 1 : 0);
        Lista(sb, "descartesJugadores", obs.CartasDescartadas);

        List<string> senales = new();
        foreach (TipoSenalMus s in obs.SenalesRecibidas) senales.Add(s.ToString());
        ListaTexto(sb, "senalesRecibidas", senales);

        List<string> historial = new();
        foreach (EventoMus e in obs.HistorialMano) historial.Add(e.ToString());
        ListaTexto(sb, "historial", historial);

        List<string> textosLegales = new();
        foreach (AccionMus a in legales) textosLegales.Add(a.Tipo + ":" + a.Valor);
        ListaTexto(sb, "legales", textosLegales);

        Campo(sb, "accionTipo", accion.Tipo.ToString());
        Campo(sb, "accionValor", accion.Valor);

        partidaActual.Add(new Decision { Json = sb, Pareja = obs.Pareja, Mano = mano });
    }

    public void TerminarMano(PartidaMus partida)
    {
        int mano = partida.ManosJugadas - 1;
        int ganado0 = partida.PuntosGanadosEnMano(0);
        int ganado1 = partida.PuntosGanadosEnMano(1);

        foreach (Decision d in partidaActual)
        {
            if (d.Mano != mano)
                continue;

            d.RecompensaMano = d.Pareja == 0 ? ganado0 - ganado1 : ganado1 - ganado0;
        }
    }

    public void TerminarPartida(int parejaGanadora)
    {
        if (salida == null)
            return;

        foreach (Decision d in partidaActual)
        {
            StringBuilder sb = d.Json;
            Campo(sb, "recompensaMano", d.RecompensaMano);
            int gano = parejaGanadora < 0 ? 0 : d.Pareja == parejaGanadora ? 1 : -1;
            sb.Append("\"resultadoPartida\":").Append(gano).Append('}');
            salida.WriteLine(sb.ToString());
            LineasEscritas++;
        }

        partidaActual.Clear();
    }

    public void Dispose()
    {
        salida?.Flush();
        salida?.Dispose();
    }

    private static void Campo(StringBuilder sb, string nombre, int valor)
    {
        sb.Append('"').Append(nombre).Append("\":").Append(valor).Append(',');
    }

    private static void Campo(StringBuilder sb, string nombre, bool valor)
    {
        sb.Append('"').Append(nombre).Append("\":").Append(valor ? "true" : "false").Append(',');
    }

    private static void Campo(StringBuilder sb, string nombre, string valor)
    {
        sb.Append('"').Append(nombre).Append("\":\"").Append(Escapar(valor)).Append("\",");
    }

    private static void Lista(StringBuilder sb, string nombre, int[] valores)
    {
        sb.Append('"').Append(nombre).Append("\":[");

        for (int i = 0; i < valores.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(valores[i]);
        }

        sb.Append("],");
    }

    private static void ListaTexto(StringBuilder sb, string nombre, List<string> valores)
    {
        sb.Append('"').Append(nombre).Append("\":[");

        for (int i = 0; i < valores.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(Escapar(valores[i])).Append('"');
        }

        sb.Append("],");
    }

    private static string Escapar(string texto)
    {
        return texto.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
