using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Repercusion
{
    // Esc o P abre y cierra la pausa.
    public class MenuPausa : MonoBehaviour
    {
        public GameObject panelPausa;
        public GameObject primerBoton;

        [Tooltip("Nombre de la escena del menú principal")]
        public string escenaMenu = "MenuPrincipal";

        public bool EnPausa { get; private set; }

        private void Start()
        {
            if (panelPausa != null) panelPausa.SetActive(false);
        }

        private void Update()
        {
            if (Entrada.Tecla(KeyCode.Escape) || Entrada.Tecla(KeyCode.P))
            {
                if (EnPausa) Continuar();
                else Pausar();
            }
        }

        public void Pausar()
        {
            EnPausa = true;
            Time.timeScale = 0f;
            if (panelPausa != null) panelPausa.SetActive(true);
            if (EventSystem.current != null && primerBoton != null)
                EventSystem.current.SetSelectedGameObject(primerBoton);
        }

        public void Continuar()
        {
            EnPausa = false;
            Time.timeScale = 1f;
            if (panelPausa != null) panelPausa.SetActive(false);
        }

        public void ReiniciarSala()
        {
            Continuar();
            EcoGrabadora g = FindAnyObjectByType<EcoGrabadora>();
            if (g != null && g.SalaActual != null) g.ReiniciarSala();
            else GameManager.Mensaje("Esto funciona dentro de una sala de puzle");
        }

        public void IrAlMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(escenaMenu);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
