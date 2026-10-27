
using UnityEngine;

public class BocadilloMirarCamara : MonoBehaviour
{
    [Header("Camara del jugador")]
    [SerializeField] private Transform camaraJugador;

    [Header("Correccion de orientacion")]
    [SerializeField] private float giroY = 0f;

    private void LateUpdate()
    {
        if (camaraJugador == null)
            return;

        // El bocadillo se orienta igual que
        // nuestra camara para que sea legible.
        transform.rotation =
            camaraJugador.rotation *
            Quaternion.Euler(0f, giroY, 0f);
    }
}
