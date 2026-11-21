using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// MODO DEMO: cuatro bots juegan solos, la camara se mueve sola y salen rotulos
// que explican cada jugada. Pensado para grabar videos y ensenar a jugar.
//   F8  -> empezar / salir de la demo
//   H   -> ocultar o mostrar los paneles (para un video mas limpio)
//   C   -> ocultar o mostrar las cartas de todos
//   V   -> cambiar entre vista de jugador (la de siempre) y camara de cine
public partial class VistaMesaOnlineMus
{
    [Header("Modo demo (videos)")]
    [Tooltip("Cuanto mas despacio va la mesa en la demo (1 = normal).")]
    [SerializeField] private float ritmoVisualDemo = 1.9f;
    [SerializeField] private float pausaSenaDemo = 3f;
    [SerializeField] private float segundosTablaDemo = 7f;
    [SerializeField] private float segundosCartasDemo = 5f;
    [SerializeField] private float segundosFinPartidaDemo = 12f;

    [Header("Camara de la demo")]
    [Tooltip("Si esta marcado, la demo se ve como la ve un jugador sentado (silla de LILA). Tecla V para cambiar.")]
    [SerializeField] private bool vistaJugadorDemo = true;
    [SerializeField] private float inclinacionCartasDemo = 50f;
    [SerializeField] private float segundosMirarCartasDemo = 2.5f;
    [SerializeField] private float radioPlanoGeneral = 2.3f;
    [SerializeField] private float alturaPlanoGeneral = 1.35f;
    [SerializeField] private float velocidadGiroGeneral = 5f;
    [SerializeField] private float distanciaPlanoJugador = 1.25f;
    [SerializeField] private float distanciaPlanoSena = 0.75f;
    [SerializeField] private float suavidadCamara = 2.2f;
    [SerializeField] private float segundosPlanoJugador = 2.4f;

    private enum PlanoDemo
    {
        General,
        Jugador,
        Sena,
        Cartas
    }

    private bool demoPendiente;
    private bool uiLimpiaDemo;
    private bool cartasDemoVisibles = true;

    private PlanoDemo planoDemo;
    private int puestoPlanoDemo;
    private float finPlanoDemo;
    private float anguloGeneralDemo;
    private bool camaraDemoIniciada;
    private Vector3 posCamDemo, posCamOriginal;
    private Quaternion rotCamDemo, rotCamOriginal;

    private float inicioRevisionDemo = -1f;
    private float inicioFinPartidaDemo = -1f;

    private readonly HashSet<string> explicadoDemo = new();
    private FaseMus faseRotuladaDemo = FaseMus.FinPartida;

    private struct Rotulo
    {
        public string Titulo;
        public string Detalle;
    }

    private readonly Queue<Rotulo> rotulosDemo = new();
    private float finRotuloDemo;

    private GameObject cajaRotuloDemo, panelCartasDemo, botonSalirDemo;
    private TextMeshProUGUI txtRotuloTitulo, txtRotuloDetalle;
    private readonly TextMeshProUGUI[] nombresCartasDemo = new TextMeshProUGUI[4];
    private readonly Image[] imagenesCartasDemo = new Image[16];
    private readonly int[] cartasDemoMostradas = { -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2 };

    private bool DemoActiva => partida != null && partida.ModoDemo && partida.IsSpawned;

    // ---------- Entrar y salir ----------

    private void EmpezarDemoDesdeMenu()
    {
        GuardarNombre(campoNombreUI != null ? campoNombreUI.text : "");
        demoPendiente = true;

        NetworkManager red = NetworkManager.Singleton;

        if (red == null || !red.IsListening)
            conexion?.CrearPartida();
    }

    private void SalirDeLaDemo()
    {
        demoPendiente = false;

        if (conexion != null)
            conexion.Salir();
        else if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();
    }

    // ---------- Cada frame ----------

