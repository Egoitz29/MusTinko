
using System;
using UnityEngine;

public class CanalSenalesMus : MonoBehaviour
{
    [Header("Jugadores 0, 1, 2 y 3")]
    [SerializeField]
    private SenalesPorCartasMus[] jugadores =
        new SenalesPorCartasMus[4];

    [Header("Diagnostico")]
    [SerializeField] private bool mostrarMensajes = true;

    public event Action<int, int, ControlSenalesMus.TipoSenal>
        SenalComunicada;

    private void OnEnable()
    {
        Conectar();
    }

    private void OnDisable()
    {
        Desconectar();
    }

    private void Conectar()
    {
        if (jugadores == null)
            return;

        for (int i = 0; i < jugadores.Length; i++)
        {
            if (jugadores[i] == null)
                continue;

            jugadores[i].SenalRealizada -= RecibirSenal;
            jugadores[i].SenalRealizada += RecibirSenal;
        }
    }

    private void Desconectar()
    {
        if (jugadores == null)
            return;

        for (int i = 0; i < jugadores.Length; i++)
        {
            if (jugadores[i] != null)
                jugadores[i].SenalRealizada -= RecibirSenal;
        }
    }

    private void RecibirSenal(
        SenalesPorCartasMus origen,
        ControlSenalesMus.TipoSenal tipo)
    {
        int emisor = origen.IndiceJugador;

        if (emisor < 0 || emisor > 3 ||
            jugadores == null ||
            emisor >= jugadores.Length ||
            jugadores[emisor] != origen)
        {
            Debug.LogWarning(
                "[MUS SENAS] Emisor no registrado correctamente.",
                this
            );
            return;
        }

        int receptor = (emisor + 2) % 4;

        SenalComunicada?.Invoke(emisor, receptor, tipo);

        if (mostrarMensajes)
        {
            Debug.Log(
                "[MUS SENAS] Jugador " + emisor +
                " comunica a su companero " + receptor +
                ": " + NombreSenal(tipo),
                this
            );
        }
    }

    private static string NombreSenal(
        ControlSenalesMus.TipoSenal tipo)
    {
        switch (tipo)
        {
            case ControlSenalesMus.TipoSenal.TreintaYUna:
                return "31";

            case ControlSenalesMus.TipoSenal.Duples:
                return "DUPLES";

            case ControlSenalesMus.TipoSenal.DosReyes:
                return "DOS REYES";

            case ControlSenalesMus.TipoSenal.DosAses:
                return "DOS ASES";

            default:
                return tipo.ToString();
        }
    }
}
