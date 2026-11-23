#if MUS_TINKO_INTERNET
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

// SALAS POR INTERNET (Unity Lobby + Relay, con el paquete Multiplayer Services)
//
//  - CREAR SALA: la sala sale en la lista de todos ("Mesa de Egoitz", jugadores y entrada en monedas).
//  - ENTRAR: te unes y eliges silla en la sala, como en la WiFi.
//  - Para empezar hacen falta al menos 2 personas (maximo 4); las sillas libres las ocupan bots.
//  - Mientras se juega, la sala se cierra (no sale en la lista ni se puede entrar). Al terminar se vuelve a abrir.
//
// Necesita el proyecto vinculado a Unity Cloud con Lobby y Relay activados (Project Settings > Services).
public class SalaInternetMus : MonoBehaviour
{
    public const int MaxJugadores = 4;
    public const int MinimoJugadores = 2;

    private const string ClaveVersion = "version";
    private const string ClaveApuesta = "apuesta";
    private const string VersionSalas = "mus1";   // si cambian las reglas de red, se sube y no se mezclan versiones

    public struct SalaPublica
    {
        public string Id;
        public string Nombre;
        public int Jugadores;
        public int Apuesta;
    }

    private ISession sesion;
    private readonly List<SalaPublica> salas = new();
    private string estado = "";
    private bool ocupado, buscando, guardando;
    private PartidaOnlineMus partida;
    private bool cerradaEnviada;
    private int apuestaEnviada = -1;
    private float sinRedDesde = -1f;

    public IReadOnlyList<SalaPublica> Salas => salas;
    public bool Buscando => buscando;
    public bool Ocupado => ocupado;
    public string Estado => estado;
    public bool EnSala => sesion != null;
    public string Codigo => sesion?.Code ?? "";
    public string NombreSala => sesion?.Name ?? "";
    public int Busquedas { get; private set; }

    // ------------------------------------------------------------------ servicios

    private async Task<bool> PrepararServicios()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                InitializationOptions opciones = new();
#if UNITY_EDITOR
                // Varias ventanas de prueba en el mismo ordenador: cada una con su propio jugador.
                opciones.SetProfile("editor" + UnityEngine.Random.Range(0, 1000000));
#endif
                await UnityServices.InitializeAsync(opciones);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            return true;
        }
        catch (Exception e)
        {
            estado = "No se pudo conectar con los servidores. Revisa Internet.";
            Debug.LogException(e);
            return false;
        }
    }

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
                QuerySessionsOptions consulta = new() { Count = 20 };
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

                    int apuesta = 0;

                    if (info.Properties != null && info.Properties.TryGetValue(ClaveApuesta, out SessionProperty p))
                        int.TryParse(p.Value, out apuesta);

                    salas.Add(new SalaPublica
                    {
                        Id = info.Id,
                        Nombre = info.Name,
                        Jugadores = info.MaxPlayers - info.AvailableSlots,
                        Apuesta = apuesta
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

        buscando = false;
    }

    // ------------------------------------------------------------------ crear / entrar / salir

    public async void CrearSala(string nombreJugador, int apuesta)
    {
        if (ocupado || EnSala)
            return;

        ocupado = true;
        estado = "Creando sala...";

        if (await PrepararServicios())
        {
            try
            {
                string nombre = "Mesa de " + (string.IsNullOrWhiteSpace(nombreJugador) ? "Mus" : nombreJugador.Trim());
                SessionOptions opciones = new SessionOptions
                {
                    Name = nombre,
                    MaxPlayers = MaxJugadores,
                    IsPrivate = false,
                    SessionProperties = new Dictionary<string, SessionProperty>
                    {
                        { ClaveVersion, new SessionProperty(VersionSalas, VisibilityPropertyOptions.Public, PropertyIndex.String1) },
                        { ClaveApuesta, new SessionProperty(Mathf.Max(0, apuesta).ToString(), VisibilityPropertyOptions.Public, PropertyIndex.Number1) }
                    }
                }.WithRelayNetwork();

                IHostSession nueva = await MultiplayerService.Instance.CreateSessionAsync(opciones);
                Entrar(nueva);
                cerradaEnviada = false;
                apuestaEnviada = Mathf.Max(0, apuesta);
                estado = "";
                Debug.Log("[MUS INTERNET] Sala creada: " + nueva.Name);
            }
            catch (Exception e)
            {
                estado = "No se pudo crear la sala: " + Mensaje(e);
                Debug.LogException(e);
            }
        }

        ocupado = false;
    }

    public async void Unirse(string idSala)
    {
        if (ocupado || EnSala || string.IsNullOrEmpty(idSala))
            return;

        ocupado = true;
        estado = "Entrando en la sala...";

        if (await PrepararServicios())
        {
            try
            {
                Entrar(await MultiplayerService.Instance.JoinSessionByIdAsync(idSala));
                estado = "";
                Debug.Log("[MUS INTERNET] Dentro de la sala " + sesion.Name);
            }
            catch (Exception e)
            {
                estado = "No se pudo entrar: " + Mensaje(e);
                Debug.LogException(e);
            }
        }

        ocupado = false;
    }

    // Con codigo (por si alguna vez se quieren salas privadas).
    public async void UnirseConCodigo(string codigo)
    {
        codigo = (codigo ?? "").Trim().ToUpperInvariant();

        if (ocupado || EnSala || codigo.Length == 0)
            return;

        ocupado = true;
        estado = "Entrando en la sala " + codigo + "...";

        if (await PrepararServicios())
        {
            try
            {
                Entrar(await MultiplayerService.Instance.JoinSessionByCodeAsync(codigo));
                estado = "";
            }
            catch (Exception e)
            {
                estado = "No se pudo entrar: " + Mensaje(e);
                Debug.LogException(e);
            }
        }

        ocupado = false;
    }

    private void Entrar(ISession nueva)
    {
        sesion = nueva;
        sinRedDesde = -1f;
        nueva.RemovedFromSession += AlSalirDeLaSala;
        nueva.Deleted += AlSalirDeLaSala;
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

        if (partida == null || !sesion.IsHost)
            return;

        partida.MinimoHumanos = MinimoJugadores;
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
