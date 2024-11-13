namespace Timesfaner_work.BaseManager
{/// <summary>
 /// 饿汉单例父类
 /// </summary>
    public abstract class EagerSingletonBase<T> where T : class, new()    {
        // 构造函数私有化，外部不能new
        protected EagerSingletonBase() { }
        private static T instance = new T();

        public static T Instance
        {
            get
            {
                return instance;
            }
        }
        
 }
}