using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.Utilities;
using Timesfaner_work.BehaviorTree;
using Timesfaner_work.BehaviorTree.BTNodeBase;
using Timesfaner_work.BehaviorTree.Test;
using Timesfaner_work.BehaviorTree.Tree;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.SceneManagement;

namespace Timesfaner_work.BehaviorTree.View
{
    public class BTWindow : EditorWindow
    {
        public static BTWindow windowRoot;
        public TreeView treeView;
        public InspectorView InspectorView;
        
        [MenuItem("Tools/BTWindow  _&t")] //ALT B
        public static void ShowExample()
        {
            BTWindow wnd = GetWindow<BTWindow>();
            wnd.titleContent = new GUIContent("BTWindow");
        }

        public void CreateGUI()
        {
            int id = BtSetting.GetSetting().TreeID;
            IgetBt igetBt = EditorUtility.InstanceIDToObject(id) as IgetBt;
            windowRoot = this;
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            var visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Timesfaner_work/BehaviorTree/View/BTWindow.uxml");
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Timesfaner_work/BehaviorTree/View/BTWindow.uss");
            visualTree.CloneTree(root);
//窗口建根
            treeView = root.Q<TreeView>();
            InspectorView = root.Q<InspectorView>();
            if(igetBt ==null) return;
            if(igetBt.GetRoot()==null) return;
            CreateRoot(igetBt.GetRoot());
            //连接节点
            treeView.nodes.OfType<NodeView>().ForEach(n => n.LinkLine());
        }

        private void OnDestroy()
        {
            Save();
        }

        public void Save()
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        /// <summary>
        /// 通过创建根节点创建树
        /// </summary>
        /// <param name="rootNode"></param>
        public void CreateRoot(BtNodeBase rootNode)
        {
            if (rootNode==null)return;
            
            NodeView nodeView = new NodeView(rootNode);
            nodeView.SetPosition(new Rect(rootNode.Position, Vector2.one));
            treeView.AddElement(nodeView);
            treeView.NodeViews.Add(rootNode.Guid,nodeView);
            switch (rootNode)
            {
                case BtComposite composite:
                    composite.ChildNodes.ForEach(CreateChild);
                    break;
                case BtPrecondition precondition:
                    CreateChild(precondition.ChildNode);
                    break;
            }
        }
        public void CreateChild(BtNodeBase nodeData)
        {
            if (nodeData==null)return;
            
            NodeView nodeView = new NodeView(nodeData);
            nodeView.SetPosition(new Rect(nodeData.Position, Vector2.one));
            treeView.AddElement(nodeView);
            treeView.NodeViews.Add(nodeData.Guid,nodeView);
            //view.NodeViews.Add(rootNode.Guid,nodeView);
            switch (nodeData)
            {
                case BtComposite composite:
                    composite.ChildNodes.ForEach(CreateChild);
                    break;
                case BtPrecondition precondition:
                    CreateChild(precondition.ChildNode);
                    break;
            }
        }

    }

    public class RightClickMenu : ScriptableObject, ISearchWindowProvider
    {
        public delegate bool SelectEntryDelegate(SearchTreeEntry searchTreeEntry, SearchWindowContext context);

        public SelectEntryDelegate OnSelectEntryHandle;

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>();

            entries.Add(new SearchTreeGroupEntry(new GUIContent("Create Node")));
            entries = AddNodeType<BtComposite>(entries, "组合节点");
            entries = AddNodeType<BtPrecondition>(entries, "条件节点");
            entries = AddNodeType<BtActionNode>(entries, "行为节点");
            return entries;
        }

        /// <summary>
        /// 通过反射获取对应的菜单数据
        /// </summary>
        public List<SearchTreeEntry> AddNodeType<T>(List<SearchTreeEntry> entries, string pathName)
        {
            entries.Add(new SearchTreeGroupEntry(new GUIContent(pathName)) { level = 1 });
            List<System.Type> rootNodeTypes = GetDerivedClasses(typeof(T));
            foreach (var rootType in rootNodeTypes)
            {
                string menuName = rootType.Name;

                entries.Add(new SearchTreeEntry(new GUIContent(menuName)) { level = 2, userData = rootType });
            }

            return entries;
        }

        /// <summary>
        /// 获取所有继承了该类型的类型，包含子类的子类
        /// </summary>
        public static List<Type> GetDerivedClasses(Type type)
        {
            List<Type> derivedClasses = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in assembly.GetTypes())
                {
                    if (t.IsClass && !t.IsAbstract && type.IsAssignableFrom(t))
                    {
                        derivedClasses.Add(t);
                    }
                }
            }

            return derivedClasses;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            if (OnSelectEntryHandle == null)
            {
                return false;
            }

            return OnSelectEntryHandle
                (SearchTreeEntry, context);
        }
    }
}