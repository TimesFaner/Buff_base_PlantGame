/****************************************************************************
 * Copyright (c) 2017 Thor Brigsted UNDER MIT LICENSE  see licenses.txt
 * Copyright (c) 2022 liangxiegame UNDER Paid MIT LICENSE  see licenses.txt
 *
 * xNode: https://github.com/Siccity/xNode
 ****************************************************************************/

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using QFramework.Internal;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QFramework
{
    /// <summary> Contains GUI methods </summary>
    public partial class GUIGraphWindow
    {
        private static readonly Vector3[] polyLineTempArray = new Vector3[2];
        private List<GUIGraphNode> culledNodes;
        public GUIGraphEditor graphEditor;
        private List<Object> selectionCache;

        /// <summary> 19 if docked, 22 if not </summary>
        private int topPadding => isDocked() ? 19 : 22;

        protected virtual void OnGUI()
        {
            var e = Event.current;
            var m = GUI.matrix;
            if (graph == null) return;
            ValidateGraphEditor();
            Controls();

            DrawGrid(position, zoom, panOffset);
            DrawConnections();
            DrawDraggedConnection();
            DrawNodes();
            DrawSelectionBox();
            DrawTooltip();
            graphEditor.OnGUI();

            // Run and reset onLateGUI
            if (onLateGUI != null)
            {
                onLateGUI();
                onLateGUI = null;
            }

            GUI.matrix = m;
        }

        /// <summary>
        ///     Executed after all other window GUI. Useful if Zoom is ruining your day. Automatically resets after being
        ///     run.
        /// </summary>
        public event Action onLateGUI;

        public static void BeginZoomed(Rect rect, float zoom, float topPadding)
        {
            GUI.EndClip();

            GUIUtility.ScaleAroundPivot(Vector2.one / zoom, rect.size * 0.5f);
            var padding = new Vector4(0, topPadding, 0, 0);
            padding *= zoom;
            GUI.BeginClip(new Rect(-(rect.width * zoom - rect.width) * 0.5f,
                -((rect.height * zoom - rect.height) * 0.5f) + topPadding * zoom,
                rect.width * zoom,
                rect.height * zoom));
        }

        public static void EndZoomed(Rect rect, float zoom, float topPadding)
        {
            GUIUtility.ScaleAroundPivot(Vector2.one * zoom, rect.size * 0.5f);
            var offset = new Vector3(
                (rect.width * zoom - rect.width) * 0.5f,
                (rect.height * zoom - rect.height) * 0.5f + -topPadding * zoom + topPadding,
                0);
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);
        }

        public void DrawGrid(Rect rect, float zoom, Vector2 panOffset)
        {
            rect.position = Vector2.zero;

            var center = rect.size / 2f;
            var gridTex = graphEditor.GetGridTexture();
            var crossTex = graphEditor.GetSecondaryGridTexture();

            // Offset from origin in tile units
            var xOffset = -(center.x * zoom + panOffset.x) / gridTex.width;
            var yOffset = ((center.y - rect.size.y) * zoom + panOffset.y) / gridTex.height;

            var tileOffset = new Vector2(xOffset, yOffset);

            // Amount of tiles
            var tileAmountX = Mathf.Round(rect.size.x * zoom) / gridTex.width;
            var tileAmountY = Mathf.Round(rect.size.y * zoom) / gridTex.height;

            var tileAmount = new Vector2(tileAmountX, tileAmountY);

            // Draw tiled background
            GUI.DrawTextureWithTexCoords(rect, gridTex, new Rect(tileOffset, tileAmount));
            GUI.DrawTextureWithTexCoords(rect, crossTex, new Rect(tileOffset + new Vector2(0.5f, 0.5f), tileAmount));
        }

        public void DrawSelectionBox()
        {
            if (currentActivity == NodeActivity.DragGrid)
            {
                var curPos = WindowToGridPosition(Event.current.mousePosition);
                var size = curPos - dragBoxStart;
                var r = new Rect(dragBoxStart, size);
                r.position = GridToWindowPosition(r.position);
                r.size /= zoom;
                Handles.DrawSolidRectangleWithOutline(r, new Color(0, 0, 0, 0.1f), new Color(1, 1, 1, 0.6f));
            }
        }

        public static bool DropdownButton(string name, float width)
        {
            return GUILayout.Button(name, EditorStyles.toolbarDropDown, GUILayout.Width(width));
        }

        /// <summary> Show right-click context menu for hovered reroute </summary>
        private void ShowRerouteContextMenu(GUIGraphRerouteReference reroute)
        {
            var contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Remove"), false, () => reroute.RemovePoint());
            contextMenu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
            if (GUIGraphPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
        }

        /// <summary> Show right-click context menu for hovered port </summary>
        private void ShowPortContextMenu(GUIGraphNodePort hoveredPort)
        {
            var contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Clear Connections"), false, () => hoveredPort.ClearConnections());
            contextMenu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
            if (GUIGraphPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
        }

        private static Vector2 CalculateBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            var u = 1 - t;
            float tt = t * t, uu = u * u;
            float uuu = uu * u, ttt = tt * t;
            return new Vector2(
                uuu * p0.x + 3 * uu * t * p1.x + 3 * u * tt * p2.x + ttt * p3.x,
                uuu * p0.y + 3 * uu * t * p1.y + 3 * u * tt * p2.y + ttt * p3.y
            );
        }

        /// <summary> Draws a line segment without allocating temporary arrays </summary>
        private static void DrawAAPolyLineNonAlloc(float thickness, Vector2 p0, Vector2 p1)
        {
            polyLineTempArray[0].x = p0.x;
            polyLineTempArray[0].y = p0.y;
            polyLineTempArray[1].x = p1.x;
            polyLineTempArray[1].y = p1.y;
            Handles.DrawAAPolyLine(thickness, polyLineTempArray);
        }

        /// <summary> Draw a bezier from output to input in grid coordinates </summary>
        public void DrawNoodle(Gradient gradient, GUIGraphConnectionPath path, GUIGraphConnectionStroke stroke,
            float thickness,
            List<Vector2> gridPoints)
        {
            // convert grid points to window points
            for (var i = 0; i < gridPoints.Count; ++i)
                gridPoints[i] = GridToWindowPosition(gridPoints[i]);

            var originalHandlesColor = Handles.color;
            Handles.color = gradient.Evaluate(0f);
            var length = gridPoints.Count;
            switch (path)
            {
                case GUIGraphConnectionPath.Curvy:
                    var outputTangent = Vector2.right;
                    for (var i = 0; i < length - 1; i++)
                    {
                        Vector2 inputTangent;
                        // Cached most variables that repeat themselves here to avoid so many indexer calls :p
                        var point_a = gridPoints[i];
                        var point_b = gridPoints[i + 1];
                        var dist_ab = Vector2.Distance(point_a, point_b);
                        if (i == 0) outputTangent = zoom * dist_ab * 0.01f * Vector2.right;
                        if (i < length - 2)
                        {
                            var point_c = gridPoints[i + 2];
                            var ab = (point_b - point_a).normalized;
                            var cb = (point_b - point_c).normalized;
                            var ac = (point_c - point_a).normalized;
                            var p = (ab + cb) * 0.5f;
                            var tangentLength = (dist_ab + Vector2.Distance(point_b, point_c)) * 0.005f * zoom;
                            var side = ac.x * (point_b.y - point_a.y) - ac.y * (point_b.x - point_a.x);

                            p = tangentLength * Mathf.Sign(side) * new Vector2(-p.y, p.x);
                            inputTangent = p;
                        }
                        else
                        {
                            inputTangent = zoom * dist_ab * 0.01f * Vector2.left;
                        }

                        // Calculates the tangents for the bezier's curves.
                        var zoomCoef = 50 / zoom;
                        var tangent_a = point_a + outputTangent * zoomCoef;
                        var tangent_b = point_b + inputTangent * zoomCoef;
                        // Hover effect.
                        var division = Mathf.RoundToInt(.2f * dist_ab) + 3;
                        // Coloring and bezier drawing.
                        var draw = 0;
                        var bezierPrevious = point_a;
                        for (var j = 1; j <= division; ++j)
                        {
                            if (stroke == GUIGraphConnectionStroke.Dashed)
                            {
                                draw++;
                                if (draw >= 2) draw = -2;
                                if (draw < 0) continue;
                                if (draw == 0)
                                    bezierPrevious = CalculateBezierPoint(point_a, tangent_a, tangent_b, point_b,
                                        (j - 1f) / division);
                            }

                            if (i == length - 2)
                                Handles.color = gradient.Evaluate((j + 1f) / division);
                            var bezierNext = CalculateBezierPoint(point_a, tangent_a, tangent_b, point_b,
                                j / (float)division);
                            DrawAAPolyLineNonAlloc(thickness, bezierPrevious, bezierNext);
                            bezierPrevious = bezierNext;
                        }

                        outputTangent = -inputTangent;
                    }

                    break;
                case GUIGraphConnectionPath.Straight:
                    for (var i = 0; i < length - 1; i++)
                    {
                        var point_a = gridPoints[i];
                        var point_b = gridPoints[i + 1];
                        // Draws the line with the coloring.
                        var prev_point = point_a;
                        // Approximately one segment per 5 pixels
                        var segments = (int)Vector2.Distance(point_a, point_b) / 5;
                        segments = Math.Max(segments, 1);

                        var draw = 0;
                        for (var j = 0; j <= segments; j++)
                        {
                            draw++;
                            var t = j / (float)segments;
                            var lerp = Vector2.Lerp(point_a, point_b, t);
                            if (draw > 0)
                            {
                                if (i == length - 2) Handles.color = gradient.Evaluate(t);
                                DrawAAPolyLineNonAlloc(thickness, prev_point, lerp);
                            }

                            prev_point = lerp;
                            if (stroke == GUIGraphConnectionStroke.Dashed && draw >= 2) draw = -2;
                        }
                    }

                    break;
                case GUIGraphConnectionPath.Angled:
                    for (var i = 0; i < length - 1; i++)
                    {
                        if (i == length - 1) continue; // Skip last index
                        if (gridPoints[i].x <= gridPoints[i + 1].x - 50 / zoom)
                        {
                            var midpoint = (gridPoints[i].x + gridPoints[i + 1].x) * 0.5f;
                            var start_1 = gridPoints[i];
                            var end_1 = gridPoints[i + 1];
                            start_1.x = midpoint;
                            end_1.x = midpoint;
                            if (i == length - 2)
                            {
                                DrawAAPolyLineNonAlloc(thickness, gridPoints[i], start_1);
                                Handles.color = gradient.Evaluate(0.5f);
                                DrawAAPolyLineNonAlloc(thickness, start_1, end_1);
                                Handles.color = gradient.Evaluate(1f);
                                DrawAAPolyLineNonAlloc(thickness, end_1, gridPoints[i + 1]);
                            }
                            else
                            {
                                DrawAAPolyLineNonAlloc(thickness, gridPoints[i], start_1);
                                DrawAAPolyLineNonAlloc(thickness, start_1, end_1);
                                DrawAAPolyLineNonAlloc(thickness, end_1, gridPoints[i + 1]);
                            }
                        }
                        else
                        {
                            var midpoint = (gridPoints[i].y + gridPoints[i + 1].y) * 0.5f;
                            var start_1 = gridPoints[i];
                            var end_1 = gridPoints[i + 1];
                            start_1.x += 25 / zoom;
                            end_1.x -= 25 / zoom;
                            var start_2 = start_1;
                            var end_2 = end_1;
                            start_2.y = midpoint;
                            end_2.y = midpoint;
                            if (i == length - 2)
                            {
                                DrawAAPolyLineNonAlloc(thickness, gridPoints[i], start_1);
                                Handles.color = gradient.Evaluate(0.25f);
                                DrawAAPolyLineNonAlloc(thickness, start_1, start_2);
                                Handles.color = gradient.Evaluate(0.5f);
                                DrawAAPolyLineNonAlloc(thickness, start_2, end_2);
                                Handles.color = gradient.Evaluate(0.75f);
                                DrawAAPolyLineNonAlloc(thickness, end_2, end_1);
                                Handles.color = gradient.Evaluate(1f);
                                DrawAAPolyLineNonAlloc(thickness, end_1, gridPoints[i + 1]);
                            }
                            else
                            {
                                DrawAAPolyLineNonAlloc(thickness, gridPoints[i], start_1);
                                DrawAAPolyLineNonAlloc(thickness, start_1, start_2);
                                DrawAAPolyLineNonAlloc(thickness, start_2, end_2);
                                DrawAAPolyLineNonAlloc(thickness, end_2, end_1);
                                DrawAAPolyLineNonAlloc(thickness, end_1, gridPoints[i + 1]);
                            }
                        }
                    }

                    break;
                case GUIGraphConnectionPath.ShaderLab:
                    var start = gridPoints[0];
                    var end = gridPoints[length - 1];
                    //Modify first and last point in array so we can loop trough them nicely.
                    gridPoints[0] = gridPoints[0] + Vector2.right * (20 / zoom);
                    gridPoints[length - 1] = gridPoints[length - 1] + Vector2.left * (20 / zoom);
                    //Draw first vertical lines going out from nodes
                    Handles.color = gradient.Evaluate(0f);
                    DrawAAPolyLineNonAlloc(thickness, start, gridPoints[0]);
                    Handles.color = gradient.Evaluate(1f);
                    DrawAAPolyLineNonAlloc(thickness, end, gridPoints[length - 1]);
                    for (var i = 0; i < length - 1; i++)
                    {
                        var point_a = gridPoints[i];
                        var point_b = gridPoints[i + 1];
                        // Draws the line with the coloring.
                        var prev_point = point_a;
                        // Approximately one segment per 5 pixels
                        var segments = (int)Vector2.Distance(point_a, point_b) / 5;
                        segments = Math.Max(segments, 1);

                        var draw = 0;
                        for (var j = 0; j <= segments; j++)
                        {
                            draw++;
                            var t = j / (float)segments;
                            var lerp = Vector2.Lerp(point_a, point_b, t);
                            if (draw > 0)
                            {
                                if (i == length - 2) Handles.color = gradient.Evaluate(t);
                                DrawAAPolyLineNonAlloc(thickness, prev_point, lerp);
                            }

                            prev_point = lerp;
                            if (stroke == GUIGraphConnectionStroke.Dashed && draw >= 2) draw = -2;
                        }
                    }

                    gridPoints[0] = start;
                    gridPoints[length - 1] = end;
                    break;
            }

            Handles.color = originalHandlesColor;
        }

        /// <summary> Draws all connections </summary>
        public void DrawConnections()
        {
            var mousePos = Event.current.mousePosition;
            var selection = preBoxSelectionReroute != null
                ? new List<GUIGraphRerouteReference>(preBoxSelectionReroute)
                : new List<GUIGraphRerouteReference>();
            hoveredReroute = new GUIGraphRerouteReference();

            var gridPoints = new List<Vector2>(2);

            var col = GUI.color;
            foreach (var node in graph.nodes)
            {
                //If a null node is found, return. This can happen if the nodes associated script is deleted. It is currently not possible in Unity to delete a null asset.
                if (node == null) continue;

                // Draw full connections and output > reroute
                foreach (var output in node.Outputs)
                {
                    //Needs cleanup. Null checks are ugly
                    Rect fromRect;
                    if (!portConnectionPoints.TryGetValue(output, out fromRect)) continue;

                    var portColor = graphEditor.GetPortColor(output);
                    for (var k = 0; k < output.ConnectionCount; k++)
                    {
                        var input = output.GetConnection(k);

                        var noodleGradient = graphEditor.GetNoodleGradient(output, input);
                        var noodleThickness = graphEditor.GetNoodleThickness(output, input);
                        var guiGraphConnectionPath = graphEditor.GetNoodlePath(output, input);
                        var guiGraphConnectionStroke = graphEditor.GetNoodleStroke(output, input);

                        // Error handling
                        if (input == null)
                            continue; //If a script has been updated and the port doesn't exist, it is removed and null is returned. If this happens, return.
                        if (!input.IsConnectedTo(output)) input.Connect(output);
                        Rect toRect;
                        if (!portConnectionPoints.TryGetValue(input, out toRect)) continue;

                        var reroutePoints = output.GetReroutePoints(k);

                        gridPoints.Clear();
                        gridPoints.Add(fromRect.center);
                        gridPoints.AddRange(reroutePoints);
                        gridPoints.Add(toRect.center);
                        DrawNoodle(noodleGradient, guiGraphConnectionPath, guiGraphConnectionStroke, noodleThickness,
                            gridPoints);

                        // Loop through reroute points again and draw the points
                        for (var i = 0; i < reroutePoints.Count; i++)
                        {
                            var rerouteRef = new GUIGraphRerouteReference(output, k, i);
                            // Draw reroute point at position
                            var rect = new Rect(reroutePoints[i], new Vector2(12, 12));
                            rect.position = new Vector2(rect.position.x - 6, rect.position.y - 6);
                            rect = GridToWindowRect(rect);

                            // Draw selected reroute points with an outline
                            if (selectedReroutes.Contains(rerouteRef))
                            {
                                GUI.color = GUIGraphPreferences.GetSettings().highlightColor;
                                GUI.DrawTexture(rect, GUIGraphResources.DotOuter);
                            }

                            GUI.color = portColor;
                            GUI.DrawTexture(rect, GUIGraphResources.Dot);
                            if (rect.Overlaps(selectionBox)) selection.Add(rerouteRef);
                            if (rect.Contains(mousePos)) hoveredReroute = rerouteRef;
                        }
                    }
                }
            }

            GUI.color = col;
            if (Event.current.type != EventType.Layout && currentActivity == NodeActivity.DragGrid)
                selectedReroutes = selection;
        }

        private void DrawNodes()
        {
            var e = Event.current;
            if (e.type == EventType.Layout) selectionCache = new List<Object>(Selection.objects);

            MethodInfo onValidate = null;
            if (Selection.activeObject != null && Selection.activeObject is GUIGraphNode)
            {
                onValidate = Selection.activeObject.GetType().GetMethod("OnValidate");
                if (onValidate != null) EditorGUI.BeginChangeCheck();
            }

            BeginZoomed(position, zoom, topPadding);

            var mousePos = Event.current.mousePosition;

            if (e.type != EventType.Layout)
            {
                hoveredNode = null;
                hoveredPort = null;
            }

            var preSelection = preBoxSelection != null
                ? new List<Object>(preBoxSelection)
                : new List<Object>();

            // Selection box stuff
            var boxStartPos = GridToWindowPositionNoClipped(dragBoxStart);
            var boxSize = mousePos - boxStartPos;
            if (boxSize.x < 0)
            {
                boxStartPos.x += boxSize.x;
                boxSize.x = Mathf.Abs(boxSize.x);
            }

            if (boxSize.y < 0)
            {
                boxStartPos.y += boxSize.y;
                boxSize.y = Mathf.Abs(boxSize.y);
            }

            var selectionBox = new Rect(boxStartPos, boxSize);

            //Save guiColor so we can revert it
            var guiColor = GUI.color;

            var removeEntries = new List<GUIGraphNodePort>();

            if (e.type == EventType.Layout) culledNodes = new List<GUIGraphNode>();
            for (var n = 0; n < graph.nodes.Count; n++)
            {
                // Skip null nodes. The user could be in the process of renaming scripts, so removing them at this point is not advisable.
                if (graph.nodes[n] == null) continue;
                if (n >= graph.nodes.Count) return;
                var node = graph.nodes[n];

                // Culling
                if (e.type == EventType.Layout)
                {
                    // Cull unselected nodes outside view
                    if (!Selection.Contains(node) && ShouldBeCulled(node))
                    {
                        culledNodes.Add(node);
                        continue;
                    }
                }
                else if (culledNodes.Contains(node))
                {
                    continue;
                }

                if (e.type == EventType.Repaint)
                {
                    removeEntries.Clear();
                    foreach (var kvp in portConnectionPoints)
                        if (kvp.Key.node == node)
                            removeEntries.Add(kvp.Key);
                    foreach (var k in removeEntries) portConnectionPoints.Remove(k);
                }

                var nodeEditor = GUIGraphNodeEditor.GetEditor(node, this);

                GUIGraphNodeEditor.portPositions.Clear();

                // Set default label width. This is potentially overridden in OnBodyGUI
                EditorGUIUtility.labelWidth = 84;

                //Get node position
                var nodePos = GridToWindowPositionNoClipped(node.position);

                GUILayout.BeginArea(new Rect(nodePos, new Vector2(nodeEditor.GetWidth(), 4000)));

                var selected = selectionCache.Contains(graph.nodes[n]);

                if (selected)
                {
                    var style = new GUIStyle(nodeEditor.GetBodyStyle());
                    var highlightStyle = new GUIStyle(nodeEditor.GetBodyHighlightStyle());
                    highlightStyle.padding = style.padding;
                    style.padding = new RectOffset();
                    GUI.color = nodeEditor.GetTint();
                    GUILayout.BeginVertical(style);
                    GUI.color = GUIGraphPreferences.GetSettings().highlightColor;
                    GUILayout.BeginVertical(new GUIStyle(highlightStyle));
                }
                else
                {
                    var style = new GUIStyle(nodeEditor.GetBodyStyle());
                    GUI.color = nodeEditor.GetTint();
                    GUILayout.BeginVertical(style);
                }

                GUI.color = guiColor;
                EditorGUI.BeginChangeCheck();

                //Draw node contents
                nodeEditor.OnHeaderGUI();
                nodeEditor.OnBodyGUI();

                //If user changed a value, notify other scripts through onUpdateNode
                if (EditorGUI.EndChangeCheck())
                {
                    if (GUIGraphNodeEditor.onUpdateNode != null) GUIGraphNodeEditor.onUpdateNode(node);
                    EditorUtility.SetDirty(node);
                    nodeEditor.serializedObject.ApplyModifiedProperties();
                }

                GUILayout.EndVertical();

                //Cache data about the node for next frame
                if (e.type == EventType.Repaint)
                {
                    var size = GUILayoutUtility.GetLastRect().size;
                    if (nodeSizes.ContainsKey(node)) nodeSizes[node] = size;
                    else nodeSizes.Add(node, size);

                    foreach (var kvp in GUIGraphNodeEditor.portPositions)
                    {
                        var portHandlePos = kvp.Value;
                        portHandlePos += node.position;
                        var rect = new Rect(portHandlePos.x - 8, portHandlePos.y - 8, 16, 16);
                        portConnectionPoints[kvp.Key] = rect;
                    }
                }

                if (selected) GUILayout.EndVertical();

                if (e.type != EventType.Layout)
                {
                    //Check if we are hovering this node
                    var nodeSize = GUILayoutUtility.GetLastRect().size;
                    var windowRect = new Rect(nodePos, nodeSize);
                    if (windowRect.Contains(mousePos)) hoveredNode = node;

                    //If dragging a selection box, add nodes inside to selection
                    if (currentActivity == NodeActivity.DragGrid)
                        if (windowRect.Overlaps(selectionBox))
                            preSelection.Add(node);

                    //Check if we are hovering any of this nodes ports
                    //Check input ports
                    foreach (var input in node.Inputs)
                    {
                        //Check if port rect is available
                        if (!portConnectionPoints.ContainsKey(input)) continue;
                        var r = GridToWindowRectNoClipped(portConnectionPoints[input]);
                        if (r.Contains(mousePos)) hoveredPort = input;
                    }

                    //Check all output ports
                    foreach (var output in node.Outputs)
                    {
                        //Check if port rect is available
                        if (!portConnectionPoints.ContainsKey(output)) continue;
                        var r = GridToWindowRectNoClipped(portConnectionPoints[output]);
                        if (r.Contains(mousePos)) hoveredPort = output;
                    }
                }

                GUILayout.EndArea();
            }

            if (e.type != EventType.Layout && currentActivity == NodeActivity.DragGrid)
                Selection.objects = preSelection.ToArray();
            EndZoomed(position, zoom, topPadding);

            //If a change in is detected in the selected node, call OnValidate method. 
            //This is done through reflection because OnValidate is only relevant in editor, 
            //and thus, the code should not be included in build.
            if (onValidate != null && EditorGUI.EndChangeCheck()) onValidate.Invoke(Selection.activeObject, null);
        }

        private bool ShouldBeCulled(GUIGraphNode node)
        {
            var nodePos = GridToWindowPositionNoClipped(node.position);
            if (nodePos.x / _zoom > position.width) return true; // Right

            if (nodePos.y / _zoom > position.height) return true; // Bottom

            if (nodeSizes.ContainsKey(node))
            {
                var size = nodeSizes[node];
                if (nodePos.x + size.x < 0) return true; // Left
                if (nodePos.y + size.y < 0) return true; // Top
            }

            return false;
        }

        private void DrawTooltip()
        {
            if (hoveredPort != null && GUIGraphPreferences.GetSettings().portTooltips &&
                graphEditor != null)
            {
                var tooltip = graphEditor.GetPortTooltip(hoveredPort);
                if (string.IsNullOrEmpty(tooltip)) return;
                var content = new GUIContent(tooltip);
                var size = GUIGraphResources.styles.Tooltip.CalcSize(content);
                size.x += 8;
                var rect = new Rect(Event.current.mousePosition - size, size);
                EditorGUI.LabelField(rect, content, GUIGraphResources.styles.Tooltip);
                Repaint();
            }
        }
    }
}
#endif