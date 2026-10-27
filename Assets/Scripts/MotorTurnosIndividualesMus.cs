using System;

// Controla QUIEN habla. MotorEnvitesMus sigue controlando el envite POR PAREJAS.
// Jugadores: 0 y 2 pareja 0; 1 y 3 pareja 1.
public class MotorTurnosIndividualesMus
{
    private readonly MotorEnvitesMus motor = new MotorEnvitesMus();
    private readonly bool[] habilitados = new bool[4];
    private readonly bool[] rechazoIndividual = new bool[4];

    private int pasesConsecutivos;
    private int totalHabilitados;

    public MotorEnvitesMus Motor => motor;
    public int JugadorActual { get; private set; } = -1;
    public bool Cerrado { get; private set; }
    public bool HayEnvitePendiente =>
        motor.Estado == MotorEnvitesMus.EstadoEnvite.PendienteRespuesta ||
        motor.Estado == MotorEnvitesMus.EstadoEnvite.OrdagoPendiente;

    public void Reiniciar()
    {
        motor.Reiniciar();
        JugadorActual = -1;
        Cerrado = false;
        pasesConsecutivos = 0;
        totalHabilitados = 0;
        Array.Clear(habilitados, 0, habilitados.Length);
        Array.Clear(rechazoIndividual, 0, rechazoIndividual.Length);
    }

    public void Iniciar(int jugadorMano, bool[] puedenHablar)
    {
        if (jugadorMano < 0 || jugadorMano > 3)
            throw new ArgumentOutOfRangeException(nameof(jugadorMano));

        if (puedenHablar == null || puedenHablar.Length != 4)
            throw new ArgumentException("Se necesitan 4 autorizaciones de turno.");

        Reiniciar();
        for (int i = 0; i < 4; i++)
        {
            habilitados[i] = puedenHablar[i];
            if (habilitados[i]) totalHabilitados++;
        }

        if (totalHabilitados == 0)
        {
            Cerrado = true;
            return;
        }

        JugadorActual = BuscarHabilitadoDesde(jugadorMano);
    }

    public bool Pasar(int jugador)
    {
        if (!PuedeActuar(jugador) || HayEnvitePendiente)
            return false;

        pasesConsecutivos++;
        if (pasesConsecutivos >= totalHabilitados)
        {
            Cerrar();
            return true;
        }

        JugadorActual = BuscarHabilitadoDesde((jugador + 1) % 4);
        return true;
    }

    public bool Envidar(int jugador, int tantos = 2)
    {
        if (!PuedeActuar(jugador) || HayEnvitePendiente)
            return false;

        int pareja = jugador % 2;
        if (BuscarRespondedorDesde(jugador, pareja) < 0)
            return false;

        if (!motor.Envidar(pareja, tantos))
            return false;

        PrepararRespuestas(jugador);
        return true;
    }

    public bool Reenvidar(int jugador, int tantosAdicionales = 2)
    {
        if (!PuedeActuar(jugador) ||
            motor.Estado != MotorEnvitesMus.EstadoEnvite.PendienteRespuesta)
            return false;

        if (!motor.Reenvidar(jugador % 2, tantosAdicionales))
            return false;

        PrepararRespuestas(jugador);
        return true;
    }

    public bool LanzarOrdago(int jugador)
    {
        if (!PuedeActuar(jugador) ||
            motor.Estado == MotorEnvitesMus.EstadoEnvite.OrdagoPendiente)
            return false;

        int pareja = jugador % 2;
        if (BuscarRespondedorDesde(jugador, pareja) < 0)
            return false;

        if (!motor.LanzarOrdago(pareja))
            return false;

        PrepararRespuestas(jugador);
        return true;
    }

    public bool Quiero(int jugador)
    {
        if (!PuedeActuar(jugador) || !HayEnvitePendiente)
            return false;

        if (!motor.Quiero(jugador % 2))
            return false;

        // Un QUIERO basta para comprometer a la pareja.
        Cerrar();
        return true;
    }

    public bool NoQuiero(int jugador)
    {
        if (!PuedeActuar(jugador) || !HayEnvitePendiente)
            return false;

        rechazoIndividual[jugador] = true;

        // NO QUIERO no cierra hasta escuchar al otro jugador de la pareja.
        int siguiente = BuscarRespondedorDesde(jugador, motor.ParejaEnvidante);
        if (siguiente >= 0)
        {
            JugadorActual = siguiente;
            return true;
        }

        if (!motor.NoQuiero(jugador % 2))
            return false;

        Cerrar();
        return true;
    }

    private void PrepararRespuestas(int autor)
    {
        Array.Clear(rechazoIndividual, 0, rechazoIndividual.Length);
        pasesConsecutivos = 0;
        JugadorActual = BuscarRespondedorDesde(autor, motor.ParejaEnvidante);
        if (JugadorActual < 0)
            throw new InvalidOperationException("Ningun rival puede responder al envite.");
    }

    private int BuscarRespondedorDesde(int jugadorPrevio, int parejaQueEnvida)
    {
        for (int paso = 1; paso <= 4; paso++)
        {
            int candidato = (jugadorPrevio + paso) % 4;
            if (habilitados[candidato] &&
                candidato % 2 != parejaQueEnvida &&
                !rechazoIndividual[candidato])
                return candidato;
        }
        return -1;
    }

    private int BuscarHabilitadoDesde(int primero)
    {
        for (int paso = 0; paso < 4; paso++)
        {
            int candidato = (primero + paso) % 4;
            if (habilitados[candidato]) return candidato;
        }
        return -1;
    }

    private bool PuedeActuar(int jugador)
    {
        return !Cerrado &&
            jugador >= 0 && jugador < 4 &&
            JugadorActual == jugador && habilitados[jugador];
    }

    private void Cerrar()
    {
        Cerrado = true;
        JugadorActual = -1;
    }
}
