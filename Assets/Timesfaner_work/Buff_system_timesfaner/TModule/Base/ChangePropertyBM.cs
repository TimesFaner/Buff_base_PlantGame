using Buff_system_timesfaner.TBuff_base;
using Remake;
using UnityEngine;

namespace Buff_system_timesfaner.TModule.Base
{[CreateAssetMenu(fileName = "ChangePropertyBM", menuName = "Buff/BuffMoudle/ChangePropertyBM")]
    public class ChangePropertyBM : BuffInvoke
    {
    public float damage;

                
        public override void CallandInvoke(Buffinfo _buffinfo, Damageinfo _damageinfo = null)
        {
        _buffinfo.target.GetComponent<HealthController>().GetHurt(damage);
        }
        
    }
}