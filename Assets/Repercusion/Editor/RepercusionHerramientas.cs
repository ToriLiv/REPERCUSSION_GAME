using System.Collections.Generic;
using System.Linq;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;

namespace Repercusion.Herramientas
{
    // Agrega el menú "Repercusión" arriba en Unity. Cada opción arma algo ya conectado:
    //   0. Configurar sprites          (lo hacen solos los demás pasos)
    //   1. Crear escena del Menú principal
    //   2. Agregar HUD, pausa y GameManager a esta escena
    //   3. Crear Sala 1 (placa + puerta + llave)
    //   4. Crear peligro, brasero y poción de prueba
    //   5. Crear puerta de salida
    public static class RepercusionHerramientas
    {
        private const string MENU = "Repercusión/";

        // Estética: negro, blanco y grises. Monoespaciada para textos, serif cursiva para títulos.
        private static readonly Color FONDO = new Color32(6, 6, 6, 255);
        private static readonly Color BLANCO = new Color32(245, 245, 245, 255);
        private static readonly Color GRIS = new Color32(140, 140, 140, 255);
        private static readonly Color GRIS_OSCURO = new Color32(107, 107, 107, 255);
        private static readonly Color CAPA_OSCURA = new Color(0.024f, 0.024f, 0.024f, 0.94f);

        private const float MARGEN = 144f;       // margen izquierdo del diseño
        private const float ESPACIADO = 22f;     // separación entre letras de los textos chicos

        // =====================================================================
        // RUTAS Y SPRITES
        // =====================================================================

        private static string Raiz()
        {
            const string fin = "/Editor/RepercusionHerramientas.cs";
            foreach (string id in AssetDatabase.FindAssets("RepercusionHerramientas t:Script"))
            {
                string p = AssetDatabase.GUIDToAssetPath(id);
                if (p.EndsWith(fin)) return p.Substring(0, p.Length - fin.Length);
            }
            return "Assets/Repercusion";
        }

        [MenuItem(MENU + "0. Configurar sprites", priority = 0)]
        public static void ConfigurarSpritesMenu()
        {
            int n = ConfigurarSprites();
            EditorUtility.DisplayDialog("Repercusión", "Sprites listos (" + n + " actualizados).", "OK");
        }

        private static int ConfigurarSprites()
        {
            int cambiados = 0;
            foreach (string id in AssetDatabase.FindAssets("t:Texture2D", new[] { Raiz() }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(id);
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                bool mundo = nombre.StartsWith("mundo_");
                bool ui = nombre.StartsWith("ui_");
                bool px = nombre.StartsWith("px_");
                if (!mundo && !ui && !px) continue;

                TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (ti == null) continue;

                if (px)
                {
                    if (ConfigurarPixelArt(ti)) cambiados++;
                    continue;
                }

                float ppu = mundo ? 128f : 100f;   // mundo: 128 px = 1 casilla
                Vector4 borde = Borde(nombre);

                bool igual = ti.textureType == TextureImporterType.Sprite
                             && ti.spriteImportMode == SpriteImportMode.Single
                             && Mathf.Approximately(ti.spritePixelsPerUnit, ppu)
                             && ti.spriteBorder == borde
                             && !ti.mipmapEnabled
                             && ti.textureCompression == TextureImporterCompression.Uncompressed;
                if (igual) continue;

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = ppu;
                ti.spriteBorder = borde;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Bilinear;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
                cambiados++;
            }
            return cambiados;
        }

        // Pixel art de paredes y puerta: 16 px = 1 casilla, sin suavizado, malla completa (para dibujo en mosaico)
        private static bool ConfigurarPixelArt(TextureImporter ti)
        {
            TextureImporterSettings st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            bool igual = ti.textureType == TextureImporterType.Sprite
                         && ti.spriteImportMode == SpriteImportMode.Single
                         && Mathf.Approximately(ti.spritePixelsPerUnit, 16f)
                         && ti.filterMode == FilterMode.Point
                         && !ti.mipmapEnabled
                         && ti.textureCompression == TextureImporterCompression.Uncompressed
                         && st.spriteMeshType == SpriteMeshType.FullRect;
            if (igual) return false;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16f;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Point;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            st.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
            return true;
        }

        // Bordes para que paneles y botones se estiren sin deformar las esquinas (9-slice)
        private static Vector4 Borde(string nombre)
        {
            switch (nombre)
            {
                case "ui_panel": return new Vector4(30, 30, 30, 30);
                case "ui_boton":
                case "ui_boton_activo": return new Vector4(46, 46, 46, 46);
                case "ui_barra_marco": return new Vector4(15, 15, 15, 15);
                case "ui_tecla": return new Vector4(20, 20, 20, 20);
                default: return Vector4.zero;
            }
        }

        private static Sprite S(string nombre)
        {
            string raiz = Raiz();
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(raiz + "/UI/Sprites/" + nombre + ".png");
            if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(raiz + "/Mundo/Sprites/" + nombre + ".png");
            if (s == null) Debug.LogWarning("Repercusión: no encontré el sprite " + nombre);
            return s;
        }

        // Deja todo listo antes de construir. Devuelve false si hay que esperar.
        private static bool Preparar(bool necesitaTexto)
        {
            ConfigurarSprites();
            if (!necesitaTexto) return true;

            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null) return true;

            TMP_PackageResourceImporter.ImportResources(true, false, false);
            EditorUtility.DisplayDialog("Repercusión",
                "Estoy importando los recursos de TextMeshPro (los textos los necesitan).\n\n" +
                "Espera a que termine y vuelve a usar este mismo comando.", "OK");
            return false;
        }

        // =====================================================================
        // 1. MENÚ PRINCIPAL
        // =====================================================================

        [MenuItem(MENU + "1. Crear escena del Menú principal", priority = 20)]
        public static void CrearEscenaMenu()
        {
            if (!Preparar(true)) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string carpeta = AsegurarCarpeta(Raiz(), "Escenas");
            string ruta = carpeta + "/MenuPrincipal.unity";
            if (File.Exists(ruta) && !EditorUtility.DisplayDialog("Repercusión",
                    "Ya existe la escena MenuPrincipal. ¿Reemplazarla?", "Reemplazar", "Cancelar"))
                return;

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CrearCamara();
            AsegurarEventSystem();
            ConstruirMenu();

            TraducirEscena(escena);
            EditorSceneManager.SaveScene(escena, ruta);
            AgregarAlBuild(ruta, MENU_ESCENA);
            EditorUtility.DisplayDialog("Repercusión",
                "Listo: escena MenuPrincipal creada y puesta primera en la lista de Build.\n\n" +
                "Ahora usa el paso 1b para la intro.", "OK");
        }

        private static void ConstruirMenu()
        {
            GameObject canvas = CrearCanvas("UI_MenuPrincipal", 0);
            Transform c = canvas.transform;
            MenuPrincipal menu = canvas.AddComponent<MenuPrincipal>();

            FondoConCirculos(c, 1f, true);

            // Bloque de título (arriba a la izquierda)
            TextoArribaIzq(c, "Firma", "UN JUEGO DE TORILIV", 20f, GRIS, MARGEN, 146f, 900f, 30f, ESPACIADO);
            TextMeshProUGUI titulo = TextoArribaIzq(c, "Titulo", "Repercusión", 160f, BLANCO, MARGEN - 6f, 178f, 1100f, 190f, 0f, true);
            titulo.fontStyle = FontStyles.Normal;
            TextoArribaIzq(c, "Lema", "Ecos de lo que pudo haber sido.", 24f, new Color32(185, 185, 185, 255), MARGEN, 390f, 900f, 36f, 0f);

            // Opciones numeradas
            GameObject botones = NuevoUI("Botones", c);
            Estirar(botones, 0f);
            UnityEngine.UI.Button jugar = Opcion(botones.transform, "OpcionJugar", "01", "JUGAR", 628f);
            UnityEngine.UI.Button como = Opcion(botones.transform, "OpcionComoJugar", "02", "CÓMO JUGAR", 712f);
            UnityEngine.UI.Button salir = Opcion(botones.transform, "OpcionSalir", "03", "SALIR", 796f);
            UnityEventTools.AddPersistentListener(jugar.onClick, new UnityAction(menu.Jugar));
            UnityEventTools.AddPersistentListener(como.onClick, new UnityAction(menu.ComoJugar));
            UnityEventTools.AddPersistentListener(salir.onClick, new UnityAction(menu.Salir));

            // Pie
            TextoAbajo(c, "Pie", "«MUJERES CREANDO VIDEOJUEGOS»  ·  UVG", 18f, GRIS_OSCURO, true);
            TextoAbajo(c, "Version", "VERSIÓN DEMO", 18f, GRIS_OSCURO, false);

            // Panel "Cómo jugar"
            GameObject comoJugar = NuevoUI("ComoJugar", c);
            Estirar(comoJugar, 0f);
            Imagen(comoJugar, null, FONDO, true);              // negro sólido: tapa todo el menú
            FondoConCirculos(comoJugar.transform, 1f, true);

            TextoArribaIzq(comoJugar.transform, "Firma", "INSTRUCCIONES", 20f, GRIS, MARGEN, 146f, 900f, 30f, ESPACIADO);
            TextoArribaIzq(comoJugar.transform, "Titulo", "Cómo jugar", 120f, BLANCO, MARGEN - 4f, 178f, 1100f, 150f, 0f, true);

            string filas =
                "<color=#F5F5F5>W  A  S  D</color><pos=300>Moverte (también con las flechas)\n" +
                "<color=#F5F5F5>E</color><pos=300>Dejar un eco: una copia de ti se queda donde estás\n" +
                "<color=#F5F5F5>ESC</color><pos=300>Pausa";
            TextMeshProUGUI controles = TextoArribaIzq(comoJugar.transform, "Controles", filas, 26f, GRIS, MARGEN, 380f, 1300f, 140f, 0f);
            controles.lineSpacing = 30f;

            TextoArribaIzq(comoJugar.transform, "Objetivo",
                "Pisa las placas para abrir las puertas. Tus ecos repiten lo que hiciste: úsalos para " +
                "sostener las placas mientras tú pasas. Reúne las <color=#F5F5F5>3 llaves</color> y vuelve a la " +
                "puerta del centro. Los peligros gastan tu <color=#F5F5F5>luz</color>; el fuego la recupera.",
                24f, GRIS, MARGEN, 560f, 1050f, 140f, 0f).lineSpacing = 20f;

            UnityEngine.UI.Button volver = Opcion(comoJugar.transform, "OpcionVolver", "01", "VOLVER", 796f);
            UnityEventTools.AddPersistentListener(volver.onClick, new UnityAction(menu.MostrarBotones));
            comoJugar.SetActive(false);

            menu.panelBotones = botones;
            menu.panelComoJugar = comoJugar;
            menu.primerBoton = jugar.gameObject;
            menu.botonCerrarComoJugar = volver.gameObject;
            EditorUtility.SetDirty(menu);
        }

        // =====================================================================
        // 1b. INTRO
        // =====================================================================

        [MenuItem(MENU + "1b. Crear escena de Intro", priority = 20)]
        public static void CrearEscenaIntro()
        {
            if (!Preparar(true)) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string carpeta = AsegurarCarpeta(Raiz(), "Escenas");
            string ruta = carpeta + "/Intro.unity";
            if (File.Exists(ruta) && !EditorUtility.DisplayDialog("Repercusión",
                    "Ya existe la escena Intro. ¿Reemplazarla?", "Reemplazar", "Cancelar"))
                return;

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CrearCamara();

            GameObject canvas = CrearCanvas("UI_Intro", 0);
            Transform c = canvas.transform;
            GameObject fondo = NuevoUI("Fondo", c);
            Estirar(fondo, 0f);
            Imagen(fondo, null, FONDO);
            FondoConCirculos(c, 0.3f, true);

            GameObject glitch = NuevoUI("Glitch", c);
            Estirar(glitch, 0f);
            for (int i = 0; i < 6; i++)
            {
                GameObject barra = NuevoUI("Barra" + (i + 1), glitch.transform);
                Imagen(barra, null, new Color(1f, 1f, 1f, 0.6f));
                GlitchUI g = barra.AddComponent<GlitchUI>();
                g.intervalo = new Vector2(0.03f, 0.12f);
            }
            glitch.SetActive(false); // la intro lo prende en "Y el tiempo se rompió."

            GameObject contenido = NuevoUI("Contenido", c);
            Estirar(contenido, 0f);
            CanvasGroup grupo = contenido.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.blocksRaycasts = false;

            GameObject img = NuevoUI("Imagen", contenido.transform);
            Colocar(img, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 90f), new Vector2(1000f, 560f));
            UnityEngine.UI.Image imagen = Imagen(img, null, Color.white);
            imagen.preserveAspect = true;
            img.SetActive(false);

            TextMeshProUGUI texto = Texto(contenido.transform, "Texto", "", 62f, BLANCO, TextAlignmentOptions.Center, false, true);
            Colocar(texto.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(1500f, 260f));
            texto.lineSpacing = 6f;

