using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Repercusion
{
    // Opción de menú estilo "01  ——  JUGAR".
    // Al seleccionarla (mouse o teclado) aparece la línea, el texto se corre y se pone blanco.
    public class OpcionMenu : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
    {
        public TMP_Text numero;
        public TMP_Text texto;
        public RectTransform linea;

        public Color colorNormal = new Color32(140, 140, 140, 255);
        public Color colorActivo = new Color32(245, 245, 245, 255);
        public Color numeroNormal = new Color32(107, 107, 107, 255);
        public float desplazamiento = 84f;
        public float velocidad = 10f;

        private float xTexto;
        private float t;
        private bool activo;

        private void Awake()
        {
            if (texto != null) xTexto = texto.rectTransform.anchoredPosition.x;
            Aplicar();
        }

        private void OnDisable()
        {
            activo = false;
            t = 0f;
            Aplicar();
        }

        private void Update()
        {
            float meta = activo ? 1f : 0f;
            if (Mathf.Approximately(t, meta)) return;
            t = Mathf.MoveTowards(t, meta, Time.unscaledDeltaTime * velocidad);
            Aplicar();
        }

        private void Aplicar()
        {
            float suave = t * t * (3f - 2f * t);
            if (linea != null) linea.localScale = new Vector3(suave, 1f, 1f);
            if (texto != null)
            {
                Vector2 p = texto.rectTransform.anchoredPosition;
                texto.rectTransform.anchoredPosition = new Vector2(xTexto + desplazamiento * suave, p.y);
                texto.color = Color.Lerp(colorNormal, colorActivo, suave);
                texto.fontStyle = activo ? FontStyles.Bold : FontStyles.Normal;
            }
            if (numero != null) numero.color = Color.Lerp(numeroNormal, colorActivo, suave * 0.6f);
        }

        // Pasar el mouse = seleccionar, así mouse y teclado se comportan igual
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData eventData) { activo = true; }
        public void OnDeselect(BaseEventData eventData) { activo = false; }
    }
}
