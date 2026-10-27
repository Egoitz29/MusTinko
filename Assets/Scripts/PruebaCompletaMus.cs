
using UnityEngine;

public class PruebaCompletaMus : MonoBehaviour
{
    [SerializeField] private RepartoPrueba reparto;
    [SerializeField] private MarcadorMus marcador;

    [ContextMenu("Prueba - Tanteo completo conocido")]
    private void EjecutarPrueba()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Primero pulsa Play.");
            return;
        }

        if (reparto == null || marcador == null)
        {
            Debug.LogError("Faltan referencias.");
            return;
        }

        // Para asegurar el resultado esperado,
        // ejecuta la prueba en una sesión nueva de Play.
        if (marcador.PuntosPareja02 != 0 ||
            marcador.PuntosPareja13 != 0)
        {
            Debug.LogWarning(
                "La prueba necesita un marcador inicial 0-0."
            );
            return;
        }

        reparto.PrepararManosFijasPrueba();
        marcador.PrepararNuevaMano();

        // GRANDE: nuestra pareja envida 2.
        // Los rivales aceptan.
        MotorEnvitesMus grande = new MotorEnvitesMus();

        grande.Envidar(0, 2);
        grande.Quiero(1);

        marcador.RegistrarLance(
            GestorFasesMus.Fase.Grande, grande);

        // CHICA: los rivales envidan.
        // Nosotros rechazamos.
        MotorEnvitesMus chica = new MotorEnvitesMus();

        chica.Envidar(1, 2);
        chica.NoQuiero(0);

        marcador.RegistrarLance(
            GestorFasesMus.Fase.Chica, chica);

        // PARES: todos pasan.
        MotorEnvitesMus pares = new MotorEnvitesMus();

        marcador.RegistrarLance(
            GestorFasesMus.Fase.Pares, pares);

        // JUEGO: envidamos 2,
        // los rivales reenvidan 2 más
        // y nosotros aceptamos.
        MotorEnvitesMus juego = new MotorEnvitesMus();

        juego.Envidar(0, 2);
        juego.Reenvidar(1, 2);
        juego.Quiero(0);

        marcador.RegistrarLance(
            GestorFasesMus.Fase.JuegoPunto, juego);

        // Resolver y cobrar una sola vez.
        marcador.ContabilizarManoConEnvites();

        Debug.Log(
            "RESULTADO OBTENIDO: " +
            marcador.PuntosPareja02 + " - " +
            marcador.PuntosPareja13
        );

        if (marcador.PuntosPareja02 == 14 &&
            marcador.PuntosPareja13 == 1)
        {
            Debug.Log("PRUEBA COMPLETA SUPERADA.");
        }
        else
        {
            Debug.LogError(
                "PRUEBA FALLIDA. Esperado: 14 - 1."
            );
        }
    }
}
