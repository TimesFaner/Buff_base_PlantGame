using System;
using System.Collections;
using System.Collections.Generic;
using Remake;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = Remake.PlayerInput;

namespace Timesfaner_work.Action_system
{
    public static class CompositeKeytoTick
    {
        public static MonoBehaviour mono = EventsMgr.Instance;

        // public Dictionary<string, InputActionReference> InputActionsDic;
        // public List<InputActionReference> CurrentInput;

        /// <summary>
        ///     按键同时按下时触发
        /// </summary>
        /// <param name="theDelegate"></param>
        /// <param name="keys"></param>
        public static void TickWithkeysSameTime(this Delegate theDelegate, List<InputActionReference> keys)
        {
            if (CheckInput(keys))
            {
                theDelegate.Method.Invoke(theDelegate.Target, null);
            }
        }


        public static void InputActionDicInit()
        {
            InputDicClass.Instance.GetRf();
            PlayerInputAction tempInput = PlayerInput.Instance.playerInputAction;
            // 为当前所存的inputaction的输入监听
            foreach (var inputAction in tempInput)
            {
                inputAction.started += AddCallback;
            }
        }
/// <summary>
/// 用完取消委托
/// </summary>
        public static void InputActionDicInitCancel()
        {
            InputDicClass.Instance.GetRf();
            PlayerInputAction tempInput = PlayerInput.Instance.playerInputAction;
            // 为当前所存的inputaction的输入监听
            foreach (var inputAction in tempInput)
            {
                inputAction.started -= AddCallback;
            }
        }
        public static void AddCallback( InputAction.CallbackContext context)
        {
            var inputAction = context.action;
            if (!inputAction.bindings[0].isComposite && All_Data.isTickTime)
            {
                // Debug.Log("Adding InputAction to currentInput: " + inputAction.name);
                InputDicClass.Instance.currentInput.Add(InputActionReference.Create(inputAction));
                // Debug.Log(InputDicClass.Instance.currentInput[0]+"curr"+InputDicClass.Instance.currentInput.Count);
            }
            
        }


        /// <summary>
        /// 是否顺序按下对应输入（还没有适配Composite）
        /// </summary>
        /// <param name="theDelegate"></param>
        /// <param name="keys"></param>
        /// <param name="limitTime"></param>
        public static void TickWithOrder(this Delegate theDelegate, List<string> keys, float limitTime)
        {
            List<InputActionReference> templist = new List<InputActionReference>();
            foreach (var key in keys)
            {
                templist.Add(InputDicClass.Instance.InputActionsDic[key]);
            }

            IEnumerator temp()
            {
                All_Data.isTickTime = true;
                while (limitTime > 0)
                {
                    if (CheckOrder(templist))
                    {
                        Debug.Log("cccheck");
                        theDelegate.Method.Invoke(theDelegate.Target, null);
                        All_Data.isTickTime = false;
                         break;
                    }

                    yield return new WaitForSeconds(Time.deltaTime);
                    limitTime -= Time.deltaTime;
                }
                Debug.Log("QTE TIME OUT");
                All_Data.isTickTime = false;
            }

            Debug.Log("QTE CANCELED");
            InputDicClass.Instance.currentInput.Clear();

            mono.StartCoroutine(temp());
        }


        /// <summary>
        ///  检测是否顺序按下
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static bool CheckOrder(List<InputActionReference> keys)
        {
            string templist = "";
            string templist2 = "";
            foreach (var item in keys)
            {
                templist+=(item.name);
                // Debug.Log("keyname:"+item.name);
            }
            foreach (var item in InputDicClass.Instance.currentInput)
            {
                templist2+=( item.name);
                // Debug.Log("22keyname:"+item.name);
            }
            if (templist == templist2)
            {
                Debug.Log("yesyes"+InputDicClass.Instance.currentInput[0].name);
                InputDicClass.Instance.currentInput.Clear();
                return true;
            }
            InputDicClass.Instance.currentInput?.RemoveAt(0);//此次失败去除首位输入，下次继续检测剩余输入
            return false;
        }

        public static bool CheckInput(List<InputActionReference> keys)
        {
            if (keys.TrueForAll(a => a.action.IsPressed())) return true;

            return false;
        }
    }

    /// <summary>
    /// 可用于搭配CompositeKeytoTick使用
    /// </summary>
    public class Test
    {
        public void Test1()
        {
            Console.WriteLine("Test1");
        }

        private Delegate A()
        {
            return new Action(Test1);
        }

        public void Test2()
        {
            Console.WriteLine("Test2");
        }
    }
}