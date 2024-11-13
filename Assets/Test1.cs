using System;
using System.Collections;
using System.Collections.Generic;
using Buff_system_timesfaner.Uility;
using Timesfaner_work.Action_system;
using UnityEngine;
using UnityEngine.InputSystem;

public class Test1 : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        CompositeKeytoTick.InputActionDicInit();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDisable()
    {
        CompositeKeytoTick.InputActionDicInitCancel();
    }

    Delegate De()
    {
        return new Action(deb);
    }
    public void deb()
    {
        Debug.Log("dddd");
    }

    [Button]public void tt()
    {
        InputDicClass.Instance.GetRf();
        foreach (var VARIABLE in InputDicClass.Instance.Datas)
        {
            Debug.Log("keys:"+VARIABLE.name);
        } 
        De().TickWithOrder(new List<string>(){"Attack"},3);
    }
}
