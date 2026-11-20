
using System.Collections;
using TMPro;
using UnityEngine;

public class BocadilloMus3D : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject fondoComic;
    [SerializeField] private TMP_Text textoJugada;

    [Header("Configuracion")]
    [SerializeField] private float duracion = 2f;
    [SerializeField] private bool probarAlIniciar = false;

    [Header("Aparicion")]
    [SerializeField] private bool animarAparicion = true;
    [SerializeField] private float duracionAparicion = 0.18f;

    private Coroutine rutinaOcultar;
    private Coroutine rutinaAparicion;
    private Vector3 escalaOriginal;
    private bool escalaGuardada;
    private Color colorOriginal;
    private bool colorGuardado;

    private void Start()
    {
        Ocultar();

        if (probarAlIniciar)
            Mostrar("¡ENVIDO 2!");
    }

    // Mensaje temporal: desaparece solo.
    public void Mostrar(string mensaje)
    {
        if (!MostrarContenido(mensaje))
            return;

        rutinaOcultar = StartCoroutine(OcultarDespues());
    }

    // Mensaje persistente: permanece visible.
    public void MostrarFijo(string mensaje)
    {
        MostrarContenido(mensaje);
    }

    public void MostrarFijo(string mensaje, Color color)
    {
        if (MostrarContenido(mensaje))
            textoJugada.color = color;
    }

    public void Mostrar(string mensaje, Color color)
    {
        if (!MostrarContenido(mensaje))
            return;

        textoJugada.color = color;
        rutinaOcultar = StartCoroutine(OcultarDespues());
    }

    private IEnumerator Aparecer()
    {
        Transform t = fondoComic.transform;
        float tiempo = 0f;

        while (tiempo < duracionAparicion)
        {
            tiempo += Time.deltaTime;
            float k = Mathf.Clamp01(tiempo / duracionAparicion);
            float rebote = 1f + 0.12f * Mathf.Sin(k * Mathf.PI);
            t.localScale = escalaOriginal * Mathf.Lerp(0.6f, 1f, k) * rebote;
            yield return null;
        }

        t.localScale = escalaOriginal;
        rutinaAparicion = null;
    }

    private bool MostrarContenido(string mensaje)
    {
        if (fondoComic == null || textoJugada == null)
        {
            Debug.LogError("Faltan referencias del bocadillo.");
            return false;
        }

        if (rutinaOcultar != null)
        {
            StopCoroutine(rutinaOcultar);
            rutinaOcultar = null;
        }

        if (!colorGuardado)
        {
            colorOriginal = textoJugada.color;
            colorGuardado = true;
        }

        if (!escalaGuardada)
        {
            escalaOriginal = fondoComic.transform.localScale;
            escalaGuardada = true;
        }

        bool yaVisible = fondoComic.activeSelf && textoJugada.text == mensaje;

        textoJugada.text = mensaje;
        textoJugada.color = colorOriginal;

        fondoComic.SetActive(true);
        textoJugada.gameObject.SetActive(true);

        if (animarAparicion && !yaVisible && isActiveAndEnabled)
        {
            if (rutinaAparicion != null)
                StopCoroutine(rutinaAparicion);

            rutinaAparicion = StartCoroutine(Aparecer());
        }

        return true;
    }

    private IEnumerator OcultarDespues()
    {
        yield return new WaitForSeconds(duracion);

        rutinaOcultar = null;
        Ocultar();
    }

    public void Ocultar()
    {
        if (rutinaOcultar != null)
        {
            StopCoroutine(rutinaOcultar);
            rutinaOcultar = null;
        }

        if (rutinaAparicion != null)
        {
            StopCoroutine(rutinaAparicion);
            rutinaAparicion = null;
        }

        if (fondoComic != null)
        {
            if (escalaGuardada)
                fondoComic.transform.localScale = escalaOriginal;

            fondoComic.SetActive(false);
        }

        if (textoJugada != null)
            textoJugada.gameObject.SetActive(false);
    }

    [ContextMenu("PRUEBA - Mostrar PASO fijo")]
    private void ProbarPasoFijo()
    {
        MostrarFijo("PASO");
    }

    [ContextMenu("PRUEBA - Ocultar bocadillo")]
    private void ProbarOcultar()
    {
        Ocultar();
    }
}