            GameObject flashGO = NuevoUI("Flash", c);
            Estirar(flashGO, 0f);
            Imagen(flashGO, null, Color.white);
            CanvasGroup flash = flashGO.AddComponent<CanvasGroup>();
            flash.alpha = 0f;
            flash.blocksRaycasts = false;

            TextoAbajo(c, "Pista", "CLIC O ESPACIO: CONTINUAR   ·   ESC: SALTAR", 16f, GRIS_OSCURO, false);

            IntroSecuencia intro = canvas.AddComponent<IntroSecuencia>();
            intro.grupo = grupo;
            intro.texto = texto;
            intro.imagen = imagen;
            intro.flash = flash;
            intro.glitch = glitch;
            EditorUtility.SetDirty(intro);

            TraducirEscena(escena);
            EditorSceneManager.SaveScene(escena, ruta);
            AgregarAlBuild(ruta, INTRO_ESCENA);
            EditorUtility.DisplayDialog("Repercusión",
                "Listo: escena Intro creada (va después del menú).\n\n" +
                "Los textos se cambian en el objeto UI_Intro, componente Intro Secuencia.\n\n" +
                "Ahora abre tu escena del juego y usa el paso 2.", "OK");
        }

        // =====================================================================
        // 2. HUD + PAUSA + GAMEMANAGER
        // =====================================================================

        [MenuItem(MENU + "2. Agregar HUD, pausa y GameManager a esta escena", priority = 21)]
        public static void CrearHUD()
        {
            if (!Preparar(true)) return;

            if (Object.FindAnyObjectByType<HUD>() != null &&
                !EditorUtility.DisplayDialog("Repercusión", "Esta escena ya tiene un HUD. ¿Crear otro?", "Crear otro", "Cancelar"))
                return;

            AsegurarEventSystem();
            ConstruirHUD();
            string avisoEco = ConfigurarJugadorYCamara();

            Scene escena = SceneManager.GetActiveScene();
            TraducirEscena(escena);
            EditorSceneManager.MarkSceneDirty(escena);

            string avisoBuild;
            if (string.IsNullOrEmpty(escena.path))
            {
                avisoBuild = "La escena no está guardada: guárdala (Ctrl+S) y agrégala en File > Build Profiles > Scene List.";
            }
            else
            {
                AgregarAlBuild(escena.path, JUEGO_ESCENA);
                avisoBuild = "La escena quedó en la lista de Build.";
            }

            EditorUtility.DisplayDialog("Repercusión", "HUD y pausa listos.\n\n" + avisoEco + "\n\n" + avisoBuild +
                                        "\n\nGuarda la escena con Ctrl+S.", "OK");
        }

        private static void ConstruirHUD()
        {
            GameObject canvas = CrearCanvas("UI_Juego", 10);
            Transform c = canvas.transform;
            HUD hud = canvas.AddComponent<HUD>();

            // ---- Luz (arriba a la izquierda): etiqueta + línea fina
            TextoArribaIzq(c, "EtiquetaLuz", "LUZ", 18f, GRIS, 72f, 56f, 300f, 28f, ESPACIADO);
            GameObject fondoBarra = NuevoUI("FondoBarra", c);
            Colocar(fondoBarra, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(72f, -96f), new Vector2(380f, 4f));
            Imagen(fondoBarra, null, new Color(1f, 1f, 1f, 0.15f));
            GameObject relleno = NuevoUI("Relleno", fondoBarra.transform);
            Estirar(relleno, 0f);
            UnityEngine.UI.Image barra = Imagen(relleno, S("ui_barra_relleno"), BLANCO);
            barra.type = UnityEngine.UI.Image.Type.Filled;
            barra.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            barra.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            barra.fillAmount = 1f;

            // ---- Llaves (arriba a la derecha)
            TextMeshProUGUI etiquetaLlaves = TextoArribaIzq(c, "EtiquetaLlaves", "LLAVES", 18f, GRIS, 0f, 56f, 300f, 28f, ESPACIADO);
            Colocar(etiquetaLlaves.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-72f, -56f), new Vector2(300f, 28f));
            etiquetaLlaves.alignment = TextAlignmentOptions.TopRight;

