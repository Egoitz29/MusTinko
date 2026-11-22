using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// Parametros del bot probabilista (todos entre 0 y 1; se ajustan entrenando con millones de partidas).
public class ParametrosProbabilistaMus
{
    public const int Tamano = 14;

    public float CortarMus = 0.52f;          // fuerza de la mano para cortar el mus
    public float Envidar = 0.6f;             // probabilidad de ganar a partir de la que envida
    public float Querer = 0.5f;              // probabilidad a partir de la que quiere 2 tantos
    public float ExigenciaPorTanto = 0.35f;  // cuanto mas exige para querer envites grandes
    public float Subir = 0.75f;              // a partir de aqui sube la apuesta
    public float Ordago = 0.93f;             // a partir de aqui echa ordago
    public float QuererOrdago = 0.88f;       // a partir de aqui acepta un ordago
    public float Farol = 0.05f;              // probabilidad de envidar sin llevar nada
    public float Cantidad = 0.35f;           // cuantos tantos envida por cada punto de ventaja
    public float MaximoCantidad = 0.25f;     // envite maximo (de 2 a 20 tantos)
    public float PesoCompanero = 0.5f;       // cuanto cuenta lo que dice su companero con las senas
    public float ProbabilidadSena = 0.8f;    // cuantas senas hace
    public float Urgencia = 0.3f;            // cuanto arriesga si los rivales estan a punto de ganar
    public float PesoPares = 0.5f;           // cuanto pesan pares y juego al decidir el mus

    public float[] AVector() => new[]
    {
        CortarMus, Envidar, Querer, ExigenciaPorTanto, Subir, Ordago, QuererOrdago, Farol,
        Cantidad, MaximoCantidad, PesoCompanero, ProbabilidadSena, Urgencia, PesoPares
    };

    public static ParametrosProbabilistaMus DesdeVector(float[] v)
    {
        float L(int i) => Math.Max(0f, Math.Min(1f, v[i]));
        return new ParametrosProbabilistaMus
        {
            CortarMus = L(0), Envidar = L(1), Querer = L(2), ExigenciaPorTanto = L(3), Subir = L(4), Ordago = L(5),
            QuererOrdago = L(6), Farol = L(7), Cantidad = L(8), MaximoCantidad = L(9), PesoCompanero = L(10),
            ProbabilidadSena = L(11), Urgencia = L(12), PesoPares = L(13)
        };
    }

    public string Guardar() => "# Bot probabilista de Mus TINKO (entrenado)\n" +
                               string.Join(";", AVector().Select(x => x.ToString("0.0000", CultureInfo.InvariantCulture)));

    public static ParametrosProbabilistaMus Cargar(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return null;

        foreach (string linea in texto.Split('\n'))
        {
            string l = linea.Trim();

            if (l.Length == 0 || l[0] == '#')
                continue;

            string[] p = l.Split(';');

            if (p.Length != Tamano)
                return null;

            return DesdeVector(p.Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToArray());
        }

        return null;
    }

    public override string ToString() => string.Join(" ", AVector().Select(x => x.ToString("0.00", CultureInfo.InvariantCulture)));
}

// Bot que decide con la probabilidad real de ganar cada lance (TablasProbabilidadMus),
// usa los envites libres (envida y sube mas cuanta mas ventaja tiene) y exige mas para querer envites grandes.
public class BotProbabilistaMus : IBotMus
{
    private readonly ParametrosProbabilistaMus p;
    private readonly BotReglasMus ayudante;   // senas y descartes, como el bot de reglas

    public string Nombre { get; }

    public BotProbabilistaMus(string nombre, ParametrosProbabilistaMus parametros)
    {
        Nombre = nombre;
        p = parametros ?? new ParametrosProbabilistaMus();
        ayudante = new BotReglasMus(nombre + "_ayuda", new ParametrosBotMus { ProbabilidadSenal = p.ProbabilidadSena });
    }

    public IEnumerable<TipoSenalMus> ElegirSenales(ObservacionMus obs, Random azar) => ayudante.ElegirSenales(obs, azar);

    public AccionMus Decidir(ObservacionMus obs, IReadOnlyList<AccionMus> legales, Random azar)
    {
        switch (obs.Fase)
        {
            case FaseMus.Mus:
                return DecidirMus(obs, legales);
            case FaseMus.Descarte:
                return ayudante.Decidir(obs, legales, azar);
            default:
                return DecidirLance(obs, legales, azar);
        }
    }

    // ---------- Probabilidades ----------

    // Lo que han declarado al empezar pares o juego (Dato 1 = tiene).
    private static bool Declarado(ObservacionMus obs, FaseMus fase, bool soloCompanero)
    {
        foreach (EventoMus e in obs.HistorialMano)
            if (e.Tipo == TipoEventoMus.Declaracion && e.Fase == fase && e.Dato == 1 && (!soloCompanero || e.Jugador == obs.Companero))
                return true;

        return false;
    }

    private float ProbabilidadLance(ObservacionMus obs, FaseMus fase)
    {
        int pos = obs.PosicionRespectoMano;

        switch (fase)
        {
            case FaseMus.Grande: return TablasProbabilidadMus.Probabilidad(obs.Cartas, pos, TablasProbabilidadMus.Grande);
            case FaseMus.Chica: return TablasProbabilidadMus.Probabilidad(obs.Cartas, pos, TablasProbabilidadMus.Chica);
            case FaseMus.Pares:
                if (obs.Evaluacion.Pares == EvaluadorManoMus.TipoPares.SinPares)
                    return 0f;
                return TablasProbabilidadMus.Probabilidad(obs.Cartas, pos,
                    Declarado(obs, FaseMus.Pares, true) ? TablasProbabilidadMus.ParesConCompanero : TablasProbabilidadMus.Pares);
            default:
                if (obs.Evaluacion.TieneJuego)
                    return TablasProbabilidadMus.Probabilidad(obs.Cartas, pos,
                        Declarado(obs, FaseMus.JuegoPunto, true) ? TablasProbabilidadMus.JuegoConCompanero : TablasProbabilidadMus.Juego);
                return Declarado(obs, FaseMus.JuegoPunto, false) ? 0f : TablasProbabilidadMus.Probabilidad(obs.Cartas, pos, TablasProbabilidadMus.Punto);
        }
    }

