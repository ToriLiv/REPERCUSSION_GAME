using UnityEngine;
using UnityEngine.SceneManagement;

namespace Repercusion
{
    // La puerta del centro. Se abre cuando Eco llega con todas las llaves.
    // Usa dos colliders: uno normal que bloquea (bloqueo) y uno "Is Trigger" más grande alrededor.
    public class PuertaSalida : MonoBehaviour
    {
        public Collider2D bloqueo;
        public SpriteRenderer visual;
        public Sprite spriteAbierta;
        public Color colorAbierta = new Color(1f, 1f, 1f, 0.35f);

        public string mensajeFaltan = "La puerta está sellada. Te faltan {0} llaves";
        public string mensajeFaltaUna = "La puerta está sellada. Te falta 1 llave";
        public string mensajeFinal = "Escapaste. Fin de la demo";

        [Tooltip("Opcional: escena que se carga al salir (por ejemplo, el menú). Vacío = no carga nada")]
        public string escenaAlTerminar = "";
        public float esperaAntesDeCargar = 3f;

        private bool abierta;

        private void OnTriggerEnter2D(Collider2D otro)
        {
            if (abierta || otro.GetComponentInParent<LuzEco>() == null) return;

            GameManager gm = GameManager.Instancia;
            if (gm == null) return;

            if (gm.TieneTodasLasLlaves)
            {
                abierta = true;
                if (bloqueo != null) bloqueo.enabled = false;
                if (visual != null)
                {
                    if (spriteAbierta != null) visual.sprite = spriteAbierta;
                    visual.color = colorAbierta;
                }
                GameManager.Mensaje(mensajeFinal, esperaAntesDeCargar + 1f);
                SonidosJuego.Tocar(SonidosJuego.Tipo.Reja);
                if (!string.IsNullOrEmpty(escenaAlTerminar)) Invoke(nameof(Cargar), esperaAntesDeCargar);
            }
            else
            {
                int faltan = gm.llavesNecesarias - gm.Llaves;
                GameManager.Mensaje(faltan == 1 ? mensajeFaltaUna : string.Format(mensajeFaltan, faltan));
            }
        }

        private void Cargar()
        {
            SceneManager.LoadScene(escenaAlTerminar);
        }
    }
}
