using UnityEngine;

namespace Repercusion
{
    // Zona roja que gasta la luz de Eco. Necesita un Collider2D con "Is Trigger".
    [RequireComponent(typeof(Collider2D))]
    public class ZonaPeligro : MonoBehaviour
    {
        [Tooltip("Luz que quita al entrar")]
        public float golpeAlEntrar = 10f;

        [Tooltip("Luz que quita por segundo mientras Eco sigue dentro")]
        public float perdidaPorSegundo = 15f;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            LuzEco luz = otro.GetComponentInParent<LuzEco>();
            if (luz != null) luz.QuitarConGolpe(golpeAlEntrar);
        }

        private void OnTriggerStay2D(Collider2D otro)
        {
            LuzEco luz = otro.GetComponentInParent<LuzEco>();
            if (luz != null) luz.Quitar(perdidaPorSegundo * Time.deltaTime);
        }
    }
}
