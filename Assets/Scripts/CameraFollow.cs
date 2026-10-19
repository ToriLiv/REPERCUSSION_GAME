using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform objective;                       //----echo
    public float blur = 8f;
    public Vector3 movement = new Vector3(0f, 0f, -10f);   //---camera z 

    private void LateUpdate()
    {
        if (objective == null) return;
        Vector3 goal = objective.position + movement;
        float t = 1f - Mathf.Exp(-blur  * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, goal, t);
    }
}
