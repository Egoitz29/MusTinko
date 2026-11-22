using UnityEngine;

// Cartas 3D en las manos de los personajes: un abanico de 4 cartas que ensena
// el reverso a los demas. Cuando el personaje mira sus cartas, las sube y las pone
// mas rectas; cuando no, las baja hacia la mesa.
public partial class VistaMesaOnlineMus
{
    [Header("Cartas en la mano (reverso)")]
    [SerializeField] private bool mostrarCartasEnMano = true;
    [SerializeField] private string rutaReverso = "Baraja/Reverso_Clasico";
    [Tooltip("Usa el reverso que el jugador lleva puesto en la taquilla (si no, siempre rutaReverso).")]
    [SerializeField] private bool usarReversoDeLaTaquilla = true;
    private string reversoCargado;
    [SerializeField] private Vector2 tamanoCartaEnMano = new(0.11f, 0.17f);
    [Tooltip("Grados de apertura entre carta y carta del abanico.")]
    [SerializeField] private float aperturaAbanico = 11f;
    [Tooltip("Altura sobre la silla si no se encuentran las manos del modelo.")]
    [SerializeField] private float alturaCartasEnMano = 0.82f;
    [Tooltip("Cuanto se adelantan las cartas hacia la mesa desde el personaje.")]
    [SerializeField] private float adelanteCartasEnMano = 0.28f;
    [SerializeField] private float inclinacionCartasBajadas = 58f;
    [SerializeField] private float inclinacionCartasMirando = 18f;
    [Tooltip("Cuanto suben las cartas al mirarlas (en altos de carta: 0.2 = una quinta parte de la carta).")]
    [SerializeField] private float subidaCartasAlMirar = 0.2f;
    [Tooltip("Cuanto se acercan a la cara al mirarlas (en altos de carta).")]
    [SerializeField] private float acercarCartasAlMirar = 0.35f;

    [Header("Tamano segun el personaje")]
    [Tooltip("Alto de la carta comparado con la distancia entre las dos manos del personaje.")]
    [SerializeField] private float altoCartaManos = 0.38f;
    [Tooltip("Cuanto se adelantan las cartas por delante de las manos (respecto a la distancia entre manos).")]
    [SerializeField] private float adelanteRespectoManos = 0.3f;
    [Tooltip("Si el modelo no tiene manos, se multiplica el tamano por esto.")]
    [SerializeField] private float escalaSinManos = 2.5f;

    private sealed class AbanicoMano
    {
        public GameObject Raiz;
        public Transform ManoD, ManoI;
        public GameObject Modelo;
        public float Peso;
    }

    private readonly AbanicoMano[] abanicos = new AbanicoMano[4];
    private Material materialReverso, materialAnverso;
    private bool cartasEnManoPreparadas;

    private void PrepararCartasEnMano()
    {
        cartasEnManoPreparadas = true;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            Debug.LogWarning("Cartas en la mano: no se encontro el shader URP/Lit.");
            mostrarCartasEnMano = false;
            return;
        }

        if (usarReversoDeLaTaquilla)
            rutaReverso = TaquillaMus.RutaReverso;

        reversoCargado = rutaReverso;
        Texture2D reverso = Resources.Load<Texture2D>(rutaReverso);
        materialReverso = new Material(shader);
        materialReverso.color = Color.white;

        if (reverso != null)
        {
            materialReverso.mainTexture = reverso;

            if (materialReverso.HasProperty("_BaseMap"))
                materialReverso.SetTexture("_BaseMap", reverso);
        }
        else
        {
            Debug.LogWarning("Cartas en la mano: no se encontro la textura " + rutaReverso);
            materialReverso.color = new Color(0.55f, 0.12f, 0.12f);
        }

        // Recorte por transparencia para las esquinas redondeadas del reverso.
        if (materialReverso.HasProperty("_AlphaClip"))
        {
            materialReverso.SetFloat("_AlphaClip", 1f);
            materialReverso.SetFloat("_Cutoff", 0.5f);
            materialReverso.EnableKeyword("_ALPHATEST_ON");
        }

        if (materialReverso.HasProperty("_Smoothness"))
            materialReverso.SetFloat("_Smoothness", 0.35f);

        materialAnverso = new Material(shader);
        materialAnverso.color = new Color(0.96f, 0.92f, 0.84f);

