using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PruebaTurnosMusOnline : NetworkBehaviour
{
    private const int JugadoresNecesarios = 4;
    private const int SinDecidir = -1;
    private const int DiceMus = 1;
    private const int DiceNoHayMus = 2;

    private const int EstadoEsperando = 0;
    private const int EstadoDecidiendo = 1;
    private const int EstadoTerminado = 2;

    private static readonly string[] NombresPuesto = { "LILA", "MOSTAZA", "MENTA", "CORAL" };

    private readonly NetworkVariable<int> estado = new(EstadoEsperando);
    private readonly NetworkVariable<int> turno = new(-1);
    private readonly NetworkVariable<int> jugadorQueCorta = new(-1);
    private readonly NetworkVariable<int> conectados = new(0);

    private readonly NetworkVariable<int> respuesta0 = new(SinDecidir);
    private readonly NetworkVariable<int> respuesta1 = new(SinDecidir);
    private readonly NetworkVariable<int> respuesta2 = new(SinDecidir);
    private readonly NetworkVariable<int> respuesta3 = new(SinDecidir);

    private readonly NetworkVariable<ulong> idJugador1 = new(ulong.MaxValue);
    private readonly NetworkVariable<ulong> idJugador2 = new(ulong.MaxValue);
    private readonly NetworkVariable<ulong> idJugador3 = new(ulong.MaxValue);

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        NetworkManager.OnClientConnectedCallback += ClienteConectado;
        NetworkManager.OnClientDisconnectCallback += ClienteDesconectado;

        PrepararJugadores(ulong.MaxValue);
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
        Debug.Log("[MUS ONLINE] Conexion recibida. ID: " + id);
        PrepararJugadores(ulong.MaxValue);
    }

    private void ClienteDesconectado(ulong id)
    {
        Debug.LogWarning("[MUS ONLINE] Desconexion. ID: " + id);
        PrepararJugadores(id);
    }

    private void PrepararJugadores(ulong idExcluido)
    {
        if (!IsServer)
            return;

        List<ulong> clientes = new();

        foreach (ulong id in NetworkManager.ConnectedClientsIds)
        {
            if (id != Unity.Netcode.NetworkManager.ServerClientId && id != idExcluido)
                clientes.Add(id);
        }

        clientes.Sort();
        conectados.Value = clientes.Count + 1;

        if (clientes.Count != JugadoresNecesarios - 1)
        {
            idJugador1.Value = ulong.MaxValue;
            idJugador2.Value = ulong.MaxValue;
            idJugador3.Value = ulong.MaxValue;
            estado.Value = EstadoEsperando;
            turno.Value = -1;

            Debug.Log(
                "[MUS ONLINE] Esperando jugadores: " +
                conectados.Value + "/" + JugadoresNecesarios
            );
            return;
        }

        if (estado.Value != EstadoEsperando &&
            idJugador1.Value == clientes[0] &&
            idJugador2.Value == clientes[1] &&
            idJugador3.Value == clientes[2])
            return;

        idJugador1.Value = clientes[0];
        idJugador2.Value = clientes[1];
        idJugador3.Value = clientes[2];

        Debug.Log(
            "[MUS ONLINE] Puestos asignados. 0=host, 1=" + clientes[0] +
            ", 2=" + clientes[1] + ", 3=" + clientes[2]
        );

        ReiniciarRonda();
    }

    private void ReiniciarRonda()
    {
        if (!IsServer || idJugador3.Value == ulong.MaxValue)
            return;

        respuesta0.Value = SinDecidir;
        respuesta1.Value = SinDecidir;
        respuesta2.Value = SinDecidir;
        respuesta3.Value = SinDecidir;
        jugadorQueCorta.Value = -1;
        turno.Value = 0;
        estado.Value = EstadoDecidiendo;

        Debug.Log("[MUS ONLINE] Nueva ronda. Empieza el jugador 0.");
    }

    private int ObtenerPuesto(ulong id)
    {
        if (id == Unity.Netcode.NetworkManager.ServerClientId)
            return 0;

        if (id == ulong.MaxValue)
            return -1;

        if (id == idJugador1.Value)
            return 1;

        if (id == idJugador2.Value)
            return 2;

        if (id == idJugador3.Value)
            return 3;

        return -1;
    }

    private int ObtenerRespuesta(int jugador)
    {
        return jugador switch
        {
            0 => respuesta0.Value,
            1 => respuesta1.Value,
            2 => respuesta2.Value,
            3 => respuesta3.Value,
            _ => SinDecidir
        };
    }

    private void GuardarRespuesta(int jugador, int decision)
    {
        switch (jugador)
        {
            case 0: respuesta0.Value = decision; break;
            case 1: respuesta1.Value = decision; break;
            case 2: respuesta2.Value = decision; break;
            case 3: respuesta3.Value = decision; break;
        }
    }

    [Rpc(SendTo.Server)]
    private void DecidirMusRpc(bool cortar, RpcParams parametros = default)
    {
        ulong emisor = parametros.Receive.SenderClientId;
        int jugador = ObtenerPuesto(emisor);

        if (estado.Value != EstadoDecidiendo)
        {
            Debug.LogWarning(
                "[MUS ONLINE] Accion rechazada: no hay ronda en curso. Emisor " + emisor
            );
            return;
        }

        if (jugador < 0 || jugador != turno.Value)
        {
            Debug.LogWarning(
                "[MUS ONLINE] Accion rechazada: turno incorrecto. Puesto " + jugador +
                ", turno " + turno.Value
            );
            return;
        }

        if (ObtenerRespuesta(jugador) != SinDecidir)
        {
            Debug.LogWarning("[MUS ONLINE] Accion rechazada: el jugador ya decidio.");
            return;
        }

        GuardarRespuesta(jugador, cortar ? DiceNoHayMus : DiceMus);

        Debug.Log(
            "[MUS ONLINE] Jugador " + jugador +
            (cortar ? " dice NO HAY MUS" : " dice MUS")
        );

        if (cortar)
        {
            jugadorQueCorta.Value = jugador;
            turno.Value = -1;
            estado.Value = EstadoTerminado;
            Debug.Log("[MUS ONLINE] Jugador " + jugador + " corta Mus. Tocaria GRANDE.");
        }
        else if (jugador == JugadoresNecesarios - 1)
        {
            turno.Value = -1;
            estado.Value = EstadoTerminado;
            Debug.Log("[MUS ONLINE] Todos dicen MUS. Tocaria DESCARTE.");
        }
        else
        {
            turno.Value = jugador + 1;
        }
    }

    private static string TextoRespuesta(int valor)
    {
        if (valor == DiceMus)
            return "MUS";

        if (valor == DiceNoHayMus)
            return "NO HAY MUS";

        return "Sin decidir";
    }

    private static string NombreJugador(int puesto)
    {
        if (puesto < 0 || puesto >= NombresPuesto.Length)
            return "?";

        return puesto + " " + NombresPuesto[puesto];
    }

    private static string NombrePareja(int puesto)
    {
        return puesto % 2 == 0 ? "A (0 y 2)" : "B (1 y 3)";
    }

    private void OnGUI()
    {
        // Panel de pruebas: con la interfaz nueva no se ensena (no debe salir en la build).
        if (VistaMesaOnlineMus.InterfazNuevaActiva)
            return;

        float ancho = Mathf.Min(320f, Screen.width - 40f);
        float x = Mathf.Max(20f, Screen.width - ancho - 20f);

        GUILayout.BeginArea(
            new Rect(x, 20, ancho, 380),
            "MUS TINKO - MUS ONLINE (4)",
            GUI.skin.window
        );

        if (!IsSpawned)
        {
            GUILayout.Label("Sin conexion.");
            GUILayout.EndArea();
            return;
        }

        int local = ObtenerPuesto(NetworkManager.LocalClientId);

        GUILayout.Label("Conectados: " + conectados.Value + "/" + JugadoresNecesarios);

        if (estado.Value == EstadoEsperando)
        {
            GUILayout.Label("Esperando a cuatro jugadores...");
            GUILayout.EndArea();
            return;
        }

        GUILayout.Label("Tu puesto: " + (local >= 0 ? NombreJugador(local) : "sin asignar"));

        if (local >= 0)
            GUILayout.Label("Tu pareja: " + NombrePareja(local));

        GUILayout.Space(6);

        for (int i = 0; i < JugadoresNecesarios; i++)
        {
            string marca = i == turno.Value ? "> " : "   ";
            GUILayout.Label(marca + NombreJugador(i) + ": " + TextoRespuesta(ObtenerRespuesta(i)));
        }

        GUILayout.Space(6);

        if (estado.Value == EstadoDecidiendo)
        {
            GUILayout.Label("Turno de: " + NombreJugador(turno.Value));

            if (local == turno.Value)
            {
                if (GUILayout.Button("MUS", GUILayout.Height(32)))
                    DecidirMusRpc(false);

                if (GUILayout.Button("NO HAY MUS", GUILayout.Height(32)))
                    DecidirMusRpc(true);
            }
            else
            {
                GUILayout.Label("Esperando la decision de otro jugador...");

                if (GUILayout.Button("PRUEBA: ACTUAR FUERA DE TURNO", GUILayout.Height(24)))
                    DecidirMusRpc(false);
            }
        }
        else
        {
            if (jugadorQueCorta.Value >= 0)
                GUILayout.Label(NombreJugador(jugadorQueCorta.Value) + " corta Mus. Tocaria GRANDE.");
            else
                GUILayout.Label("TODOS DICEN MUS. Siguiente fase: DESCARTE.");

            if (IsServer && GUILayout.Button("NUEVA RONDA", GUILayout.Height(32)))
                ReiniciarRonda();
        }

        GUILayout.EndArea();
    }
}
