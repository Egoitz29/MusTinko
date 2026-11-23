#if !MUS_TINKO_SIN_INTERNET
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

// SALAS POR INTERNET (Unity Lobby + Relay, con el paquete Multiplayer Services)
//
//  - CREAR SALA: publica (sale en la lista) o con codigo (solo entra quien tenga el codigo).
//    Se eligen la entrada en monedas, a cuantos tantos y el tiempo por turno.
//  - ENTRAR: te unes y eliges silla. Para empezar hacen falta al menos 2 personas (maximo 4); el resto, bots.
//  - Mientras se juega, la sala se cierra (no sale en la lista ni se puede entrar). Al terminar se vuelve a abrir.
//  - Cada jugador lleva su nombre en la sala (para "jugasteis hace poco" en Amigos).
public class SalaInternetMus : MonoBehaviour
{
    public const int MaxJugadores = 4;
    public const int MinimoJugadores = 2;

    private const string ClaveVersion = "version";
    private const string ClaveApuesta = "apuesta";
    private const string ClaveTantos = "tantos";
    private const string ClaveNombre = "nombre";
    private const string VersionSalas = "mus1";   // si cambian las reglas de red, se sube y no se mezclan versiones

    public struct SalaPublica
    {
        public string Id;
        public string Nombre;
        public int Jugadores;
        public int Apuesta;
        public int Tantos;
    }

    public class Opciones
    {
        public string Nombre = "";
        public bool Privada;
        public int Apuesta;
        public int Tantos = 40;
        public float Segundos = 25f;
    }

    public struct JugadorReciente
    {
        public string Id;
        public string Nombre;
    }

    private ISession sesion;
    private readonly List<SalaPublica> salas = new();
    private string estado = "";
    private bool ocupado, buscando, guardando;
    private PartidaOnlineMus partida;
    private bool cerradaEnviada;
    private int apuestaEnviada = -1;
    private float sinRedDesde = -1f;
    private Opciones pendientes;
    private string miNombre = "";

    public IReadOnlyList<SalaPublica> Salas => salas;
    public bool Buscando => buscando;
    public bool Ocupado => ocupado;
    public string Estado => estado;
    public bool EnSala => sesion != null;
    public bool SoyAnfitrion => sesion != null && sesion.IsHost;
    public string IdSala => sesion?.Id ?? "";
    public string Codigo => sesion?.Code ?? "";
    public string NombreSala => sesion?.Name ?? "";
    public bool SalaPrivada => sesion != null && sesion.IsPrivate;
    public int JugadoresEnSala => sesion?.PlayerCount ?? 0;
    public int Busquedas { get; private set; }

    // Personas con las que has estado en la sala (para anadirlas como amigos).
    public static readonly List<JugadorReciente> Recientes = new();

    private Task<bool> PrepararServicios() => ServiciosUnityMus.Preparar();

    private static string Mensaje(Exception e)
    {
        if (e is SessionException s)
        {
            switch (s.Error)
            {
                case SessionError.SessionNotFound:
                case SessionError.SessionDeleted:
                    return "esa sala ya no existe.";
                case SessionError.NotAuthorized:
                    return "sin permiso (revisa que Lobby y Relay esten activados en Unity Cloud).";
            }
        }

        string m = e.Message ?? "";

        if (m.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0)
            return "la sala esta llena.";

        if (m.IndexOf("locked", StringComparison.OrdinalIgnoreCase) >= 0)
            return "la partida ya ha empezado.";

        return m;
    }

    private Dictionary<string, PlayerProperty> PropiedadesJugador() => new()
    {
        { ClaveNombre, new PlayerProperty(string.IsNullOrWhiteSpace(miNombre) ? "Jugador" : miNombre, VisibilityPropertyOptions.Member) }
    };

    // ------------------------------------------------------------------ lista de salas

    public async void BuscarSalas()
    {
        if (buscando || ocupado || EnSala)
            return;

        buscando = true;
        Busquedas++;

        if (await PrepararServicios())
        {
            try
            {
                QuerySessionsOptions consulta = new() { Count = 30 };
                consulta.FilterOptions.Add(new FilterOption(FilterField.AvailableSlots, "0", FilterOperation.Greater));
                consulta.FilterOptions.Add(new FilterOption(FilterField.StringIndex1, VersionSalas, FilterOperation.Equal));
                consulta.SortOptions.Add(new SortOption(SortOrder.Descending, SortField.CreationTime));
                QuerySessionsResults resultado = await MultiplayerService.Instance.QuerySessionsAsync(consulta);

                salas.Clear();

                foreach (ISessionInfo info in resultado.Sessions)
                {
                    // Las que estan jugando (cerradas) o con contrasena no se ensenan.
                    if (info.IsLocked || info.HasPassword)
                        continue;

                    salas.Add(new SalaPublica
                    {
                        Id = info.Id,
                        Nombre = info.Name,
                        Jugadores = info.MaxPlayers - info.AvailableSlots,
                        Apuesta = Numero(info.Properties, ClaveApuesta, 0),
                        Tantos = Numero(info.Properties, ClaveTantos, 40)
                    });
                }

                estado = "";
            }
            catch (Exception e)
            {
                estado = "No se pudieron cargar las salas: " + Mensaje(e);
                Debug.LogWarning("[MUS INTERNET] " + e);
            }
        }
        else
        {
            estado = ServiciosUnityMus.Error;
        }

        buscando = false;
    }

