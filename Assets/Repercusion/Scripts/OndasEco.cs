using UnityEngine;

namespace Repercusion
{
    // Anillos que nacen en el centro de los círculos y se expanden despacio, como ecos.
    public class OndasEco : MonoBehaviour
    {
        public UnityEngine.UI.Image[] anillos;
        [Tooltip("Segundos que tarda cada onda en llegar a su tamaño máximo")]
        public float duracion = 7f;
        public float tamanoInicial = 30f;
        public float tamanoMaximo = 1500f;
        [Range(0f, 1f)] public float alfaMaxima = 0.3f;

        private void Update()
        {
            if (anillos == null || anillos.Length == 0) return;
            for (int i = 0; i < anillos.Length; i++)
            {
                if (anillos[i] == null) continue;
                // Cada onda va desfasada para que nunca salgan todas juntas
                float f = Mathf.Repeat(Time.unscaledTime / duracion + (float)i / anillos.Length, 1f);
                float lado = Mathf.Lerp(tamanoInicial, tamanoMaximo, f);
                anillos[i].rectTransform.sizeDelta = new Vector2(lado, lado);

                float aparecer = Mathf.Clamp01(f * 8f);   // entra suave
                Color c = anillos[i].color;
                c.a = alfaMaxima * aparecer * (1f - f);    // se desvanece al crecer
                anillos[i].color = c;
            }
        }
    }
}
