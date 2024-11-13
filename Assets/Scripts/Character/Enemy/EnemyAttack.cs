using UnityEngine;

public delegate void CdTimeAttack_DE();

public class EnemyAttack : MonoBehaviour
{
    public LayerMask _layerMask;
    public float damage = 10f;
    private readonly float radius = 1.2f;
    private float enemycdtime;
    private float nowCdTime;


    private void Start()
    {
        enemycdtime = All_Data.EnemyAttackCd;
        nowCdTime = enemycdtime;
    }

    private void Update()
    {
        CdDoIt(FixedTimeAttack);

        // nowCdTime -= Time.deltaTime;
        // if (nowCdTime<=0)
        // {
        //     nowCdTime=enemycdtime;
        //     Debug.Log("333");
        //     //FixedTimeAttack();
        //     
        // }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Debug.Log("1");
        if (other.CompareTag("Player"))
        {
            //Attack(other,damage);
        }
    }


    public void Attack(Collider2D _collider, float _damage)
    {
        _collider?.GetComponent<HealthController>().GetHurt(_damage);
        Debug.Log(_collider.gameObject);
    }

    private void CdDoIt(CdTimeAttack_DE callback)
    {
        nowCdTime -= Time.deltaTime;
        if (nowCdTime <= 0)
        {
            nowCdTime = enemycdtime;
            callback();
        }
    }

    private void FixedTimeAttack()
    {
        bool isat = Physics2D.OverlapCircle(transform.position, radius, 1 << 6);
        var other = Physics2D.OverlapCircle(transform.position, radius, 1 << 6);
        if (isat)
        {
            Attack(other, damage);
            Debug.Log('3');
            isat = !isat;
        }
    }
}