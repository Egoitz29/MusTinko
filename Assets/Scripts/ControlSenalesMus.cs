
using UnityEngine;

public class ControlSenalesMus : MonoBehaviour
{
    public enum TipoSenal
    {
        TreintaYUna,
        Duples,
        DosReyes,
        DosAses
    }

    [SerializeField] private Animator animator;

    private static readonly int Hacer31 =
        Animator.StringToHash("Hacer31");

    private static readonly int HacerDuples =
        Animator.StringToHash("HacerDuples");

    private static readonly int HacerDosReyes =
        Animator.StringToHash("HacerDosReyes");

    private static readonly int HacerDosAses =
        Animator.StringToHash("HacerDosAses");

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("MENTA: No se ha encontrado el Animator.", this);
    }

    public void EjecutarSenal(TipoSenal tipo)
    {
        if (animator == null || !animator.isActiveAndEnabled)
            return;

        animator.ResetTrigger(Hacer31);
        animator.ResetTrigger(HacerDuples);
        animator.ResetTrigger(HacerDosReyes);
        animator.ResetTrigger(HacerDosAses);

        switch (tipo)
        {
            case TipoSenal.TreintaYUna:
                animator.SetTrigger(Hacer31);
                break;

            case TipoSenal.Duples:
                animator.SetTrigger(HacerDuples);
                break;

            case TipoSenal.DosReyes:
                animator.SetTrigger(HacerDosReyes);
                break;

            case TipoSenal.DosAses:
                animator.SetTrigger(HacerDosAses);
                break;
        }
    }

    public void Senal31()
    {
        EjecutarSenal(TipoSenal.TreintaYUna);
    }

    public void SenalDuples()
    {
        EjecutarSenal(TipoSenal.Duples);
    }

    public void SenalDosReyes()
    {
        EjecutarSenal(TipoSenal.DosReyes);
    }

    public void SenalDosAses()
    {
        EjecutarSenal(TipoSenal.DosAses);
    }

    [ContextMenu("Probar 31")]
    private void Probar31()
    {
        Senal31();
    }

    [ContextMenu("Probar Duples")]
    private void ProbarDuples()
    {
        SenalDuples();
    }

    [ContextMenu("Probar Dos Reyes")]
    private void ProbarDosReyes()
    {
        SenalDosReyes();
    }

    [ContextMenu("Probar Dos Ases")]
    private void ProbarDosAses()
    {
        SenalDosAses();
    }
}
