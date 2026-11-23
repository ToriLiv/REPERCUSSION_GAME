using UnityEngine;

namespace Repercusion
{
    // Muestra un mensaje en pantalla cuando Eco entra al área (pistas, nombres de zonas...).
    // Necesita un Collider2D con "Is Trigger". Para las pruebas con jugadores "sin ayuda", desactívalo.
    [RequireComponent(typeof(Collider2D))]
    public class ZonaMensaje : MonoBehaviour
    {
        [TextArea] public string mensaje = "Pista: ¿y si te ayudas a ti misma? Presiona R";
        public bool soloUnaVez = true;
        [Tooltip("Segundos en pantalla (0 = lo normal)")]
        public float duracion = 0f;

        private bool mostrado;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            if (mostrado && soloUnaVez) return;
            if (otro.GetComponentInParent<LuzEco>() == null) return;
            mostrado = true;
            if (duracion > 0f) GameManager.Mensaje(mensaje, duracion);
            else GameManager.Mensaje(mensaje);
        }
    }
}
