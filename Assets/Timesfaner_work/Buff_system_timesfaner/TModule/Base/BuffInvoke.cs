using Buff_system_timesfaner.TBuff_base;
using UnityEngine;

namespace Buff_system_timesfaner.TModule.Base
{
    public abstract class BuffInvoke : ScriptableObject
    {
        public abstract void CallandInvoke(Buffinfo _buffinfo, Damageinfo _damageinfo = null);
    }
}