using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Timesfaner_work.BehaviorTree.BTNodeBase
{
    public class Sequence : BtComposite
    {
        [LabelText("执行index")] [FoldoutGroup("@NodeName")]
        public int currnetNode;

        public override BehaviourState Tick()
        {
            if (ChildNodes.Count == 0)
            {
                NodeState = BehaviourState.失败;
                return BehaviourState.失败;
            }


            var state = ChildNodes[currnetNode].Tick();
            switch (state)
            {
                case BehaviourState.成功:
                    currnetNode++;
                    if (currnetNode >= ChildNodes.Count) currnetNode = 0;

                    NodeState = BehaviourState.成功;
                    return BehaviourState.成功;
                default:
                    NodeState = state;
                    return state;
            }
        }
    }

    public class Selector : BtComposite
    {
        [LabelText("选择的index")] [FoldoutGroup("@NodeName")]
        public int selectIndex;

        public override BehaviourState Tick()
        {
            if (ChildNodes.Count == 0)
            {
                ChangeFailState();
                return NodeState;
            }

            var selectState = ChildNodes[selectIndex].Tick();

            switch (selectState)
            {
                case BehaviourState.失败:
                    ChangeFailState();
                    break;
                default:
                    selectIndex = 0;
                    NodeState = selectState;
                    return selectState;
            }

            for (var i = 0; i < ChildNodes.Count; i++)
            {
                var state = ChildNodes[i].Tick();
                if (state == BehaviourState.失败 || selectIndex == i) continue;
                selectIndex = i;
                NodeState = state;
                return state;
            }

            ChangeFailState();
            return BehaviourState.失败;
        }
    }

    public class Parallel : BtComposite
    {
        public override BehaviourState Tick()
        {
            var starts = new List<BehaviourState>();
            for (var i = 0; i < ChildNodes.Count; i++)
            {
                var start = ChildNodes[i].Tick();
                switch (start)
                {
                    case BehaviourState.失败:
                        ChangeFailState();
                        return NodeState;
                    default:
                        starts.Add(start);
                        break;
                }
            }

            for (var i = 0; i < starts.Count; i++)
                if (starts[i] == BehaviourState.执行中)
                {
                    NodeState = BehaviourState.执行中;
                    return BehaviourState.执行中;
                }

            NodeState = BehaviourState.成功;
            return BehaviourState.成功;
        }
    }

    public class Repeat : BtPrecondition
    {
        [LabelText("循环次数")] [FoldoutGroup("@NodeName")]
        public int LoopNumber;

        [LabelText("循环停止数")] [FoldoutGroup("@NodeName")]
        public int LoopStop;

        public override BehaviourState Tick()
        {
            var start = ChildNode.Tick();
            if (LoopStop <= LoopNumber)
            {
                LoopNumber = 0;
                NodeState = BehaviourState.成功;
                return BehaviourState.成功;
            }

            LoopNumber++;

            if (start == BehaviourState.失败)
                ChangeFailState();
            else
                NodeState = BehaviourState.执行中;

            return NodeState;
        }
    }

    public class So : BtPrecondition
    {
        [LabelText("执行条件")] [FoldoutGroup("@NodeName")]
        public Func<bool> Condition;

        public override BehaviourState Tick()
        {
            if (Condition == null)
            {
                ChangeFailState();
                return BehaviourState.失败;
            }

            if (Condition.Invoke())
            {
                NodeState = ChildNode.Tick();
                return NodeState;
            }

            ChangeFailState();
            return BehaviourState.失败;
        }
    }

    public class Not : BtPrecondition
    {
        [LabelText("执行条件")] [FoldoutGroup("@NodeName")]
        public Func<bool> Condition;

        public override BehaviourState Tick()
        {
            if (Condition == null)
            {
                ChangeFailState();
                return BehaviourState.失败;
            }

            if (!Condition.Invoke())
            {
                NodeState = ChildNode.Tick();
                return NodeState;
            }

            ChangeFailState();
            return BehaviourState.失败;
        }
    }

    public class Run : BtActionNode
    {
        public override BehaviourState Tick()
        {
            Running();
            NodeState = BehaviourState.成功;
            return BehaviourState.成功;
        }

        public void Running()
        {
            Debug.Log(NodeName + " 节点执行了！");
        }
    }

    public class Running : BtActionNode
    {
        [LabelText("执行进度")] [FoldoutGroup("@NodeName")]
        public float Schedule;

        public override BehaviourState Tick()
        {
            if (Schedule >= 0.6f)
            {
                Schedule = 0;
                Debug.Log(NodeName + " 节点任务完成");
                NodeState = BehaviourState.成功;
                return BehaviourState.成功;
            }

            Schedule += Time.deltaTime;
            //Debug.Log(NodeName+ " 节点进度： "+Schedule*20+"%");
            NodeState = BehaviourState.执行中;
            return BehaviourState.执行中;
        }
    }
}