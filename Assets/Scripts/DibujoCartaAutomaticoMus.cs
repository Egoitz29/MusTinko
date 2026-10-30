using UnityEngine;

/// <summary>
/// Muestra el dibujo correcto de una carta del jugador local (0).
/// Colocar en el objeto DibujoCarta que ya tiene SpriteRenderer.
/// No modifica el reparto ni la seleccion/descarte.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DibujoCartaAutomaticoMus : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RepartoPrueba reparto;

    [Header("Carta de nuestra mano (0, 1, 2 o 3)")]
    [Range(0, 3)]
    [SerializeField] private int posicionCarta = 0;

    // El PNG Cartas_Tatú.png debe estar en Assets/Resources/Baraja/.
    // Ya debe estar cortado en 40 sprites (Multiple, 10 x 4).
    private const string RutaBaraja = "Baraja/Cartas_Tatú";

    private readonly Sprite[] spritesPorIndice = new Sprite[40];
    private SpriteRenderer dibujo;
    private string cartaAnterior;
    private bool preparado;

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();

        Sprite[] sprites = Resources.LoadAll<Sprite>(RutaBaraja);
        foreach (Sprite sprite in sprites)
        {
            // Unity crea nombres como Cartas_Tatú_0 ... Cartas_Tatú_39.
            int separador = sprite.name.LastIndexOf('_');
            if (separador < 0 ||
                !int.TryParse(sprite.name.Substring(separador + 1), out int indice) ||
                indice < 0 || indice >= spritesPorIndice.Length)
                continue;

            spritesPorIndice[indice] = sprite;
        }

        int encontrados = 0;
        foreach (Sprite sprite in spritesPorIndice)
            if (sprite != null) encontrados++;

        if (encontrados != 40)
        {
            Debug.LogError(
                $"DibujoCartaAutomaticoMus: encontradas {encontrados}/40 cartas en " +
                $"Resources/{RutaBaraja}. Revisa la ubicacion, el corte y los nombres.", this);
            enabled = false;
            return;
        }

        if (reparto == null)
        {
            Debug.LogError("DibujoCartaAutomaticoMus: asigna GestorReparto (RepartoPrueba).", this);
            enabled = false;
            return;
        }

        preparado = true;
    }

    private void Update()
    {
        if (!preparado) return;

        string carta = reparto.ObtenerCartaJugador(0, posicionCarta);
        if (string.IsNullOrEmpty(carta) || carta == cartaAnterior) return;

        if (!TryObtenerIndiceSprite(carta, out int indice))
        {
            Debug.LogWarning("DibujoCartaAutomaticoMus: carta no reconocida: " + carta, this);
            return;
        }

        dibujo.sprite = spritesPorIndice[indice];
        cartaAnterior = carta;
    }

    private static bool TryObtenerIndiceSprite(string carta, out int indice)
    {
        indice = -1;
        string[] partes = carta.Trim().ToUpperInvariant().Split('\n');
        if (partes.Length != 2) return false;

        string valor = partes[0].Trim();
        string palo = partes[1].Trim();

        // Cada fila contiene: AS,2,3,4,5,6,7,SOTA,CABALLO,REY.
        int columna;
        switch (valor)
        {
            case "AS": columna = 0; break;
            case "2": columna = 1; break;
            case "3": columna = 2; break;
            case "4": columna = 3; break;
            case "5": columna = 4; break;
            case "6": columna = 5; break;
            case "7": columna = 6; break;
            case "SOTA": columna = 7; break;
            case "CABALLO": columna = 8; break;
            case "REY": columna = 9; break;
            default: return false;
        }

        // Orden de las filas de Cartas_Tatú tal como se ve en Unity.
        int fila;
        switch (palo)
        {
            case "ESPADAS": fila = 0; break;
            case "COPAS": fila = 1; break;
            case "OROS": fila = 2; break;
            case "BASTOS": fila = 3; break;
            default: return false;
        }

        indice = fila * 10 + columna;
        return true;
    }
}
