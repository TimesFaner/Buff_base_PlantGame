/****************************************************************************
 * Copyright (c) 2016 ~ 2022 liangxiegame UNDER MIT LICENSE
 *
 * https://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace QFramework
{
    internal static class User
    {
        public static BindableProperty<string> Username = new(LoadString("username"));
        public static BindableProperty<string> Password = new(LoadString("password"));
        public static BindableProperty<string> Token = new(LoadString("token"));

        public static bool Logined =>
            !string.IsNullOrEmpty(Token.Value) &&
            !string.IsNullOrEmpty(Username.Value) &&
            !string.IsNullOrEmpty(Password.Value);


        public static void Save()
        {
            Username.SaveString("username");
            Password.SaveString("password");
            Token.SaveString("token");
        }

        public static void Clear()
        {
            Username.Value = string.Empty;
            Password.Value = string.Empty;
            Token.Value = string.Empty;
            Save();
        }

        public static void SaveString(this BindableProperty<string> selfProperty, string key)
        {
            EditorPrefs.SetString(key, selfProperty.Value);
        }


        public static string LoadString(string key)
        {
            return EditorPrefs.GetString(key, string.Empty);
        }
    }


    public class ReadmeWindow : EditorWindow
    {
        private PackageVersion mPackageVersion;
        private Readme mReadme;

        private Vector2 mScrollPos = Vector2.zero;

        public void OnGUI()
        {
            mScrollPos = GUILayout.BeginScrollView(mScrollPos, true, true, GUILayout.Width(580), GUILayout.Height(300));

            GUILayout.Label("类型:" + mPackageVersion.Type);

            mReadme.items.ForEach(item =>
            {
                EasyIMGUI
                    .Custom()
                    .OnGUI(() =>
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);
                        GUILayout.BeginVertical();
                        GUILayout.BeginHorizontal();

                        GUILayout.Label("version: " + item.version, GUILayout.Width(130));
                        GUILayout.Label("author: " + item.author);
                        GUILayout.Label("date: " + item.date);

                        if (item.author == User.Username.Value || User.Username.Value == "liangxie")
                            if (GUILayout.Button("删除"))
                                //                            RenderEndCommandExecuter.PushCommand(() =>
                                //                            {
                                new PackageManagerServer().DeletePackage(item.PackageId,
                                    () => { mReadme.items.Remove(item); });
                        //                            });
                        GUILayout.EndHorizontal();
                        GUILayout.Label(item.content);
                        GUILayout.EndVertical();

                        GUILayout.EndHorizontal();
                    }).DrawGUI();
            });

            GUILayout.EndScrollView();
        }


        public static void Init(Readme readme, PackageVersion packageVersion)
        {
            var readmeWin = (ReadmeWindow)GetWindow(typeof(ReadmeWindow), true, packageVersion.Name, true);
            readmeWin.mReadme = readme;
            readmeWin.mPackageVersion = packageVersion;
            readmeWin.position = new Rect(Screen.width / 2, Screen.height / 2, 600, 300);
            readmeWin.Show();
        }
    }

    [Serializable]
    public class PackageInfosRequestCache
    {
        public List<PackageRepository> PackageRepositories = new();

        private static string mFilePath
        {
            get
            {
                var dirPath = Application.dataPath + "/.qframework/PackageManager/";

                if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

                return dirPath + "PackageInfosRequestCache.json";
            }
        }

        public static PackageInfosRequestCache Get()
        {
            if (File.Exists(mFilePath))
            {
                var cacheJson = File.ReadAllText(mFilePath);

                if (cacheJson.IsTrimNotNullAndEmpty()) return new PackageInfosRequestCache();
                try
                {
                    var retValue = JsonUtility.FromJson<PackageInfosRequestCache>(cacheJson);

                    if (retValue.PackageRepositories == null) return new PackageInfosRequestCache();
                }
                catch (Exception)
                {
                    return new PackageInfosRequestCache();
                }
            }

            return new PackageInfosRequestCache();
        }

        public void Save()
        {
            File.WriteAllText(mFilePath, JsonUtility.ToJson(this));
        }
    }


    public static class FrameworkMenuItems
    {
        public const string Preferences = "QFramework/Preferences... %e";
        public const string PackageKit = "QFramework/PackageKit... %#e";

        public const string Feedback = "QFramework/Feedback";
    }

    public static class FrameworkMenuItemsPriorities
    {
        public const int Preferences = 1;

        public const int Feedback = 11;
    }


    public class SubWindow : EditorWindow, IMGUILayout
    {
        private List<IMGUIView> mChildren { get; } = new();

        private void OnGUI()
        {
            mChildren.ForEach(view => view.DrawGUI());
        }

        public string Id { get; set; }
        public bool Visible { get; set; }

        public Func<bool> VisibleCondition { get; set; }

        void IMGUIView.DrawGUI()
        {
        }

        IMGUILayout IMGUIView.Parent { get; set; }

        public FluentGUIStyle Style { get; set; } = new(() => new GUIStyle());

        Color IMGUIView.BackgroundColor { get; set; }

        void IMGUIView.RefreshNextFrame()
        {
        }

        void IMGUIView.AddLayoutOption(GUILayoutOption option)
        {
        }

        void IMGUIView.RemoveFromParent()
        {
        }

        void IMGUIView.Refresh()
        {
        }

        public void Hide()
        {
            throw new NotImplementedException();
        }

        public IMGUILayout AddChild(IMGUIView view)
        {
            mChildren.Add(view);
            view.Parent = this;
            return this;
        }

        public void RemoveChild(IMGUIView view)
        {
            mChildren.Add(view);
            view.Parent = null;
        }

        public void Clear()
        {
            mChildren.Clear();
        }

        public void Dispose()
        {
        }
    }

    public abstract class Window : EditorWindow, IDisposable
    {
        protected bool mShowing;
        public static Window MainWindow { get; protected set; }

        public IMGUIViewController ViewController { get; set; }

        private void OnGUI()
        {
            if (ViewController != null) ViewController.View.DrawGUI();

            RenderEndCommandExecutor.ExecuteCommand();
        }

        public void Dispose()
        {
            OnDispose();
        }

        public T CreateViewController<T>() where T : IMGUIViewController, new()
        {
            var t = new T();
            t.SetUpView();
            return t;
        }

        public static void Open<T>(string title) where T : Window
        {
            MainWindow = GetWindow<T>(true);

            if (!MainWindow.mShowing)
            {
                MainWindow.position = new Rect(Screen.width / 2, Screen.height / 2, 800, 600);
                MainWindow.titleContent = new GUIContent(title);
                MainWindow.Init();
                MainWindow.mShowing = true;
                MainWindow.Show();
            }
            else
            {
                MainWindow.mShowing = false;
                MainWindow.Dispose();
                MainWindow.Close();
                MainWindow = null;
            }
        }

        public static SubWindow CreateSubWindow(string name = "SubWindow")
        {
            var window = GetWindow<SubWindow>(true, name);
            window.Clear();
            return window;
        }

        private void Init()
        {
            OnInit();
        }


        public void PushCommand(Action command)
        {
            RenderEndCommandExecutor.PushCommand(command);
        }


        protected abstract void OnInit();
        protected abstract void OnDispose();
    }


    public static class WindowExtension
    {
        public static T PushCommand<T>(this T view, Action command) where T : IMGUIView
        {
            RenderEndCommandExecutor.PushCommand(command);
            return view;
        }
    }

    public static class SubWindowExtension
    {
        public static T Postion<T>(this T subWindow, int x, int y) where T : SubWindow
        {
            var rect = subWindow.position;
            rect.x = x;
            rect.y = y;
            subWindow.position = rect;

            return subWindow;
        }

        public static T Size<T>(this T subWindow, int width, int height) where T : SubWindow
        {
            var rect = subWindow.position;
            rect.width = width;
            rect.height = height;
            subWindow.position = rect;

            return subWindow;
        }

        public static T PostionScreenCenter<T>(this T subWindow) where T : SubWindow
        {
            var rect = subWindow.position;
            rect.x = Screen.width / 2;
            rect.y = Screen.height / 2;
            subWindow.position = rect;

            return subWindow;
        }
    }

    public static class EditorUtils
    {
        public static string CurrentSelectPath => Selection.activeObject == null
            ? null
            : AssetDatabase.GetAssetPath(Selection.activeObject);

        public static string GetSelectedPathOrFallback()
        {
            var path = string.Empty;

            foreach (var obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);

                if (!string.IsNullOrEmpty(path) && File.Exists(path)) return path;
            }

            return path;
        }

        public static void MarkCurrentSceneDirty()
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public static string AssetsPath2ABSPath(string assetsPath)
        {
            var assetRootPath = Path.GetFullPath(Application.dataPath);
            return assetRootPath.Substring(0, assetRootPath.Length - 6) + assetsPath;
        }

        public static string ABSPath2AssetsPath(string absPath)
        {
            var assetRootPath = Path.GetFullPath(Application.dataPath);
            Debug.Log(assetRootPath);
            Debug.Log(Path.GetFullPath(absPath));
            return "Assets" + Path.GetFullPath(absPath).Substring(assetRootPath.Length).Replace("\\", "/");
        }


        public static string AssetPath2ReltivePath(string path)
        {
            if (path == null) return null;

            return path.Replace("Assets/", "");
        }

        public static bool ExcuteCmd(string toolName, string args, bool isThrowExcpetion = true)
        {
            var process = new Process();
            process.StartInfo.FileName = toolName;
            process.StartInfo.Arguments = args;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            OuputProcessLog(process, isThrowExcpetion);
            return true;
        }

        public static void OuputProcessLog(Process p, bool isThrowExcpetion)
        {
            var standardError = string.Empty;
            p.BeginErrorReadLine();

            p.ErrorDataReceived += (sender, outLine) => { standardError += outLine.Data; };

            var standardOutput = string.Empty;
            p.BeginOutputReadLine();
            p.OutputDataReceived += (sender, outLine) => { standardOutput += outLine.Data; };

            p.WaitForExit();
            p.Close();

            Debug.Log(standardOutput);
            if (standardError.Length > 0)
            {
                if (isThrowExcpetion)
                {
                    Debug.Log(standardError);
                    throw new Exception(standardError);
                }

                Debug.Log(standardError);
            }
        }

        public static Dictionary<string, string> ParseArgs(string argString)
        {
            var curPos = argString.IndexOf('-');
            var result = new Dictionary<string, string>();

            while (curPos != -1 && curPos < argString.Length)
            {
                var nextPos = argString.IndexOf('-', curPos + 1);
                var item = string.Empty;

                if (nextPos != -1)
                    item = argString.Substring(curPos + 1, nextPos - curPos - 1);
                else
                    item = argString.Substring(curPos + 1, argString.Length - curPos - 1);

                item = StringTrim(item);
                var splitPos = item.IndexOf(' ');

                if (splitPos == -1)
                {
                    var key = StringTrim(item);
                    result[key] = "";
                }
                else
                {
                    var key = StringTrim(item.Substring(0, splitPos));
                    var value = StringTrim(item.Substring(splitPos + 1, item.Length - splitPos - 1));
                    result[key] = value;
                }

                curPos = nextPos;
            }

            return result;
        }

        public static string GetFileMD5Value(string absPath)
        {
            if (!File.Exists(absPath))
                return "";

            var md5CSP = new MD5CryptoServiceProvider();
            var file = new FileStream(absPath, FileMode.Open);
            var retVal = md5CSP.ComputeHash(file);
            file.Close();
            var result = "";

            for (var i = 0; i < retVal.Length; i++) result += retVal[i].ToString("x2");

            return result;
        }


        public static string StringTrim(string str, params char[] trimer)
        {
            var startIndex = 0;
            var endIndex = str.Length;

            for (var i = 0; i < str.Length; ++i)
                if (!IsInCharArray(trimer, str[i]))
                {
                    startIndex = i;
                    break;
                }

            for (var i = str.Length - 1; i >= 0; --i)
                if (!IsInCharArray(trimer, str[i]))
                {
                    endIndex = i;
                    break;
                }

            if (startIndex == 0 && endIndex == str.Length) return string.Empty;

            return str.Substring(startIndex, endIndex - startIndex + 1);
        }

        public static string StringTrim(string str)
        {
            return StringTrim(str, ' ', '\t');
        }

        private static bool IsInCharArray(char[] array, char c)
        {
            for (var i = 0; i < array.Length; ++i)
                if (array[i] == c)
                    return true;

            return false;
        }
    }

    public static class MouseSelector
    {
        public static string GetSelectedPathOrFallback()
        {
            var path = string.Empty;

            foreach (var obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                }
            }

            return path;
        }
    }


    internal class ColorView : IMGUIAbstractView
    {
        public ColorView(Color color)
        {
            Color = new BindableProperty<Color>(color);
        }

        public BindableProperty<Color> Color { get; }

        protected override void OnGUI()
        {
            Color.Value = EditorGUILayout.ColorField(Color.Value, LayoutStyles);
        }
    }


    internal class EnumPopupView : IMGUIAbstractView
    {
        public EnumPopupView(Enum initValue)
        {
            ValueProperty = new BindableProperty<Enum>(initValue);
            ValueProperty.Value = initValue;
            Style = new FluentGUIStyle(() => EditorStyles.popup);
        }

        public BindableProperty<Enum> ValueProperty { get; set; }

        protected override void OnGUI()
        {
            var enumType = ValueProperty.Value;
            ValueProperty.Value = EditorGUILayout.EnumPopup(enumType, Style.Value, LayoutStyles);
        }
    }


    public abstract class IMGUIViewController
    {
        public VerticalLayout View = new();

        public abstract void SetUpView();
    }
}
#endif