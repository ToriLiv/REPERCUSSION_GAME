using System.Collections.Generic;
using UnityEngine;

namespace Repercusion
{
    // Guía de la sala: va dando instrucciones según lo que hace la jugadora
    // (entrar, pararse en una placa buena, dejar ecos, abrir la reja).
    // Los mensajes se ponen en fila para que no se tapen unos a otros.
    [RequireComponent(typeof(Collider2D))]
    public class GuiaSala : MonoBehaviour
    {
        [Header("Partes de la sala")]
        public PlacaPresion[] placasBuenas;
        public Puerta reja;

        [Header("Mensajes")]
        [TextArea] public string alEntrar1 = "Está oscuro: la oscuridad gasta tu luz. Los braseros la recargan";
        [TextArea] public string alEntrar2 = "Acércate a la inscripción de piedra para leerla";
        [TextArea] public string enPlacaBuena = "Presiona R: dejarás un eco aquí que sostendrá la placa";
        [TextArea] public string trasPrimerEco = "Tu eco sostiene esa piedra. Falta otra, también lejos del fuego";
        [TextArea] public string trasSegundoEco = "Tus ecos sostienen las dos piedras. Ve a la reja";
        [TextArea] public string alReiniciar = "Empieza de nuevo: deja un eco en cada piedra de la sombra";
        [TextArea] public string rejaAbierta = "La reja se abrió";
        public float duracion = 4.5f;

        private readonly Queue<string> cola = new Queue<string>();
        private float proximo;
        private Collider2D zona;
        private bool dentro;
        private int ecosAntes;
        private bool dijoReja;
        private float proximoAvisoPlaca;

        private void Awake()
        {
            zona = GetComponent<Collider2D>();
        }

        private void OnEnable() { EcoGrabadora.OnEcosCambiaron += AlCambiarEcos; }
        private void OnDisable() { EcoGrabadora.OnEcosCambiaron -= AlCambiarEcos; }

        private bool JugadoraAqui()
        {
            LuzEco luz = LuzEco.Jugadora;
            if (luz == null || zona == null) return false;
            Bounds b = zona.bounds;
            b.Expand(3f); // al entrar, Eco todavía puede estar en la puerta de la sala
            return b.Contains(luz.transform.position);
        }

        private void AlCambiarEcos(int actuales, int maximo, bool enSala)
        {
            bool aqui = enSala && JugadoraAqui();

            if (aqui && !dentro)
            {
                // Primero se ve el nombre de la sala; luego las instrucciones
                cola.Clear();
                proximo = Time.time + 3f;
                cola.Enqueue(alEntrar1);
                cola.Enqueue(alEntrar2);
                dijoReja = false;
            }
            else if (aqui)
            {
                if (actuales == 1 && ecosAntes == 0) Decir(trasPrimerEco);
                else if (actuales >= 2 && ecosAntes < 2) Decir(trasSegundoEco);
                else if (actuales == 0 && ecosAntes > 0) Decir(alReiniciar);
            }

            dentro = aqui;
            ecosAntes = aqui ? actuales : 0;
        }

        private void Decir(string texto)
        {
            // Lo nuevo es más importante que lo que estaba en fila
            cola.Clear();
            cola.Enqueue(texto);
            proximo = Mathf.Min(proximo, Time.time);
        }

        private void Update()
        {
            if (dentro)
            {
                // Parada en una placa buena y todavía le quedan ecos: le decimos qué hacer
                if (ecosAntes < 2 && Time.time >= proximoAvisoPlaca && PisandoPlacaBuena())
                {
                    Decir(enPlacaBuena);
                    proximoAvisoPlaca = Time.time + 8f;
                }
                if (reja != null && reja.Abierta && !dijoReja)
                {
                    dijoReja = true;
                    Decir(rejaAbierta);
                }
            }

            if (cola.Count > 0 && Time.time >= proximo)
            {
                GameManager.Mensaje(cola.Dequeue(), duracion);
                proximo = Time.time + duracion + 0.3f;
            }
        }

        private bool PisandoPlacaBuena()
        {
            LuzEco luz = LuzEco.Jugadora;
            if (luz == null || placasBuenas == null) return false;
            Pisador p = luz.GetComponent<Pisador>();
            Vector2 pies = p != null ? p.PuntoPies : (Vector2)luz.transform.position;
            foreach (PlacaPresion placa in placasBuenas)
            {
                if (placa == null) continue;
                Collider2D c = placa.GetComponent<Collider2D>();
                if (c != null && c.OverlapPoint(pies)) return true;
            }
            return false;
        }
    }
}
