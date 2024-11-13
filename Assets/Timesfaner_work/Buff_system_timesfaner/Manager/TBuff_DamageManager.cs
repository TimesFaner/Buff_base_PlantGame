using Buff_system_timesfaner.TBuff_base;
using Buff_system_timesfaner.TBuffHandler;
using Timesfaner_work.BaseManager;

namespace Timesfaner_work.Buff_system_timesfaner.Manager
{
    public class TBuff_DamageManager : SingletonMonoBase<TBuff_DamageManager>
    {
        public void SubmitDamage(Damageinfo damageinfo)
        {
            var createrBuffhandler = damageinfo.creater?.GetComponent<Buffhandler>();
            var targetBuffhandler = damageinfo.creater?.GetComponent<Buffhandler>();

            if (createrBuffhandler)
                foreach (var buffinfo in createrBuffhandler.Bufflist)
                    buffinfo.buffData.OnHit?.CallandInvoke(buffinfo, damageinfo);

            if (targetBuffhandler)
            {
                targetBuffhandler.gameObject.GetComponent<HealthController>().GetHurt(damageinfo.damage);
                foreach (var buffinfo in targetBuffhandler.Bufflist)
                    buffinfo.buffData.OnBeHit?.CallandInvoke(buffinfo, damageinfo);

                var a = targetBuffhandler?.GetComponent<HealthController>();
                if (a.CanbeKill(damageinfo))
                {
                    foreach (var buffinfo in targetBuffhandler.Bufflist)
                        buffinfo.buffData.OnBekill?.CallandInvoke(buffinfo, damageinfo);
                    if (a.CanbeKill(damageinfo)) //二次判断是否死亡
                        if (createrBuffhandler)
                            foreach (var buffinfo in createrBuffhandler.Bufflist)
                                buffinfo.buffData.OnKill?.CallandInvoke(buffinfo, damageinfo);
                }
            }
        }
    }
}