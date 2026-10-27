
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

    private Coroutine rutinaOcultar;

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

        textoJugada.text = mensaje;

        fondoComic.SetActive(true);
        textoJugada.gameObject.SetActive(true);

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

        if (fondoComic != null)
            fondoComic.SetActive(false);

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
