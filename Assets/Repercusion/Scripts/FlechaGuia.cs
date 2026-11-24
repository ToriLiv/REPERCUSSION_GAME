using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Repercusion
{
    // Indica al jugador a dónde ir dentro de la sala:
    //   1) Una flecha sobre la tablilla hasta que la lea.
    //   2) Después, las losas libres brillan y laten (son los objetivos).
    //      Tras el primer error (orden equivocado o un eco que se desvaneció),
    //      la flecha también señala la SIGUIENTE losa del orden correcto.
    //      Si "mostrarOrdenSiempre" está activo, la señala desde el principio.
    //   3) Con la reja abierta, la flecha señala la llave.
    //   4) Al tomar la llave, desaparece.
    public class FlechaGuia : MonoBehaviour
    {
        public Tablilla tablilla;
        [Tooltip("Losas en el orden correcto")]
        public PlacaPresion[] losas;
        public Puerta reja;
        public Transform llave;
        public LosasEnOrden losasEnOrden;

        [Header("Flecha")]
        public Transform flecha;
        [Tooltip("La tablilla está arriba en el muro: la flecha va a su lado y apunta hacia ella")]
        public Vector2 ladoTablilla = new Vector2(-1.4f, 0f);
        public float alturaLosa = 1.4f;
        public float alturaLlave = 1.3f;
        public float rebote = 0.18f;
        [Tooltip("Distancia al borde de la pantalla cuando el objetivo no se ve (0..0.5)")]
        public float margen = 0.08f;

        [Header("Ayuda")]
        [Tooltip("Activado: la flecha señala la siguiente losa desde el inicio.\n" +
                 "Desactivado: solo después del primer error.")]
        public bool mostrarOrdenSiempre = false;

        [Header("Brillo de las losas")]
        public Color colorBrillo = new Color(0.45f, 1f, 0.9f);
        public float intensidadBrillo = 1.6f;
        public float radioBrillo = 1.6f;

        private Light2D[] brillos;
        private SpriteRenderer[] destellos;   // el símbolo encendido, latiendo encima de la losa
        private SpriteRenderer flechaSR;

        private void Start()
        {
            if (flecha != null) flechaSR = flecha.GetComponentInChildren<SpriteRenderer>();
            if (losas == null) return;
            brillos = new Light2D[losas.Length];
            destellos = new SpriteRenderer[losas.Length];
            for (int i = 0; i < losas.Length; i++)
            {
                if (losas[i] == null) continue;
                GameObject go = new GameObject("Glow");
                go.transform.SetParent(losas[i].transform, false);

                // Copia del símbolo encendido encima de la losa: late suave cuando hay que ir ahí
                SpriteRenderer v = losas[i].visual;
                if (v != null && losas[i].spritePresionada != null)
                {
                    go.transform.position = v.transform.position;
                    go.transform.rotation = v.transform.rotation;
                    go.transform.localScale = Vector3.one;
                    SpriteRenderer d = go.AddComponent<SpriteRenderer>();
                    d.sprite = losas[i].spritePresionada;
                    d.sharedMaterial = v.sharedMaterial;
                    d.sortingLayerID = v.sortingLayerID;
                    d.sortingOrder = v.sortingOrder + 1;
                    d.color = new Color(1f, 1f, 1f, 0f);
                    destellos[i] = d;
                }

                Light2D l = go.AddComponent<Light2D>();
                l.lightType = Light2D.LightType.Point;
                l.color = colorBrillo;
                l.intensity = 0f;
                l.pointLightInnerRadius = 0.3f;
                l.pointLightOuterRadius = radioBrillo;
                brillos[i] = l;
            }
        }

        private void Update()
        {
            bool enSala = false;
            LuzEco jugadora = LuzEco.Jugadora;
            if (jugadora != null)
            {
                EcoGrabadora g = jugadora.GetComponent<EcoGrabadora>();
                enSala = g != null && g.SalaActual != null && g.SalaActual.transform == transform;
            }

            bool tablaLeida = tablilla == null || tablilla.Leida;
            bool rejaAbierta = reja != null && reja.Abierta;
            bool llaveTomada = llave == null || !llave.gameObject.activeInHierarchy;

            // Losas: laten mientras estén libres y la reja siga cerrada
            float pulso = 0.55f + 0.45f * Mathf.Sin(Time.time * 3f);
            if (brillos != null)
            {
                for (int i = 0; i < brillos.Length; i++)
                {
                    if (brillos[i] == null) continue;
                    bool brilla = enSala && tablaLeida && !rejaAbierta && !losas[i].Presionada;
                    brillos[i].intensity = brilla ? intensidadBrillo * pulso : 0f;
                    if (destellos[i] != null)
                        destellos[i].color = new Color(1f, 1f, 1f, brilla ? 0.15f + 0.6f * pulso : 0f);
                }
            }

            // ¿A dónde apunta la flecha?
            Vector3? objetivo = null;
            if (enSala)
            {
                if (!tablaLeida && tablilla != null)
                    objetivo = tablilla.transform.position + (Vector3)ladoTablilla;
                else if (rejaAbierta && !llaveTomada)
                    objetivo = llave.position + Vector3.up * alturaLlave;
                else if (!rejaAbierta && (mostrarOrdenSiempre || (losasEnOrden != null && losasEnOrden.Fallos > 0)))
                {
                    PlacaPresion siguiente = SiguienteLosa();
                    if (siguiente != null) objetivo = siguiente.transform.position + Vector3.up * alturaLosa;
                }
            }

            if (flecha == null) return;
            bool ver = objetivo.HasValue;
            if (flecha.gameObject.activeSelf != ver) flecha.gameObject.SetActive(ver);
            if (!ver) return;

            // Junto a la tablilla la flecha apunta de lado; en lo demás, hacia abajo
            bool deLado = !(tablilla == null || tablilla.Leida);
            Vector3 dirRebote = deLado ? Vector3.left : Vector3.up;
            if (deLado && ladoTablilla.x > 0f) dirRebote = Vector3.right;
            float angulo = deLado ? (ladoTablilla.x < 0f ? 90f : -90f) : 0f;
            Vector3 destino = objetivo.Value;

            // Si el objetivo está fuera de la pantalla, la flecha se queda en el borde apuntando hacia él
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 vp = cam.WorldToViewportPoint(destino);
                if (vp.x < margen || vp.x > 1f - margen || vp.y < margen || vp.y > 1f - margen)
                {
                    Vector3 borde = new Vector3(Mathf.Clamp(vp.x, margen, 1f - margen),
                                                Mathf.Clamp(vp.y, margen, 1f - margen), vp.z);
                    Vector3 enBorde = cam.ViewportToWorldPoint(borde);
                    enBorde.z = destino.z;
                    Vector2 hacia = (Vector2)(destino - enBorde);
                    if (hacia.sqrMagnitude > 0.0001f)
                    {
                        // El dibujo apunta hacia abajo: se gira para que apunte al objetivo
                        angulo = Mathf.Atan2(hacia.x, -hacia.y) * Mathf.Rad2Deg;
                        dirRebote = -(Vector3)hacia.normalized;
                    }
                    destino = enBorde;
                }
            }

            flecha.rotation = Quaternion.Euler(0f, 0f, angulo);
            flecha.position = destino + dirRebote * (Mathf.Sin(Time.time * 4f) * rebote);
            if (flechaSR != null)
            {
                Color c = flechaSR.color;
                c.a = 0.75f + 0.25f * Mathf.Sin(Time.time * 6f);
                flechaSR.color = c;
            }
        }

        // La primera losa del orden que todavía no está presionada
        private PlacaPresion SiguienteLosa()
        {
            if (losas == null) return null;
            foreach (PlacaPresion p in losas)
            {
                if (p != null && !p.Presionada) return p;
            }
            return null;
        }
    }
}
