using UnityEditor;
using UnityEditor.SceneManagement;

namespace Repercusion.Herramientas
{
    // Hace que el botón Play de Unity siempre empiece en el Menú principal,
    // tengas abierta la escena que tengas. Se apaga/prende en el menú Repercusión.
    [InitializeOnLoad]
    public static class EmpezarDesdeMenu
    {
        private const string CLAVE = "Repercusion.EmpezarDesdeMenu";
        private const string OPCION = "Repercusión/Play empieza en el Menú principal";

        static EmpezarDesdeMenu()
        {
            EditorApplication.delayCall += Aplicar;
        }

        private static bool Activo
        {
            get { return EditorPrefs.GetBool(CLAVE, true); }
            set { EditorPrefs.SetBool(CLAVE, value); }
        }

        [MenuItem(OPCION, priority = 60)]
        private static void Alternar()
        {
            Activo = !Activo;
            Aplicar();
        }

        [MenuItem(OPCION, true)]
        private static bool AlternarValidar()
        {
            Menu.SetChecked(OPCION, Activo);
            return true;
        }

        private static void Aplicar()
        {
            SceneAsset menu = null;
            if (Activo)
            {
                foreach (string id in AssetDatabase.FindAssets("MenuPrincipal t:Scene"))
                {
                    string ruta = AssetDatabase.GUIDToAssetPath(id);
                    if (ruta.EndsWith("/MenuPrincipal.unity"))
                    {
                        menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta);
                        break;
                    }
                }
            }
            EditorSceneManager.playModeStartScene = menu;
        }
    }
}
