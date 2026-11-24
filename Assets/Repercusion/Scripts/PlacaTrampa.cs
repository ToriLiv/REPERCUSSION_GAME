using UnityEngine;

namespace Repercusion
{
    // Placa trampa: si Eco la pisa con los pies, pierde luz.
    // (Si la pisa un eco no quita luz, pero igual traba la reja si es "prohibida").
    [RequireComponent(typeof(PlacaPresion))]
    public class PlacaTrampa : MonoBehaviour
    {
        public float luzQueQuita = 20f;
        [TextArea] public string mensaje = "La piedra junto al fuego mintió...";

        private PlacaPresion placa;
        private Collider2D zona;
        private bool estabaPisada;

        private void Awake()
        {
            placa = GetComponent<PlacaPresion>();
            zona = GetComponent<Collider2D>();
        }

        private void FixedUpdate()
        {
            LuzEco luz = LuzEco.Jugadora;
            bool pisada = false;
            if (luz != null && zona != null)
            {
                Pisador p = luz.GetComponent<Pisador>();
                Vector2 pies = p != null ? p.PuntoPies : (Vector2)luz.transform.position;
                pisada = zona.OverlapPoint(pies);
            }
            if (pisada && !estabaPisada)
            {
                luz.Quitar(luzQueQuita);
                SonidosJuego.Tocar(SonidosJuego.Tipo.Trampa);
                if (!string.IsNullOrEmpty(mensaje)) GameManager.Mensaje(mensaje);
            }
            estabaPisada = pisada;
        }
    }
}
