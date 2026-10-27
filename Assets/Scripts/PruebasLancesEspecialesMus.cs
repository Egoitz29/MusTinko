
using System;
using System.Collections.Generic;
using UnityEngine;

public class PruebasLancesEspecialesMus : MonoBehaviour
{
    [ContextMenu("Prueba - Seis casos Pares y Juego")]
    private void EjecutarPruebas()
    {
        bool correcto = true;

        // 1. Nadie tiene pares.
        correcto &= Probar(
            "Nadie tiene Pares",
            GestorFasesMus.Fase.Pares,
            false, false,
            "AS-OROS|4-OROS|5-OROS|6-OROS",
            "REY-COPAS|7-COPAS|SOTA-COPAS|AS-COPAS",
            "CABALLO-ESPADAS|6-ESPADAS|AS-ESPADAS|5-ESPADAS",
            "3-BASTOS|2-BASTOS|4-BASTOS|7-BASTOS"
        );

        // 2. Solo nuestra pareja tiene pares.
        correcto &= Probar(
            "Solo una pareja tiene Pares",
            GestorFasesMus.Fase.Pares,
            false, false,
            "REY-OROS|REY-COPAS|4-OROS|5-OROS",
            "AS-ESPADAS|4-COPAS|6-COPAS|7-COPAS",
            "2-BASTOS|5-ESPADAS|6-ESPADAS|7-ESPADAS",
            "3-BASTOS|5-BASTOS|6-BASTOS|4-BASTOS"
        );

        // 3. Ambas parejas tienen pares.
        correcto &= Probar(
            "Ambas parejas tienen Pares",
            GestorFasesMus.Fase.Pares,
            true, false,
            "REY-OROS|REY-COPAS|5-OROS|6-OROS",
            "AS-ESPADAS|2-BASTOS|7-COPAS|SOTA-ESPADAS",
            "4-OROS|6-COPAS|7-ESPADAS|SOTA-COPAS",
            "3-BASTOS|CABALLO-OROS|4-ESPADAS|5-BASTOS"
        );

        // 4. Nadie tiene Juego: se juega Punto.
        correcto &= Probar(
            "Nadie tiene Juego: Punto",
            GestorFasesMus.Fase.JuegoPunto,
            true, true,
            "AS-OROS|4-OROS|5-OROS|6-OROS",
            "AS-COPAS|4-COPAS|5-COPAS|7-COPAS",
            "2-ESPADAS|4-ESPADAS|5-ESPADAS|7-ESPADAS",
            "2-BASTOS|4-BASTOS|6-BASTOS|7-BASTOS"
        );

        // 5. Solo nuestra pareja tiene Juego.
        correcto &= Probar(
            "Solo una pareja tiene Juego",
            GestorFasesMus.Fase.JuegoPunto,
            false, false,
            "REY-OROS|SOTA-COPAS|CABALLO-ESPADAS|AS-BASTOS",
            "AS-OROS|4-COPAS|5-COPAS|6-COPAS",
            "2-OROS|4-OROS|6-OROS|7-OROS",
            "2-ESPADAS|4-ESPADAS|5-ESPADAS|7-ESPADAS"
        );

        // 6. Ambas parejas tienen Juego.
        correcto &= Probar(
            "Ambas parejas tienen Juego",
            GestorFasesMus.Fase.JuegoPunto,
            true, false,
            "REY-OROS|SOTA-COPAS|CABALLO-ESPADAS|AS-BASTOS",
            "3-OROS|SOTA-ESPADAS|REY-COPAS|5-BASTOS",
            "4-OROS|6-OROS|7-OROS|AS-COPAS",
            "AS-ESPADAS|4-COPAS|6-BASTOS|7-BASTOS"
        );

        if (correcto)
            Debug.Log("LAS 6 PRUEBAS HAN SIDO SUPERADAS.");
        else
            Debug.LogError("HAY ERRORES EN LAS REGLAS.");
    }

    private bool Probar(
        string nombre,
        GestorFasesMus.Fase fase,
        bool esperadoEnvites,
        bool esperadoPunto,
        string jugador0,
        string jugador1,
        string jugador2,
        string jugador3)
    {
        try
        {
            string[] definiciones =
            {
                jugador0, jugador1, jugador2, jugador3
            };

            var resultados =
                new EvaluadorManoMus.Resultado[4];

            HashSet<string> cartasUsadas =
                new HashSet<string>();

            for (int jugador = 0; jugador < 4; jugador++)
            {
                string[] valores =
                    definiciones[jugador].Split('|');

                if (valores.Length != 4)
                    throw new Exception(
                        "Cada jugador necesita 4 cartas."
                    );

                string[] mano = new string[4];

                for (int i = 0; i < 4; i++)
                {
                    mano[i] = valores[i].Replace('-', '\n');

                    if (!cartasUsadas.Add(mano[i]))
                        throw new Exception(
                            "Carta repetida: " + mano[i]
                        );
                }

                resultados[jugador] =
                    EvaluadorManoMus.Evaluar(mano);
            }

            var decision =
                ReglasLancesEspecialesMus.Evaluar(
                    resultados, fase
                );

            bool supera =
                decision.PermiteEnvites == esperadoEnvites &&
                decision.EsPunto == esperadoPunto;

            if (supera)
                Debug.Log("CORRECTO: " + nombre);
            else
                Debug.LogError(
                    "FALLO: " + nombre +
                    " | " + decision.Mensaje
                );

            return supera;
        }
        catch (Exception error)
        {
            Debug.LogError(
                "ERROR en " + nombre + ": " + error.Message
            );
            return false;
        }
    }
}
