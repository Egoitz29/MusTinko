using System;
using System.Collections.Generic;
using System.Globalization;

public interface IBotMus
{
    string Nombre { get; }
    AccionMus Decidir(ObservacionMus obs, IReadOnlyList<AccionMus> legales, Random azar);
    IEnumerable<TipoSenalMus> ElegirSenales(ObservacionMus obs, Random azar);
}

public class BotAleatorioMus : IBotMus
{
    public string Nombre => "Aleatorio";

    public AccionMus Decidir(ObservacionMus obs, IReadOnlyList<AccionMus> legales, Random azar)
    {
        return legales[azar.Next(legales.Count)];
    }

    public IEnumerable<TipoSenalMus> ElegirSenales(ObservacionMus obs, Random azar)
    {
        yield break;
    }
}

[Serializable]
public class ParametrosBotMus
{
    public float UmbralCortarMus = 0.55f;
    public float UmbralEnvidar = 0.55f;
    public float UmbralQuerer = 0.5f;
    public float UmbralReenvidar = 0.75f;
    public float UmbralOrdago = 0.92f;
    public float UmbralQuererOrdago = 0.85f;
    public float ProbabilidadFarol = 0.05f;
    public float ProbabilidadSenal = 0.8f;
    public float AumentoPorSenal = 0.25f;
    public float AumentoQuererPorSenal = 0.15f;
    public bool UsaSenales = true;

    public const int NumeroParametros = 10;

    public float[] AVector()
    {
        return new[]
        {
            UmbralCortarMus, UmbralEnvidar, UmbralQuerer, UmbralReenvidar, UmbralOrdago,
            UmbralQuererOrdago, ProbabilidadFarol, ProbabilidadSenal, AumentoPorSenal, AumentoQuererPorSenal
        };
    }

    public static ParametrosBotMus DesdeVector(float[] v, bool usaSenales = true)
    {
        return new ParametrosBotMus
        {
            UmbralCortarMus = Limitar(v[0]),
            UmbralEnvidar = Limitar(v[1]),
            UmbralQuerer = Limitar(v[2]),
            UmbralReenvidar = Limitar(v[3]),
            UmbralOrdago = Limitar(v[4]),
            UmbralQuererOrdago = Limitar(v[5]),
            ProbabilidadFarol = Math.Min(0.5f, Limitar(v[6])),
            ProbabilidadSenal = Limitar(v[7]),
            AumentoPorSenal = Math.Min(0.6f, Limitar(v[8])),
            AumentoQuererPorSenal = Math.Min(0.6f, Limitar(v[9])),
            UsaSenales = usaSenales
        };
    }

    public ParametrosBotMus Copiar() => DesdeVector(AVector(), UsaSenales);

    public override string ToString()
    {
        string[] nombres =
        {
            "cortarMus", "envidar", "querer", "reenvidar", "ordago",
            "quererOrdago", "farol", "senal", "aumentoSenal", "aumentoQuererSenal"
        };

        float[] v = AVector();
        List<string> partes = new();

        for (int i = 0; i < v.Length; i++)
            partes.Add(nombres[i] + "=" + v[i].ToString("0.000", CultureInfo.InvariantCulture));

        return string.Join(", ", partes) + ", usaSenales=" + UsaSenales;
    }

    private static float Limitar(float valor) => Math.Max(0f, Math.Min(1f, valor));

    public static ParametrosBotMus Equilibrado() => new();

    public static ParametrosBotMus Prudente() => new()
    {
        UmbralCortarMus = 0.5f,
        UmbralEnvidar = 0.7f,
        UmbralQuerer = 0.65f,
        UmbralReenvidar = 0.88f,
        UmbralOrdago = 0.98f,
        UmbralQuererOrdago = 0.93f,
        ProbabilidadFarol = 0.01f
    };

    public static ParametrosBotMus Agresivo() => new()
    {
        UmbralCortarMus = 0.6f,
        UmbralEnvidar = 0.4f,
        UmbralQuerer = 0.38f,
        UmbralReenvidar = 0.6f,
        UmbralOrdago = 0.85f,
        UmbralQuererOrdago = 0.75f,
        ProbabilidadFarol = 0.15f
    };

    public static ParametrosBotMus SinSenales()
    {
        ParametrosBotMus p = new() { UsaSenales = false, ProbabilidadSenal = 0f };
        return p;
    }
}

public class BotReglasMus : IBotMus
{
    private readonly ParametrosBotMus p;

    public string Nombre { get; }
    public ParametrosBotMus Parametros => p;

