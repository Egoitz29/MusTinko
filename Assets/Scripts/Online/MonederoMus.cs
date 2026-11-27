using System;
using UnityEngine;

// Monedas del jugador, guardadas en este dispositivo.
// Solo se ganan jugando: no se compran ni se cambian por dinero.
// (Mas adelante se pueden pasar a la nube con Unity Cloud Save para el online.)
public static class MonederoMus
{
    public const int SaldoInicial = 1000;
    public const int RegaloDiario = 200;

    private const string ClaveSaldo = "MusTinko_Monedas";
    private const string ClaveRegalo = "MusTinko_UltimoRegalo";
    private const string ClaveJugadas = "MusTinko_PartidasJugadas";
    private const string ClaveGanadas = "MusTinko_PartidasGanadas";

    public static event Action Cambio;

    public static int Saldo
    {
        get
        {
            try
            {
                if (!PlayerPrefs.HasKey(ClaveSaldo))
                {
                    PlayerPrefs.SetInt(ClaveSaldo, SaldoInicial);
                    PlayerPrefs.Save();
                }

                return PlayerPrefs.GetInt(ClaveSaldo, SaldoInicial);
            }
            catch
            {
                return SaldoInicial;
            }
        }
        private set
        {
            try
            {
                PlayerPrefs.SetInt(ClaveSaldo, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
            catch
            {
            }

            Cambio?.Invoke();
        }
    }

    public static int PartidasJugadas => LeerEntero(ClaveJugadas);
    public static int PartidasGanadas => LeerEntero(ClaveGanadas);

    public static bool PuedePagar(int cantidad) => cantidad <= 0 || Saldo >= cantidad;

    public static bool Pagar(int cantidad)
    {
        if (cantidad <= 0)
            return true;

        int saldo = Saldo;

        if (saldo < cantidad)
            return false;

        Saldo = saldo - cantidad;
        return true;
    }

    public static void Cobrar(int cantidad)
    {
        if (cantidad > 0)
            Saldo = Saldo + cantidad;
    }

    public static void ContarPartida(bool ganada)
    {
        EscribirEntero(ClaveJugadas, PartidasJugadas + 1);

        if (ganada)
            EscribirEntero(ClaveGanadas, PartidasGanadas + 1);
    }

    private static string Hoy => DateTime.Now.ToString("yyyyMMdd");

    public static bool RegaloDisponible
    {
        get
        {
            try { return PlayerPrefs.GetString(ClaveRegalo, "") != Hoy; }
            catch { return false; }
        }
    }

    public static bool ReclamarRegalo()
    {
        if (!RegaloDisponible)
            return false;

        try
        {
            PlayerPrefs.SetString(ClaveRegalo, Hoy);
        }
        catch
        {
            return false;
        }

        Cobrar(RegaloDiario);
        return true;
    }

    // Codigo de desarrollador: escribiendo "monedas" donde se anaden amigos te da 10.000 monedas.
    // Solo funciona una vez en cada movil. No lo cuentes :)
    public const string CodigoDesarrollador = "monedas";
    public const int PremioDesarrollador = 10000;
    private const string ClaveCodigoDesarrollador = "MusTinko_CodigoDev";

    public static bool EsCodigoDesarrollador(string texto) =>
        string.Equals((texto ?? "").Trim(), CodigoDesarrollador, StringComparison.OrdinalIgnoreCase);

    // true si se han dado las monedas; false si ya se habia usado.
    public static bool CanjearCodigoDesarrollador()
    {
        if (LeerEntero(ClaveCodigoDesarrollador) != 0)
            return false;

        EscribirEntero(ClaveCodigoDesarrollador, 1);
        Cobrar(PremioDesarrollador);
        return true;
    }

    // Con puntos de miles: 12500 -> "12.500".
    public static string Formato(int cantidad) => cantidad.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", ".");

    private static int LeerEntero(string clave)
    {
        try { return PlayerPrefs.GetInt(clave, 0); }
        catch { return 0; }
    }

    private static void EscribirEntero(string clave, int valor)
    {
        try
        {
            PlayerPrefs.SetInt(clave, valor);
            PlayerPrefs.Save();
        }
        catch
        {
        }
    }
}