    private void ActualizarDemo()
    {
        Keyboard teclado = Keyboard.current;

        if (teclado != null && teclado.f8Key.wasPressedThisFrame)
        {
            if (DemoActiva)
                SalirDeLaDemo();
            else
                EmpezarDemoDesdeMenu();
        }

        if (demoPendiente && partida.IsSpawned && partida.IsServer)
        {
            demoPendiente = false;
            EmpezarDemoNueva();
        }

        if (!DemoActiva)
        {
            if (camaraDemoIniciada)
                RestaurarCamaraDemo();

            rotulosDemo.Clear();
            return;
        }

        if (teclado != null && teclado.hKey.wasPressedThisFrame)
            uiLimpiaDemo = !uiLimpiaDemo;

        if (teclado != null && teclado.cKey.wasPressedThisFrame)
            cartasDemoVisibles = !cartasDemoVisibles;

        if (teclado != null && teclado.vKey.wasPressedThisFrame)
            vistaJugadorDemo = !vistaJugadorDemo;

        if (teclado != null && teclado.escapeKey.wasPressedThisFrame)
            SalirDeLaDemo();

        AvanzarFinDeManoDemo();
    }

    private void EmpezarDemoNueva()
    {
        explicadoDemo.Clear();
        rotulosDemo.Clear();
        faseRotuladaDemo = FaseMus.FinPartida;
        inicioRevisionDemo = -1f;
        inicioFinPartidaDemo = -1f;
        ManoNuevaComoEspectador();
        partida.EmpezarDemoServidor();
        PonerRotulo("MODO DEMO: MUS TINKO",
            "Juegan 4 bots. Lo ves desde la silla de LILA: sus cartas son las de abajo. LILA va con MENTA (enfrente) contra MOSTAZA y CORAL.");
    }

    private void AvanzarFinDeManoDemo()
    {
        EstadoPublicoMus e = partida.Estado;

        if (!e.EnCurso && partida.PartidaEmpezada)
        {
            if (inicioFinPartidaDemo < 0f)
            {
                inicioFinPartidaDemo = Time.time;
                PonerRotulo("FIN DE LA PARTIDA",
                    "Gana la pareja " + (e.ParejaGanadora == 0 ? "LILA y MENTA" : "MOSTAZA y CORAL") + ". En unos segundos empieza otra.");
            }

            if (Time.time - inicioFinPartidaDemo > segundosFinPartidaDemo && partida.IsServer)
                EmpezarDemoNueva();

            return;
        }

        inicioFinPartidaDemo = -1f;

        if (!enRevision)
        {
            inicioRevisionDemo = -1f;
            return;
        }

        if (inicioRevisionDemo < 0f)
            inicioRevisionDemo = Time.time;

        float t = Time.time - inicioRevisionDemo;

        if (tablaVisible && t > segundosTablaDemo)
        {
            tablaVisible = false;
            PonerRotulo("LAS CARTAS DE TODOS", "Al final de la mano se ensenan las cartas para comprobar quien gana cada lance.");
        }

        if (t > segundosTablaDemo + segundosCartasDemo && partida.PuedePasarDeMano && cola.Count == 0)
        {
            partida.PedirSiguienteMano();
            ManoNuevaComoEspectador();
        }
    }

    // Lo mismo que hace ProcesarMano al empezar mano, pero sin cartas propias.
    private void ManoNuevaComoEspectador()
    {
        if (revelador != null)
            revelador.OcultarManos();

        for (int sitio = 0; sitio < 4; sitio++)
            reparto.EstablecerManoExterna(sitio, new[] { "", "", "", "" });

        manosEnMesa = false;
        firmaManoSillaDemo = null;
        OcultarBocadillos();

        for (int i = 0; i < resultadosLance.Length; i++)
            resultadosLance[i] = null;

        declaracionesJuego = 0;
        algunoTieneJuego = false;
        modoUltimoLance = null;
        senasHechas.Clear();
        faseRotuladaDemo = FaseMus.FinPartida;

        if (enRevision)
            ActivarVistaJugador(false);
    }

    // ---------- Camara automatica ----------

    private void LateUpdate()
    {
        if (DemoActiva)
            DirigirCamaraDemo();
    }

