using System.Collections;
using Timesfaner_work.BaseManager;
using UnityEngine;

public class EnemyGenerater : SingletonMonoBase<EnemyGenerater>
{
    [Header(" 在场敌人数量上限 ")] [SerializeField]
    public int enemyCount;

    [Header(" 在场敌人刷新时间 ")] [SerializeField]
    public float cdTime;

    // Start is called before the first frame update
    private void Start()
    {
        StartCoroutine("Generate");
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private IEnumerator Generate()
    {
        while (true)
        {
            Debug.Log("协程开启");
            if (All_Data.isGaming)
            {
                if (Pool.Instance.thecount.ContainsKey("Prefabs/Enemy"))
                {
                    Debug.Log("contains：" + "Prefabs/Enemy" + Pool.Instance.thecount["Prefabs/Enemy"]);

                    if (Pool.Instance.thecount["Prefabs/Enemy"] <= enemyCount - 1)
                        Pool.Instance.GetObj("Prefabs/Enemy", Vector3.zero);
                }
                else
                {
                    Pool.Instance.GetObj("Prefabs/Enemy", Vector3.zero);
                }
            }

            yield return new WaitForSeconds(cdTime);
        }
    }
}