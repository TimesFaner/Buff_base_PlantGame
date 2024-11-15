/****************************************************************************
 * Copyright (c) 2017 Thor Brigsted UNDER MIT LICENSE  see licenses.txt
 * Copyright (c) 2022 liangxiegame UNDER Paid MIT LICENSE  see licenses.txt
 *
 * xNode: https://github.com/Siccity/xNode
 ****************************************************************************/

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QFramework
{
    [InitializeOnLoad]
    public partial class GUIGraphWindow : EditorWindow, IUnRegisterList
    {
        public static GUIGraphWindow current;
        [SerializeField] private NodePortReference[] _references = new NodePortReference[0];
        [SerializeField] private Rect[] _rects = new Rect[0];
        public GUIGraph graph;

        private Func<bool> _isDocked;

        private Vector2 _panOffset;

        private float _zoom = 1;

        /// <summary> Stores node positions for all nodePorts. </summary>
        public Dictionary<GUIGraphNodePort, Rect> portConnectionPoints { get; } = new();

        private Func<bool> isDocked
        {
            get
            {
                if (_isDocked == null) _isDocked = this.GetIsDockedDelegate();
                return _isDocked;
            }
        }

        public Dictionary<GUIGraphNode, Vector2> nodeSizes { get; } = new();

        public Vector2 panOffset
        {
            get => _panOffset;
            set
            {
                _panOffset = value;
                Repaint();
            }
        }

        public float zoom
        {
            get => _zoom;
            set
            {
                _zoom = Mathf.Clamp(value, GUIGraphPreferences.GetSettings().minZoom,
                    GUIGraphPreferences.GetSettings().maxZoom);
                Repaint();
            }
        }

        private void OnEnable()
        {
            // Reload portConnectionPoints if there are any
            var length = _references.Length;
            if (length == _rects.Length)
                for (var i = 0; i < length; i++)
                {
                    var nodePort = _references[i].GetNodePort();
                    if (nodePort != null)
                        portConnectionPoints.Add(nodePort, _rects[i]);
                }

            LocaleKitEditor.IsCN.RegisterWithInitValue(_ => { graphEditor?.OnLanguageChanged(); })
                .AddToUnregisterList(this);
        }

        private void OnDisable()
        {
            // Cache portConnectionPoints before serialization starts
            var count = portConnectionPoints.Count;
            _references = new NodePortReference[count];
            _rects = new Rect[count];
            var index = 0;
            foreach (var portConnectionPoint in portConnectionPoints)
            {
                _references[index] = new NodePortReference(portConnectionPoint.Key);
                _rects[index] = portConnectionPoint.Value;
                index++;
            }

            this.UnRegisterAll();
        }

        private void OnFocus()
        {
            current = this;
            ValidateGraphEditor();
            if (graphEditor != null)
            {
                graphEditor.OnWindowFocus();
                if (GUIGraphPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            }

            dragThreshold = Math.Max(1f, Screen.width / 1000f);
        }

        private void OnLostFocus()
        {
            if (graphEditor != null) graphEditor.OnWindowFocusLost();
        }

        public List<IUnRegister> UnregisterList { get; } = new();

        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        /// <summary> Handle Selection Change events</summary>
        private static void OnSelectionChanged()
        {
            var nodeGraph = Selection.activeObject as GUIGraph;
            if (nodeGraph && !AssetDatabase.Contains(nodeGraph)) Open(nodeGraph);
        }

        /// <summary> Make sure the graph editor is assigned and to the right object </summary>
        private void ValidateGraphEditor()
        {
            var graphEditor = GUIGraphEditor.GetEditor(graph, this);
            if (this.graphEditor != graphEditor && graphEditor != null)
            {
                this.graphEditor = graphEditor;
                graphEditor.OnOpen();
            }
        }

        /// <summary> Create editor window </summary>
        public static GUIGraphWindow Init()
        {
            var w = CreateInstance<GUIGraphWindow>();
            w.titleContent = new GUIContent("GraphKit.IMGUI");
            w.wantsMouseMove = true;
            w.Show();
            return w;
        }

        public void Save()
        {
            if (AssetDatabase.Contains(graph))
            {
                EditorUtility.SetDirty(graph);
                if (GUIGraphPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            }
            else
            {
                SaveAs();
            }
        }

        public void SaveAs()
        {
            var path = EditorUtility.SaveFilePanelInProject("Save NodeGraph", "NewNodeGraph", "asset", "");
            if (string.IsNullOrEmpty(path)) return;

            var existingGraph = AssetDatabase.LoadAssetAtPath<GUIGraph>(path);
            if (existingGraph != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(graph, path);
            EditorUtility.SetDirty(graph);
            if (GUIGraphPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
        }

        private void DraggableWindow(int windowID)
        {
            GUI.DragWindow();
        }

        public Vector2 WindowToGridPosition(Vector2 windowPosition)
        {
            return (windowPosition - position.size * 0.5f - panOffset / zoom) * zoom;
        }

        public Vector2 GridToWindowPosition(Vector2 gridPosition)
        {
            return position.size * 0.5f + panOffset / zoom + gridPosition / zoom;
        }

        public Rect GridToWindowRectNoClipped(Rect gridRect)
        {
            gridRect.position = GridToWindowPositionNoClipped(gridRect.position);
            return gridRect;
        }

        public Rect GridToWindowRect(Rect gridRect)
        {
            gridRect.position = GridToWindowPosition(gridRect.position);
            gridRect.size /= zoom;
            return gridRect;
        }

        public Vector2 GridToWindowPositionNoClipped(Vector2 gridPosition)
        {
            var center = position.size * 0.5f;
            // UI Sharpness complete fix - Round final offset not panOffset
            var xOffset = Mathf.Round(center.x * zoom + (panOffset.x + gridPosition.x));
            var yOffset = Mathf.Round(center.y * zoom + (panOffset.y + gridPosition.y));
            return new Vector2(xOffset, yOffset);
        }

        public void SelectNode(GUIGraphNode node, bool add)
        {
            if (add)
            {
                var selection = new List<Object>(Selection.objects);
                selection.Add(node);
                Selection.objects = selection.ToArray();
            }
            else
            {
                Selection.objects = new Object[] { node };
            }
        }

        public void DeselectNode(GUIGraphNode node)
        {
            var selection = new List<Object>(Selection.objects);
            selection.Remove(node);
            Selection.objects = selection.ToArray();
        }

        [OnOpenAsset(0)]
        public static bool OnOpen(int instanceID, int line)
        {
            var nodeGraph = EditorUtility.InstanceIDToObject(instanceID) as GUIGraph;
            if (nodeGraph != null)
            {
                Open(nodeGraph);
                return true;
            }

            return false;
        }


        private static void TryUpdateGraphWindowName(GUIGraph graph)
        {
            var w = GetWindow(typeof(GUIGraphWindow), false, graph.Name, true) as GUIGraphWindow;
            if (w.titleContent.text != graph.Name) w.titleContent.text = graph.Name;
        }

        public static GUIGraphWindow OpenWithGraph(GUIGraph graph)
        {
            TryUpdateGraphWindowName(graph);
            if (!graph) return null;
            var window = Open(graph);
            var sceneView = GetWindow<SceneView>();
            sceneView.AddTab(window);
            return window;
        }


        /// <summary>Open the provided graph in the NodeEditor</summary>
        private static GUIGraphWindow Open(GUIGraph graph)
        {
            TryUpdateGraphWindowName(graph);
            if (!graph) return null;
            var w = GetWindow(typeof(GUIGraphWindow), false, graph.Name, true) as GUIGraphWindow;
            w.wantsMouseMove = true;
            w.graph = graph;
            return w;
        }

        /// <summary> Repaint all open NodeEditorWindows. </summary>
        public static void RepaintAll()
        {
            var windows = Resources.FindObjectsOfTypeAll<GUIGraphWindow>();
            for (var i = 0; i < windows.Length; i++) windows[i].Repaint();
        }

        [Serializable]
        private class NodePortReference
        {
            [SerializeField] private GUIGraphNode _node;
            [SerializeField] private string _name;

            public NodePortReference(GUIGraphNodePort nodePort)
            {
                _node = nodePort.node;
                _name = nodePort.fieldName;
            }

            public GUIGraphNodePort GetNodePort()
            {
                if (_node == null) return null;

                return _node.GetPort(_name);
            }
        }
    }
}
#endif