            GameObject fila = NuevoUI("Iconos", c);
            Colocar(fila, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-66f, -90f), new Vector2(300f, 44f));
            UnityEngine.UI.HorizontalLayoutGroup h = fila.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleRight;
            h.childControlWidth = false;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            UnityEngine.UI.Image[] llaves = new UnityEngine.UI.Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject l = NuevoUI("Llave" + (i + 1), fila.transform);
                ((RectTransform)l.transform).sizeDelta = new Vector2(52f, 44f);
                llaves[i] = Imagen(l, S("ui_icono_llave"), new Color(1f, 1f, 1f, 0.18f));
                llaves[i].preserveAspect = true;
            }

            // ---- Ecos (abajo a la izquierda, solo dentro de una sala)
            GameObject panelEcos = NuevoUI("PanelEcos", c);
            Colocar(panelEcos, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(72f, 56f), new Vector2(600f, 80f));
            TextMeshProUGUI textoEcos = TextoArribaIzq(panelEcos.transform, "TextoEcos", "ECOS  0/1", 24f, BLANCO, 0f, 0f, 600f, 34f, 12f);
            TextoArribaIzq(panelEcos.transform, "Pista", "E  —  DEJAR ECO", 16f, GRIS, 0f, 44f, 600f, 26f, ESPACIADO);
            panelEcos.SetActive(false);

            // ---- Pista de pausa (abajo a la derecha)
            TextoAbajo(c, "PistaPausa", "ESC  —  PAUSA", 16f, GRIS_OSCURO, false);

            // ---- Mensajes (arriba al centro)
            GameObject mensaje = NuevoUI("Mensaje", c);
            Colocar(mensaje, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -150f), new Vector2(1300f, 84f));
            Imagen(mensaje, null, new Color(0.024f, 0.024f, 0.024f, 0.75f));
            CanvasGroup grupo = mensaje.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.interactable = false;
            grupo.blocksRaycasts = false;
            GameObject lineaArriba = NuevoUI("LineaArriba", mensaje.transform);
            Colocar(lineaArriba, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 1f));
            Imagen(lineaArriba, null, new Color(1f, 1f, 1f, 0.25f));
            GameObject lineaAbajo = NuevoUI("LineaAbajo", mensaje.transform);
            Colocar(lineaAbajo, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 1f));
            Imagen(lineaAbajo, null, new Color(1f, 1f, 1f, 0.25f));
            TextMeshProUGUI textoMensaje = Texto(mensaje.transform, "Texto", "Sala 1  ·  El primer eco", 40f, BLANCO,
                                                 TextAlignmentOptions.Center, false, true);
            Estirar(textoMensaje.gameObject, 8f);

            hud.barraLuz = barra;
            hud.iconoLuz = null;
            hud.iconosLlave = llaves;
            hud.panelEcos = panelEcos;
            hud.textoEcos = textoEcos;
            hud.grupoMensaje = grupo;
            hud.textoMensaje = textoMensaje;
            EditorUtility.SetDirty(hud);

            // ---- Pausa (mismo estilo que el menú)
            MenuPausa pausa = canvas.AddComponent<MenuPausa>();
            GameObject capa = NuevoUI("Pausa", c);
            Estirar(capa, 0f);
            Imagen(capa, null, CAPA_OSCURA, true);
            FondoConCirculos(capa.transform, 0.5f, false);

            TextoArribaIzq(capa.transform, "Firma", "JUEGO EN PAUSA", 20f, GRIS, MARGEN, 146f, 900f, 30f, ESPACIADO);
            TextoArribaIzq(capa.transform, "Titulo", "Pausa", 140f, BLANCO, MARGEN - 4f, 178f, 1000f, 170f, 0f, true);

            UnityEngine.UI.Button continuar = Opcion(capa.transform, "OpcionContinuar", "01", "CONTINUAR", 544f);
            UnityEngine.UI.Button reiniciar = Opcion(capa.transform, "OpcionReiniciarSala", "02", "REINICIAR SALA", 628f);
            UnityEngine.UI.Button alMenu = Opcion(capa.transform, "OpcionMenu", "03", "MENÚ PRINCIPAL", 712f);
            UnityEventTools.AddPersistentListener(continuar.onClick, new UnityAction(pausa.Continuar));
            UnityEventTools.AddPersistentListener(reiniciar.onClick, new UnityAction(pausa.ReiniciarSala));
            UnityEventTools.AddPersistentListener(alMenu.onClick, new UnityAction(pausa.IrAlMenu));
            capa.SetActive(false);

            pausa.panelPausa = capa;
            pausa.primerBoton = continuar.gameObject;
            EditorUtility.SetDirty(pausa);

            Selection.activeGameObject = canvas;
        }

        private static string ConfigurarJugadorYCamara()
        {
            GameObject eco = BuscarEco();
            string aviso;
            if (eco == null)
            {
                aviso = "OJO: no encontré a Eco. Ponle el Tag \"Player\", selecciónala y usa " +
                        "\"Repercusión > Configurar a Eco (seleccionada)\".";
            }
            else
            {
                aviso = ConfigurarEco(eco);
            }

            if (Object.FindAnyObjectByType<GameManager>() == null)
            {
                GameObject gmGO = new GameObject("GameManager");
                Undo.RegisterCreatedObjectUndo(gmGO, "Crear GameManager");
                GameManager gm = gmGO.AddComponent<GameManager>();

                GameObject centro = new GameObject("PuntoCentro");
                Undo.RegisterCreatedObjectUndo(centro, "Crear PuntoCentro");
                centro.transform.position = eco != null ? eco.transform.position : Vector3.zero;
                gm.puntoCentro = centro.transform;
                EditorUtility.SetDirty(gm);
                aviso += "\n\nCreé GameManager y PuntoCentro (donde reaparece Eco). Mueve PuntoCentro al centro del mapa.";
            }

            Camera cam = Camera.main;
            bool tieneSeguimiento = false;
            if (cam != null)
            {
                foreach (MonoBehaviour mb in cam.GetComponents<MonoBehaviour>())
                    if (mb != null && mb.GetType().Name == "CameraFollow" && mb.enabled) tieneSeguimiento = true;
            }
            if (cam != null && !tieneSeguimiento)
            {
                CamaraTopDown seguir = cam.GetComponent<CamaraTopDown>();
                if (seguir == null) seguir = Undo.AddComponent<CamaraTopDown>(cam.gameObject);
                if (eco != null) seguir.objetivo = eco.transform;
                EditorUtility.SetDirty(seguir);

                foreach (MonoBehaviour mb in cam.GetComponents<MonoBehaviour>())
                {
                    if (mb != null && mb.GetType().Name == "CameraFollow" && mb.enabled)
                    {
                        Undo.RecordObject(mb, "Desactivar CameraFollow");
                        mb.enabled = false;
                        aviso += "\nDesactivé tu CameraFollow (solo seguía en X); ahora la cámara usa CamaraTopDown.";
                    }
                }
            }
            return aviso;
        }

        [MenuItem(MENU + "Configurar a Eco (seleccionada)", priority = 40)]
        public static void ConfigurarEcoSeleccionada()
        {
            GameObject eco = Selection.activeGameObject;
            if (eco == null)
            {
                EditorUtility.DisplayDialog("Repercusión", "Primero selecciona a Eco en la jerarquía.", "OK");
                return;
            }
            EditorUtility.DisplayDialog("Repercusión", ConfigurarEco(eco), "OK");
        }

        private static string ConfigurarEco(GameObject eco)
        {
            if (eco.GetComponent<LuzEco>() == null) Undo.AddComponent<LuzEco>(eco);
            if (eco.GetComponent<EcoGrabadora>() == null) Undo.AddComponent<EcoGrabadora>(eco);
            TraducirEscena(eco.scene);
            EditorSceneManager.MarkSceneDirty(eco.scene);

            string aviso = "Eco (" + eco.name + ") ya tiene LuzEco y EcoGrabadora.";
            foreach (MonoBehaviour mb in eco.GetComponents<MonoBehaviour>())
            {
                if (mb != null && mb.GetType().Name == "PlayerEnergy" && mb.enabled)
                {
                    Undo.RecordObject(mb, "Desactivar PlayerEnergy");
                    mb.enabled = false;
                    aviso += "\nDesactivé tu PlayerEnergy (ahora la luz la maneja LuzEco).";
                }
            }
            Rigidbody2D rb = eco.GetComponent<Rigidbody2D>();
            if (rb != null && rb.gravityScale != 0f)
            {
                Undo.RecordObject(rb, "Gravedad 0");
                rb.gravityScale = 0f;
                aviso += "\nPuse Gravity Scale en 0 (top-down).";
            }
            return aviso;
        }

        private static GameObject BuscarEco()
        {
            GameObject eco = GameObject.FindWithTag("Player");
            if (eco != null) return eco;
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>())
            {
                if (mb != null && mb.GetType().Name == "TopDownMovement") return mb.gameObject;
            }
            LuzEco luz = Object.FindAnyObjectByType<LuzEco>();
            return luz != null ? luz.gameObject : null;
        }

        // =====================================================================
        // 3. SALA 1
        // =====================================================================

        [MenuItem(MENU + "3. Crear Sala 1 (placa + puerta + llave)", priority = 22)]
        public static void CrearSala1()
        {
            Preparar(false);
            Orden orden = CalcularOrden();
            Vector3 centro = PosicionVista();

            GameObject raiz = new GameObject("Sala1_ElPrimerEco");
            Undo.RegisterCreatedObjectUndo(raiz, "Crear Sala 1");
            raiz.transform.position = centro;

            BoxCollider2D zona = raiz.AddComponent<BoxCollider2D>();
            zona.isTrigger = true;
            zona.size = new Vector2(12f, 8f);
            SalaPuzle sala = raiz.AddComponent<SalaPuzle>();
            sala.nombreSala = "Sala 1";
            sala.subtitulo = "El primer eco";
            sala.maxEcos = 1;

            GameObject inicio = Hijo(raiz, "PuntoInicio", new Vector2(-4.5f, 0f));
            sala.puntoInicio = inicio.transform;

            // Placa
            GameObject placaGO = Hijo(raiz, "Placa", new Vector2(-1.5f, 2f));
            SpriteRenderer placaSR = Dibujo(placaGO, "mundo_placa_suelta", orden, orden.suelo);
            BoxCollider2D placaCol = placaGO.AddComponent<BoxCollider2D>();
            placaCol.isTrigger = true;
            placaCol.size = new Vector2(0.8f, 0.8f);
            PlacaPresion placa = placaGO.AddComponent<PlacaPresion>();
            placa.visual = placaSR;
            placa.spriteSuelta = S("mundo_placa_suelta");
            placa.spritePresionada = S("mundo_placa_presionada");

            // Puerta (vertical, 1 x 3 casillas)
            GameObject puertaGO = Hijo(raiz, "Puerta", new Vector2(2.5f, 0f));
            SpriteRenderer puertaSR = Dibujo(puertaGO, "mundo_puerta_cerrada", orden, orden.objetos);
            BoxCollider2D puertaCol = puertaGO.AddComponent<BoxCollider2D>();
            puertaCol.size = new Vector2(0.8f, 3f);
            Puerta puerta = puertaGO.AddComponent<Puerta>();
            puerta.placas = new[] { placa };
            puerta.bloqueo = puertaCol;
            puerta.visual = puertaSR;
            puerta.spriteCerrada = S("mundo_puerta_cerrada");
            puerta.spriteAbierta = S("mundo_puerta_abierta");
            puerta.colorAbierta = Color.white;

            // Pista antes de la puerta (desactívala para las pruebas "sin ayuda")
            GameObject pista = Hijo(raiz, "Pista", new Vector2(1.2f, 0f));
            BoxCollider2D pistaCol = pista.AddComponent<BoxCollider2D>();
            pistaCol.isTrigger = true;
            pistaCol.size = new Vector2(1.2f, 3f);
            ZonaMensaje zm = pista.AddComponent<ZonaMensaje>();
            zm.mensaje = "La puerta se cierra si nadie pisa la placa... ¿y si te ayudas a ti misma? (R)";

            // Llave detrás de la puerta
            CrearLlave(raiz, new Vector2(4.5f, 0f), orden);

            Selection.activeGameObject = raiz;
            TraducirEscena(raiz.scene);
            EditorSceneManager.MarkSceneDirty(raiz.scene);
            EditorUtility.DisplayDialog("Repercusión",
                "Sala 1 creada donde estás mirando en la vista Scene.\n\n" +
                "1. Muévela a su lugar en el mapa.\n" +
                "2. Ajusta el área (Box Collider 2D del objeto Sala1, botón Edit Collider) para que cubra toda la sala.\n" +
                "3. Mueve la Puerta para que tape el paso hacia la llave, y PuntoInicio a la entrada.", "OK");
        }

        private static void CrearLlave(GameObject padre, Vector2 local, Orden orden)
        {
            GameObject llaveGO = Hijo(padre, "Llave", local);
            Dibujo(llaveGO, "mundo_llave", orden, orden.objetos);
            llaveGO.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            CircleCollider2D col = llaveGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;
            llaveGO.AddComponent<Llave>();
        }

        // Sala 1 en el cuarto de arriba a la izquierda del mapa de Demo (x -12..21, y 38..51).
        // Un muro parte el cuarto: abajo la entrada, dos braseros y cuatro placas; arriba la llave.
        // ACERTIJO: la reja pide las dos placas que están lejos del fuego (en la sombra).
        // Las dos que están junto a los braseros son trampa: quitan luz y traban la reja.
        // Necesitas dos ecos (uno en cada placa) para cruzar tú. La oscuridad gasta tu luz;
        // los braseros la recargan. Si se apaga, vuelves a la entrada y tus ecos desaparecen.
        [MenuItem(MENU + "3b. Crear Sala 1 en el cuarto de arriba a la izquierda", priority = 22)]
        public static void CrearSala1ArribaIzquierda()
        {
            Preparar(false);
            Orden orden = CalcularOrden();
            int ordenMuro = OrdenParedes(orden);

            GameObject vieja = Buscar("Sala1_ElPrimerEco");
            if (vieja != null) Undo.DestroyObjectImmediate(vieja);

            Vector3 centro = new Vector3(4.5f, 44.5f, 0f);
            GameObject raiz = new GameObject("Sala1_ElPrimerEco");
            Undo.RegisterCreatedObjectUndo(raiz, "Crear Sala 1");
            raiz.transform.position = centro;

            BoxCollider2D zona = raiz.AddComponent<BoxCollider2D>();
            zona.isTrigger = true;
            zona.size = new Vector2(33f, 13f);
            SalaPuzle sala = raiz.AddComponent<SalaPuzle>();
            sala.nombreSala = "Sala 1";
            sala.subtitulo = "El primer eco";
            sala.maxEcos = 4;
            sala.ecosQuietos = true;
            sala.ecosAutomaticos = true;   // al pisar una losa, el eco se queda solo
            sala.vidaEco = 30f;            // ...pero se desvanece a los 30 s: hay que darse prisa

            System.Func<float, float, Vector2> L = (x, y) => new Vector2(x - centro.x, y - centro.y);

            GameObject inicio = Hijo(raiz, "PuntoInicio", L(17f, 40f));
            sala.puntoInicio = inicio.transform;

            // Muro que parte el cuarto, con hueco para la reja en x -7..-4
            Muro(raiz, "Muro_Izquierdo", -12f, -7f, 44f, orden, ordenMuro, L);
            Muro(raiz, "Muro_Derecho", -4f, 21f, 44f, orden, ordenMuro, L);

            // La oscuridad de la sala gasta la luz poco a poco
            GameObject osc = Hijo(raiz, "Oscuridad", Vector2.zero);
            BoxCollider2D oscCol = osc.AddComponent<BoxCollider2D>();
            oscCol.isTrigger = true;
            oscCol.size = new Vector2(33f, 13f);
            ZonaPeligro oscuridad = osc.AddComponent<ZonaPeligro>();
            oscuridad.golpeAlEntrar = 0f;
            oscuridad.perdidaPorSegundo = 2f;

            // Braseros: recargan la luz
            Brasero(raiz, "Brasero_Entrada", L(12.5f, 39.5f), orden);
            Brasero(raiz, "Brasero_Reja", L(-10.5f, 39.5f), orden);

            // Cuatro losas con símbolos. Al pisar una, tu eco se queda en ella.
            // La reja pide las cuatro en el orden de la tablilla: reloj, llama, ojo, luna.
            PlacaPresion losaReloj = Placa(raiz, "Losa_Reloj", "reloj", L(-9f, 42.5f), orden, ordenMuro + 1, false);
            PlacaPresion losaLuna = Placa(raiz, "Losa_Luna", "luna", L(9.5f, 42.5f), orden, ordenMuro + 1, false);
            PlacaPresion losaOjo = Placa(raiz, "Losa_Ojo", "ojo", L(19.5f, 42.5f), orden, ordenMuro + 1, false);
            PlacaPresion losaLlama = Placa(raiz, "Losa_Llama", "llama", L(1.5f, 39.5f), orden, ordenMuro + 1, false);

            // Mensaje de inicio en español
            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                Undo.RecordObject(gm, "Mensaje inicial");
                gm.mensajeInicial = "La salida está sellada. Encuentra las 3 llaves";
                EditorUtility.SetDirty(gm);
            }

            // Reja
            GameObject puertaGO = Hijo(raiz, "Reja", L(-5.5f, 45.5f));
            SpriteRenderer puertaSR = Dibujo(puertaGO, "px_puerta_cerrada", orden, ordenMuro);
            BoxCollider2D puertaCol = puertaGO.AddComponent<BoxCollider2D>();
            puertaCol.size = new Vector2(3f, 3f);
            Puerta puerta = puertaGO.AddComponent<Puerta>();
            puerta.placas = new[] { losaReloj, losaLlama, losaOjo, losaLuna };
            puerta.enOrden = true;
            puerta.placasProhibidas = new PlacaPresion[0];
            puerta.quedarseAbierta = true;   // una vez resuelto, la reja ya no se cierra

            LosasEnOrden enOrden = raiz.AddComponent<LosasEnOrden>();
            enOrden.losas = new[] { losaReloj, losaLlama, losaOjo, losaLuna };
            enOrden.reja = puerta;
            enOrden.fuenteSusurro = Fuente("Titulo");
            puerta.bloqueo = puertaCol;
            puerta.visual = puertaSR;
            puerta.spriteCerrada = S("px_puerta_cerrada");
            puerta.spriteAbierta = S("px_puerta_abierta");
            puerta.colorAbierta = Color.white;
            Luz(puertaGO, "Luz", new Vector2(0f, -1.2f), new Color(0.8f, 0.9f, 0.95f), 0.35f, 3f);

            // Tablilla con el acertijo, colgada en la cara del muro (y 44..46), entre la luna y el ojo.
            // Ya no está en el piso: el muro no deja pasar, así que no estorba el camino.
            // Brilla y late en verde para que se note que ahí está la clave.
            GameObject tabla = Hijo(raiz, "Inscripcion", L(14.5f, 45.2f));
            SpriteRenderer tablaSR = Dibujo(tabla, "px_inscripcion", orden, ordenMuro + 1);
            GameObject tablaLuz = Luz(tabla, "Luz", new Vector2(0f, -0.4f), new Color(0.45f, 1f, 0.9f), 1.2f, 3.2f);
            Fuego latido = tabla.AddComponent<Fuego>();   // la luz late suave para que se note
            latido.render = tablaSR;
            latido.cuadros = new Sprite[0];
            latido.luz = tablaLuz.GetComponent<Light2D>();
            latido.radioBase = 3.2f;
            latido.intensidadBase = 1.2f;

            // Halo verde detrás de la tablilla: un resplandor sobre la piedra del muro
            GameObject halo = Hijo(tabla, "Halo", Vector2.zero);
            SpriteRenderer haloSR = Dibujo(halo, "px_inscripcion", orden, ordenMuro);
            haloSR.color = new Color(0.45f, 1f, 0.9f, 0.35f);
            halo.transform.localScale = new Vector3(1.35f, 1.25f, 1f);
            Luz(tabla, "Resplandor", Vector2.zero, new Color(0.45f, 1f, 0.9f), 0.6f, 1.4f);

            // Texto flotante: el acertijo aparece sobre la tablilla cuando te acercas (sin tecla)
            GameObject aviso = Hijo(tabla, "Aviso", new Vector2(4.4f, 0.2f));   // a la derecha, sobre el muro
            TextMeshPro avisoTxt = aviso.AddComponent<TextMeshPro>();
            TMP_FontAsset titulo = Fuente("Titulo");
            if (titulo != null) avisoTxt.font = titulo;
            avisoTxt.text = "";
            avisoTxt.fontSize = 2.6f;
            avisoTxt.lineSpacing = 0f;
            avisoTxt.alignment = TextAlignmentOptions.Left;
            avisoTxt.color = new Color(0.8f, 1f, 0.96f);
            avisoTxt.rectTransform.sizeDelta = new Vector2(6.6f, 3f);
            avisoTxt.sortingLayerID = orden.capa;
            avisoTxt.sortingOrder = orden.objetos + 5;

            Tablilla tablilla = tabla.AddComponent<Tablilla>();
            tablilla.titulo = "Tablilla de piedra";
            tablilla.texto = "«Primero, lo que cuenta el tiempo perdido.\nDespués, lo que arde en la oscuridad.\n" +
                             "Luego, lo que todo lo mira.\nAl final, lo que brilla sin quemar.»";
            tablilla.simbolos = new[] { S("px_losa_luna"), S("px_losa_ojo"), S("px_losa_llama"), S("px_losa_reloj") };
            tablilla.marcador = null;   // ahora la señala la flecha guía
            tablilla.aviso = aviso;

            CrearPanelLectura();

            // Pistas
            Pista(raiz, "Pista_Reja", L(-5.5f, 41.5f), new Vector2(3f, 3f),
                "La reja pide las cuatro losas, en el orden de la tablilla", true, 4f);

            // Guía: instrucciones paso a paso mientras juegas la sala
            GuiaSala guia = raiz.AddComponent<GuiaSala>();
            guia.placasBuenas = new PlacaPresion[0];

            // El texto de ayuda del HUD: aquí no se usa la E
            GameObject uiJuego = Buscar("UI_Juego");
            if (uiJuego != null)
            {
                foreach (TMP_Text t in uiJuego.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (!t.text.Contains("DEJAR ECO") && !t.text.Contains("NUEVO INTENTO") && !t.text.Contains("PISA UNA LOSA")) continue;
                    Undo.RecordObject(t, "Ayuda de ecos");
                    t.text = "PISA UNA LOSA  —  TU ECO SE QUEDA UN RATO";
                    EditorUtility.SetDirty(t);
                }
            }
            guia.reja = puerta;

            // Llave arriba, detrás de la reja
            CrearLlave(raiz, L(-5.5f, 49f), orden);
            Transform llave = raiz.transform.Find("Llave");
            if (llave != null)
            {
                llave.localScale = new Vector3(1.4f, 1.4f, 1f);
                Luz(llave.gameObject, "Luz", Vector2.zero, new Color(1f, 0.85f, 0.5f), 0.8f, 2.2f);
            }

            // Flecha guía: señala la tablilla, luego la siguiente losa (tras el primer error) y luego la llave.
            // Las losas libres laten con luz verde para que se note que hay que ir ahí.
            GameObject flechaGO = Hijo(raiz, "GuideArrow", L(14.5f, 47f));
            Dibujo(flechaGO, "px_flecha", orden, orden.objetos + 6);
            flechaGO.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            Luz(flechaGO, "Luz", Vector2.zero, new Color(0.45f, 1f, 0.9f), 0.8f, 1.6f);
            FlechaGuia guiaFlecha = raiz.AddComponent<FlechaGuia>();
            guiaFlecha.tablilla = tablilla;
            guiaFlecha.losas = new[] { losaReloj, losaLlama, losaOjo, losaLuna };
            guiaFlecha.reja = puerta;
            guiaFlecha.llave = llave;
            guiaFlecha.losasEnOrden = enOrden;
            guiaFlecha.flecha = flechaGO.transform;
            guiaFlecha.mostrarOrdenSiempre = false;

            // Eco: pisa con los pies, y su eco repite los últimos 12 s
            GameObject ecoJugador = BuscarEco();
            Pisador pisEco = ecoJugador != null ? ecoJugador.GetComponent<Pisador>() : null;
            if (pisEco != null)
            {
                Undo.RecordObject(pisEco, "Pies de Eco");
                pisEco.offsetPies = new Vector2(0f, -0.9f);
                EditorUtility.SetDirty(pisEco);
            }
            EcoGrabadora grab = ecoJugador != null ? ecoJugador.GetComponent<EcoGrabadora>() : null;
            if (grab != null)
            {
                Undo.RecordObject(grab, "Duración del eco");
                grab.duracionMaxima = 12f;
                EditorUtility.SetDirty(grab);
            }

            // Vista 3/4: Eco choca con los pies, no con todo el cuerpo
            // (así puede pararse pegada a las paredes y pasar junto a braseros)
            if (ecoJugador != null)
            {
                foreach (BoxCollider2D bc in ecoJugador.GetComponents<BoxCollider2D>())
                {
                    if (bc.isTrigger) continue;
                    Undo.RecordObject(bc, "Colisión de Eco");
                    bc.size = new Vector2(0.7f, 0.5f);
                    bc.offset = new Vector2(0f, -0.85f);
                    EditorUtility.SetDirty(bc);
                    break;
                }
            }

            AsegurarOscuridad(ecoJugador);

            Selection.activeGameObject = raiz;
            SceneView vista = SceneView.lastActiveSceneView;
            if (vista != null) vista.Frame(new Bounds(centro, new Vector3(36f, 16f, 1f)), false);
            TraducirEscena(raiz.scene);
            EditorSceneManager.MarkSceneDirty(raiz.scene);
        }

        private static PlacaPresion Placa(GameObject raiz, string nombre, string simbolo, Vector2 local, Orden orden, int ordenEnCapa, bool trampa)
        {
            GameObject go = Hijo(raiz, nombre, local);
            SpriteRenderer sr = Dibujo(go, "px_losa_" + simbolo, orden, ordenEnCapa);
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 1.6f);
            PlacaPresion placa = go.AddComponent<PlacaPresion>();
            placa.visual = sr;
            placa.spriteSuelta = S("px_losa_" + simbolo);
            placa.spritePresionada = S("px_losa_" + simbolo + "_on");
            GameObject brillo = Luz(go, "Brillo", Vector2.zero, new Color(0.45f, 1f, 0.9f), 0.9f, 1.8f);
            brillo.SetActive(false);
            placa.encenderAlPresionar = brillo;
            if (trampa) go.AddComponent<PlacaTrampa>().mensaje = "Esa losa mentía…";
            return placa;
        }

        private static void Brasero(GameObject raiz, string nombre, Vector2 local, Orden orden)
        {
            GameObject go = Hijo(raiz, nombre, local);
            SpriteRenderer sr = Dibujo(go, "px_brasero_0", orden, orden.objetos);
            CircleCollider2D area = go.AddComponent<CircleCollider2D>();
            area.isTrigger = true;
            area.offset = new Vector2(0f, -0.5f);
            area.radius = 2.4f;
            FuenteDeLuz f = go.AddComponent<FuenteDeLuz>();
            f.tipo = FuenteDeLuz.Tipo.Brasero;
            f.recargaPorSegundo = 30f;

            GameObject luzGO = Luz(go, "Luz", new Vector2(0f, 0.4f), new Color(1f, 0.82f, 0.6f), 1.1f, 4.5f);
            Fuego fuego = go.AddComponent<Fuego>();
            fuego.render = sr;
            fuego.cuadros = new[] { S("px_brasero_0"), S("px_brasero_1"), S("px_brasero_2") };
            fuego.luz = luzGO.GetComponent<Light2D>();
            fuego.radioBase = 4.5f;
            fuego.intensidadBase = 1.1f;
        }

        private static GameObject Luz(GameObject padre, string nombre, Vector2 local, Color color, float intensidad, float radio)
        {
            GameObject go = Hijo(padre, nombre, local);
            Light2D l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color;
            l.intensity = intensidad;
            l.pointLightOuterRadius = radio;
            l.pointLightInnerRadius = radio * 0.2f;
            return go;
        }

        // Oscuridad general del nivel + la luz que Eco lleva encima
        private static void AsegurarOscuridad(GameObject eco)
        {
            GameObject g = Buscar("Oscuridad_Global");
            if (g == null)
            {
                g = new GameObject("Oscuridad_Global");
                Undo.RegisterCreatedObjectUndo(g, "Oscuridad");
                Light2D lg = g.AddComponent<Light2D>();
                lg.lightType = Light2D.LightType.Global;
            }
            Light2D global = g.GetComponent<Light2D>();
            if (global != null)
            {
                Undo.RecordObject(global, "Oscuridad");
                global.color = new Color(0.75f, 0.85f, 0.9f);
                global.intensity = 0.1f;
            }
            if (eco != null && eco.transform.Find("LuzDeEco") == null && eco.transform.Find(EnIngles("LuzDeEco")) == null)
            {
                GameObject l = new GameObject("LuzDeEco");
                Undo.RegisterCreatedObjectUndo(l, "Luz de Eco");
                l.transform.SetParent(eco.transform, false);
                l.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                Light2D le = l.AddComponent<Light2D>();
                le.lightType = Light2D.LightType.Point;
                le.color = new Color(1f, 0.97f, 0.92f);
                le.intensity = 1f;
                le.pointLightOuterRadius = 7f;
                le.pointLightInnerRadius = 2f;
                l.AddComponent<LuzVisualEco>();
            }
            LuzVisualEco lv = eco != null ? eco.GetComponentInChildren<LuzVisualEco>() : null;
            if (lv != null)
            {
                Undo.RecordObject(lv, "Luz de Eco");
                lv.intensidad = 1.25f;
                lv.radioMaximo = 7.5f;
            }
        }

        // Panel para leer tablillas, dentro de la UI del juego (UI_Juego)
        private static void CrearPanelLectura()
        {
            GameObject ui = Buscar("UI_Juego");
            if (ui == null) return;
            Transform viejo = ui.transform.Find("PanelLectura");
            if (viejo == null) viejo = ui.transform.Find(EnIngles("PanelLectura"));
            if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);

            GameObject capa = NuevoUI("PanelLectura", ui.transform);
            Undo.RegisterCreatedObjectUndo(capa, "Panel de lectura");
            Estirar(capa, 0f);
            Imagen(capa, null, CAPA_OSCURA, true);
            FondoConCirculos(capa.transform, 0.35f, false);
            CanvasGroup grupo = capa.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.blocksRaycasts = false;
            grupo.interactable = false;

            TextMeshProUGUI etiqueta = Texto(capa.transform, "Firma", "TABLILLA DE PIEDRA", 20f, GRIS, TextAlignmentOptions.Center);
            etiqueta.characterSpacing = ESPACIADO;
            Colocar(etiqueta.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 300f), new Vector2(900f, 40f));

            GameObject linea = NuevoUI("Linea", capa.transform);
            Colocar(linea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 262f), new Vector2(120f, 1f));
            Imagen(linea, null, new Color(1f, 1f, 1f, 0.35f));

            TextMeshProUGUI cuerpo = Texto(capa.transform, "Texto", "", 50f, BLANCO, TextAlignmentOptions.Center, false, true);
            cuerpo.lineSpacing = 18f;
            Colocar(cuerpo.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 40f), new Vector2(1400f, 400f));

            GameObject fila = NuevoUI("Simbolos", capa.transform);
            Colocar(fila, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -250f), new Vector2(700f, 110f));
            UnityEngine.UI.Image[] iconos = new UnityEngine.UI.Image[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject ic = NuevoUI("Simbolo" + (i + 1), fila.transform);
                Colocar(ic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(-240f + i * 160f, 0f), new Vector2(96f, 96f));
                iconos[i] = Imagen(ic, null, Color.white);
                iconos[i].preserveAspect = true;
            }

            TextMeshProUGUI pie = Texto(capa.transform, "Pie", "CUALQUIER TECLA  —  CERRAR", 18f, GRIS_OSCURO, TextAlignmentOptions.Center);
            pie.characterSpacing = ESPACIADO;
            Colocar(pie.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -390f), new Vector2(900f, 40f));

            PanelLectura panel = capa.AddComponent<PanelLectura>();
            panel.grupo = grupo;
            panel.titulo = etiqueta;
            panel.texto = cuerpo;
            panel.simbolos = iconos;
            panel.pie = pie;
            panel.cierreAutomatico = 15f;
            EditorUtility.SetDirty(panel);
        }

        // Muro horizontal de x0 a x1 (mundo), cara de ladrillo de 2 casillas + tope de 1, con colisión
        private static void Muro(GameObject raiz, string nombre, float x0, float x1, float yBase,
                                 Orden orden, int ordenMuro, System.Func<float, float, Vector2> L)
        {
            float ancho = x1 - x0;
            GameObject muro = Hijo(raiz, nombre, L((x0 + x1) / 2f, yBase + 1.5f));
            BoxCollider2D col = muro.AddComponent<BoxCollider2D>();
            col.size = new Vector2(ancho, 3f);

            GameObject cara = Hijo(muro, "Cara", new Vector2(0f, -0.5f));
            SpriteRenderer sc = Dibujo(cara, "px_pared_cara", orden, ordenMuro);
            sc.drawMode = SpriteDrawMode.Tiled;
            sc.size = new Vector2(ancho, 2f);

            GameObject tope = Hijo(muro, "Tope", new Vector2(0f, 1f));
            SpriteRenderer st = Dibujo(tope, "px_pared_tope", orden, ordenMuro);
            st.drawMode = SpriteDrawMode.Tiled;
            st.size = new Vector2(ancho, 1f);
        }

        private static void Pista(GameObject raiz, string nombre, Vector2 local, Vector2 tam, string texto,
                                  bool soloUnaVez = true, float duracion = 0f)
        {
            GameObject go = Hijo(raiz, nombre, local);
            BoxCollider2D c = go.AddComponent<BoxCollider2D>();
            c.isTrigger = true;
            c.size = tam;
            ZonaMensaje zm = go.AddComponent<ZonaMensaje>();
            zm.mensaje = texto;
            zm.soloUnaVez = soloUnaVez;
            zm.duracion = duracion;
        }

        // Paredes y objetos del piso van justo debajo de Eco; si el suelo queda empatado, lo baja uno.
        private static int OrdenParedes(Orden orden)
        {
            // Suelo < paredes < cosas del piso (placas, alfombras, estandartes) < Eco
            int muro = orden.objetos - 2;
            foreach (TilemapRenderer r in Object.FindObjectsByType<TilemapRenderer>())
            {
                if (r.sortingLayerID != orden.capa) continue;
                if (EsPared(r.gameObject))
                {
                    if (r.sortingOrder != muro) { Undo.RecordObject(r, "Orden de paredes"); r.sortingOrder = muro; }
                    continue;
                }
                if (r.sortingOrder >= muro)
                {
                    Undo.RecordObject(r, "Orden del suelo");
                    r.sortingOrder = muro - 1;
                }
            }
            return muro;
        }

        // =====================================================================
        // 6. PAREDES (colisión invisible alrededor del suelo)
        // =====================================================================

        [MenuItem(MENU + "6. Crear paredes del mapa (colisión)", priority = 25)]
        public static void CrearParedes()
        {
            Tilemap suelo = null;
            foreach (Tilemap t in Object.FindObjectsByType<Tilemap>())
                if (!EsPared(t.gameObject)) { suelo = t; break; }
            if (suelo == null)
            {
                EditorUtility.DisplayDialog("Repercusión", "No encontré el Tilemap del suelo en esta escena.", "OK");
                return;
            }

            Transform padre = suelo.transform.parent;
            GameObject viejo = Buscar("Paredes_Colision");
            if (viejo != null) Undo.DestroyObjectImmediate(viejo);

            GameObject go = new GameObject("Paredes_Colision");
            Undo.RegisterCreatedObjectUndo(go, "Crear paredes");
            go.transform.SetParent(padre, false);
            go.transform.localPosition = suelo.transform.localPosition;

            Tilemap tm = go.AddComponent<Tilemap>();
            TilemapRenderer tr = go.AddComponent<TilemapRenderer>();
            tr.enabled = false; // invisible: solo choca
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            TilemapCollider2D tc = go.AddComponent<TilemapCollider2D>();
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;
            go.AddComponent<CompositeCollider2D>();

            Tile pared = TilePared();
            suelo.CompressBounds();
            BoundsInt b = suelo.cellBounds;
            List<Vector3Int> celdas = new List<Vector3Int>();
            for (int x = b.xMin - 1; x <= b.xMax; x++)
            {
                for (int y = b.yMin - 1; y <= b.yMax; y++)
                {
                    Vector3Int p = new Vector3Int(x, y, 0);
                    if (suelo.HasTile(p)) continue;
                    bool bordea = false;
                    for (int dx = -1; dx <= 1 && !bordea; dx++)
                        for (int dy = -1; dy <= 1 && !bordea; dy++)
                            if ((dx != 0 || dy != 0) && suelo.HasTile(new Vector3Int(x + dx, y + dy, 0)))
                                bordea = true;
                    if (bordea) celdas.Add(p);
                }
            }
            TileBase[] tiles = new TileBase[celdas.Count];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = pared;
            tm.SetTiles(celdas.ToArray(), tiles);

            // Dibujo de las paredes (misma estética que el suelo): cara de ladrillo arriba de cada cuarto,
            // tope oscuro en los costados y abajo.
            Preparar(false);
            Orden orden = CalcularOrden();
            int ordenMuro = OrdenParedes(orden);
            GameObject viejoDibujo = Buscar("Paredes_Dibujo");
            if (viejoDibujo != null) Undo.DestroyObjectImmediate(viejoDibujo);
            GameObject dib = new GameObject("Paredes_Dibujo");
            Undo.RegisterCreatedObjectUndo(dib, "Dibujar paredes");
            dib.transform.SetParent(padre, false);
            dib.transform.localPosition = suelo.transform.localPosition;
            Tilemap tmDib = dib.AddComponent<Tilemap>();
            TilemapRenderer trDib = dib.AddComponent<TilemapRenderer>();
            trDib.sortingLayerID = orden.capa;
            trDib.sortingOrder = ordenMuro;

            Tile tAlta = TileDibujo("Pared_Alta", "px_pared_alta");
            Tile tBaja = TileDibujo("Pared_Baja", "px_pared_baja");
            Tile tTope = TileDibujo("Pared_Tope", "px_pared_tope");
            List<Vector3Int> pos = new List<Vector3Int>();
            List<TileBase> tls = new List<TileBase>();
            for (int x = b.xMin - 2; x <= b.xMax + 1; x++)
            {
                for (int y = b.yMin - 2; y <= b.yMax + 3; y++)
                {
                    Vector3Int p = new Vector3Int(x, y, 0);
                    if (suelo.HasTile(p)) continue;
                    TileBase t = null;
                    if (suelo.HasTile(new Vector3Int(x, y - 1, 0))) t = tBaja;
                    else if (suelo.HasTile(new Vector3Int(x, y - 2, 0))) t = tAlta;
                    else
                    {
                        for (int dx = -1; dx <= 1 && t == null; dx++)
                            for (int dy = -3; dy <= 1 && t == null; dy++)
                                if (suelo.HasTile(new Vector3Int(x + dx, y + dy, 0))) t = tTope;
                    }
                    if (t != null) { pos.Add(p); tls.Add(t); }
                }
            }
            tmDib.SetTiles(pos.ToArray(), tls.ToArray());

            Selection.activeGameObject = go;
            TraducirEscena(go.scene);
            EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("Repercusión: paredes creadas (" + celdas.Count + " de colisión, " + pos.Count + " dibujadas).");
        }

        private static Tile TileDibujo(string nombre, string sprite)
        {
            string carpeta = AsegurarCarpeta(Raiz() + "/Mundo", "Tiles");
            string ruta = carpeta + "/" + nombre + ".asset";
            Tile t = AssetDatabase.LoadAssetAtPath<Tile>(ruta);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(t, ruta);
            }
            t.sprite = S(sprite);
            t.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(t);
            AssetDatabase.SaveAssets();
            return t;
        }

        private static Tile TilePared()
        {
            string carpeta = AsegurarCarpeta(Raiz() + "/Mundo", "Tiles");
            string ruta = carpeta + "/ColisionPared.asset";
            Tile t = AssetDatabase.LoadAssetAtPath<Tile>(ruta);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<Tile>();
                t.sprite = S("mundo_peligro");
                t.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(t, ruta);
                AssetDatabase.SaveAssets();
            }
            return t;
        }

        // =====================================================================
        // 7. SUELO DE UN SOLO COLOR
        // =====================================================================

        private static Tilemap BuscarSuelo()
        {
            foreach (Tilemap t in Object.FindObjectsByType<Tilemap>())
                if (!EsPared(t.gameObject)) return t;
            return null;
        }

        [MenuItem(MENU + "7. Suelo de un solo color", priority = 26)]
        public static void UnificarSuelo()
        {
            Tilemap suelo = BuscarSuelo();
            if (suelo == null) { EditorUtility.DisplayDialog("Repercusión", "No encontré el suelo.", "OK"); return; }

            suelo.CompressBounds();
            BoundsInt b = suelo.cellBounds;
            Dictionary<TileBase, int> cuenta = new Dictionary<TileBase, int>();
            List<Vector3Int> celdas = new List<Vector3Int>();
            foreach (Vector3Int p in b.allPositionsWithin)
            {
                TileBase t = suelo.GetTile(p);
                if (t == null) continue;
                celdas.Add(p);
                cuenta[t] = cuenta.TryGetValue(t, out int n) ? n + 1 : 1;
            }
            // El ladrillo verde oscuro (mainlevbuild_773); si no está, el que más se usa
            TileBase elegido = cuenta.Keys.FirstOrDefault(t => t.name == "mainlevbuild_773");
            if (elegido == null) elegido = cuenta.OrderByDescending(kv => kv.Value).First().Key;

            Undo.RegisterCompleteObjectUndo(suelo, "Suelo de un solo color");
            int cambiadas = 0;
            foreach (Vector3Int p in celdas)
            {
                if (suelo.GetTile(p) == elegido && suelo.GetColor(p) == Color.white) continue;
                suelo.SetTile(p, elegido);
                suelo.SetColor(p, Color.white);
                cambiadas++;
            }
            TraducirEscena(suelo.gameObject.scene);
            EditorSceneManager.MarkSceneDirty(suelo.gameObject.scene);
            Debug.Log("Repercusión: suelo unificado con " + elegido.name + " (" + cambiadas + " casillas cambiadas).");
        }

        // =====================================================================
        // 8. DECORAR SALAS (con el pack RF Castle: estandartes, alfombras, huesos, rocas...)
        // =====================================================================

        private struct SalaDeco
        {
            public string nombre; public RectInt r; public bool puzzle;
            public SalaDeco(string n, int x0, int y0, int x1, int y1, bool p = false)
            { nombre = n; r = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1); puzzle = p; }
        }

        private static SalaDeco[] Salas()
        {
            // Cuartos del mapa de Demo (en casillas, esquinas incluidas)
            return new SalaDeco[]
            {
                new SalaDeco("Deco_Inicio", -14, -6, 15, 4),
                new SalaDeco("Deco_Izquierda", -36, 13, -19, 30),
                new SalaDeco("Deco_Sala1", -12, 38, 20, 50, true),
                new SalaDeco("Deco_Centro", 43, 5, 70, 17),
                new SalaDeco("Deco_ArribaCentro", 73, 46, 94, 58),
                new SalaDeco("Deco_ArribaPasillo", 57, 38, 72, 45),
                new SalaDeco("Deco_Abajo", 86, -19, 102, 1),
                new SalaDeco("Deco_Derecha", 136, 38, 163, 42),
                new SalaDeco("Deco_Fondo", 184, 31, 205, 40),
            };
        }

        [MenuItem(MENU + "8. Decorar salas", priority = 27)]
        public static void DecorarSalas()
        {
            Preparar(false);
            Orden orden = CalcularOrden();
            int muro = OrdenParedes(orden);
            Tilemap suelo = BuscarSuelo();
            if (suelo == null) return;

            GameObject vieja = Buscar("Decoracion");
            if (vieja != null) Undo.DestroyObjectImmediate(vieja);
            GameObject raiz = new GameObject("Decoracion");
            Undo.RegisterCreatedObjectUndo(raiz, "Decorar salas");

            SalaDeco[] salas = Salas();

            // En la Sala 1 no se tapa nada del acertijo (placas, braseros, reja, llave...)
            List<Vector2> libres = new List<Vector2>
            {
                new Vector2(1.5f, 39.5f), new Vector2(19.5f, 42.5f), new Vector2(9.5f, 42.5f), new Vector2(-9f, 42.5f),
                new Vector2(12.5f, 39.5f), new Vector2(-10.5f, 39.5f), new Vector2(14.5f, 43f),
                new Vector2(-5.5f, 45.5f), new Vector2(-5.5f, 49f), new Vector2(17f, 40f)
            };

            for (int i = 0; i < salas.Length; i++)
                Decorar(salas[i], suelo, raiz, orden, muro, 17 + i * 31, libres);

            Selection.activeGameObject = raiz;
            TraducirEscena(raiz.scene);
            EditorSceneManager.MarkSceneDirty(raiz.scene);
        }

        private static void Decorar(SalaDeco sala, Tilemap suelo, GameObject raiz, Orden orden, int muro,
                                   int semilla, List<Vector2> libres)
        {
            System.Random rnd = new System.Random(semilla);
            RectInt r = sala.r;
            GameObject g = new GameObject(sala.nombre);
            g.transform.SetParent(raiz.transform, false);
            List<Vector2> ocupado = new List<Vector2>(sala.puzzle ? libres : new List<Vector2>());
            int piso = muro + 1;

            // Estandartes colgados en la pared de arriba
            int n = r.width >= 24 ? 3 : 2;
            for (int k = 1; k <= n; k++)
            {
                int cx = r.xMin + Mathf.RoundToInt(r.width * k / (n + 1f));
                int y = r.yMax - 1;
                while (y < r.yMax + 8 && suelo.HasTile(new Vector3Int(cx, y + 1, 0))) y++;
                if (!suelo.HasTile(new Vector3Int(cx, y, 0))) continue;
                if (suelo.HasTile(new Vector3Int(cx, y + 2, 0))) continue;
                string spr = (k % 2 == 1) ? "px_deco_estandarte" : "px_deco_estandarte_chico";
                Sprite s = S(spr);
                if (s == null) continue;
                float alto = s.bounds.size.y;
                Pieza(g, "Estandarte", spr, new Vector2(cx + 0.5f, y + 1f + 1.4f - alto / 2f), orden, piso, false);
            }

            // Alfombra al centro (no en la sala del acertijo)
            Rect alfombra = Rect.zero;
            if (!sala.puzzle && r.width >= 12 && r.height >= 8)
            {
                string spr = r.height > r.width ? "px_deco_alfombra_v" : "px_deco_alfombra";
                Vector2 c = new Vector2(r.xMin + r.width / 2f, r.yMin + r.height / 2f);
                Pieza(g, "Alfombra", spr, c, orden, piso, false);
                Sprite s = S(spr);
                if (s != null) alfombra = new Rect(c - (Vector2)s.bounds.extents, s.bounds.size);
            }

            // Candelabros con luz en las esquinas de arriba (no en la sala del acertijo: ahí solo el fuego guía)
            if (!sala.puzzle)
            {
                foreach (float x in new[] { r.xMin + 1.2f, r.xMax - 1.2f })
                {
                    Vector2 p = new Vector2(x, r.yMax - 0.2f);
                    if (!suelo.HasTile(Vector3Int.FloorToInt(new Vector3(p.x, p.y - 1f, 0)))) continue;
                    Sprite[] cuadros = SpritesRF("lightA");
                    GameObject cand;
                    if (cuadros.Length > 0)
                    {
                        cand = new GameObject("Candelabro");
                        cand.transform.SetParent(g.transform, false);
                        cand.transform.position = new Vector3(p.x, p.y + 1f, 0f);
                        SpriteRenderer sr = cand.AddComponent<SpriteRenderer>();
                        sr.sprite = cuadros[0];
                        sr.sortingLayerID = orden.capa;
                        sr.sortingOrder = orden.objetos;
                        Animar(cand, sr, cuadros, new Vector2(0f, 0.9f), 0.85f, 3.4f);
                    }
                    else
                    {
                        cand = Pieza(g, "Candelabro", "px_deco_candelabro", p, orden, orden.objetos, false);
                        Luz(cand, "Luz", new Vector2(0f, 0.7f), new Color(1f, 0.8f, 0.55f), 0.8f, 3.2f);
                    }
                    ocupado.Add(p);
                }
            }

            // Cosas sueltas: huesos, rocas, jarrones, barriles...
            // Sin huesos ni rocas
            string[] dePiso = { "px_deco_libros" };
            string[] dePie = { "px_deco_jarrones", "px_deco_jarron", "px_deco_barril", "px_deco_cubeta" };
            int cantidad = Mathf.Clamp(r.width * r.height / 45, 3, 9);
            int intentos = 0, puestos = 0;
            while (puestos < cantidad && intentos++ < 200)
            {
                // Pegadas a los bordes, donde no estorban el paso
                bool pegadoX = rnd.NextDouble() < 0.5;
                float x = pegadoX ? (rnd.NextDouble() < 0.5 ? r.xMin + 1f : r.xMax - 0.5f)
                                  : r.xMin + 1f + (float)rnd.NextDouble() * (r.width - 2f);
                float y = pegadoX ? r.yMin + 1f + (float)rnd.NextDouble() * (r.height - 2f)
                                  : (rnd.NextDouble() < 0.5 ? r.yMin + 1f : r.yMax - 0.3f);
                Vector2 p = new Vector2(x, y);
                if (!suelo.HasTile(Vector3Int.FloorToInt(new Vector3(p.x, p.y, 0)))) continue;
                if (alfombra.width > 0 && alfombra.Contains(p)) continue;
                if (sala.puzzle && p.y > 43.5f && p.y < 47.5f) continue; // el muro que parte la Sala 1
                if (ocupado.Any(o => Vector2.Distance(o, p) < 2.6f)) continue;

                bool piso2 = rnd.NextDouble() < 0.25;
                string spr = piso2 ? dePiso[rnd.Next(dePiso.Length)] : dePie[rnd.Next(dePie.Length)];
                Pieza(g, "Deco", spr, p, orden, piso2 ? piso : orden.objetos, rnd.NextDouble() < 0.5);
                ocupado.Add(p);
                puestos++;
            }
        }

        private static GameObject Pieza(GameObject padre, string nombre, string sprite, Vector2 mundo,
                                        Orden orden, int ordenEnCapa, bool voltear)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre.transform, false);
            go.transform.position = new Vector3(mundo.x, mundo.y, 0f);
            SpriteRenderer sr = Dibujo(go, sprite, orden, ordenEnCapa);
            sr.flipX = voltear;
            return go;
        }

        // Cuadros de animación del pack RF Castle (lightA = candelabro de pie, lightB = antorcha de pared)
        private static Sprite[] SpritesRF(string nombre)
        {
            List<Sprite> lista = new List<Sprite>();
            for (int i = 1; i <= 3; i++)
            {
                string ruta = "Assets/RF Castle/Animated/" + nombre + "0" + i + ".png";
                Sprite sp = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
                if (sp != null) lista.Add(sp);
            }
            return lista.ToArray();
        }

        private static void Animar(GameObject go, SpriteRenderer sr, Sprite[] cuadros, Vector2 luzLocal, float intensidad, float radio)
        {
            GameObject luzGO = Luz(go, "Luz", luzLocal, new Color(1f, 0.78f, 0.5f), intensidad, radio);
            Fuego f = go.AddComponent<Fuego>();
            f.render = sr;
            f.cuadros = cuadros;
            f.cuadrosPorSegundo = 7f;
            f.luz = luzGO.GetComponent<Light2D>();
            f.radioBase = radio;
            f.intensidadBase = intensidad;
        }

        // =====================================================================
        // 9. DECORAR PASILLOS (antorchas en las paredes y estandartes)
        // =====================================================================

        [MenuItem(MENU + "9. Decorar pasillos (antorchas)", priority = 28)]
        public static void DecorarPasillos()
        {
            Preparar(false);
            Orden orden = CalcularOrden();
            int muro = OrdenParedes(orden);
            Tilemap suelo = BuscarSuelo();
            if (suelo == null) return;
            Sprite[] antorcha = SpritesRF("lightB");
            if (antorcha.Length == 0)
            {
                EditorUtility.DisplayDialog("Repercusión", "No encontré las antorchas en Assets/RF Castle/Animated (lightB01..03).", "OK");
                return;
            }

            GameObject vieja = Buscar("Decoracion_Pasillos");
            if (vieja != null) Undo.DestroyObjectImmediate(vieja);
            GameObject raiz = new GameObject("Decoracion_Pasillos");
            Undo.RegisterCreatedObjectUndo(raiz, "Decorar pasillos");

            SalaDeco[] salas = Salas();
            System.Func<int, int, bool> F = (x, y) => suelo.HasTile(new Vector3Int(x, y, 0));
            System.Func<int, int, bool> EnSala = (x, y) =>
            {
                foreach (SalaDeco sd in salas)
                    if (x >= sd.r.xMin - 1 && x < sd.r.xMax + 1 && y >= sd.r.yMin - 1 && y < sd.r.yMax + 1) return true;
                return false;
            };

            suelo.CompressBounds();
            BoundsInt b = suelo.cellBounds;
            int antorchas = 0, estandartes = 0;
            for (int x = b.xMin; x < b.xMax; x++)
            {
                for (int y = b.yMin; y < b.yMax; y++)
                {
                    if (!F(x, y) || EnSala(x, y)) continue;

                    // Pared de arriba (se ve la cara de ladrillo): antorcha cada 8 casillas, estandarte en medio
                    if (!F(x, y + 1) && !F(x, y + 2) && F(x - 1, y) && F(x + 1, y))
                    {
                        int m = ((x % 8) + 8) % 8;
                        if (m == 0)
                        {
                            Antorcha(raiz, antorcha, new Vector2(x + 0.5f, y + 2.1f), false, orden, muro + 1);
                            antorchas++;
                        }
                        else if (m == 4)
                        {
                            Sprite s = S("px_deco_estandarte_chico");
                            if (s != null)
                            {
                                Pieza(raiz, "Estandarte", "px_deco_estandarte_chico",
                                      new Vector2(x + 0.5f, y + 1f + 1.4f - s.bounds.size.y / 2f), orden, muro + 1, false);
                                estandartes++;
                            }
                        }
                    }

                    // Paredes de los lados (pasillos verticales): antorcha cada 7 casillas
                    bool izq = !F(x - 1, y) && F(x + 1, y);
                    bool der = !F(x + 1, y) && F(x - 1, y);
                    if ((izq || der) && (((y % 7) + 7) % 7 == 3) && F(x, y - 1) && F(x, y + 1))
                    {
                        float px = izq ? x - 0.05f : x + 1.05f;
                        Antorcha(raiz, antorcha, new Vector2(px, y + 0.8f), der, orden, muro + 1);
                        antorchas++;
                    }
                }
            }

            Selection.activeGameObject = raiz;
            TraducirEscena(raiz.scene);
            EditorSceneManager.MarkSceneDirty(raiz.scene);
            Debug.Log("Repercusión: pasillos decorados (" + antorchas + " antorchas, " + estandartes + " estandartes).");
        }

        private static void Antorcha(GameObject padre, Sprite[] cuadros, Vector2 mundo, bool voltear, Orden orden, int ordenEnCapa)
        {
            GameObject go = new GameObject("Antorcha");
            go.transform.SetParent(padre.transform, false);
            go.transform.position = new Vector3(mundo.x, mundo.y, 0f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = cuadros[0];
            sr.flipX = voltear;
            sr.sortingLayerID = orden.capa;
            sr.sortingOrder = ordenEnCapa;
            Animar(go, sr, cuadros, new Vector2(0f, 0.3f), 0.9f, 3.6f);
        }

        // =====================================================================
        // NOMBRES EN INGLÉS (Hierarchy)
        // Los objetos se crean con nombre en español y al final de cada comando
        // se traducen. Los textos del juego (lo que ve la jugadora) siguen en español.
        // =====================================================================

        private static readonly Dictionary<string, string> INGLES = new Dictionary<string, string>
        {
            // Escena del juego
            { "Sala1_ElPrimerEco", "Room1_FirstEcho" }, { "PuntoInicio", "StartPoint" }, { "PuntoCentro", "CenterPoint" },
            { "Muro_Izquierdo", "Wall_Left" }, { "Muro_Derecho", "Wall_Right" }, { "Cara", "Face" }, { "Tope", "Top" },
            { "Oscuridad", "Darkness" }, { "Oscuridad_Global", "GlobalDarkness" }, { "LuzDeEco", "EcoLight" },
            { "Brasero", "Brazier" }, { "Brasero_Entrada", "Brazier_Entrance" }, { "Brasero_Reja", "Brazier_Gate" },
            { "Placa", "PressurePlate" }, { "Placa_Sombra_Centro", "Plate_Shadow_Center" },
            { "Placa_Sombra_Esquina", "Plate_Shadow_Corner" }, { "Placa_Fuego_Entrada", "Plate_Fire_Entrance" },
            { "Placa_Fuego_Reja", "Plate_Fire_Gate" },
            { "Losa_Reloj", "Slab_Hourglass" }, { "Losa_Luna", "Slab_Moon" }, { "Losa_Ojo", "Slab_Eye" },
            { "Losa_Llama", "Slab_Flame" },
            { "Musica", "Music" }, { "Sonidos", "SoundEffects" }, { "Brillo", "Glow" }, { "Luz", "Light" },
            { "Reja", "Gate" }, { "Puerta", "Door" }, { "PuertaSalida", "ExitDoor" }, { "Llave", "Key" },
            { "Inscripcion", "Inscription" }, { "Marcador", "Marker" }, { "Aviso", "Prompt" },
            { "PanelLectura", "ReadingPanel" }, { "Simbolos", "Symbols" }, { "Simbolo", "Symbol" }, { "Leer_Inscripcion", "Read_Inscription" },
            { "Pista", "Hint" }, { "Pista_Entrada", "Hint_Entrance" }, { "Pista_Reja", "Hint_Gate" },
            { "ZonaPeligro", "DangerZone" }, { "PruebaLuz", "LightTest" }, { "Pocion", "Potion" },
            { "Paredes_Colision", "Walls_Collision" }, { "Paredes_Dibujo", "Walls_Art" },
            { "Decoracion", "RoomDecor" }, { "Decoracion_Pasillos", "CorridorDecor" },
            { "Deco_Inicio", "Decor_Start" }, { "Deco_Izquierda", "Decor_Left" }, { "Deco_Sala1", "Decor_Room1" },
            { "Deco_Centro", "Decor_Center" }, { "Deco_ArribaCentro", "Decor_TopCenter" },
            { "Deco_ArribaPasillo", "Decor_TopHall" }, { "Deco_Abajo", "Decor_Bottom" },
            { "Deco_Derecha", "Decor_Right" }, { "Deco_Fondo", "Decor_Far" },
            { "Deco", "Prop" }, { "Alfombra", "Rug" }, { "Estandarte", "Banner" }, { "Candelabro", "Candelabra" },
            { "Antorcha", "Torch" },
            // Interfaz
            { "UI_MenuPrincipal", "UI_MainMenu" }, { "UI_Juego", "UI_Game" },
            { "Firma", "Credit" }, { "Titulo", "Title" }, { "Lema", "Tagline" }, { "Pie", "Footer" },
            { "Botones", "Buttons" }, { "ComoJugar", "HowToPlay" }, { "Contenido", "Content" },
            { "Controles", "Controls" }, { "Objetivo", "Goal" }, { "Fondo", "Background" },
            { "FondoCirculos", "CirclesBackground" }, { "FondoBarra", "BarBackground" }, { "Relleno", "Fill" },
            { "Barra", "Bar" }, { "Onda", "Ring" }, { "Ondas", "Rings" }, { "Imagen", "Image" },
            { "Texto", "Text" }, { "Numero", "Number" }, { "Linea", "Line" },
            { "LineaArriba", "LineTop" }, { "LineaAbajo", "LineBottom" }, { "Mensaje", "Message" },
            { "Iconos", "Icons" }, { "EtiquetaLuz", "LightLabel" }, { "EtiquetaLlaves", "KeysLabel" },
            { "PanelEcos", "EchoesPanel" }, { "TextoEcos", "EchoesText" }, { "Pausa", "Pause" },
            { "PistaPausa", "PauseHint" }, { "Version", "Version" },
            { "OpcionJugar", "Option_Play" }, { "OpcionComoJugar", "Option_HowToPlay" },
            { "OpcionSalir", "Option_Quit" }, { "OpcionVolver", "Option_Back" },
            { "OpcionContinuar", "Option_Resume" }, { "OpcionReiniciarSala", "Option_RestartRoom" },
            { "OpcionMenu", "Option_MainMenu" },
        };

        private static string EnIngles(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return nombre;
            if (INGLES.TryGetValue(nombre, out string en)) return en;
            // "Llave1" -> "Key1", "Barra3" -> "Bar3"
            System.Text.RegularExpressions.Match m =
                System.Text.RegularExpressions.Regex.Match(nombre, @"^(.*?)(\d+)$");
            if (m.Success && INGLES.TryGetValue(m.Groups[1].Value, out string en2)) return en2 + m.Groups[2].Value;
            return nombre;
        }

        // Busca un objeto por su nombre en español o en inglés
        private static GameObject Buscar(string nombre)
        {
            GameObject go = GameObject.Find(nombre);
            return go != null ? go : GameObject.Find(EnIngles(nombre));
        }

        private static bool EsPared(GameObject go)
        {
            return go.name.StartsWith("Paredes_") || go.name.StartsWith("Walls_");
        }

        private static void TraducirEscena(Scene escena)
        {
            if (!escena.IsValid()) return;
            foreach (GameObject raiz in escena.GetRootGameObjects()) Traducir(raiz.transform);
        }

        private static void Traducir(Transform t)
        {
            string en = EnIngles(t.gameObject.name);
            if (en != t.gameObject.name)
            {
                Undo.RecordObject(t.gameObject, "Nombre en inglés");
                t.gameObject.name = en;
            }
            foreach (Transform hijo in t) Traducir(hijo);
        }

        // Traduce y guarda todas las escenas del Build (Menú, Intro y Demo) y vuelve a la que tenías abierta
        [MenuItem(MENU + "10. Nombres en inglés (todas las escenas)", priority = 29)]
        public static void TraducirTodasLasEscenas()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string abierta = SceneManager.GetActiveScene().path;
            int n = 0;
            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrEmpty(e.path) || !File.Exists(e.path)) continue;
                Scene escena = EditorSceneManager.OpenScene(e.path, OpenSceneMode.Single);
                TraducirEscena(escena);
                EditorSceneManager.SaveScene(escena);
                n++;
            }
            if (!string.IsNullOrEmpty(abierta)) EditorSceneManager.OpenScene(abierta, OpenSceneMode.Single);
            Debug.Log("Repercusión: nombres en inglés en " + n + " escenas.");
        }

        // Cambia los textos viejos de la tecla R por la tecla E en todas las escenas del Build
        [MenuItem(MENU + "11. Textos de la tecla E (todas las escenas)", priority = 30)]
        public static void TextosTeclaE()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string abierta = SceneManager.GetActiveScene().path;
            Dictionary<string, string> cambios = new Dictionary<string, string>
            {
                { "R  —  NUEVO INTENTO", "E  —  DEJAR ECO" },
                { "<color=#F5F5F5>R</color><pos=300>Nuevo intento: tu intento anterior se queda como un eco",
                  "<color=#F5F5F5>E</color><pos=300>Dejar un eco: una copia de ti se queda donde estás" },
            };
            int n = 0;
            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrEmpty(e.path) || !File.Exists(e.path)) continue;
                Scene escena = EditorSceneManager.OpenScene(e.path, OpenSceneMode.Single);
                bool cambio = false;
                foreach (GameObject raiz in escena.GetRootGameObjects())
                {
                    foreach (TMP_Text t in raiz.GetComponentsInChildren<TMP_Text>(true))
                    {
                        string nuevoTexto = t.text;
                        foreach (KeyValuePair<string, string> kv in cambios) nuevoTexto = nuevoTexto.Replace(kv.Key, kv.Value);
                        if (nuevoTexto == t.text) continue;
                        Undo.RecordObject(t, "Texto tecla E");
                        t.text = nuevoTexto;
                        EditorUtility.SetDirty(t);
                        cambio = true;
                        n++;
                    }
                    foreach (GuiaSala g in raiz.GetComponentsInChildren<GuiaSala>(true))
                    {
                        if (!g.enPlacaBuena.Contains("Presiona R")) continue;
                        g.enPlacaBuena = g.enPlacaBuena.Replace("Presiona R", "Presiona E");
                        EditorUtility.SetDirty(g);
                        cambio = true;
                        n++;
                    }
                }
                if (cambio) EditorSceneManager.SaveScene(escena);
            }
            if (!string.IsNullOrEmpty(abierta)) EditorSceneManager.OpenScene(abierta, OpenSceneMode.Single);
            Debug.Log("Repercusión: " + n + " textos cambiados a la tecla E.");
        }

        // =====================================================================
        // 12. AUDIO: música de fondo, pasos de Eco y efectos (eco, losa, reja, llave, trampa)
        // Los sonidos están en Repercusion/Audio. Para cambiar uno, reemplaza el .wav con el mismo nombre.
        // =====================================================================

        private static AudioClip Audio(string nombre)
        {
            AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>(Raiz() + "/Audio/" + nombre + ".wav");
            if (c == null) Debug.LogWarning("Repercusión: no encontré el audio " + nombre);
            return c;
        }

        private static void ConfigurarAudios()
        {
            foreach (string id in AssetDatabase.FindAssets("t:AudioClip", new[] { Raiz() + "/Audio" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(id);
                AudioImporter ai = AssetImporter.GetAtPath(ruta) as AudioImporter;
                if (ai == null) continue;
                bool musica = Path.GetFileName(ruta).StartsWith("musica_");
                AudioImporterSampleSettings st = ai.defaultSampleSettings;
                AudioClipLoadType carga = musica ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                if (st.loadType == carga && ai.forceToMono) continue;
                st.loadType = carga;
                st.compressionFormat = AudioCompressionFormat.Vorbis;
                st.quality = musica ? 0.6f : 0.8f;
                ai.defaultSampleSettings = st;
                ai.forceToMono = true;
                ai.SaveAndReimport();
            }
        }

        [MenuItem(MENU + "12. Música y sonidos (todas las escenas)", priority = 31)]
        public static void PonerAudio()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ConfigurarAudios();
            string abierta = SceneManager.GetActiveScene().path;

            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrEmpty(e.path) || !File.Exists(e.path)) continue;
                Scene escena = EditorSceneManager.OpenScene(e.path, OpenSceneMode.Single);
                bool esJuego = Object.FindAnyObjectByType<GameManager>() != null;

                // Música
                GameObject m = Buscar("Musica");
                if (m == null) { m = new GameObject("Musica"); Undo.RegisterCreatedObjectUndo(m, "Música"); }
                MusicaFondo mf = m.GetComponent<MusicaFondo>();
                if (mf == null) mf = m.AddComponent<MusicaFondo>();
                mf.musica = Audio(esJuego ? "musica_calabozo" : "musica_menu");
                mf.volumen = esJuego ? 0.45f : 0.5f;
                EditorUtility.SetDirty(mf);

                if (esJuego)
                {
                    // Efectos
                    GameObject sj = Buscar("Sonidos");
                    if (sj == null) { sj = new GameObject("Sonidos"); Undo.RegisterCreatedObjectUndo(sj, "Sonidos"); }
                    SonidosJuego s = sj.GetComponent<SonidosJuego>();
                    if (s == null) s = sj.AddComponent<SonidosJuego>();
                    s.eco = Audio("sfx_eco");
                    s.losa = Audio("sfx_losa");
                    s.reja = Audio("sfx_reja");
                    s.llave = Audio("sfx_llave");
                    s.trampa = Audio("sfx_trampa");
                    EditorUtility.SetDirty(s);

                    // Pasos de Eco
                    GameObject eco = BuscarEco();
                    if (eco != null)
                    {
                        Pasos p = eco.GetComponent<Pasos>();
                        if (p == null) p = Undo.AddComponent<Pasos>(eco);
                        p.pasos = new[] { Audio("sfx_paso_1"), Audio("sfx_paso_2"), Audio("sfx_paso_3"), Audio("sfx_paso_4") };
                        EditorUtility.SetDirty(p);
                    }
                }

                TraducirEscena(escena);
                EditorSceneManager.SaveScene(escena);
            }
            if (!string.IsNullOrEmpty(abierta)) EditorSceneManager.OpenScene(abierta, OpenSceneMode.Single);
            Debug.Log("Repercusión: música y sonidos listos.");
        }

        // Puerta de salida en el cuarto del centro: el final de la demo.
        // Con las 3 llaves se abre, sale "Escapaste" y vuelve al menú principal.
        [MenuItem(MENU + "5b. Puerta de salida en el cuarto del centro", priority = 24)]
        public static void PuertaSalidaCentro()
        {
            Preparar(false);
            Orden orden = CalcularOrden();
            int ordenMuro = OrdenParedes(orden);

            GameObject vieja = Buscar("PuertaSalida");
            if (vieja != null) Undo.DestroyObjectImmediate(vieja);

            GameObject go = new GameObject("PuertaSalida");
            Undo.RegisterCreatedObjectUndo(go, "Puerta de salida");
            go.transform.position = new Vector3(56.5f, 10f, 0f);
            go.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            SpriteRenderer sr = Dibujo(go, "px_puerta_cerrada", orden, orden.objetos);
            sr.color = new Color(1f, 0.85f, 0.55f);
            BoxCollider2D bloqueo = go.AddComponent<BoxCollider2D>();
            bloqueo.offset = new Vector2(0f, -1.1f);
            bloqueo.size = new Vector2(3f, 0.8f);
            BoxCollider2D zona = go.AddComponent<BoxCollider2D>();
            zona.isTrigger = true;
            zona.offset = new Vector2(0f, -1.6f);
            zona.size = new Vector2(3.6f, 2.4f);

            PuertaSalida ps = go.AddComponent<PuertaSalida>();
            ps.bloqueo = bloqueo;
            ps.visual = sr;
            ps.spriteAbierta = S("px_puerta_abierta");
            ps.colorAbierta = Color.white;
            ps.mensajeFinal = "Eco cruzó la puerta. Escapó del calabozo… por ahora.\nFin de la demo";
            ps.escenaAlTerminar = "MenuPrincipal";
            ps.esperaAntesDeCargar = 4f;

            Luz(go, "Luz", new Vector2(0f, 0f), new Color(1f, 0.85f, 0.55f), 1.2f, 5f);

            // Los objetos de decoración pegados a la puerta estorban: se quitan (la alfombra se queda)
            foreach (SpriteRenderer otro in Object.FindObjectsByType<SpriteRenderer>())
            {
                if (otro == null || otro.gameObject == go) continue;
                if (otro.transform.parent == null || !otro.transform.parent.name.StartsWith("Decor_Center")) continue;
                if (otro.name == "Rug" || otro.name == "Alfombra") continue; // la alfombra queda bajo la puerta
                if (Vector2.Distance(otro.transform.position, go.transform.position) < 4f)
                    Undo.DestroyObjectImmediate(otro.gameObject);
            }

            Selection.activeGameObject = go;
            TraducirEscena(go.scene);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        // =====================================================================
        // 4. PELIGRO, BRASERO, POCIÓN
        // =====================================================================

        [MenuItem(MENU + "4. Crear peligro, brasero y poción de prueba", priority = 23)]
        public static void CrearPeligroYLuz()
        {
            Preparar(false);
            Orden orden = CalcularOrden();
            Vector3 centro = PosicionVista();

            GameObject raiz = new GameObject("PruebaLuz");
            Undo.RegisterCreatedObjectUndo(raiz, "Crear prueba de luz");
            raiz.transform.position = centro;

            GameObject peligro = Hijo(raiz, "ZonaPeligro", new Vector2(-2.5f, 0f));
            Dibujo(peligro, "mundo_peligro", orden, orden.suelo);
            peligro.transform.localScale = new Vector3(2f, 2f, 1f);
            BoxCollider2D pc = peligro.AddComponent<BoxCollider2D>();
            pc.isTrigger = true;
            pc.size = Vector2.one;
            peligro.AddComponent<ZonaPeligro>();

            GameObject brasero = Hijo(raiz, "Brasero", new Vector2(1f, 0f));
            Dibujo(brasero, "mundo_brasero", orden, orden.objetos);
            CircleCollider2D bc = brasero.AddComponent<CircleCollider2D>();
            bc.isTrigger = true;
            bc.radius = 1.4f;
            FuenteDeLuz fb = brasero.AddComponent<FuenteDeLuz>();
            fb.tipo = FuenteDeLuz.Tipo.Brasero;

            GameObject pocion = Hijo(raiz, "Pocion", new Vector2(3f, 0f));
            Dibujo(pocion, "mundo_pocion", orden, orden.objetos);
            pocion.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            CircleCollider2D poc = pocion.AddComponent<CircleCollider2D>();
            poc.isTrigger = true;
            poc.radius = 0.45f;
            FuenteDeLuz fp = pocion.AddComponent<FuenteDeLuz>();
            fp.tipo = FuenteDeLuz.Tipo.Pocion;

            Selection.activeGameObject = raiz;
            TraducirEscena(raiz.scene);
            EditorSceneManager.MarkSceneDirty(raiz.scene);
        }

        // =====================================================================
        // 5. PUERTA DE SALIDA
        // =====================================================================

        [MenuItem(MENU + "5. Crear puerta de salida (centro)", priority = 24)]
        public static void CrearPuertaSalida()
        {
            Preparar(false);
            Orden orden = CalcularOrden();

            GameObject go = new GameObject("PuertaSalida");
            Undo.RegisterCreatedObjectUndo(go, "Crear puerta de salida");
            go.transform.position = PosicionVista();
            go.transform.rotation = Quaternion.Euler(0f, 0f, 90f); // horizontal, 3 casillas de ancho

            SpriteRenderer sr = Dibujo(go, "mundo_puerta_cerrada", orden, orden.objetos);
            sr.color = new Color32(255, 200, 87, 255);
            BoxCollider2D bloqueo = go.AddComponent<BoxCollider2D>();
            bloqueo.size = new Vector2(0.8f, 3f);
            BoxCollider2D zona = go.AddComponent<BoxCollider2D>();
            zona.isTrigger = true;
            zona.size = new Vector2(2.6f, 3.4f);

            PuertaSalida ps = go.AddComponent<PuertaSalida>();
            ps.bloqueo = bloqueo;
            ps.visual = sr;
            ps.spriteAbierta = S("mundo_puerta_abierta");
            ps.colorAbierta = Color.white;

            Selection.activeGameObject = go;
            TraducirEscena(go.scene);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        // =====================================================================
        // FUENTES (Repercusion/UI/Fuentes/Mono y /Titulo)
        // =====================================================================

        private static TMP_FontAsset Fuente(string subcarpeta)
        {
            string carpeta = Raiz() + "/UI/Fuentes/" + subcarpeta;
            if (!AssetDatabase.IsValidFolder(carpeta)) return null;

            foreach (string id in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { carpeta }))
            {
                TMP_FontAsset existente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(id));
                if (existente != null) return existente;
            }

            // Primera vez: crea la fuente de TextMeshPro a partir del .ttf/.otf de la carpeta
            foreach (string id in AssetDatabase.FindAssets("t:Font", new[] { carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(id);
                Font ttf = AssetDatabase.LoadAssetAtPath<Font>(ruta);
                if (ttf == null) continue;

                TMP_FontAsset nueva = TMP_FontAsset.CreateFontAsset(ttf);
                if (nueva == null) continue;
                nueva.name = Path.GetFileNameWithoutExtension(ruta) + " SDF";
                AssetDatabase.CreateAsset(nueva, carpeta + "/" + nueva.name + ".asset");
                if (nueva.atlasTextures != null && nueva.atlasTextures.Length > 0 && nueva.atlasTextures[0] != null)
                {
                    nueva.atlasTextures[0].name = nueva.name + " Atlas";
                    AssetDatabase.AddObjectToAsset(nueva.atlasTextures[0], nueva);
                }
                if (nueva.material != null)
                {
                    nueva.material.name = nueva.name + " Material";
                    AssetDatabase.AddObjectToAsset(nueva.material, nueva);
                }
                AssetDatabase.SaveAssets();
                return nueva;
            }
            return null;
        }

        // =====================================================================
        // AYUDANTES: UI
        // =====================================================================

        private static void CrearCamara()
        {
            GameObject camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0f, 0f, -10f);
            Camera cam = camGO.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = FONDO;
        }

        // Fondo negro con círculos concéntricos a la derecha y ondas que se expanden desde el centro
        private static void FondoConCirculos(Transform padre, float opacidad, bool conOndas)
        {
            GameObject fondo = NuevoUI("FondoCirculos", padre);
            Estirar(fondo, 0f);
            Imagen(fondo, S("ui_fondo_menu"), new Color(1f, 1f, 1f, opacidad));

            if (!conOndas) return;
            GameObject ondas = NuevoUI("Ondas", padre);
            Colocar(ondas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(480f, -60f), new Vector2(10f, 10f));
            UnityEngine.UI.Image[] anillos = new UnityEngine.UI.Image[3];
            for (int i = 0; i < anillos.Length; i++)
            {
                GameObject a = NuevoUI("Onda" + (i + 1), ondas.transform);
                Colocar(a, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(40f, 40f));
                anillos[i] = Imagen(a, S("ui_anillo"), new Color(1f, 1f, 1f, 0f));
            }
            OndasEco o = ondas.AddComponent<OndasEco>();
            o.anillos = anillos;
            o.alfaMaxima = 0.3f * opacidad;
            EditorUtility.SetDirty(o);
        }

        // Texto anclado arriba a la izquierda (x, y desde la esquina, en px de 1920x1080)
        private static TextMeshProUGUI TextoArribaIzq(Transform padre, string nombre, string texto, float tamano, Color color,
                                                      float x, float y, float ancho, float alto, float espaciado,
                                                      bool titulo = false)
        {
            TextMeshProUGUI t = Texto(padre, nombre, texto, tamano, color, TextAlignmentOptions.TopLeft, false, titulo);
            Colocar(t.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x, -y), new Vector2(ancho, alto));
            t.characterSpacing = espaciado;
            return t;
        }

        // Texto chico en el pie de página, a la izquierda o a la derecha
        private static TextMeshProUGUI TextoAbajo(Transform padre, string nombre, string texto, float tamano, Color color, bool izquierda)
        {
            TextMeshProUGUI t = Texto(padre, nombre, texto, tamano, color,
                                      izquierda ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.BottomRight);
            Vector2 ancla = izquierda ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            Colocar(t.gameObject, ancla, ancla, ancla, new Vector2(izquierda ? MARGEN : -MARGEN, 64f), new Vector2(1000f, 30f));
            t.characterSpacing = ESPACIADO;
            return t;
        }

        // Opción del menú: "01  ——  JUGAR". La seleccionada se pone blanca y aparece la línea.
        private static UnityEngine.UI.Button Opcion(Transform padre, string nombre, string numero, string texto, float y)
        {
            GameObject go = NuevoUI(nombre, padre);
            Colocar(go, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(MARGEN, -y), new Vector2(900f, 64f));
            UnityEngine.UI.Image zona = Imagen(go, null, new Color(0f, 0f, 0f, 0f), true);

            UnityEngine.UI.Button b = go.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = zona;
            b.transition = UnityEngine.UI.Selectable.Transition.None;

            TextMeshProUGUI num = Texto(go.transform, "Numero", numero, 18f, GRIS_OSCURO, TextAlignmentOptions.Left);
            Colocar(num.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    Vector2.zero, new Vector2(60f, 40f));
            num.characterSpacing = 12f;

            GameObject linea = NuevoUI("Linea", go.transform);
            Colocar(linea, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(66f, 0f), new Vector2(84f, 2f));
            Imagen(linea, null, BLANCO);
            linea.transform.localScale = new Vector3(0f, 1f, 1f);

            TextMeshProUGUI etiqueta = Texto(go.transform, "Texto", texto, 32f, GRIS, TextAlignmentOptions.Left);
            Colocar(etiqueta.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(90f, 0f), new Vector2(760f, 50f));
            etiqueta.characterSpacing = 18f;

            OpcionMenu opcion = go.AddComponent<OpcionMenu>();
            opcion.numero = num;
            opcion.texto = etiqueta;
            opcion.linea = (RectTransform)linea.transform;
            EditorUtility.SetDirty(opcion);
            return b;
        }

        private static GameObject CrearCanvas(string nombre, int orden)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Canvas),
                                           typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            go.layer = 5; // UI
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = orden;

            UnityEngine.UI.CanvasScaler cs = go.GetComponent<UnityEngine.UI.CanvasScaler>();
            cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;

            Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
            return go;
        }

        private static void AsegurarEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
