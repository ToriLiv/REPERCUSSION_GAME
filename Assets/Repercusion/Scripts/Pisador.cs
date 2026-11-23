using System.Collections.Generic;
using UnityEngine;

namespace Repercusion
{
    // Marca algo que puede presionar placas: Eco y sus ecos.
    // Las placas revisan si el punto de los pies está encima de ellas.
    public class Pisador : MonoBehaviour
    {
        public static readonly List<Pisador> Todos = new List<Pisador>();

        [Tooltip("Distancia desde el centro del objeto hasta los pies (se calcula sola en Eco)")]
        public Vector2 offsetPies = Vector2.zero;

        public Vector2 PuntoPies => (Vector2)transform.position + offsetPies;

        private void OnEnable()
        {
            if (!Todos.Contains(this)) Todos.Add(this);
        }

        private void OnDisable()
        {
            Todos.Remove(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(PuntoPies, 0.1f);
        }
    }
}
