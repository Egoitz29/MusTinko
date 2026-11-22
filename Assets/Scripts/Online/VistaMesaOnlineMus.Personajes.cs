using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Cada jugador lleva SU personaje (elegido en la taquilla) a la silla donde se sienta:
// los modelos 3D se cambian de silla, y se les ponen sus complementos (bigote, gafas, sombrero...).
// Ademas, con la taquilla abierta la camara se pone delante de tu personaje (probador).
public partial class VistaMesaOnlineMus
{
    [Header("Personajes y complementos")]
    [SerializeField] private string rutaComplementos = "Modelos/Complementos_Tinko";
    [Tooltip("Distancia de la camara del probador (en alturas de cabeza).")]
    [SerializeField] private float distanciaProbador = 1.25f;

    private GameObject[] modeloDePersonaje;
    private Vector3[] anclaPosicion;      // cadera del que estaba sentado en cada silla al empezar
    private Quaternion[] anclaRotacion;   // giro del cuerpo (CONTROL_RAIZ) de ese mismo
    private Quaternion[] giroCuerpo;      // giro del cuerpo respecto a la raiz de cada modelo
    private Vector3[] caderaLocal;        // cadera respecto a la raiz de cada modelo
    private readonly Dictionary<GameObject, Transform> cuerpos = new();
    private readonly Dictionary<GameObject, Transform> cabezas = new();
    private readonly Dictionary<GameObject, List<Renderer>> rendersComplementos = new();

    // En tus modelos la raiz (LILA_Posicion...) no tiene giro: el giro y la escala estan en el modelo hijo,
    // y cada uno es distinto. Por eso se trabaja siempre con el cuerpo (CONTROL_RAIZ) y la cabeza (CONTROL_CABEZA).
    private Transform Buscar(GameObject modelo, string final, Dictionary<GameObject, Transform> cache)
    {
        if (modelo == null)
            return null;

        if (cache.TryGetValue(modelo, out Transform t) && t != null)
            return t;

        t = null;

        foreach (Transform h in modelo.GetComponentsInChildren<Transform>(true))
            if (h.name.EndsWith(final))
            {
                t = h;
                break;
            }

        cache[modelo] = t;
        return t;
    }

    private Vector3 CuerpoDe(GameObject modelo)
    {
        Transform t = Buscar(modelo, "_CONTROL_RAIZ", cuerpos);
        return t != null ? t.position : modelo != null ? modelo.transform.position : Vector3.zero;
    }

    private Vector3 CabezaDe(GameObject modelo)
    {
        Transform t = Buscar(modelo, "_CONTROL_CABEZA", cabezas);
        return t != null ? t.position : CuerpoDe(modelo) + Vector3.up * alturaCabezas;
    }

    // La cadera (donde se apoya en la silla): un tercio del camino de los pies a la cabeza.
    private Vector3 CaderaDe(GameObject modelo) => Vector3.Lerp(CuerpoDe(modelo), CabezaDe(modelo), 0.32f);

    private Quaternion GiroCuerpoDe(GameObject modelo)
    {
        Transform t = Buscar(modelo, "_CONTROL_RAIZ", cuerpos);
        return t != null ? t.rotation : modelo.transform.rotation;
    }
    private string claveColocacion;
    private string claveComplementos;
    private bool necesitaColorear;

    private GameObject plantillaComplementos;
    private bool plantillaCargada;
    private readonly Dictionary<GameObject, List<GameObject>> complementosPuestos = new();
    private readonly Dictionary<GameObject, List<Renderer>> piezasEscondidas = new();

    private int complementoProbador = -1;

    // ---------- Quien soy y que llevo ----------

    // Mi silla: en partida la de la partida; en la sala la que he elegido; en el menu la 0.
    private int MiPuesto()
    {
        if (partida.PuestoLocal >= 0)
            return partida.PuestoLocal;

        NetworkManager red = NetworkManager.Singleton;

        if (red == null || !red.IsListening || !partida.IsSpawned)
            return 0;

        for (int p = 0; p < 4; p++)
            if (partida.ClienteEnSitio(p) == red.LocalClientId)
                return p;

        return -1;
    }

    private bool EnLaRed => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && partida.IsSpawned;

    // Aspecto (personaje, traje, complementos) de quien esta en esa silla.
    private int AspectoDePuesto(int puesto)
    {
        if (!EnLaRed)
            return puesto == 0 ? TaquillaMus.AspectoPropio : 0;

        return partida.AspectoEnSitio(puesto);
    }

    // ---------- Cada frame ----------

    private void ActualizarPersonajes()
    {
        // Sin conexion: yo en la silla 0 con mi personaje, el resto los que sobran.
        if (!EnLaRed)
        {
            int[] r = ColoresJugadoresMus.Repartir(new[] { TaquillaMus.PersonajeMio, -1, -1, -1 });
            ColoresJugadoresMus.PonerPersonajes(r[0], r[1], r[2], r[3]);
        }

        ColocarPersonajes();
        PonerComplementosEnLaMesa();
        EsconderComplementosEnPrimeraPersona();
    }