    private void EnfocarDemo(PlanoDemo plano, int puesto, float segundos)
    {
        if (!DemoActiva || puesto < 0 || puesto > 3)
            return;

        // Un primer plano de una sena no lo interrumpe una jugada normal.
        if (planoDemo == PlanoDemo.Sena && plano == PlanoDemo.Jugador && Time.time < finPlanoDemo)
            return;

        planoDemo = plano;
        puestoPlanoDemo = puesto;
        finPlanoDemo = Time.time + segundos;
    }

    private Vector3 CabezaModeloDemo(int puesto)
    {
        GameObject modelo = ModeloEnSitio(Relativo(puesto));
        return modelo != null ? modelo.transform.position + Vector3.up * alturaCabezas : CentroMesa();
    }

    private void DirigirCamaraDemo()
    {
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;

        if (cam == null)
            return;

        if (!camaraDemoIniciada)
        {
            ModeloEnSitio(0);
            Transform t0 = cam.transform;
            posCamOriginal = camaraMovida ? posicionCamaraGuardada : t0.position;
            rotCamOriginal = camaraMovida ? rotacionCamaraGuardada : t0.rotation;
            posCamDemo = t0.position;
            rotCamDemo = t0.rotation;
            anguloGeneralDemo = 0f;
            camaraDemoIniciada = true;
        }

        if (controlCamaraJugador != null && controlCamaraJugador.enabled)
            controlCamaraJugador.enabled = false;

        if (Time.time > finPlanoDemo)
            planoDemo = PlanoDemo.General;

        if (vistaJugadorDemo)
        {
            CamaraDeJugadorDemo(cam);
            return;
        }

        Vector3 centro = CentroMesa();
        anguloGeneralDemo += Time.deltaTime * velocidadGiroGeneral;
        Quaternion giro = Quaternion.Euler(0f, anguloGeneralDemo, 0f);
        Vector3 destino;
        Vector3 mirar;

        if (enRevision)
        {
            destino = centro + giro * new Vector3(0f, 2f, -0.9f);
            mirar = centro;
        }
        else if (planoDemo == PlanoDemo.General)
        {
            destino = centro + giro * new Vector3(0f, alturaPlanoGeneral, -radioPlanoGeneral);
            mirar = centro + Vector3.up * 0.15f;
        }
        else
        {
            bool sena = planoDemo == PlanoDemo.Sena;
            Vector3 cabeza = CabezaModeloDemo(puestoPlanoDemo);
            Vector3 haciaCentro = centro - cabeza;
            haciaCentro.y = 0f;

            if (haciaCentro.sqrMagnitude < 0.0001f)
                haciaCentro = Vector3.forward;

            haciaCentro.Normalize();
            Vector3 lado = Vector3.Cross(Vector3.up, haciaCentro);
            destino = cabeza + haciaCentro * (sena ? distanciaPlanoSena : distanciaPlanoJugador)
                      + Vector3.up * (sena ? 0.05f : 0.2f) + lado * (sena ? 0.12f : 0.35f);
            mirar = cabeza - Vector3.up * (sena ? 0f : 0.1f);
        }

        float k = 1f - Mathf.Exp(-Time.deltaTime * suavidadCamara);
        posCamDemo = Vector3.Lerp(posCamDemo, destino, k);
        Vector3 direccion = mirar - posCamDemo;

        if (direccion.sqrMagnitude > 0.0001f)
            rotCamDemo = Quaternion.Slerp(rotCamDemo, Quaternion.LookRotation(direccion, Vector3.up), k);

        cam.transform.SetPositionAndRotation(posCamDemo, rotCamDemo);
    }

