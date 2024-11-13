using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Timesfaner_work.BehaviorTree.BTNodeBase;
using Timesfaner_work.BehaviorTree.Tree;
using UnityEngine;
using UnityEngine.UIElements;

namespace Timesfaner_work.BehaviorTree.View
{
    public class InspectorView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits>
        {
        }

        public IMGUIContainer inspectorBar;
        public InspectorViewData viewData;

        public InspectorView()
        {
            inspectorBar = new IMGUIContainer() { name = "inspectorBar" };
            inspectorBar.style.flexGrow = 1;
            CreateInspectorView();
            Add(inspectorBar);
        }

        /// <summary>
        /// 更新选择节点面板显示
        /// </summary>
        public void UpdateInspector()
        {
            HashSet<BtNodeBase> node = BTWindow.windowRoot.treeView.selection.OfType<NodeView>().Select(n => n.NodeData)
                .ToHashSet();
            viewData.DataView.Clear();
            foreach (var n in node)
            {
                viewData.DataView.Add(n);
            }
        }

        private async void CreateInspectorView()
        {
            viewData = Resources.Load<InspectorViewData>("InspectorViewData");
            await Task.Delay(100);
            // 利用odin创建一个面板
            var odinEditor = UnityEditor.Editor.CreateEditor(viewData);

            inspectorBar.onGUIHandler += () => { odinEditor.OnInspectorGUI(); };
        }
    }
}