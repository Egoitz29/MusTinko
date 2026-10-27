
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ReveladorManosMus : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RepartoPrueba reparto;

    [Header("Posicion de la mesa")]
    [SerializeField] private float alturaCartas = 0.865f;

    [Header("Distribucion")]
    [SerializeField] private float distanciaFilas = 0.43f;
    [SerializeField] private float distanciaLaterales = 0.74f;
    [SerializeField] private float separacionHorizontal = 0.31f;
    [SerializeField] private float separacionVertical = 0.24f;

    private readonly List<GameObject> cartasMostradas =
        new List<GameObject>();

    private Material materialCarta;

    private void Awake()
    {
        Shader shader = Shader.Find(
            "Universal Render Pipeline/Lit"
        );

        if (shader == null)
        {
            Debug.LogError(
                "No se encontro el shader URP/Lit."
            );
            return;
        }

        materialCarta = new Material(shader);

        materialCarta.color =
            new Color(0.96f, 0.91f, 0.82f);
    }

    // =========================================
    // MOSTRAR LAS CUATRO MANOS
    // =========================================

    public void MostrarManos()
    {
        if (reparto == null || materialCarta == null)
        {
            Debug.LogError(
                "Falta GestorReparto o material de cartas."
            );
            return;
        }

        OcultarManos();

        for (int jugador = 0; jugador < 4; jugador++)
        {
            for (int posicion = 0; posicion < 4; posicion++)
            {
                string carta =
                    reparto.ObtenerCartaJugador(
                        jugador, posicion
                    );

                if (string.IsNullOrEmpty(carta))
                    continue;

                Vector3 ubicacion =
                    CalcularPosicion(jugador, posicion);

                CrearCarta3D(
                    jugador,
                    posicion,
                    carta,
                    ubicacion
                );
            }
        }

        Debug.Log(
            "REVELADO DE MANO: " +
            cartasMostradas.Count +
            " cartas colocadas sobre la mesa."
        );
    }

    // =========================================
    // CALCULAR POSICIONES
    // =========================================

    private Vector3 CalcularPosicion(
        int jugador,
        int posicion)
    {
        float desplazamiento =
            posicion - 1.5f;

        switch (jugador)
        {
            // Nosotros: lado cercano de la mesa.
            case 0:
                return new Vector3(
                    desplazamiento * separacionHorizontal,
                    alturaCartas,
                    -distanciaFilas
                );

            // Rival derecho.
            case 1:
                return new Vector3(
                    distanciaLaterales,
                    alturaCartas,
                    desplazamiento * separacionVertical
                );

            // Nuestro compañero enfrente.
            case 2:
                return new Vector3(
                    desplazamiento * separacionHorizontal,
                    alturaCartas,
                    distanciaFilas
                );

            // Rival izquierdo.
            case 3:
                return new Vector3(
                    -distanciaLaterales,
                    alturaCartas,
                    desplazamiento * separacionVertical
                );

            default:
                return Vector3.zero;
        }
    }

    // =========================================
    // CREAR CARTA 3D
    // =========================================


    private void CrearCarta3D(
        int jugador,
        int posicion,
        string contenido,
        Vector3 ubicacion)
    {
        // Objeto principal de la carta
        GameObject carta = new GameObject(
            "Carta3D_Jugador" + jugador +
            "_Posicion" + posicion
        );

        carta.transform.SetParent(transform, false);
        carta.transform.position = ubicacion;

        // =====================================
        // BASE DE LA CARTA
        // =====================================

        GameObject baseCarta = GameObject.CreatePrimitive(
            PrimitiveType.Cube
        );

        baseCarta.name = "BaseCarta";

        baseCarta.transform.SetParent(
            carta.transform, false
        );

        baseCarta.transform.localPosition = Vector3.zero;

        baseCarta.transform.localScale =
            new Vector3(0.19f, 0.012f, 0.23f);

        MeshRenderer renderer =
            baseCarta.GetComponent<MeshRenderer>();

        renderer.sharedMaterial = materialCarta;

        // =====================================
        // TEXTO DE LA CARTA
        // =====================================

        GameObject objetoTexto = new GameObject(
            "ValorYPalo"
        );

        objetoTexto.transform.SetParent(
            carta.transform, false
        );

        // Situamos el texto por encima del cubo
        // para evitar que quede oculto.
        objetoTexto.transform.localPosition =
            new Vector3(0f, 0.018f, 0f);

        // Texto tumbado sobre la mesa
        objetoTexto.transform.localRotation =
            Quaternion.Euler(90f, 0f, 0f);

        objetoTexto.transform.localScale =
            Vector3.one * 0.02f;

        TextMeshPro texto =
            objetoTexto.AddComponent<TextMeshPro>();

        texto.text = contenido;

        // Tamaño de letra para las cartas 3D
        texto.fontSize = 15f;

        // Impedimos que Unity reduzca automáticamente
        // las letras al ajustar el texto.
        texto.enableAutoSizing = false;

        // Letras en negrita y centradas
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;

        // Color negro
        texto.color = Color.black;

        // Área disponible para el texto
        texto.rectTransform.sizeDelta =
            new Vector2(9f, 10f);


        // =====================================
        // REGISTRAR LA CARTA
        // =====================================

        cartasMostradas.Add(carta);
    }


    // =========================================
    // OCULTAR LAS CARTAS AL INICIAR OTRA MANO
    // =========================================

    public void OcultarManos()
    {
        foreach (GameObject carta in cartasMostradas)
        {
            if (carta != null)
                Destroy(carta);
        }

        cartasMostradas.Clear();
    }

    private void OnDestroy()
    {
        if (materialCarta != null)
            Destroy(materialCarta);
    }
}