    // Como un jugador sentado: no se mueve de la silla, solo gira la cabeza
    // hacia quien habla y de vez en cuando mira sus cartas.
    private void CamaraDeJugadorDemo(Camera cam)
    {
        if (enRevision)
        {
            // Al final de la mano manda la vista de la mesa de siempre.
            posCamDemo = cam.transform.position;
            rotCamDemo = cam.transform.rotation;
            return;
        }

        Vector3 destino = posCamOriginal;
        Quaternion objetivo = rotCamOriginal;

        // Con las senas por mirada, la camara mira justo donde mira LILA (lo que ella ve).
        int miradaSilla = partida.SenasConMirada && partida.Estado.EnCurso ? partida.MiradaDe(0) : -2;

        if (miradaSilla == PartidaMus.MiraCartas)
        {
            objetivo = rotCamOriginal * Quaternion.Euler(inclinacionCartasDemo, 0f, 0f);
        }
        else if (miradaSilla >= 0 && Relativo(miradaSilla) != 0)
        {
            Vector3 direccion = CabezaModeloDemo(miradaSilla) - destino;

            if (direccion.sqrMagnitude > 0.0001f)
                objetivo = Quaternion.LookRotation(direccion, Vector3.up);
        }
        else if (planoDemo == PlanoDemo.Cartas)
        {
            objetivo = rotCamOriginal * Quaternion.Euler(inclinacionCartasDemo, 0f, 0f);
        }
        else if (planoDemo != PlanoDemo.General && Relativo(puestoPlanoDemo) != 0)
        {
            Vector3 cabeza = CabezaModeloDemo(puestoPlanoDemo);
            Vector3 direccion = cabeza - destino;

            if (direccion.sqrMagnitude > 0.0001f)
                objetivo = Quaternion.LookRotation(direccion, Vector3.up);
        }

        float k = 1f - Mathf.Exp(-Time.deltaTime * suavidadCamara * 1.4f);
        posCamDemo = Vector3.Lerp(posCamDemo, destino, k);
        rotCamDemo = Quaternion.Slerp(rotCamDemo, objetivo, k);
        cam.transform.SetPositionAndRotation(posCamDemo, rotCamDemo);
    }

    private void RotuloDemoMiraCartas(int puesto)
    {
        if (!DemoActiva || !partida.SenasConMirada)
            return;

        string detalle = Explicar("miracartas",
            "Mientras mira sus cartas no ve a nadie: es un buen momento para que los otros pasen senas sin que les pillen.");

        // Solo se avisa con rotulo la primera vez, para no llenar la pantalla.
        if (detalle.Length > 0)
            PonerRotulo(N(puesto) + " esta mirando sus cartas", detalle);
    }

    private void RestaurarCamaraDemo()
    {
        camaraDemoIniciada = false;
        Camera cam = camaraJugador != null ? camaraJugador : Camera.main;

        if (cam != null)
            cam.transform.SetPositionAndRotation(posCamOriginal, rotCamOriginal);

        camaraMovida = false;

        if (controlCamaraJugador != null)
            controlCamaraJugador.enabled = true;
    }

    // ---------- Rotulos ----------

    private void PonerRotulo(string titulo, string detalle)
    {
        if (!DemoActiva && !demoPendiente)
            return;

        // Si se acumulan muchos, se quitan los mas viejos para no ir con retraso.
        while (rotulosDemo.Count > 2)
            rotulosDemo.Dequeue();

        rotulosDemo.Enqueue(new Rotulo { Titulo = titulo, Detalle = detalle });
    }

    // La primera vez de cada cosa se explica con detalle; despues, solo el titulo.
    private string Explicar(string clave, string detalle)
    {
        return explicadoDemo.Add(clave) ? detalle : "";
    }

    private static string N(int puesto) => ColoresJugadoresMus.Nombre(puesto);

