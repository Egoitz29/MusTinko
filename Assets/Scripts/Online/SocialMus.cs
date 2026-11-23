#if !MUS_TINKO_SIN_INTERNET
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using Unity.Services.Friends.Options;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

// AMIGOS Y CLASIFICACION (Unity Friends + Leaderboards)
//
//  Amigos:
//   - Tu codigo de amigo es tu nombre de Unity ("Egoitz#4821"). Se anade a alguien con su codigo.
//   - Lista con su estado en directo: en el menu, en su sala (con el id para UNIRSE), jugando o desconectado.
//   - INVITAR manda un mensaje con tu sala; al otro le sale un aviso para unirse.
//  Clasificacion:
//   - "mus_semanal" (se reinicia cada lunes) y "mus_global". Hay que crearlas en Unity Cloud con esos ids
//     (mas alto es mejor, guardar la mejor puntuacion). Los puntos salen de PerfilMus.
//
// Si los servicios no estan activados, todo sigue funcionando y la pantalla explica que falta.
public class SocialMus : MonoBehaviour
{
    public const string ClasificacionSemanal = "mus_semanal";
    public const string ClasificacionGlobal = "mus_global";

    [Serializable]
    public class ActividadMus
    {
        public string Estado = "";
        public string SalaId = "";
        public string SalaNombre = "";
        public int Jugadores;
    }

    [Serializable]
    public class MensajeMus
    {
        public string Tipo = "";
        public string SalaId = "";
        public string SalaNombre = "";
        public string De = "";
    }

    public class Amigo
    {
        public string Id;
        public string Nombre;
        public string Codigo;
        public Availability Disponibilidad;
        public DateTime UltimaVez;
        public ActividadMus Actividad = new();

        public bool Conectado => Disponibilidad == Availability.Online || Disponibilidad == Availability.Busy || Disponibilidad == Availability.Away;
        public bool PuedeUnirse => Conectado && Actividad != null && !string.IsNullOrEmpty(Actividad.SalaId) && Actividad.Jugadores < 4;
    }

    public class Invitacion
    {
        public string DeId;
        public string De;
        public string SalaId;
        public string SalaNombre;
    }

    public class Puesto
    {
        public int Rango;
        public string Id;
        public string Nombre;
        public double Puntos;
        public bool Yo;
    }

    private static SocialMus instancia;

    // Se piden al registro de Unity Services y no a FriendsService.Instance / LeaderboardsService.Instance:
    // en el editor esos accesos se vacian al cargar la escena aunque los servicios ya esten iniciados.
    private static IFriendsService Amistad =>
        UnityServices.Instance?.GetFriendsService() ?? throw new InvalidOperationException("Friends no esta iniciado (¿esta activado en Unity Cloud?)");

    private static ILeaderboardsService Tablas =>
        UnityServices.Instance?.GetLeaderboardsService() ?? throw new InvalidOperationException("Leaderboards no esta iniciado (¿esta activado en Unity Cloud?)");

    public static SocialMus Instancia
    {
        get
        {
            if (instancia == null)
                instancia = new GameObject("SocialMus").AddComponent<SocialMus>();
            return instancia;
        }
    }

    public bool AmigosListos { get; private set; }
    public string ErrorAmigos { get; private set; } = "";
    public string AvisoAmigos { get; private set; } = "";
    public int Version { get; private set; }
    public readonly List<Amigo> Amigos = new();
    public readonly List<Amigo> Solicitudes = new();
    public readonly List<Amigo> Enviadas = new();
    public Invitacion InvitacionPendiente { get; set; }

    private Task<bool> preparandoAmigos;
    private bool eventosPuestos;
    private string presenciaEnviada = "";
    private float siguientePresencia;
    private readonly List<string> invitarAlCrearSala = new();

    // ------------------------------------------------------------------ amigos

