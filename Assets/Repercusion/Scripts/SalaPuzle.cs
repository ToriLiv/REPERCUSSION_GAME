using UnityEngine;

namespace Repercusion
{
    // Marca el área de una sala de puzle. Mientras Eco está dentro, sus intentos se graban
    // y con la tecla R puede empezar un intento nuevo que deja un eco.
    // Necesita un Collider2D con "Is Trigger" que cubra toda la sala.
    [RequireComponent(typeof(Collider2D))]
    public class SalaPuzle : MonoBehaviour
    {
        public string nombreSala = "Sala 1";
        public string subtitulo = "El primer eco";

        [Tooltip("Donde empieza cada intento nuevo")]
        public Transform puntoInicio;

        [Tooltip("Cuántos ecos puede haber a la vez en esta sala")]
        [Min(0)] public int maxEcos = 1;

        public Vector2 Inicio => puntoInicio != null ? (Vector2)puntoInicio.position : (Vector2)transform.position;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            EcoGrabadora g = otro.GetComponentInParent<EcoGrabadora>();
            if (g != null) g.EntrarASala(this);
        }

        private void OnTriggerExit2D(Collider2D otro)
        {
            EcoGrabadora g = otro.GetComponentInParent<EcoGrabadora>();
            if (g != null) g.SalirDeSala(this);
        }

        private void OnDrawGizmos()
        {
            if (puntoInicio == null) return;
            Gizmos.color = new Color(0.15f, 0.85f, 0.75f);
            Gizmos.DrawWireSphere(puntoInicio.position, 0.35f);
        }
    }
}
