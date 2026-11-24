using TMPro;
using UnityEngine;

namespace Repercusion
{
    // Panel a pantalla completa para leer tablillas. Detiene el juego mientras está abierto
    // (la luz no se gasta). Se cierra solo a los "cierreAutomatico" segundos o con cualquier tecla.
    public class PanelLectura : MonoBehaviour
    {
        public CanvasGroup grupo;
        public TMP_Text titulo;
        public TMP_Text texto;
        public UnityEngine.UI.Image[] simbolos;
        public float letrasPorSegundo = 70f;
        public float cierreAutomatico = 15f;
        public TMP_Text pie;

        public static bool Abierto { get; private set; }
        public static int UltimoCierre { get; private set; } = -1;

        private static PanelLectura instancia;
        private float escalaAntes = 1f;
        private int abiertoEnCuadro;
        private float tiempoAbierto;

        private void Awake()
        {
            instancia = this;
            Abierto = false;
            if (grupo != null)
            {
                grupo.alpha = 0f;
                grupo.blocksRaycasts = false;
                grupo.interactable = false;
            }
        }

        private void OnDestroy()
        {
            if (instancia == this)
            {
                instancia = null;
                if (Abierto) Time.timeScale = 1f;
                Abierto = false;
            }
        }

        public static void Abrir(Tablilla t)
        {
            if (instancia == null) { GameManager.Mensaje(t.texto, 9f); return; }
            instancia.Mostrar(t);
        }

        private void Mostrar(Tablilla t)
        {
            if (titulo != null) titulo.text = t.titulo.ToUpperInvariant();
            if (texto != null)
            {
                texto.text = t.texto;
                texto.maxVisibleCharacters = 0;
            }
            for (int i = 0; simbolos != null && i < simbolos.Length; i++)
            {
                bool hay = t.simbolos != null && i < t.simbolos.Length && t.simbolos[i] != null;
                simbolos[i].gameObject.SetActive(hay);
                if (hay) simbolos[i].sprite = t.simbolos[i];
            }
            escalaAntes = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            Abierto = true;
            abiertoEnCuadro = Time.frameCount;
            tiempoAbierto = 0f;
            if (grupo != null) grupo.blocksRaycasts = true;
            SonidosJuego.Tocar(SonidosJuego.Tipo.Losa, 0.6f);
        }

        private void Cerrar()
        {
            Abierto = false;
            UltimoCierre = Time.frameCount;
            Time.timeScale = escalaAntes;
            if (grupo != null) grupo.blocksRaycasts = false;
        }

        private void Update()
        {
            if (grupo != null)
                grupo.alpha = Mathf.MoveTowards(grupo.alpha, Abierto ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            if (!Abierto) return;

            tiempoAbierto += Time.unscaledDeltaTime;
            if (texto != null) texto.maxVisibleCharacters = Mathf.FloorToInt(tiempoAbierto * letrasPorSegundo);

            float queda = cierreAutomatico - tiempoAbierto;
            if (pie != null) pie.text = "CUALQUIER TECLA  —  CERRAR    ·    " + Mathf.CeilToInt(Mathf.Max(0f, queda));
            if (queda <= 0f) { Cerrar(); return; }

            if (Time.frameCount > abiertoEnCuadro + 1 && tiempoAbierto > 0.4f && Entrada.CualquierTecla())
            {
                // Primer toque: termina de escribir. Segundo: cierra.
                if (texto != null && texto.maxVisibleCharacters < texto.text.Length)
                    tiempoAbierto = Mathf.Max(tiempoAbierto, texto.text.Length / letrasPorSegundo);
                else
                    Cerrar();
            }
        }
    }
}
