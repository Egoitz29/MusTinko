using System;
using System.Collections.Generic;
using UnityEngine;

// PERFIL DEL JUGADOR (guardado en este aparato, como las monedas)
//  - Nivel y experiencia: +20 por partida, +30 por ganar, +10 si se gana con ordago.
//  - Estadisticas: partidas, victorias, ordagos, racha, lances ganados, monedas ganadas.
//  - Logros (8), pareja favorita, ultimas partidas.
//  - Reto de la semana (ganar 5 partidas) y puntos para la clasificacion:
//    3 por victoria y 1 por partida, solo en partidas con al menos 2 personas (contra bots no cuenta).
public static class PerfilMus
{
    private const string Clave = "MusTinko_Perfil";
    public const int RetoVictorias = 5;
    public const int PremioReto = 300;

    [Serializable]
    public class PartidaGuardada
    {
        public bool Ganada;
        public bool Ordago;
        public string Con = "";
        public string Contra = "";
        public int Nos;
        public int Ellos;
        public long Fecha;
    }

    [Serializable]
    public class Pareja
    {
        public string Nombre = "";
        public int Partidas;
        public int Ganadas;
    }

    [Serializable]
    private class Datos
    {
        public int Partidas, Ganadas, OrdagosGanados, Racha, MejorRacha, Xp, Senas, MonedasGanadas, Remontadas, PartidasLlenas;
        public int[] Lances = new int[5];
        public int Semana = -1, GanadasSemana, PuntosSemana, PuntosTotal;
        public bool RetoReclamado, Tuvo31, TuvoDuplesReyes;
        public List<PartidaGuardada> Ultimas = new();
        public List<Pareja> Parejas = new();
    }

    public class Logro
    {
        public string Nombre;
        public string Detalle;
        public bool Hecho;
        public string Progreso;
    }

    public static event Action Cambio;

    private static Datos datos;

    private static Datos D
    {
        get
        {
            if (datos != null)
                return datos;

            try
            {
                string json = PlayerPrefs.GetString(Clave, "");
                datos = string.IsNullOrEmpty(json) ? new Datos() : JsonUtility.FromJson<Datos>(json) ?? new Datos();
            }
            catch (Exception)
            {
                datos = new Datos();
            }

            if (datos.Lances == null || datos.Lances.Length != 5) datos.Lances = new int[5];
            datos.Ultimas ??= new List<PartidaGuardada>();
            datos.Parejas ??= new List<Pareja>();
            ComprobarSemana(datos);
            return datos;
        }
    }

