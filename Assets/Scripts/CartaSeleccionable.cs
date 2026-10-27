
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CartaSeleccionable : MonoBehaviour
{
    [Header("Color al seleccionar")]
    [SerializeField]
    private Color colorSeleccionado =
        new Color(1f, 0.8f, 0.3f);

    private Button boton;
    private Image imagen;

    private Color colorOriginal;
    private bool seleccionada;

    public bool EstaSeleccionada => seleccionada;

    private void Awake()
    {
        boton = GetComponent<Button>();
        imagen = GetComponent<Image>();

        colorOriginal = imagen.color;

        // Evita que el efecto del Button
        // interfiera con nuestros colores.
        boton.transition = Selectable.Transition.None;

        boton.onClick.AddListener(AlternarSeleccion);
    }

    public void AlternarSeleccion()
    {
        seleccionada = !seleccionada;

        ActualizarVisual();

        Debug.Log(
            $"{gameObject.name}: " +
            (seleccionada ? "Seleccionada" : "Deseleccionada")
        );
    }

    public void Deseleccionar()
    {
        seleccionada = false;
        ActualizarVisual();
    }

    private void ActualizarVisual()
    {
        imagen.color = seleccionada
            ? colorSeleccionado
            : colorOriginal;
    }

    private void OnDestroy()
    {
        if (boton != null)
            boton.onClick.RemoveListener(AlternarSeleccion);
    }
}
