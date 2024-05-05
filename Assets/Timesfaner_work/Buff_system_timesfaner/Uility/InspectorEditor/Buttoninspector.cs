using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine.Windows;

namespace Buff_system_timesfaner.Uility
{
    [CustomEditor(typeof(MonoBehaviour),true)]
    public class MonoButtoninspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MonoBehaviour script = (MonoBehaviour)target;
            
            var methons = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public );
            foreach (var methon in methons)
            {
                var attributes = methon.GetCustomAttributes(typeof(ButtonAttribute),true);
                if (attributes.Length > 0)
                {
                    if (GUILayout.Button(methon.Name))
                    {
                        methon.Invoke(script, null);
                    }
                }
            }
        }
    }
    [CustomEditor(typeof(ScriptableObject),true)]
    public class SOButtoninspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            ScriptableObject script = (ScriptableObject)target;
            
            var methons = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public );
            foreach (var methon in methons)
            {
                var attributes = methon.GetCustomAttributes(typeof(ButtonAttribute),true);
                if (attributes.Length > 0)
                {
                    if (GUILayout.Button(methon.Name))
                    {
                        methon.Invoke(script, null);
                    }
                }
            }
        }
    }
    
    
}

