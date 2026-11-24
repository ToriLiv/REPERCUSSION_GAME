using UnityEngine;

namespace Repercusion
{
    // Llave que Eco recoge al tocarla. Flota suavemente.
    // Necesita un Collider2D con "Is Trigger".
    [RequireComponent(typeof(Collider2D))]
    public class Llave : MonoBehaviour
    {
        [Header("Flotar")]
        public float amplitud = 0.12f;
        public float velocidad = 2.5f;

        [Tooltip("Opcional: efecto que aparece al recogerla")]
        public GameObject efectoAlTomar;

        private Vector3 inicio;
        private bool tomada;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Start()
        {
            inicio = transform.position;
        }

        private void Update()
        {
            transform.position = inicio + Vector3.up * (Mathf.Sin(Time.time * velocidad) * amplitud);
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            if (tomada) return;
            if (otro.GetComponentInParent<LuzEco>() == null) return; // solo Eco, no sus ecos

            tomada = true;
            SonidosJuego.Tocar(SonidosJuego.Tipo.Llave);
            if (GameManager.Instancia != null) GameManager.Instancia.AgregarLlave();
            if (efectoAlTomar != null) Instantiate(efectoAlTomar, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
