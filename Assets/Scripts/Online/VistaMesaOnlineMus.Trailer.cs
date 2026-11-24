using UnityEngine;

// Lo que necesita el director del trailer (TrailerMus) para manejar la mesa:
//  - ModoTrailer: se esconde toda la interfaz menos los bocadillos de los personajes.
//  - Arrancar/parar la demo (4 bots jugando solos) y elegir su camara.
//  - Donde esta la mesa y la cabeza de cada personaje, y la camara del juego.
public partial class VistaMesaOnlineMus
{
    public bool ModoTrailer { get; set; }

    public bool DemoEnMarcha => DemoActiva;
    public Vector3 CentroDeLaMesa => CentroMesa();
    public Vector3 CabezaDelPuesto(int puesto) => CabezaModeloDemo(puesto);
    public Camera CamaraDelJuego => camaraJugador != null ? camaraJugador : Camera.main;

    public void EmpezarDemoParaTrailer()
    {
        if (!DemoActiva)
            EmpezarDemoDesdeMenu();
    }

    public void SalirDemoDelTrailer()
    {
        if (DemoActiva)
            SalirDeLaDemo();
    }

    // true: se ve como un jugador sentado (con sus cartas); false: camara de cine que va a quien habla.
    public void VistaDemoJugador(bool jugador) => vistaJugadorDemo = jugador;

    // Interfaz durante el trailer: solo los bocadillos ("¡ORDAGO!", "ENVIDO 5"...).
    private void ActualizarInterfazTrailer()
    {
        for (int i = 0; i < raizUI.childCount; i++)
        {
            Transform hijo = raizUI.GetChild(i);
            bool bocadillo = hijo.name.StartsWith("Bocadillo_");

            if (!bocadillo && hijo.gameObject.activeSelf)
                hijo.gameObject.SetActive(false);
        }

        ActualizarCamaraRetratos(false);
        ActualizarBocadillosUI(partida.IsSpawned && partida.PartidaEmpezada && !enRevision);
    }
}
