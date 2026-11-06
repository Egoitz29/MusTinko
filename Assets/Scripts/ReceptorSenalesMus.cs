
using System.Collections.Generic;
using UnityEngine;

public class ReceptorSenalesMus : MonoBehaviour
{
    [Header("Identidad")]
    [Range(0, 3)]
    [SerializeField] private int jugadorReceptor = 0;

    [Header("Referencias")]
    [SerializeField] private CanalSenalesMus canal;
    [SerializeField] private GestorFasesMus gestorFases;

    private readonly HashSet<ControlSenalesMus.TipoSenal>
        senalesRecibidas = new();

    private GestorFasesMus.Fase faseAnterior =
        (GestorFasesMus.Fase)(-1);

    private void Awake()
    {
        if (canal == null)
            canal = GetComponentInParent<CanalSenalesMus>();
    }

    private void OnEnable()
    {
        if (canal == null)
            canal = GetComponentInParent<CanalSenalesMus>();

        if (canal != null)
            canal.SenalComunicada += RecibirSenal;
        else
            Debug.LogError(
                "[MUS SENAS] Falta CanalSenalesMus.",
                this
            );
    }

    private void OnDisable()
    {
        if (canal != null)
            canal.SenalComunicada -= RecibirSenal;
    }

    private void Update()
    {
        if (gestorFases == null)
            return;

        var fase = gestorFases.FaseActual;

        if (fase == faseAnterior)
            return;

        faseAnterior = fase;

        if (fase == GestorFasesMus.Fase.Mus)
            LimpiarMemoria();
    }

    private void RecibirSenal(
        int emisor,
        int receptor,
        ControlSenalesMus.TipoSenal tipo)
    {
        if (receptor != jugadorReceptor)
            return;

        if (emisor != (jugadorReceptor + 2) % 4)
            return;

        bool nueva = senalesRecibidas.Add(tipo);

        if (!nueva)
            return;

        Debug.Log(
            "[MUS RECEPTOR] Jugador " +
            jugadorReceptor +
            " recibe de su companero " +
            emisor +
            " la sena: " +
            tipo,
            this
        );
    }

    public bool ConoceSenal(
        ControlSenalesMus.TipoSenal tipo)
    {
        return senalesRecibidas.Contains(tipo);
    }

    public void LimpiarMemoria()
    {
        senalesRecibidas.Clear();
    }

    [ContextMenu("Diagnosticar senales recibidas")]
    private void Diagnosticar()
    {
        Debug.Log(
            "[MUS RECEPTOR] Jugador " +
            jugadorReceptor +
            " | Senales recordadas: " +
            senalesRecibidas.Count,
            this
        );

        foreach (var tipo in senalesRecibidas)
        {
            Debug.Log(
                "[MUS RECEPTOR] Recuerda: " +
                tipo,
                this
            );
        }
    }
}