        if (materialAnverso.HasProperty("_Smoothness"))
            materialAnverso.SetFloat("_Smoothness", 0.2f);
    }

    private AbanicoMano CrearAbanico(int sitio)
    {
        GameObject modelo = ModeloEnSitio(sitio);

        if (modelo == null)
            return null;

        AbanicoMano a = new() { Modelo = modelo, Raiz = new GameObject("CartasEnMano_" + modelo.name) };

        foreach (Transform t in modelo.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.EndsWith("_Manopla_D")) a.ManoD = t;
            else if (t.name.EndsWith("_Manopla_I")) a.ManoI = t;
        }

        for (int c = 0; c < 4; c++)
        {
            // Cada carta gira desde abajo (como cuando se abren en abanico).
            GameObject pivote = new("Carta_" + c);
            pivote.transform.SetParent(a.Raiz.transform, false);
            pivote.transform.localRotation = Quaternion.Euler(0f, 0f, (1.5f - c) * aperturaAbanico);
            pivote.transform.localPosition = new Vector3(0f, 0f, -0.002f * c);

            GameObject atras = CrearCara(pivote.transform, materialReverso, "Reverso");
            atras.transform.localPosition = new Vector3(0f, tamanoCartaEnMano.y * 0.42f, -0.0006f);
            atras.transform.localRotation = Quaternion.identity;

            GameObject delante = CrearCara(pivote.transform, materialAnverso, "Anverso");
            delante.transform.localPosition = new Vector3(0f, tamanoCartaEnMano.y * 0.42f, 0.0006f);
            delante.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        a.Raiz.SetActive(false);
        return a;
    }

    private GameObject CrearCara(Transform padre, Material material, string nombre)
    {
        GameObject cara = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cara.name = nombre;
        Collider col = cara.GetComponent<Collider>();

        if (col != null)
            Destroy(col);

        cara.transform.SetParent(padre, false);
        cara.transform.localScale = new Vector3(tamanoCartaEnMano.x, tamanoCartaEnMano.y, 1f);
        MeshRenderer r = cara.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return cara;
    }

    private void ActualizarCartasEnMano()
    {
        if (!mostrarCartasEnMano)
            return;

        if (!cartasEnManoPreparadas)
            PrepararCartasEnMano();

        if (!mostrarCartasEnMano)
            return;

        // Si ha cambiado el reverso en la taquilla, se cambia la textura.
        if (usarReversoDeLaTaquilla && TaquillaMus.RutaReverso != reversoCargado && materialReverso != null)
        {
            Texture2D nuevo = Resources.Load<Texture2D>(TaquillaMus.RutaReverso);
            reversoCargado = TaquillaMus.RutaReverso;

            if (nuevo != null)
            {
                materialReverso.mainTexture = nuevo;

                if (materialReverso.HasProperty("_BaseMap"))
                    materialReverso.SetTexture("_BaseMap", nuevo);
            }
        }

        bool hayMano = partida.IsSpawned && partida.Estado.EnCurso && !manosEnMesa && !enRevision;
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;
        Vector3 centro = CentroMesa();

        for (int sitio = 0; sitio < 4; sitio++)
        {
            if (abanicos[sitio] == null && hayMano)
                abanicos[sitio] = CrearAbanico(sitio);

            AbanicoMano a = abanicos[sitio];

            if (a == null)
                continue;

            // La silla desde la que miras no necesita cartas 3D (ya las ves abajo),
            // salvo si la camara se ha ido lejos (camara de cine de la demo).
            Vector3 cabeza = a.Modelo.transform.position + Vector3.up * alturaCabezas;
            bool camaraCerca = cam != null && (cam.transform.position - cabeza).sqrMagnitude < 0.7f * 0.7f;
            bool visible = hayMano && !(sitio == 0 && camaraCerca);

            if (a.Raiz.activeSelf != visible)
                a.Raiz.SetActive(visible);

            if (!visible)
                continue;

            int puesto = PuestoDeSitio(sitio);
            GestosMus g = puesto >= 0 ? GestosDePuesto(puesto) : null;
            bool mirando = g != null && g.MirandoCartas;
            a.Peso = Mathf.MoveTowards(a.Peso, mirando ? 1f : 0f, Time.deltaTime * 4f);
            float w = Mathf.SmoothStep(0f, 1f, a.Peso);

            Vector3 haciaCentro = centro - a.Modelo.transform.position;
            haciaCentro.y = 0f;

            if (haciaCentro.sqrMagnitude < 0.0001f)
                haciaCentro = Vector3.forward;

            haciaCentro.Normalize();

            Vector3 ancla;
            float escala;

            if (a.ManoD != null && a.ManoI != null)
            {
                // Las cartas se miden con las manos: asi encajan con el tamano de cada modelo.
                float entreManos = Mathf.Max(0.05f, Vector3.Distance(a.ManoD.position, a.ManoI.position));
                escala = entreManos * altoCartaManos / tamanoCartaEnMano.y;
                ancla = (a.ManoD.position + a.ManoI.position) * 0.5f + haciaCentro * (entreManos * adelanteRespectoManos);
            }
            else
            {
                escala = escalaSinManos;
                ancla = a.Modelo.transform.position + Vector3.up * alturaCartasEnMano + haciaCentro * adelanteCartasEnMano;
            }

            // Al mirarlas suben un poco y se acercan a la cara, sin irse por encima de la cabeza.
            float altoCarta = tamanoCartaEnMano.y * escala;
            ancla += Vector3.up * (subidaCartasAlMirar * altoCarta * w) - haciaCentro * (acercarCartasAlMirar * altoCarta * w);
            a.Raiz.transform.localScale = Vector3.one * escala;

            // El reverso mira hacia la mesa; inclinado hacia atras para que se vea desde las otras sillas.
            float inclinacion = Mathf.Lerp(inclinacionCartasBajadas, inclinacionCartasMirando, w);
            Quaternion mirarMesa = Quaternion.LookRotation(-haciaCentro, Vector3.up);
            Quaternion rot = mirarMesa * Quaternion.Euler(inclinacion, 0f, 0f);

            a.Raiz.transform.SetPositionAndRotation(ancla, rot);
        }
    }

    private int PuestoDeSitio(int sitio)
    {
        int local = partida.PuestoLocal;
        return local < 0 ? sitio : (local + sitio) % 4;
    }
}
