using System.Collections.Generic;
using UnityEngine;

public class GestosMus : MonoBehaviour
{
    [Header("Fuerza y duracion de los gestos")]
    [Range(0.5f, 3f)][SerializeField] private float intensidad = 1.5f;
    [SerializeField] private float duracionMantener = 1.2f;
    [SerializeField] private float duracionEntrada = 0.18f;
    [SerializeField] private float duracionSalida = 0.25f;

    [Header("Ejes de la cabeza (cambialos si gira hacia donde no toca)")]
    [SerializeField] private Vector3 ejeLadear = new(0f, 0f, 1f);
    [SerializeField] private Vector3 ejeAgachar = new(1f, 0f, 0f);

    private enum Modo
    {
        Escala,
        Posicion,
        Rotacion
    }

    private class Canal
    {
        public Transform Pieza;
        public Modo Modo;
        public Vector3 Valor;
        public bool[] Ejes;
        public bool Escrito;
        public Vector3 BaseV;
        public Quaternion BaseQ;
        public Vector3 UltimoV;
        public Quaternion UltimoQ;
    }

    private readonly Dictionary<string, Transform> piezas = new();
    private readonly List<Canal> canales = new();
    private float inicio = -100f;
    private float mantener;
    private int rebotes;

    public bool Preparado => piezas.Count > 0;

    private void Awake()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            int i = n.IndexOf('_', n.IndexOf('_') + 1);
            string clave = i >= 0 ? n.Substring(i + 1) : n;

