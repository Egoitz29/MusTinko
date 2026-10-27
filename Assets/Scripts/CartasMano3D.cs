using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Vista y seleccion de las cuatro cartas 3D del jugador local.
/// El reparto y el descarte siguen controlados por RepartoPrueba y
/// las cuatro CartaSeleccionable de la interfaz existente.
/// </summary>
public class CartasMano3D : MonoBehaviour
{
    [Header("Reparto existente")]
    [SerializeField] private RepartoPrueba reparto;

    [Header("Cuatro rectangulos 3D, en orden de la mano")]
    [SerializeField] private Transform[] cartas3D = new Transform[4];

    [Header("Seleccion ya existente en el Canvas")]
    [Tooltip("Arrastra las cuatro cartas que tienen CartaSeleccionable, en el mismo orden.")]
    [SerializeField] private CartaSeleccionable[] cartasInterfaz = new CartaSeleccionable[4];

    [Header("Camara desde la que miramos nuestras cartas")]
    [SerializeField] private Camera camaraJugador;

    [Header("Texto sobre las cartas")]
    [SerializeField, Min(0.002f)] private float alturaTexto = 0.006f;
    [SerializeField, Min(0.01f)] private float escalaTexto = 0.05f;

    [Header("Efecto al seleccionar una carta")]
    [SerializeField, Min(0f)] private float elevacion = 0.04f;
    [SerializeField, Min(0f)] private float desplazamientoHaciaMesa = 0.055f;
    [SerializeField, Min(1f)] private float velocidadMovimiento = 12f;

    [Header("Pulsacion tactil / raton")]
    [SerializeField, Min(0f)] private float desplazamientoMaximoToque = 22f;

    private readonly TextMeshPro[] textos = new TextMeshPro[4];
    private readonly Vector3[] posicionesReposo = new Vector3[4];
    private readonly List<RaycastResult> resultadosUI = new List<RaycastResult>();

    private bool preparado;
    private bool ratonPulsado;
    private bool tactilPulsado;
    private Vector2 inicioRaton;
    private Vector2 inicioTactil;

