using UnityEngine;
using UnityEngine.EventSystems;

namespace Repercusion
{
    // Al pasar el mouse o seleccionar con el teclado, el botón crece un poco
    // y se invierte: fondo blanco con letras negras.
    public class BotonAnimado : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
    {
        public float escala = 1.05f;
        public float velocidad = 12f;

        [Header("Inversión de colores")]
        public UnityEngine.UI.Image imagen;
        public Sprite spriteNormal;
        public Sprite spriteActivo;
        public TMPro.TMP_Text texto;
        public Color textoNormal = Color.white;
        public Color textoActivo = Color.black;

        private Vector3 escalaBase = Vector3.one;
        private bool activo;

        private void Awake()
        {
            escalaBase = transform.localScale;
            Aplicar();
        }

        private void OnDisable()
        {
            activo = false;
            transform.localScale = escalaBase;
            Aplicar();
        }

        private void Update()
        {
            Vector3 meta = activo ? escalaBase * escala : escalaBase;
            transform.localScale = Vector3.Lerp(transform.localScale, meta, Time.unscaledDeltaTime * velocidad);
        }

        // Pasar el mouse = seleccionar, así mouse y teclado se comportan igual
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData eventData) { activo = true; Aplicar(); }
        public void OnDeselect(BaseEventData eventData) { activo = false; Aplicar(); }

        private void Aplicar()
        {
            if (imagen != null && spriteNormal != null && spriteActivo != null)
                imagen.sprite = activo ? spriteActivo : spriteNormal;
            if (texto != null) texto.color = activo ? textoActivo : textoNormal;
        }
    }
}
