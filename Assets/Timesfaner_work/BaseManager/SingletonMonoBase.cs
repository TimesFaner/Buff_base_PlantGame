using System.Collections;
using System.Net.Security;
using UnityEngine;

namespace Timesfaner_work.BaseManager
{
    /// <summary>
    ///     继承mono的 自动单例基类、不new不用挂载场景物体中
    /// </summary>
    public  class SingletonMonoBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        // 场景可能会多挂载导致唯一性出问题、或者切换场景时出问题
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    var obj = new GameObject();
                    obj.name = typeof(T).ToString();
                    instance = obj.AddComponent<T>();
                   // DontDestroyOnLoad(obj);
                }

                return instance;
            }
        }

// // 记得·base.AWAKE
         protected virtual void Awake()
         {
             instance = this as T;
         }


        
    }
}