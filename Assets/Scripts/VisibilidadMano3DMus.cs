using UnityEngine;

/// <summary>
/// Muestra la representacion 3D de la mano solo cuando la mirada
/// esta suficientemente inclinada hacia las cartas.
/// No desactiva los GameObjects, colliders ni la logica de reparto/descarte.
/// </summary>
public class VisibilidadMano3DMus : MonoBehaviour
{
    [Header("Camara del jugador")]
    [SerializeField] private Transform camaraJugador;

    [Header("Vista de cartas")]
    [Tooltip("A partir de esta inclinacion hacia abajo se muestran las cartas.")]
    [Range(30f, 85f)]
    [SerializeField] private float inclinacionMinima = 52f;

    private Renderer[] renderizadores;
    private bool[] estadosOriginales;
    private bool? ultimaVisibilidad;

    private void Start()
    {
        if (camaraJugador == null && Camera.main != null)
            camaraJugador = Camera.main.transform;

        if (camaraJugador == null)
        {
            Debug.LogError("VisibilidadMano3DMus: asigna Main Camera.", this);
            enabled = false;
            return;
        }

        // Los textos de CartasMano3D se crean en Awake, antes de Start.
        renderizadores = GetComponentsInChildren<Renderer>(true);
        estadosOriginales = new bool[renderizadores.Length];
        for (int i = 0; i < renderizadores.Length; i++)
            estadosOriginales[i] = renderizadores[i].enabled;

        ActualizarVisibilidad();
    }

    private void LateUpdate()
    {
        ActualizarVisibilidad();
    }

    private void ActualizarVisibilidad()
    {
        if (camaraJugador == null || renderizadores == null)
            return;

        // 0 grados: frente. 90 grados: totalmente hacia abajo.
        float inclinacion = Mathf.Asin(
            Mathf.Clamp(-camaraJugador.forward.y, -1f, 1f)
        ) * Mathf.Rad2Deg;

        bool mostrar = inclinacion >= inclinacionMinima;
        if (ultimaVisibilidad.HasValue && ultimaVisibilidad.Value == mostrar)
            return;

        ultimaVisibilidad = mostrar;
        for (int i = 0; i < renderizadores.Length; i++)
        {
            if (renderizadores[i] != null)
                renderizadores[i].enabled = mostrar && estadosOriginales[i];
        }
    }

    private void OnDisable()
    {
        // Restauramos los estados originales al quitar/desactivar el componente.
        if (renderizadores == null || estadosOriginales == null)
            return;

        for (int i = 0; i < renderizadores.Length; i++)
        {
            if (renderizadores[i] != null)
                renderizadores[i].enabled = estadosOriginales[i];
        }
    }
}
