using System.Collections.Generic;
using System.Linq;
using Buff_system_timesfaner.TBuff_base;
using Buff_system_timesfaner.TBuffHandler;
using Timesfaner_work.BaseManager;
using UnityEngine;

public class TBuff_center : SingletonMonoBase<TBuff_center>
{
    private const string AssetPath = "T_";
    public Dictionary<int, BuffData> BuffDataDic = new();

    protected void Awake()
    {
        DontDestroyOnLoad(gameObject);
        LoadBuffData();
    }

    private void LoadBuffData()
    {
        var tempBuffData = Resources.LoadAll<BuffData>("BuffData");
        // var tempData = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/Timesfaner_work/Buff_system_timesfaner/Data/BuffData");
        // var tempBuffData = tempData as BuffData[];
        tempBuffData.ToList().ForEach(buffData => BuffDataDic.Add(buffData.Id, buffData));
        //  Debug.Log("Load"+tempBuffData);
    }

    public void AddBuff(GameObject creator, GameObject target, int buffID)
    {
        if (target.GetComponent<Buffhandler>() == null) Debug.LogError("Target has no BuffHandler component");

        var buffInfo = new Buffinfo(BuffDataDic[buffID], creator, target);
        target.GetComponent<Buffhandler>().AddBuff(buffInfo);
    }
}