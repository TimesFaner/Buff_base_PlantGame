/****************************************************************************
 * Copyright (c) 2021 ~ 2022 liangxiegame UNDER MIT License
 *
 * https://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using UnityEngine;

namespace QFramework
{
    public static class PlatformCheck
    {
        public static bool IsAndroid
        {
            get
            {
                var retValue = false;
#if UNITY_ANDROID
                retValue = true;
#endif
                return retValue;
            }
        }

        public static bool IsEditor
        {
            get
            {
                var retValue = false;
#if UNITY_EDITOR
                retValue = true;
#endif
                return retValue;
            }
        }

        public static bool IsiOS
        {
            get
            {
                var retValue = false;
#if UNITY_IOS
				retValue = true;
#endif
                return retValue;
            }
        }

        public static bool IsStandardAlone
        {
            get
            {
                var retValue = false;
#if UNITY_STANDALONE
                retValue = true;
#endif
                return retValue;
            }
        }

        public static bool IsWin
        {
            get
            {
                var retValue = false;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                retValue = true;
#endif
                return retValue;
            }
        }

        public static bool IsWebGL => Application.platform == RuntimePlatform.WebGLPlayer;
    }
}