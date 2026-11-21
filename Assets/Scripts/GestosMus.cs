using System.Collections.Generic;
using UnityEngine;

public enum ReaccionMus
{
    Asentir,
    Negar,
    Animarse,
    Ordago,
    Encoger,
    MirarCartas,
    Celebrar,
    Triste
}

public class GestosMus : MonoBehaviour
{
    [Header("Senas")]
    [Range(0.5f, 3f)][SerializeField] private float intensidad = 1.5f;
    [SerializeField] private float duracionMantener = 1.2f;
    [SerializeField] private float duracionEntrada = 0.18f;
    [SerializeField] private float duracionSalida = 0.25f;

    [Header("Vida")]
    [SerializeField] private bool vida = true;
    [SerializeField] private float respiracion = 0.018f;
    [SerializeField] private float respiracionesPorMinuto = 16f;
    [SerializeField] private float parpadeoMinimo = 2.5f;
    [SerializeField] private float parpadeoMaximo = 6f;
    [SerializeField] private float balanceoCabeza = 3f;
    [SerializeField] private float giroMaximoCabeza = 35f;

    [Header("Ejes del modelo (cambialos si gira hacia donde no toca)")]
    [Tooltip("Eje para girar la cabeza a izquierda y derecha.")]
    [SerializeField] private Vector3 ejeGirar = new(0f, 0f, 1f);
    [Tooltip("Eje para ladear la cabeza hacia un hombro.")]
    [SerializeField] private Vector3 ejeLadear = new(0f, 1f, 0f);
    [Tooltip("Eje para asentir o agachar la cabeza.")]
    [SerializeField] private Vector3 ejeAgachar = new(1f, 0f, 0f);

    private sealed class Acumulado
    {
        public Vector3 Posicion;
        public Quaternion Rotacion = Quaternion.identity;
        public Vector3 EscalaPor = Vector3.one;
        public Vector3 EscalaObjetivo;
        public Vector3 PesoObjetivo;
        public bool Usado;
        public bool Escrito;
        public Vector3 BaseP, BaseS, UltimaP, UltimaS;
        public Quaternion BaseR, UltimaR;
    }

    private enum Modo { Escala, Posicion, Rotacion }

    private struct Canal
    {
        public string Pieza;
        public Modo Modo;
        public Vector3 Valor;
        public bool[] Ejes;
    }

    private readonly Dictionary<string, Transform> piezas = new();
    private readonly Dictionary<Transform, Acumulado> acumulados = new();
    private readonly List<Canal> canales = new();
    private float inicioSena = -100f;
    private float mantener;
    private int rebotes;

    private ReaccionMus reaccion;
    private float inicioReaccion = -100f;
    private float duracionReaccion;

    private Vector3? objetivoMirada;
    private float finMirada;
    private float giroActual;
    private float proximoParpadeo;
    private float inicioParpadeo = -100f;
    private float semilla;

    public bool Preparado => piezas.Count > 0;

    private void Awake()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            int primero = n.IndexOf('_');
            int segundo = primero >= 0 ? n.IndexOf('_', primero + 1) : -1;
            string clave = segundo >= 0 ? n.Substring(segundo + 1) : n;

