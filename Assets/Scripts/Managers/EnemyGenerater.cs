using System.Collections;
using System.Collections.Generic;
using Manager;
using UnityEngine;

public class EnemyGenerater : SingletonMonoBase<EnemyGenerater>
{
    [Header(" 在场敌人数量上限 ")] [SerializeField]
    public int enemyCount; 
    [Header(" 在场敌人刷新时间 ")] [SerializeField]
    public float cdTime;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine("Generate");
    }

    // Update is called once per frame
    void Update()
    {
       
            
         
    }
    
    IEnumerator  Generate()
    {
        while (true)
        {
            Debug.Log("协程开启");
            if (All_Data.isGaming )
            {
                if (PoolManager.Instance.thecount.ContainsKey("Prefabs/Enemy"))
                {
                    Debug.Log("contains："+"Prefabs/Enemy"+PoolManager.Instance.thecount["Prefabs/Enemy"]);
                
                    if (PoolManager.Instance.thecount["Prefabs/Enemy"] <= enemyCount-1)
                    {
                        PoolManager.Instance.GetObj("Prefabs/Enemy",Vector3.zero);
                    }
                }
                else
                {
                    PoolManager.Instance.GetObj("Prefabs/Enemy",Vector3.zero);
                }
            }
            yield return new WaitForSeconds(cdTime);
        }
        
    }
    
   
}
