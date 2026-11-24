using UnityEngine;
using UnityEngine.Events;

namespace Repercusion
{
    // Placa que se activa cuando Eco o un eco tiene los pies encima.
    // Necesita un Collider2D (Is Trigger) que marca el área de la placa.
    [RequireComponent(typeof(Collider2D))]
    public class PlacaPresion : MonoBehaviour
    {
        [Header("Visual")]
        public SpriteRenderer visual;
        public Sprite spriteSuelta;
        public Sprite spritePresionada;
        public Color colorSuelta = Color.white;
        public Color colorPresionada = Color.white;

        [Tooltip("Opcional: un objeto (por ejemplo una Light 2D) que se enciende al presionar")]
        public GameObject encenderAlPresionar;

        [Header("Eventos (opcional)")]
        public UnityEvent alPresionar;
        public UnityEvent alSoltar;

        public bool Presionada { get; private set; }

        private Collider2D zona;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
            visual = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            zona = GetComponent<Collider2D>();
            zona.isTrigger = true;
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            Aplicar(false);
        }

        private void FixedUpdate()
        {
            bool alguienEncima = false;
            for (int i = 0; i < Pisador.Todos.Count; i++)
            {
                Pisador p = Pisador.Todos[i];
                if (p != null && zona.OverlapPoint(p.PuntoPies))
                {
                    alguienEncima = true;
                    break;
                }
            }

            if (alguienEncima == Presionada) return;

            Presionada = alguienEncima;
            Aplicar(Presionada);
            if (Presionada)
            {
                SonidosJuego.Tocar(SonidosJuego.Tipo.Losa, 0.8f);
                alPresionar?.Invoke();
            }
            else alSoltar?.Invoke();
        }

        private void Aplicar(bool presionada)
        {
            if (visual != null)
            {
                Sprite s = presionada ? spritePresionada : spriteSuelta;
                if (s != null) visual.sprite = s;
                visual.color = presionada ? colorPresionada : colorSuelta;
            }
            if (encenderAlPresionar != null) encenderAlPresionar.SetActive(presionada);
        }
    }
}