            if (!piezas.ContainsKey(clave))
                piezas[clave] = t;
        }

        semilla = Random.value * 100f;
        proximoParpadeo = Time.time + Random.Range(parpadeoMinimo, parpadeoMaximo);
    }

    private Transform P(string clave) => piezas.TryGetValue(clave, out Transform t) ? t : null;

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

        inicioSena = Time.time;
    }

    public void Reaccionar(ReaccionMus tipo)
    {
        reaccion = tipo;
        inicioReaccion = Time.time;
        duracionReaccion = tipo switch
        {
            ReaccionMus.Asentir => 0.9f,
            ReaccionMus.Negar => 1.0f,
            ReaccionMus.Animarse => 1.0f,
            ReaccionMus.Ordago => 1.6f,
            ReaccionMus.Encoger => 0.9f,
            ReaccionMus.MirarCartas => 1.2f,
            ReaccionMus.Celebrar => 2.2f,
            _ => 2.2f
        };
    }

    public void MirarA(Vector3 puntoMundo, float segundos)
    {
        objetivoMirada = puntoMundo;
        finMirada = Time.time + segundos;
    }

    private void Mordisco()
    {
        Escala("Sonrisa", x: 0.001f);
        Escala("Boca_Mordisco_Labio", x: 0.135f);
        Escala("Boca_Mordisco_Fondo", x: 0.135f);
        Escala("Boca_Mordisco_Dientes", x: 0.11f);
        Mover("Boca_Mordisco_Labio", new Vector3(0, 0, 0.0012f));
    }

    private void Escala(string clave, float x = float.NaN, float y = float.NaN, float z = float.NaN)
    {
        canales.Add(new Canal
        {
            Pieza = clave,
            Modo = Modo.Escala,
            Valor = new Vector3(float.IsNaN(x) ? 0 : x, float.IsNaN(y) ? 0 : y, float.IsNaN(z) ? 0 : z),
            Ejes = new[] { !float.IsNaN(x), !float.IsNaN(y), !float.IsNaN(z) }
        });
    }

    private void Mover(string clave, Vector3 desplazamiento) =>
        canales.Add(new Canal { Pieza = clave, Modo = Modo.Posicion, Valor = desplazamiento * intensidad });

    private void Girar(string clave, Vector3 grados) =>
        canales.Add(new Canal { Pieza = clave, Modo = Modo.Rotacion, Valor = grados * intensidad });

    private Acumulado A(string clave)
    {
        Transform t = P(clave);

        if (t == null)
            return null;

        if (!acumulados.TryGetValue(t, out Acumulado a))
        {
            a = new Acumulado();
            acumulados[t] = a;
        }

        a.Usado = true;
        return a;
    }

    private void SumarPosicion(string clave, Vector3 delta)
    {
        Acumulado a = A(clave);
        if (a != null) a.Posicion += delta;
    }

    private void SumarGiro(string clave, Vector3 eje, float grados)
    {
        Acumulado a = A(clave);
        if (a != null) a.Rotacion *= Quaternion.AngleAxis(grados, eje);
    }

    private void MultiplicarEscala(string clave, Vector3 factor)
    {
        Acumulado a = A(clave);
        if (a != null) a.EscalaPor = Vector3.Scale(a.EscalaPor, factor);
    }

    private void FijarEscala(string clave, int eje, float valor, float peso)
    {
        Acumulado a = A(clave);
        if (a == null || peso <= 0f) return;

        if (peso >= a.PesoObjetivo[eje])
        {
            Vector3 v = a.EscalaObjetivo;
            v[eje] = valor;
            a.EscalaObjetivo = v;
            Vector3 p = a.PesoObjetivo;
            p[eje] = peso;
            a.PesoObjetivo = p;
        }
    }

    private float PesoSena(float t)
    {
        float total = duracionEntrada + mantener + duracionSalida;

        if (t < 0f || t > total)
            return 0f;

        float w = t < duracionEntrada ? t / duracionEntrada
            : t > duracionEntrada + mantener ? 1f - (t - duracionEntrada - mantener) / duracionSalida
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
        foreach (Acumulado a in acumulados.Values)
        {
            a.Posicion = Vector3.zero;
            a.Rotacion = Quaternion.identity;
            a.EscalaPor = Vector3.one;
            a.PesoObjetivo = Vector3.zero;
            a.Usado = false;
        }

        if (vida)
            Vida();

        AplicarReaccion();
        AplicarSena();
        Escribir();
    }

    private void Vida()
    {
        float t = Time.time;
        float respira = Mathf.Sin((t + semilla) * Mathf.PI * 2f * respiracionesPorMinuto / 60f);

        MultiplicarEscala("Poncho_Silueta", new Vector3(1f + respiracion * respira, 1f + respiracion * respira, 1f + respiracion * 0.4f * respira));
        SumarPosicion("CONTROL_CABEZA", new Vector3(0f, 0f, 0.00018f * respira));
        SumarPosicion("Manopla_D", new Vector3(0f, 0f, 0.0001f * respira));
        SumarPosicion("Manopla_I", new Vector3(0f, 0f, 0.0001f * respira));

        float ruidoGiro = (Mathf.PerlinNoise(semilla, t * 0.15f) - 0.5f) * 2f * balanceoCabeza;
        float ruidoLadeo = (Mathf.PerlinNoise(semilla + 10f, t * 0.12f) - 0.5f) * 2f * balanceoCabeza * 0.6f;

        float objetivoGiro = ruidoGiro;
        float mirarPupila = 0f;

        Transform cabeza = P("CONTROL_CABEZA");

        if (objetivoMirada.HasValue && Time.time < finMirada && cabeza != null && cabeza.parent != null)
        {
            Vector3 local = cabeza.parent.InverseTransformPoint(objetivoMirada.Value) - cabeza.localPosition;
            float angulo = Mathf.Atan2(local.x, -local.y) * Mathf.Rad2Deg;
            objetivoGiro = Mathf.Clamp(angulo, -giroMaximoCabeza, giroMaximoCabeza);
            mirarPupila = Mathf.Clamp((angulo - objetivoGiro) / 30f, -1f, 1f);
        }

        giroActual = Mathf.Lerp(giroActual, objetivoGiro, 1f - Mathf.Exp(-Time.deltaTime * 5f));
        SumarGiro("CONTROL_CABEZA", ejeGirar, giroActual);
        SumarGiro("CONTROL_CABEZA", ejeLadear, ruidoLadeo);
        SumarPosicion("CONTROL_MIRADA_I", new Vector3(0.0005f * mirarPupila, 0f, 0f));
        SumarPosicion("CONTROL_MIRADA_D", new Vector3(0.0005f * mirarPupila, 0f, 0f));

        if (Time.time >= proximoParpadeo)
        {
            inicioParpadeo = Time.time;
            proximoParpadeo = Time.time + Random.Range(parpadeoMinimo, parpadeoMaximo);
        }

        float tp = Time.time - inicioParpadeo;

        if (tp >= 0f && tp < 0.18f)
        {
            float cierre = tp < 0.07f ? tp / 0.07f : 1f - (tp - 0.07f) / 0.11f;
            FijarEscala("CONTROL_OJO_I", 2, 0.06f, Mathf.Clamp01(cierre));
            FijarEscala("CONTROL_OJO_D", 2, 0.06f, Mathf.Clamp01(cierre));
        }
    }

    private void AplicarReaccion()
    {
        float t = Time.time - inicioReaccion;

        if (t < 0f || t > duracionReaccion)
            return;

        float k = t / duracionReaccion;
        float env = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
        float f = intensidad;

        switch (reaccion)
        {
            case ReaccionMus.Asentir:
                SumarGiro("CONTROL_CABEZA", ejeAgachar, Mathf.Sin(k * Mathf.PI * 4f) * 9f * env * f);
                break;

            case ReaccionMus.Negar:
                SumarGiro("CONTROL_CABEZA", ejeGirar, Mathf.Sin(k * Mathf.PI * 6f) * 14f * env * f);
                SumarPosicion("CONTROL_CEJA_I", new Vector3(0, 0, -0.0007f * env));
                SumarPosicion("CONTROL_CEJA_D", new Vector3(0, 0, -0.0007f * env));
                break;

            case ReaccionMus.Animarse:
                SumarGiro("CONTROL_CABEZA", ejeAgachar, -6f * env * f);
                SumarPosicion("CONTROL_CEJA_I", new Vector3(0, 0, 0.0015f * env));
                SumarPosicion("CONTROL_CEJA_D", new Vector3(0, 0, 0.0015f * env));
                SumarPosicion("Manopla_D", new Vector3(0, -0.002f * env, 0.0015f * env));
                break;

            case ReaccionMus.Ordago:
                SumarPosicion("Manopla_D", new Vector3(0, -0.001f * env, 0.006f * env * f));
                SumarPosicion("Manopla_I", new Vector3(0, -0.001f * env, 0.006f * env * f));
                SumarPosicion("CONTROL_CEJA_I", new Vector3(0, 0, 0.0022f * env));
                SumarPosicion("CONTROL_CEJA_D", new Vector3(0, 0, 0.0022f * env));
                FijarEscala("CONTROL_OJO_I", 2, 1.2f, env);
                FijarEscala("CONTROL_OJO_D", 2, 1.2f, env);
                break;

            case ReaccionMus.Encoger:
                SumarPosicion("Manopla_D", new Vector3(0, 0, 0.002f * env));
                SumarPosicion("Manopla_I", new Vector3(0, 0, 0.002f * env));
                SumarGiro("CONTROL_CABEZA", ejeLadear, 7f * env * f);
                break;

            case ReaccionMus.MirarCartas:
                SumarGiro("CONTROL_CABEZA", ejeAgachar, 12f * env);
                SumarPosicion("CONTROL_MIRADA_I", new Vector3(0, 0, -0.0005f * env));
                SumarPosicion("CONTROL_MIRADA_D", new Vector3(0, 0, -0.0005f * env));
                break;

            case ReaccionMus.Celebrar:
                float salto = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f)) * env;
                SumarPosicion("CONTROL_CABEZA", new Vector3(0, 0, 0.0012f * salto));
                SumarPosicion("Manopla_D", new Vector3(0, 0, 0.007f * env * f));
                SumarPosicion("Manopla_I", new Vector3(0, 0, 0.007f * env * f));
                MultiplicarEscala("Mejilla_I", Vector3.one * (1f + 0.3f * env));
                MultiplicarEscala("Mejilla_D", Vector3.one * (1f + 0.3f * env));
                break;

            case ReaccionMus.Triste:
                SumarGiro("CONTROL_CABEZA", ejeAgachar, 14f * env * f);
                SumarPosicion("CONTROL_CEJA_I", new Vector3(0, 0, -0.001f * env));
                SumarPosicion("CONTROL_CEJA_D", new Vector3(0, 0, -0.001f * env));
                SumarPosicion("Manopla_D", new Vector3(0, 0, -0.0015f * env));
                SumarPosicion("Manopla_I", new Vector3(0, 0, -0.0015f * env));
                break;
        }
    }

    private void AplicarSena()
    {
        if (canales.Count == 0)
            return;

        float w = PesoSena(Time.time - inicioSena);

        if (w <= 0f)
        {
            if (Time.time - inicioSena > duracionEntrada + mantener + duracionSalida)
                canales.Clear();
            return;
        }

        foreach (Canal c in canales)
        {
            switch (c.Modo)
            {
                case Modo.Escala:
                    for (int eje = 0; eje < 3; eje++)
                        if (c.Ejes[eje])
                            FijarEscala(c.Pieza, eje, c.Valor[eje], w);
                    break;
                case Modo.Posicion:
                    SumarPosicion(c.Pieza, c.Valor * w);
                    break;
                case Modo.Rotacion:
                    Acumulado a = A(c.Pieza);
                    if (a != null) a.Rotacion *= Quaternion.Euler(c.Valor * w);
                    break;
            }
        }
    }

    private void Escribir()
    {
        foreach (KeyValuePair<Transform, Acumulado> par in acumulados)
        {
            Transform t = par.Key;
            Acumulado a = par.Value;

            if (t == null)
                continue;

            Vector3 p = t.localPosition;
            Quaternion r = t.localRotation;
            Vector3 s = t.localScale;

            Vector3 baseP = a.Escrito && p == a.UltimaP ? a.BaseP : p;
            Quaternion baseR = a.Escrito && r == a.UltimaR ? a.BaseR : r;
            Vector3 baseS = a.Escrito && s == a.UltimaS ? a.BaseS : s;

            Vector3 nuevaS = Vector3.Scale(baseS, a.EscalaPor);
            for (int eje = 0; eje < 3; eje++)
                if (a.PesoObjetivo[eje] > 0f)
                    nuevaS[eje] = Mathf.Lerp(nuevaS[eje], a.EscalaObjetivo[eje], a.PesoObjetivo[eje]);

            Vector3 nuevaP = baseP + a.Posicion;
            Quaternion nuevaR = baseR * a.Rotacion;

            t.localPosition = nuevaP;
            t.localRotation = nuevaR;
            t.localScale = nuevaS;

            a.BaseP = baseP;
            a.BaseR = baseR;
            a.BaseS = baseS;
            a.UltimaP = nuevaP;
            a.UltimaR = nuevaR;
            a.UltimaS = nuevaS;
            a.Escrito = true;
        }
    }

    [ContextMenu("Probar 31")] private void P31() => Hacer(TipoSenalMus.TreintaYUna);
    [ContextMenu("Probar Duples")] private void PDuples() => Hacer(TipoSenalMus.Duples);
    [ContextMenu("Probar Dos reyes")] private void PDosReyes() => Hacer(TipoSenalMus.DosReyes);
    [ContextMenu("Probar Dos ases")] private void PDosAses() => Hacer(TipoSenalMus.DosAses);
    [ContextMenu("Probar Tres reyes")] private void PTresReyes() => Hacer(TipoSenalMus.TresReyes);
    [ContextMenu("Probar Tres ases")] private void PTresAses() => Hacer(TipoSenalMus.TresAses);
    [ContextMenu("Probar Ciego")] private void PCiego() => Hacer(TipoSenalMus.Ciego);
    [ContextMenu("Reaccion: asentir")] private void RAsentir() => Reaccionar(ReaccionMus.Asentir);
    [ContextMenu("Reaccion: negar")] private void RNegar() => Reaccionar(ReaccionMus.Negar);
    [ContextMenu("Reaccion: ordago")] private void ROrdago() => Reaccionar(ReaccionMus.Ordago);
    [ContextMenu("Reaccion: celebrar")] private void RCelebrar() => Reaccionar(ReaccionMus.Celebrar);
    [ContextMenu("Reaccion: triste")] private void RTriste() => Reaccionar(ReaccionMus.Triste);
}