    // Si la camara esta dentro de la cabeza de un personaje (tu silla), no se ven sus complementos
    // (si no, verias las gafas o el bigote por dentro).
    private void EsconderComplementosEnPrimeraPersona()
    {
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;

        if (cam == null)
            return;

        foreach (KeyValuePair<GameObject, List<Renderer>> par in rendersComplementos)
        {
            if (par.Key == null)
                continue;

            float alto = Mathf.Max(0.2f, CabezaDe(par.Key).y - CuerpoDe(par.Key).y);
            bool dentro = (cam.transform.position - CabezaDe(par.Key)).sqrMagnitude < alto * alto * 0.36f;

            foreach (Renderer r in par.Value)
                if (r != null && r.enabled == dentro)
                    r.enabled = !dentro;
        }
    }

    private void ColocarPersonajes()
    {
        if (modeloDePersonaje == null)
        {
            modeloDePersonaje = new GameObject[4];
            anclaPosicion = new Vector3[4];
            anclaRotacion = new Quaternion[4];
            giroCuerpo = new Quaternion[4];
            caderaLocal = new Vector3[4];

            for (int k = 0; k < 4; k++)
            {
                GameObject m = GameObject.Find(SitiosMesaMus.NombresModelos[k]);
                modeloDePersonaje[k] = m;

                if (m == null)
                    continue;

                Transform raiz = m.transform;
                giroCuerpo[k] = Quaternion.Inverse(raiz.rotation) * GiroCuerpoDe(m);
                caderaLocal[k] = Quaternion.Inverse(raiz.rotation) * (CaderaDe(m) - raiz.position);
            }

            // Cada silla es donde estaba sentado su modelo al empezar (su cadera y hacia donde miraba).
            for (int sitio = 0; sitio < 4; sitio++)
            {
                GameObject m = ModeloEnSitio(sitio);

                if (m == null)
                {
                    modeloDePersonaje = null;
                    return;
                }

                anclaPosicion[sitio] = CaderaDe(m);
                anclaRotacion[sitio] = GiroCuerpoDe(m);
            }
        }

        string clave = partida.PuestoLocal + "|" + ColoresJugadoresMus.Version;

        if (clave == claveColocacion)
            return;

        claveColocacion = clave;

        for (int sitio = 0; sitio < 4; sitio++)
        {
            int k = ColoresJugadoresMus.PersonajeDe(PuestoDeSitio(sitio));
            GameObject m = modeloDePersonaje[k];

            if (m == null)
                continue;

            // Se gira la raiz para que el cuerpo mire como miraba el de esa silla, y se mueve para que la cadera caiga en su sitio.
            Quaternion giroRaiz = anclaRotacion[sitio] * Quaternion.Inverse(giroCuerpo[k]);
            m.transform.SetPositionAndRotation(anclaPosicion[sitio] - giroRaiz * caderaLocal[k], giroRaiz);
            modelosPorSitio[sitio] = m;
            gestos[sitio] = null;
        }

        ReiniciarAbanicos();
        claveComplementos = null;
        necesitaColorear = true;
    }

    // ---------- Complementos ----------

    private void PonerComplementosEnLaMesa()
    {
        if (modeloDePersonaje == null)
            return;

        string clave = claveColocacion + "|" + partida.VersionSala + "|" + TaquillaMus.AspectoPropio + "|" + complementoProbador + "|" + taquillaAbierta;

        if (clave == claveComplementos)
            return;

        claveComplementos = clave;
        int mio = MiPuesto();

        for (int puesto = 0; puesto < 4; puesto++)
        {
            int k = ColoresJugadoresMus.PersonajeDe(puesto);
            int mascara = TaquillaMus.ComplementosDe(AspectoDePuesto(puesto), k);

            // En tu silla se ve siempre lo que llevas puesto ahora (y lo que te estas probando).
            if (puesto == mio)
            {
                mascara = TaquillaMus.ComplementosMios;

                if (taquillaAbierta && complementoProbador >= 0)
                {
                    HuecoComplemento hueco = TaquillaMus.Complementos[complementoProbador].Hueco;

                    for (int i = 0; i < TaquillaMus.Complementos.Length; i++)
                        if (TaquillaMus.Complementos[i].Hueco == hueco)
                            mascara &= ~(1 << i);

                    mascara |= 1 << complementoProbador;
                }
            }

            PonerComplementos(modeloDePersonaje[k], mascara);
        }

        necesitaColorear = true;
    }

