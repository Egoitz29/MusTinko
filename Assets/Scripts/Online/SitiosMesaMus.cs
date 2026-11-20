using UnityEngine;

public static class SitiosMesaMus
{
    public static readonly string[] NombresModelos = { "LILA_Posicion", "MOSTAZA_Posicion", "MENTA_Posicion", "CORAL_Posicion" };

    public static GameObject[] Asignar(Vector3 centro, Vector3 posicionJugador)
    {
        GameObject[] resultado = new GameObject[4];
        GameObject[] modelos = new GameObject[4];

        for (int i = 0; i < 4; i++)
            modelos[i] = GameObject.Find(NombresModelos[i]);

        Vector3 haciaMesa = centro - posicionJugador;
        haciaMesa.y = 0f;

        if (haciaMesa.sqrMagnitude < 0.0001f)
            haciaMesa = Vector3.forward;

        haciaMesa.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, haciaMesa);
        bool[] usado = new bool[4];

        for (int sitio = 0; sitio < 4; sitio++)
        {
            Vector3 direccion = sitio switch
            {
                0 => -haciaMesa,
                1 => derecha,
                2 => haciaMesa,
                _ => -derecha
            };

            int mejor = -1;
            float mejorPuntuacion = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                if (usado[i] || modelos[i] == null)
                    continue;

                Vector3 v = modelos[i].transform.position - centro;
                v.y = 0f;
                float puntuacion = Vector3.Dot(v.normalized, direccion);

                if (puntuacion > mejorPuntuacion)
                {
                    mejorPuntuacion = puntuacion;
                    mejor = i;
                }
            }

            if (mejor >= 0)
            {
                usado[mejor] = true;
                resultado[sitio] = modelos[mejor];
            }
        }

        return resultado;
    }
}
