/****************************************************************************
 * Copyright (c) 2015 - 2022 liangxiegame UNDER MIT License
 *
 * http://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace QFramework
{
    internal class MonoUpdateActionExecutor : MonoBehaviour, IActionExecutor
    {
        private readonly Dictionary<IAction, Action<IActionController>> mExecutingActions = new();

        private readonly List<KeyValuePair<IAction, Action<IActionController>>> mPrepareExecutionActions = new();

        private readonly List<IAction> mToActionRemove = new();

        private void Update()
        {
            if (mPrepareExecutionActions.Count > 0)
            {
                foreach (var prepareExecutionAction in mPrepareExecutionActions)
                    if (mExecutingActions.ContainsKey(prepareExecutionAction.Key))
                        mExecutingActions[prepareExecutionAction.Key] = prepareExecutionAction.Value;
                    else
                        mExecutingActions.Add(prepareExecutionAction.Key, prepareExecutionAction.Value);

                mPrepareExecutionActions.Clear();
            }

            foreach (var actionAndFinishCallback in mExecutingActions)
                if (this.UpdateAction(actionAndFinishCallback.Key, Time.deltaTime, actionAndFinishCallback.Value))
                    mToActionRemove.Add(actionAndFinishCallback.Key);

            if (mToActionRemove.Count > 0)
            {
                foreach (var action in mToActionRemove) mExecutingActions.Remove(action);

                mToActionRemove.Clear();
            }
        }

        public void Execute(IAction action, Action<IActionController> onFinish = null)
        {
            if (action.Status == ActionStatus.Finished) action.Reset();
            if (this.UpdateAction(action, 0, onFinish)) return;

            mPrepareExecutionActions.Add(new KeyValuePair<IAction, Action<IActionController>>(action, onFinish));
        }
    }

    public static class MonoUpdateActionExecutorExtension
    {
        public static IAction ExecuteByUpdate<T>(this T self, IAction action, Action<IActionController> onFinish = null)
            where T : MonoBehaviour
        {
            if (action.Status == ActionStatus.Finished) action.Reset();
            self.gameObject.GetOrAddComponent<MonoUpdateActionExecutor>().Execute(action, onFinish);
            return action;
        }
    }
}