using System;
using System.Collections.Generic;

public static class CartasMus
{
    public const int TotalCartas = 40;

    public static readonly string[] Valores =
    {
        "AS", "2", "3", "4", "5",
        "6", "7", "SOTA", "CABALLO", "REY"
    };

    public static readonly string[] Palos =
    {
        "OROS", "COPAS", "ESPADAS", "BASTOS"
    };

    public static int Valor(int carta) => carta % 10;

    public static int Palo(int carta) => carta / 10;

    public static string Texto(int carta)
    {
        if (carta < 0 || carta >= TotalCartas)
            return "";

        return Valores[Valor(carta)] + "\n" + Palos[Palo(carta)];
    }

    public static string TextoCorto(int carta)
    {
        if (carta < 0 || carta >= TotalCartas)
            return "?";

        return Valores[Valor(carta)] + "-" + Palos[Palo(carta)];
    }

    public static int Rango(int carta)
    {
        switch (Valor(carta))
        {
            case 0:
            case 1: return 1;
            case 2: return 8;
            case 3: return 2;
            case 4: return 3;
            case 5: return 4;
            case 6: return 5;
            case 7: return 6;
            case 8: return 7;
            default: return 8;
        }
    }

    public static string[] ATexto(IReadOnlyList<int> cartas)
    {
        string[] textos = new string[cartas.Count];

        for (int i = 0; i < cartas.Count; i++)
            textos[i] = Texto(cartas[i]);

        return textos;
    }

    public static EvaluadorManoMus.Resultado Evaluar(IReadOnlyList<int> cartas)
    {
        return EvaluadorManoMus.Evaluar(ATexto(cartas));
    }

    public static int Desde(string valor, string palo)
    {
        int v = Array.IndexOf(Valores, valor.Trim().ToUpperInvariant());
        int p = Array.IndexOf(Palos, palo.Trim().ToUpperInvariant());

        if (v < 0 || p < 0)
            throw new ArgumentException("Carta desconocida: " + valor + " de " + palo);

        return p * 10 + v;
    }
}

public class BarajaMus
{
    private readonly List<int> mazo = new();
    private readonly List<int> descartes = new();
    private readonly Random azar;

    public int CartasEnMazo => mazo.Count;
    public int CartasDescartadas => descartes.Count;

    public BarajaMus(Random azar)
    {
        this.azar = azar ?? new Random();
        Reiniciar();
    }

    public void Reiniciar()
    {
        mazo.Clear();
        descartes.Clear();

        for (int i = 0; i < CartasMus.TotalCartas; i++)
            mazo.Add(i);

        Barajar(mazo);
    }

    public int Sacar()
    {
        if (mazo.Count == 0)
        {
            mazo.AddRange(descartes);
            descartes.Clear();
            Barajar(mazo);
        }

        if (mazo.Count == 0)
            throw new InvalidOperationException("No quedan cartas en la baraja.");

        int ultima = mazo.Count - 1;
        int carta = mazo[ultima];
        mazo.RemoveAt(ultima);
        return carta;
    }

    public void Descartar(int carta)
    {
        descartes.Add(carta);
    }

    // Baraja completa salvo las cartas indicadas (las que ya estan en las manos).
    public void ReiniciarSin(ICollection<int> enManos)
    {
        Reiniciar();
        mazo.RemoveAll(enManos.Contains);
    }

    private void Barajar(List<int> cartas)
    {
        for (int i = cartas.Count - 1; i > 0; i--)
        {
            int j = azar.Next(i + 1);
            (cartas[i], cartas[j]) = (cartas[j], cartas[i]);
        }
    }
}
