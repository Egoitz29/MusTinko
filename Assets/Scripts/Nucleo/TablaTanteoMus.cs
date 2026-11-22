using System.Collections.Generic;

public static class TablaTanteoMus
{
    public class Fila
    {
        public string Lance;
        public int Ganador = -1;   // -1 nadie, 0 nosotros, 1 ellos (quien gana el lance con las cartas)
        public readonly List<string> Nosotros = new();
        public readonly List<string> Ellos = new();
    }

    public class Tabla
    {
        public readonly Fila[] Filas = new Fila[4];
        public int TotalNosotros;
        public int TotalEllos;
        public int CobradoAntesNosotros;
        public int CobradoAntesEllos;
        public string Ordago;
    }

    public static string NombreLance(int lance, bool hayJuego)
    {
        switch (lance)
        {
            case 0: return "GRANDE";
            case 1: return "CHICA";
            case 2: return "PARES";
            default: return hayJuego ? "JUEGO" : "PUNTO";
        }
    }

    public static bool HayJuego(int[] cartas16)
    {
        if (cartas16 == null || cartas16.Length < 16)
            return true;

        for (int p = 0; p < 4; p++)
            if (CartasMus.Evaluar(Mano(cartas16, p)).TieneJuego)
                return true;

        return false;
    }

    public static string TextoConcepto(ConceptoTanteoMus c, int[] cartas16, string[] nombres)
    {
        string quien = c.Jugador >= 0 && c.Jugador < nombres.Length ? nombres[c.Jugador] : "";

        switch (c.Tipo)
        {
            case TipoConceptoMus.EnPaso:
                return "En paso +" + c.Tantos;
            case TipoConceptoMus.Querido:
                return "Envite querido +" + c.Tantos;
            case TipoConceptoMus.NoQuerido:
                return "No querido +" + c.Tantos + " (ya sumado)";
            case TipoConceptoMus.Pares:
                return NombrePares(cartas16, c.Jugador) + " de " + quien + " +" + c.Tantos;
            case TipoConceptoMus.Juego:
                return "Juego " + PuntosJuego(cartas16, c.Jugador) + " de " + quien + " +" + c.Tantos;
            case TipoConceptoMus.Punto:
                return "Punto +" + c.Tantos;
            case TipoConceptoMus.Ordago:
                return "ORDAGO ganado por " + quien;
            default:
                return c.Tipo + " +" + c.Tantos;
        }
    }

    public static Tabla Construir(IReadOnlyList<ConceptoTanteoMus> conceptos, int[] cartas16, int parejaLocal, string[] nombres)
    {
        Tabla tabla = new();
        bool hayJuego = HayJuego(cartas16);

        for (int i = 0; i < 4; i++)
            tabla.Filas[i] = new Fila { Lance = NombreLance(i, hayJuego) };

        foreach (ConceptoTanteoMus c in conceptos)
        {
            bool nuestro = c.Pareja == parejaLocal;
            string texto = TextoConcepto(c, cartas16, nombres);
            Fila fila = tabla.Filas[c.Lance < 0 || c.Lance > 3 ? 3 : c.Lance];

            if (nuestro) fila.Nosotros.Add(texto);
            else fila.Ellos.Add(texto);

            // El "no quiero" se cobra aunque no se gane con las cartas: no cuenta como ganador del lance.
            if (c.Tipo != TipoConceptoMus.NoQuerido)
                fila.Ganador = nuestro ? 0 : 1;

            if (c.Tipo == TipoConceptoMus.Ordago)
                tabla.Ordago = texto;

            if (nuestro)
            {
                tabla.TotalNosotros += c.Tantos;
                if (c.CobradoAlMomento) tabla.CobradoAntesNosotros += c.Tantos;
            }
            else
            {
                tabla.TotalEllos += c.Tantos;
                if (c.CobradoAlMomento) tabla.CobradoAntesEllos += c.Tantos;
            }
        }

        return tabla;
    }

    private static int[] Mano(int[] cartas16, int jugador)
    {
        return new[]
        {
            cartas16[jugador * 4], cartas16[jugador * 4 + 1],
            cartas16[jugador * 4 + 2], cartas16[jugador * 4 + 3]
        };
    }

    private static string NombrePares(int[] cartas16, int jugador)
    {
        if (cartas16 == null || cartas16.Length < 16 || jugador < 0 || jugador > 3)
            return "Pares";

        switch (CartasMus.Evaluar(Mano(cartas16, jugador)).Pares)
        {
            case EvaluadorManoMus.TipoPares.Duples: return "Duples";
            case EvaluadorManoMus.TipoPares.Medias: return "Medias";
            default: return "Par";
        }
    }

    private static string PuntosJuego(int[] cartas16, int jugador)
    {
        if (cartas16 == null || cartas16.Length < 16 || jugador < 0 || jugador > 3)
            return "";

        return CartasMus.Evaluar(Mano(cartas16, jugador)).PuntosJuego.ToString();
    }
}