    private static int Numero(IReadOnlyDictionary<string, SessionProperty> propiedades, string clave, int porDefecto)
    {
        if (propiedades != null && propiedades.TryGetValue(clave, out SessionProperty p) && int.TryParse(p.Value, out int n))
            return n;

        return porDefecto;
    }

    // ------------------------------------------------------------------ crear / entrar / salir

    public void CrearSala(string nombreJugador, int apuesta) =>
        CrearSala(nombreJugador, new Opciones { Apuesta = apuesta });

    public async void CrearSala(string nombreJugador, Opciones opciones)
    {
        if (ocupado || EnSala || opciones == null)
            return;

        ocupado = true;
        miNombre = nombreJugador ?? "";
        estado = "Creando sala...";

        if (await PrepararServicios())
        {
            try
            {
                string nombre = !string.IsNullOrWhiteSpace(opciones.Nombre) ? opciones.Nombre.Trim()
                    : "Mesa de " + (string.IsNullOrWhiteSpace(miNombre) ? "Mus" : miNombre.Trim());
                SessionOptions so = new SessionOptions
                {
                    Name = nombre,
                    MaxPlayers = MaxJugadores,
                    IsPrivate = opciones.Privada,
                    PlayerProperties = PropiedadesJugador(),
                    SessionProperties = new Dictionary<string, SessionProperty>
                    {
                        { ClaveVersion, new SessionProperty(VersionSalas, VisibilityPropertyOptions.Public, PropertyIndex.String1) },
                        { ClaveApuesta, new SessionProperty(Mathf.Max(0, opciones.Apuesta).ToString(), VisibilityPropertyOptions.Public, PropertyIndex.Number1) },
                        { ClaveTantos, new SessionProperty(opciones.Tantos.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.Number2) }
                    }
                }.WithRelayNetwork();

                IHostSession nueva = await MultiplayerService.Instance.CreateSessionAsync(so);
                Entrar(nueva);
                pendientes = opciones;
                cerradaEnviada = false;
                apuestaEnviada = Mathf.Max(0, opciones.Apuesta);
                estado = "";
                Debug.Log("[MUS INTERNET] Sala creada: " + nueva.Name + (opciones.Privada ? " (codigo " + nueva.Code + ")" : ""));
            }
            catch (Exception e)
            {
                estado = "No se pudo crear la sala: " + Mensaje(e);
                Debug.LogException(e);
            }
        }
        else
        {
            estado = ServiciosUnityMus.Error;
        }

        ocupado = false;
    }

    public async void Unirse(string idSala, string nombreJugador = "")
    {
        if (ocupado || EnSala || string.IsNullOrEmpty(idSala))
            return;

        ocupado = true;
        miNombre = nombreJugador ?? "";
        estado = "Entrando en la sala...";

        if (await PrepararServicios())
        {
            try
            {
                Entrar(await MultiplayerService.Instance.JoinSessionByIdAsync(idSala, new JoinSessionOptions { PlayerProperties = PropiedadesJugador() }));
                estado = "";
                Debug.Log("[MUS INTERNET] Dentro de la sala " + sesion.Name);
            }
            catch (Exception e)
            {
                estado = "No se pudo entrar: " + Mensaje(e);
                Debug.LogException(e);
            }
        }
        else
        {
            estado = ServiciosUnityMus.Error;
        }

        ocupado = false;
    }

    // Salas con codigo (privadas).
    public async void UnirseConCodigo(string codigo, string nombreJugador = "")
    {
        codigo = (codigo ?? "").Trim().ToUpperInvariant();

        if (ocupado || EnSala || codigo.Length == 0)
            return;

        ocupado = true;
        miNombre = nombreJugador ?? "";
        estado = "Entrando en la sala " + codigo + "...";

        if (await PrepararServicios())
        {
            try
            {
                Entrar(await MultiplayerService.Instance.JoinSessionByCodeAsync(codigo, new JoinSessionOptions { PlayerProperties = PropiedadesJugador() }));
                estado = "";
            }
            catch (Exception e)
            {
                estado = "No se pudo entrar: " + Mensaje(e);
                Debug.LogException(e);
            }
        }
        else
        {
            estado = ServiciosUnityMus.Error;
        }

        ocupado = false;
    }

    private void Entrar(ISession nueva)
    {
        sesion = nueva;
        sinRedDesde = -1f;
        nueva.RemovedFromSession += AlSalirDeLaSala;
        nueva.Deleted += AlSalirDeLaSala;
        nueva.Changed += AnotarRecientes;
        AnotarRecientes();
    }

