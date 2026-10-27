
public class MotorEnvitesMus
{
    public enum EstadoEnvite
    {
        SinApuesta,
        PendienteRespuesta,
        Aceptado,
        Rechazado,
        OrdagoPendiente,
        OrdagoAceptado
    }

    public EstadoEnvite Estado { get; private set; }
        = EstadoEnvite.SinApuesta;

    public int ParejaEnvidante { get; private set; } = -1;

    public int TantosApostados { get; private set; }

    public int TantosAnteriores { get; private set; }

    public int ParejaGanadoraNegada { get; private set; } = -1;

    public int TantosPorNegada { get; private set; }

    public bool HayOrdago =>
        Estado == EstadoEnvite.OrdagoPendiente ||
        Estado == EstadoEnvite.OrdagoAceptado;

    // ========================================
    // ENVITE INICIAL
    // ========================================

    public bool Envidar(int pareja, int tantos = 2)
    {
        if (Estado != EstadoEnvite.SinApuesta)
            return false;

        if (!ParejaValida(pareja) || tantos < 2)
            return false;

        ParejaEnvidante = pareja;
        TantosApostados = tantos;
        TantosAnteriores = 0;

        Estado = EstadoEnvite.PendienteRespuesta;

        return true;
    }

    // ========================================
    // REENVIDO
    // ========================================

    public bool Reenvidar(int pareja, int tantosAdicionales = 2)
    {
        if (Estado != EstadoEnvite.PendienteRespuesta)
            return false;

        if (!PuedeResponder(pareja))
            return false;

        if (tantosAdicionales < 1)
            return false;

        TantosAnteriores = TantosApostados;
        TantosApostados += tantosAdicionales;

        ParejaEnvidante = pareja;

        return true;
    }

    // ========================================
    // ÓRDAGO
    // ========================================

    public bool LanzarOrdago(int pareja)
    {
        if (!ParejaValida(pareja))
            return false;

        // Órdago directo, sin apuestas anteriores.
        if (Estado == EstadoEnvite.SinApuesta)
        {
            ParejaEnvidante = pareja;

            TantosAnteriores = 0;
            TantosApostados = 0;

            Estado = EstadoEnvite.OrdagoPendiente;

            return true;
        }

        // Órdago como respuesta a un envite
        // o reenvido de la pareja contraria.
        if (Estado == EstadoEnvite.PendienteRespuesta &&
            PuedeResponder(pareja))
        {
            // Guardamos la última apuesta ofrecida.
            // Si rechazan el órdago, se ganará
            // ese importe anterior.
            TantosAnteriores = TantosApostados;

            ParejaEnvidante = pareja;

            Estado = EstadoEnvite.OrdagoPendiente;

            return true;
        }

        return false;
    }

    // ========================================
    // QUIERO
    // ========================================

    public bool Quiero(int pareja)
    {
        if (!PuedeResponder(pareja))
            return false;

        if (Estado == EstadoEnvite.OrdagoPendiente)
        {
            // Aquí debe resolverse inmediatamente
            // el lance comparando las cuatro manos.
            Estado = EstadoEnvite.OrdagoAceptado;
        }
        else
        {
            Estado = EstadoEnvite.Aceptado;
        }

        return true;
    }

    // ========================================
    // NO QUIERO
    // ========================================

    public bool NoQuiero(int pareja)
    {
        if (!PuedeResponder(pareja))
            return false;

        ParejaGanadoraNegada = ParejaEnvidante;

        TantosPorNegada =
            TantosAnteriores > 0
                ? TantosAnteriores
                : 1;

        Estado = EstadoEnvite.Rechazado;

        return true;
    }

    // ========================================
    // VALIDACIONES
    // ========================================

    private bool PuedeResponder(int pareja)
    {
        bool pendiente =
            Estado == EstadoEnvite.PendienteRespuesta ||
            Estado == EstadoEnvite.OrdagoPendiente;

        return pendiente &&
               ParejaValida(pareja) &&
               pareja != ParejaEnvidante;
    }

    private bool ParejaValida(int pareja)
    {
        return pareja == 0 || pareja == 1;
    }

    // ========================================
    // REINICIO PARA OTRO LANCE
    // ========================================

    public void Reiniciar()
    {
        Estado = EstadoEnvite.SinApuesta;
        ParejaEnvidante = -1;
        TantosApostados = 0;
        TantosAnteriores = 0;
        ParejaGanadoraNegada = -1;
        TantosPorNegada = 0;
    }
}