    public Task<bool> PrepararAmigos()
    {
        if (preparandoAmigos == null || (preparandoAmigos.IsCompleted && !preparandoAmigos.Result))
            preparandoAmigos = PrepararAmigosAhora();

        return preparandoAmigos;
    }

    private async Task<bool> PrepararAmigosAhora()
    {
        if (!await ServiciosUnityMus.Preparar())
        {
            ErrorAmigos = ServiciosUnityMus.Error;
            Version++;
            return false;
        }

        try
        {
            await Amistad.InitializeAsync(new InitializeOptions().WithEvents(true).WithMemberPresence(true).WithMemberProfile(true));
            if (!eventosPuestos)
            {
                eventosPuestos = true;
                Amistad.RelationshipAdded += _ => Refrescar();
                Amistad.RelationshipDeleted += _ => Refrescar();
                Amistad.PresenceUpdated += _ => Refrescar();
                Amistad.MessageReceived += AlRecibirMensaje;
            }
            AmigosListos = true;
            ErrorAmigos = "";
            Refrescar();
            return true;
        }
        catch (Exception e)
        {
            ErrorAmigos = "No se pudo cargar Amigos (¿esta activado Friends en Unity Cloud?): " + e.Message;
            Debug.LogWarning("[AMIGOS] " + e);
            Version++;
            return false;
        }
    }

    private void Refrescar()
    {
        if (!AmigosListos)
            return;

        Llenar(Amigos, Amistad.Friends);
        Llenar(Solicitudes, Amistad.IncomingFriendRequests);
        Llenar(Enviadas, Amistad.OutgoingFriendRequests);

        // Primero los conectados.
        Amigos.Sort((a, b) => a.Conectado != b.Conectado ? (a.Conectado ? -1 : 1) : string.Compare(a.Nombre, b.Nombre, StringComparison.OrdinalIgnoreCase));
        Version++;
    }

    private static void Llenar(List<Amigo> lista, IReadOnlyList<Relationship> relaciones)
    {
        lista.Clear();

        if (relaciones == null)
            return;

        foreach (Relationship r in relaciones)
        {
            if (r?.Member == null)
                continue;

            Amigo a = new()
            {
                Id = r.Member.Id,
                Codigo = r.Member.Profile?.Name ?? "",
                Disponibilidad = r.Member.Presence?.Availability ?? Availability.Unknown,
                UltimaVez = r.Member.Presence?.LastSeen ?? DateTime.MinValue
            };
            a.Nombre = string.IsNullOrEmpty(a.Codigo) ? "Jugador" : ServiciosUnityMus.SinCodigo(a.Codigo);

            try
            {
                if (r.Member.Presence != null)
                    a.Actividad = r.Member.Presence.GetActivity<ActividadMus>() ?? new ActividadMus();
            }
            catch (Exception)
            {
                a.Actividad = new ActividadMus();
            }

            lista.Add(a);
        }
    }

    public string TextoEstado(Amigo a)
    {
        if (a.Conectado)
        {
            if (!string.IsNullOrEmpty(a.Actividad?.Estado))
                return a.Actividad.Estado;
            return "Conectado";
        }

        if (a.UltimaVez <= DateTime.MinValue.AddDays(1))
            return "Desconectado";

        TimeSpan t = DateTime.UtcNow - a.UltimaVez.ToUniversalTime();
        if (t.TotalHours < 1) return "Desconectado hace " + Mathf.Max(1, (int)t.TotalMinutes) + " min";
        if (t.TotalDays < 1) return "Desconectado hace " + (int)t.TotalHours + " h";
        return "Desconectado hace " + (int)t.TotalDays + " dias";
    }

    public async void AnadirPorCodigo(string codigo)
    {
        codigo = (codigo ?? "").Trim();

        if (codigo.Length == 0)
            return;

        if (!codigo.Contains("#"))
        {
            Avisar("El codigo es tipo Nombre#1234 (con la almohadilla).");
            return;
        }

        if (!await PrepararAmigos())
            return;

        try
        {
            await Amistad.AddFriendByNameAsync(codigo);
            Avisar("Solicitud enviada a " + ServiciosUnityMus.SinCodigo(codigo));
        }
        catch (Exception e)
        {
            Avisar("No se pudo anadir: " + e.Message);
        }

        Refrescar();
    }

