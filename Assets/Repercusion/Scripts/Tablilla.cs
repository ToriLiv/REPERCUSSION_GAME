using TMPro;
using UnityEngine;

namespace Repercusion
{
    // Tablilla de piedra con una pista.
    // La primera vez que te acercas se abre el panel de lectura grande (se cierra solo a los 15 s).
    // Después, cada vez que pasas cerca, el acertijo aparece flotando sobre la tablilla,
    // sin pausar el juego y sin apretar ninguna tecla. Al alejarte se desvanece.
    public class Tablilla : MonoBehaviour
    {
        public string titulo = "Tablilla de piedra";
        [TextArea(3, 8)] public string texto;
        [Tooltip("Símbolos que se muestran debajo del texto (opcional)")]
        public Sprite[] simbolos;
        public float radio = 2.5f;
        [Tooltip("Qué tan rápido aparece y se desvanece el texto flotante")]
        public float velocidadFundido = 3f;

        [Header("Partes")]
        public GameObject marcador;
        [Tooltip("Texto flotante sobre la tablilla (muestra el acertijo al acercarse)")]
        public GameObject aviso;

        public static Tablilla Cercana { get; private set; }

        private bool leida;
        public bool Leida => leida;
        private Vector3 baseMarcador;
        private TMP_Text avisoTexto;
        private Color colorAviso;
        private float alfa;

        private void Start()
        {
            if (marcador != null) baseMarcador = marcador.transform.localPosition;
            if (aviso != null)
            {
                avisoTexto = aviso.GetComponent<TMP_Text>();
                if (avisoTexto != null)
                {
                    avisoTexto.text = texto;
                    colorAviso = avisoTexto.color;
                }
                aviso.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (Cercana == this) Cercana = null;
        }

        private void Update()
        {
            bool cerca = false;
            LuzEco jugadora = LuzEco.Jugadora;
            if (jugadora != null)
            {
                Pisador p = jugadora.GetComponent<Pisador>();
                Vector2 pies = p != null ? p.PuntoPies : (Vector2)jugadora.transform.position;
                cerca = Vector2.Distance(pies, (Vector2)transform.position + Vector2.down * 0.8f) < radio;
            }
            if (cerca) Cercana = this;
            else if (Cercana == this) Cercana = null;

            if (marcador != null)
            {
                marcador.SetActive(!leida);
                marcador.transform.localPosition = baseMarcador + Vector3.up * (Mathf.Sin(Time.unscaledTime * 2.5f) * 0.12f);
            }

            // Primera vez: el panel grande
            if (cerca && !leida && !PanelLectura.Abierto && Time.timeScale > 0f &&
                Time.frameCount != PanelLectura.UltimoCierre)
            {
                leida = true;
                PanelLectura.Abrir(this);
            }

            // Después: el acertijo flota sobre la tablilla mientras estés cerca
            bool mostrar = cerca && leida && !PanelLectura.Abierto;
            alfa = Mathf.MoveTowards(alfa, mostrar ? 1f : 0f, velocidadFundido * Time.unscaledDeltaTime);
            if (aviso != null)
            {
                aviso.SetActive(alfa > 0.01f);
                if (avisoTexto != null)
                    avisoTexto.color = new Color(colorAviso.r, colorAviso.g, colorAviso.b, colorAviso.a * alfa);
            }
        }
    }
}