    private void PonerComplementos(GameObject modelo, int mascara)
    {
        if (modelo == null)
            return;

        if (!plantillaCargada)
        {
            plantillaCargada = true;
            GameObject recurso = Resources.Load<GameObject>(rutaComplementos);

            if (recurso == null)
            {
                Debug.LogWarning("[PERSONAJES] No se encontro " + rutaComplementos + " en Resources: sin complementos.");
            }
            else
            {
                plantillaComplementos = Instantiate(recurso);
                plantillaComplementos.name = "PlantillaComplementos";
                plantillaComplementos.SetActive(false);
            }
        }

        // Quitar lo que llevaba.
        if (complementosPuestos.TryGetValue(modelo, out List<GameObject> antes))
            foreach (GameObject o in antes)
                if (o != null)
                    Destroy(o);

        if (piezasEscondidas.TryGetValue(modelo, out List<Renderer> escondidas))
            foreach (Renderer r in escondidas)
                if (r != null)
                    r.enabled = true;

        List<GameObject> puestos = new();
        List<Renderer> ocultas = new();
        List<Renderer> renders = new();
        complementosPuestos[modelo] = puestos;
        piezasEscondidas[modelo] = ocultas;
        rendersComplementos[modelo] = renders;

        if (plantillaComplementos == null || mascara == 0)
            return;

        Transform[] delModelo = modelo.GetComponentsInChildren<Transform>(true);
        Transform[] dePlantilla = plantillaComplementos.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < TaquillaMus.Complementos.Length; i++)
        {
            if ((mascara & (1 << i)) == 0)
                continue;

            ComplementoMus c = TaquillaMus.Complementos[i];
            string prefijo = "ACC_" + c.Nombre + "_";

            foreach (Transform pieza in dePlantilla)
            {
                if (!pieza.name.StartsWith(prefijo) || pieza.parent == null || !pieza.parent.name.StartsWith("ACC_CONTROL_"))
                    continue;

                // Se cuelga del mismo control del personaje (CONTROL_CABEZA o CONTROL_RAIZ).
                string control = "_" + pieza.parent.name.Substring(4);
                Transform destino = null;

                foreach (Transform t in delModelo)
                    if (t.name.EndsWith(control))
                    {
                        destino = t;
                        break;
                    }

                if (destino == null)
                    continue;

                GameObject copia = Instantiate(pieza.gameObject, destino, false);
                copia.name = pieza.name;
                copia.SetActive(true);
                puestos.Add(copia);
                renders.AddRange(copia.GetComponentsInChildren<Renderer>(true));
            }

            if (c.TapaCabeza)
                foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
                    if (r.enabled && (r.name.Contains("_Gorro_") || r.name.Contains("_Pelo_Casquete")))
                    {
                        r.enabled = false;
                        ocultas.Add(r);
                    }
        }
    }

    // ---------- Camara del probador ----------

    private bool camaraProbadorActiva;
    private Vector3 posAntesProbador, posCamProbador;
    private Quaternion rotAntesProbador, rotCamProbador;

    private GameObject MiModelo()
    {
        int puesto = MiPuesto();

        if (modeloDePersonaje == null)
            return null;

        int k = puesto >= 0 ? ColoresJugadoresMus.PersonajeDe(puesto) : TaquillaMus.PersonajeMio;
        return modeloDePersonaje[k];
    }

    private void CamaraProbador()
    {
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;
        GameObject modelo = MiModelo();

        if (cam == null || modelo == null)
            return;

        if (!camaraProbadorActiva)
        {
            camaraProbadorActiva = true;
            posAntesProbador = posCamProbador = cam.transform.position;
            rotAntesProbador = rotCamProbador = cam.transform.rotation;
        }

        if (controlCamaraJugador != null && controlCamaraJugador.enabled)
            controlCamaraJugador.enabled = false;

        Vector3 pie = CuerpoDe(modelo);
        Vector3 posCabeza = CabezaDe(modelo);
        float alto = Mathf.Max(0.3f, posCabeza.y - pie.y);
        Vector3 haciaCentro = CentroMesa() - pie;
        haciaCentro.y = 0f;
        haciaCentro = haciaCentro.sqrMagnitude > 0.0001f ? haciaCentro.normalized : Vector3.forward;
        Vector3 lado = Vector3.Cross(Vector3.up, haciaCentro);

        // La taquilla tapa la parte izquierda: se apunta un poco a un lado para que el personaje salga a la derecha.
        Vector3 mirar = posCabeza - Vector3.up * (alto * 0.35f) + lado * (alto * 0.45f);
        Vector3 destino = posCabeza + haciaCentro * (alto * distanciaProbador) + lado * (alto * 0.45f) + Vector3.up * (alto * 0.1f);

        float k = 1f - Mathf.Exp(-Time.deltaTime * 4f);
        posCamProbador = Vector3.Lerp(posCamProbador, destino, k);
        rotCamProbador = Quaternion.Slerp(rotCamProbador, Quaternion.LookRotation(mirar - destino, Vector3.up), k);
        cam.transform.SetPositionAndRotation(posCamProbador, rotCamProbador);
    }

    private void RestaurarCamaraProbador()
    {
        camaraProbadorActiva = false;
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;

        if (cam != null)
            cam.transform.SetPositionAndRotation(posAntesProbador, rotAntesProbador);

        if (controlCamaraJugador != null)
            controlCamaraJugador.enabled = true;
    }
}
