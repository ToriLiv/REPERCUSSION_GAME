using TMPro;
using UnityEngine;

namespace Repercusion
{
    // Muestra la luz, las llaves, los ecos y los mensajes en pantalla.
    // Lo arma solo el menú "Repercusión > 2. Agregar HUD y pausa a esta escena".
    public class HUD : MonoBehaviour
    {
        [Header("Luz")]
        public UnityEngine.UI.Image barraLuz;
        public UnityEngine.UI.Image iconoLuz;
        public Color colorLuz = new Color(0.96f, 0.96f, 0.96f);
        public Color colorLuzBaja = new Color(0.35f, 0.35f, 0.35f);
        [Range(0f, 1f)] public float umbralLuzBaja = 0.25f;

        [Header("Llaves")]
        public UnityEngine.UI.Image[] iconosLlave;
        public Color llaveApagada = new Color(1f, 1f, 1f, 0.18f);
        public Color llaveEncendida = Color.white;

        [Header("Ecos")]
        public GameObject panelEcos;
        public TMP_Text textoEcos;

        [Header("Mensajes")]
        public CanvasGroup grupoMensaje;
        public TMP_Text textoMensaje;
        public float duracionMensaje = 2.5f;

        private float luzObjetivo = 1f;
        private float luzMostrada = 1f;
        private float finMensaje = -1f;

        private void OnEnable()
        {
            LuzEco.OnLuzCambio += AlCambiarLuz;
            GameManager.OnLlavesCambiaron += AlCambiarLlaves;
            GameManager.OnMensaje += MostrarMensaje;
            GameManager.OnMensajeLargo += MostrarMensaje;
            if (textoMensaje != null)
            {
                // Los textos largos se achican para caber en la franja
                textoMensaje.enableAutoSizing = true;
                textoMensaje.fontSizeMax = textoMensaje.fontSize > 0 ? textoMensaje.fontSize : 40f;
                textoMensaje.fontSizeMin = 20f;
            }
            EcoGrabadora.OnEcosCambiaron += AlCambiarEcos;
        }

        private void OnDisable()
        {
            LuzEco.OnLuzCambio -= AlCambiarLuz;
            GameManager.OnLlavesCambiaron -= AlCambiarLlaves;
            GameManager.OnMensaje -= MostrarMensaje;
            GameManager.OnMensajeLargo -= MostrarMensaje;
            EcoGrabadora.OnEcosCambiaron -= AlCambiarEcos;
        }

        private void Start()
        {
            if (grupoMensaje != null) grupoMensaje.alpha = 0f;

            GameManager gm = GameManager.Instancia;
            AlCambiarLlaves(gm != null ? gm.Llaves : 0, gm != null ? gm.llavesNecesarias : 3);

            LuzEco luz = FindAnyObjectByType<LuzEco>();
            luzObjetivo = luz != null ? luz.LuzNormalizada : 1f;
            luzMostrada = luzObjetivo;
        }

        private void Update()
        {
            // Barra de luz: baja suave y parpadea cuando queda poca
            luzMostrada = Mathf.MoveTowards(luzMostrada, luzObjetivo, Time.unscaledDeltaTime * 1.5f);
            Color c = colorLuz;
            if (luzObjetivo <= umbralLuzBaja)
                c = Color.Lerp(colorLuz, colorLuzBaja, Mathf.PingPong(Time.unscaledTime * 3f, 1f));

            if (barraLuz != null)
            {
                barraLuz.fillAmount = luzMostrada;
                barraLuz.color = c;
            }
            if (iconoLuz != null) iconoLuz.color = Color.Lerp(new Color(1f, 1f, 1f, 0.35f), Color.white, luzMostrada * 1.5f);

            // Mensaje: aparece y se desvanece
            if (grupoMensaje != null)
            {
                float objetivo = Time.unscaledTime < finMensaje ? 1f : 0f;
                grupoMensaje.alpha = Mathf.MoveTowards(grupoMensaje.alpha, objetivo, Time.unscaledDeltaTime * 4f);
            }
        }

        private void AlCambiarLuz(float valor)
        {
            luzObjetivo = Mathf.Clamp01(valor);
        }

        private void AlCambiarLlaves(int actuales, int necesarias)
        {
            if (iconosLlave == null) return;
            for (int i = 0; i < iconosLlave.Length; i++)
            {
                if (iconosLlave[i] == null) continue;
                iconosLlave[i].gameObject.SetActive(i < necesarias);
                iconosLlave[i].color = i < actuales ? llaveEncendida : llaveApagada;
            }
        }

        private void AlCambiarEcos(int actuales, int maximo, bool enSala)
        {
            if (panelEcos != null) panelEcos.SetActive(enSala && maximo > 0);
            if (textoEcos != null) textoEcos.text = "ECOS  " + actuales + "/" + maximo;
        }

        public void MostrarMensaje(string texto)
        {
            MostrarMensaje(texto, duracionMensaje);
        }

        private RectTransform rtMensaje;
        private float altoBaseMensaje;

        public void MostrarMensaje(string texto, float duracion)
        {
            // La franja crece hacia abajo si el texto tiene varias líneas
            if (grupoMensaje != null)
            {
                if (rtMensaje == null)
                {
                    rtMensaje = grupoMensaje.GetComponent<RectTransform>();
                    if (rtMensaje != null) altoBaseMensaje = rtMensaje.sizeDelta.y;
                }
                if (rtMensaje != null)
                {
                    int lineas = string.IsNullOrEmpty(texto) ? 1 : texto.Split('\n').Length;
                    rtMensaje.sizeDelta = new Vector2(rtMensaje.sizeDelta.x, altoBaseMensaje + (lineas - 1) * 48f);
                }
            }
            if (textoMensaje != null) textoMensaje.text = texto;
            finMensaje = Time.unscaledTime + duracion;
        }
    }
}
