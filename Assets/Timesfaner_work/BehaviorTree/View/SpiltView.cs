using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Timesfaner_work.BehaviorTree.View
{
    public class SplitView : TwoPaneSplitView
    {
        #region VisualElement内容
        public InspectorView InspectorView;
        public Label InspectorTitle;
        public TreeView TreeView;
        #endregion

        
        public new class UxmlFactory : UxmlFactory<SplitView,UxmlTraits>{}
        public SplitView()
        {
            Init();
        }

        private void Init(){}
    }
}