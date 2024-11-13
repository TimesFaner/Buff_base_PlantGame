using System;
using System.Reflection;

namespace Timesfaner_work.BaseManager
{
    /// <summary>
    ///     脱离mono的单例基类，直接继承避免冗余
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class LazySingletonBase<T> where T : class
    {
//单例要求：
//唯一性：限制new
//构造函数在外部可以随意调用时会破坏其安全性，所以要实现私有无参构造函数
//多线程访问时
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    var type = typeof(T);
                    //反射拿类型 构造器
                    var info = type.GetConstructor
                    ( //约束条件；绑定对象，参数类型，参数修饰符
                        BindingFlags.Instance | BindingFlags.NonPublic, //成员私有
                        null, // 无绑定对象
                        Type.EmptyTypes, // 无参
                        null //无参数修饰符
                    );
                    instance = info.Invoke(null) as T;
                }

                return instance;
            }
        }
    }
}