            if (!piezas.ContainsKey(clave))
                piezas[clave] = t;
        }
    }

    private Transform P(string clave) => piezas.TryGetValue(clave, out Transform t) ? t : null;

    private void Escala(string clave, float x = float.NaN, float y = float.NaN, float z = float.NaN)
    {
        Transform t = P(clave);
        if (t == null) return;

        canales.Add(new Canal
        {
            Pieza = t,
            Modo = Modo.Escala,
            Valor = new Vector3(float.IsNaN(x) ? 0 : x, float.IsNaN(y) ? 0 : y, float.IsNaN(z) ? 0 : z),
            Ejes = new[] { !float.IsNaN(x), !float.IsNaN(y), !float.IsNaN(z) }
        });
    }

    private void Mover(string clave, Vector3 desplazamiento)
    {
        Transform t = P(clave);
        if (t != null) canales.Add(new Canal { Pieza = t, Modo = Modo.Posicion, Valor = desplazamiento * intensidad });
    }

    private void Girar(string clave, Vector3 grados)
    {
        Transform t = P(clave);
        if (t != null) canales.Add(new Canal { Pieza = t, Modo = Modo.Rotacion, Valor = grados * intensidad });
    }

    public void Hacer(TipoSenalMus tipo)
    {
        canales.Clear();
        rebotes = 1;
        mantener = duracionMantener;

        switch (tipo)
        {
            case TipoSenalMus.TreintaYUna:
                Escala("CONTROL_OJO_I", z: 0.04f);
                Mover("CONTROL_CEJA_I", new Vector3(0, 0, -0.0012f));
                Mover("Mejilla_I", new Vector3(0, 0, 0.0008f));
                Girar("CONTROL_CABEZA", ejeLadear * 6f);
                break;

            case TipoSenalMus.Duples:
                Mover("CONTROL_CEJA_I", new Vector3(0, 0, 0.0022f));
                Mover("CONTROL_CEJA_D", new Vector3(0, 0, 0.0022f));
                Escala("CONTROL_OJO_I", z: 1.15f);
                Escala("CONTROL_OJO_D", z: 1.15f);
                rebotes = 2;
                break;

            case TipoSenalMus.DosReyes:
                Mordisco();
                break;

            case TipoSenalMus.DosAses:
                Escala("Lengua_DosAses", z: 0.065f * Mathf.Max(1f, intensidad));
                break;

            case TipoSenalMus.TresReyes:
                Mordisco();
                Girar("CONTROL_CABEZA", ejeLadear * -10f);
                Mover("CONTROL_CEJA_D", new Vector3(0, 0, 0.0015f));
                break;

            case TipoSenalMus.TresAses:
                Escala("Lengua_DosAses", z: 0.065f * Mathf.Max(1f, intensidad));
                Girar("Lengua_DosAses", new Vector3(0, 0, 18f));
                Girar("CONTROL_CABEZA", ejeLadear * 10f);
                break;

            case TipoSenalMus.Ciego:
                Escala("CONTROL_OJO_I", z: 0.04f);
                Escala("CONTROL_OJO_D", z: 0.04f);
                Mover("CONTROL_CEJA_I", new Vector3(0, 0, -0.0008f));
                Mover("CONTROL_CEJA_D", new Vector3(0, 0, -0.0008f));
                Girar("CONTROL_CABEZA", ejeAgachar * 5f);
                mantener = duracionMantener * 1.3f;
                break;
        }

        inicio = Time.time;
    }

    private void Mordisco()
    {
        Escala("Sonrisa", x: 0.001f);
        Escala("Boca_Mordisco_Labio", x: 0.135f);
        Escala("Boca_Mordisco_Fondo", x: 0.135f);
        Escala("Boca_Mordisco_Dientes", x: 0.11f);
        Mover("Boca_Mordisco_Labio", new Vector3(0, 0, 0.0012f));
    }

    private float Peso(float t)
    {
        float total = duracionEntrada + mantener + duracionSalida;

        if (t < 0f || t > total)
            return 0f;

        float w = t < duracionEntrada
            ? t / duracionEntrada
            : t > duracionEntrada + mantener
                ? 1f - (t - duracionEntrada - mantener) / duracionSalida
                : 1f;

        if (rebotes > 1 && t >= duracionEntrada && t <= duracionEntrada + mantener)
        {
            float k = (t - duracionEntrada) / mantener;
            w *= 0.55f + 0.45f * Mathf.Abs(Mathf.Cos(k * Mathf.PI * rebotes));
        }

        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(w));
    }

    private void LateUpdate()
    {
        if (canales.Count == 0)
            return;

        float w = Peso(Time.time - inicio);

        bool terminado = w <= 0f && Time.time - inicio > duracionEntrada + mantener + duracionSalida;

        foreach (Canal c in canales)
        {
            if (c.Pieza == null)
                continue;

            switch (c.Modo)
            {
                case Modo.Escala:
                {
                    Vector3 actual = c.Pieza.localScale;
                    Vector3 b = c.Escrito && actual == c.UltimoV ? c.BaseV : actual;
                    Vector3 s = b;
                    if (c.Ejes[0]) s.x = Mathf.Lerp(b.x, c.Valor.x, w);
                    if (c.Ejes[1]) s.y = Mathf.Lerp(b.y, c.Valor.y, w);
                    if (c.Ejes[2]) s.z = Mathf.Lerp(b.z, c.Valor.z, w);
                    c.Pieza.localScale = s;
                    c.BaseV = b;
                    c.UltimoV = s;
                    break;
                }

                case Modo.Posicion:
                {
                    Vector3 actual = c.Pieza.localPosition;
                    Vector3 b = c.Escrito && actual == c.UltimoV ? c.BaseV : actual;
                    Vector3 p = b + c.Valor * w;
                    c.Pieza.localPosition = p;
                    c.BaseV = b;
                    c.UltimoV = p;
                    break;
                }

                case Modo.Rotacion:
                {
                    Quaternion actual = c.Pieza.localRotation;
                    Quaternion b = c.Escrito && actual == c.UltimoQ ? c.BaseQ : actual;
                    Quaternion r = b * Quaternion.Euler(c.Valor * w);
                    c.Pieza.localRotation = r;
                    c.BaseQ = b;
                    c.UltimoQ = r;
                    break;
                }
            }

            c.Escrito = true;
        }

        if (terminado)
            canales.Clear();
    }

    [ContextMenu("Probar 31")] private void P31() => Hacer(TipoSenalMus.TreintaYUna);
    [ContextMenu("Probar Duples")] private void PDuples() => Hacer(TipoSenalMus.Duples);
    [ContextMenu("Probar Dos reyes")] private void PDosReyes() => Hacer(TipoSenalMus.DosReyes);
    [ContextMenu("Probar Dos ases")] private void PDosAses() => Hacer(TipoSenalMus.DosAses);
    [ContextMenu("Probar Tres reyes")] private void PTresReyes() => Hacer(TipoSenalMus.TresReyes);
    [ContextMenu("Probar Tres ases")] private void PTresAses() => Hacer(TipoSenalMus.TresAses);
    [ContextMenu("Probar Ciego")] private void PCiego() => Hacer(TipoSenalMus.Ciego);
}
