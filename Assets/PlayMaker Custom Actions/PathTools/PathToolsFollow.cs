#if PLAYMAKER
using UnityEngine;
using Romi.PathTools;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("PathTools")]
	[ActionTarget(typeof(PathScript), "path")]
	public class PathToolsFollow : FsmStateAction
	{
		public FsmOwnerDefault objectFollowPath;

		[ActionSection("Path Settings")]
		[RequiredField]
		[ObjectType(typeof(PathScript))]
		public FsmObject path;

		public FsmFloat speed;

		public FsmFloat rotationSpeed;
		public FsmBool followPathRotation;

		[Tooltip("Set it to Use Variable:NONE, to use the path nodes up vector")]
		public FsmVector3 customUpVector;

		[ObjectType(typeof(LoopMode))]
		public FsmEnum loopType;

		[ActionSection("Result")]
		[UIHint(UIHint.Variable)]
		[Tooltip("Get the distance travelled so far.")]
		public FsmFloat currentDistance;

		[UIHint(UIHint.Variable)]
		[Tooltip("Get the path total distance.")]
		public FsmFloat pathDistance;

		[ActionSection("Event")]
		[Tooltip("Only for Loop type STOP")]
		public FsmEvent OnFinishedEvent;

		private float distance;
		private PathScript currentPath;
		private Transform objTransform;

		// Code that runs on entering the state.
		public override void OnEnter()
		{
			distance = 0f;
			currentPath = (PathScript)path.Value;
			objTransform = Fsm.GetOwnerDefaultTarget(objectFollowPath).transform;

			if (!pathDistance.IsNone)
			{
				pathDistance.Value = currentPath.PathDistance;
			}
		}

		// Code that runs every frame.
		public override void OnUpdate()
		{
			distance += speed.Value * Time.deltaTime;

			if (distance >= currentPath.PathDistance * 0.999f)
            {
				if ((LoopMode)loopType.Value == LoopMode.Stop)
                {
					Fsm.Event(OnFinishedEvent);
					Finish();
                }
			}

			if ((LoopMode)loopType.Value == LoopMode.PingPong)
			{
				if (distance >= currentPath.PathDistance || distance <= 0f)
					speed.Value *= -1f;
			}

			if ((LoopMode)loopType.Value == LoopMode.Loop)
			{
				distance %= currentPath.PathDistance;
			}

			objTransform.position = currentPath.GetPositionAtDistance(distance);

			if (followPathRotation.Value)
            {
				Vector3 up = customUpVector.IsNone ? currentPath.GetUpVectorAtDistance(distance) : customUpVector.Value;
				objTransform.rotation = Quaternion.Lerp(objTransform.rotation, currentPath.GetRotationAtDistance(distance, up), rotationSpeed.Value * Time.deltaTime);
			}

			if (!currentDistance.IsNone)
            {
				currentDistance.Value = distance;
            }
		}
	}
}
#endif
