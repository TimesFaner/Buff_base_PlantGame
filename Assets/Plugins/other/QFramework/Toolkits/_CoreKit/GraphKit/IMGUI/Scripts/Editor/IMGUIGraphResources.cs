/****************************************************************************
 * Copyright (c) 2017 Thor Brigsted UNDER MIT LICENSE  see licenses.txt
 * Copyright (c) 2022 liangxiegame UNDER Paid MIT LICENSE  see licenses.txt
 *
 * xNode: https://github.com/Siccity/xNode
 ****************************************************************************/

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace QFramework
{
    public static class GUIGraphResources
    {
        private static Texture2D mDot;

        private static Texture2D mDotOuter;

        private static Texture2D mNodeBody;

        private static Texture2D mNodeHighlight;

        private static Styles mStyles;

        // Textures
        public static Texture2D Dot => mDot != null ? mDot : mDot = Resources.Load<Texture2D>("graphkit_imgui_dot");

        public static Texture2D DotOuter => mDotOuter != null
            ? mDotOuter
            : mDotOuter = Resources.Load<Texture2D>("graphkit_imgui_dot_outer");

        public static Texture2D NodeBody =>
            mNodeBody != null ? mNodeBody : mNodeBody = Resources.Load<Texture2D>("graphkit_imgui_node");

        public static Texture2D NodeHighlight =>
            mNodeHighlight != null
                ? mNodeHighlight
                : mNodeHighlight = Resources.Load<Texture2D>("graphkit_imgui_node_highlight");

        // Styles
        public static Styles styles => mStyles ?? (mStyles = new Styles());

        public static GUIStyle OutputPort => new(EditorStyles.label) { alignment = TextAnchor.UpperRight };

        public static Texture2D GenerateGridTexture(Color line, Color bg)
        {
            var tex = new Texture2D(64, 64);
            var cols = new Color[64 * 64];
            for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++)
            {
                var col = bg;
                if (y % 16 == 0 || x % 16 == 0) col = Color.Lerp(line, bg, 0.65f);
                if (y == 63 || x == 63) col = Color.Lerp(line, bg, 0.35f);
                cols[y * 64 + x] = col;
            }

            tex.SetPixels(cols);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.name = "Grid";
            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateCrossTexture(Color line)
        {
            var tex = new Texture2D(64, 64);
            var cols = new Color[64 * 64];
            for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++)
            {
                var col = line;
                if (y != 31 && x != 31) col.a = 0;
                cols[y * 64 + x] = col;
            }

            tex.SetPixels(cols);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.name = "Grid";
            tex.Apply();
            return tex;
        }

        public class Styles
        {
            public readonly GUIStyle InputPort;
            public readonly GUIStyle nodeBody;
            public readonly GUIStyle NodeHeader;
            public readonly GUIStyle NodeHighlight;
            public readonly GUIStyle nodeLabel;
            public readonly GUIStyle Tooltip;

            public Styles()
            {
                var baseStyle = new GUIStyle("Label")
                {
                    fixedHeight = 18,
                    normal =
                    {
                        textColor = Color.white
                    },
                    fontStyle = FontStyle.Bold
                };

                InputPort = new GUIStyle(baseStyle)
                {
                    alignment = TextAnchor.UpperLeft,
                    padding =
                    {
                        left = 10
                    },
                    normal =
                    {
                        textColor = Color.white
                    }
                };

                NodeHeader = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal =
                    {
                        textColor = Color.white
                    }
                };


                nodeLabel = new GUIStyle
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal =
                    {
                        textColor = Color.white
                    }
                };

                nodeBody = new GUIStyle
                {
                    normal =
                    {
                        background = NodeBody,
                        textColor = Color.white
                    },
                    border = new RectOffset(32, 32, 32, 32),
                    padding = new RectOffset(16, 16, 4, 16)
                };

                NodeHighlight = new GUIStyle
                {
                    normal =
                    {
                        background = GUIGraphResources.NodeHighlight
                    },
                    border = new RectOffset(32, 32, 32, 32)
                };

                Tooltip = new GUIStyle("helpBox")
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }
    }
}
#endif