    private static void Guardar()
    {
        try
        {
            PlayerPrefs.SetString(Clave, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
        }
        catch (Exception)
        {
        }

        Cambio?.Invoke();
    }

    // ---------------------------------------------------------------- semana

    public static int SemanaActual => (int)((DateTime.UtcNow.Date - new DateTime(2024, 1, 1)).TotalDays / 7);   // empieza en lunes

    private static void ComprobarSemana(Datos d)
    {
        int semana = SemanaActual;

        if (d.Semana == semana)
            return;

        d.Semana = semana;
        d.GanadasSemana = 0;
        d.PuntosSemana = 0;
        d.RetoReclamado = false;
    }

    public static TimeSpan QuedaSemana
    {
        get
        {
            DateTime inicio = new DateTime(2024, 1, 1).AddDays((SemanaActual + 1) * 7);
            return inicio - DateTime.UtcNow;
        }
    }

    // ---------------------------------------------------------------- lectura

    public static int Partidas => D.Partidas;
    public static int Ganadas => D.Ganadas;
    public static int PorcentajeVictorias => D.Partidas == 0 ? 0 : Mathf.RoundToInt(100f * D.Ganadas / D.Partidas);
    public static int OrdagosGanados => D.OrdagosGanados;
    public static int MejorRacha => D.MejorRacha;
    public static int MonedasGanadas => D.MonedasGanadas;
    public static int GanadasSemana { get { ComprobarSemana(D); return D.GanadasSemana; } }
    public static int PuntosSemana { get { ComprobarSemana(D); return D.PuntosSemana; } }
    public static int PuntosTotal => D.PuntosTotal;
    public static IReadOnlyList<PartidaGuardada> Ultimas => D.Ultimas;

    public static string LanceFavorito
    {
        get
        {
            string[] nombres = { "Grande", "Chica", "Pares", "Juego", "Punto" };
            int mejor = 0;

            for (int i = 1; i < 5; i++)
                if (D.Lances[i] > D.Lances[mejor])
                    mejor = i;

            return D.Lances[mejor] == 0 ? "-" : nombres[mejor];
        }
    }

    // Nivel: cada nivel pide 100 de experiencia mas que el anterior (200, 300, 400...).
    private static int Coste(int nivel) => 100 + 100 * nivel;

    public static int Nivel
    {
        get
        {
            int n = 1, resto = D.Xp;
            while (resto >= Coste(n)) { resto -= Coste(n); n++; }
            return n;
        }
    }

    public static int XpEnNivel
    {
        get
        {
            int n = 1, resto = D.Xp;
            while (resto >= Coste(n)) { resto -= Coste(n); n++; }
            return resto;
        }
    }

    public static int XpParaSubir => Coste(Nivel);

    public static string Titulo =>
        D.OrdagosGanados >= 10 ? "Rey del ordago" :
        D.Ganadas >= 50 ? "Veterano de la taberna" :
        D.Ganadas >= 10 ? "Buena mano" : "Aprendiz de mus";

    public static Pareja ParejaFavorita
    {
        get
        {
            Pareja mejor = null;

            foreach (Pareja p in D.Parejas)
                if (mejor == null || p.Partidas > mejor.Partidas)
                    mejor = p;

            return mejor;
        }
    }

    public static List<Logro> Logros()
    {
        Datos d = D;
        return new List<Logro>
        {
            Nuevo("Primer ordago", "Ganar con un ordago", d.OrdagosGanados >= 1, ""),
            Nuevo("La 31", "Llevar 31 de juego", d.Tuvo31, ""),
            Nuevo("Duples de reyes", "Cuatro reyes en la mano", d.TuvoDuplesReyes, ""),
            Nuevo("Remontada", "Ganar perdiendo de 20", d.Remontadas >= 1, ""),
            Nuevo("10 victorias", "Ganar 10 partidas", d.Ganadas >= 10, Mathf.Min(d.Ganadas, 10) + " de 10"),
            Nuevo("100 victorias", "Ganar 100 partidas", d.Ganadas >= 100, Mathf.Min(d.Ganadas, 100) + " de 100"),
            Nuevo("Senas de maestro", "Hacer 50 senas", d.Senas >= 50, Mathf.Min(d.Senas, 50) + " de 50"),
            Nuevo("Taberna llena", "Jugar con 4 personas", d.PartidasLlenas >= 1, "")
        };
    }

    private static Logro Nuevo(string nombre, string detalle, bool hecho, string progreso) =>
        new() { Nombre = nombre, Detalle = detalle, Hecho = hecho, Progreso = progreso };

    public static int LogrosHechos
    {
        get
        {
            int n = 0;
            foreach (Logro l in Logros()) if (l.Hecho) n++;
            return n;
        }
    }

    // ---------------------------------------------------------------- reto de la semana

    public static bool RetoCumplido => GanadasSemana >= RetoVictorias;
    public static bool RetoReclamado { get { ComprobarSemana(D); return D.RetoReclamado; } }

    public static bool ReclamarReto()
    {
        if (!RetoCumplido || RetoReclamado)
            return false;

        D.RetoReclamado = true;
        MonederoMus.Cobrar(PremioReto);
        Guardar();
        return true;
    }

    // ---------------------------------------------------------------- registrar

    public struct Resumen
    {
        public bool Ganada;
        public bool PorOrdago;
        public int Nos;
        public int Ellos;
        public string Companero;
        public bool CompaneroHumano;
        public string Rivales;
        public int Humanos;
        public int PeorDiferencia;   // lo maximo que se ha ido perdiendo durante la partida
        public int MonedasGanadas;
    }

    public static void RegistrarPartida(Resumen r)
    {
        Datos d = D;
        ComprobarSemana(d);

        d.Partidas++;
        d.Xp += 20;

        if (r.Ganada)
        {
            d.Ganadas++;
            d.GanadasSemana++;
            d.Racha++;
            d.MejorRacha = Mathf.Max(d.MejorRacha, d.Racha);
            d.Xp += 30;

            if (r.PorOrdago)
            {
                d.OrdagosGanados++;
                d.Xp += 10;
            }

            if (r.PeorDiferencia >= 20)
                d.Remontadas++;
        }
        else
        {
            d.Racha = 0;
        }

        d.MonedasGanadas += Mathf.Max(0, r.MonedasGanadas);

        if (r.Humanos >= 4)
            d.PartidasLlenas++;

        // Clasificacion: solo cuenta si habia al menos 2 personas.
        if (r.Humanos >= 2)
        {
            int puntos = 1 + (r.Ganada ? 3 : 0);
            d.PuntosSemana += puntos;
            d.PuntosTotal += puntos;
        }

        if (r.CompaneroHumano && !string.IsNullOrEmpty(r.Companero))
        {
            Pareja p = d.Parejas.Find(x => x.Nombre == r.Companero);

            if (p == null)
            {
                p = new Pareja { Nombre = r.Companero };
                d.Parejas.Add(p);
            }

            p.Partidas++;
            if (r.Ganada) p.Ganadas++;
        }

        d.Ultimas.Insert(0, new PartidaGuardada
        {
            Ganada = r.Ganada,
            Ordago = r.PorOrdago,
            Con = "Con " + (r.CompaneroHumano ? r.Companero : "un bot"),
            Contra = "contra " + r.Rivales,
            Nos = r.Nos,
            Ellos = r.Ellos,
            Fecha = DateTime.UtcNow.Ticks
        });

        while (d.Ultimas.Count > 10)
            d.Ultimas.RemoveAt(d.Ultimas.Count - 1);

        Guardar();
    }

    // Al acabar cada mano: lances que ha ganado tu pareja con las cartas (0 grande ... 4 punto) y tu mano.
    public static void RegistrarMano(bool[] lancesGanados, EvaluadorManoMus.Resultado mia)
    {
        Datos d = D;

        for (int i = 0; i < 5 && lancesGanados != null && i < lancesGanados.Length; i++)
            if (lancesGanados[i])
                d.Lances[i]++;

        if (mia != null)
        {
            if (mia.PuntosJuego == 31)
                d.Tuvo31 = true;

            if (mia.Pares == EvaluadorManoMus.TipoPares.Duples && mia.RangosOrdenados != null && mia.RangosOrdenados.Length == 4 &&
                Array.TrueForAll(mia.RangosOrdenados, r => r == mia.RangosOrdenados[0]) && mia.RangosOrdenados[0] == 8)
                d.TuvoDuplesReyes = true;
        }

        Guardar();
    }

    public static void RegistrarSena()
    {
        D.Senas++;
        Guardar();
    }

    public static string Hace(long ticks)
    {
        if (ticks <= 0)
            return "";

        TimeSpan t = DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);

        if (t.TotalMinutes < 60) return "hace " + Mathf.Max(1, (int)t.TotalMinutes) + " min";
        if (t.TotalHours < 24) return "hace " + (int)t.TotalHours + " h";
        if (t.TotalDays < 2) return "ayer";
        return "hace " + (int)t.TotalDays + " dias";
    }
}
