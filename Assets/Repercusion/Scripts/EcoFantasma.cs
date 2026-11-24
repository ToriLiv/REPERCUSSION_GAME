using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

        [Header("Vida del eco")]
        [Tooltip("Segundos que dura el eco (0 = para siempre)")]
        public float vida = 0f;
        [Tooltip("En los últimos segundos el eco tiembla y se apaga")]
        public float avisoFinal = 8f;

        private float nacio;
        private Light2D luz;
        private float intensidadLuz;

        // Ya se le acabó el tiempo (EcoGrabadora lo borra)
        public bool Expirado => vida > 0f && Time.time - nacio >= vida;

        // 1 = recién nacido, 0 = a punto de desaparecer (solo cuenta el tramo final)
        public float Fuerza
        {
            get
            {
                if (vida <= 0f) return 1f;
                float queda = vida - (Time.time - nacio);
                return Mathf.Clamp01(queda / Mathf.Max(0.01f, avisoFinal));
            }
        }

        private TextMeshPro susurro;
        private float susurroHasta;

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
            nacio = Time.time;
            luz = GetComponent<Light2D>();
            if (luz != null) intensidadLuz = luz.intensity;

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

            float fuerza = Fuerza;
            if (sr != null && Time.timeScale > 0f && (parpadeo || fuerza < 1f))
            {
                Color c = colorBase;
                c.a *= Mathf.Lerp(0.25f, 1f, fuerza);
                // Cuanto menos le queda, más tiembla
                float probabilidad = parpadeo ? 0.04f : 0f;
                if (fuerza < 1f) probabilidad = Mathf.Max(probabilidad, Mathf.Lerp(0.35f, 0.05f, fuerza));
                if (Random.value < probabilidad) c.a *= 0.25f;
                sr.color = c;
            }
            if (luz != null) luz.intensity = intensidadLuz * Mathf.Lerp(0.2f, 1f, fuerza);

            if (susurro != null)
            {
                float queda = susurroHasta - Time.time;
                Color sc = susurro.color;
                sc.a = Mathf.Clamp01(queda / 1f);
                susurro.color = sc;
                if (queda <= 0f) { Destroy(susurro.gameObject); susurro = null; }
            }
        }

        // Una frase corta flota sobre el eco unos segundos: es un recuerdo de la científica
        public void Susurrar(string texto, TMP_FontAsset fuente, float duracion = 3.5f)
        {
            if (string.IsNullOrEmpty(texto)) return;
            if (susurro != null) Destroy(susurro.gameObject);
            GameObject go = new GameObject("Whisper");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.7f / Mathf.Max(0.01f, transform.localScale.y), 0f);
            susurro = go.AddComponent<TextMeshPro>();
            if (fuente != null) susurro.font = fuente;
            susurro.text = texto;
            susurro.fontSize = 3f;
            susurro.alignment = TextAlignmentOptions.Center;
            susurro.color = new Color(0.8f, 1f, 0.96f, 1f);
            susurro.rectTransform.sizeDelta = new Vector2(8f, 1f);
            if (sr != null)
            {
                susurro.sortingLayerID = sr.sortingLayerID;
                susurro.sortingOrder = sr.sortingOrder + 10;
            }
            susurroHasta = Time.time + duracion;
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
