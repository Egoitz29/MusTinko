using UnityEngine;

public static class EscalaGUIMus
{
    public static float Factor { get; private set; } = 1f;
    public static float Ancho => Screen.width / Factor;
    public static float Alto => Screen.height / Factor;
    public static bool EsMovil => Application.isMobilePlatform;

    public static void Aplicar()
    {
        float altoBase = EsMovil ? 720f : 900f;
        float anchoBase = EsMovil ? 1280f : 1600f;
        Factor = Mathf.Max(0.5f, Mathf.Min(Screen.width / anchoBase, Screen.height / altoBase));
        GUI.matrix = Matrix4x4.Scale(new Vector3(Factor, Factor, 1f));
    }
}
