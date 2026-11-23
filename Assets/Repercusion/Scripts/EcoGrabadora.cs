using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;

namespace Repercusion
{
    // LA MECÁNICA PRINCIPAL. Va en el objeto de Eco.
    // Dentro de una SalaPuzle graba cada intento. Al presionar R:
    //   1) el intento grabado se convierte en un "eco" que repite tus movimientos,
    //   2) Eco vuelve al inicio de la sala,
    //   3) todos los ecos repiten su camino desde el principio, al mismo tiempo que tú.
    // Cuando un eco termina su camino se queda quieto, así puede sostener una placa.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Pisador))]
    public class EcoGrabadora : MonoBehaviour
    {
        public KeyCode teclaNuevoIntento = KeyCode.R;

        [Tooltip("Segundos máximos que se graba cada intento")]
        public float duracionMaxima = 40f;

        [Header("Aspecto de los ecos")]
        public Color colorEco = new Color(0.15f, 0.85f, 0.75f, 0.6f);
        [Tooltip("Parpadeo tipo glitch en los ecos")]
        public bool parpadeo = true;

        [Header("Eventos (opcional)")]
        [Tooltip("Por ejemplo: arrastra aquí tu GlitchPulse y elige Pulso()")]
        public UnityEvent alNuevoIntento;

        // (ecos actuales, máximo de la sala, si está dentro de una sala)
        public static event Action<int, int, bool> OnEcosCambiaron;

        public SalaPuzle SalaActual => sala;

        private readonly List<Vector2> posiciones = new List<Vector2>();
        private readonly List<Vector2> direcciones = new List<Vector2>();
        private readonly List<bool> moviendose = new List<bool>();
        private readonly List<EcoFantasma> ecos = new List<EcoFantasma>();

        private SalaPuzle sala;
        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Animator anim;
        private Pisador pisador;
        private Vector2 posAnterior;
        private Vector2 ultimaDir = Vector2.down;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponentInChildren<SpriteRenderer>();
            anim = GetComponent<Animator>();
            pisador = GetComponent<Pisador>();

