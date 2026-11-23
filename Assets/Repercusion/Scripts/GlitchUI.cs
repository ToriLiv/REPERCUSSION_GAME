using UnityEngine;

namespace Repercusion
{
    // Barra que salta de lugar al azar dentro de su padre, como un fallo de pantalla.
    // Versión para UI de tu GlitchBar. Va en un objeto con Image.
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class GlitchUI : MonoBehaviour
    {
        public Vector2 anchoRango = new Vector2(120f, 520f);
        public float alto = 6f;
        public Vector2 intervalo = new Vector2(0.06f, 0.45f);
        public Color[] colores =
        {
            new Color(1f, 1f, 1f, 0.7f),
            new Color(1f, 1f, 1f, 0.35f),
            new Color(0.6f, 0.6f, 0.6f, 0.5f)
        };

        private UnityEngine.UI.Image imagen;
        private RectTransform rt;
        private RectTransform area;
        private float siguiente;

        private void Awake()
        {
            imagen = GetComponent<UnityEngine.UI.Image>();
            imagen.raycastTarget = false;
            rt = (RectTransform)transform;
            area = transform.parent as RectTransform;
        }

        private void Update()
        {
            if (Time.unscaledTime < siguiente || area == null) return;
            siguiente = Time.unscaledTime + Random.Range(intervalo.x, intervalo.y);

            Rect r = area.rect;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Random.Range(anchoRango.x, anchoRango.y), alto);
            rt.anchoredPosition = new Vector2(Random.Range(-r.width / 2f, r.width / 2f),
                                              Random.Range(-r.height / 2f, r.height / 2f));
            if (colores != null && colores.Length > 0) imagen.color = colores[Random.Range(0, colores.Length)];
            imagen.enabled = Random.value > 0.3f;
        }
    }
}
