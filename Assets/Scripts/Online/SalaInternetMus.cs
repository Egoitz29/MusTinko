#if MUS_TINKO_INTERNET
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class SalaInternetMus : MonoBehaviour
{
    private const int MaxJugadores = 4;

    private ISession sesion;
    private string codigoEscrito = "";
    private string estado = "";
    private bool ocupado;

    public string Codigo => sesion?.Code ?? "";

    private async Task<bool> PrepararServicios()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            return true;
        }
        catch (Exception e)
        {
            estado = "No se pudo conectar con Unity Services. Revisa Internet y que el proyecto este vinculado.";
            Debug.LogException(e);
            return false;
        }
    }

    public async void CrearSala()
    {
        if (ocupado)
            return;

        ocupado = true;
        estado = "Creando sala...";

        if (await PrepararServicios())
        {
            try
            {
                SessionOptions opciones = new SessionOptions { MaxPlayers = MaxJugadores }.WithRelayNetwork();
                sesion = await MultiplayerService.Instance.CreateSessionAsync(opciones);
                estado = "Sala creada. Codigo: " + sesion.Code;
                Debug.Log("[MUS INTERNET] Sala creada con codigo " + sesion.Code);
            }
            catch (Exception e)
            {
                estado = "No se pudo crear la sala: " + e.Message;
                Debug.LogException(e);
            }
        }

        ocupado = false;
    }

    public async void UnirseConCodigo(string codigo)
    {
        if (ocupado)
            return;

        codigo = (codigo ?? "").Trim().ToUpperInvariant();

        if (codigo.Length == 0)
        {
            estado = "Escribe el codigo de la sala.";
            return;
        }

        ocupado = true;
        estado = "Entrando en la sala " + codigo + "...";

        if (await PrepararServicios())
        {
            try
            {
                sesion = await MultiplayerService.Instance.JoinSessionByCodeAsync(codigo);
                estado = "Dentro de la sala " + sesion.Code;
                Debug.Log("[MUS INTERNET] Unido a la sala " + sesion.Code);
            }
            catch (Exception e)
            {
                estado = "No se pudo entrar: revisa el codigo. (" + e.Message + ")";
                Debug.LogException(e);
            }
        }

        ocupado = false;
    }

    public async void Salir()
    {
        if (sesion == null)
            return;

        try
        {
            await sesion.LeaveAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        sesion = null;
        estado = "";
    }

    private void OnGUI()
    {
        EscalaGUIMus.Aplicar();

        NetworkManager red = NetworkManager.Singleton;
        bool conectado = red != null && red.IsListening;

        if (conectado)
        {
            if (sesion != null && red.IsHost)
            {
                GUI.Box(new Rect(20, EscalaGUIMus.Alto - 80, 420, 34), "CODIGO DE LA SALA:  " + sesion.Code + "   (dalo a tus amigos)");
            }

            return;
        }

        GUILayout.BeginArea(new Rect(20, 360, Mathf.Min(460f, EscalaGUIMus.Ancho - 40f), 230), "JUGAR POR INTERNET", GUI.skin.window);

        GUI.enabled = !ocupado;

        if (GUILayout.Button("CREAR SALA CON CODIGO", GUILayout.Height(50)))
            CrearSala();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Codigo:", GUILayout.Width(70));
        codigoEscrito = GUILayout.TextField(codigoEscrito, 12, GUILayout.Height(40));
        GUILayout.EndHorizontal();

        if (GUILayout.Button("UNIRSE CON CODIGO", GUILayout.Height(50)))
            UnirseConCodigo(codigoEscrito);

        GUI.enabled = true;

        if (!string.IsNullOrEmpty(estado))
            GUILayout.Label(estado);

        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        if (sesion != null)
            _ = sesion.LeaveAsync();
    }
}
#endif
