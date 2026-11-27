using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Cada jugador se viste a su gusto (colores, traje y complementos): el modelo 3D de cada silla se colorea
// con lo que lleva quien se sienta ahi, y se le ponen sus complementos (bigote, gafas, sombrero...).
// Ademas, con la taquilla abierta la camara se pone delante de tu personaje (probador).
public partial class VistaMesaOnlineMus
{
    [Header("Personajes y complementos")]
    [SerializeField] private string rutaComplementos = "Modelos/Complementos_Tinko";
    [Tooltip("Si esta marcado, cada jugador lleva su personaje a su silla (los modelos se cambian de sitio).")]
    [SerializeField] private bool cambiarPersonajesDeSilla = true;
    [Header("Tamano de los personajes y sitio de la camara")]
    [Tooltip("Tamano de los personajes (1 = el de los de los lados en tu escena). Mas pequeno = la mesa se ve mas.")]
    [UnityEngine.Range(0.6f, 1.2f)][SerializeField] private float escalaPersonajes = 0.88f;
    [Tooltip("Cuanto se echa hacia atras la camara de tu silla (metros). 0 = donde la pusiste en la escena.")]
    [SerializeField] private float alejarCamara = 0.35f;
    [Tooltip("Cuanto sube la camara de tu silla (metros).")]
    [SerializeField] private float subirCamara = 0.1f;
    [Tooltip("Distancia de la camara del probador (en alturas de cabeza).")]
    [SerializeField] private float distanciaProbador = 1.25f;

    private GameObject[] modeloDePersonaje;
    private Vector3[] anclaPosicion;      // cadera del que estaba sentado en cada silla al empezar
    private Vector3[] anclaMirada;        // desde esa cadera, hacia donde esta la mesa (en horizontal)
    private Quaternion[] giroOriginal;    // giro de la raiz de cada modelo al empezar
    private Vector3[] miradaLocal;        // hacia donde mira cada modelo (hacia la mesa), respecto a su raiz
    private Vector3[] caderaLocal;        // cadera respecto a la raiz de cada modelo
    private float alturaObjetivo;         // todos del mismo tamano (ahora se ven desde todos los lados)
    private float[] alturaOriginal;       // tamano de cada modelo al empezar
    private Vector3[] escalaOriginal;     // escala de la raiz de cada modelo al empezar
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

    private static Vector3 Plano(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.000001f ? v.normalized : Vector3.forward;
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
            ColoresJugadoresMus.PonerPersonajes(0, 1, 2, 3);
            ColoresJugadoresMus.PonerAspectos(TaquillaMus.AspectoPropio, 0, 0, 0);
        }

