using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// Encuentra partidas de Mus TINKO en la misma WiFi sin tener que escribir la IP.
//
//  - El que crea la partida (host) escucha en el puerto UDP 47777 y contesta "aqui hay partida" a quien pregunte.
//  - El que busca pregunta a toda su red: a la direccion de difusion y una a una a las 254 direcciones de su WiFi
//    (asi funciona tambien en moviles, donde la difusion a veces se pierde). No necesita permisos especiales.
//  - Tambien vale con el zona WiFi (hotspot) de uno de los moviles.
//
// Todo va en hilos aparte: aqui no se toca nada de Unity.
public static class BuscadorLanMus
{
    public const int PuertoDescubrir = 47777;
    private const string Pregunta = "MUSTINKO?1";
    private const string Respuesta = "MUSTINKO!1";

    public struct PartidaLan
    {
        public string Ip;
        public string Nombre;
        public int Jugadores;
        public bool EnCurso;
    }

    // ------------------------------------------------------------------ host: contestar

    private static UdpClient escucha;
    private static volatile bool pararEscucha;
    private static volatile string infoHost = "Partida|1|0";

    public static bool Anunciando => escucha != null;

    public static void PonerInfo(string nombre, int jugadores, bool enCurso)
    {
        nombre = string.IsNullOrWhiteSpace(nombre) ? "Partida" : nombre.Replace("|", " ").Trim();
        infoHost = nombre + "|" + jugadores + "|" + (enCurso ? 1 : 0);
    }

    public static bool EmpezarAnuncio()
    {
        if (escucha != null)
            return true;

        try
        {
            UdpClient u = new(AddressFamily.InterNetwork);
            u.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            u.Client.Bind(new IPEndPoint(IPAddress.Any, PuertoDescubrir));
            u.Client.ReceiveTimeout = 500;
            escucha = u;
            pararEscucha = false;
            new Thread(() => Escuchar(u)) { IsBackground = true, Name = "MusTinko_Anuncio" }.Start();
            return true;
        }
        catch (Exception)
        {
            // Puerto ocupado (por ejemplo, dos hosts en el mismo ordenador): se juega igual, solo que no se anuncia.
            escucha = null;
            return false;
        }
    }

    public static void PararAnuncio()
    {
        pararEscucha = true;

        try
        {
            escucha?.Close();
        }
        catch (Exception)
        {
        }

        escucha = null;
    }

    private static void Escuchar(UdpClient u)
    {
        while (!pararEscucha)
        {
            try
            {
                IPEndPoint quien = new(IPAddress.Any, 0);
                byte[] datos = u.Receive(ref quien);

                if (Encoding.UTF8.GetString(datos) != Pregunta)
                    continue;

                byte[] r = Encoding.UTF8.GetBytes(Respuesta + "|" + infoHost);
                u.Send(r, r.Length, quien);
            }
            catch (SocketException)
            {
                // Tiempo de espera agotado: se sigue escuchando.
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception)
            {
                if (pararEscucha)
                    break;
            }
        }
    }

    // ------------------------------------------------------------------ buscar

    private static readonly object cerrojo = new();
    private static readonly List<PartidaLan> encontradas = new();
    private static volatile bool buscando;

    public static bool Buscando => buscando;
    public static int Busquedas { get; private set; }

    public static List<PartidaLan> Encontradas()
    {
        lock (cerrojo)
            return new List<PartidaLan>(encontradas);
    }

    public static void Buscar(double segundos = 1.8)
    {
        if (buscando)
            return;

        buscando = true;
        Busquedas++;

        lock (cerrojo)
            encontradas.Clear();

        new Thread(() =>
        {
            try
            {
                Preguntar(segundos);
            }
            catch (Exception)
            {
            }
            finally
            {
                buscando = false;
            }
        }) { IsBackground = true, Name = "MusTinko_Buscar" }.Start();
    }

    private static void Preguntar(double segundos)
    {
        using UdpClient u = new(0, AddressFamily.InterNetwork);
        u.EnableBroadcast = true;
        // Sin esperas al enviar: los mensajes a direcciones que no existen se quedan en cola unos segundos
        // y, si se llena, el envio se bloquearia. Mejor perder alguno que quedarse colgado.
        u.Client.Blocking = false;

        try
        {
            u.Client.SendBufferSize = 1 << 20;
        }
        catch (Exception)
        {
        }

        byte[] pregunta = Encoding.UTF8.GetBytes(Pregunta);
        List<IPEndPoint> destinos = Destinos();
        DateTime fin = DateTime.UtcNow.AddSeconds(segundos);
        DateTime segundaRonda = DateTime.UtcNow.AddSeconds(segundos * 0.4);
        bool repetida = false;

        Enviar(u, pregunta, destinos);

        while (DateTime.UtcNow < fin)
        {
            // Se pregunta dos veces por si algun mensaje se pierde.
            if (!repetida && DateTime.UtcNow > segundaRonda)
            {
                repetida = true;
                Enviar(u, pregunta, destinos);
            }

            // Solo se lee cuando ya ha llegado algo: una lectura con espera a veces se queda colgada varios segundos
            // (cuando la red avisa de que muchas de las direcciones preguntadas no existen).
            if (u.Available <= 0)
            {
                Thread.Sleep(15);
                continue;
            }

            try
            {
                IPEndPoint quien = new(IPAddress.Any, 0);
                string texto = Encoding.UTF8.GetString(u.Receive(ref quien));
                Anotar(texto, quien.Address);
            }
            catch (SocketException)
            {
            }
        }
    }

