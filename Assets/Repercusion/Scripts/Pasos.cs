using UnityEngine;

namespace Repercusion
{
    // Sonido de pasos de Eco: mientras camina, toca un paso al azar a ritmo constante.
    [RequireComponent(typeof(Rigidbody2D))]
    public class Pasos : MonoBehaviour
    {
        public AudioClip[] pasos;
        [Tooltip("Segundos entre un paso y otro")]
        public float intervalo = 0.34f;
        [Range(0f, 1f)] public float volumen = 0.45f;
        [Tooltip("Velocidad mínima para que suenen")]
        public float velocidadMinima = 0.5f;

        private Rigidbody2D rb;
        private AudioSource fuente;
        private float siguiente;
        private int ultimo = -1;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            fuente = gameObject.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;
        }

        private void Update()
        {
            if (pasos == null || pasos.Length == 0 || Time.timeScale <= 0f) return;
            bool caminando = rb.linearVelocity.magnitude > velocidadMinima;
            if (!caminando)
            {
                // Al empezar a caminar, el primer paso suena casi de inmediato
                siguiente = Mathf.Min(siguiente, Time.time + 0.08f);
                return;
            }
            if (Time.time < siguiente) return;

            int i = Random.Range(0, pasos.Length);
            if (pasos.Length > 1 && i == ultimo) i = (i + 1) % pasos.Length;
            ultimo = i;
            fuente.pitch = Random.Range(0.92f, 1.08f);
            fuente.PlayOneShot(pasos[i], volumen * Random.Range(0.85f, 1f));
            siguiente = Time.time + intervalo;
        }
    }
}
