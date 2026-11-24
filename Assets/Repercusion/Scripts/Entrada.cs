using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Repercusion
{
    // Lee teclado y mouse con el Input System nuevo (o con el viejo si el proyecto lo usa).
    public static class Entrada
    {
        public static bool Tecla(KeyCode tecla)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            if (tecla == KeyCode.Return) return kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
            if (System.Enum.TryParse(tecla.ToString(), out Key key) && key != Key.None)
                return kb[key].wasPressedThisFrame;
            return false;
#else
            return Input.GetKeyDown(tecla);
#endif
        }

        // Cualquier tecla del teclado (o clic) en este cuadro
        public static bool CualquierTecla()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) || Clic();
#else
            return Input.anyKeyDown;
#endif
        }

        public static bool Clic()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }
    }
}
