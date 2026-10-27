using TMPro;
using UnityEngine;

public class PantallaVictoriaMus : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private MarcadorMus marcador;

    [Header("Panel de victoria (dentro de Canvas)")]
    [SerializeField] private GameObject panelVictoria;
    [SerializeField] private TMP_Text textoTitulo;
    [SerializeField] private TMP_Text textoDetalle;

    private bool panelMostrado;

    private void Start()
    {
        panelMostrado = false;

        if (panelVictoria != null)
            panelVictoria.SetActive(false);
    }

    private void Update()
    {
        if (marcador == null)
            return;

        bool partidaTerminada = marcador.JuegoTerminado;

        if (partidaTerminada && !panelMostrado)
        {
            panelMostrado = true;

            if (textoTitulo != null)
            {
                textoTitulo.text = marcador.ParejaGanadoraJuego == 0
                    ? "¡VICTORIA DE TU PAREJA!"
                    : "¡VICTORIA DE LOS RIVALES!";
            }

            if (textoDetalle != null)
            {
                textoDetalle.text =
                    "TU PAREJA: " + marcador.PuntosPareja02 +
                    "     RIVALES: " + marcador.PuntosPareja13 +
                    "\n" + marcador.MotivoVictoria;
            }

            if (panelVictoria != null)
                panelVictoria.SetActive(true);
        }
        else if (!partidaTerminada && panelMostrado)
        {
            panelMostrado = false;

            if (panelVictoria != null)
                panelVictoria.SetActive(false);
        }
    }
}
