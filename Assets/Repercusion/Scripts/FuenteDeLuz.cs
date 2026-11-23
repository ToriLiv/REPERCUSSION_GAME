using UnityEngine;

namespace Repercusion
{
    // Brasero: recarga la luz mientras Eco está cerca.
    // Poción: da luz de golpe y desaparece.
    // Necesita un Collider2D con "Is Trigger" (el área donde recarga).
    [RequireComponent(typeof(Collider2D))]
    public class FuenteDeLuz : MonoBehaviour
    {
        public enum Tipo { Brasero, Pocion }

        public Tipo tipo = Tipo.Brasero;

        [Tooltip("Poción: cuánta luz da")]
        public float cantidad = 35f;

        [Tooltip("Brasero: luz por segundo mientras Eco está cerca")]
        public float recargaPorSegundo = 25f;

        private bool usada;
        private bool avisado;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            LuzEco luz = otro.GetComponentInParent<LuzEco>();
            if (luz == null) return;

            if (tipo == Tipo.Pocion)
            {
                if (usada) return;
                usada = true;
                luz.Recuperar(cantidad);
                GameManager.Mensaje("+ Luz");
                gameObject.SetActive(false);
            }
            else if (!avisado)
            {
                avisado = true;
                GameManager.Mensaje("El fuego recarga tu luz");
            }
        }

        private void OnTriggerStay2D(Collider2D otro)
        {
            if (tipo != Tipo.Brasero) return;
            LuzEco luz = otro.GetComponentInParent<LuzEco>();
            if (luz != null) luz.Recuperar(recargaPorSegundo * Time.deltaTime);
        }
    }
}
