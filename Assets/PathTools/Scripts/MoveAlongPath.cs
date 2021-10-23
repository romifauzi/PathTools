using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Romi.PathTools
{
    public class MoveAlongPath : MonoBehaviour
    {
        [SerializeField] PathScript path;
        [SerializeField] float speed = 2f, rotationSpeed = 5f;

        [Header("Debug")]
        [SerializeField] float distance;

        private float runtimeDistance;

        private void Start()
        {
            runtimeDistance = 0f;
        }

        // Update is called once per frame
        void Update()
        {
            runtimeDistance += speed * Time.deltaTime;
            transform.position = path.GetPositionAtDistance(runtimeDistance);
            Quaternion targetRot = path.GetRotationAtDistance(runtimeDistance, path.GetUpVectorAtDistance(runtimeDistance));
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        private void OnValidate()
        {
            if (!path.IsPathReady())
                return;

            transform.position = path.GetPositionAtDistance(distance);
            Quaternion targetRot = path.GetRotationAtDistance(distance, path.GetUpVectorAtDistance(distance));
            transform.rotation = targetRot;
        }
    }
}