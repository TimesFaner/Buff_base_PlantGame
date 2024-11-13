/****************************************************************************
 * Copyright (c) 2019 Gwaredd Mountain UNDER MIT License
 * Copyright (c) 2022 liangxiegame UNDER MIT License
 *
 * https://github.com/gwaredd/UnityMarkdownViewer
 * http://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using UnityEngine;

namespace QFramework
{
    public class MDStyleConverter
    {
        private const int Variable = 0;
        private const int FixedInline = 7;
        private const int FixedBlock = 8;

        private static readonly string[] CustomStyles =
        {
            "variable",
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6",
            "fixed_inline",
            "fixed_block"
        };

        private readonly Color linkColor = new(0.41f, 0.71f, 1.0f, 1.0f);
        private readonly GUIStyle[] mReference;
        private readonly GUIStyle[] mWorking;
        private MDStyle mCurrentStyle = MDStyle.Default;

        public MDStyleConverter(GUISkin skin)
        {
            mReference = new GUIStyle[CustomStyles.Length];
            mWorking = new GUIStyle[CustomStyles.Length];

            for (var i = 0; i < CustomStyles.Length; i++)
            {
                mReference[i] = skin.GetStyle(CustomStyles[i]);
                mWorking[i] = new GUIStyle(mReference[i]);
            }
        }


        //------------------------------------------------------------------------------

        public GUIStyle Apply(MDStyle src)
        {
            if (src.Block) return mWorking[FixedBlock];

            var style = mWorking[src.Size];

            if (mCurrentStyle != src)
            {
                var reference = mReference[src.Fixed ? FixedInline : Variable];

                style.font = reference.font;
                style.fontStyle = src.GetFontStyle();
                style.normal.textColor = src.Link ? linkColor : reference.normal.textColor;
                mCurrentStyle = src;
            }

            style.richText = true;
            return style;
        }
    }
}