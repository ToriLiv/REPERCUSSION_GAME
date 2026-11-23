using UnityEngine;

namespace Repercusion
{
    // Cámara que sigue a Eco en X y en Y (tu CameraFollow solo seguía en X).
    // Va en la Main Camera.
    public class CamaraTopDown : MonoBehaviour
    {
        public Transform objetivo;
        [Tooltip("Más alto = más lento")]
        public float suavizado = 0.15f;
        public Vector2 desfase = Vector2.zero;

        [Tooltip("Si Eco se teletransporta más lejos que esto, la cámara salta sin deslizarse")]
        public float distanciaSalto = 8f;

        [Header("Límites del mapa (opcional)")]
        public bool usarLimites = false;
        public Vector2 limiteMin = new Vector2(-50f, -50f);
        public Vector2 limiteMax = new Vector2(50f, 50f);

        private Vector3 velocidad;

        private void Start()
        {
            if (objetivo == null)
            {
                LuzEco eco = FindAnyObjectByType<LuzEco>();
                if (eco != null) objetivo = eco.transform;
            }
            if (objetivo != null) transform.position = Destino();
        }

        private void LateUpdate()
        {
            if (objetivo == null) return;
            Vector3 destino = Destino();

            if (Vector2.Distance(transform.position, destino) > distanciaSalto)
            {
                transform.position = destino;
                velocidad = Vector3.zero;
                return;
            }
            transform.position = Vector3.SmoothDamp(transform.position, destino, ref velocidad, suavizado);
        }

        private Vector3 Destino()
        {
            float x = objetivo.position.x + desfase.x;
            float y = objetivo.position.y + desfase.y;
            if (usarLimites)
            {
                x = Mathf.Clamp(x, limiteMin.x, limiteMax.x);
                y = Mathf.Clamp(y, limiteMin.y, limiteMax.y);
            }
            return new Vector3(x, y, transform.position.z);
        }
    }
}
