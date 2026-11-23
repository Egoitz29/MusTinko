#if !MUS_TINKO_SIN_INTERNET
using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

// Conexion con los servicios de Unity (Lobby/Relay, Friends, Leaderboards).
// Se prepara una sola vez: se inicia Unity Services y se entra como jugador anonimo.
// Cada jugador tiene un nombre de Unity tipo "Egoitz#4821": ese es su CODIGO DE AMIGO.
public static class ServiciosUnityMus
{
    private static Task<bool> preparando;
    private static Task nombrePendiente;

    public static string Error { get; private set; } = "";

    // En el editor, AuthenticationService.Instance se vacia al cargar la escena: si pasa, se pide al registro.
    private static IAuthenticationService Auth
    {
        get
        {
            try
            {
                return AuthenticationService.Instance;
            }
            catch (Exception)
            {
            }

            try
            {
                return UnityServices.Instance?.GetService<IAuthenticationService>();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public static bool Listo => UnityServices.State == ServicesInitializationState.Initialized && Auth != null && Auth.IsSignedIn;

    public static string IdJugador => Listo ? Auth.PlayerId : "";

    // "Egoitz#4821" (o vacio si aun no hay).
    public static string CodigoAmigo => Listo ? Auth.PlayerName ?? "" : "";

    // Una sola preparacion a la vez (varias pantallas la piden a la vez).
    public static Task<bool> Preparar()
    {
        if (preparando == null || (preparando.IsCompleted && !preparando.Result))
            preparando = PrepararAhora();

        return preparando;
    }

    private static async Task<bool> PrepararAhora()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                InitializationOptions opciones = new();
#if UNITY_EDITOR
                // Cada ventana del editor (tambien las de Multiplayer Play Mode, que son copias del proyecto)
                // es un jugador distinto, pero siempre el mismo: asi tu codigo de amigo no cambia al darle a Play.
                opciones.SetProfile("editor" + Huella(Application.dataPath));
#endif
                await UnityServices.InitializeAsync(opciones);
            }

            if (!Auth.IsSignedIn)
                await Auth.SignInAnonymouslyAsync();

            // El nombre de Unity (tu codigo de amigo): se pide y se pone igual que tu apodo.
            try
            {
                await Auth.GetPlayerNameAsync();
                string apodo = PlayerPrefs.GetString("MusTinko_Nombre", "");

                if (!string.IsNullOrWhiteSpace(apodo))
                    PonerNombre(apodo);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SERVICIOS] No se pudo leer el nombre de jugador: " + e.Message);
            }

            Error = "";
            return true;
        }
        catch (Exception e)
        {
            Error = "No se pudo conectar con los servidores de Unity: " + e.Message;
            Debug.LogException(e);
            return false;
        }
    }

    // El nombre de Unity (el de la clasificacion y el codigo de amigo) sigue a tu apodo del juego.
    public static void PonerNombre(string apodo)
    {
        string limpio = Limpiar(apodo);

        if (limpio.Length == 0 || !Listo)
            return;

        string actual = Auth.PlayerName ?? "";
        int almohadilla = actual.LastIndexOf('#');
        string sinCodigo = almohadilla >= 0 ? actual.Substring(0, almohadilla) : actual;

        if (sinCodigo == limpio || (nombrePendiente != null && !nombrePendiente.IsCompleted))
            return;

        nombrePendiente = CambiarNombre(limpio);
    }

    private static async Task CambiarNombre(string limpio)
    {
        try
        {
            await Auth.UpdatePlayerNameAsync(limpio);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SERVICIOS] No se pudo cambiar el nombre de jugador: " + e.Message);
        }
    }

    // Numero estable a partir de un texto (no se usa GetHashCode porque cambia entre ejecuciones en algunos sistemas).
    private static uint Huella(string texto)
    {
        uint h = 2166136261;

        foreach (char c in texto ?? "")
            h = (h ^ c) * 16777619;

        return h % 1000000;
    }

    // Unity no deja espacios ni simbolos raros en el nombre de jugador.
    private static string Limpiar(string apodo)
    {
        StringBuilder sb = new();

        foreach (char c in (apodo ?? "").Trim())
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')
                sb.Append(c);
            else if (c == ' ')
                sb.Append('_');

            if (sb.Length >= 20)
                break;
        }

        return sb.ToString();
    }

    // "Egoitz#4821" -> "Egoitz"
    public static string SinCodigo(string nombreUnity)
    {
        if (string.IsNullOrEmpty(nombreUnity))
            return "";

        int almohadilla = nombreUnity.LastIndexOf('#');
        return (almohadilla >= 0 ? nombreUnity.Substring(0, almohadilla) : nombreUnity).Replace('_', ' ');
    }
}
#endif
