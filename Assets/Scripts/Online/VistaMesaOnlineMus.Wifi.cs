using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// BUSCAR PARTIDAS EN MI WIFI (menu principal)
// Un movil crea la partida y el otro la encuentra con un boton, sin escribir la IP.
// El segundo jugador se sienta enfrente del host: sois pareja y los bots juegan en las otras dos sillas.
public partial class VistaMesaOnlineMus
{
    private Button botonBuscarLan;
    private TextMeshProUGUI txtBuscarLan, txtInfoLan;
    private readonly Button[] botonesLan = new Button[2];
    private readonly TextMeshProUGUI[] txtBotonesLan = new TextMeshProUGUI[2];
    private readonly string[] ipsLan = new string[2];
    private float siguienteRefrescoLan;

    private void CrearBusquedaLan(Transform caja, float y)
    {
        botonBuscarLan = Boton(caja, "azul", "BUSCAR PARTIDAS EN MI WIFI", new Vector2(560, 70), BuscarPartidasLan, 26);
        Rect(botonBuscarLan.gameObject, caja, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(560, 70));
        txtBuscarLan = botonBuscarLan.GetComponentInChildren<TextMeshProUGUI>();

        y -= 80;
        GameObject info = new("InfoLan", typeof(RectTransform));
        Rect(info, caja, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(640, 110));
        txtInfoLan = Texto(info.transform, "", 22, Crema);

        for (int i = 0; i < botonesLan.Length; i++)
        {
            int indice = i;
            botonesLan[i] = Boton(caja, "verde", "", new Vector2(600, 56), () => UnirseLan(indice), 24);
            Rect(botonesLan[i].gameObject, caja, new Vector2(0.5f, 1f), new Vector2(0, y - i * 62), new Vector2(600, 56));
            txtBotonesLan[i] = botonesLan[i].GetComponentInChildren<TextMeshProUGUI>();
            botonesLan[i].gameObject.SetActive(false);
        }
    }

    private void BuscarPartidasLan()
    {
        conexion?.RefrescarIp();
        BuscadorLanMus.Buscar();
        siguienteRefrescoLan = 0f;
    }

    private void UnirseLan(int indice)
    {
        string ip = ipsLan[indice];

        if (string.IsNullOrEmpty(ip))
            return;

        if (campoIpUI != null)
            campoIpUI.text = ip;

        GuardarNombre(campoNombreUI.text);
        conexion?.Unirse(ip);
    }

    private void ActualizarBusquedaLan()
    {
        if (botonBuscarLan == null || Time.unscaledTime < siguienteRefrescoLan)
            return;

        siguienteRefrescoLan = Time.unscaledTime + 0.2f;
        bool buscando = BuscadorLanMus.Buscando;
        botonBuscarLan.interactable = !buscando;
        txtBuscarLan.text = buscando ? "BUSCANDO..." : "BUSCAR PARTIDAS EN MI WIFI";

        List<BuscadorLanMus.PartidaLan> lista = BuscadorLanMus.Busquedas > 0 ? BuscadorLanMus.Encontradas() : new List<BuscadorLanMus.PartidaLan>();

        for (int i = 0; i < botonesLan.Length; i++)
        {
            bool hay = i < lista.Count;
            botonesLan[i].gameObject.SetActive(hay);
            ipsLan[i] = hay ? lista[i].Ip : null;

            if (!hay)
                continue;

            BuscadorLanMus.PartidaLan p = lista[i];
            string sillas = p.EnCurso ? "ya empezada" : p.Jugadores + (p.Jugadores == 1 ? " jugador" : " jugadores");
            txtBotonesLan[i].text = "UNIRSE A " + p.Nombre.ToUpperInvariant() + "  <size=70%>(" + p.Ip + ", " + sillas + ")</size>";
        }

        if (lista.Count > 0)
            txtInfoLan.text = "";
        else if (buscando)
            txtInfoLan.text = "Buscando partidas en tu WiFi...";
        else if (BuscadorLanMus.Busquedas > 0)
            txtInfoLan.text = "<color=#FFB080>No se ha encontrado ninguna partida.</color>\n<size=85%>Mira que el otro movil haya pulsado CREAR PARTIDA y que esteis en la misma WiFi " +
                              "(o conectate a su zona WiFi). Tambien puedes escribir su IP arriba.</size>";
        else
            txtInfoLan.text = "<size=90%>Para jugar en pareja con otro movil: el pulsa CREAR PARTIDA y tu BUSCAR.</size>";
    }
}
