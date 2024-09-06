using UnityEditor;
using UnityEngine;

namespace Romi.PathTools
{
    public class BakedPath : PathBase
    {
        [SerializeField] private AnimationCurve[] position;
        [SerializeField] private AnimationCurve[] orientation;
        [SerializeField] private AnimationCurve[] upVector;
        [SerializeField, HideInInspector] private float distance;

        public override float PathDistance => distance;

        public void Bake(PathScript path)
        {
            if (path == null) return;

            distance = path.PathDistance;

            var step = path.Step;

            var currentDistance = 0f;

            position = new AnimationCurve[3];
            orientation = new AnimationCurve[4];
            upVector = new AnimationCurve[3];

            while (currentDistance < distance)
            {
                var t = currentDistance / distance;

                var pos = path.GetPositionAtDistance(currentDistance, true);
                var rot = path.GetRotationAtDistance(currentDistance, path.GetUpVectorAtDistance(currentDistance));
                var up = path.GetUpVectorAtDistance(currentDistance);

                for (var i = 0; i < position.Length; i++)
                {
                    if (position[i] == null)
                    {
                        position[i] = new AnimationCurve();
                        position[i].preWrapMode = WrapMode.Loop;
                        position[i].postWrapMode = WrapMode.Loop;
                    }

                    position[i].AddKey(t, pos[i]);
                }

                for (var i = 0; i < orientation.Length; i++)
                {
                    if (orientation[i] == null)
                    {
                        orientation[i] = new AnimationCurve();
                        orientation[i].preWrapMode = WrapMode.Loop;
                        orientation[i].postWrapMode = WrapMode.Loop;
                    }

                    orientation[i].AddKey(t, rot[i]);
                }

                for (var i = 0; i < upVector.Length; i++)
                {
                    if (upVector[i] == null)
                    {
                        upVector[i] = new AnimationCurve();
                        upVector[i].preWrapMode = WrapMode.Loop;
                        upVector[i].postWrapMode = WrapMode.Loop;
                    }

                    upVector[i].AddKey(t, up[i]);
                }

                currentDistance += step;
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public override Vector3 GetPositionAtDistance(float distance, bool local = false)
        {
            var t = distance / PathDistance;
            var pos = new Vector3(position[0].Evaluate(t), position[1].Evaluate(t), position[2].Evaluate(t));
            return transform.TransformPoint(pos);
        }

        public override Quaternion GetRotationAtDistance(float distance)
        {
            var t = distance / PathDistance;
            var rot = new Quaternion(orientation[0].Evaluate(t), orientation[1].Evaluate(t), orientation[2].Evaluate(t), orientation[3].Evaluate(t));
            return rot;
        }

        public override Quaternion GetRotationAtDistance(float distance, Vector3 up)
        {
            return GetRotationAtDistance(distance);
        }

        public override Vector3 GetUpVectorAtDistance(float distance)
        {
            var t = distance / PathDistance;
            var up = new Vector3(upVector[0].Evaluate(t), upVector[1].Evaluate(t), upVector[2].Evaluate(t));
            return up;
        }

        public override bool IsPathReady()
        {
            var ready = true;

            foreach (var item in position)
            {
                ready &= item.length > 0;
            }

            foreach (var item in orientation)
            {
                ready &= item.length > 0;
            }

            foreach (var item in upVector)
            {
                ready &= item.length > 0;
            }

            return ready;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var step = 0.05f;

            for (float i = step; i < PathDistance; i += step)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(GetPositionAtDistance(i), GetPositionAtDistance(i - step));
            }
        }
#endif
    }
}