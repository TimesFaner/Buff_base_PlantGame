using UnityEngine;

namespace Buff_system_timesfaner.TBuff_base
{
    public class Buffproperty
    {
    }

    public class Buffinfo
    {
        public BuffData buffData;
        public GameObject creater;
        public int curLevel;
        public float durationTimer;
        public GameObject target;
        public float tickTimer;

        /// <summary>
        ///     公共构造函数
        /// </summary>
        /// <param name="creater"></param>
        /// <param name="target"></param>
        /// <param name="durationTimer"></param>
        /// <param name="tickTimer"></param>
        /// <param name="curLevel"></param>
        public Buffinfo(BuffData buffData, GameObject creater, GameObject target)
        {
            this.creater = creater;
            this.target = target;
            this.buffData = buffData;
        }
    }

    public class Damageinfo
    {
        public GameObject creater, target;
        public int damage;

        /// <summary>
        /// </summary>
        /// <param name="creater"></param>
        /// <param name="target"></param>
        /// <param name="damage"></param>
        public Damageinfo(GameObject creater, GameObject target, int damage)
        {
            this.creater = creater;
            this.target = target;
            this.damage = damage;
        }
    }

    /// <summary>
    ///     考虑是叠加还是一次性全部处理
    /// </summary>
    public enum AddType
    {
        addsub,
        replace,
        indepent,
        keep
    }
}