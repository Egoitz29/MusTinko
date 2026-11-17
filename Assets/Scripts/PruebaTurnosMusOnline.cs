
using Unity.Netcode;
using UnityEngine;

public class PruebaTurnosMusOnline : NetworkBehaviour
{
    private readonly NetworkVariable<int> estado = new(0);
    private readonly NetworkVariable<int> turno = new(-1);
    private readonly NetworkVariable<int> respuestaHost = new(-1);
    private readonly NetworkVariable<int> respuestaCliente = new(-1);
    private readonly NetworkVariable<ulong> idCliente = new(ulong.MaxValue);

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        NetworkManager.OnClientConnectedCallback += ClienteConectado;
        NetworkManager.OnClientDisconnectCallback += ClienteDesconectado;

        PrepararJugadores();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer || NetworkManager == null)
            return;

        NetworkManager.OnClientConnectedCallback -= ClienteConectado;
        NetworkManager.OnClientDisconnectCallback -= ClienteDesconectado;
    }

    private void ClienteConectado(ulong id)
    {
        PrepararJugadores();
    }

    private void ClienteDesconectado(ulong id)
    {
        PrepararJugadores();
    }

    private void PrepararJugadores()
    {
        if (!IsServer)
            return;

        if (NetworkManager.ConnectedClientsIds.Count != 2)
        {
            estado.Value = 0;
            turno.Value = -1;
            idCliente.Value = ulong.MaxValue;
            return;
        }

        ulong segundoJugador = ulong.MaxValue;

        foreach (ulong id in NetworkManager.ConnectedClientsIds)
        {
            if (id != Unity.Netcode.NetworkManager.ServerClientId)
                segundoJugador = id;
        }

        if (segundoJugador == ulong.MaxValue)
            return;

        idCliente.Value = segundoJugador;
        ReiniciarRonda();
    }

    private void ReiniciarRonda()
    {
        if (!IsServer || NetworkManager.ConnectedClientsIds.Count != 2)
            return;

        respuestaHost.Value = -1;
        respuestaCliente.Value = -1;
        turno.Value = 0;
        estado.Value = 1;

        Debug.Log("[MUS ONLINE] Nueva ronda. Empieza el host.");
    }

    [Rpc(SendTo.Server)]
    private void DecidirMusRpc(bool cortar, RpcParams parametros = default)
    {
        if (estado.Value != 1)
            return;

        ulong emisor = parametros.Receive.SenderClientId;

        int jugador = -1;

        if (emisor == Unity.Netcode.NetworkManager.ServerClientId)
            jugador = 0;
        else if (emisor == idCliente.Value)
            jugador = 1;

        if (jugador != turno.Value)
        {
            Debug.LogWarning("[MUS ONLINE] Accion rechazada: fuera de turno.");
            return;
        }

        int decision = cortar ? 2 : 1;

        if (jugador == 0)
            respuestaHost.Value = decision;
        else
            respuestaCliente.Value = decision;

        Debug.Log(
            "[MUS ONLINE] Jugador " + jugador +
            (cortar ? " dice NO HAY MUS" : " dice MUS")
        );

        if (cortar)
        {
            turno.Value = -1;
            estado.Value = 2;
        }
        else if (jugador == 0)
        {
            turno.Value = 1;
        }
        else
        {
            turno.Value = -1;
            estado.Value = 2;
        }
    }

    private static string TextoRespuesta(int valor)
    {
        if (valor == 1)
            return "MUS";

        if (valor == 2)
            return "NO HAY MUS";

        return "Sin decidir";
    }

    private void OnGUI()
    {
        float ancho = Mathf.Min(350f, Screen.width - 40f);
        float x = Mathf.Max(20f, Screen.width - ancho - 20f);

        GUILayout.BeginArea(
            new Rect(x, 20, ancho, 260),
            "MUS TINKO - TURNOS ONLINE",
            GUI.skin.window
        );

        if (!IsSpawned || estado.Value == 0)
        {
            GUILayout.Label("Esperando a dos jugadores...");
            GUILayout.EndArea();
            return;
        }

        GUILayout.Label("HOST: " + TextoRespuesta(respuestaHost.Value));
        GUILayout.Label("CLIENTE: " + TextoRespuesta(respuestaCliente.Value));

        if (estado.Value == 1)
        {
            GUILayout.Label(
                turno.Value == 0 ? "Turno del HOST" : "Turno del CLIENTE"
            );

            int jugadorLocal = -1;

            if (NetworkManager.LocalClientId ==
                Unity.Netcode.NetworkManager.ServerClientId)
                jugadorLocal = 0;
            else if (NetworkManager.LocalClientId == idCliente.Value)
                jugadorLocal = 1;

            if (jugadorLocal == turno.Value)
            {
                if (GUILayout.Button("MUS", GUILayout.Height(35)))
                    DecidirMusRpc(false);

                if (GUILayout.Button("NO HAY MUS", GUILayout.Height(35)))
                    DecidirMusRpc(true);
            }
            else
            {
                GUILayout.Label("Esperando la decision del otro jugador...");
            }
        }
        else
        {
            if (respuestaHost.Value == 2)
                GUILayout.Label("El HOST corta Mus. Tocaria GRANDE.");
            else if (respuestaCliente.Value == 2)
                GUILayout.Label("El CLIENTE corta Mus. Tocaria GRANDE.");
            else
                GUILayout.Label("Ambos dicen MUS. Tocaria DESCARTE.");

            if (IsServer)
            {
                if (GUILayout.Button("NUEVA RONDA", GUILayout.Height(35)))
                    ReiniciarRonda();
            }
        }

        GUILayout.EndArea();
    }
}