    public async void AnadirPorId(string id, string nombre)
    {
        if (string.IsNullOrEmpty(id) || !await PrepararAmigos())
            return;

        try
        {
            await Amistad.AddFriendAsync(id);
            Avisar("Solicitud enviada a " + nombre);
        }
        catch (Exception e)
        {
            Avisar("No se pudo anadir: " + e.Message);
        }

        Refrescar();
    }

    // Aceptar una solicitud es "anadir" a quien te la mando.
    public void Aceptar(Amigo a) => AnadirPorId(a.Id, a.Nombre);

    public async void Rechazar(Amigo a)
    {
        try
        {
            await Amistad.DeleteIncomingFriendRequestAsync(a.Id);
        }
        catch (Exception e)
        {
            Avisar("No se pudo rechazar: " + e.Message);
        }

        Refrescar();
    }

    public async void Invitar(string idAmigo, string idSala, string nombreSala, string miNombre)
    {
        if (!AmigosListos || string.IsNullOrEmpty(idSala))
            return;

        try
        {
            await Amistad.MessageAsync(idAmigo, new MensajeMus { Tipo = "invitacion", SalaId = idSala, SalaNombre = nombreSala, De = miNombre });
            Avisar("Invitacion enviada");
        }
        catch (Exception e)
        {
            Avisar("No se pudo invitar: " + e.Message);
        }
    }

    // Desde el menu (sin sala): se crea una sala con codigo y, en cuanto exista, se invita.
    public void InvitarCuandoHayaSala(string idAmigo)
    {
        if (!invitarAlCrearSala.Contains(idAmigo))
            invitarAlCrearSala.Add(idAmigo);
    }

    public void EnviarInvitacionesPendientes(string idSala, string nombreSala, string miNombre)
    {
        foreach (string id in invitarAlCrearSala)
            Invitar(id, idSala, nombreSala, miNombre);

        invitarAlCrearSala.Clear();
    }

    public bool HayInvitacionesPendientes => invitarAlCrearSala.Count > 0;

    private void AlRecibirMensaje(IMessageReceivedEvent e)
    {
        try
        {
            MensajeMus m = e.GetAs<MensajeMus>();

            if (m != null && m.Tipo == "invitacion" && !string.IsNullOrEmpty(m.SalaId))
            {
                InvitacionPendiente = new Invitacion { DeId = e.UserId, De = m.De, SalaId = m.SalaId, SalaNombre = m.SalaNombre };
                Version++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AMIGOS] Mensaje no valido: " + ex.Message);
        }
    }

    // Lo que ven tus amigos de ti ("En el menu", "En su sala 2/4", "Jugando").
    public void ActualizarPresencia(string estado, string salaId, string salaNombre, int jugadores)
    {
        if (!AmigosListos)
            return;

        string clave = estado + "|" + salaId + "|" + jugadores;

        if (clave == presenciaEnviada || Time.unscaledTime < siguientePresencia)
            return;

        presenciaEnviada = clave;
        siguientePresencia = Time.unscaledTime + 3f;
        EnviarPresencia(new ActividadMus { Estado = estado, SalaId = salaId ?? "", SalaNombre = salaNombre ?? "", Jugadores = jugadores });
    }

