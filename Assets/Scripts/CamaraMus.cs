using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CamaraMus : MonoBehaviour
{
    private enum Vista
    {
        Izquierda,
        Frente,
        Derecha,
        Cartas
    }

    [Header("Posiciones fijas de la mirada")]
    [Range(0f, 65f)]
    [SerializeField] private float anguloLateral = 35f;

    [Range(30f, 85f)]
    [SerializeField] private float inclinacionCartas = 68f;

    [Header("Transicion suave entre posiciones")]
    [Min(0.05f)]
    [SerializeField] private float duracionTransicion = 0.25f;

    [Header("Deslizamiento (raton y tactil)")]
    [Range(0.02f, 0.20f)]
    [SerializeField] private float distanciaMinimaDeslizamiento = 0.06f;

    private float horizontalInicial;
    private float verticalInicial;

    private Vista vistaActual = Vista.Frente;
    private Quaternion rotacionOrigen;
    private Quaternion rotacionDestino;
    private float tiempoTransicion;
    private bool enTransicion;

    private bool arrastrandoRaton;
    private bool arrastrandoTactil;
    private Vector2 inicioRaton;
    private Vector2 inicioTactil;

    private readonly List<RaycastResult> resultadosUI =
        new List<RaycastResult>();

    private void Start()
    {
        horizontalInicial = transform.eulerAngles.y;
        verticalInicial = Mathf.DeltaAngle(0f, transform.eulerAngles.x);
        rotacionOrigen = transform.rotation;
        rotacionDestino = transform.rotation;
    }

    private void Update()
    {
        // PC: pruebas directas de las cuatro vistas.
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) MirarIzquierda();
            if (Keyboard.current.digit2Key.wasPressedThisFrame) MirarFrente();
            if (Keyboard.current.digit3Key.wasPressedThisFrame) MirarDerecha();
            if (Keyboard.current.digit4Key.wasPressedThisFrame) MirarCartas();
        }

        // Android: un gesto completo selecciona la siguiente posicion.
        if (Touchscreen.current != null)
        {
            var dedo = Touchscreen.current.primaryTouch;

            if (dedo.press.wasPressedThisFrame)
            {
                inicioTactil = dedo.position.ReadValue();
                arrastrandoTactil =
                    inicioTactil.x >= Screen.width * 0.5f &&
                    !EstaSobreUI(inicioTactil);
            }

            if (dedo.press.wasReleasedThisFrame)
            {
                if (arrastrandoTactil)
                    InterpretarDeslizamiento(dedo.position.ReadValue() - inicioTactil);

                arrastrandoTactil = false;
            }

            if (dedo.press.isPressed || dedo.press.wasReleasedThisFrame)
                return;
        }

        // PC: arrastrar y soltar con el raton equivale a un gesto tactil.
        if (Mouse.current != null)
        {
            var raton = Mouse.current;

            if (raton.leftButton.wasPressedThisFrame)
            {
                inicioRaton = raton.position.ReadValue();
                arrastrandoRaton = !EstaSobreUI(inicioRaton);
            }

            if (raton.leftButton.wasReleasedThisFrame)
            {
                if (arrastrandoRaton)
                    InterpretarDeslizamiento(raton.position.ReadValue() - inicioRaton);

                arrastrandoRaton = false;
            }
        }
    }

    private void LateUpdate()
    {
        if (!enTransicion)
            return;

        tiempoTransicion += Time.deltaTime;
        float t = Mathf.Clamp01(tiempoTransicion / duracionTransicion);

        // SmoothStep suaviza el inicio y el final. La llegada es exacta.
        float progreso = Mathf.SmoothStep(0f, 1f, t);
        transform.rotation = Quaternion.Slerp(rotacionOrigen, rotacionDestino, progreso);

        if (t >= 1f)
        {
            transform.rotation = rotacionDestino;
            enTransicion = false;
        }
    }

    private void InterpretarDeslizamiento(Vector2 movimiento)
    {
        // Un gesto no modifica la rotacion libremente: elige una vista.
        if (Mathf.Abs(movimiento.x) >= Mathf.Abs(movimiento.y))
        {
            if (Mathf.Abs(movimiento.x) < Screen.width * distanciaMinimaDeslizamiento)
                return;

            // Arrastrar a la izquierda: mirar un puesto mas a la derecha.
            if (movimiento.x < 0f)
                DesplazarHorizontal(1);
            else
                DesplazarHorizontal(-1);
        }
        else
        {
            if (Mathf.Abs(movimiento.y) < Screen.height * distanciaMinimaDeslizamiento)
                return;

            // Bajar el dedo: mirar las cartas. Subirlo: volver al frente.
            if (movimiento.y < 0f)
                MirarCartas();
            else
                MirarFrente();
        }
    }

    private void DesplazarHorizontal(int direccion)
    {
        if (vistaActual == Vista.Cartas)
        {
            MirarFrente();
            return;
        }

        int indice = vistaActual == Vista.Izquierda ? 0 :
                     vistaActual == Vista.Frente ? 1 : 2;

        indice = Mathf.Clamp(indice + direccion, 0, 2);

        if (indice == 0) MirarIzquierda();
        else if (indice == 1) MirarFrente();
        else MirarDerecha();
    }

    private void CambiarVista(Vista nuevaVista)
    {
        if (nuevaVista == vistaActual && !enTransicion)
            return;

        vistaActual = nuevaVista;
        float horizontal = 0f;
        float vertical = verticalInicial;

        switch (vistaActual)
        {
            case Vista.Izquierda:
                horizontal = -anguloLateral;
                break;
            case Vista.Derecha:
                horizontal = anguloLateral;
                break;
            case Vista.Cartas:
                vertical = inclinacionCartas;
                break;
        }

        // Si cambiamos de idea durante un giro, se parte de la posicion
        // visual actual, sin salto brusco ni parada a mitad de camino.
        rotacionOrigen = transform.rotation;
        rotacionDestino = Quaternion.Euler(
            vertical,
            horizontalInicial + horizontal,
            0f
        );
        tiempoTransicion = 0f;
        enTransicion = true;
    }

    private bool EstaSobreUI(Vector2 posicion)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData datos = new PointerEventData(EventSystem.current);
        datos.position = posicion;
        resultadosUI.Clear();
        EventSystem.current.RaycastAll(datos, resultadosUI);
        return resultadosUI.Count > 0;
    }

    // Siguen siendo publicos para futuros botones tactiles de Android.
    public void MirarIzquierda() { CambiarVista(Vista.Izquierda); }
    public void MirarFrente() { CambiarVista(Vista.Frente); }
    public void MirarDerecha() { CambiarVista(Vista.Derecha); }
    public void MirarCartas() { CambiarVista(Vista.Cartas); }

    // Se conserva para los demas scripts que ya usen este metodo.
    public void RecentrarVista()
    {
        vistaActual = Vista.Frente;
        enTransicion = false;
        tiempoTransicion = 0f;
        rotacionDestino = Quaternion.Euler(verticalInicial, horizontalInicial, 0f);
        rotacionOrigen = rotacionDestino;
        transform.rotation = rotacionDestino;
    }
}