    public BotReglasMus(string nombre, ParametrosBotMus parametros)
    {
        Nombre = nombre;
        p = parametros ?? new ParametrosBotMus();
    }

    public AccionMus Decidir(ObservacionMus obs, IReadOnlyList<AccionMus> legales, Random azar)
    {
        switch (obs.Fase)
        {
            case FaseMus.Mus:
                return DecidirMus(obs, legales);
            case FaseMus.Descarte:
                return AccionMus.Descartar(ElegirDescarte(obs.Cartas));
            default:
                return DecidirLance(obs, legales, azar);
        }
    }

    public IEnumerable<TipoSenalMus> ElegirSenales(ObservacionMus obs, Random azar)
    {
        if (!p.UsaSenales)
            yield break;

        foreach (TipoSenalMus tipo in (TipoSenalMus[])Enum.GetValues(typeof(TipoSenalMus)))
        {
            if (PartidaMus.CumpleSenal(obs.Cartas, obs.Evaluacion, tipo) &&
                azar.NextDouble() < p.ProbabilidadSenal)
                yield return tipo;
        }
    }

    private AccionMus DecidirMus(ObservacionMus obs, IReadOnlyList<AccionMus> legales)
    {
        bool puedeMus = Contiene(legales, TipoAccionMus.Mus);

        if (!puedeMus || FuerzaMus(obs.Evaluacion, obs.Cartas) >= p.UmbralCortarMus)
            return AccionMus.NoHayMus;

        return AccionMus.Mus;
    }

    private AccionMus DecidirLance(ObservacionMus obs, IReadOnlyList<AccionMus> legales, Random azar)
    {
        float fuerza = FuerzaLance(obs.Fase, obs);
        float aumentoSenal = p.UsaSenales ? AumentoSenal(obs) : 0f;
        bool pendiente = Contiene(legales, TipoAccionMus.Quiero);
        bool ordagoPendiente = obs.EstadoEnvite == MotorEnvitesMus.EstadoEnvite.OrdagoPendiente;
        bool desesperado = obs.PuntosRivales >= obs.Limite - 5 && obs.PuntosPropios < obs.Limite - 10;

        if (!pendiente)
        {
            float valor = fuerza + aumentoSenal * p.AumentoPorSenal / 0.25f;

            if (valor >= p.UmbralOrdago || (desesperado && valor >= p.UmbralOrdago - 0.15f))
                return AccionMus.Ordago;

            if (valor >= p.UmbralEnvidar || azar.NextDouble() < p.ProbabilidadFarol)
                return Buscar(legales, TipoAccionMus.Envidar, AccionMus.Pasar);

            return AccionMus.Pasar;
        }

        float valorRespuesta = fuerza + aumentoSenal * p.AumentoQuererPorSenal / 0.25f;

        if (ordagoPendiente)
        {
            float umbral = desesperado ? p.UmbralQuererOrdago - 0.15f : p.UmbralQuererOrdago;
            return valorRespuesta >= umbral ? AccionMus.Quiero : AccionMus.NoQuiero;
        }

        if (valorRespuesta >= p.UmbralOrdago && Contiene(legales, TipoAccionMus.Ordago))
            return AccionMus.Ordago;

        if (valorRespuesta >= p.UmbralReenvidar && obs.TantosApostados < 6 && Contiene(legales, TipoAccionMus.Reenvidar))
            return Buscar(legales, TipoAccionMus.Reenvidar, AccionMus.Quiero);

        if (valorRespuesta >= p.UmbralQuerer)
            return AccionMus.Quiero;

        return AccionMus.NoQuiero;
    }

    private static float AumentoSenal(ObservacionMus obs)
    {
        foreach (TipoSenalMus senal in obs.SenalesRecibidas)
        {
            if (obs.Fase == FaseMus.Grande && senal == TipoSenalMus.DosReyes) return 0.25f;
            if (obs.Fase == FaseMus.Chica && senal == TipoSenalMus.DosAses) return 0.25f;
            if (obs.Fase == FaseMus.Pares && senal == TipoSenalMus.Duples) return 0.25f;
            if (obs.Fase == FaseMus.JuegoPunto && senal == TipoSenalMus.TreintaYUna) return 0.25f;
        }

        return 0f;
    }

