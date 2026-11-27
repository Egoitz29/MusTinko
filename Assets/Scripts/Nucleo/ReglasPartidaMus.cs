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

    // Regla callejera: quien tenga un 4, un 5, un 6 y un 7 (de cualquier palo) puede cantar
    // "pedrete" en su turno de mus: cobra 1 tanto al momento y cambia sus cuatro cartas.
    // Es voluntario: si no lo canta, se queda con esas cartas.
    public bool Pedrete;

    public ReglasPartidaMus Copia() => (ReglasPartidaMus)MemberwiseClone();

    public override string ToString()
    {
        return Limite + " tantos" + (LaReal ? ", la Real" : "") + (Pedrete ? ", pedrete" : "");
    }
}
