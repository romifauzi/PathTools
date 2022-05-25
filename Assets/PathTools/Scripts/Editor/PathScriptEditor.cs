using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Romi.PathTools
{
    [CustomEditor(typeof(PathScript))]
    public class PathScriptEditor : Editor
    {
        PathScript source;
        int selectedId = -1;
        float pickSize = 1f;
        float subPickSize = 0.3f;

        SerializedProperty handleSize;

        SelectedNode currentSelectedNode;

        private void OnEnable()
        {
            source = (PathScript)target;
            handleSize = serializedObject.FindProperty("handleMulti");
        }

        private void OnSceneGUI()
        {
            for (int i = 0; i < source.Nodes.Count; i++)
            {
                Vector3 nodePos = LocalToWorld(source.Nodes[i].localPos);
                Handles.color = Color.green;

                if (i == selectedId)
                    continue;

                if (Handles.Button(nodePos, Quaternion.identity, handleSize.floatValue, HandleUtility.GetHandleSize(nodePos) * pickSize, Handles.SphereHandleCap))
                {
                    source.lastPos = source.Nodes[i].localPos;
                    source.lastLeftHandlePos = source.Nodes[i].leftHandle;
                    source.lastRightHandlePos = source.Nodes[i].rightHandle;
                    currentSelectedNode = SelectedNode.Main;
                    selectedId = i;
                }
            }

            if (selectedId >= 0)
                DrawBezierControl(selectedId);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (source.Nodes.Count == 0)
                selectedId = -1;

            EditorGUI.BeginChangeCheck();

            Undo.RecordObject(source, "Modify Path Properties");

            DrawNodeInspector(selectedId);

            if (GUILayout.Button("Add Node"))
                source.AddNode();

            source.closeLoop = EditorGUILayout.Toggle("Close Loop", source.closeLoop);
            source.showUpVector = EditorGUILayout.Toggle("Show Orientation", source.showUpVector);

            EditorGUILayout.PropertyField(handleSize, new GUIContent("Handle Size"));

            EditorGUILayout.LabelField(string.Format("Path Length: {0}", source.PathDistance));

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(source);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawNodeInspector(int id)
        {
            EditorGUILayout.BeginVertical("Box");
            if (id < 0)
                EditorGUILayout.LabelField(string.Format("No selected node"));
            else
            {
                EditorGUILayout.LabelField(string.Format("Current selected Node: {0}", id));
                source.Nodes[id].orientation = EditorGUILayout.FloatField("Orientation: ", source.Nodes[id].orientation);
                source.Nodes[id].tangentType = (TangentType)EditorGUILayout.EnumPopup("Tangent Type: ", source.Nodes[id].tangentType);
            }

            EditorGUILayout.EndVertical();
        }

        void DrawBezierControl(int id)
        {
            EditorGUI.BeginChangeCheck();

            Undo.RecordObject(source, "Modify Path Nodes");

            switch(currentSelectedNode)
            {
                case SelectedNode.Main:
                    //show position handle for main node, show button for tangent nodes
                    source.Nodes[id].localPos = WorldToLocal(Handles.PositionHandle(LocalToWorld(source.Nodes[id].localPos), Quaternion.identity));
                    DrawNodeButton(source.Nodes[id].leftHandle, SelectedNode.LeftTangent, handleSize.floatValue * 0.6f, Color.red);
                    DrawNodeButton(source.Nodes[id].rightHandle, SelectedNode.RightTangent, handleSize.floatValue * 0.6f, Color.red);
                    break;
                case SelectedNode.LeftTangent:
                    source.Nodes[id].leftHandle = WorldToLocal(Handles.PositionHandle(LocalToWorld(source.Nodes[id].leftHandle), Quaternion.identity));
                    DrawNodeButton(source.Nodes[id].localPos, SelectedNode.Main, handleSize.floatValue, Color.green);
                    DrawNodeButton(source.Nodes[id].rightHandle, SelectedNode.RightTangent, handleSize.floatValue * 0.6f, Color.red);
                    break;
                case SelectedNode.RightTangent:
                    source.Nodes[id].rightHandle = WorldToLocal(Handles.PositionHandle(LocalToWorld(source.Nodes[id].rightHandle), Quaternion.identity));
                    DrawNodeButton(source.Nodes[id].localPos, SelectedNode.Main, handleSize.floatValue, Color.green);
                    DrawNodeButton(source.Nodes[id].leftHandle, SelectedNode.LeftTangent, handleSize.floatValue * 0.6f, Color.red);
                    break;
            }

            Handles.color = Color.yellow;
            Handles.DrawDottedLine(LocalToWorld(source.Nodes[id].leftHandle), LocalToWorld(source.Nodes[id].localPos), 3f);
            Handles.DrawDottedLine(LocalToWorld(source.Nodes[id].localPos), LocalToWorld(source.Nodes[id].rightHandle), 3f);

            //source.UpdatePath();

            MovedPoints(source.Nodes[id]);

            if (source.Nodes[id].tangentType == TangentType.Aligned)
                MovedTangent(source.Nodes[id]);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(source);
            }
        }

        private void DrawNodeButton(Vector3 pos, SelectedNode newNode, float radius, Color color = default)
        {
            Vector3 nodePos = LocalToWorld(pos);
            Handles.color = color;
            if (Handles.Button(nodePos, Quaternion.identity, radius, HandleUtility.GetHandleSize(nodePos) * subPickSize, Handles.SphereHandleCap))
            {
                currentSelectedNode = newNode;
            }
        }

        private void MovedPoints(Node node)
        {
            if (source.lastPos == node.localPos)
                return;

            Vector3 delta = node.localPos - source.lastPos;
            node.leftHandle += delta;
            node.rightHandle += delta;
            source.lastPos = node.localPos;
        }

        private void MovedTangent(Node node)
        {
            if (source.lastLeftHandlePos != node.leftHandle)
            {
                node.rightHandle = AdjustTangent(node.leftHandle, node.rightHandle, node.localPos);
                source.lastLeftHandlePos = node.leftHandle;
            }
            else if (source.lastRightHandlePos != node.rightHandle)
            {
                node.leftHandle = AdjustTangent(node.rightHandle, node.leftHandle, node.localPos);
                source.lastRightHandlePos = node.rightHandle;
            }
        }

        Vector3 AdjustTangent(Vector3 movedTangent, Vector3 followTangent, Vector3 midPoint)
        {
            Vector3 direction = (movedTangent - midPoint).normalized;

            float adjustLength = (followTangent - midPoint).magnitude;

            return midPoint + (-direction * adjustLength);
        }

        Vector3 LocalToWorld(Vector3 localPos)
        {
            return source.transform.TransformPoint(localPos);
        }

        Vector3 WorldToLocal(Vector3 worldPos)
        {
            return source.transform.InverseTransformPoint(worldPos);
        }
    }
}