    private static void Enviar(UdpClient u, byte[] datos, List<IPEndPoint> destinos)
    {
        foreach (IPEndPoint d in destinos)
        {
            try
            {
                u.Send(datos, datos.Length, d);
            }
            catch (Exception)
            {
            }
        }
    }

    private static List<IPEndPoint> Destinos()
    {
        List<IPEndPoint> d = new()
        {
            new IPEndPoint(IPAddress.Broadcast, PuertoDescubrir),
            new IPEndPoint(IPAddress.Loopback, PuertoDescubrir)   // otro programa en este mismo ordenador
        };
        HashSet<string> vistas = new();

        foreach (DireccionLocal local in DireccionesLocales())
        {
            byte[] b = local.Ip.GetAddressBytes();

            // Difusion de la red (segun su mascara) y, por si acaso, una a una todas las de la red /24.
            if (local.Mascara != null)
            {
                byte[] m = local.Mascara.GetAddressBytes();
                byte[] difusion = new byte[4];
                for (int i = 0; i < 4; i++) difusion[i] = (byte)(b[i] | ~m[i]);
                d.Add(new IPEndPoint(new IPAddress(difusion), PuertoDescubrir));
            }

            string red = b[0] + "." + b[1] + "." + b[2];

            if (!vistas.Add(red))
                continue;

            for (int i = 1; i < 255; i++)
            {
                if (i == b[3])
                    continue;

                d.Add(new IPEndPoint(new IPAddress(new[] { b[0], b[1], b[2], (byte)i }), PuertoDescubrir));
            }
        }

        return d;
    }

    private static void Anotar(string texto, IPAddress desde)
    {
        string[] p = texto.Split('|');

        if (p.Length < 4 || p[0] != Respuesta)
            return;

        PartidaLan nueva = new()
        {
            Ip = desde.ToString(),
            Nombre = p[1],
            Jugadores = int.TryParse(p[2], out int j) ? j : 1,
            EnCurso = p[3] == "1"
        };

        lock (cerrojo)
        {
            for (int i = 0; i < encontradas.Count; i++)
            {
                if (encontradas[i].Ip == nueva.Ip)
                    return;

                // La misma partida vista por 127.0.0.1 y por la WiFi: mejor la de la WiFi.
                if (encontradas[i].Nombre == nueva.Nombre && encontradas[i].Jugadores == nueva.Jugadores)
                {
                    if (encontradas[i].Ip == "127.0.0.1")
                        encontradas[i] = nueva;
                    return;
                }
            }

            encontradas.Add(nueva);
        }
    }

    // ------------------------------------------------------------------ IPs de este aparato

    public struct DireccionLocal
    {
        public IPAddress Ip;
        public IPAddress Mascara;
        public string Interfaz;
    }

    // Las IPv4 de este aparato, primero las de la WiFi / zona WiFi (redes privadas) y al final los datos moviles.
    public static List<DireccionLocal> DireccionesLocales()
    {
        List<DireccionLocal> r = new();

        try
        {
            foreach (NetworkInterface interfaz in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (interfaz.OperationalStatus != OperationalStatus.Up && interfaz.OperationalStatus != OperationalStatus.Unknown)
                    continue;

                if (interfaz.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (UnicastIPAddressInformation dir in interfaz.GetIPProperties().UnicastAddresses)
                {
                    if (dir.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(dir.Address))
                        continue;

                    IPAddress mascara = null;

                    try
                    {
                        mascara = dir.IPv4Mask;
                    }
                    catch (Exception)
                    {
                    }

                    Agregar(r, dir.Address, mascara, interfaz.Name ?? "");
                }
            }
        }
        catch (Exception)
        {
        }

        // Si lo anterior falla (pasa en algunos Android), la IP con la que se sale a la red.
        try
        {
            using Socket s = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect("8.8.8.8", 65530);

            if (s.LocalEndPoint is IPEndPoint ep && !IPAddress.IsLoopback(ep.Address) && !ep.Address.Equals(IPAddress.Any))
                Agregar(r, ep.Address, null, "ruta");
        }
        catch (Exception)
        {
        }

        r.Sort((a, b) => Prioridad(a).CompareTo(Prioridad(b)));
        return r;
    }

    private static void Agregar(List<DireccionLocal> lista, IPAddress ip, IPAddress mascara, string interfaz)
    {
        foreach (DireccionLocal d in lista)
            if (d.Ip.Equals(ip))
                return;

        lista.Add(new DireccionLocal { Ip = ip, Mascara = mascara, Interfaz = interfaz });
    }

    private static int Prioridad(DireccionLocal d)
    {
        byte[] b = d.Ip.GetAddressBytes();
        bool privada = b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
        string n = d.Interfaz.ToLowerInvariant();
        bool wifi = n.StartsWith("wlan") || n.StartsWith("ap") || n.StartsWith("swlan") || n.Contains("wi-fi") || n.Contains("wifi") ||
                    n.StartsWith("en") || n.StartsWith("eth");
        bool movil = n.StartsWith("rmnet") || n.StartsWith("ccmni") || n.StartsWith("pdp") || n.StartsWith("v4-rmnet");
        return (privada ? 0 : 4) + (wifi ? 0 : 1) + (movil ? 2 : 0);
    }

    // Texto para ensenar al jugador: la IP buena primero y, si hay mas redes privadas, tambien esas.
    public static string TextoIps()
    {
        List<string> ips = new();

        foreach (DireccionLocal d in DireccionesLocales())
        {
            byte[] b = d.Ip.GetAddressBytes();
            bool privada = b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);

            if (privada || ips.Count == 0)
                ips.Add(d.Ip.ToString());
        }

        return string.Join("  /  ", ips);
    }
}
