using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Romi.PathTools
{
    public class PathScript : MonoBehaviour
    {
        #region VARIABLES
        [SerializeField] private List<Node> nodes = new List<Node>();
        [SerializeField] private int selectedId;
        public bool closeLoop, showUpVector;

        private const int CURVE_SEGMENT = 20;

        public Vector3 lastPos, lastLeftHandlePos, lastRightHandlePos;

        private List<Vector3> curvedPositions = new List<Vector3>();
        private List<float> orientations = new List<float>();
        #endregion

        #region PROPERTIES
        public List<Node> Nodes { get => nodes; }
        public int SelectedId { get => selectedId; set => selectedId = value; }
        public float PathDistance { get; private set; }
        #endregion

        #region PRIVATE METHODS
        private void Awake()
        {
            UpdatePath();
        }

        Vector3 CalculateBezierPath(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            // (1 - t)^3 * p0 + 3(1-t)^2 * t * p1 + 3(1-t) * t^2 * p2 + t^3 * p3

            float oneMinusT = 1f - t;

            Vector3 result = Mathf.Pow(oneMinusT, 3f) * p0 + 3f * Mathf.Pow(oneMinusT, 2f) * t * p1
                + 3f * oneMinusT * (t * t) * p2 + Mathf.Pow(t, 3f) * p3;

            return result;
        }

        List<Vector3> GetCurveNodes()
        {
            List<Vector3> curvedNodes = new List<Vector3>();

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                Vector3 p0 = (nodes[i].localPos);
                Vector3 p1 = (nodes[i].rightHandle);
                Vector3 p2 = (nodes[i + 1].leftHandle);
                Vector3 p3 = (nodes[i + 1].localPos);
                for (int j = 0; j < CURVE_SEGMENT; j++)
                {
                    float t = j / (float)CURVE_SEGMENT;
                    curvedNodes.Add(CalculateBezierPath(p0, p1, p2, p3, t));
                }
            }

            if (closeLoop)
            {
                int id = nodes.Count - 1;

                Vector3 p0 = (nodes[id].localPos);
                Vector3 p1 = (nodes[id].rightHandle);
                Vector3 p2 = (nodes[0].leftHandle);
                Vector3 p3 = (nodes[0].localPos);

                for (int j = 0; j < CURVE_SEGMENT; j++)
                {
                    float t = j / (float)CURVE_SEGMENT;
                    curvedNodes.Add(CalculateBezierPath(p0, p1, p2, p3, t));
                }
            }

            return curvedNodes;
        }

        List<float> GetOrientationAlongCurve()
        {
            List<float> orienationNodes = new List<float>();

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                for (int j = 0; j < CURVE_SEGMENT; j++)
                {
                    float t = j / (float)CURVE_SEGMENT;

                    orienationNodes.Add(Mathf.Lerp(nodes[i].orientation, nodes[i + 1].orientation, t));
                }
            }

            if (closeLoop)
            {
                int id = nodes.Count - 1;

                for (int j = 0; j < CURVE_SEGMENT; j++)
                {
                    float t = j / (float)CURVE_SEGMENT;

                    orienationNodes.Add(Mathf.Lerp(nodes[id].orientation, nodes[0].orientation, t));
                }
            }

            return orienationNodes;
        }

        Vector3 LocalToWorld(Vector3 localPos)
        {
            return transform.TransformPoint(localPos);
        }

        Vector3 WorldToLocal(Vector3 worldPos)
        {
            return transform.InverseTransformPoint(worldPos);
        }

        //convert distance
        private void GetPrecisePoint(float distance, int count, out int posIndex, out float precision)
        {
            //loop distance when below 0 or exceed max pathDistance
            distance = PathDistance + (distance % PathDistance);

            //normalize distance to range 0-1
            float normalizedDistance = (distance % PathDistance) / PathDistance;

            //convert distance to the corresponding curved positions list Index
            float distanceToIndex = normalizedDistance * count;

            //Floor the resulting index
            posIndex = Mathf.FloorToInt(distanceToIndex);

            //extract the decimals from the resulting index
            precision = distanceToIndex - posIndex;
        }

        #endregion

        #region PUBLIC METHODS
        public void AddNode()
        {
            Vector3 randomPos = Random.insideUnitCircle;
            randomPos = new Vector3(randomPos.x, randomPos.z, randomPos.y);

            Vector3 newPos = (nodes.Count > 0 ? nodes[nodes.Count - 1].localPos : transform.position) + (randomPos * 3f);
            nodes.Add(new Node(newPos));
        }

        public void UpdatePath()
        {
            curvedPositions = GetCurveNodes();
            orientations = GetOrientationAlongCurve();

            PathDistance = 0f;

            for (int i = 0; i < curvedPositions.Count; i++)
            {
                Vector3 a = (closeLoop && i == 0) ? curvedPositions[curvedPositions.Count - 1] : curvedPositions[Mathf.Max(i - 1, 0)];
                Vector3 b = curvedPositions[i];
                float distance = (b - a).magnitude;
                PathDistance += distance;
            }
        }

        public Vector3 GetPositionAtDistance(float distance)
        {
            Vector3 pos = Vector3.zero;

            GetPrecisePoint(distance, curvedPositions.Count, out int posIndex, out float precision);

            bool lastPosInList = posIndex == curvedPositions.Count - 1;

            //define the next index
            int nextId = lastPosInList ? 0 : posIndex + 1;

            if (lastPosInList && !closeLoop)
                precision = 1f;

            //get the precise position on curve at distance
            pos = transform.TransformPoint(Vector3.Lerp(curvedPositions[posIndex], curvedPositions[nextId], precision));

            return pos;
        }

        public Quaternion GetRotationAtDistance(float distance, Vector3 up)
        {
            Quaternion quat;

            GetPrecisePoint(distance, curvedPositions.Count, out int posIndex, out float precision);

            int nextId = posIndex == curvedPositions.Count - 1 ? 0 : posIndex + 1;

            quat = Quaternion.LookRotation(curvedPositions[nextId] - curvedPositions[posIndex], up);

            return quat;
        }

        public Quaternion GetRotationAtDistance(float distance)
        {
            return GetRotationAtDistance(distance, Vector3.up);
        }

        public Vector3 GetUpVectorAtDistance(float distance)
        {
            GetPrecisePoint(distance, curvedPositions.Count, out int posIndex, out float precision);

            //draw point up with orientation influence
            Vector3 direction = (curvedPositions[posIndex] - curvedPositions[posIndex == 0 ? (curvedPositions.Count - 1) : (posIndex - 1)]).normalized;
            Vector3 finalDirection = Quaternion.AngleAxis(orientations[posIndex], direction) * Vector3.up;

            return finalDirection;
        }

        public bool IsPathReady()
        {
            return curvedPositions.Count > 0;
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                UpdatePath();

            for (int i = 0; i < curvedPositions.Count; i++)
            {
                if (!closeLoop && i == 0)
                    continue;

                Gizmos.color = Color.green;
                Gizmos.DrawLine(LocalToWorld(curvedPositions[i == 0 ? curvedPositions.Count - 1 : i - 1]), LocalToWorld(curvedPositions[i]));
            }

            if (showUpVector)
            {
                for (int i = 0; i < orientations.Count; i++)
                {
                    //draw point up with orientation influence
                    Vector3 direction = (curvedPositions[i] - curvedPositions[i == 0 ? (curvedPositions.Count - 1) : (i - 1)]).normalized;
                    Vector3 finalDirection = Quaternion.AngleAxis(orientations[i], direction) * Vector3.up;
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(LocalToWorld(curvedPositions[i]), LocalToWorld(curvedPositions[i]) + (finalDirection * 0.4f));
                }
            }
        }
#endif
    }

    [System.Serializable]
    public class Node
    {
        public Vector3 localPos, leftHandle, rightHandle;
        public float orientation;
        public TangentType tangentType = TangentType.Aligned;
        public Node(Vector3 pos)
        {
            localPos = pos;
            leftHandle = pos + Vector3.left;
            rightHandle = pos + Vector3.right;
        }
    }
}

