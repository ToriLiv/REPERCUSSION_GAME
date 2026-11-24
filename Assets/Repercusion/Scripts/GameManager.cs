using System;
using UnityEngine;
using UnityEngine.Events;

namespace Repercusion
{
    // Va UNO en la escena del juego. Guarda las llaves y regresa a Eco al centro
    // cuando se queda sin luz (las llaves se conservan).
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instancia { get; private set; }

        [Header("Llaves")]
        public int llavesNecesarias = 3;
        [SerializeField] private int llaves = 0;

        [Header("Regreso al centro")]
        [Tooltip("Donde reaparece Eco cuando su luz llega a 0")]
        public Transform puntoCentro;

        [Header("Mensaje al empezar")]
        [TextArea] public string mensajeInicial = "La salida está sellada. Encuentra las 3 llaves";
        public float esperaMensajeInicial = 1.2f;

        [Header("Eventos (opcional)")]
        public UnityEvent alQuedarseSinLuz;

        // (llaves actuales, llaves necesarias)
        public static event Action<int, int> OnLlavesCambiaron;
        public static event Action<string> OnMensaje;

        public int Llaves => llaves;
        public bool TieneTodasLasLlaves => llaves >= llavesNecesarias;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            OnLlavesCambiaron?.Invoke(llaves, llavesNecesarias);
            if (!string.IsNullOrEmpty(mensajeInicial)) Invoke(nameof(MostrarMensajeInicial), esperaMensajeInicial);
        }

        private void MostrarMensajeInicial()
        {
            Mensaje(mensajeInicial);
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        public void AgregarLlave()
        {
            llaves = Mathf.Min(llaves + 1, llavesNecesarias);
            OnLlavesCambiaron?.Invoke(llaves, llavesNecesarias);

            if (TieneTodasLasLlaves)
                Mensaje("¡Tienes todas las llaves! Vuelve a la puerta del centro");
            else
                Mensaje("Llave obtenida  " + llaves + "/" + llavesNecesarias);
        }

        public static void Mensaje(string texto)
        {
            OnMensaje?.Invoke(texto);
        }

        // Mensaje que se queda más tiempo en pantalla (acertijos, inscripciones)
        public static event Action<string, float> OnMensajeLargo;

        public static void Mensaje(string texto, float duracion)
        {
            if (OnMensajeLargo != null) OnMensajeLargo.Invoke(texto, duracion);
            else OnMensaje?.Invoke(texto);
        }

        public void RegresarAlCentro(GameObject jugador)
        {
            EcoGrabadora grabadora = jugador.GetComponent<EcoGrabadora>();
            if (grabadora != null) grabadora.SalirDeSala(null);

            if (puntoCentro != null) Teletransportar(jugador, puntoCentro.position);

            alQuedarseSinLuz?.Invoke();
            Mensaje("Tu luz se apagó... vuelves al inicio");
        }

        public static void Teletransportar(GameObject objeto, Vector2 destino)
        {
            Rigidbody2D rb = objeto.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = destino;
                rb.linearVelocity = Vector2.zero;
            }
            objeto.transform.position = new Vector3(destino.x, destino.y, objeto.transform.position.z);
        }
    }
}
