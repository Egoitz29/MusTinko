
using Unity.Netcode;
using UnityEngine;

public class EstadoPartidaOnlineMus : NetworkBehaviour
{
    private readonly NetworkVariable<int> puntos =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        puntos.OnValueChanged += PuntosActualizados;
        Debug.Log("[MUS ONLINE] Estado sincronizado: " + puntos.Value);
    }

    public override void OnNetworkDespawn()
    {
        puntos.OnValueChanged -= PuntosActualizados;
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(
            new Rect(20, 250, 350, 150),
            "MUS TINKO - ESTADO COMPARTIDO",
            GUI.skin.window
        );

        if (!IsSpawned)
        {
            GUILayout.Label("Esperando sincronizacion...");
        }
        else
        {
            GUILayout.Label("Puntos compartidos: " + puntos.Value);

            if (GUILayout.Button("SUMAR 1 PUNTO", GUILayout.Height(40)))
                SumarPuntoRpc();
        }

        GUILayout.EndArea();
    }

    [Rpc(SendTo.Server)]
    private void SumarPuntoRpc()
    {
        puntos.Value++;
    }

    private void PuntosActualizados(int anterior, int actual)
    {
        Debug.Log("[MUS ONLINE] Puntos: " + anterior + " -> " + actual);
    }
}
