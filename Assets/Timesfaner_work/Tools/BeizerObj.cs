using System;
using System.Collections;
using UnityEngine;

namespace Timesfaner_work.Tools
{
    public class BeizerObj : MonoBehaviour
    {
        public float speed =5f;
/// <summary>
/// 贝塞尔曲线的路径
/// </summary>
/// <param name="startPosition"></param>
/// <param name="endPosition"></param>
/// <param name="controlPoint"></param>
/// <returns></returns>
       public IEnumerator FollowThePath(Vector3 startPosition, Vector3 endPosition, Vector3 controlPoint)
        {
            for (float i = 0; i <= 1; i+=Time.deltaTime)
            {
                Vector3 p1 = Vector3.Lerp(startPosition, controlPoint, i);
                Vector3 p2 = Vector3.Lerp(controlPoint, endPosition, i);
                Vector3 p = Vector3.Lerp(p1, p2, i);
                yield return MoveToEnd(p);
            }
            yield return MovejustToEnd(endPosition);
            
        }

       private IEnumerator MoveToEnd(Vector3 p)
        {
            while (Vector3.Distance(p,transform.position)>0.1f)
            {
                Vector3 dir = p - transform.position;
                transform.up = dir;
                transform.position = Vector3.MoveTowards(transform.position, p, speed * Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator MovejustToEnd(Vector3 p)
        {
            while (Vector3.Distance(p,transform.position)>0.1f)
            {
                Vector3 dir = p - transform.position;
                transform.up = dir;
                
                transform.position = Vector3.MoveTowards(transform.position, p, speed * Time.deltaTime);
                yield return null;
                
            }
        }
        
    }
}