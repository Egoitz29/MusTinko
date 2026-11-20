using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class PruebaConexionMus : MonoBehaviour
{
    private const string ClaveIp = "MusTinko_UltimaIp";

    [SerializeField] private ushort puerto = 7777;

    private NetworkManager red;
    private string estado = "Sin conectar";
    private string ipHost = "127.0.0.1";
    private string ipLocal = "";

    private void Start()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        try
        {
            ipHost = PlayerPrefs.GetString(ClaveIp, "127.0.0.1");
        }
        catch
        {
            ipHost = "127.0.0.1";
        }

        ipLocal = ObtenerIpLocal();
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
        EscalaGUIMus.Aplicar();

        if (red != null && red.IsListening)
        {
            string texto = red.IsHost
                ? "HOST  |  conectados: " + red.ConnectedClients.Count + (string.IsNullOrEmpty(ipLocal) ? "" : "  |  tu IP: " + ipLocal)
                : (red.IsConnectedClient ? "CLIENTE  |  conectado" : "CLIENTE  |  conectando...");

            GUI.Box(new Rect(20, EscalaGUIMus.Alto - 40, 420, 30), texto);

            if (GUI.Button(new Rect(450, EscalaGUIMus.Alto - 40, 110, 30), "SALIR"))
                red.Shutdown();

            return;
        }

        float ancho = Mathf.Min(460f, EscalaGUIMus.Ancho - 40f);
        GUILayout.BeginArea(new Rect(20, 20, ancho, 330), "MUS TINKO", GUI.skin.window);

        GUILayout.Label("Estado: " + estado);

        if (red == null)
        {
            GUILayout.EndArea();
            return;
        }

        if (GUILayout.Button("CREAR PARTIDA (host, el resto bots)", GUILayout.Height(54)))
            IniciarHost();

        if (!string.IsNullOrEmpty(ipLocal))
            GUILayout.Label("Tu IP en esta red: " + ipLocal + "   (los demas se unen con ella)");

        GUILayout.Space(10);
        GUILayout.Label("Unirse a la partida de otro (misma WiFi):");

        GUILayout.BeginHorizontal();
        GUILayout.Label("IP del host:", GUILayout.Width(90));
        ipHost = GUILayout.TextField(ipHost, 40, GUILayout.Height(40));
        GUILayout.EndHorizontal();

        if (GUILayout.Button("UNIRSE", GUILayout.Height(54)))
            IniciarCliente();

        GUILayout.EndArea();
    }

    private UnityTransport Transporte => red != null ? red.GetComponent<UnityTransport>() : null;

    private void IniciarHost()
    {
        UnityTransport transporte = Transporte;

        if (transporte != null)
            transporte.SetConnectionData("127.0.0.1", puerto, "0.0.0.0");

        if (red.StartHost())
        {
            estado = "Host iniciado";
            Debug.Log("[MUS ONLINE] Host iniciado. IP local: " + ipLocal + ":" + puerto);
        }
        else
        {
            estado = "Error al iniciar host";
            Debug.LogError("[MUS ONLINE] No se pudo iniciar el host.");
        }
    }

    private void IniciarCliente()
    {
        string ip = string.IsNullOrWhiteSpace(ipHost) ? "127.0.0.1" : ipHost.Trim();
        UnityTransport transporte = Transporte;

        if (transporte != null)
            transporte.SetConnectionData(ip, puerto);

        try
        {
            PlayerPrefs.SetString(ClaveIp, ip);
            PlayerPrefs.Save();
        }
        catch
        {
        }

        if (red.StartClient())
        {
            estado = "Conectando a " + ip + "...";
            Debug.Log("[MUS ONLINE] Intentando conectar a " + ip + ":" + puerto);
        }
        else
        {
            estado = "Error al iniciar cliente";
            Debug.LogError("[MUS ONLINE] No se pudo iniciar el cliente.");
        }
    }

    private static string ObtenerIpLocal()
    {
        try
        {
            foreach (NetworkInterface interfaz in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (interfaz.OperationalStatus != OperationalStatus.Up ||
                    interfaz.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (UnicastIPAddressInformation direccion in interfaz.GetIPProperties().UnicastAddresses)
                {
                    IPAddress ip = direccion.Address;

                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                        return ip.ToString();
                }
            }
        }
        catch
        {
        }

        try
        {
            foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    return ip.ToString();
        }
        catch
        {
        }

        return "";
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
            estado = "Desconectado. Revisa la IP y que esteis en la misma WiFi.";
    }

    private void OnDestroy()
    {
        if (red == null)
            return;

        red.OnClientConnectedCallback -= ClienteConectado;
        red.OnClientDisconnectCallback -= ClienteDesconectado;
    }
}