    private void RotuloDemoEvento(EventoMus e)
    {
        if (!DemoActiva)
            return;

        if (e.Tipo == TipoEventoMus.FinMano)
        {
            planoDemo = PlanoDemo.General;
            PonerRotulo("FIN DE LA MANO", Explicar("finmano",
                "Ahora se cuentan los tantos de cada lance. La tabla explica de donde sale cada punto."));
            return;
        }

        if (e.Tipo == TipoEventoMus.LanceCerrado)
        {
            planoDemo = PlanoDemo.General;
            PonerRotulo(NombreLanceDemo(e.Fase) + ": " + TextoCierre(e), Explicar("cierre",
                "Si el envite se ha querido, quien gana se sabe al final, al ensenar las cartas."));
            return;
        }

        if (PartidaMus.EsLance(e.Fase) && e.Fase != faseRotuladaDemo)
        {
            faseRotuladaDemo = e.Fase;
            PonerRotulo("EMPIEZA " + NombreLanceDemo(e.Fase), ExplicacionLance(e.Fase));
        }

        if (e.Tipo == TipoEventoMus.Declaracion)
        {
            EnfocarDemo(PlanoDemo.Jugador, e.Jugador, segundosPlanoJugador);
            string que = e.Fase == FaseMus.Pares ? "PARES" : "JUEGO";
            PonerRotulo(N(e.Jugador) + ": " + que + (e.Dato == 1 ? " SI" : " NO"), Explicar("decl" + que,
                e.Fase == FaseMus.Pares
                    ? "Antes de apostar a pares, cada uno dice si tiene pareja. Solo apuestan los que tienen."
                    : "Cada uno dice si suma 31 o mas. Si nadie tiene juego, se juega a PUNTO."));
            return;
        }

        if (e.Tipo != TipoEventoMus.Accion)
            return;

        EnfocarDemo(PlanoDemo.Jugador, e.Jugador, segundosPlanoJugador);
        string quien = N(e.Jugador);

        switch (e.Accion.Tipo)
        {
            case TipoAccionMus.Mus:
                if (e.Dato == 1)
                    PonerRotulo(quien + " dice MUS", Explicar("corrido",
                        "MUS CORRIDO: en la primera mano se va pidiendo mus hasta que alguien corta. El que corta empieza siendo mano."));
                else
                    PonerRotulo(quien + " dice MUS", Explicar("mus",
                        "Quiere cambiar cartas. Si los cuatro dicen mus, todos tiran las cartas que no quieren."));
                break;
            case TipoAccionMus.NoHayMus:
                PonerRotulo(quien + ": NO HAY MUS", Explicar("nohaymus",
                    "Corta el mus: se juega con estas cartas. Ahora vienen los lances: Grande, Chica, Pares y Juego."));
                break;
            case TipoAccionMus.Descartar:
                PonerRotulo(quien + " se descarta de " + AccionMus.ContarBits(e.Accion.Valor), Explicar("descarte",
                    "Tira las cartas que no le sirven y recibe otras nuevas del mazo."));
                break;
            case TipoAccionMus.Pasar:
                PonerRotulo(quien + ": PASO", Explicar("paso", "No apuesta. Si todos pasan, el lance queda 'en paso' y se cobra al final."));
                break;
            case TipoAccionMus.Envidar:
                PonerRotulo("Esto es un ENVITE: " + quien + " apuesta " + e.Accion.Valor, Explicar("envite",
                    "Los rivales pueden decir QUIERO, NO QUIERO o subir la apuesta."));
                break;
            case TipoAccionMus.Reenvidar:
                PonerRotulo(quien + " SUBE " + e.Accion.Valor + " MAS", Explicar("reenvite",
                    "Responde al envite subiendo la apuesta. Ahora decide la otra pareja."));
                break;
            case TipoAccionMus.Quiero:
                PonerRotulo(quien + ": QUIERO", Explicar("quiero",
                    "Acepta la apuesta. Los tantos se los lleva quien gane el lance, al ensenar las cartas."));
                break;
            case TipoAccionMus.NoQuiero:
                PonerRotulo(quien + ": NO QUIERO", Explicar("noquiero",
                    "Rechaza la apuesta: la pareja que envido cobra ya lo que habia antes (minimo 1 tanto)."));
                break;
            case TipoAccionMus.Ordago:
                PonerRotulo("ORDAGO de " + quien + "!", Explicar("ordago",
                    "Se lo juega todo: si lo quieren, quien gane este lance gana la partida entera."));
                break;
        }
    }

