using System;
using Buff_system_timesfaner.TBuff_base;
using Buff_system_timesfaner.TModule.Base;
using UnityEngine;

namespace Buff_system_timesfaner.TBuffHandler
{
    public interface IBuffHandler
    {
        /// <summary>
        /// 添加Buff 
        /// </summary>
        /// <param name="buffinfo">施加Buff的info</param>
        public void AddBuff(Buffinfo buffinfo);
        /// <summary>
        /// 移除Buff
        /// </summary>
        /// <param name="buffinfo">要移除的Buff </param>
        public void RemoveBuff(Buffinfo buffinfo);
        /// <summary>
        /// 注册事件：添加Buff时
        /// </summary>
        /// <param name="act">注册的行为</param>
        public void RegisterOnAddBuff(Action act);
        /// <summary>
        /// 删除事件：添加Buff时
        /// </summary>
        /// <param name="act">注册的行为</param>
        public void RemoveOnAddBuff(Action act);
        /// <summary>
        /// 注册事件：删除Buff时
        /// </summary>
        /// <param name="act">注册的行为</param>
        public void RegisterOnRemoveBuff(Action act);
        /// <summary>
        /// 删除事件：删除Buff时
        /// </summary>
        /// <param name="act">注册的行为</param>
        public void RemoveOnRemoveBuff(Action act);
    }
}