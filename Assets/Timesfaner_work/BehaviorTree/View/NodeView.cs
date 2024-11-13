using UnityEditor.Experimental.GraphView;
using Timesfaner_work.BehaviorTree.BTNodeBase;
using Timesfaner_work.BehaviorTree.Tree;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Timesfaner_work.BehaviorTree.View
{
    public class NodeView : Node
    {
        public BtNodeBase NodeData;
        public Port InputPort;
        public Port OutPort;

        public NodeView(BtNodeBase NodeData)
        {
            this.NodeData = NodeData;
            InitNode(this.NodeData);
        }

        public void InitNode(BtNodeBase NodeData)
        {
            title = NodeData.NodeName;
            //端口
            InputPort = NodePortCreate(this);

            inputContainer.Add(InputPort);
            switch (NodeData)
            {
                case BtComposite composite:
                    OutPort = NodeOutPortCreate(this, false);
                    outputContainer.Add(OutPort);
                    break;
                case BtPrecondition precondition:
                    OutPort = NodeOutPortCreate(this, true);
                    outputContainer.Add(OutPort);
                    break;
                case BtActionNode actionNode:
                    break;
            }
        }

        /// <summary>
        /// 保存每次页面的节点位置
        /// </summary>
        /// <param name="newPos"></param>
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            NodeData.Position = new Vector2(newPos.xMin, newPos.yMin);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            BTWindow.windowRoot.InspectorView.UpdateInspector();
        }
        /// <summary>
        /// 连接节点
        /// </summary>
        public void LinkLine()
        {
            TreeView graphView = BTWindow.windowRoot.treeView;
            switch (NodeData)
            {
                case BtComposite composite:
                    composite.ChildNodes.ForEach(m =>
                    {
                        graphView.AddElement(PortLink(graphView.NodeViews[m.Guid].InputPort,
                            OutPort));
                    });
                    break;
                case BtPrecondition precondition:
                    if(precondition.ChildNode == null)
                        return;
                    graphView.AddElement(PortLink( graphView.NodeViews[precondition.ChildNode.Guid].InputPort,OutPort));
                    break;
            }
        }

        public static Edge PortLink(Port p1, Port p2)
        {
            var tempEdge = new Edge()
            {
                output = p2,
                input = p1
            };
            tempEdge?.input.Connect(tempEdge);
            tempEdge?.output.Connect(tempEdge);
            return tempEdge;
        }

        /// <summary>
        /// 右键菜单
        /// </summary>
        /// <param name="evt"></param>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (! (NodeData is BtActionNode))
            {
                evt.menu.AppendAction("SetRoot", SetRoot);
            }
            
        }

        /// <summary>
        /// 设置根节点
        /// </summary>
        /// <param name="obj"></param>
        private void SetRoot(DropdownMenuAction obj)
        {
            BtSetting.GetSetting().SetRoot(NodeData);
        }

        public static Port NodePortCreate(NodeView view)
        {
            Port port = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single,
                typeof(NodeView));
            return port;
        }

        public static Port NodeOutPortCreate(NodeView view, bool issingle)
        {
            Port port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output,
                issingle ? Port.Capacity.Single : Port.Capacity.Multi,
                typeof(NodeView));
            return port;
        }
    }
}