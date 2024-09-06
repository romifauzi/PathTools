using UnityEngine;

namespace Romi.PathTools
{
    public abstract class PathBase : MonoBehaviour
    {
        public abstract Vector3 GetPositionAtDistance(float distance, bool local = false);
        public abstract Quaternion GetRotationAtDistance(float distance, Vector3 up);
        public abstract Quaternion GetRotationAtDistance(float distance);
        public abstract Vector3 GetUpVectorAtDistance(float distance);
        public abstract bool IsPathReady();
        public abstract float PathDistance { get; }
    }
}