            // Los pies = centro del collider de Eco
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) pisador.offsetPies = Vector2.Scale(col.offset, transform.lossyScale);
        }

        private void Start()
        {
            Avisar();
        }

        private void Update()
        {
            if (sala != null && Time.timeScale > 0f && Entrada.Tecla(teclaNuevoIntento))
                NuevoIntento();
        }

        private void FixedUpdate()
        {
            if (sala == null) return;
            // Guarda solo los últimos "duracionMaxima" segundos: así el eco siempre
            // termina donde estabas al presionar R, aunque hayas tardado en la sala.
            int maximo = Mathf.CeilToInt(duracionMaxima / Time.fixedDeltaTime);
            if (posiciones.Count >= maximo)
            {
                int sobran = posiciones.Count - maximo + 1;
                posiciones.RemoveRange(0, sobran);
                direcciones.RemoveRange(0, sobran);
                moviendose.RemoveRange(0, sobran);
            }

            Vector2 p = Posicion();
            Vector2 delta = p - posAnterior;
            bool mov = delta.sqrMagnitude > 0.000001f;
            if (mov) ultimaDir = delta.normalized;

            posiciones.Add(p);
            direcciones.Add(ultimaDir);
            moviendose.Add(mov);
            posAnterior = p;
        }

        private Vector2 Posicion()
        {
            return rb != null ? rb.position : (Vector2)transform.position;
        }

        public bool EnSala => sala != null;

        public void EntrarASala(SalaPuzle nueva)
        {
            if (nueva == null || sala == nueva) return;
            BorrarEcos();
            sala = nueva;
            EmpezarGrabacion();

            string titulo = sala.nombreSala;
            if (!string.IsNullOrEmpty(sala.subtitulo)) titulo += "  ·  " + sala.subtitulo;
            GameManager.Mensaje(titulo);
            Avisar();
        }

        // Pasa null para salir de cualquier sala
        public void SalirDeSala(SalaPuzle saliente)
        {
            if (saliente != null && saliente != sala) return;
            BorrarEcos();
            sala = null;
            LimpiarGrabacion();
            Avisar();
        }

        public void NuevoIntento()
        {
            if (sala == null) return;

            // Solo vale la pena dejar un eco si el intento duró algo
            if (sala.maxEcos > 0 && posiciones.Count > 5)
            {
                if (ecos.Count >= sala.maxEcos)
                {
                    EcoFantasma viejo = ecos[0];
                    ecos.RemoveAt(0);
                    if (viejo != null) Destroy(viejo.gameObject);
                }
                ecos.Add(CrearEco());
            }

            GameManager.Teletransportar(gameObject, sala.Inicio);
            foreach (EcoFantasma e in ecos)
            {
                if (e != null) e.Reiniciar();
            }
            EmpezarGrabacion();
            alNuevoIntento?.Invoke();
            Avisar();
        }

        // Borra todos los ecos y vuelve al inicio de la sala
        public void ReiniciarSala()
        {
            if (sala == null) return;
            BorrarEcos();
            GameManager.Teletransportar(gameObject, sala.Inicio);
            EmpezarGrabacion();
            Avisar();
        }

        private void EmpezarGrabacion()
        {
            LimpiarGrabacion();
            posAnterior = Posicion();
        }

        private void LimpiarGrabacion()
        {
            posiciones.Clear();
            direcciones.Clear();
            moviendose.Clear();
        }

        private void BorrarEcos()
        {
            foreach (EcoFantasma e in ecos)
            {
                if (e != null) Destroy(e.gameObject);
            }
            ecos.Clear();
        }

        private EcoFantasma CrearEco()
        {
            GameObject raiz = new GameObject("Echo_" + (ecos.Count + 1));
            raiz.transform.localScale = transform.localScale;
            Vector2 p0 = posiciones[0];
            raiz.transform.position = new Vector3(p0.x, p0.y, transform.position.z);

            // Copia el dibujo de Eco (si el sprite está en un hijo, crea un hijo igual)
            SpriteRenderer copia = null;
            if (sr != null)
            {
                GameObject duenio = raiz;
                if (sr.transform != transform)
                {
                    duenio = new GameObject(sr.gameObject.name);
                    duenio.transform.SetParent(raiz.transform, false);
                    duenio.transform.localPosition = sr.transform.localPosition;
                    duenio.transform.localRotation = sr.transform.localRotation;
                    duenio.transform.localScale = sr.transform.localScale;
                }
                copia = duenio.AddComponent<SpriteRenderer>();
                copia.sprite = sr.sprite;
                copia.sharedMaterial = sr.sharedMaterial;
                copia.sortingLayerID = sr.sortingLayerID;
                copia.sortingOrder = sr.sortingOrder - 1;
                copia.flipX = sr.flipX;
                copia.color = colorEco;

                // Si Eco se anima con DirectionSprites, el eco también
                DirectionSprites ds = sr.GetComponent<DirectionSprites>();
                if (ds != null)
                {
                    DirectionSprites dsEco = duenio.AddComponent<DirectionSprites>();
                    dsEco.front = ds.front;
                    dsEco.back = ds.back;
                    dsEco.right = ds.right;
                    dsEco.left = ds.left;
                    dsEco.squarePerSecond = ds.squarePerSecond;
                }
            }

            // Mismas animaciones que Eco
            Animator animEco = null;
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                animEco = raiz.AddComponent<Animator>();
                animEco.runtimeAnimatorController = anim.runtimeAnimatorController;
            }

            // Los ecos llevan una luz tenue: se ven en la oscuridad
            Light2D luzEco = raiz.AddComponent<Light2D>();
            luzEco.lightType = Light2D.LightType.Point;
            luzEco.color = new Color(0.45f, 1f, 0.9f);
            luzEco.intensity = 0.7f;
            luzEco.pointLightInnerRadius = 0.4f;
            luzEco.pointLightOuterRadius = 2.6f;

            Pisador pis = raiz.AddComponent<Pisador>();
            pis.offsetPies = pisador.offsetPies;

            EcoFantasma eco = raiz.AddComponent<EcoFantasma>();
            eco.Configurar(posiciones.ToArray(), direcciones.ToArray(), moviendose.ToArray(),
                           Time.fixedDeltaTime, animEco, anim != null ? anim.parameters : null,
                           copia, colorEco, parpadeo);
            return eco;
        }

        private void Avisar()
        {
            OnEcosCambiaron?.Invoke(ecos.Count, sala != null ? sala.maxEcos : 0, sala != null);
        }
    }
}
