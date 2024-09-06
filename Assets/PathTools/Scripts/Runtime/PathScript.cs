using System.Collections.Generic;
using UnityEngine;

namespace Romi.PathTools
{
    public class PathScript : PathBase
    {
        #region VARIABLES
        [SerializeField] private List<Node> nodes = new List<Node>();
        [SerializeField] private int selectedId;
#pragma warning disable 0414
        [SerializeField] private float handleMulti = 0.2f;
#pragma warning restore 0414
        [SerializeField] private float step = 0.25f;
        [SerializeField] private BakedPath bakedPathResource;
        public bool closeLoop, showUpVector;

        private float _pathDistance;

        public Vector3 lastPos, lastLeftHandlePos, lastRightHandlePos;

        [SerializeField] private List<Vector3> curvedPositions = new List<Vector3>();
        [SerializeField] private List<float> orientations = new List<float>();
        #endregion

        #region PROPERTIES
        public List<Node> Nodes { get => nodes; }
        public int SelectedId { get => selectedId; set => selectedId = value; }
        public override float PathDistance => _pathDistance;
        public float Step => step;
        #endregion

        #region PRIVATE METHODS
        private void Awake()
        {
            UpdatePath();
        }

        Vector3 CalculateBezierPath(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
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

                Interpolate(ref curvedNodes, p0, p1, p2, p3, i, closeLoop);
            }

            if (closeLoop)
            {
                int id = nodes.Count - 1;

                Vector3 p0 = (nodes[id].localPos);
                Vector3 p1 = (nodes[id].rightHandle);
                Vector3 p2 = (nodes[0].leftHandle);
                Vector3 p3 = (nodes[0].localPos);

                Interpolate(ref curvedNodes, p0, p1, p2, p3, id, closeLoop);
            }

            void Interpolate(ref List<Vector3> refNode, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int i, bool closeLoop)
            {
                int start = !closeLoop ? (i == 0 ? 0 : 1) : 0;
                int endOffset = closeLoop ? -1 : 0;

                var segment = GetSegment(p0, p1, p2, p3, step, out var distance);

                for (int j = start; j <= segment + endOffset; j++)
                {
                    float t = j / (float)segment;

                    var point = CalculateBezierPath(p0, p1, p2, p3, t);

                    refNode.Add(point);
                }
            }

            _pathDistance = 0f;

            for (int i = 0; i < curvedNodes.Count; i++)
            {
                Vector3 a = (closeLoop && i == 0) ? curvedNodes[curvedNodes.Count - 1] : curvedNodes[Mathf.Max(i - 1, 0)];
                Vector3 b = curvedNodes[i];
                float distance = (b - a).magnitude;
                _pathDistance += distance;
            }

            return curvedNodes;
        }

        List<float> GetOrientationAlongCurve()
        {
            List<float> orientationNodes = new List<float>();

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                Interpolate(ref orientationNodes, nodes, i, i + 1, closeLoop);
            }

            if (closeLoop)
            {
                int id = nodes.Count - 1;

                Interpolate(ref orientationNodes, nodes, id, 0, closeLoop);
            }

            void Interpolate(ref List<float> refOrientationNodes, List<Node> _nodes, int i, int next, bool closeLoop)
            {
                int start = !closeLoop ? (i == 0 ? 0 : 1) : 0;
                int endOffset = closeLoop ? -1 : 0;

                var segment = GetSegment(_nodes[i].localPos,_nodes[i].rightHandle,_nodes[next].leftHandle,_nodes[next].localPos, step, out var distance);

                for (int j = start; j <= segment + endOffset; j++)
                {
                    float t = j / (float)segment;

                    var value = Mathf.Lerp(_nodes[i].orientation, _nodes[next].orientation, t);

                    refOrientationNodes.Add(value);
                }
            }

