using System;
using System.Collections.Generic;
using System.Linq;
using Buff_system_timesfaner.TBuff_base;
using UnityEngine;

namespace Buff_system_timesfaner.TBuffHandler
{
    public class Buffhandler : MonoBehaviour, IBuffHandler
    {
        private Action addAction;

        //linked增删性能优于列表
        public LinkedList<Buffinfo> Bufflist = new();
        private Action forOnBuffDestroy;
        private Action forOnBuffStart;
        private Action removeAction;
        public List<Buffinfo> Removelist = new();

        #region mono

        private void Update()
        {
            BufftickandRemove();
        }

        #endregion

        public void BufftickandRemove()
        {
            foreach (var buffinfo in Bufflist)
            {
                if (buffinfo.buffData.OnTick is not null)
                {
                    if (buffinfo.tickTimer < 0)
                    {
                        buffinfo.buffData.OnTick.CallandInvoke(buffinfo);
                        buffinfo.tickTimer = buffinfo.buffData.TickMaxTime;
                    }
                    else
                    {
                        buffinfo.tickTimer -= Time.deltaTime;
                    }
                }

                //永久性
                if (buffinfo.buffData.IsForever)
                {
                }
                else
                {
                    if (buffinfo.durationTimer < 0)
                        Removelist.Add(buffinfo);
                    else
                        buffinfo.durationTimer -= Time.deltaTime;
                }
            }

            foreach (var buff in Removelist) RemoveBuff(buff);
        }

        #region 增删查改

        public Buffinfo FindBuff(int buffId)
        {
            return Bufflist.FirstOrDefault(buffInfo => buffId == buffInfo.buffData.Id);
        }

        /// <summary>
        ///     按照优先级降序,大的在前
        /// </summary>
        private void SortBuff()
        {
            var result = new LinkedList<Buffinfo>();

            foreach (var node in Bufflist)
            {
                var lln = result.First;
                while (true)
                    if (lln == null)
                    {
                        result.AddLast(node);
                        break;
                    }
                    else if (node.buffData.Priority >= lln.Value.buffData.Priority)
                    {
                        result.AddBefore(lln, node);
                        break;
                    }
                    else
                    {
                        lln = lln.Next;
                    }
            }

            Bufflist = result;
        }

        public void AddBuff(Buffinfo buffinfo)
        {
            var findbuffinfo = FindBuff(buffinfo.buffData.Id);
            if (findbuffinfo != null)
            {
                Debug.Log("is");
                if (findbuffinfo.curLevel < buffinfo.buffData.MaxLevel) findbuffinfo.curLevel++;

                if (findbuffinfo.curLevel > 0)
                    switch (findbuffinfo.buffData.AddandSubType)
                    {
                        case AddType.addsub:
                        {
                            findbuffinfo.durationTimer += findbuffinfo.buffData.Duration;
                            break;
                        }
                        case AddType.replace:
                        {
                            findbuffinfo.durationTimer = findbuffinfo.buffData.Duration;
                            break;
                        }
                        case AddType.indepent:
                            //TODO  
                            Bufflist.AddLast(findbuffinfo);
                            break;
                        case AddType.keep:
                            break;
                    }

                findbuffinfo.buffData.OnCreate.CallandInvoke(findbuffinfo);
            }
            //无此buff时
            else
            {
                Debug.Log("isno");
                buffinfo.durationTimer = buffinfo.buffData.Duration;
                buffinfo.curLevel = 1;
                Bufflist.AddLast(buffinfo);
                buffinfo.buffData.OnCreate.CallandInvoke(buffinfo);
                SortBuff();
            }
        }

        public void RemoveBuff(Buffinfo buffinfo)
        {
            buffinfo.buffData.OnRemove.CallandInvoke(buffinfo);
            switch (buffinfo.buffData.AddandSubType)
            {
                case AddType.addsub:
                {
                    //直接删除
                    buffinfo.buffData.OnDestroy.CallandInvoke(buffinfo);
                    Removelist.Add(buffinfo);
                    break;
                }
                case AddType.replace:
                {
                    //减少层次，时间重置
                    buffinfo.curLevel--;
                    buffinfo.durationTimer = buffinfo.buffData.Duration;
                    if (buffinfo.curLevel <= 0)
                    {
                        buffinfo.buffData.OnDestroy.CallandInvoke(buffinfo);
                        Bufflist.Remove(buffinfo);
                    }

                    break;
                }
                case AddType.indepent:
                    //TODO  
                    buffinfo.buffData.OnDestroy.CallandInvoke(buffinfo);
                    Bufflist.Remove(buffinfo);
                    break;
                case AddType.keep:
                {
                    //减少层次，时间保持
                    buffinfo.curLevel--;
                    if (buffinfo.curLevel <= 0)
                    {
                        buffinfo.buffData.OnDestroy.CallandInvoke(buffinfo);
                        Bufflist.Remove(buffinfo);
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region 注册其他事件

        public void RegisterOnAddBuff(Action act)
        {
            addAction += act;
        }

        public void RemoveOnAddBuff(Action act)
        {
            addAction -= act;
        }

        public void RegisterOnRemoveBuff(Action act)
        {
            removeAction += act;
        }

        public void RemoveOnRemoveBuff(Action act)
        {
            removeAction -= act;
        }

        #endregion
    }
}