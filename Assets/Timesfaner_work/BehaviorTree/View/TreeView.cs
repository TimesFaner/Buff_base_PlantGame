using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using QFramework;
using Timesfaner_work.BehaviorTree.BTNodeBase;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Timesfaner_work.BehaviorTree.View
{
    public class TreeView : GraphView
    {
        public new class UxmlFactory : UxmlFactory<TreeView, UxmlTraits>
        {
        }

        public Dictionary<string, NodeView> NodeViews = new Dictionary<string, NodeView>();
        public List<BtNodeBase> CopyNodes = new List<BtNodeBase>();
        public TreeView()
        {
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            GraphViewMenu();
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Timesfaner_work/BehaviorTree/View/BTWindow.uss"));
            
            graphViewChanged += OnGraphViewChanged;
            RegisterCallback<MouseEnterEvent>(MouseEnterCtrl);
            //键盘快捷键操作
            RegisterCallback<KeyDownEvent>(KeyDownEventCallBack);
        }

        private void KeyDownEventCallBack(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Tab)
            {
                evt.StopPropagation();
            }

            if (!evt.ctrlKey) return;
            switch (evt.keyCode)
            {
                case KeyCode.S:
                    BTWindow.windowRoot.Save();
                    evt.StopPropagation();
                    break;
                case KeyCode.E:
                   // OnStartMove();
                    evt.StopPropagation();
                    break;
                case KeyCode.X:
                  //  Cut(null);
                    evt.StopPropagation();
                    break;
                case KeyCode.C:
                    Copy();
                    evt.StopPropagation();
                    break;
                case KeyCode.V:
                    Paste(null);
                    evt.StopPropagation();
                    break;
            }
        }

        #region keyboard
        private void Copy()
        {
            List<BtNodeBase> nodeData = selection.OfType<NodeView>().Select(n => n.NodeData).ToList();
            CopyNodes = nodeData.CloneData();
        }
        private void Paste(object o)
        {
            var PasteNode = new List<NodeView>();
            for (int i = 0; i < CopyNodes.Count; i++)
            {
                var newNode = new NodeView(CopyNodes[i]);
                newNode.SetPosition(new Rect(CopyNodes[i].Position,Vector2.one));
                AddElement(newNode);
                PasteNode.Add(newNode);
                NodeViews.Add(CopyNodes[i].Guid ,newNode);
            }

            PasteNode.ForEach(n => n.LinkLine());
            CopyNodes = CopyNodes.CloneData();
        }

        #endregion

        
        private void MouseEnterCtrl(MouseEnterEvent evt)
        {
            
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange gvc)
        {
            if (gvc.edgesToCreate != null)
            {
                gvc.edgesToCreate.ForEach(edge =>
                {
                    edge.LinkLineAddData();
                });
            }

            if (gvc.elementsToRemove != null)
            {
                gvc.elementsToRemove.ForEach(ele =>
                {
                    if (ele is Edge edge)
                    {
                        edge.UnLinkLineRemoveData();
                    }
                });
            }

            return gvc;
        }

        //点击右键餐单触发
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            // evt.menu.AppendAction("Create Node" ,CreateNode);
        }
//定义连接规则
        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            return ports.Where(endPorts => 
                    endPorts.direction != startAnchor.direction && endPorts.node != startAnchor.node)
                .ToList();
        } 
        private void CreateNode(Type type,Vector2 position)
        {
            BtNodeBase _nodeData = Activator.CreateInstance(type) as BtNodeBase;
            _nodeData.NodeName = type.Name;
            NodeView node = new NodeView(_nodeData);
            _nodeData.Guid = Guid.NewGuid().ToString();
            node.SetPosition(new Rect(position,Vector2.one));
            NodeViews.Add(_nodeData.Guid ,node);
            
            this.AddElement(node);
            
            
        }
/// <summary>
/// 右键搜索菜单
/// </summary>
        private void GraphViewMenu()
        {
            var menuProvider = RightClickMenu.CreateInstance<RightClickMenu>();
            menuProvider.OnSelectEntryHandle = OnMenuSelectEntry;
            nodeCreationRequest += context =>
            {
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), menuProvider);
            };
        }
/// <summary>
/// 获取鼠标位置
/// </summary>
/// <param name="searchTreeEntry"></param>
/// <param name="context"></param>
/// <returns></returns>
        private bool OnMenuSelectEntry(SearchTreeEntry searchTreeEntry,SearchWindowContext context)
        {
            var windowRoot = BTWindow.windowRoot.rootVisualElement;
            var windowRootMousePos = windowRoot.ChangeCoordinatesTo(windowRoot.parent, context.screenMousePosition -BTWindow.windowRoot.position.position);
            var graphMousePosition = contentContainer.WorldToLocal(windowRootMousePos);
            CreateNode((Type)searchTreeEntry.userData,graphMousePosition);
            return true;
        }
    }
}