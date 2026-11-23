using System;
using UnityEngine;

namespace Repercusion
{
    // La "vida" de Eco. Va en el objeto de Eco (reemplaza a PlayerEnergy).
    // Los peligros la gastan, braseros y pociones la recuperan.
    // Si llega a 0, Eco vuelve al centro con la luz llena y sin perder llaves.
    [DisallowMultipleComponent]
    public class LuzEco : MonoBehaviour
    {
        public float luzMaxima = 100f;
        [SerializeField] private float luz = 100f;

        [Tooltip("Segundos sin recibir otro golpe después de entrar a un peligro")]
        public float invulnerable = 0.5f;

        // Valor de 0 a 1, para la barra del HUD
        public static event Action<float> OnLuzCambio;

        public float Luz => luz;

        // La luz de la jugadora (hay una sola en la escena)
        public static LuzEco Jugadora { get; private set; }

        private void OnEnable() { Jugadora = this; }
        private void OnDisable() { if (Jugadora == this) Jugadora = null; }
        public float LuzNormalizada => luzMaxima > 0f ? luz / luzMaxima : 0f;

        private float finInvulnerable;
        private bool apagandose;

        private void Awake()
        {
            // Si el Rigidbody se "duerme" al quedarse quieta, los peligros dejan de hacer daño
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }

        private void Start()
        {
            luz = luzMaxima;
            Avisar();
        }

        public void Quitar(float cantidad)
        {
            if (cantidad <= 0f || apagandose) return;
            luz = Mathf.Max(0f, luz - cantidad);
            Avisar();
            if (luz <= 0f) Apagar();
        }

        public void QuitarConGolpe(float cantidad)
        {
            if (Time.time < finInvulnerable) return;
            finInvulnerable = Time.time + invulnerable;
            Quitar(cantidad);
        }

        public void Recuperar(float cantidad)
        {
            if (cantidad <= 0f) return;
            luz = Mathf.Min(luzMaxima, luz + cantidad);
            Avisar();
        }

        private void Apagar()
        {
            apagandose = true;
            // Dentro de una sala: vuelves a su entrada y los ecos se desvanecen.
            // Fuera de las salas: vuelves al centro.
            EcoGrabadora grabadora = GetComponent<EcoGrabadora>();
            if (grabadora != null && grabadora.EnSala)
            {
                grabadora.ReiniciarSala();
                GameManager.Mensaje("Tu luz se apagó... tus ecos se desvanecen");
            }
            else if (GameManager.Instancia != null) GameManager.Instancia.RegresarAlCentro(gameObject);
            luz = luzMaxima;
            apagandose = false;
            Avisar();
        }

        private void Avisar()
        {
            OnLuzCambio?.Invoke(LuzNormalizada);
        }
    }
}
