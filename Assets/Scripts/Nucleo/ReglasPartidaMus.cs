using System;

// Reglas que se eligen al crear la sala. Los valores por defecto son el Mus de siempre:
// un juego a 40 tantos, sin reglas callejeras.
[Serializable]
public class ReglasPartidaMus
{
    // Tantos para ganar un juego (30 o 40).
    public int Limite = 40;

    // Regla callejera: el 31 de tres sietes y una sota ("la Real") gana a cualquier otro 31.
    public bool LaReal;

    public ReglasPartidaMus Copia() => (ReglasPartidaMus)MemberwiseClone();

    public override string ToString()
    {
        return Limite + " tantos" + (LaReal ? ", la Real" : "");
    }
}
