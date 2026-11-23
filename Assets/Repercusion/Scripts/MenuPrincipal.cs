using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Repercusion
{
    public class MenuPrincipal : MonoBehaviour
    {
        [Tooltip("Nombre de tu escena del juego. Si lo dejas vacío, carga la segunda escena de la lista de Build")]
        public string escenaJuego = "";

        public GameObject panelBotones;
        public GameObject panelComoJugar;
        public GameObject primerBoton;
        public GameObject botonCerrarComoJugar;

        private void Start()
        {
            Time.timeScale = 1f;
            MostrarBotones();
        }

        private void Update()
        {
            if (panelComoJugar != null && panelComoJugar.activeSelf && Entrada.Tecla(KeyCode.Escape))
                MostrarBotones();
        }

        public void Jugar()
        {
            if (!string.IsNullOrEmpty(escenaJuego))
                SceneManager.LoadScene(escenaJuego);
            else if (SceneManager.sceneCountInBuildSettings > 1)
                SceneManager.LoadScene(1);
            else
                Debug.LogWarning("Agrega tu escena del juego a File > Build Profiles (Scene List) o escribe su nombre en 'Escena Juego'.");
        }

        public void ComoJugar()
        {
            if (panelBotones != null) panelBotones.SetActive(false);
            if (panelComoJugar != null) panelComoJugar.SetActive(true);
            Seleccionar(botonCerrarComoJugar);
        }

        public void MostrarBotones()
        {
            if (panelComoJugar != null) panelComoJugar.SetActive(false);
            if (panelBotones != null) panelBotones.SetActive(true);
            Seleccionar(primerBoton);
        }

        public void Salir()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void Seleccionar(GameObject boton)
        {
            if (EventSystem.current != null && boton != null)
                EventSystem.current.SetSelectedGameObject(boton);
        }
    }
}