            return orientationNodes;
        }

        private int GetSegment(Vector3 p0,  Vector3 p1, Vector3 p2, Vector3 p3, float step,out float segmentDistance)
        {
            var chord = (p3 - p0).magnitude;
            segmentDistance = (p0 - p1).magnitude + (p1 - p2).magnitude + (p2 - p3).magnitude;
            var segment = (int)(((segmentDistance + chord) / 2)/step);
            return segment;
        }

        Vector3 LocalToWorld(Vector3 localPos)
        {
            return transform.TransformPoint(localPos);
        }

        Vector3 WorldToLocal(Vector3 worldPos)
        {
            return transform.InverseTransformPoint(worldPos);
        }

        #endregion

        #region PUBLIC METHODS
        public void AddNode()
        {
            Vector3 randomPos = UnityEngine.Random.insideUnitCircle;
            randomPos = new Vector3(randomPos.x, randomPos.z, randomPos.y);

            Vector3 newPos = (nodes.Count > 0 ? nodes[nodes.Count - 1].localPos : transform.position) + (randomPos * 3f);
            nodes.Add(new Node(newPos));
        }

        public void RemoveNode(int id)
        {
            nodes.RemoveAt(id);
        }

        public void AdjustNode(int id, Vector3 newPos, bool moveTangent = false)
        {
            if (id >= nodes.Count)
            {
                Debug.LogWarning($"Id {id} is out of range");
                return;
            }

            var newLocalPos = transform.InverseTransformPoint(newPos);
           
            if (moveTangent)
            {
                var offset = newLocalPos - nodes[id].localPos;
                nodes[id].leftHandle += offset;
                nodes[id].rightHandle += offset;
            }

            nodes[id].localPos = transform.InverseTransformPoint(newPos);
            UpdatePath();
        }

        public void UpdatePath()
        {
            curvedPositions = GetCurveNodes();
            orientations = GetOrientationAlongCurve();
        }

        //convert distance
        private void GetPrecisePoint(float distance, int count, out int posIndex, out float precision)
        {
            if (Mathf.Approximately(PathDistance, 0))
            {
                posIndex = 0;
                precision = 0;
                return;
            }    

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

        public override Vector3 GetPositionAtDistance(float distance, bool local = false)
        {
            Vector3 pos = Vector3.zero;

            GetPrecisePoint(distance, curvedPositions.Count, out int posIndex, out float precision);

            bool lastPosInList = posIndex == curvedPositions.Count - 1;

            //define the next index
            int nextId = lastPosInList ? (!closeLoop ? posIndex : 0) : posIndex + 1;

            if (lastPosInList && !closeLoop)
                precision = 0f;

            pos = Vector3.Lerp(curvedPositions[posIndex], curvedPositions[nextId], precision);

            if (local)
                return pos;

            try
            {
                //get the precise position on curve at distance
                pos = transform.TransformPoint(pos);
                return pos;
            }
            catch
            {
                return default;
            }
        }

        public override Quaternion GetRotationAtDistance(float distance, Vector3 up)
        {
            Quaternion quat;

            GetPrecisePoint(distance, curvedPositions.Count, out int posIndex, out float precision);

            //int nextId = posIndex == curvedPositions.Count - 1 ? 0 : posIndex + 1;
            int nextId = posIndex == 0 ? (closeLoop ? curvedPositions.Count - 1 : 1) : posIndex - 1;

            try
            {
                if (!closeLoop && posIndex == 0)
                    quat = Quaternion.LookRotation(curvedPositions[posIndex] - curvedPositions[nextId], up);
                else
                    quat = Quaternion.LookRotation(curvedPositions[nextId] - curvedPositions[posIndex], up);

                return quat;
            }
            catch
            {
                return Quaternion.identity;
            }
        }

        public override Quaternion GetRotationAtDistance(float distance)
        {
            return GetRotationAtDistance(distance, GetUpVectorAtDistance(distance));
        }

        public override Vector3 GetUpVectorAtDistance(float distance)
        {
            GetPrecisePoint(distance, curvedPositions.Count, out int posIndex, out float precision);
            
            Vector3 direction;

            int nextId = posIndex == 0 ? (closeLoop ? curvedPositions.Count - 1 : 1) : posIndex - 1;

            try
            {
                //draw point up with orientation influence
                if (!closeLoop && posIndex == 0)
                    direction = (curvedPositions[nextId] - curvedPositions[posIndex]).normalized;
                else
                    direction = (curvedPositions[posIndex] - curvedPositions[nextId]).normalized;

                Vector3 finalDirection = Quaternion.AngleAxis(orientations[posIndex], direction) * Vector3.up;

                return finalDirection;
            }
            catch
            {
                return default;
            }
        }

        public override bool IsPathReady()
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
                    int nextId = i == 0 ? (closeLoop ? curvedPositions.Count - 1 : 1) : i - 1;
                    Vector3 direction;

                    //draw point up with orientation influence
                    if (!closeLoop && i == 0)
                        direction = (curvedPositions[nextId] - curvedPositions[i]).normalized;
                    else
                        direction = (curvedPositions[i] - curvedPositions[nextId]).normalized;

                    Vector3 finalDirection = Quaternion.AngleAxis(orientations[i], direction) * Vector3.up;
                    var worldPos = LocalToWorld(curvedPositions[i]);
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(worldPos, worldPos + (finalDirection * 0.4f));
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

