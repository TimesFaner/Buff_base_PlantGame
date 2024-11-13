using System.Collections.Generic;
using UnityEngine;

namespace Timesfaner_work.BaseManager
{
    public class AssetBundleMgr : SingletonMonoBase<AssetBundleMgr>
    {
        //主包
        private AssetBundle mainAB = null;

        //配置文件
        private AssetBundleManifest manifest = null;
        private Dictionary<string, AssetBundle> abDic = new Dictionary<string, AssetBundle>();

        /// <summary>
        /// ab包路径
        /// </summary>
        private string pathUrl
        {
            get { return Application.streamingAssetsPath + "/"; }
        }

        private string MainABName
        {
            get
            {
#if UNITY_IOS
        return  "IOS";
#elif UNITY_ANDROID
        return "Android";
#else
                return "PC";
#endif
            }
        }

        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="resName"></param>
        public Object LoadRes(string abName, string resName)
        {
            //ab包加载
            if (mainAB == null)
            {
                mainAB =
                    AssetBundle.LoadFromFile(pathUrl + MainABName);
                manifest
                    = mainAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            }

            // 获取依赖包信息
            AssetBundle ab = null;
            string[] strs = manifest.GetDirectDependencies(abName);
            for (int i = 0; i < strs.Length; i++)
            {
                if (!abDic.ContainsKey(strs[i]))
                {
                    ab = AssetBundle.LoadFromFile(pathUrl + strs[i]);
                    abDic.Add(strs[i], ab);
                }
            }
//目标包
            if (!abDic.ContainsKey(abName))
            {
                ab = AssetBundle.LoadFromFile(pathUrl + abName);
                abDic.Add(abName, ab);
            }
            // 加载
           return abDic[abName].LoadAsset(resName);
        }
        /// <summary>
        /// 类型指定同步加载
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="resName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object LoadRes(string abName, string resName,System.Type type)
        {
            #region LoadAB

            //ab包加载
            if (mainAB == null)
            {
                mainAB =
                    AssetBundle.LoadFromFile(pathUrl + MainABName);
                manifest
                    = mainAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            }

            // 获取依赖包信息
            AssetBundle ab = null;
            string[] strs = manifest.GetDirectDependencies(abName);
            for (int i = 0; i < strs.Length; i++)
            {
                if (!abDic.ContainsKey(strs[i]))
                {
                    ab = AssetBundle.LoadFromFile(pathUrl + strs[i]);
                    abDic.Add(strs[i], ab);
                }
            }
//目标包
            if (!abDic.ContainsKey(abName))
            {
                ab = AssetBundle.LoadFromFile(pathUrl + abName);
                abDic.Add(abName, ab);
            }

            #endregion
            // 加载
            return abDic[abName].LoadAsset(resName,type);
        }
        /// <summary>
        /// 泛型指定同步加载
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="resName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Object LoadRes<T>(string abName, string resName) where T:Object
        {
            #region LoadAB

            //ab包加载
            if (mainAB == null)
            {
                mainAB =
                    AssetBundle.LoadFromFile(pathUrl + MainABName);
                manifest
                    = mainAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            }

            // 获取依赖包信息
            AssetBundle ab = null;
            string[] strs = manifest.GetDirectDependencies(abName);
            for (int i = 0; i < strs.Length; i++)
            {
                if (!abDic.ContainsKey(strs[i]))
                {
                    ab = AssetBundle.LoadFromFile(pathUrl + strs[i]);
                    abDic.Add(strs[i], ab);
                }
            }
//目标包
            if (!abDic.ContainsKey(abName))
            {
                ab = AssetBundle.LoadFromFile(pathUrl + abName);
                abDic.Add(abName, ab);
            }

            #endregion
            // 加载
            return abDic[abName].LoadAsset<T>(resName) ;
        }
        
        /// <summary>
        /// 卸载单个资源包
        /// </summary>
        /// <param name="abName"></param>
        public void UnloadRes(string abName)
        {
            if (abDic.ContainsKey(abName))
            {
                abDic[abName].Unload(false);
                abDic.Remove(abName);
            }
        }

        /// <summary>
        /// 全卸载
        /// </summary>
        public void ClearAB()
        {
            AssetBundle.UnloadAllAssetBundles(false);
            abDic.Clear();
            mainAB = null;
            manifest = null;
        }
    }
}