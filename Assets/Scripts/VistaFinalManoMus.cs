
using UnityEngine;

public class VistaFinalManoMus : MonoBehaviour
{
    [Header("Gestores")]
    [SerializeField] private GestorFasesMus gestorFases;
    [SerializeField] private MarcadorMus marcador;

    [Header("Camaras")]
    [SerializeField] private Camera camaraJugador;
    [SerializeField] private Camera camaraRevision;
    [SerializeField] private CamaraMus controlCamaraJugador;

    [Header("Elementos que se ocultan al revisar")]
    [SerializeField] private GameObject panelCartas;
    [SerializeField] private GameObject panelEnvites;
    [SerializeField] private GameObject botonRepartirPrueba;
    [SerializeField] private GameObject botonCentrarCamara;
    [SerializeField] private GameObject textoEstadoApuestas;

    [Header("Boton para continuar")]
    [SerializeField] private GameObject botonSiguienteMano;

    private bool enRevision;

    private void Start()
    {
        // Al iniciar Play, siempre comenzamos
        // utilizando la camara del jugador.
        ActivarVistaJugador(false);
    }

    private void LateUpdate()
    {
        if (gestorFases == null)
            return;

        bool manoTerminada =
            gestorFases.FaseActual ==
            GestorFasesMus.Fase.FinMano;

        // Entramos en revision cuando termina la mano.
        if (manoTerminada && !enRevision)
        {
            ActivarVistaRevision();
        }

        // Volvemos al jugador cuando comienza otra mano.
        else if (!manoTerminada && enRevision)
        {
            ActivarVistaJugador(true);
        }
    }

    // ==========================================
    // VISTA NORMAL DEL JUGADOR
    // ==========================================

    private void ActivarVistaJugador(bool recentrar)
    {
        enRevision = false;

        // Activamos nuestra camara principal.
        if (camaraJugador != null)
            camaraJugador.enabled = true;

        // Apagamos la camara de revision.
        if (camaraRevision != null)
            camaraRevision.enabled = false;

        // Reactivamos el control tactil.
        if (controlCamaraJugador != null)
        {
            if (recentrar)
                controlCamaraJugador.RecentrarVista();

            controlCamaraJugador.enabled = true;
        }

        // Recuperamos la interfaz normal.
        if (panelCartas != null)
            panelCartas.SetActive(true);

        if (botonRepartirPrueba != null)
            botonRepartirPrueba.SetActive(true);

        if (botonCentrarCamara != null)
            botonCentrarCamara.SetActive(true);

        if (textoEstadoApuestas != null)
            textoEstadoApuestas.SetActive(true);

        // El panel de apuestas lo activara
        // GestorEnvitesMus cuando comience un lance.
        if (panelEnvites != null)
            panelEnvites.SetActive(false);

        // Ocultamos el boton de continuar.
        if (botonSiguienteMano != null)
            botonSiguienteMano.SetActive(false);

        if (recentrar)
            Debug.Log("VISTA NORMAL: nueva mano.");
    }

    // ==========================================
    // VISTA DE REVISION DE LA MANO
    // ==========================================

    private void ActivarVistaRevision()
    {
        enRevision = true;

        // Apagamos la camara del jugador.
        if (camaraJugador != null)
            camaraJugador.enabled = false;

        // Detenemos el movimiento de mirada.
        if (controlCamaraJugador != null)
            controlCamaraJugador.enabled = false;

        // Mostramos la camara superior.
        if (camaraRevision != null)
            camaraRevision.enabled = true;

        // Ocultamos los controles de partida.
        if (panelCartas != null)
            panelCartas.SetActive(false);

        if (panelEnvites != null)
            panelEnvites.SetActive(false);

        if (botonRepartirPrueba != null)
            botonRepartirPrueba.SetActive(false);

        if (botonCentrarCamara != null)
            botonCentrarCamara.SetActive(false);

        if (textoEstadoApuestas != null)
            textoEstadoApuestas.SetActive(false);

        // Un ordago aceptado termina el juego.
        // No debemos ofrecer otra mano normal.
        bool finPorOrdago =
            marcador != null &&
            marcador.JuegoTerminado;

        if (botonSiguienteMano != null)
            botonSiguienteMano.SetActive(!finPorOrdago);

        Debug.Log(
            "VISTA DE REVISION ACTIVADA. " +
            "Cartas descubiertas sobre la mesa."
        );
    }
}
