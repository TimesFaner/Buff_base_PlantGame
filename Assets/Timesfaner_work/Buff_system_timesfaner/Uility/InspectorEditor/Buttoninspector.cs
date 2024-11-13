using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Buff_system_timesfaner.Uility
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class MonoButtoninspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var script = (MonoBehaviour)target;

            var methons = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (var methon in methons)
            {
                var attributes = methon.GetCustomAttributes(typeof(ButtonAttribute), true);
                if (attributes.Length > 0)
                    if (GUILayout.Button(methon.Name))
                        methon.Invoke(script, null);
            }
        }
    }

    [CustomEditor(typeof(ScriptableObject), true)]
    public class SOButtoninspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var script = (ScriptableObject)target;

            var methons = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (var methon in methons)
            {
                var attributes = methon.GetCustomAttributes(typeof(ButtonAttribute), true);
                if (attributes.Length > 0)
                    if (GUILayout.Button(methon.Name))
                        methon.Invoke(script, null);
            }
        }
    }
}