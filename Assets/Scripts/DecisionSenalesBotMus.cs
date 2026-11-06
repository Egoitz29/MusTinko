
using System.Collections;
using UnityEngine;

public class DecisionSenalesBotMus : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GestorFasesMus gestorFases;
    [SerializeField] private SenalesPorCartasMus senales;
    [SerializeField] private Camera camaraJugador;
    [SerializeField] private Transform puntoCara;

    [Header("Inteligencia del bot")]
    [Range(0f, 1f)]
    [SerializeField] private float probabilidadSenal = 0.35f;

    [Min(0f)]
    [SerializeField] private float retrasoSenal = 1.5f;

    [Header("Discrecion")]
    [SerializeField] private bool esperarMirada = true;

    [Range(0f, 0.4f)]
    [SerializeField] private float margenPantalla = 0.2f;

    [Min(1f)]
    [SerializeField] private float tiempoMaximoEspera = 15f;

    private GestorFasesMus.Fase faseAnterior =
        (GestorFasesMus.Fase)(-1);

    private bool decisionTomadaEstaMano;
    private Coroutine rutina;

    private void Awake()
    {
        if (senales == null)
            senales = GetComponent<SenalesPorCartasMus>();

        if (camaraJugador == null)
            camaraJugador = Camera.main;
    }

    private void Update()
    {
        if (gestorFases == null || senales == null)
            return;

        var fase = gestorFases.FaseActual;

        if (fase == faseAnterior)
            return;

        faseAnterior = fase;

        if (fase == GestorFasesMus.Fase.FinMano)
        {
            CancelarRutina();
            decisionTomadaEstaMano = false;
            return;
        }

        if (fase == GestorFasesMus.Fase.Grande &&
            !decisionTomadaEstaMano)
        {
            decisionTomadaEstaMano = true;
            CancelarRutina();
            rutina = StartCoroutine(DecidirSenal());
        }
    }

    private IEnumerator DecidirSenal()
    {
        yield return new WaitForSeconds(retrasoSenal);

        var elegida = ElegirSenalPrioritaria();

        if (!elegida.HasValue)
        {
            Debug.Log("[MUS SENAS] Sin combinaciones para senalar.");
            rutina = null;
            yield break;
        }

        if (Random.value >= probabilidadSenal)
        {
            Debug.Log("[MUS SENAS] El bot decide guardar silencio.");
            rutina = null;
            yield break;
        }

        Debug.Log(
            "[MUS SENAS] Sena seleccionada: " + elegida.Value
        );

        float tiempo = 0f;

        while (tiempo < tiempoMaximoEspera)
        {
            if (gestorFases == null ||
                !EsFaseDeLances(gestorFases.FaseActual))
            {
                rutina = null;
                yield break;
            }

            if (!esperarMirada || CaraEnPantalla())
            {
                if (senales.PuedeHacerSenal(elegida.Value))
                {
                    if (senales.SolicitarSenal(elegida.Value))
                    {
                        Debug.Log(
                            "[MUS SENAS] Sena ejecutada: " +
                            elegida.Value
                        );
                    }
                }

                rutina = null;
                yield break;
            }

            tiempo += Time.deltaTime;
            yield return null;
        }

        Debug.Log(
            "[MUS SENAS] No se hizo la sena: " +
            "el jugador no miro al personaje a tiempo."
        );

        rutina = null;
    }

    private ControlSenalesMus.TipoSenal? ElegirSenalPrioritaria()
    {
        var prioridades = new[]
        {
            ControlSenalesMus.TipoSenal.TreintaYUna,
            ControlSenalesMus.TipoSenal.Duples,
            ControlSenalesMus.TipoSenal.DosReyes,
            ControlSenalesMus.TipoSenal.DosAses
        };

        foreach (var tipo in prioridades)
        {
            if (senales.PuedeHacerSenal(tipo))
                return tipo;
        }

        return null;
    }

    private bool CaraEnPantalla()
    {
        if (camaraJugador == null || puntoCara == null)
            return false;

        Vector3 pantalla =
            camaraJugador.WorldToViewportPoint(puntoCara.position);

        return pantalla.z > 0f &&
               pantalla.x >= margenPantalla &&
               pantalla.x <= 1f - margenPantalla &&
               pantalla.y >= margenPantalla &&
               pantalla.y <= 1f - margenPantalla;
    }

    private bool EsFaseDeLances(GestorFasesMus.Fase fase)
    {
        return fase == GestorFasesMus.Fase.Grande ||
               fase == GestorFasesMus.Fase.Chica ||
               fase == GestorFasesMus.Fase.Pares ||
               fase == GestorFasesMus.Fase.JuegoPunto;
    }

    [ContextMenu("Probar decision inmediata")]
    private void ProbarDecision()
    {
        if (!Application.isPlaying || senales == null)
            return;

        var elegida = ElegirSenalPrioritaria();

        if (elegida.HasValue)
            senales.SolicitarSenal(elegida.Value);
        else
            Debug.Log("[MUS SENAS] No hay senas disponibles.");
    }

    private void CancelarRutina()
    {
        if (rutina == null)
            return;

        StopCoroutine(rutina);
        rutina = null;
    }

    private void OnDisable()
    {
        CancelarRutina();
        faseAnterior = (GestorFasesMus.Fase)(-1);
        decisionTomadaEstaMano = false;
    }
}
