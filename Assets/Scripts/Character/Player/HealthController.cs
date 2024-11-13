using Buff_system_timesfaner.TBuff_base;
using Mirror;
using UnityEngine;

public class HealthController : NetworkBehaviour
{
    [HideInInspector] public float health = All_Data.PlayerHealth;
    public float _currentHealth;
    public PHealthUI _PHealthUI;


    // Start is called before the first frame update
    private void Awake()
    {
    }

    private void Start()
    {
        _currentHealth = health;
        _PHealthUI.Gethealth(_currentHealth, health);

        // TBuff_center.Instance.AddBuff(this.gameObject,this.gameObject,1);
        // TBuff_DamageManager.Instance.SubmitDamage(new Damageinfo(this.gameObject,this.gameObject,10));
    }

    private void Update()
    {
    }

    public override void OnStartLocalPlayer()
    {
         _PHealthUI = GameObject.Find("HealthManager").GetComponent<PHealthUI>();
    }

    public void GetHurt(float damage)
    {
        if (_currentHealth - damage <= 0)
            _currentHealth = 0;
        else
            _currentHealth -= damage;


        _PHealthUI.Gethealth(_currentHealth, health);
    }

    public bool CanbeKill(Damageinfo damageinfo)
    {
        if (_currentHealth - damageinfo.damage <= 0)
            return true;
        return false;
    }
}