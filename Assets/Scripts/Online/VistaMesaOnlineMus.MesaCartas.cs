using System.Collections.Generic;
using UnityEngine;

// Al final de la mano, las cartas de todos caen boca arriba sobre la mesa, delante de cada jugador,
// con el modelo 3D de Blender. Se leen desde tu silla (la parte de arriba de la carta, hacia el otro lado).
public partial class VistaMesaOnlineMus
{
    [Header("Cartas sobre la mesa (fin de mano)")]
    [SerializeField] private float altoCartaMesa = 0.23f;
    [Tooltip("Distancia al centro de la fila de cartas de enfrente y la tuya.")]
    [SerializeField] private float radioFilasMesa = 0.43f;
    [Tooltip("Distancia al centro de las filas de los lados.")]
    [SerializeField] private float radioLadosMesa = 0.66f;
    [SerializeField] private float duracionCaida = 0.35f;

    private struct CartaCayendo
    {
        public Transform T;
        public Vector3 Desde, Hasta;
        public Quaternion GiroDesde, GiroHasta;
        public float Inicio;
    }

    private readonly List<GameObject> cartasEnMesa = new();
    private readonly List<CartaCayendo> cartasCayendo = new();
    private Transform raizCartasMesa;

    private Vector3 DireccionSilla(int sitio)
    {
        Vector3 centro = CentroMesa();
        Vector3 silla = anclaPosicion != null ? anclaPosicion[sitio] : CuerpoDe(ModeloEnSitio(sitio));
        return Plano(silla - centro);
    }

    private void MostrarManosEnMesa(int[] todas)
    {
        if (!Cartas3DMus.Disponible || todas == null || todas.Length < 16)
        {
            if (revelador != null)
                revelador.MostrarManos();
            return;
        }

        OcultarManosEnMesa();

        if (raizCartasMesa == null)
            raizCartasMesa = new GameObject("CartasEnLaMesa").transform;

        Vector3 centro = CentroMesa();
        Vector3 arriba = -DireccionSilla(0);   // de tu silla hacia el otro lado: asi se leen derechas
        float ancho = altoCartaMesa * Cartas3DMus.Proporcion;
        float grosor = altoCartaMesa * 0.008f;
        System.Random azar = new(todas[0] * 31 + todas[5]);

        for (int puesto = 0; puesto < 4; puesto++)
        {
            int sitio = Relativo(puesto);
            Vector3 dir = DireccionSilla(sitio);
            Vector3 lado = Vector3.Cross(Vector3.up, dir);
            bool deFrente = Mathf.Abs(Vector3.Dot(dir, arriba)) > 0.7f;

            // Las de los lados van en cascada (se ve la parte de arriba de cada una), las de frente en fila.
            float separacion = deFrente ? ancho * 1.12f : altoCartaMesa * 0.62f;
            Vector3 fila = centro + dir * (deFrente ? radioFilasMesa : radioLadosMesa);

            for (int c = 0; c < 4; c++)
            {
                string texto = CartasMus.Texto(todas[puesto * 4 + c]);
                GameObject carta = Cartas3DMus.Crear("Carta_" + puesto + "_" + c, raizCartasMesa, altoCartaMesa, texto);

                if (carta == null)
                    continue;

                float desp = (c - 1.5f) * separacion;
                // La que queda mas cerca de ti va encima, para que se vea la parte de arriba de las demas.
                float altura = grosor * 0.5f + 0.0015f * (2f - Vector3.Dot(lado * desp, arriba) / Mathf.Max(0.01f, separacion));
                Vector3 hasta = fila + lado * desp + Vector3.up * altura;
                float giroAzar = (float)(azar.NextDouble() * 6.0 - 3.0);
                Quaternion giro = Quaternion.AngleAxis(giroAzar, Vector3.up) * Quaternion.LookRotation(Vector3.down, arriba);

                carta.transform.SetPositionAndRotation(hasta + Vector3.up * 0.3f, giro);
                carta.SetActive(false);
                cartasEnMesa.Add(carta);
                cartasCayendo.Add(new CartaCayendo
                {
                    T = carta.transform,
                    Desde = hasta + Vector3.up * 0.3f + dir * 0.15f,
                    Hasta = hasta,
                    GiroDesde = giro * Quaternion.Euler(25f, 0f, 15f),
                    GiroHasta = giro,
                    Inicio = Time.time + puesto * 0.12f + c * 0.06f
                });
            }
        }
    }

    private void OcultarManosEnMesa()
    {
        foreach (GameObject c in cartasEnMesa)
            if (c != null)
                Destroy(c);

        cartasEnMesa.Clear();
        cartasCayendo.Clear();

        if (revelador != null)
            revelador.OcultarManos();
    }

    // Animacion de caida (se llama cada frame).
    private void ActualizarCartasEnMesa()
    {
        for (int i = cartasCayendo.Count - 1; i >= 0; i--)
        {
            CartaCayendo c = cartasCayendo[i];

            if (c.T == null)
            {
                cartasCayendo.RemoveAt(i);
                continue;
            }

            if (Time.time < c.Inicio)
                continue;

            if (!c.T.gameObject.activeSelf)
                c.T.gameObject.SetActive(true);

            float t = Mathf.Clamp01((Time.time - c.Inicio) / duracionCaida);
            float k = Mathf.SmoothStep(0f, 1f, t);
            c.T.SetPositionAndRotation(Vector3.Lerp(c.Desde, c.Hasta, k), Quaternion.Slerp(c.GiroDesde, c.GiroHasta, k));

            if (t >= 1f)
                cartasCayendo.RemoveAt(i);
        }
    }
}
