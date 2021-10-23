using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(NodeMove))]
public class NodeMoveEditor : Editor
{
    NodeMove source;
    
    public override void OnInspectorGUI()
    {
        source = (NodeMove)target;

        base.OnInspectorGUI();

        if (GUILayout.Button("Add Nodes!"))
        {
            source.nodes.Add(source.transform.position);
        }

        if (GUILayout.Button("Remove Nodes!"))
        {
            source.nodes.RemoveAt(source.nodes.Count - 1);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(source);
            EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
        }
    }

    private void OnSceneGUI()
    {
        source = (NodeMove)target;

        for (int i = 0; i < source.nodes.Count; i++)
        {
            source.nodes[i] = source.transform.InverseTransformPoint(Handles.PositionHandle(source.transform.TransformPoint(source.nodes[i]), Quaternion.identity));
            Handles.Label(source.transform.TransformPoint(source.nodes[i]), "Nodes" + (i + 1));
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(source);
            EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
        }
    }
}