    // Lo que ha dicho el companero con las senas sube la probabilidad de la pareja.
    private float ConCompanero(ObservacionMus obs, float propia)
    {
        float companero = BotReglasMus.EstimacionCompanero(obs, true);
        return 1f - (1f - propia) * (1f - p.PesoCompanero * companero);
    }

    // ---------- Mus ----------

    private AccionMus DecidirMus(ObservacionMus obs, IReadOnlyList<AccionMus> legales)
    {
        if (!Contiene(legales, TipoAccionMus.Mus))
            return AccionMus.NoHayMus;

        int pos = obs.PosicionRespectoMano;
        float g = TablasProbabilidadMus.Probabilidad(obs.Cartas, pos, TablasProbabilidadMus.Grande);
        float c = TablasProbabilidadMus.Probabilidad(obs.Cartas, pos, TablasProbabilidadMus.Chica);
        float pa = obs.Evaluacion.Pares != EvaluadorManoMus.TipoPares.SinPares ? Media(obs, pos, TablasProbabilidadMus.Pares, TablasProbabilidadMus.ParesConCompanero) : 0f;
        float j = obs.Evaluacion.TieneJuego ? Media(obs, pos, TablasProbabilidadMus.Juego, TablasProbabilidadMus.JuegoConCompanero)
            : 0.5f * TablasProbabilidadMus.Probabilidad(obs.Cartas, pos, TablasProbabilidadMus.Punto);

        float fuerza = (1f - p.PesoPares) * (0.6f * g + 0.4f * c) + p.PesoPares * 0.5f * (pa + j);
        fuerza = Math.Max(fuerza, Math.Max(pa, j) * 0.95f);
        return ConCompanero(obs, fuerza) >= p.CortarMus ? AccionMus.NoHayMus : AccionMus.Mus;
    }

    // Antes del mus aun no se sabe si el companero lleva pares o juego: se usa la media.
    private static float Media(ObservacionMus obs, int pos, int a, int b) =>
        0.5f * (TablasProbabilidadMus.Probabilidad(obs.Cartas, pos, a) + TablasProbabilidadMus.Probabilidad(obs.Cartas, pos, b));

    // ---------- Lances con envites libres ----------

    private AccionMus DecidirLance(ObservacionMus obs, IReadOnlyList<AccionMus> legales, Random azar)
    {
        float prob = ConCompanero(obs, ProbabilidadLance(obs, obs.Fase));
        bool pendiente = Contiene(legales, TipoAccionMus.Quiero);
        bool ordagoPendiente = obs.EstadoEnvite == MotorEnvitesMus.EstadoEnvite.OrdagoPendiente;

        // Si los rivales estan a punto de ganar, se arriesga mas.
        float urgencia = obs.PuntosRivales >= obs.Limite - 6 && obs.PuntosPropios < obs.PuntosRivales ? p.Urgencia * 0.2f : 0f;

        if (!pendiente)
        {
            if (prob >= p.Ordago - urgencia && Contiene(legales, TipoAccionMus.Ordago))
                return AccionMus.Ordago;

            bool farol = azar.NextDouble() < p.Farol * 0.3f;

            if ((prob >= p.Envidar - urgencia || farol) && Contiene(legales, TipoAccionMus.Envidar))
                return AccionMus.Envidar(Cantidad(obs, prob, p.Envidar, false));

            return AccionMus.Pasar;
        }

        if (ordagoPendiente)
            return prob >= p.QuererOrdago - urgencia ? AccionMus.Quiero : AccionMus.NoQuiero;

        // Cuanto mas hay en juego, mas seguro hay que estar para querer.
        int tantos = Math.Max(2, obs.TantosApostados);
        float exigencia = p.Querer + p.ExigenciaPorTanto * 0.12f * (float)Math.Log(tantos / 2.0, 2) - urgencia;

        if (prob >= p.Ordago - urgencia && Contiene(legales, TipoAccionMus.Ordago))
            return AccionMus.Ordago;

        if (prob >= Math.Max(p.Subir, exigencia + 0.1f) && Contiene(legales, TipoAccionMus.Reenvidar))
            return AccionMus.Reenvidar(Cantidad(obs, prob, p.Subir, true));

        return prob >= exigencia ? AccionMus.Quiero : AccionMus.NoQuiero;
    }

    // Cuantos tantos: 2 y algo mas por cada punto de ventaja sobre el umbral, sin pasarse del maximo ni del limite.
    private int Cantidad(ObservacionMus obs, float prob, float umbral, bool subida)
    {
        int maximo = 2 + (int)Math.Round(p.MaximoCantidad * 18f);
        int cantidad = 2 + (int)Math.Round(Math.Max(0f, prob - umbral) * p.Cantidad * 30f);
        int tope = subida ? obs.Limite - 1 - obs.TantosApostados : obs.Limite - 1;
        return Math.Max(2, Math.Min(Math.Min(cantidad, maximo), Math.Max(2, tope)));
    }

    private static bool Contiene(IReadOnlyList<AccionMus> legales, TipoAccionMus tipo)
    {
        foreach (AccionMus a in legales)
            if (a.Tipo == tipo)
                return true;

        return false;
    }
}