#if ENABLE_INPUT_SYSTEM
            GameObject es = new GameObject("EventSystem", typeof(EventSystem),
                                           typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
#else
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
            Undo.RegisterCreatedObjectUndo(es, "Crear EventSystem");
        }

        private static GameObject NuevoUI(string nombre, Transform padre)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(padre, false);
            return go;
        }

        private static void Colocar(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                    Vector2 posicion, Vector2 tamano)
        {
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = tamano;
            rt.anchoredPosition = posicion;
        }

        private static void Estirar(GameObject go, float margen)
        {
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(margen, margen);
            rt.offsetMax = new Vector2(-margen, -margen);
        }

        private static UnityEngine.UI.Image Imagen(GameObject go, Sprite sprite, Color color, bool recibeClics = false)
        {
            UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = recibeClics;
            if (sprite != null && sprite.border.sqrMagnitude > 0f) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        private static TextMeshProUGUI Texto(Transform padre, string nombre, string texto, float tamano, Color color,
                                             TextAlignmentOptions alineacion, bool negrita = false, bool titulo = false)
        {
            GameObject go = NuevoUI(nombre, padre);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset fuente = Fuente(titulo ? "Titulo" : "Mono");
            if (fuente != null) t.font = fuente;
            t.text = texto;
            t.fontSize = tamano;
            t.color = color;
            t.alignment = alineacion;
            t.raycastTarget = false;
            if (negrita) t.fontStyle = FontStyles.Bold;
            return t;
        }

        // =====================================================================
        // AYUDANTES: MUNDO
        // =====================================================================

        private struct Orden
        {
            public int capa;     // sorting layer
            public int suelo;    // cosas pegadas al piso (placas, peligros)
            public int objetos;  // cosas "de pie" (puertas, llaves): mismo orden que Eco
        }

        // Busca el orden de dibujo de Eco y del piso para que las placas queden
        // encima del piso y debajo de Eco.
        private static Orden CalcularOrden()
        {
            Orden o = new Orden { capa = 0, suelo = 0, objetos = 0 };
            GameObject eco = BuscarEco();
            SpriteRenderer srEco = eco != null ? eco.GetComponentInChildren<SpriteRenderer>() : null;
            if (srEco == null) return o;

            o.capa = srEco.sortingLayerID;
            o.objetos = srEco.sortingOrder;

            int piso = int.MinValue;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>())
            {
                if (r == null || r.transform.IsChildOf(eco.transform)) continue;
                if (r.sortingLayerID != o.capa) continue;
                if (r.sortingOrder < o.objetos && r.sortingOrder > piso) piso = r.sortingOrder;
            }
            // Si no hay nada debajo de Eco, el piso comparte su orden: la placa usa el mismo
            o.suelo = piso == int.MinValue ? o.objetos : Mathf.Min(piso + 1, o.objetos);
            return o;
        }

        private static SpriteRenderer Dibujo(GameObject go, string sprite, Orden orden, int ordenEnCapa)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(sprite);
            sr.sortingLayerID = orden.capa;
            sr.sortingOrder = ordenEnCapa;
            return sr;
        }

        private static GameObject Hijo(GameObject padre, string nombre, Vector2 local)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre.transform, false);
            go.transform.localPosition = new Vector3(local.x, local.y, 0f);
            return go;
        }

        private static Vector3 PosicionVista()
        {
            SceneView vista = SceneView.lastActiveSceneView;
            Vector3 p = vista != null ? vista.pivot : Vector3.zero;
            return new Vector3(Mathf.Round(p.x * 2f) / 2f, Mathf.Round(p.y * 2f) / 2f, 0f);
        }

        // =====================================================================
        // AYUDANTES: ESCENAS Y CARPETAS
        // =====================================================================

        private static string AsegurarCarpeta(string padre, string nombre)
        {
            string ruta = padre + "/" + nombre;
            if (!AssetDatabase.IsValidFolder(ruta)) AssetDatabase.CreateFolder(padre, nombre);
            return ruta;
        }

        private const int MENU_ESCENA = 0, INTRO_ESCENA = 1, JUEGO_ESCENA = 2;

        // Orden en la lista de Build: MenuPrincipal, Intro, Juego
        private static void AgregarAlBuild(string ruta, int tipo)
        {
            List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            lista.RemoveAll(s => s.path == ruta);
            EditorBuildSettingsScene nueva = new EditorBuildSettingsScene(ruta, true);

            int pos = 0;
            if (tipo >= INTRO_ESCENA && lista.Count > pos && lista[pos].path.EndsWith("/MenuPrincipal.unity")) pos++;
            if (tipo >= JUEGO_ESCENA && lista.Count > pos && lista[pos].path.EndsWith("/Intro.unity")) pos++;
            lista.Insert(Mathf.Min(pos, lista.Count), nueva);
            EditorBuildSettings.scenes = lista.ToArray();
        }
    }
}
