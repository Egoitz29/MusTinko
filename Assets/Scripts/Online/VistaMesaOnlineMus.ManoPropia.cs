using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// TU MANO EN 3D (lo que ves al mirar abajo)
//  - Tus cartas de la escena (ManoJugador3D) se quedan desactivadas pero sin borrar: siguen sirviendo para
//    tocarlas y descartar (sus colisionadores y su movimiento al seleccionarlas), y encima va la carta nueva de Blender.
//  - Al mirar tus cartas, se esconde lo que se ponga en medio (sillas, mesa, etc.) para que se vean limpias.
public partial class VistaMesaOnlineMus
{
    [Header("Tus cartas 3D (al mirar abajo)")]
    [Tooltip("Usa las cartas nuevas de Blender en tu mano. Si lo desmarcas, vuelven las de tu escena.")]
    [SerializeField] private bool usarCartasNuevasEnMano = true;
    [Tooltip("Al mirar tus cartas, esconde lo que haya entre la camara y ellas.")]
    [SerializeField] private bool despejarVistaCartas = true;
    [Tooltip("Cuanto reflejan la luz las cartas 3D (1 = normal). Mas alto = se ven mas vivas.")]
    [UnityEngine.Range(0.8f, 2.2f)][SerializeField] private float brilloCartas = 1.45f;
    [Tooltip("Luz suave encima de tus cartas cuando las miras (0 = sin luz).")]
    [UnityEngine.Range(0f, 4f)][SerializeField] private float luzLectura = 1.6f;

    private Light luzCartas;

    private bool manoPropiaPreparada;
    private float inclinacionVerCartas = 52f;
    private Transform[] cartasDeTuEscena;
    private GameObject[] cartasNuevasMano;
    private readonly List<Renderer> rendersManoVieja = new();
    private string firmaManoMostrada;
    private MonoBehaviour visibilidadVieja;

    private Renderer[] rendersEscena;
    private float proximoRepasoEscena;
    private readonly HashSet<Renderer> estorbosOcultos = new();
    private readonly HashSet<Renderer> estorbosAhora = new();

    // ---------- Preparar (una vez) ----------

