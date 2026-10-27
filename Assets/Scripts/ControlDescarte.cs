
using UnityEngine;

public class ControlDescarte : MonoBehaviour
{
    [Header("Gestor del reparto")]
    [SerializeField] private RepartoPrueba reparto;

    public void ConfirmarDescarte()
    {
        if (reparto == null)
        {
            Debug.LogError("No está asignado el gestor de reparto.");
            return;
        }

        int cantidad = reparto.DescartarSeleccionadas();

        if (cantidad > 0)
        {
            Debug.Log("Confirmadas " + cantidad + " cartas descartadas.");
        }
    }
}