    private void Awake()
    {
        if (camaraJugador == null)
            camaraJugador = Camera.main;

        if (reparto == null || camaraJugador == null ||
            cartas3D == null || cartas3D.Length != 4 ||
            cartasInterfaz == null || cartasInterfaz.Length != 4)
        {
            Debug.LogError(
                "CartasMano3D: asigna Reparto, Camara y los dos arrays de cuatro cartas.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (cartas3D[i] == null || cartasInterfaz[i] == null)
            {
                Debug.LogError("CartasMano3D: falta la carta 3D o del Canvas " + (i + 1), this);
                enabled = false;
                return;
            }

            if (cartasInterfaz[i].GetComponent<Button>() == null)
            {
                Debug.LogError("CartasMano3D: la carta " + (i + 1) +
                    " del Canvas no tiene Button.", this);
                enabled = false;
                return;
            }

            if (cartas3D[i].GetComponent<Collider>() == null)
                Debug.LogWarning("CartasMano3D: falta Collider en " + cartas3D[i].name, this);

            posicionesReposo[i] = cartas3D[i].localPosition;

            // Texto como hermano de la carta: evita deformarlo por la escala fina del cubo.
            GameObject objetoTexto = new GameObject(
                "TextoCarta3D_" + (i + 1).ToString("00"),
                typeof(RectTransform));
            objetoTexto.transform.SetParent(transform, false);

            TextMeshPro texto = objetoTexto.AddComponent<TextMeshPro>();
            texto.text = "";
            texto.color = Color.black;
            texto.alignment = TextAlignmentOptions.Center;
            texto.enableAutoSizing = true;
            texto.fontSizeMin = 0.55f;
            texto.fontSizeMax = 1.4f;
            texto.fontSize = 1.4f;
            texto.richText = false;
            texto.rectTransform.sizeDelta = new Vector2(3.1f, 4.4f);
            objetoTexto.transform.localScale = Vector3.one * escalaTexto;

            textos[i] = texto;
            AjustarTexto(i);
        }

        preparado = true;
    }

    private void Update()
    {
        if (!preparado)
            return;

        DetectarPulsacion();

        for (int i = 0; i < 4; i++)
        {
            // Carta real; cambia automaticamente cuando el reparto cambia.
            string cartaReal = reparto.ObtenerCartaJugador(0, i);
            if (!string.IsNullOrEmpty(cartaReal) && textos[i].text != cartaReal)
                textos[i].text = cartaReal;

            // Reflejar el MISMO estado de seleccion que emplea el descarte del Canvas.
            bool seleccionada = cartasInterfaz[i].EstaSeleccionada;
            Vector3 destino = posicionesReposo[i] +
                (seleccionada
                    ? new Vector3(0f, elevacion, desplazamientoHaciaMesa)
                    : Vector3.zero);

            cartas3D[i].localPosition = Vector3.MoveTowards(
                cartas3D[i].localPosition,
                destino,
                velocidadMovimiento * Time.deltaTime * 0.1f);

            // El texto acompana la elevacion de la carta.
            AjustarTexto(i);
        }
    }

    private void AjustarTexto(int i)
    {
        Transform modelo = cartas3D[i];
        Transform texto = textos[i].transform;

        texto.position = modelo.position - modelo.forward * alturaTexto;
        texto.rotation = modelo.rotation;
    }

    private void DetectarPulsacion()
    {
        // Android: se selecciona al levantar el dedo, no durante el deslizamiento.
        if (Touchscreen.current != null)
        {
            var dedo = Touchscreen.current.primaryTouch;

            if (dedo.press.wasPressedThisFrame)
            {
                inicioTactil = dedo.position.ReadValue();
                tactilPulsado = !EstaSobreUI(inicioTactil);
            }

            if (dedo.press.wasReleasedThisFrame)
            {
                Vector2 fin = dedo.position.ReadValue();
                if (tactilPulsado &&
                    Vector2.Distance(inicioTactil, fin) <= desplazamientoMaximoToque)
                    IntentarSeleccionar(fin);

                tactilPulsado = false;
            }

            // Evitar procesar dos veces el mismo toque como raton.
            if (dedo.press.isPressed || dedo.press.wasReleasedThisFrame)
                return;
        }

        // PC: equivalente con el raton.
        if (Mouse.current != null)
        {
            var raton = Mouse.current;

            if (raton.leftButton.wasPressedThisFrame)
            {
                inicioRaton = raton.position.ReadValue();
                ratonPulsado = !EstaSobreUI(inicioRaton);
            }

            if (raton.leftButton.wasReleasedThisFrame)
            {
                Vector2 fin = raton.position.ReadValue();
                if (ratonPulsado &&
                    Vector2.Distance(inicioRaton, fin) <= desplazamientoMaximoToque)
                    IntentarSeleccionar(fin);

                ratonPulsado = false;
            }
        }
    }

    private void IntentarSeleccionar(Vector2 pantalla)
    {
        if (EstaSobreUI(pantalla))
            return;

        // Solo permitir seleccionar desde la vista hacia abajo.
        // La vista frontal/lateral empieza a 15 grados; cartas esta a 68.
        if (camaraJugador.transform.forward.y > -0.65f)
            return;

        Ray rayo = camaraJugador.ScreenPointToRay(pantalla);
        if (!Physics.Raycast(rayo, out RaycastHit impacto, 10f))
            return;

        for (int i = 0; i < 4; i++)
        {
            if (impacto.transform != cartas3D[i] &&
                !impacto.transform.IsChildOf(cartas3D[i]))
                continue;

            Button botonOriginal = cartasInterfaz[i].GetComponent<Button>();

            // GestorFasesMus desactiva este boton fuera de la fase Descarte.
            if (botonOriginal == null || !botonOriginal.IsActive() ||
                !botonOriginal.IsInteractable())
                return;

            // Enviar el mismo clic que recibe el boton antiguo del Canvas.
            // No se crea un segundo estado de seleccion ni de descarte.
            if (EventSystem.current != null)
            {
                PointerEventData datos = new PointerEventData(EventSystem.current);
                datos.position = pantalla;
                datos.button = PointerEventData.InputButton.Left;
                ExecuteEvents.Execute<IPointerClickHandler>(
                    botonOriginal.gameObject, datos, ExecuteEvents.pointerClickHandler);
            }
            else
            {
                botonOriginal.onClick.Invoke();
            }

            return;
        }
    }

    private bool EstaSobreUI(Vector2 pantalla)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData datos = new PointerEventData(EventSystem.current);
        datos.position = pantalla;
        resultadosUI.Clear();
        EventSystem.current.RaycastAll(datos, resultadosUI);
        // Ignorar posibles raycasts de fisica: solo bloquea la UI del Canvas.
        foreach (RaycastResult resultado in resultadosUI)
        {
            if (resultado.module is GraphicRaycaster)
                return true;
        }
        return false;
    }
}
