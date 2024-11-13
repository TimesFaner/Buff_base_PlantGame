using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Timesfaner_work.BehaviorTree.BTNodeBase;
using Timesfaner_work.BehaviorTree.Tree;

namespace Timesfaner_work.BehaviorTree.Test
{
    public class TestBt : SerializedMonoBehaviour, IgetBt
    {
        [OdinSerialize]
        public BtNodeBase rootNode;

        private void Update()
        {
            rootNode.Tick();
        }

#if UNITY_EDITOR

        [Button] public void OpenView()
        {
            BtSetting
                .GetSetting().TreeID = GetInstanceID();
            UnityEditor.EditorApplication.ExecuteMenuItem("Tools/BTWindow");
        }

#endif
        public BtNodeBase GetRoot()
        {
            return rootNode;
        }

        public void SetRoot(BtNodeBase _node) => rootNode = _node ;
    }

    public interface IgetBt
    {
        BtNodeBase GetRoot();
        void SetRoot(BtNodeBase node); // 设置根节点
    }
}