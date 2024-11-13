using System.Collections.Generic;
using Buff_system_timesfaner.Uility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Timesfaner_work.Expand.key
{
    public class ChangeBinding : MonoBehaviour
    {
        //Action名
        public string actionLabel;
        //Binding名
        public string bindingLabel;
        //Binding的id
        public string bindingId;

        private int index;
        
         public List<InputActionReference> AllactionReference;
         public InputActionReference actionReference;
         //绑定操作
        private InputActionRebindingExtensions.RebindingOperation RebindOperation;
        
        private void InitPorperty()
        {
            actionLabel = actionReference.name;
            index = 0;
            bindingId = actionReference.action.bindings[index].id.ToString();
            UpdatePorperty(index);
            
        }

        public void ChangeAction(InputActionReference action)
        {
            actionReference = action;
            InitPorperty();
        }
        /// <summary>
        /// 替换\
        /// </summary>
        [Button]
        public void StartInteractiveRebind()
        {
            //获取bindingIndex，Bingdings数组的下标，从0开始，如果action为空返回false
            if (!CheckActionAndBinding(out int bindingIndex))
                return;

            // If the binding is a composite, we need to rebind each part in turn.
            if (actionReference.action.bindings[bindingIndex].isComposite)
            {
                var firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < actionReference.action.bindings.Count &&
                    actionReference.action.bindings[firstPartIndex].isPartOfComposite)
                    PerformInteractiveRebind(actionReference, firstPartIndex, allCompositeParts: true);
            }
            else
            {
                PerformInteractiveRebind(actionReference, bindingIndex);
            }
        }
        /// <summary>
        /// 将此Action的bindingIndex输出，
        /// 并检查此Action是否为空，如果为空返回false
        /// </summary>
        /// <param name="bindingIndex"></param>
        /// <returns></returns>
        private bool CheckActionAndBinding(out int bindingIndex)
        {
            bindingIndex = -1;
            if (actionReference == null)
                return false;
            bindingIndex = actionReference.action.bindings.IndexOf(x => x.id == new System.Guid(bindingId));
            return true;
        }

        private void PerformInteractiveRebind(InputAction action, int bindingIndex, bool allCompositeParts = false)
        {
            RebindOperation?.Cancel(); // Will null out m_RebindOperation.

            void CleanUp()
            {
                RebindOperation?.Dispose();
                RebindOperation = null;
            }

            // Configure the rebind.
            RebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("Mouse") //剔除鼠标
                .OnCancel(
                    operation => { CleanUp(); })
                .OnComplete(
                    operation =>
                    {
                        UpdatePorperty(index);
                        CleanUp();
                        if (allCompositeParts)
                        {
                            var nextBindingIndex = bindingIndex + 1;
                            if (nextBindingIndex < action.bindings.Count &&
                                action.bindings[nextBindingIndex].isPartOfComposite)
                                PerformInteractiveRebind(action, nextBindingIndex, true);
                        }
                    });
            RebindOperation.Start();
        }
/// <summary>
/// 指定
/// </summary>
/// <param name="action"></param>
/// <param name="bindingIndex"></param>
/// <param name="path"></param>
/// example: ReBindingWithstring(actionReference,1,"<Keyboard>/z");
        private void ReBindingWithstring(InputAction action,int bindingIndex, string path)
        {
            action.ApplyBindingOverride(bindingIndex, path);
            UpdatePorperty(index);
        }
        /// <summary>
        /// 刷新binding名
        /// </summary>
        /// <param name="index"></param>
        private void UpdatePorperty(int index)
        {
            bindingLabel =
                actionReference.action.GetBindingDisplayString(index);
        }
    }
}