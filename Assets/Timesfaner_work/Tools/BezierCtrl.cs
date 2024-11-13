using Buff_system_timesfaner.Uility;
using Mirror;
using Timesfaner_work.BaseManager;
using UnityEngine;

namespace Timesfaner_work.Tools
{
    public class BezierCtrl : MonoBehaviour
    {        
        public Vector3 sourcePosition;
        public Vector3 targetPosition;
        public BeizerObj beizerObj;
        [ShowInInspector]public float r = 5f;
        /// <summary>
        /// 有参
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="beizerobj"></param>
        public void Fire(Vector3 start ,Vector3 end,BeizerObj beizerobj )
        {
            
        }
        /// <summary>
        /// 无参默认
        /// </summary>
        [Button]public void Fire( )
        {
            BeizerObj tempobj = Pool.Instance.GetObj("bb", sourcePosition).GetComponent<BeizerObj>();
            StartCoroutine(tempobj.FollowThePath(sourcePosition, targetPosition, GetRandom(r)));
        }

        public Vector3 GetRandom(float a)
        {
            return sourcePosition+ Random.insideUnitSphere * a;
        }
    }
}