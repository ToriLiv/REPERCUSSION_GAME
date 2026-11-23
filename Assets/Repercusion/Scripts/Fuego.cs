using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Repercusion
{
    // Anima un brasero: cambia los cuadros de la llama y hace temblar su luz.
    public class Fuego : MonoBehaviour
    {
        public SpriteRenderer render;
        public Sprite[] cuadros;
        public float cuadrosPorSegundo = 8f;
        public Light2D luz;
        public float radioBase = 4.5f;
        public float intensidadBase = 1.1f;

        private float semilla;

        private void Awake()
        {
            semilla = Random.value * 100f;
            if (render == null) render = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (render != null && cuadros != null && cuadros.Length > 0)
            {
                int i = (int)((Time.time + semilla) * cuadrosPorSegundo) % cuadros.Length;
                render.sprite = cuadros[i];
            }
            if (luz != null)
            {
                float r = Mathf.PerlinNoise(Time.time * 3f, semilla) - 0.5f;
                luz.intensity = intensidadBase * (1f + r * 0.25f);
                luz.pointLightOuterRadius = radioBase * (1f + r * 0.08f);
            }
        }
    }
}