    private void PrepararManoPropia()
    {
        if (manoPropiaPreparada || manoJugador == null || !usarCartasNuevasEnMano || !Cartas3DMus.Disponible)
            return;

        manoPropiaPreparada = true;

        // Las cuatro cartas de tu escena, en el orden de la mano (las de CartasMano3D).
        MonoBehaviour cartasMano = manoJugador.GetComponent("CartasMano3D") as MonoBehaviour;
        cartasDeTuEscena = cartasMano != null ? Leer<Transform[]>(cartasMano, "cartas3D") : null;

        if (cartasDeTuEscena == null || cartasDeTuEscena.Length != 4)
        {
            List<Transform> encontradas = new();

            foreach (Transform t in manoJugador)
                if (t.GetComponent<Collider>() != null)
                    encontradas.Add(t);

            cartasDeTuEscena = encontradas.Count == 4 ? encontradas.ToArray() : null;
        }

        if (cartasDeTuEscena == null)
        {
            Debug.LogWarning("[MANO 3D] No se encontraron tus 4 cartas en ManoJugador3D: se quedan las de siempre.");
            return;
        }

        // Tu script de visibilidad se apaga (al apagarse deja todo como estaba) y la visibilidad la llevamos aqui.
        visibilidadVieja = manoJugador.GetComponent("VisibilidadMano3DMus") as MonoBehaviour;

        if (visibilidadVieja != null)
        {
            FieldInfo campo = visibilidadVieja.GetType().GetField("inclinacionMinima", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (campo != null && campo.GetValue(visibilidadVieja) is float valor)
                inclinacionVerCartas = valor;

            visibilidadVieja.enabled = false;
        }

        rendersManoVieja.AddRange(manoJugador.GetComponentsInChildren<Renderer>(true));
        cartasNuevasMano = new GameObject[4];

        for (int i = 0; i < 4; i++)
        {
            Transform vieja = cartasDeTuEscena[i];
            GameObject nueva = Cartas3DMus.Crear("CartaNueva", vieja, 1f, null);

            if (nueva == null)
                continue;

            // Tu carta es un cubo escalado (ancho, alto, grosor); la nueva ocupa el mismo alto, sin deformarse.
            Vector3 e = vieja.localScale;
            float alto = Mathf.Max(0.0001f, e.y);
            nueva.transform.localPosition = Vector3.zero;
            nueva.transform.localRotation = Quaternion.identity;
            nueva.transform.localScale = new Vector3(alto / Mathf.Max(0.0001f, e.x), 1f, alto / Mathf.Max(0.0001f, e.z));
            cartasNuevasMano[i] = nueva;
        }
    }

    // ---------- Cada frame ----------

    private void ActualizarManoPropia()
    {
        PrepararManoPropia();

        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;
        float inclinacion = cam != null ? Mathf.Asin(Mathf.Clamp(-cam.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg : 0f;
        bool mirandoCartas = cam != null && PrimeraPersona && inclinacion >= inclinacionVerCartas;

        if (manoPropiaPreparada && cartasDeTuEscena != null)
        {
            // Las de tu escena, siempre escondidas (siguen ahi para tocarlas).
            foreach (Renderer r in rendersManoVieja)
                if (r != null && r.enabled)
                    r.enabled = false;

            bool hayMano = cartasLocalesMostradas[0] >= 0;
            string firma = string.Join(",", cartasLocalesMostradas);

            if (firma != firmaManoMostrada)
            {
                firmaManoMostrada = firma;

                for (int i = 0; i < 4; i++)
                    if (cartasNuevasMano[i] != null)
                        Cartas3DMus.PonerCara(cartasNuevasMano[i], hayMano ? CartasMus.Texto(cartasLocalesMostradas[i]) : null);
            }

            for (int i = 0; i < 4; i++)
                if (cartasNuevasMano[i] != null)
                {
                    MeshRenderer r = cartasNuevasMano[i].GetComponent<MeshRenderer>();
                    bool ver = mirandoCartas && hayMano;

                    if (r.enabled != ver)
                        r.enabled = ver;
                }
        }

        DespejarVistaCartas(cam, mirandoCartas && despejarVistaCartas);

        if (!Mathf.Approximately(Cartas3DMus.Brillo, brilloCartas))
            Cartas3DMus.PonerBrillo(brilloCartas);

        ActualizarLuzLectura(cam, mirandoCartas);
    }

    // Luz de lectura: como acercar las cartas a la lampara. Se enciende poco a poco al mirarlas.
    private void ActualizarLuzLectura(Camera cam, bool mirandoCartas)
    {
        if (luzCartas == null)
        {
            if (!mirandoCartas || luzLectura <= 0f || manoJugador == null)
                return;

            GameObject go = new("LuzDeLectura");
            luzCartas = go.AddComponent<Light>();
            luzCartas.type = LightType.Point;
            luzCartas.color = new Color(1f, 0.96f, 0.88f);
            luzCartas.shadows = LightShadows.None;
            luzCartas.intensity = 0f;
        }

        float objetivo = mirandoCartas && manoJugador != null ? luzLectura : 0f;
        luzCartas.intensity = Mathf.MoveTowards(luzCartas.intensity, objetivo, Time.deltaTime * luzLectura * 4f);
        luzCartas.enabled = luzCartas.intensity > 0.01f;

        if (!luzCartas.enabled || cam == null || manoJugador == null)
            return;

        // Entre tus ojos y las cartas, un poco por encima: alumbra las cartas y poco mas.
        Vector3 ojos = cam.transform.position;
        Vector3 cartas = manoJugador.position;
        float distancia = Vector3.Distance(ojos, cartas);
        luzCartas.transform.position = Vector3.Lerp(ojos, cartas, 0.45f) + Vector3.up * (distancia * 0.25f);
        luzCartas.range = distancia * 1.3f;
    }

    // Esconde lo que se ponga entre la camara y tus cartas mientras las miras.
    private void DespejarVistaCartas(Camera cam, bool activo)
    {
        estorbosAhora.Clear();

        if (activo && manoJugador != null)
        {
            if (rendersEscena == null || Time.time > proximoRepasoEscena)
            {
                rendersEscena = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                proximoRepasoEscena = Time.time + 3f;
            }

            Vector3 origen = cam.transform.position;
            List<Vector3> puntos = new();

            foreach (Transform carta in PuntosDeLaMano())
                for (int esq = 0; esq < 5; esq++)
                {
                    Vector2 p = esq == 4 ? Vector2.zero : new Vector2(esq % 2 == 0 ? -0.6f : 0.6f, esq < 2 ? -0.6f : 0.6f);
                    puntos.Add(carta.TransformPoint(p.x, p.y, 0f));
                }

            foreach (Renderer r in rendersEscena)
            {
                if (r == null || (!r.enabled && !estorbosOcultos.Contains(r)) || r.transform.IsChildOf(manoJugador) || r is ParticleSystemRenderer)
                    continue;

                Bounds b = r.bounds;

                foreach (Vector3 punto in puntos)
                {
                    Vector3 hacia = punto - origen;
                    float distancia = hacia.magnitude;

                    if (b.IntersectRay(new Ray(origen, hacia / distancia), out float d) && d < distancia - 0.03f)
                    {
                        estorbosAhora.Add(r);
                        break;
                    }
                }
            }
        }

        // Se vuelve a ensenar lo que ya no estorba, y se esconde lo nuevo.
        List<Renderer> volver = new();

        foreach (Renderer r in estorbosOcultos)
            if (!estorbosAhora.Contains(r))
                volver.Add(r);

        foreach (Renderer r in volver)
        {
            estorbosOcultos.Remove(r);

            if (r != null)
                r.enabled = true;
        }

        foreach (Renderer r in estorbosAhora)
            if (r.enabled)
            {
                r.enabled = false;
                estorbosOcultos.Add(r);
            }
    }

    private IEnumerable<Transform> PuntosDeLaMano()
    {
        if (cartasDeTuEscena != null)
        {
            foreach (Transform t in cartasDeTuEscena)
                if (t != null)
                    yield return t;
        }
        else
        {
            foreach (Transform t in manoJugador)
                yield return t;
        }
    }
}