    private async void EnviarPresencia(ActividadMus actividad)
    {
        try
        {
            await Amistad.SetPresenceAsync(Availability.Online, actividad);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AMIGOS] No se pudo actualizar el estado: " + e.Message);
            presenciaEnviada = "";
        }
    }

    private float finAviso;

    private void Avisar(string texto)
    {
        AvisoAmigos = texto;
        finAviso = Time.unscaledTime + 5f;
        Version++;
    }

    private void Update()
    {
        if (AvisoAmigos.Length > 0 && Time.unscaledTime > finAviso)
        {
            AvisoAmigos = "";
            Version++;
        }
    }

    // ------------------------------------------------------------------ clasificacion

    public bool CargandoClasificacion { get; private set; }
    public string ErrorClasificacion { get; private set; } = "";
    public readonly List<Puesto> Clasificacion = new();
    public Puesto MiPuesto { get; private set; }
    public string TipoCargado { get; private set; } = "";

    // Sube tus puntos (los de PerfilMus) a las dos clasificaciones.
    public async void SubirPuntos()
    {
        if (PerfilMus.PuntosTotal <= 0 || !await ServiciosUnityMus.Preparar())
            return;

        try
        {
            await Tablas.AddPlayerScoreAsync(ClasificacionSemanal, PerfilMus.PuntosSemana);
            await Tablas.AddPlayerScoreAsync(ClasificacionGlobal, PerfilMus.PuntosTotal);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[CLASIFICACION] No se pudieron subir los puntos: " + e.Message);
        }
    }

    // tipo: "semanal", "global" o "amigos"
    public async void CargarClasificacion(string tipo)
    {
        if (CargandoClasificacion)
            return;

        CargandoClasificacion = true;
        Version++;

        if (!await ServiciosUnityMus.Preparar())
        {
            ErrorClasificacion = ServiciosUnityMus.Error;
            CargandoClasificacion = false;
            Version++;
            return;
        }

        string yo = ServiciosUnityMus.IdJugador;
        string id = tipo == "semanal" ? ClasificacionSemanal : ClasificacionGlobal;
        List<Puesto> nuevos = new();
        Puesto mio = null;

        try
        {
            if (tipo == "amigos")
            {
                await PrepararAmigos();
                List<string> ids = new() { yo };

                foreach (Amigo a in Amigos)
                    ids.Add(a.Id);

                LeaderboardScoresWithNotFoundPlayerIds r = await Tablas.GetScoresByPlayerIdsAsync(id, ids);
                List<LeaderboardEntry> lista = r.Results ?? new List<LeaderboardEntry>();
                lista.Sort((a, b) => b.Score.CompareTo(a.Score));

                for (int i = 0; i < lista.Count; i++)
                    nuevos.Add(APuesto(lista[i], i + 1, yo));

                mio = nuevos.Find(p => p.Yo);
            }
            else
            {
                LeaderboardScoresPage pagina = await Tablas.GetScoresAsync(id, new GetScoresOptions { Limit = 10 });

                foreach (LeaderboardEntry e in pagina.Results ?? new List<LeaderboardEntry>())
                    nuevos.Add(APuesto(e, e.Rank + 1, yo));

                try
                {
                    LeaderboardEntry propio = await Tablas.GetPlayerScoreAsync(id);
                    mio = APuesto(propio, propio.Rank + 1, yo);
                }
                catch (Exception)
                {
                    mio = null;   // aun no tienes puntos en esta clasificacion
                }
            }

            ErrorClasificacion = "";
        }
        catch (Exception e)
        {
            ErrorClasificacion = "No se pudo cargar la clasificacion (¿esta activado Leaderboards y creada '" + id + "' en Unity Cloud?): " + e.Message;
            Debug.LogWarning("[CLASIFICACION] " + e);
        }

        Clasificacion.Clear();
        Clasificacion.AddRange(nuevos);
        MiPuesto = mio;
        TipoCargado = tipo;
        CargandoClasificacion = false;
        Version++;
    }

    private static Puesto APuesto(LeaderboardEntry e, int rango, string yo) => new()
    {
        Rango = rango,
        Id = e.PlayerId,
        Nombre = string.IsNullOrEmpty(e.PlayerName) ? "Jugador" : ServiciosUnityMus.SinCodigo(e.PlayerName),
        Puntos = e.Score,
        Yo = e.PlayerId == yo
    };
}
#endif
