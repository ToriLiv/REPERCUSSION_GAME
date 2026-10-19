using UnityEngine;

//--animation with sprites depending on direction of movement witohout using an animator controller. This script requires a SpriteRenderer component to work properly.
[RequireComponent(typeof(SpriteRenderer))]
public class DirectionSprites : MonoBehaviour
{
    [Header("Cuadros de caminar, en orden (4 por direccion)")]
    public Sprite[] front;       //--walk down
    public Sprite[] back;        //--walk up
    public Sprite[] right;       //--walk right
    public Sprite[] left;        //--walk left

    [Header("Ajustes")]
    public float squarePerSecond = 8f;

    private const float GraceTime = 0.08f;   //----avoid flickering when changing direction

    private SpriteRenderer sr;
    private Vector2 lastPosition;
    private Sprite[] actual;
    private float time;
    private float withoutMovement = 10f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        lastPosition = transform.position;
        actual = front;
    }

    private void LateUpdate()
    {
        Vector2 pos = transform.position;
        Vector2 delta = pos - lastPosition;
        lastPosition = pos;

        if (delta.sqrMagnitude > 0.00001f)
        {
            withoutMovement = 0f;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                actual = delta.x > 0f ? right : left;
            else
                actual = delta.y > 0f ? back : front;
        }
        else
        {
            withoutMovement += Time.deltaTime;
        }

        if (actual == null || actual.Length == 0) return;

        if (withoutMovement < GraceTime)                       //--walk
        {
            time += Time.deltaTime * squarePerSecond;
            sr.sprite = actual[(int)time % actual.Length];
        }
        else                                               //--idle
        {
            time = 0f;
            sr.sprite = actual[0];
        }
    }
}
