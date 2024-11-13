using System;
using Buff_system_timesfaner.TModule.Base;
using Buff_system_timesfaner.Uility;
using UnityEditor;
using UnityEngine;

namespace Buff_system_timesfaner.TBuff_base
{
    [CreateAssetMenu(fileName = "BuffData", menuName = "Buff/BuffBase/BuffData", order = 0)]
    [Serializable]
    public class BuffData : ScriptableObject
    {
        #region Enum

        public AddType AddandSubType;

        #endregion

        [Button]
        public void AllClone()
        {
            ClonetheTData(OnCreate);
            ClonetheTData(OnRemove);
            ClonetheTData(OnTick);
            ClonetheTData(OnDestroy);
            ClonetheTData(OnHit);
            ClonetheTData(OnBeHit);
            ClonetheTData(OnKill);
            ClonetheTData(OnBekill);
        }

        public void ClonetheTData(BuffInvoke T)
        {
            if (T && !T.name.Contains("clone"))
            {
                var _type = T.GetType();
                var _SO = CreateInstance(_type) as BuffInvoke;
                _SO = Instantiate(T);
                _SO.name = T.name + "clone";
                    T = _SO;
                AssetDatabase.CreateAsset(_SO,
                    "Assets/Timesfaner_work/Buff_system_timesfaner/Data/Clone/" + _SO.name + ".asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }


        #region 对象问题

        public static BuffData CreateInstance(string buffName, int id)
        {
            var b = (BuffData)CreateInstance(buffName);
            b.Id = id;
            return b;
        }

        /// <summary>
        ///     克隆当前的Buff
        /// </summary>
        /// <returns>克隆的Buff</returns>
        public BuffData Clone()
        {
            return Instantiate(this);
        }

        #endregion


        #region Data

        public int Id;
        public string BuffName;
        public string Description;
        public Sprite Icon;
        public int MaxLevel;
        public int Priority;
        public string[] Tags;

        #endregion

        #region 时间信息

        public bool IsForever;
        public float Duration;
        public float TickMaxTime;

        #endregion

        #region Ibuffcallback

        public BuffInvoke OnCreate;

        //层数减少
        public BuffInvoke OnRemove;

        //彻底清楚
        public BuffInvoke OnDestroy;
        public BuffInvoke OnTick;
        public BuffInvoke OnHit;
        public BuffInvoke OnBeHit;
        public BuffInvoke OnBekill;
        public BuffInvoke OnKill;

        #endregion
    }
}