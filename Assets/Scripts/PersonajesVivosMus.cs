using UnityEngine;

public class PersonajesVivosMus : MonoBehaviour
{
    [Header("Para la escena local (Mesamus): color por piezas y vida a los cuatro personajes")]
    [SerializeField] private bool colorPorPiezas = true;
    [Range(0f, 1f)][SerializeField] private float saturacionRopa = 0.85f;
    [Range(0f, 1f)][SerializeField] private float brilloMaterial = 0.2f;
    [SerializeField] private bool darVida = true;

    private void Start()
    {
        Camera camara = Camera.main;
        Vector3 referencia = camara != null ? camara.transform.position : new Vector3(0f, 0f, -3f);
        GameObject[] modelos = SitiosMesaMus.Asignar(Vector3.zero, referencia);

        for (int sitio = 0; sitio < 4; sitio++)
        {
            GameObject modelo = modelos[sitio];

            if (modelo == null)
                continue;

            if (colorPorPiezas)
                ColoresJugadoresMus.AplicarAspecto(modelo, sitio, saturacionRopa, brilloMaterial);

            if (!darVida)
                continue;

            Animator animador = modelo.GetComponentInChildren<Animator>(true);
            GameObject destino = animador != null ? animador.gameObject : modelo;

            if (destino.GetComponent<GestosMus>() == null)
                destino.AddComponent<GestosMus>();
        }
    }
}