        ColocarPersonajes();
        ColocarCamaraSiCambiaDeSilla();
        PonerComplementosEnLaMesa();
        EsconderMiSilla();
    }

    // ---------- Sillas fijas: la camara va a la tuya ----------

    // Las sillas son sitios fijos de la mesa: la 0 abajo (donde estaba la camara), 1 derecha, 2 enfrente, 3 izquierda.
    // Cada jugador se sienta en la suya y la camara se pone detras de TU silla.
    private int SillaCamara
    {
        get
        {
            int p = MiPuesto();
            return p >= 0 ? p : 0;
        }
    }

    // Vista en primera persona (desde tu silla): tu personaje y tu silla no se ven.
    private bool PrimeraPersona => !taquillaAbierta && !enRevision && !(DemoActiva && !vistaJugadorDemo);

    private readonly GameObject[] sillas = new GameObject[4];
    private Transform manoJugador;          // tus cartas en 3D (ManoJugador3D): van siempre delante de tu camara
    private Vector3 manoRelPos;
    private Quaternion manoRelRot;
    private Vector3 camaraPosSilla0;
    private Quaternion camaraGiroSilla0;
    private int sillaCamaraColocada = -1;
    private GameObject modeloOculto, sillaOculta;
    private readonly List<Renderer> rendersOcultos = new();

    private void PrepararSillas(Vector3 centro)
    {
        // Las tres sillas de tu escena se asignan al sitio que tienen mas cerca.
        foreach (string nombre in new[] { "Silla_AsientoDerecha", "Silla_AsientoFrente", "Silla_AsientoIzquierda" })
        {
            GameObject silla = GameObject.Find(nombre);

            if (silla == null)
                continue;

            int mejor = -1;
            float mejorDistancia = float.MaxValue;

            for (int s = 1; s < 4; s++)
            {
                float d = (Plano3(silla.transform.position) - Plano3(anclaPosicion[s])).sqrMagnitude;

                if (d < mejorDistancia)
                {
                    mejorDistancia = d;
                    mejor = s;
                }
            }

            if (mejor >= 0)
                sillas[mejor] = silla;
        }

        // Tu sitio no tenia silla (eras la camara): se pone una igual que la de enfrente, al otro lado de la mesa.
        GameObject modelo = sillas[2];

        // La caja que hacia de asiento en tu sitio se esconde: ahora hay una silla de verdad.
        GameObject caja = GameObject.Find("AsientoJugador");

        if (caja != null && modelo != null)
            foreach (Renderer r in caja.GetComponentsInChildren<Renderer>())
                r.enabled = false;

        if (sillas[0] == null && modelo != null)
        {
            Vector3 p = modelo.transform.position;
            Vector3 espejo = new(2f * centro.x - p.x, p.y, 2f * centro.z - p.z);
            GameObject copia = Instantiate(modelo, modelo.transform.parent);
            copia.name = "Silla_AsientoAbajo";
            copia.transform.SetPositionAndRotation(espejo, Quaternion.AngleAxis(180f, Vector3.up) * modelo.transform.rotation);
            sillas[0] = copia;
        }
    }

    private static Vector3 Plano3(Vector3 v) => new(v.x, 0f, v.z);

    private void ColocarCamaraSiCambiaDeSilla()
    {
        if (modeloDePersonaje == null || anclaMirada == null)
            return;

        int silla = SillaCamara;

        if (silla == sillaCamaraColocada)
            return;

        sillaCamaraColocada = silla;
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;

        if (cam == null)
            return;

        // La misma vista de siempre, girada alrededor de la mesa hasta quedar detras de tu silla.
        float angulo = Vector3.SignedAngle(anclaMirada[0], anclaMirada[silla], Vector3.up);
        Quaternion giro = Quaternion.AngleAxis(angulo, Vector3.up);
        Vector3 centro = CentroMesa();
        Vector3 pos = centro + giro * (camaraPosSilla0 - centro);
        Quaternion rot = giro * camaraGiroSilla0;

        // Tus cartas 3D van delante de la camara de tu silla.
        if (manoJugador != null)
            manoJugador.SetPositionAndRotation(pos + rot * manoRelPos, rot * manoRelRot);

        // Si ahora mismo manda otra camara (revision, probador, demo), se cambia el sitio al que vuelve.
        if (camaraMovida)
        {
            posicionCamaraGuardada = pos;
            rotacionCamaraGuardada = rot;
        }
        else if (camaraProbadorActiva)
        {
            posAntesProbador = pos;
            rotAntesProbador = rot;
        }
        else if (DemoActiva && camaraDemoIniciada)
        {
            posCamOriginal = pos;
            rotCamOriginal = rot;
        }
        else
        {
            cam.transform.SetPositionAndRotation(pos, rot);
        }

        // La mirada libre y la camara de 4 vistas parten de la nueva silla.
        if (!miradaIniciada)
            fovBase = cam.fieldOfView;
        else
            cam.fieldOfView = fovBase;

        Vector3 e = rot.eulerAngles;
        yawBase = yawActual = yawObjetivo = e.y;
        pitchBase = pitchActual = pitchObjetivo = Mathf.DeltaAngle(0f, e.x);
        miradaIniciada = true;

        if (controlCamaraJugador != null)
            controlCamaraJugador.PonerVistaBase(rot);
    }

    // En primera persona se esconden tu personaje y tu silla (si no, los tendrias delante de la camara).
    private void EsconderMiSilla()
    {
        bool ocultar = PrimeraPersona && modelosPorSitio != null;
        GameObject modelo = ocultar ? ModeloEnSitio(SillaCamara) : null;
        GameObject silla = ocultar ? sillas[SillaCamara] : null;

        if (modelo != modeloOculto || silla != sillaOculta)
        {
            // Se vuelve a ensenar lo que se habia escondido (menos el gorro que tapa un sombrero).
            List<Renderer> tapados = modeloOculto != null && piezasEscondidas.TryGetValue(modeloOculto, out List<Renderer> l) ? l : null;

            foreach (Renderer r in rendersOcultos)
                if (r != null && (tapados == null || !tapados.Contains(r)))
                    r.enabled = true;

            rendersOcultos.Clear();
            modeloOculto = modelo;
            sillaOculta = silla;
        }

        // Cada frame, por si han aparecido piezas nuevas (complementos, cartas...).
        Ocultar(modeloOculto);
        Ocultar(sillaOculta);
    }

    private void Ocultar(GameObject objeto)
    {
        if (objeto == null)
            return;

        foreach (Renderer r in objeto.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled)
                continue;

            r.enabled = false;
            rendersOcultos.Add(r);
        }
    }

    private bool sillasPreparadas = true;

    private void ColocarPersonajes()
    {
        if (!sillasPreparadas)
            return;

        // Se espera a que las animaciones ya hayan colocado a todos (la pose del primer frame no vale).
        if (modeloDePersonaje == null && Time.frameCount < 5)
            return;

        if (modeloDePersonaje == null)
        {
            modeloDePersonaje = new GameObject[4];
            anclaPosicion = new Vector3[4];
            anclaMirada = new Vector3[4];
            giroOriginal = new Quaternion[4];
            miradaLocal = new Vector3[4];
            caderaLocal = new Vector3[4];
            alturaOriginal = new float[4];
            escalaOriginal = new Vector3[4];

            // Centro de la mesa (tu silla esta mas atras que las demas, asi que no vale la media de las sillas).
            Vector3 centro = CentroMesa();

            for (int sitio = 0; sitio < 4; sitio++)
            {
                if (ModeloEnSitio(sitio) == null)
                {
                    modeloDePersonaje = null;
                    sillasPreparadas = false;
                    Debug.LogWarning("[PERSONAJES] Falta algun modelo en la mesa: no se cambian de silla.");
                    return;
                }
            }

            // Sillas 1, 2 y 3: donde estaba la cadera de su modelo y hacia donde queda la mesa desde ahi.
            float sumaAlturas = 0f;

            for (int sitio = 1; sitio < 4; sitio++)
            {
                GameObject ocupante = ModeloEnSitio(sitio);
                anclaPosicion[sitio] = CaderaDe(ocupante);
                anclaMirada[sitio] = Plano(centro - anclaPosicion[sitio]);

                if (sitio != 2)
                    sumaAlturas += Vector3.Distance(CuerpoDe(ocupante), CabezaDe(ocupante));
            }

            // Silla 0 (la tuya): en tu escena el modelo estaba escondido detras de la camara;
            // se pone como la de enfrente, al otro lado de la mesa.
            Vector3 frente = anclaPosicion[2];
            anclaPosicion[0] = new Vector3(2f * centro.x - frente.x, frente.y, 2f * centro.z - frente.z);
            anclaMirada[0] = Plano(centro - anclaPosicion[0]);

            // Todos del tamano de los de los lados (MENTA estaba mas pequeno y ahora se ve desde todas partes).
            alturaObjetivo = sumaAlturas / 2f * escalaPersonajes;

            // Las caderas, a la altura de las de los lados (son sillas iguales y todos tienen ya el mismo tamano).
            float alturaCadera = (anclaPosicion[1].y + anclaPosicion[3].y) / 2f;
            anclaPosicion[0].y = alturaCadera;
            anclaPosicion[2].y = alturaCadera;

            PrepararSillas(centro);

            // La camara de tu escena es la de la silla 0; para las demas se gira alrededor de la mesa.
            Camera cam = camaraJugador != null ? camaraJugador : Camera.main;

            if (cam != null)
            {
                Vector3 camaraEscena = camaraMovida ? posicionCamaraGuardada : cam.transform.position;
                camaraGiroSilla0 = miradaIniciada ? Quaternion.Euler(pitchBase, yawBase, 0f)
                    : camaraMovida ? rotacionCamaraGuardada : cam.transform.rotation;

                // Tus cartas 3D (las que ves al mirar abajo): se guarda donde estan respecto a la camara de tu escena,
                // para llevarlas siempre delante de tu camara (en cualquier silla).
                GameObject mano = GameObject.Find("ManoJugador3D");

                if (mano != null)
                {
                    manoJugador = mano.transform;
                    Quaternion inversa = Quaternion.Inverse(camaraGiroSilla0);
                    manoRelPos = inversa * (manoJugador.position - camaraEscena);
                    manoRelRot = inversa * manoJugador.rotation;
                }

                // Un poco mas atras y arriba: se ve mejor la mesa y los personajes no llenan la pantalla.
                camaraPosSilla0 = camaraEscena + Plano(camaraEscena - centro) * alejarCamara + Vector3.up * subirCamara;
            }

            // Cada modelo: hacia donde mira (en tu escena todos miran a la mesa) y su cadera, respecto a su raiz.
            for (int k = 0; k < 4; k++)
            {
                GameObject m = GameObject.Find(SitiosMesaMus.NombresModelos[k]);
                modeloDePersonaje[k] = m;

                if (m == null)
                    continue;

                Transform raiz = m.transform;
                giroOriginal[k] = raiz.rotation;
                escalaOriginal[k] = raiz.localScale;
                alturaOriginal[k] = Mathf.Max(0.01f, Vector3.Distance(CuerpoDe(m), CabezaDe(m)));
                miradaLocal[k] = Quaternion.Inverse(raiz.rotation) * Plano(centro - CaderaDe(m));
                caderaLocal[k] = Quaternion.Inverse(raiz.rotation) * (CaderaDe(m) - raiz.position);
            }
        }

        // Si esta desmarcado, los modelos se quedan como en tu escena (la camara si va a tu silla).
        if (!cambiarPersonajesDeSilla)
            return;

        string clave = ColoresJugadoresMus.Version.ToString();

        if (clave == claveColocacion)
            return;

        claveColocacion = clave;

        for (int sitio = 0; sitio < 4; sitio++)
        {
            int k = ColoresJugadoresMus.PersonajeDe(sitio);
            GameObject m = modeloDePersonaje[k];

            if (m == null)
                continue;

            // Se gira solo en horizontal para que vuelva a mirar a la mesa desde esta silla,
            // y se mueve para que su cadera quede donde estaba la del que se sentaba aqui.
            Vector3 mirabaAntes = giroOriginal[k] * miradaLocal[k];
            float angulo = Vector3.SignedAngle(mirabaAntes, anclaMirada[sitio], Vector3.up);
            Quaternion giroRaiz = Quaternion.AngleAxis(angulo, Vector3.up) * giroOriginal[k];

            // Todos del mismo tamano.
            float factor = alturaObjetivo > 0.01f ? alturaObjetivo / alturaOriginal[k] : 1f;
            m.transform.localScale = escalaOriginal[k] * factor;
            m.transform.SetPositionAndRotation(anclaPosicion[sitio] - giroRaiz * (caderaLocal[k] * factor), giroRaiz);
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
            int mascara = TaquillaMus.ComplementosDe(AspectoDePuesto(puesto), puesto);

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
                AjustarComplemento(c.Nombre, copia.transform);
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

    // Con las cabezas tan grandes, la pajarita y la bufanda quedaban debajo de la barbilla y dentro del poncho
    // (no se veian), y el pendiente era diminuto. Se agrandan y se sacan hacia fuera.
    // Ejes de las piezas (como en Blender): -Y hacia delante, +Z hacia arriba (respecto al CONTROL_RAIZ).
    private static void AjustarComplemento(string nombre, Transform pieza)
    {
        switch (nombre)
        {
            case "Pajarita":
                Escalar(pieza, new Vector3(0f, -0.43f, 1.37f), 1.9f, new Vector3(0f, -0.14f, -0.12f));
                break;
            case "Bufanda":
                Escalar(pieza, new Vector3(0f, -0.07f, 1.27f), 1.22f, new Vector3(0f, -0.04f, -0.02f));
                pieza.localScale = Vector3.Scale(pieza.localScale, new Vector3(1f, 1f, 1.5f));
                break;
            case "Pendiente":
                // Solo cuelga de un lado: se agranda sobre si mismo y baja un poco de la oreja.
                pieza.localScale *= 2.4f;
                pieza.localPosition += new Vector3(0f, 0f, -0.08f);
                break;
        }
    }

    private static void Escalar(Transform pieza, Vector3 centro, float factor, Vector3 mover)
    {
        // El centro solo se usa en Y y Z (en X todas estas piezas son simetricas).
        Vector3 p = pieza.localPosition;
        Vector3 c = new(p.x, centro.y, centro.z);
        pieza.localPosition = c + (p - c) * factor + new Vector3(p.x * (factor - 1f), 0f, 0f) + mover;
        pieza.localScale *= factor;
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

        int k = ColoresJugadoresMus.PersonajeDe(Mathf.Max(0, puesto));
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
