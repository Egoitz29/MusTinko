
using UnityEngine;

public class IAEnvitesPorSenalesMus : MonoBehaviour
{
    public enum Accion
    {
        Envidar,
        Querer,
        Reenvidar
    }

    [Header("Receptores 0, 1, 2 y 3")]
    [SerializeField]
    private ReceptorSenalesMus[] receptores =
        new ReceptorSenalesMus[4];

    [Header("Influencia de las señas")]
    [Range(0f, 0.5f)]
    [SerializeField] private float aumentoEnvidar = 0.25f;

    [Range(0f, 0.5f)]
    [SerializeField] private float aumentoQuerer = 0.15f;

    [Range(0f, 0.5f)]
    [SerializeField] private float aumentoReenvidar = 0.08f;

    public float AjustarProbabilidad(
        int jugador,
        GestorFasesMus.Fase fase,
        Accion accion,
        float probabilidadBase)
    {
        float probabilidad = Mathf.Clamp01(probabilidadBase);

        if (jugador < 1 || jugador > 3)
            return probabilidad;

        if (receptores == null ||
            jugador >= receptores.Length ||
            receptores[jugador] == null)
            return probabilidad;

        if (!TieneInformacionUtil(jugador, fase))
            return probabilidad;

        float aumento = 0f;

        switch (accion)
        {
            case Accion.Envidar:
                aumento = aumentoEnvidar;
                break;

            case Accion.Querer:
                aumento = aumentoQuerer;
                break;

            case Accion.Reenvidar:
                aumento = aumentoReenvidar;
                break;
        }

        return Mathf.Clamp01(probabilidad + aumento);
    }

    private bool TieneInformacionUtil(
        int jugador,
        GestorFasesMus.Fase fase)
    {
        var receptor = receptores[jugador];

        ControlSenalesMus.TipoSenal tipo;

        switch (fase)
        {
            case GestorFasesMus.Fase.Grande:
                tipo = ControlSenalesMus.TipoSenal.DosReyes;
                break;

            case GestorFasesMus.Fase.Chica:
                tipo = ControlSenalesMus.TipoSenal.DosAses;
                break;

            case GestorFasesMus.Fase.Pares:
                tipo = ControlSenalesMus.TipoSenal.Duples;
                break;

            case GestorFasesMus.Fase.JuegoPunto:
                tipo = ControlSenalesMus.TipoSenal.TreintaYUna;
                break;

            default:
                return false;
        }

        return receptor.ConoceSenal(tipo);
    }

    [ContextMenu("Diagnosticar influencia de señas")]
    private void Diagnosticar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Ejecuta el diagnostico durante Play.");
            return;
        }

        var fases = new[]
        {
            GestorFasesMus.Fase.Grande,
            GestorFasesMus.Fase.Chica,
            GestorFasesMus.Fase.Pares,
            GestorFasesMus.Fase.JuegoPunto
        };

        for (int jugador = 1; jugador <= 3; jugador++)
        {
            foreach (var fase in fases)
            {
                float resultado = AjustarProbabilidad(
                    jugador, fase, Accion.Envidar, 0.5f
                );

                if (resultado <= 0.5f)
                    continue;

                Debug.Log(
                    "[IA SENAS] Jugador " + jugador +
                    " | " + fase +
                    " | Envidar: 0.50 -> " +
                    resultado.ToString("F2"),
                    this
                );
            }
        }
    }
}