    // Guarda quien hay en la sala (menos tu) para poder anadirlos como amigos despues.
    private void AnotarRecientes()
    {
        if (sesion == null || sesion.Players == null)
            return;

        string yo = ServiciosUnityMus.IdJugador;

        foreach (IReadOnlyPlayer p in sesion.Players)
        {
            if (p == null || p.Id == yo || Recientes.Exists(r => r.Id == p.Id))
                continue;

            string nombre = p.Properties != null && p.Properties.TryGetValue(ClaveNombre, out PlayerProperty n) ? n.Value : "Jugador";
            Recientes.Insert(0, new JugadorReciente { Id = p.Id, Nombre = nombre });
        }

        while (Recientes.Count > 8)
            Recientes.RemoveAt(Recientes.Count - 1);
    }

    private void AlSalirDeLaSala()
    {
        if (sesion == null)
            return;

        sesion = null;
        estado = "La sala se ha cerrado.";

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    public async void Salir()
    {
        ISession s = sesion;
        sesion = null;
        pendientes = null;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        if (s == null)
            return;

        try
        {
            // El que la creo la borra (si no, se queda en la lista sin nadie que la lleve).
            if (s.IsHost)
                await s.AsHost().DeleteAsync();
            else
                await s.LeaveAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MUS INTERNET] Al salir de la sala: " + e.Message);
        }
    }

    // ------------------------------------------------------------------ mientras estas en una sala

    private void Update()
    {
        if (partida == null)
            partida = FindFirstObjectByType<PartidaOnlineMus>();

        if (sesion == null)
        {
            if (partida != null)
                partida.MinimoHumanos = 1;
            return;
        }

        // Si se ha cerrado la conexion (has salido, o el que creo la sala se ha ido), se deja la sala.
        NetworkManager red = NetworkManager.Singleton;

        if (red == null || !red.IsListening)
        {
            if (sinRedDesde < 0f)
                sinRedDesde = Time.unscaledTime;
            else if (Time.unscaledTime - sinRedDesde > 4f)
            {
                estado = "Has salido de la sala (o se ha perdido la conexion).";
                Salir();
            }
            return;
        }

        sinRedDesde = -1f;

        if (partida == null || !sesion.IsHost || !partida.IsSpawned)
            return;

        partida.MinimoHumanos = MinimoJugadores;

        // Las opciones elegidas al crear la sala se aplican en cuanto la mesa existe.
        if (pendientes != null && partida.IsServer)
        {
            partida.CambiarApuesta(Mathf.Max(0, pendientes.Apuesta));
            partida.ConfigurarReglas(pendientes.Tantos, pendientes.Segundos);
            pendientes = null;
        }

        bool cerrar = partida.PartidaEnCurso;
        int apuesta = partida.ApuestaSalaVista;

        if (!guardando && (cerrar != cerradaEnviada || apuesta != apuestaEnviada))
            GuardarSala(cerrar, apuesta);
    }

    private async void GuardarSala(bool cerrar, int apuesta)
    {
        ISession s = sesion;

        if (s == null)
            return;

        guardando = true;

        try
        {
            IHostSession host = s.AsHost();
            host.IsLocked = cerrar;
            host.SetProperty(ClaveApuesta, new SessionProperty(apuesta.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.Number1));
            await host.SavePropertiesAsync();
            cerradaEnviada = cerrar;
            apuestaEnviada = apuesta;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MUS INTERNET] No se pudo actualizar la sala: " + e.Message);
            await Task.Delay(3000);
        }

        guardando = false;
    }

    private void OnApplicationQuit() => SalirSinEsperar();
    private void OnDestroy() => SalirSinEsperar();

    private void SalirSinEsperar()
    {
        ISession s = sesion;
        sesion = null;

        if (s == null)
            return;

        try
        {
            _ = s.IsHost ? s.AsHost().DeleteAsync() : s.LeaveAsync();
        }
        catch (Exception)
        {
        }
    }

    // ------------------------------------------------------------------ interfaz antigua (sin la interfaz nueva)

    private void OnGUI()
    {
        if (VistaMesaOnlineMus.InterfazNuevaActiva)
            return;

        EscalaGUIMus.Aplicar();
        NetworkManager red = NetworkManager.Singleton;

        if (red != null && red.IsListening)
            return;

        GUILayout.BeginArea(new Rect(20, 360, Mathf.Min(460f, EscalaGUIMus.Ancho - 40f), 300), "SALAS POR INTERNET", GUI.skin.window);
        GUI.enabled = !ocupado;

        if (GUILayout.Button("CREAR SALA", GUILayout.Height(46)))
            CrearSala("", 0);

        if (GUILayout.Button(buscando ? "BUSCANDO..." : "ACTUALIZAR LISTA", GUILayout.Height(36)))
            BuscarSalas();

        foreach (SalaPublica s in salas)
            if (GUILayout.Button(s.Nombre + "  (" + s.Jugadores + "/" + MaxJugadores + ")", GUILayout.Height(36)))
                Unirse(s.Id);

        GUI.enabled = true;

        if (!string.IsNullOrEmpty(estado))
            GUILayout.Label(estado);

        GUILayout.EndArea();
    }
}
#endif
