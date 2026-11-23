using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// SALA (antes de empezar) y RED SOCIAL
//  - SALIR de la sala (si eres el que la creo, se cierra para todos).
//  - INVITAR (salas por Internet): lista de amigos conectados para mandarles una invitacion a tu mesa.
//  - Tus amigos ven donde estas: "En el menu", "En su sala 2/4" (con boton UNIRSE) o "Jugando".
public partial class VistaMesaOnlineMus
{
    private void PintarPestana(Button b, bool activa)
    {
        string color = activa ? "naranja" : "gris";
        Image img = b.GetComponent<Image>();
        img.sprite = SpriteUI("boton_" + color, 22);
        b.spriteState = new SpriteState { pressedSprite = SpriteUI("boton_" + color + "_pulsado", 22) };
    }

    // Boton SALIR de la sala (antes de empezar la partida) y SALIR AL MENU al acabar.
    private void SalirDeLaSala()
    {
#if !MUS_TINKO_SIN_INTERNET
        if (SalaInternet() != null && SalaInternet().EnSala)
        {
            SalaInternet().Salir();
            return;
        }
#endif
        conexion?.Salir();
    }

    private bool EnSalaPorInternet
    {
        get
        {
#if !MUS_TINKO_SIN_INTERNET
            return SalaInternet() != null && SalaInternet().EnSala;
#else
            return false;
#endif
        }
    }

    // ------------------------------------------------------------------ invitar desde la sala

    private Button botonInvitarSala;
    private GameObject panelInvitarSala;
    private FilaAmigo[] filasInvitar;
    private TextMeshProUGUI txtInfoInvitar;

    private void CrearInvitarSala()
    {
        botonInvitarSala = Boton(panelSalaUI.transform, "morado", "INVITAR", new Vector2(190, 58), () =>
        {
            panelInvitarSala.SetActive(!panelInvitarSala.activeSelf);
#if !MUS_TINKO_SIN_INTERNET
            _ = SocialMus.Instancia.PrepararAmigos();
#endif
        }, 26);
        Rect(botonInvitarSala.gameObject, panelSalaUI.transform, new Vector2(1f, 1f), new Vector2(-22, -22), new Vector2(190, 58));

        panelInvitarSala = Panel("InvitarAmigos", panelSalaUI.transform, "panel_madera_oscura", new Vector2(1f, 1f), new Vector2(-22, -92), new Vector2(540, 650));
        Letra(panelInvitarSala.transform, 26, 18, 420, 54, "Invitar amigos", 44, Oro, true);
        BotonEn(panelInvitarSala.transform, 540 - 26 - 60, 18, 60, 56, "rojo", "X", () => panelInvitarSala.SetActive(false), 26);
        filasInvitar = new FilaAmigo[5];

        for (int i = 0; i < filasInvitar.Length; i++)
            filasInvitar[i] = CrearFilaAmigo(panelInvitarSala.transform, 22, 96 + i * 106, 496, 70, 24, 150);

        txtInfoInvitar = Letra(panelInvitarSala.transform, 26, 100, 488, 400, "", 24, CremaApagada, false, TextAlignmentOptions.Top);
        panelInvitarSala.SetActive(false);
    }

    private void ActualizarInvitarSala()
    {
        if (botonInvitarSala == null)
            return;

        bool internet = EnSalaPorInternet;
        botonInvitarSala.gameObject.SetActive(internet);

        if (!internet)
        {
            panelInvitarSala.SetActive(false);
            return;
        }

        if (!panelInvitarSala.activeSelf)
            return;

#if !MUS_TINKO_SIN_INTERNET
        SocialMus s = SocialMus.Instancia;
        SalaInternetMus sala = SalaInternet();
        List<SocialMus.Amigo> conectados = s.Amigos.FindAll(a => a.Conectado);

        for (int i = 0; i < filasInvitar.Length; i++)
        {
            FilaAmigo f = filasInvitar[i];
            bool hay = i < conectados.Count;
            f.Raiz.SetActive(hay);

            if (!hay)
                continue;

            SocialMus.Amigo a = conectados[i];
            PintarCara(f.Cara, a.Nombre, false);
            f.Nombre.text = a.Nombre;
            f.Estado.text = s.TextoEstado(a);

            if (a.Actividad != null && a.Actividad.SalaId == sala.IdSala)
            {
                MostrarAccion(f.Accion, f.TxtAccion, "gris", "AQUI");
                f.AlPulsar = null;
            }
            else
            {
                MostrarAccion(f.Accion, f.TxtAccion, "morado", "INVITAR");
                f.AlPulsar = () => s.Invitar(a.Id, sala.IdSala, sala.NombreSala, NombreLocal);
            }
        }

        txtInfoInvitar.text = !s.AmigosListos ? (string.IsNullOrEmpty(s.ErrorAmigos) ? "Cargando amigos..." : s.ErrorAmigos)
            : conectados.Count == 0 ? "Ningun amigo conectado ahora mismo." + (sala.SalaPrivada ? "\n\nTambien pueden entrar con el codigo:\n<size=150%><b>" + sala.Codigo + "</b></size>" : "")
            : "";
#endif
    }

    // ------------------------------------------------------------------ estado para tus amigos

    private float siguienteRedSocial;

    // Cada poco: lo que ven tus amigos de ti, invitaciones pendientes al crear sala.
    private void ActualizarRedSocial()
    {
#if !MUS_TINKO_SIN_INTERNET
        if (Time.unscaledTime < siguienteRedSocial)
            return;

        siguienteRedSocial = Time.unscaledTime + 1f;
        SocialMus s = SocialMus.Instancia;
        SalaInternetMus sala = SalaInternet();

        if (sala != null && sala.EnSala && s.HayInvitacionesPendientes && !string.IsNullOrEmpty(sala.IdSala))
            s.EnviarInvitacionesPendientes(sala.IdSala, sala.NombreSala, NombreLocal);

        if (!s.AmigosListos)
            return;

        bool jugando = partida != null && partida.IsSpawned && partida.PartidaEmpezada;

        if (sala != null && sala.EnSala)
        {
            if (jugando)
                s.ActualizarPresencia("Jugando en " + sala.NombreSala, "", "", 4);
            else
                s.ActualizarPresencia("En su sala · " + sala.JugadoresEnSala + "/" + SalaInternetMus.MaxJugadores, sala.IdSala, sala.NombreSala, sala.JugadoresEnSala);
        }
        else if (jugando)
        {
            s.ActualizarPresencia("Jugando", "", "", 4);
        }
        else
        {
            s.ActualizarPresencia("En el menu", "", "", 0);
        }
#endif
    }
}
