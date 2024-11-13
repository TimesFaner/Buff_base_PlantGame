using System.Collections.Generic;
using Sirenix.OdinInspector;
using Timesfaner_work.BehaviorTree.BTNodeBase;
using Timesfaner_work.BehaviorTree.View;
using UnityEngine.UIElements;

namespace Timesfaner_work.BehaviorTree.Tree
{
    // [CreateAssetMenu]
    public class InspectorViewData: SerializedScriptableObject

    {
        // 数据显示处
        public IMGUIContainer inspectorBar;
        //
        public HashSet<BtNodeBase> DataView = new HashSet<BtNodeBase>();
        // public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits>{}
        
    }
}