    private void RotuloDemoSena(int puesto, TipoSenalMus tipo)
    {
        if (!DemoActiva)
            return;

        EnfocarDemo(PlanoDemo.Sena, puesto, pausaSenaDemo);
        int companero = (puesto + 2) % 4;
        bool miSilla = vistaJugadorDemo && Relativo(puesto) == 0;
        PonerRotulo((miSilla ? N(puesto) + " (tu silla) hace la SENA: " : "Mira como " + N(puesto) + " hace una SENA: ") + SenalesMus.Nombre(tipo),
            "Gesto: " + SenalesMus.Gesto(tipo) + ". Es para " + N(companero) +
            Explicar("sena", ". Solo vale si el companero le esta mirando; si un rival le mira, la pilla."));
    }

    private static string NombreLanceDemo(FaseMus fase)
    {
        switch (fase)
        {
            case FaseMus.Grande: return "GRANDE";
            case FaseMus.Chica: return "CHICA";
            case FaseMus.Pares: return "PARES";
            default: return "JUEGO / PUNTO";
        }
    }

    private string ExplicacionLance(FaseMus fase)
    {
        switch (fase)
        {
            case FaseMus.Grande:
                return Explicar("grande", "Gana quien tiene las cartas mas ALTAS. Los reyes (y los treses) son lo mejor.");
            case FaseMus.Chica:
                return Explicar("chica", "Al reves que grande: gana quien tiene las cartas mas BAJAS. Los ases (y los doses) son lo mejor.");
            case FaseMus.Pares:
                return Explicar("pares", "Pareja < medias (tres iguales) < duples (dos parejas). Gana la mejor combinacion.");
            default:
                return Explicar("juego", "JUEGO es sumar 31 o mas: 31 es lo mejor, luego 32, 40, 37... Si nadie llega, se juega a PUNTO: gana quien mas se acerca a 30.");
        }
    }

    // ---------- Interfaz de la demo ----------

    private void CrearInterfazDemo()
    {
        cajaRotuloDemo = Panel("RotuloDemo", raizUI, "panel_madera", new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(1500, 190));
        cajaRotuloDemo.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);

        GameObject titulo = new("Titulo", typeof(RectTransform));
        Rect(titulo, cajaRotuloDemo.transform, new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(1440, 78));
        txtRotuloTitulo = Texto(titulo.transform, "", 54, Oro, true);

        GameObject detalle = new("Detalle", typeof(RectTransform));
        Rect(detalle, cajaRotuloDemo.transform, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(1440, 90));
        txtRotuloDetalle = Texto(detalle.transform, "", 30, Crema);
        cajaRotuloDemo.SetActive(false);

        panelCartasDemo = Panel("CartasDemo", raizUI, "panel_madera", new Vector2(0f, 0.5f), new Vector2(20, 40), new Vector2(430, 760));
        panelCartasDemo.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

        GameObject cabecera = new("Cabecera", typeof(RectTransform));
        Rect(cabecera, panelCartasDemo.transform, new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(400, 50));
        Texto(cabecera.transform, "CARTAS DE TODOS", 32, Oro, true);

        for (int p = 0; p < 4; p++)
        {
            float y = -70 - p * 172;
            GameObject nombre = new("Nombre_" + p, typeof(RectTransform));
            Rect(nombre, panelCartasDemo.transform, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(400, 36));
            nombresCartasDemo[p] = Texto(nombre.transform, N(p), 24, Crema, false, TextAlignmentOptions.Left);

            for (int c = 0; c < 4; c++)
            {
                GameObject carta = new("Carta", typeof(RectTransform), typeof(Image));
                Rect(carta, panelCartasDemo.transform, new Vector2(0f, 1f), new Vector2(24 + c * 96, y - 38), new Vector2(88, 128));
                Image img = carta.GetComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;
                imagenesCartasDemo[p * 4 + c] = img;
            }
        }

        panelCartasDemo.SetActive(false);

