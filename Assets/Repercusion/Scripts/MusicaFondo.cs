using UnityEngine;

namespace Repercusion
{
    // Música de fondo de la escena: empieza suave (fundido) y se repite sin fin.
    public class MusicaFondo : MonoBehaviour
    {
        public AudioClip musica;
        [Range(0f, 1f)] public float volumen = 0.5f;
        public float fundidoEntrada = 2.5f;

        private AudioSource fuente;
        private float t;

        private void Awake()
        {
            fuente = GetComponent<AudioSource>();
            if (fuente == null) fuente = gameObject.AddComponent<AudioSource>();
            fuente.clip = musica;
            fuente.loop = true;
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;
            fuente.volume = 0f;
            fuente.ignoreListenerPause = true;
        }

        private void Start()
        {
            if (musica != null) fuente.Play();
        }

        private void Update()
        {
            if (t >= fundidoEntrada) return;
            t += Time.unscaledDeltaTime;
            fuente.volume = volumen * Mathf.Clamp01(t / Mathf.Max(0.01f, fundidoEntrada));
        }
    }
}
