#if PLAYMAKER
using Romi.PathTools;
using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("Path Tools")]
	[ActionTarget(typeof(PathScript), "path")]
	public class PathToolsModify : FsmStateAction
	{
		[ActionSection("Path Settings")]
		[RequiredField]
		[ObjectType(typeof(PathScript))]
		public FsmObject path;
		public FsmInt nodeId;
		public FsmVector3 newNodePos;
		public FsmBool moveTangent;

		// Code that runs on entering the state.
		public override void OnEnter()
		{
			var currentPath = (PathScript)path.Value;

			currentPath.AdjustNode(nodeId.Value, newNodePos.Value, moveTangent.Value);

			Finish();
		}
	}

}
#endif