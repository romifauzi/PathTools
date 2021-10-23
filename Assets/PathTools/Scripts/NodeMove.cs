using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeMove : MonoBehaviour
{
    public float speed = 2f, rotateSpeed = 2f, bankingValue = 5f;
    public bool rotateObject, loopMove;
    public int loopToNode;
    public List<Vector3> nodes = new List<Vector3>();

    [SerializeField] bool baked;

    private const int CURVE_SEGMENT = 20;
    private int realLoopNode;
    private Transform parent;
    private float nextAngleGrab;

    private List<Vector3> path = new List<Vector3>();

    private Vector3 startPos;

    private Quaternion rotation;

    private void OnEnable()
    {
        path = GetCurveNodes();

        startPos = transform.position;

        DefineParent();

        StartCoroutine(StartMove());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator StartMove()
    {
        float oldAngle = 0f;
        int posID = 1;

        transform.position = startPos + path[0];

        while (loopMove || posID < path.Count - 1)
        {
            //this handles the index of the path vectors, and decide the next target position
            if (((startPos + path[posID]) - transform.position).sqrMagnitude < 0.01f)
            {
                if (loopMove)
                {
                    if (posID < path.Count - 1)
                    {
                        posID += 1;
                    }
                    else
                    {
                        posID = realLoopNode;
                    }
                }
                else
                {
                    if (posID < path.Count - 1)
                    {
                        posID += 1;
                    }
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, startPos + path[posID], speed * Time.deltaTime);

            if (rotateObject)
            {
                if (Time.time > nextAngleGrab)
                {
                    nextAngleGrab = Time.time + 0.5f;

                    Vector3 dir = (startPos + path[posID]) - transform.position;

                    if (dir.sqrMagnitude > 0.01f)
                    {
                        rotation = Quaternion.LookRotation(dir, Vector3.up);
                    }

                    float zBank = Mathf.Clamp(rotation.eulerAngles.y - oldAngle, -10f, 10f);

                    Quaternion banking = Quaternion.Euler(0f, 0f, Mathf.Ceil(zBank) * -bankingValue);

                    rotation *= banking;

                    oldAngle = rotation.eulerAngles.y;
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotateSpeed * Time.deltaTime);
            }

            yield return null;
        }
    }

    List<Vector3> GetCurveNodes()
    {
        DefineParent();

        List<Vector3> curvedNodes = new List<Vector3>();

        for (int i = 0; i < nodes.Count - 3; i += 3)
        {
            Vector3 p0 = (nodes[i]);
            Vector3 p1 = (nodes[i + 1]);
            Vector3 p2 = (nodes[i + 2]);
            Vector3 p3 = (nodes[i + 3]);

            for (int j = 0; j <= CURVE_SEGMENT; j++)
            {
                float t = j / (float)CURVE_SEGMENT;
                curvedNodes.Add(CalculateBezierPath(p0, p1, p2, p3, t));
            }
        }

        realLoopNode = (int)(curvedNodes.Count * (loopToNode / (float)nodes.Count));

        return curvedNodes;
    }

    private void DefineParent()
    {
        if (transform.parent)
        {
            parent = transform.parent;
        }
        else
        {
            parent = transform;
        }
    }

    Vector3 CalculateBezierPath(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // (1 - t)^3 * p0 + 3(1-t)^2 * t * p1 + 3(1-t) * t^2 * p2 + t^3 * p3

        float oneMinusT = 1f - t;

        Vector3 result = Mathf.Pow(oneMinusT, 3f) * p0 + 3f * Mathf.Pow(oneMinusT, 2f) * t * p1 
            + 3f * oneMinusT * (t * t) * p2 + Mathf.Pow(t, 3f) * p3;

        return result;
    }

    private void OnDrawGizmosSelected()
    {
        DefineParent();

        Vector3 offset = Application.isPlaying ? startPos : transform.position;
        List<Vector3> curvePositions = Application.isPlaying ? path : GetCurveNodes();

        for (int i = 1; i < curvePositions.Count; i++)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(offset + curvePositions[i - 1], offset + curvePositions[i]);
        }

        for (int i = 1; i < nodes.Count; i++)
        {
            Color gizmoColor = Color.yellow;
            gizmoColor.a = 0.5f;
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(offset + (nodes[i - 1]), offset + (nodes[i]));
        }
    }
}
