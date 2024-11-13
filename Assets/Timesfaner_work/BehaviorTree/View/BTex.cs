using System.Collections.Generic;
using System.Linq;
using QFramework;
using Sirenix.Serialization;
using Timesfaner_work.BehaviorTree.BTNodeBase;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Timesfaner_work.BehaviorTree.View
{
    public static class BTex
    {
        public static void LinkLineAddData(this Edge edge)
        {
            NodeView outNode = edge.output.node as NodeView;
            NodeView InputNode = edge.input.node as NodeView;
            switch (outNode.NodeData)
            {
                case BtComposite composite:
                    composite.ChildNodes.Add(InputNode.NodeData);
                    return;
                case BtPrecondition precondition:
                    precondition.ChildNode =
                        InputNode.NodeData;
                    return;
            }
        }

        public static void UnLinkLineRemoveData(this Edge edge)
        {
            {
                NodeView outNode = edge.output.node as NodeView;
                NodeView InputNode = edge.input.node as NodeView;
                switch (outNode.NodeData)
                {
                    case BtComposite composite:
                        composite.ChildNodes.Remove(InputNode.NodeData);
                        return;
                    case BtPrecondition precondition:
                        precondition.ChildNode =
                            null;
                        return;
                }
            }
        }
        /// <summary>
        /// 用odin序列化去克隆选择的节点
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public static List<BtNodeBase> CloneData(this List<BtNodeBase> nodes)
        {
            byte[] nodeBytes= SerializationUtility.SerializeValue(nodes, DataFormat.Binary);
            var toNode = SerializationUtility.DeserializeValue<List<BtNodeBase>>(nodeBytes ,DataFormat.Binary);
            
            //删掉未复制的子数据 并随机新的Guid 位置向右下偏移
            for (int i = 0; i < toNode.Count; i++)
            {
                toNode[i].Guid = System.Guid.NewGuid().ToString(); 
                switch (toNode[i])
                {
                    case BtComposite composite:
                        if (composite.ChildNodes .Count==0)break;
                        composite.ChildNodes = composite.ChildNodes.Intersect(toNode).ToList();
                        break;
                    case BtPrecondition precondition :
                        if (precondition.ChildNode == null)break;
                        if (!toNode.Exists(n => n == precondition.ChildNode))
                        {
                            precondition.ChildNode = null;  
                        }
                        break;
                }
                toNode[i].Position += Vector2.one * 30;
            }
            
            return toNode;
        }
    }
    
}