        Button salir = Boton(raizUI, "rojo", "SALIR DE LA DEMO", new Vector2(320, 70), SalirDeLaDemo, 24);
        Rect(salir.gameObject, raizUI, new Vector2(0f, 1f), new Vector2(190, -50), new Vector2(320, 70));
        botonSalirDemo = salir.gameObject;
        botonSalirDemo.SetActive(false);
    }

    private void AjustarInterfazDemo(bool enMesa, bool empezada, bool revision)
    {
        if (cajaRotuloDemo == null)
            CrearInterfazDemo();

        bool demo = DemoActiva || (demoPendiente && enMesa);
        botonSalirDemo.SetActive(demo && !uiLimpiaDemo);

        if (!demo)
        {
            cajaRotuloDemo.SetActive(false);
            panelCartasDemo.SetActive(false);
            return;
        }

        // En la demo sobran la sala, el panel del host y el chat.
        panelSalaUI.SetActive(false);
        panelHostUI.SetActive(false);
        botonChatUI.gameObject.SetActive(false);
        panelChatUI.SetActive(false);
        avisoChatUI.SetActive(false);

        if (uiLimpiaDemo)
        {
            panelHistorialUI.SetActive(false);
            botonHistorialUI.gameObject.SetActive(false);
        }

        ActualizarRotuloDemo();
        ActualizarCartasDemo(empezada && !revision && cartasDemoVisibles);
    }

    private void ActualizarRotuloDemo()
    {
        if (Time.time >= finRotuloDemo && rotulosDemo.Count > 0)
        {
            Rotulo r = rotulosDemo.Dequeue();
            txtRotuloTitulo.text = r.Titulo;
            txtRotuloDetalle.text = r.Detalle;
            bool largo = !string.IsNullOrEmpty(r.Detalle);
            cajaRotuloDemo.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, largo ? 190 : 104);
            float lectura = largo ? 2.5f + r.Detalle.Length * 0.045f : 2.2f;
            finRotuloDemo = Time.time + Mathf.Min(lectura, 7f);
        }

        cajaRotuloDemo.SetActive(Time.time < finRotuloDemo + 0.6f && txtRotuloTitulo.text.Length > 0);
    }

    private string firmaManoSillaDemo;

    // La silla de la camara (LILA) ensena su mano abajo, como si fuera la tuya.
    private void MostrarManoDeLaSillaDemo()
    {
        int puesto = 0;
        int[] mano = partida.CartasDemo(puesto);

        if (mano == null || mano[0] < 0)
            return;

        string firma = string.Join(",", mano);

        if (firma == firmaManoSillaDemo)
            return;

        firmaManoSillaDemo = firma;
        reparto.EstablecerManoExterna(Relativo(puesto), CartasMus.ATexto(mano));

        if (panelCartas != null)
            panelCartas.SetActive(true);

        if (vistaJugadorDemo)
            EnfocarDemo(PlanoDemo.Cartas, puesto, segundosMirarCartasDemo);
    }

    private void ActualizarCartasDemo(bool visible)
    {
        panelCartasDemo.SetActive(visible);

        if (!visible)
        {
            if (DemoActiva && !enRevision && cola.Count == 0)
                MostrarManoDeLaSillaDemo();

            return;
        }

        // Se actualizan cuando la mesa ya ha ensenado lo ultimo (asi no se adelantan los descartes).
        if (cola.Count > 0)
            return;

        int turno = partida.Estado.EnCurso ? partida.Estado.JugadorActual : -1;
        MostrarManoDeLaSillaDemo();

        for (int p = 0; p < 4; p++)
        {
            int[] mano = partida.CartasDemo(p);
            nombresCartasDemo[p].text = (p == turno ? "> " : "") + N(p) +
                "  <size=70%>" + (p % 2 == 0 ? "pareja LILA-MENTA" : "pareja MOSTAZA-CORAL") + "</size>";

            for (int c = 0; c < 4; c++)
            {
                int carta = mano != null ? mano[c] : -1;
                Image img = imagenesCartasDemo[p * 4 + c];

                if (cartasDemoMostradas[p * 4 + c] == carta)
                    continue;

                cartasDemoMostradas[p * 4 + c] = carta;
                img.sprite = carta >= 0 ? BarajaSpritesMus.Obtener(CartasMus.Texto(carta)) : null;
                img.enabled = img.sprite != null;
            }
        }
    }
}
