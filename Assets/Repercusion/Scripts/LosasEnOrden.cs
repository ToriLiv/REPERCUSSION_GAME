using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Repercusion
{
    // Losas que dejan un eco solas: al pisar una losa vacía, aparece un eco de Eco sobre ella
    // y se queda sosteniéndola... pero solo un rato: los ecos se desvanecen (SalaPuzle.vidaEco).
    // La reja (Puerta con "en orden") pide todas a la vez, en el orden de la lista.
    // Si se pisan todas en otro orden, los ecos se desvanecen solos y se vuelve a intentar.
    // Cada eco nuevo susurra un recuerdo de la científica.
    public class LosasEnOrden : MonoBehaviour
    {
        public PlacaPresion[] losas;
        public Puerta reja;
        public float esperaReinicio = 1.5f;
        [TextArea] public string mensajeOrdenEquivocado = "Ese no era el orden… tus ecos se desvanecen. Inténtalo otra vez";
        [TextArea] public string mensajeEcoDesvanecido = "Tu eco se desvaneció… los ecos no duran para siempre. Date prisa";

        [Header("Susurros de los ecos")]
        public TMP_FontAsset fuenteSusurro;
        [TextArea] public string[] susurros =
        {
            "Esta vez sí va a funcionar…",
            "Solo un ajuste más.",
            "¿Dónde me equivoqué?",
            "Casi lo tenía…",
        };

        // Qué eco está sosteniendo cada losa
        private readonly Dictionary<PlacaPresion, EcoFantasma> conEco = new Dictionary<PlacaPresion, EcoFantasma>();
        // Losas donde Eco estaba parada al reiniciar: no dejan eco hasta que se baje de ellas
        private readonly HashSet<PlacaPresion> esperandoQueSalga = new HashSet<PlacaPresion>();
        private EcoGrabadora grabadora;
        private float reiniciarEn = -1f;
        private int siguienteSusurro;
        private bool avisoDesvanecidoDado;

        // Cuántas veces falló (orden equivocado o un eco que se desvaneció): FlechaGuia lo usa
        public int Fallos { get; private set; }

        private void OnEnable()
        {
            EcoGrabadora.OnEcosCambiaron += AlCambiarEcos;
            EcoGrabadora.OnEcoDesvanecido += AlDesvanecerse;
        }

        private void OnDisable()
        {
            EcoGrabadora.OnEcosCambiaron -= AlCambiarEcos;
            EcoGrabadora.OnEcoDesvanecido -= AlDesvanecerse;
        }

        private void AlCambiarEcos(int actuales, int maximo, bool enSala)
        {
            if (actuales == 0) conEco.Clear();   // se apagó la luz, se reinició la sala, etc.
            if (!enSala) avisoDesvanecidoDado = false;
        }

        private void AlDesvanecerse()
        {
            if (reja != null && reja.Abierta) return;
            if (grabadora == null || grabadora.SalaActual == null) return;
            Fallos++;
            if (avisoDesvanecidoDado || string.IsNullOrEmpty(mensajeEcoDesvanecido)) return;
            avisoDesvanecidoDado = true;
            GameManager.Mensaje(mensajeEcoDesvanecido, 4f);
        }

        private void Update()
        {
            LuzEco jugadora = LuzEco.Jugadora;
            if (jugadora == null || losas == null) return;
            if (grabadora == null) grabadora = jugadora.GetComponent<EcoGrabadora>();
            if (grabadora == null || grabadora.SalaActual == null) return;

            // Las losas cuyo eco ya se desvaneció quedan libres otra vez
            List<PlacaPresion> libres = null;
            foreach (KeyValuePair<PlacaPresion, EcoFantasma> par in conEco)
            {
                if (par.Value != null) continue;
                if (libres == null) libres = new List<PlacaPresion>();
                libres.Add(par.Key);
            }
            if (libres != null) foreach (PlacaPresion l in libres) conEco.Remove(l);

            // Reinicio automático cuando el orden quedó mal
            if (reja != null && reja.OrdenEquivocado && reiniciarEn < 0f)
                reiniciarEn = Time.time + esperaReinicio;
            if (reiniciarEn > 0f && Time.time >= reiniciarEn)
            {
                reiniciarEn = -1f;
                Fallos++;
                conEco.Clear();
                esperandoQueSalga.Clear();
                foreach (PlacaPresion l in losas) if (l != null) esperandoQueSalga.Add(l);
                grabadora.BorrarEcosDeSala();
                SonidosJuego.Tocar(SonidosJuego.Tipo.Trampa, 0.7f);
                if (!string.IsNullOrEmpty(mensajeOrdenEquivocado)) GameManager.Mensaje(mensajeOrdenEquivocado, 4f);
                return;
            }
            if (reiniciarEn > 0f) return;

            // Con la reja ya abierta no hace falta dejar más ecos
            if (reja != null && reja.Abierta && reja.quedarseAbierta) return;

            Pisador p = jugadora.GetComponent<Pisador>();
            Vector2 pies = p != null ? p.PuntoPies : (Vector2)jugadora.transform.position;
            Vector2 offset = p != null ? p.offsetPies : Vector2.zero;

            foreach (PlacaPresion losa in losas)
            {
                if (losa == null || conEco.ContainsKey(losa)) continue;
                Collider2D zona = losa.GetComponent<Collider2D>();
                bool pisando = zona != null && zona.OverlapPoint(pies);
                if (!pisando) { esperandoQueSalga.Remove(losa); continue; }
                if (esperandoQueSalga.Contains(losa)) continue;

                // El eco aparece con los pies justo en el centro de la losa
                EcoFantasma eco = grabadora.DejarEcoEn((Vector2)losa.transform.position - offset);
                if (eco == null) continue;
                conEco[losa] = eco;
                if (susurros != null && susurros.Length > 0)
                {
                    eco.Susurrar(susurros[siguienteSusurro % susurros.Length], fuenteSusurro);
                    siguienteSusurro++;
                }
            }
        }
    }
}
