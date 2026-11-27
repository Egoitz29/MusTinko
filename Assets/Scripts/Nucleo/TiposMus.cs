using System;
using System.Collections.Generic;

public enum FaseMus
{
    Mus,
    Descarte,
    Grande,
    Chica,
    Pares,
    JuegoPunto,
    FinMano,
    FinPartida
}

public enum TipoAccionMus
{
    Mus,
    NoHayMus,
    Descartar,
    Pasar,
    Envidar,
    Reenvidar,
    Quiero,
    NoQuiero,
    Ordago,
    Pedrete      // siempre al final: el valor numerico viaja por la red
}

public enum TipoSenalMus
{
    TreintaYUna,
    Duples,
    DosReyes,
    DosAses,
    TresReyes,
    TresAses,
    Ciego
}

public static class SenalesMus
{
    public static string Nombre(TipoSenalMus tipo)
    {
        switch (tipo)
        {
            case TipoSenalMus.TreintaYUna: return "31";
            case TipoSenalMus.Duples: return "DUPLES";
            case TipoSenalMus.DosReyes: return "DOS REYES";
            case TipoSenalMus.DosAses: return "DOS ASES";
            case TipoSenalMus.TresReyes: return "TRES REYES";
            case TipoSenalMus.TresAses: return "TRES ASES";
            default: return "CIEGO (nada)";
        }
    }

    public static string Gesto(TipoSenalMus tipo)
    {
        switch (tipo)
        {
            case TipoSenalMus.TreintaYUna: return "guino";
            case TipoSenalMus.Duples: return "subir las cejas";
            case TipoSenalMus.DosReyes: return "morder el labio";
            case TipoSenalMus.DosAses: return "sacar la lengua";
            case TipoSenalMus.TresReyes: return "morder el labio y ladear la cabeza";
            case TipoSenalMus.TresAses: return "lengua y ladear la cabeza";
            default: return "cerrar los ojos";
        }
    }
}

public enum TipoConceptoMus
{
    EnPaso,
    Querido,
    NoQuerido,
    Pares,
    Juego,
    Punto,
    Ordago,
    Pedrete      // siempre al final: el valor numerico viaja por la red
}

public readonly struct ConceptoTanteoMus
{
    public readonly int Lance;
    public readonly TipoConceptoMus Tipo;
    public readonly int Pareja;
    public readonly int Tantos;
    public readonly int Jugador;

    public ConceptoTanteoMus(int lance, TipoConceptoMus tipo, int pareja, int tantos, int jugador = -1)
    {
        Lance = lance;
        Tipo = tipo;
        Pareja = pareja;
        Tantos = tantos;
        Jugador = jugador;
    }

    public bool CobradoAlMomento => Tipo == TipoConceptoMus.NoQuerido || Tipo == TipoConceptoMus.Pedrete;
}

public enum TipoEventoMus
{
    Accion,
    Declaracion,
    LanceCerrado,
    FinMano
}

public readonly struct AccionMus : IEquatable<AccionMus>
{
    public readonly TipoAccionMus Tipo;
    public readonly int Valor;

    public AccionMus(TipoAccionMus tipo, int valor = 0)
    {
        Tipo = tipo;
        Valor = valor;
    }

    public static AccionMus Mus => new(TipoAccionMus.Mus);
    public static AccionMus NoHayMus => new(TipoAccionMus.NoHayMus);
    public static AccionMus Pasar => new(TipoAccionMus.Pasar);
    public static AccionMus Quiero => new(TipoAccionMus.Quiero);
    public static AccionMus NoQuiero => new(TipoAccionMus.NoQuiero);
    public static AccionMus Ordago => new(TipoAccionMus.Ordago);
    public static AccionMus Pedrete => new(TipoAccionMus.Pedrete);
    public static AccionMus Envidar(int tantos = 2) => new(TipoAccionMus.Envidar, tantos);
    public static AccionMus Reenvidar(int tantos = 2) => new(TipoAccionMus.Reenvidar, tantos);
    public static AccionMus Descartar(int mascara) => new(TipoAccionMus.Descartar, mascara);

    public bool Equals(AccionMus otra) => Tipo == otra.Tipo && Valor == otra.Valor;
    public override bool Equals(object obj) => obj is AccionMus otra && Equals(otra);
    public override int GetHashCode() => ((int)Tipo * 397) ^ Valor;

    public override string ToString()
    {
        switch (Tipo)
        {
            case TipoAccionMus.Mus: return "MUS";
            case TipoAccionMus.NoHayMus: return "NO HAY MUS";
            case TipoAccionMus.Descartar: return "DESCARTA " + ContarBits(Valor);
            case TipoAccionMus.Pasar: return "PASO";
            case TipoAccionMus.Envidar: return "ENVIDO " + Valor;
            case TipoAccionMus.Reenvidar: return "ENVIDO " + Valor + " MAS";
            case TipoAccionMus.Quiero: return "QUIERO";
            case TipoAccionMus.NoQuiero: return "NO QUIERO";
            case TipoAccionMus.Ordago: return "ORDAGO";
            case TipoAccionMus.Pedrete: return "PEDRETE";
            default: return Tipo.ToString();
        }
    }

    public static int ContarBits(int mascara)
    {
        int cuenta = 0;

        for (int i = 0; i < 4; i++)
            if ((mascara & (1 << i)) != 0)
                cuenta++;

        return cuenta;
    }
}

public readonly struct EventoMus
{
    public readonly TipoEventoMus Tipo;
    public readonly FaseMus Fase;
    public readonly int Jugador;
    public readonly AccionMus Accion;
    public readonly int Dato;

    public EventoMus(TipoEventoMus tipo, FaseMus fase, int jugador, AccionMus accion, int dato = 0)
    {
        Tipo = tipo;
        Fase = fase;
        Jugador = jugador;
        Accion = accion;
        Dato = dato;
    }

    public override string ToString()
    {
        switch (Tipo)
        {
            case TipoEventoMus.Accion:
                return "J" + Jugador + " " + Accion;
            case TipoEventoMus.Declaracion:
                return "J" + Jugador + " " + (Fase == FaseMus.Pares ? "PARES " : "JUEGO ") + (Dato == 1 ? "SI" : "NO");
            case TipoEventoMus.LanceCerrado:
                return Fase + " cerrado";
            default:
                return "Fin de mano";
        }
    }
}

public class ObservacionMus
{
    public int Jugador;
    public int Pareja => Jugador % 2;
    public int Companero => (Jugador + 2) % 4;
    public FaseMus Fase;
    public int Mano;
    public int JugadorActual;
    public bool MusCorrido;
    public int RondaMus;
    public int[] Cartas = new int[4];
    public EvaluadorManoMus.Resultado Evaluacion;
    public int PuntosPropios;
    public int PuntosRivales;
    public int Limite;
    public MotorEnvitesMus.EstadoEnvite EstadoEnvite;
    public int ParejaEnvidante;
    public int TantosApostados;
    public bool[] PuedeHablarLance = new bool[4];
    public int[] CartasDescartadas = new int[4];
    public readonly List<EventoMus> HistorialMano = new();
    public readonly List<TipoSenalMus> SenalesRecibidas = new();
    public readonly List<KeyValuePair<int, TipoSenalMus>> SenalesPilladas = new();

    public int PosicionRespectoMano => (Jugador - Mano + 4) % 4;
    public bool EsPostre => PosicionRespectoMano == 3;
}
