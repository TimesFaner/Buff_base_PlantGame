using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace Buff_system_timesfaner.Uility
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class Cloneinspector :Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var so = (MonoBehaviour)target;
            var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                var attributes = field.GetCustomAttributes(typeof(Cloneinspector), true);

                if (attributes.Length > 0)
                {
                }
            }
        }
    }
}