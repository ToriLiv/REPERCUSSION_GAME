using UnityEngine;

namespace Repercusion
{
    // Efectos de sonido del juego (eco, losa, reja, llave, trampa).
    // Hay uno por escena; cualquier script lo usa así: SonidosJuego.Tocar(SonidosJuego.Tipo.Llave);
    public class SonidosJuego : MonoBehaviour
    {
        public enum Tipo { Eco, Losa, Reja, Llave, Trampa }

        public AudioClip eco;
        public AudioClip losa;
        public AudioClip reja;
        public AudioClip llave;
        public AudioClip trampa;
        [Range(0f, 1f)] public float volumen = 0.8f;

        private static SonidosJuego instancia;
        private AudioSource fuente;

        private void Awake()
        {
            instancia = this;
            fuente = GetComponent<AudioSource>();
            if (fuente == null) fuente = gameObject.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;
        }

        private void OnDestroy()
        {
            if (instancia == this) instancia = null;
        }

        public static void Tocar(Tipo tipo, float volumenExtra = 1f)
        {
            if (instancia == null) return;
            AudioClip clip = instancia.Clip(tipo);
            if (clip == null) return;
            instancia.fuente.pitch = Random.Range(0.96f, 1.04f);
            instancia.fuente.PlayOneShot(clip, instancia.volumen * volumenExtra);
        }

        private AudioClip Clip(Tipo tipo)
        {
            switch (tipo)
            {
                case Tipo.Eco: return eco;
                case Tipo.Losa: return losa;
                case Tipo.Reja: return reja;
                case Tipo.Llave: return llave;
                default: return trampa;
            }
        }
    }
}
