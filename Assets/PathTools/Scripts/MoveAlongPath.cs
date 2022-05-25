using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Romi.PathTools
{
    public class MoveAlongPath : MonoBehaviour
    {
        [SerializeField] PathScript path;
        [SerializeField] float speed = 2f, rotationSpeed = 5f;
        [SerializeField] LoopMode loopMode;

        [Space(20)]
        [SerializeField] bool useCustomUpVector;
        [SerializeField] Vector3 customUpVector = Vector3.up;

        [Header("Debug")]
        [SerializeField] float distance;
        [SerializeField] float pathLength;

        private float runtimeDistance;
        private float speedDirection = 1f;

        //only for loop mode stop, to stop update from running
        private bool arrived;

        private void Start()
        {
            runtimeDistance = 0f;
        }

        // Update is called once per frame
        void Update()
        {
            if (arrived)
                return;

            runtimeDistance += speed * speedDirection * Time.deltaTime;

            if (loopMode == LoopMode.PingPong)
            {
                if (runtimeDistance >= path.PathDistance || runtimeDistance <= 0f)
                {
                    speedDirection *= -1f;
                }
            }
            else if (loopMode == LoopMode.Stop)
            {
                var adjustedDistance = path.PathDistance * 0.999f;

                runtimeDistance = Mathf.Clamp(runtimeDistance, 0f, adjustedDistance);

                if (runtimeDistance >= adjustedDistance)
                    arrived = true;
            }
            else if (loopMode == LoopMode.Loop)
            {
                runtimeDistance %= path.PathDistance;
            }

            Debug.Log(runtimeDistance);
            transform.position = path.GetPositionAtDistance(runtimeDistance);
            Quaternion targetRot = path.GetRotationAtDistance(runtimeDistance, useCustomUpVector ? customUpVector : path.GetUpVectorAtDistance(runtimeDistance));
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!path.IsPathReady())
                return;

            transform.position = path.GetPositionAtDistance(distance);
            Quaternion targetRot = path.GetRotationAtDistance(distance, path.GetUpVectorAtDistance(distance));
            transform.rotation = targetRot;

            pathLength = path.PathDistance;
        }
#endif
    }
}