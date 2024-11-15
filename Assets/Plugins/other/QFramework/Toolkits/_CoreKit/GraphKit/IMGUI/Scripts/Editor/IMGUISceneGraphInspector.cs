/****************************************************************************
 * Copyright (c) 2017 Thor Brigsted UNDER MIT LICENSE  see licenses.txt
 * Copyright (c) 2022 liangxiegame UNDER Paid MIT LICENSE  see licenses.txt
 *
 * xNode: https://github.com/Siccity/xNode
 ****************************************************************************/

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace QFramework
{
    [CustomEditor(typeof(GUISceneGraph), true)]
    public class GUISceneGraphInspector : Editor
    {
        private Type graphType;
        private bool removeSafely;
        private GUISceneGraph sceneGraph;

        private void OnEnable()
        {
            sceneGraph = target as GUISceneGraph;
            var sceneGraphType = sceneGraph.GetType();
            if (sceneGraphType == typeof(GUISceneGraph))
            {
                graphType = null;
            }
            else
            {
                var baseType = sceneGraphType.BaseType;
                if (baseType.IsGenericType) graphType = sceneGraphType = baseType.GetGenericArguments()[0];
            }
        }

        /// <summary>
        ///     在场景的 Inspector 上渲染
        ///     Render in scene obj's inspecotor
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (sceneGraph.graph == null)
            {
                if (GUILayout.Button("New graph", GUILayout.Height(40)))
                {
                    if (graphType == null)
                    {
                        var graphTypes = typeof(GUIGraph).GetDerivedTypes();
                        var menu = new GenericMenu();
                        for (var i = 0; i < graphTypes.Length; i++)
                        {
                            var graphType = graphTypes[i];
                            menu.AddItem(new GUIContent(graphType.Name), false, () => CreateGraph(graphType));
                        }

                        menu.ShowAsContext();
                    }
                    else
                    {
                        CreateGraph(graphType);
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Open graph", GUILayout.Height(40)))
                    GUIGraphWindow.OpenWithGraph(sceneGraph.graph);

                if (removeSafely)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Really remove graph?");
                    GUI.color = new Color(1, 0.8f, 0.8f);
                    if (GUILayout.Button("Remove"))
                    {
                        removeSafely = false;
                        Undo.RecordObject(sceneGraph, "Removed graph");
                        sceneGraph.graph = null;
                    }

                    GUI.color = Color.white;
                    if (GUILayout.Button("Cancel")) removeSafely = false;

                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUI.color = new Color(1, 0.8f, 0.8f);
                    if (GUILayout.Button("Remove graph")) removeSafely = true;

                    GUI.color = Color.white;
                }
            }
        }

        public void CreateGraph(Type type)
        {
            Undo.RecordObject(sceneGraph, "Create graph");
            sceneGraph.graph = CreateInstance(type) as GUIGraph;
            sceneGraph.graph.name = sceneGraph.name + "-graph";
        }
    }
}
#endif