using UnityEngine;
using UnityEngine.UI;

// FIN DE PARTIDA
//  En la pantalla final (tanteo y mesa) salen dos botones nuevos:
//   - OTRA PARTIDA: todos vuelven a la sala, cada uno en su silla; podeis cambiaros y el host pulsa EMPEZAR.
//   - SALIR AL MENU: te vas al menu principal (si eres el que creo la sala, se cierra para todos).
//  Si nadie pulsa nada, el servidor devuelve a todos a la sala al cabo de un rato (PartidaOnlineMus.volverASalaSolo).
public partial class VistaMesaOnlineMus
{
    private Button botonOtraPartidaUI, botonSalirFinUI;              // en la hoja del tanteo
    private Button botonOtraPartidaMesaUI, botonSalirFinMesaUI;      // abajo, cuando se ve la mesa
    private bool empezadaVista;

    // Botones de la hoja del tanteo (se rehace cada mano).
    private void CrearBotonesFinTanteo(Transform fila)
    {
        botonOtraPartidaUI = Boton(fila, "verde", "OTRA PARTIDA", new Vector2(290, 90), PedirOtraPartida, 30);
        botonSalirFinUI = Boton(fila, "rojo", "SALIR AL MENU", new Vector2(220, 90), SalirDeLaSala, 26);
    }

    // Botones de abajo cuando se ve la mesa.
    private void CrearBotonesFinMesa(Transform fila)
    {
        botonOtraPartidaMesaUI = Boton(fila, "verde", "OTRA PARTIDA", new Vector2(260, 96), PedirOtraPartida, 30);
        botonSalirFinMesaUI = Boton(fila, "rojo", "SALIR AL MENU", new Vector2(200, 96), SalirDeLaSala, 24);
    }

    private void PedirOtraPartida() => partida.PedirOtraPartida();

    private void ActualizarBotonesFin()
    {
        bool acabada = partida.PartidaAcabada;

        if (botonOtraPartidaUI != null) botonOtraPartidaUI.gameObject.SetActive(acabada);
        if (botonSalirFinUI != null) botonSalirFinUI.gameObject.SetActive(acabada);
        if (botonOtraPartidaMesaUI != null) botonOtraPartidaMesaUI.gameObject.SetActive(acabada);
        if (botonSalirFinMesaUI != null) botonSalirFinMesaUI.gameObject.SetActive(acabada);
    }

    // Cuando la partida deja de estar empezada (todos a la sala, o te has ido al menu), se limpia la mesa:
    // fuera la vista de revision, el tanteo, las cartas de la mesa y las de tu mano.
    private void ActualizarVueltaASala()
    {
        bool empezada = partida.IsSpawned && partida.PartidaEmpezada;

        if (empezadaVista && !empezada)
            LimpiarMesaParaSala();

        empezadaVista = empezada;
    }

    // Al acabar cada mano: que lances ha ganado tu pareja con las cartas y tu mano (para el perfil y los logros).
    private void RegistrarManoEnPerfil(int[] todas)
    {
        int local = partida.PuestoLocal;

        if (local < 0 || DemoActiva || tabla == null || todas == null || todas.Length < 16)
            return;

        bool[] ganados = new bool[5];

        for (int i = 0; i < tabla.Filas.Length; i++)
        {
            TablaTanteoMus.Fila f = tabla.Filas[i];

            if (f == null || f.Ganador != 0)
                continue;

            int indice = i < 3 ? i : f.Lance == "PUNTO" ? 4 : 3;
            ganados[indice] = true;
        }

        int[] mias = { todas[local * 4], todas[local * 4 + 1], todas[local * 4 + 2], todas[local * 4 + 3] };
        PerfilMus.RegistrarMano(ganados, CartasMus.Evaluar(mias));
    }

    private void LimpiarMesaParaSala()
    {
        cola.Clear();
        ManoNuevaComoEspectador();
        cartasLocalesMostradas = new[] { -1, -1, -1, -1 };
        tabla = null;
        tablaMostrada = null;
        puntosVistos0 = puntosVistos1 = 0;

        if (panelTanteoUI != null) panelTanteoUI.SetActive(false);
        if (botonesMesaUI != null) botonesMesaUI.SetActive(false);

        ActivarVistaJugador(true);
    }
}
