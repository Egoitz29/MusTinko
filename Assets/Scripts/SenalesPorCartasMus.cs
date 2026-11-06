
using System;
using UnityEngine;

public class SenalesPorCartasMus : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RepartoPrueba reparto;
    [SerializeField] private ControlSenalesMus controlSenales;

    [Header("Jugador representado")]
    [Range(0, 3)]
    [SerializeField] private int indiceJugador = 2;

    public int IndiceJugador => indiceJugador;

    public event Action<SenalesPorCartasMus, ControlSenalesMus.TipoSenal>
        SenalRealizada;

    private void Awake()
    {
        if (controlSenales == null)
            controlSenales = GetComponent<ControlSenalesMus>();
    }

    public bool PuedeHacerSenal(ControlSenalesMus.TipoSenal tipo)
    {
        if (!ManoDisponible())
            return false;

        EvaluadorManoMus.Resultado resultado =
            reparto.EvaluarJugador(indiceJugador);

        if (resultado == null)
            return false;

        switch (tipo)
        {
            case ControlSenalesMus.TipoSenal.TreintaYUna:
                return resultado.PuntosJuego == 31;

            case ControlSenalesMus.TipoSenal.Duples:
                return resultado.Pares ==
                    EvaluadorManoMus.TipoPares.Duples;

            case ControlSenalesMus.TipoSenal.DosReyes:
                return ContarEquivalentes("REY", "3") >= 2;

            case ControlSenalesMus.TipoSenal.DosAses:
                return ContarEquivalentes("AS", "2") >= 2;
        }

        return false;
    }

    public bool SolicitarSenal(ControlSenalesMus.TipoSenal tipo)
    {
        if (controlSenales == null)
        {
            Debug.LogError("Falta ControlSenalesMus.", this);
            return false;
        }

        if (!PuedeHacerSenal(tipo))
        {
            Debug.Log(
                "Jugador " + indiceJugador +
                " no puede hacer la sena " + tipo,
                this
            );

            return false;
        }

        controlSenales.EjecutarSenal(tipo);

        SenalRealizada?.Invoke(this, tipo);

        Debug.Log(
            "Jugador " + indiceJugador +
            " realiza la sena " + tipo,
            this
        );

        return true;
    }

    private bool ManoDisponible()
    {
        if (reparto == null)
        {
            Debug.LogWarning("Falta RepartoPrueba.", this);
            return false;
        }

        for (int i = 0; i < 4; i++)
        {
            string carta = reparto.ObtenerCartaJugador(
                indiceJugador, i
            );

            if (string.IsNullOrWhiteSpace(carta))
                return false;
        }

        return true;
    }

    private int ContarEquivalentes(string primero, string segundo)
    {
        int cantidad = 0;

        for (int i = 0; i < 4; i++)
        {
            string carta = reparto.ObtenerCartaJugador(
                indiceJugador, i
            );

            string valor = carta.Split('\n')[0]
                .Trim()
                .ToUpperInvariant();

            if (valor == primero || valor == segundo)
                cantidad++;
        }

        return cantidad;
    }

    [ContextMenu("Diagnosticar senales disponibles")]
    private void Diagnosticar()
    {
        if (!ManoDisponible())
        {
            Debug.LogWarning("La mano no esta disponible.");
            return;
        }

        Debug.Log(
            "Jugador " + indiceJugador +
            " | 31: " +
            PuedeHacerSenal(ControlSenalesMus.TipoSenal.TreintaYUna) +
            " | Duples: " +
            PuedeHacerSenal(ControlSenalesMus.TipoSenal.Duples) +
            " | Dos reyes: " +
            PuedeHacerSenal(ControlSenalesMus.TipoSenal.DosReyes) +
            " | Dos ases: " +
            PuedeHacerSenal(ControlSenalesMus.TipoSenal.DosAses)
        );
    }

    [ContextMenu("Hacer sena 31")]
    private void Probar31()
    {
        SolicitarSenal(ControlSenalesMus.TipoSenal.TreintaYUna);
    }

    [ContextMenu("Hacer sena Duples")]
    private void ProbarDuples()
    {
        SolicitarSenal(ControlSenalesMus.TipoSenal.Duples);
    }

    [ContextMenu("Hacer sena Dos Reyes")]
    private void ProbarDosReyes()
    {
        SolicitarSenal(ControlSenalesMus.TipoSenal.DosReyes);
    }

    [ContextMenu("Hacer sena Dos Ases")]
    private void ProbarDosAses()
    {
        SolicitarSenal(ControlSenalesMus.TipoSenal.DosAses);
    }
}