    public static float FuerzaLance(FaseMus fase, ObservacionMus obs)
    {
        EvaluadorManoMus.Resultado e = obs.Evaluacion;
        int[] r = e.RangosOrdenados;
        float posicion = obs.PosicionRespectoMano == 0 ? 0.05f : obs.EsPostre ? -0.03f : 0f;

        switch (fase)
        {
            case FaseMus.Grande:
            {
                float f = 0f;
                foreach (int rango in r)
                    f += rango == 8 ? 0.27f : rango == 7 ? 0.08f : rango == 6 ? 0.04f : 0f;
                return Limitar(f + posicion);
            }
            case FaseMus.Chica:
            {
                float f = 0f;
                foreach (int rango in r)
                    f += rango == 1 ? 0.27f : rango == 2 ? 0.08f : rango == 3 ? 0.04f : 0f;
                return Limitar(f + posicion);
            }
            case FaseMus.Pares:
            {
                float f;
                switch (e.Pares)
                {
                    case EvaluadorManoMus.TipoPares.Duples: f = 0.85f + r[3] * 0.02f; break;
                    case EvaluadorManoMus.TipoPares.Medias: f = 0.6f + r[1] * 0.03f; break;
                    case EvaluadorManoMus.TipoPares.Par: f = 0.2f + RangoPar(r) * 0.045f; break;
                    default: f = 0f; break;
                }
                return Limitar(f + posicion);
            }
            default:
            {
                if (!e.TieneJuego)
                {
                    bool hayJuego = false;
                    foreach (EventoMus ev in obs.HistorialMano)
                        if (ev.Tipo == TipoEventoMus.Declaracion && ev.Fase == FaseMus.JuegoPunto && ev.Dato == 1)
                            hayJuego = true;

                    if (hayJuego)
                        return 0f;

                    return Limitar((e.PuntosJuego - 20) / 10f + posicion);
                }

                float f;
                switch (e.PuntosJuego)
                {
                    case 31: f = 1f; break;
                    case 32: f = 0.8f; break;
                    case 40: f = 0.62f; break;
                    case 37: f = 0.5f; break;
                    case 36: f = 0.45f; break;
                    case 35: f = 0.4f; break;
                    case 34: f = 0.35f; break;
                    default: f = 0.3f; break;
                }
                return Limitar(f + posicion);
            }
        }
    }

    public static float FuerzaMus(EvaluadorManoMus.Resultado e, int[] cartas)
    {
        float f = 0f;

        switch (e.Pares)
        {
            case EvaluadorManoMus.TipoPares.Duples: f += 0.8f; break;
            case EvaluadorManoMus.TipoPares.Medias: f += 0.75f; break;
            case EvaluadorManoMus.TipoPares.Par: f += 0.3f; break;
        }

        if (e.TieneJuego)
        {
            switch (e.PuntosJuego)
            {
                case 31: f += 0.75f; break;
                case 32: f += 0.65f; break;
                case 40: f += 0.45f; break;
                default: f += 0.25f; break;
            }
        }

        int reyes = 0;
        int ases = 0;

        foreach (int rango in e.RangosOrdenados)
        {
            if (rango == 8) reyes++;
            if (rango == 1) ases++;
        }

        if (reyes >= 2) f += 0.25f;
        if (ases >= 3) f += 0.25f;

        return Limitar(f);
    }

    public static int ElegirDescarte(int[] cartas)
    {
        int[] cuenta = new int[9];

        foreach (int carta in cartas)
            cuenta[CartasMus.Rango(carta)]++;

        int mascara = 0;

        for (int i = 0; i < cartas.Length; i++)
        {
            int rango = CartasMus.Rango(cartas[i]);

            if (rango == 8 || cuenta[rango] >= 2)
                continue;

            mascara |= 1 << i;
        }

        if (mascara == 0)
        {
            int peor = -1;
            int peorRango = int.MaxValue;

            for (int i = 0; i < cartas.Length; i++)
            {
                int rango = CartasMus.Rango(cartas[i]);

                if (rango == 8)
                    continue;

                if (rango < peorRango)
                {
                    peorRango = rango;
                    peor = i;
                }
            }

            mascara = peor >= 0 ? 1 << peor : 1;
        }

        return mascara;
    }

    private static int RangoPar(int[] r)
    {
        for (int i = 1; i < r.Length; i++)
            if (r[i] == r[i - 1])
                return r[i];

        return 0;
    }

    private static bool Contiene(IReadOnlyList<AccionMus> legales, TipoAccionMus tipo)
    {
        foreach (AccionMus a in legales)
            if (a.Tipo == tipo)
                return true;

        return false;
    }

    private static AccionMus Buscar(IReadOnlyList<AccionMus> legales, TipoAccionMus tipo, AccionMus porDefecto)
    {
        foreach (AccionMus a in legales)
            if (a.Tipo == tipo)
                return a;

        return porDefecto;
    }

    private static float Limitar(float valor) => Math.Max(0f, Math.Min(1f, valor));
}
