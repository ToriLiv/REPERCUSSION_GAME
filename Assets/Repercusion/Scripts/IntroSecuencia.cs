using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Repercusion
{
    // Intro tipo "diapositivas": texto que se escribe letra por letra, imagen opcional
    // y un golpe de glitch en las diapositivas marcadas.
    // Clic, Espacio o Enter: avanza.  Esc: salta toda la intro.
    public class IntroSecuencia : MonoBehaviour
    {
        [Serializable]
        public class Diapositiva
        {
            [TextArea(2, 4)] public string texto;
            [Tooltip("Opcional: dibujo de Clip Studio para esta parte")]
            public Sprite imagen;
            [Tooltip("Segundos que se queda después de terminar de escribirse")]
            public float espera = 2.5f;
            [Tooltip("Golpe de glitch al aparecer")]
            public bool glitch;
        }

        public Diapositiva[] diapositivas =
        {
            new Diapositiva { texto = "Eco era una científica brillante…\ny ambiciosa." },
            new Diapositiva { texto = "Quería crear lo que nadie había logrado:\nuna máquina para atravesar el tiempo." },
            new Diapositiva { texto = "Estuvo a punto de conseguirlo." },
            new Diapositiva { texto = "Y entonces, falló.", glitch = true, espera = 1.5f },
            new Diapositiva { texto = "Despertó en un calabozo\nque no existe en ningún tiempo." },
            new Diapositiva { texto = "La única salida está sellada con tres llaves." },
            new Diapositiva { texto = "Aquí la fuerza no sirve.\nSolo su ingenio…" },
            new Diapositiva { texto = "…y los ecos de lo que pudo haber sido.", espera = 3f },
        };

        [Header("Partes (las conecta el menú Repercusión)")]
        public CanvasGroup grupo;
        public TMP_Text texto;
        public UnityEngine.UI.Image imagen;
        public CanvasGroup flash;
        public GameObject glitch;

        [Header("Ritmo")]
        public float letrasPorSegundo = 38f;
        public float fundido = 0.6f;

        [Tooltip("Escena que sigue. Vacío = la siguiente en la lista de Build")]
        public string escenaSiguiente = "";

        private bool avanzar;
        private bool terminando;

        private void Start()
        {
            if (flash != null) flash.alpha = 0f;
            if (glitch != null) glitch.SetActive(false);
            StartCoroutine(Reproducir());
        }

        private void Update()
        {
            if (Entrada.Tecla(KeyCode.Escape)) { Terminar(); return; }
            if (Entrada.Clic() || Entrada.Tecla(KeyCode.Space) || Entrada.Tecla(KeyCode.Return))
                avanzar = true;
        }

        private IEnumerator Reproducir()
        {
            foreach (Diapositiva d in diapositivas)
            {
                Preparar(d);
                yield return Fundir(0f, 1f);
                if (d.glitch) StartCoroutine(Glitch());

                // Escribir letra por letra (un clic lo completa)
                avanzar = false;
                texto.ForceMeshUpdate();
                int total = texto.textInfo.characterCount;
                float t = 0f;
                while (texto.maxVisibleCharacters < total && !avanzar)
                {
                    t += Time.deltaTime * letrasPorSegundo;
                    texto.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(t));
                    yield return null;
                }
                texto.maxVisibleCharacters = total;

                // Esperar (otro clic lo salta)
                avanzar = false;
                float espera = 0f;
                while (espera < d.espera && !avanzar)
                {
                    espera += Time.deltaTime;
                    yield return null;
                }

                yield return Fundir(1f, 0f);
            }
            Terminar();
        }

        private void Preparar(Diapositiva d)
        {
            texto.text = d.texto;
            texto.maxVisibleCharacters = 0;

            bool conImagen = imagen != null && d.imagen != null;
            if (imagen != null)
            {
                imagen.gameObject.SetActive(conImagen);
                if (conImagen) imagen.sprite = d.imagen;
            }
            // Con imagen, el texto baja; sin imagen, va al centro
            texto.rectTransform.anchoredPosition = new Vector2(0f, conImagen ? -330f : 0f);
        }

        private IEnumerator Fundir(float desde, float hasta)
        {
            if (grupo == null) yield break;
            float t = 0f;
            while (t < fundido)
            {
                t += Time.deltaTime;
                grupo.alpha = Mathf.Lerp(desde, hasta, t / fundido);
                yield return null;
            }
            grupo.alpha = hasta;
        }

        private IEnumerator Glitch()
        {
            if (glitch != null) glitch.SetActive(true);
            Vector2 base_ = texto.rectTransform.anchoredPosition;
            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.deltaTime;
                if (flash != null) flash.alpha = UnityEngine.Random.value < 0.35f ? UnityEngine.Random.Range(0.3f, 0.9f) : 0f;
                texto.rectTransform.anchoredPosition = base_ + new Vector2(UnityEngine.Random.Range(-14f, 14f), UnityEngine.Random.Range(-4f, 4f));
                yield return null;
            }
            if (flash != null) flash.alpha = 0f;
            texto.rectTransform.anchoredPosition = base_;
            if (glitch != null) glitch.SetActive(false);
        }

        private void Terminar()
        {
            if (terminando) return;
            terminando = true;
            StopAllCoroutines();

            if (!string.IsNullOrEmpty(escenaSiguiente))
                SceneManager.LoadScene(escenaSiguiente);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }
}
