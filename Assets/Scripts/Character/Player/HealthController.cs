using System;
using System.Collections;
using System.Collections.Generic;
using Buff_system_timesfaner.TBuff_base;
using Buff_system_timesfaner.TBuffHandler;
using Mirror;
using UnityEngine;
using UnityEngine.XR;

public class HealthController : NetworkBehaviour
{
    [HideInInspector]public float health = All_Data.PlayerHealth;
     public float _currentHealth;
     public PHealthUI _PHealthUI;


    // Start is called before the first frame update
    private void Awake()
    {
        
    }

    public override void OnStartLocalPlayer()
    {
        _PHealthUI = new PHealthUI();
    }

    void Start()
    {
        _currentHealth = health;
        _PHealthUI.Gethealth(_currentHealth,health);

        TBuff_center.Instance.AddBuff(this.gameObject,this.gameObject,1);
    }

    public void GetHurt(float damage)
    {
        if ((_currentHealth - damage) <= 0)
        {
            _currentHealth = 0;
        }
        else
        {
            _currentHealth -= damage;
        }
        
        
        _PHealthUI.Gethealth(_currentHealth,health);
        
    }

    public bool CanbeKill(Damageinfo damageinfo)
    {
        if ((_currentHealth - damageinfo.damage) <= 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Update()
    {
   
    }
}
