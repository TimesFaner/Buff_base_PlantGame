using System;
using System.Collections.Generic;
using Timesfaner_work.BaseManager;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Timesfaner_work.Action_system
{[Serializable]
    public class InputDicClass : SingletonMonoBase<InputDicClass>
    {
        public List<InputReferenceData> Datas = new List<InputReferenceData>();
        public Dictionary<string, InputActionReference> InputActionsDic = new Dictionary<string, InputActionReference>();
        public List<InputActionReference> currentInput = new List<InputActionReference>();

        /// <summary>
        /// 在使用前需要调用此接受引用
        /// </summary>
        public void GetRf()
        {
            InputActionsDic.Clear();
            for (int i = 0; i < Datas.Count; i++)
            {
                InputActionsDic.Add(Datas[i].name, Datas[i].actionRf);
            } 
        }
        
        ~InputDicClass()
        {
            InputActionsDic.Clear();
        }
            
    }
    
    
    [System.Serializable]
    public struct InputReferenceData
    {
        public string name;
        public InputActionReference actionRf;
    }
}