
using Unity.Netcode;
using UnityEngine;

public class PruebaConexionMus : MonoBehaviour
{
    private NetworkManager red;
    private string estado = "Sin conectar";

    private void Start()
    {
        red = NetworkManager.Singleton;

        if (red == null)
        {
            estado = "ERROR: Falta NetworkManager";
            Debug.LogError(estado);
            return;
        }

        red.OnClientConnectedCallback += ClienteConectado;
        red.OnClientDisconnectCallback += ClienteDesconectado;
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(
            new Rect(20, 20, 350, 210),
            "MUS TINKO - MULTIJUGADOR",
            GUI.skin.window
        );

        GUILayout.Label("Estado: " + estado);

        if (red == null)
        {
            GUILayout.EndArea();
            return;
        }

        if (!red.IsListening)
        {
            if (GUILayout.Button("INICIAR HOST", GUILayout.Height(40)))
                IniciarHost();

            if (GUILayout.Button("CONECTAR CLIENTE", GUILayout.Height(40)))
                IniciarCliente();
        }
        else
        {
            GUILayout.Label(red.IsHost ? "ROL: HOST" : "ROL: CLIENTE");

            if (red.IsHost)
            {
                GUILayout.Label(
                    "Jugadores conectados: " + red.ConnectedClients.Count
                );
            }
            else
            {
                GUILayout.Label(
                    red.IsConnectedClient ? "Conexion establecida" : "Conectando..."
                );
            }
        }

        GUILayout.EndArea();
    }

    private void IniciarHost()
    {
        if (red.StartHost())
        {
            estado = "Host iniciado";
            Debug.Log("[MUS ONLINE] Host iniciado correctamente.");
        }
        else
        {
            estado = "Error al iniciar host";
            Debug.LogError("[MUS ONLINE] No se pudo iniciar el host.");
        }
    }

    private void IniciarCliente()
    {
        if (red.StartClient())
        {
            estado = "Conectando...";
            Debug.Log("[MUS ONLINE] Intentando conectar al host.");
        }
        else
        {
            estado = "Error al iniciar cliente";
            Debug.LogError("[MUS ONLINE] No se pudo iniciar el cliente.");
        }
    }

    private void ClienteConectado(ulong id)
    {
        if (red.IsHost)
        {
            estado = "Jugadores conectados: " + red.ConnectedClients.Count;
            Debug.Log("[MUS ONLINE] Jugador conectado. ID: " + id);
        }
        else if (id == red.LocalClientId)
        {
            estado = "Conectado al host";
            Debug.Log("[MUS ONLINE] Conexion al host establecida.");
        }
    }

    private void ClienteDesconectado(ulong id)
    {
        Debug.LogWarning("[MUS ONLINE] Jugador desconectado: " + id);

        if (red.IsHost)
            estado = "Jugadores conectados: " + red.ConnectedClients.Count;
        else
            estado = "Cliente desconectado";
    }

    private void OnDestroy()
    {
        if (red == null)
            return;

        red.OnClientConnectedCallback -= ClienteConectado;
        red.OnClientDisconnectCallback -= ClienteDesconectado;
    }
}
