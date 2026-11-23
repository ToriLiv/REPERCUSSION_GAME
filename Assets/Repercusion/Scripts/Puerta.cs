using UnityEngine;
using UnityEngine.Events;

namespace Repercusion
{
    // Puerta que se abre cuando TODAS sus placas están presionadas.
    // Necesita un Collider2D normal (no trigger) que bloquea el paso.
    public class Puerta : MonoBehaviour
    {
        [Tooltip("Todas estas placas deben estar presionadas para abrir")]
        public PlacaPresion[] placas;

        [Tooltip("Si alguna de estas está presionada, la puerta NO abre (placas trampa)")]
        public PlacaPresion[] placasProhibidas;

        [Tooltip("Si está activo, una vez abierta ya no se cierra")]
        public bool quedarseAbierta = false;

        [Header("Partes")]
        public Collider2D bloqueo;
        public SpriteRenderer visual;
        public Sprite spriteCerrada;
        public Sprite spriteAbierta;
        public Color colorAbierta = new Color(1f, 1f, 1f, 0.35f);

        [Header("Eventos (opcional)")]
        public UnityEvent alAbrir;
        public UnityEvent alCerrar;

        public bool Abierta { get; private set; }

        private Color colorCerrada = Color.white;

        private void Awake()
        {
            if (bloqueo == null)
            {
                foreach (Collider2D c in GetComponents<Collider2D>())
                {
                    if (!c.isTrigger) { bloqueo = c; break; }
                }
            }
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (visual != null) colorCerrada = visual.color;
            Aplicar(false);
        }

        private void Update()
        {
            if (Abierta && quedarseAbierta) return;

            bool todas = placas != null && placas.Length > 0;
            if (todas)
            {
                foreach (PlacaPresion p in placas)
                {
                    if (p == null || !p.Presionada) { todas = false; break; }
                }
            }

            if (todas && placasProhibidas != null)
            {
                foreach (PlacaPresion p in placasProhibidas)
                {
                    if (p != null && p.Presionada) { todas = false; break; }
                }
            }

            if (todas != Abierta) Cambiar(todas);
        }

        public void Cambiar(bool abrir)
        {
            Abierta = abrir;
            Aplicar(abrir);
            if (abrir) alAbrir?.Invoke();
            else alCerrar?.Invoke();
        }

        private void Aplicar(bool abierta)
        {
            if (bloqueo != null) bloqueo.enabled = !abierta;
            if (visual != null)
            {
                Sprite s = abierta ? spriteAbierta : spriteCerrada;
                if (s != null) visual.sprite = s;
                visual.color = abierta ? colorAbierta : colorCerrada;
            }
        }
    }
}
