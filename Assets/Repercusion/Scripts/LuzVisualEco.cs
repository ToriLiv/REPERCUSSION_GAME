using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Repercusion
{
    // La luz que Eco lleva encima. Se achica cuando la luz (LuzEco) baja
    // y parpadea cuando está por apagarse. Va en un hijo de Eco con una Light 2D.
    [RequireComponent(typeof(Light2D))]
    public class LuzVisualEco : MonoBehaviour
    {
        public float radioMinimo = 1.8f;
        public float radioMaximo = 7f;
        public float intensidad = 1f;
        [Range(0f, 1f)] public float umbralParpadeo = 0.25f;

        private Light2D luz2D;
        private LuzEco luzEco;
        private float mostrado = 1f;

        private void Awake()
        {
            luz2D = GetComponent<Light2D>();
            luzEco = GetComponentInParent<LuzEco>();
        }

        private void Update()
        {
            float objetivo = luzEco != null ? luzEco.LuzNormalizada : 1f;
            mostrado = Mathf.Lerp(mostrado, objetivo, Time.deltaTime * 4f);

            bool baja = mostrado < umbralParpadeo;
            float ruido = Mathf.PerlinNoise(Time.time * (baja ? 9f : 2f), 0.3f) - 0.5f;
            float parpadeo = ruido * (baja ? 0.6f : 0.08f);

            float radio = Mathf.Lerp(radioMinimo, radioMaximo, mostrado) * (1f + parpadeo);
            luz2D.pointLightOuterRadius = Mathf.Max(0.5f, radio);
            luz2D.pointLightInnerRadius = luz2D.pointLightOuterRadius * 0.3f;
            luz2D.intensity = intensidad * (0.55f + 0.45f * mostrado) * (1f + parpadeo);
        }
    }
}
