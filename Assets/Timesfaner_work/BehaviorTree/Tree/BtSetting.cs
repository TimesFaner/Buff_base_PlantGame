using Sirenix.OdinInspector;
using Timesfaner_work.BehaviorTree.BTNodeBase;
using Timesfaner_work.BehaviorTree.Test;
using Unity.VisualScripting;
using UnityEngine;
namespace Timesfaner_work.BehaviorTree.Tree
{
    [CreateAssetMenu]
    public class BtSetting : SerializedScriptableObject
    {
        public int TreeID;

        public static BtSetting GetSetting() 
        {
            return 
                Resources.Load<BtSetting>("BtSetting" );
        }
#if UNITY_EDITOR
        public IgetBt GetTree()
        {
            return   UnityEditor.EditorUtility.InstanceIDToObject(TreeID) as IgetBt;
        }

        public void SetRoot(BtNodeBase rootNode)
        {
            GetTree().SetRoot(rootNode);
        }
#endif

    }


}