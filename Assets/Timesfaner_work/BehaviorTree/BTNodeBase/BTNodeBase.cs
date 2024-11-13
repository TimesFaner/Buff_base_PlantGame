using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Timesfaner_work.BehaviorTree.BTNodeBase
{
    public enum BehaviourState
    {
        未执行,成功,失败,执行中
    }
    
    public enum NodeType
    {
        无,根节点,组合节点,条件节点,行为节点
    }

    #region 根数据
    [BoxGroup]
    [HideLabel]
    [HideReferenceObjectPicker]
    public abstract class BtNodeBase
    {
        [FoldoutGroup("基础数据"),LabelText("标识")]
        public string Guid;
        [FoldoutGroup("基础数据"),LabelText("位置")]
        public Vector2 Position;
        [FoldoutGroup("基础数据"),LabelText("名称")]
        public string NodeName;
        [FoldoutGroup("基础数据"),LabelText("类型")]
        public NodeType NodeType;
        [FoldoutGroup("基础数据"),LabelText("状态")]
        public BehaviourState NodeState;

        public abstract BehaviourState Tick();


        protected void ChangeFailState()
        {
            NodeState = BehaviourState.失败;
            switch (this)
            {
                case BtComposite composite:
                    composite.ChildNodes.ForEach(node=> node.ChangeFailState());
                    break;
                case BtPrecondition precondition:
                    precondition.ChildNode?.ChangeFailState();
                    break;
            }
        }

    }

    public abstract class BtComposite : BtNodeBase
    {
        [FoldoutGroup("@NodeName"),LabelText("子节点")]
        public List<BtNodeBase> ChildNodes = new List<BtNodeBase>();
    }

    public abstract class BtPrecondition : BtNodeBase
    {
        [FoldoutGroup("@NodeName"),LabelText("子节点")]
        public BtNodeBase ChildNode;
    }

    public abstract class BtActionNode : BtNodeBase { }
    #endregion
    
}