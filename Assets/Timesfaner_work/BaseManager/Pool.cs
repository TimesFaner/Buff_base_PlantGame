using System.Collections.Generic;
using UnityEngine;

namespace Timesfaner_work.BaseManager
{
    /// <summary>
    ///     对象池、用就拿，不用就还
    /// </summary>
    public class Pool : LazySingletonBase<Pool>
    {
        //管理所有容器的池子
        private readonly Dictionary<string, Stack<GameObject>> poolDic = new();

        //对应物品的记录在场值
        public Dictionary<string, int> thecount = new();

        //首先得是单例模式，避免违反唯一性
        private Pool()
        {
        }

        /// <summary>
        ///     取出对象
        /// </summary>
        /// <param name="_objname"></param>
        /// <returns></returns>
        public GameObject GetObj(string _objname, Vector3 _thePos)
        {
            // Debug.Log("TOget");
            GameObject theObj;
            // 情况1：
            if (poolDic.ContainsKey(_objname) && poolDic[_objname].Count > 0)
            {
                // 容器里弹出
                theObj = poolDic[_objname].Pop();
                theObj.transform.position = _thePos;
                //设置可见
                theObj.SetActive(true);
                //对应go的在场数量
                thecount[_objname] += 1;
                // Debug.Log(thecount + "数量：" + thecount[_objname]);
            }
            //情况2 需要创造
            else
            {
                theObj = Object.Instantiate(Resources.Load<GameObject>(_objname));
                theObj.transform.position = _thePos;
                // GameObject.Instantiate(Resources.Load<GameObject>(_objname));
                //对应go的数量
                if (!thecount.ContainsKey(_objname))
                {
                    thecount.Add(_objname, 1);
                    // Debug.Log(thecount + "数量：" + thecount[_objname]);
                }
                else
                {
                    //对应go的在场数量
                    thecount[_objname] += 1;
                    // Debug.Log(thecount + "数量：" + thecount[_objname]);
                }
            }

            return theObj;
        }

        /// <summary>
        /// 配合assetbundle的生成
        /// </summary>
        /// <param name="_objname"></param>
        /// <param name="_thePos"></param>
        /// <param name="_abundlename"></param>
        /// <returns></returns>
        public GameObject GetObj(string _objname, Vector3 _thePos, string _abundlename)
        {
            // Debug.Log("TOget");
            GameObject theObj;
            // 情况1：
            if (poolDic.ContainsKey(_objname) && poolDic[_objname].Count > 0)
            {
                // 容器里弹出
                theObj = poolDic[_objname].Pop();
                theObj.transform.position = _thePos;
                //设置可见
                theObj.SetActive(true);
                //对应go的在场数量
                thecount[_objname] += 1;
                // Debug.Log(thecount + "数量：" + thecount[_objname]);
            }
            //情况2 需要创造
            else
            {
                theObj = Object.Instantiate((GameObject)AssetBundleMgr.Instance.LoadRes(_abundlename, _objname));
                theObj.transform.position = _thePos;
                // GameObject.Instantiate(Resources.Load<GameObject>(_objname));
                //对应go的数量
                if (!thecount.ContainsKey(_objname))
                {
                    thecount.Add(_objname, 1);
                    // Debug.Log(thecount + "数量：" + thecount[_objname]);
                }
                else
                {
                    //对应go的在场数量
                    thecount[_objname] += 1;
                    // Debug.Log(thecount + "数量：" + thecount[_objname]);
                }
            }

            return theObj;
        }
        
        /// <summary>
        ///     放入东西时、采用set-false
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_obj"></param>
        public void PushObj(string _name, GameObject _obj)
        {
            //激活对象
            _obj.SetActive(false); //当然可以远离、类似于帕鲁副本）
            //有对应容器
            if (poolDic.ContainsKey(_name))
            {
                //存入_obj
                // Debug.Log("push -1");
                poolDic[_name].Push(_obj);
                // Debug.Log("stack have" + poolDic[_name].Count);
            }
            else
            {
                // Debug.Log("PUsh -2" + _name);
                //先造容器
                poolDic.Add(_name, new Stack<GameObject>());
                //再放东西
                poolDic[_name].Push(_obj);
                // Debug.Log("stack have" + poolDic[_name].Count);
            }

            //对应物品在场数量减一
            thecount[_name] -= 1;
        }

        /// <summary>
        ///     清除对象池子
        /// </summary>
        public void ClrPool()
        {
            poolDic.Clear();
            thecount.Clear();
        }

        /// <summary>
        ///     ji对应物品数量加一
        /// </summary>
        /// <param name="_name"></param>
        public void AddtheNameCount(string _name)
        {
            thecount[_name] += 1;
        }
    }
}