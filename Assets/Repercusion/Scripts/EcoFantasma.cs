using UnityEngine;

namespace Repercusion
{
    // Lo crea EcoGrabadora automáticamente. Repite un intento grabado
    // y al terminar se queda quieto en su último lugar.
    public class EcoFantasma : MonoBehaviour
    {
        [Header("Parámetros del Animator de Eco")]
        public string paramMoviendose = "IsMoving";
        public string paramX = "MoveX";
        public string paramY = "MoveY";

        private Vector2[] posiciones;
        private Vector2[] direcciones;
        private bool[] moviendose;
        private float paso = 0.02f;
        private float t;

        private Animator anim;
        private SpriteRenderer sr;
        private Color colorBase = Color.white;
        private bool parpadeo;
        private bool tieneMov, tieneX, tieneY;

        public bool Terminado => posiciones == null || t / paso >= posiciones.Length - 1;

        public void Configurar(Vector2[] pos, Vector2[] dir, bool[] mov, float pasoFijo,
                               Animator animator, AnimatorControllerParameter[] parametros,
                               SpriteRenderer render, Color color, bool conParpadeo)
        {
            posiciones = pos;
            direcciones = dir;
            moviendose = mov;
            paso = Mathf.Max(0.0001f, pasoFijo);
            anim = animator;
            sr = render;
            colorBase = color;
            parpadeo = conParpadeo;

            if (anim != null && parametros != null)
            {
                foreach (AnimatorControllerParameter p in parametros)
                {
                    if (p.name == paramMoviendose) tieneMov = true;
                    if (p.name == paramX) tieneX = true;
                    if (p.name == paramY) tieneY = true;
                }
            }
            Reiniciar();
        }

        public void Reiniciar()
        {
            t = 0f;
            Mostrar(0f);
        }

        private void Update()
        {
            if (posiciones == null || posiciones.Length == 0) return;
            t += Time.deltaTime;
            Mostrar(t / paso);

            if (sr != null && parpadeo && Time.timeScale > 0f)
            {
                Color c = colorBase;
                if (Random.value < 0.04f) c.a *= 0.25f;
                sr.color = c;
            }
        }

        private void Mostrar(float cuadro)
        {
            int ultimo = posiciones.Length - 1;
            int i = Mathf.Clamp(Mathf.FloorToInt(cuadro), 0, ultimo);
            int j = Mathf.Min(i + 1, ultimo);
            float k = Mathf.Clamp01(cuadro - i);

            Vector2 p = Vector2.Lerp(posiciones[i], posiciones[j], i == j ? 0f : k);
            transform.position = new Vector3(p.x, p.y, transform.position.z);

            bool termino = i >= ultimo;
            Animar(!termino && moviendose[i], direcciones[i]);
        }

        private void Animar(bool mov, Vector2 dir)
        {
            if (anim == null) return;
            if (tieneMov) anim.SetBool(paramMoviendose, mov);
            if (tieneX) anim.SetFloat(paramX, dir.x);
            if (tieneY) anim.SetFloat(paramY, dir.y);
        }
    }
}
