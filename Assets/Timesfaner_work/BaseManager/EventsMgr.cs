using System;
using System.Collections;
using System.Collections.Generic;
using Timesfaner_work.BaseManager;
using UnityEngine;
using UnityEngine.EventSystems;
/// <summary>
/// 事件中心 *TimesFaner*
/// </summary>
public class EventsMgr : SingletonMonoBase<EventsMgr>
{
  private Dictionary<string, EventHandler> eventDic = new Dictionary<string, EventHandler>();
// 注册
  public void RegisterEvent(string _eventname,EventHandler _eventhandler)
  {
    if (eventDic.ContainsKey(_eventname))
    {
      eventDic[_eventname] += _eventhandler;
    }
    else
    {
      eventDic.Add(_eventname,_eventhandler);
    }
  }
  /// <summary>
  /// 取消注册事件
  /// </summary>
  /// <param name="_eventname"></param>
  /// <param name="_eventhandler"></param>
  public void UnRegisterEvent(string _eventname,EventHandler _eventhandler) 
  {
    if (eventDic.ContainsKey(_eventname))
    {
      eventDic[_eventname] -= _eventhandler;
    }
    else
    {
      Debug.LogError("EventMgr:UnRegisterEvent->没有该事件："+_eventname);
    }
  }
/// <summary>
/// 无参发布
/// </summary>
/// <param name="_eventname"></param>
/// <param name="_objects"></param>
  public void SendEvent(string _eventname, params object[] _objects)
  {
    if (eventDic.ContainsKey(_eventname))
    {
      eventDic[_eventname]?.Invoke(_objects,EventArgs.Empty);
    }
  }
  /// <summary>
  /// 带参数的发布
  /// </summary>
  /// <param name="_eventname"></param>
  /// <param name="_args"></param>
  /// <param name="_objects"></param>
  public void SendEvent(string _eventname,  EventArgs _args,params object[] _objects)
  {
    if (eventDic.ContainsKey(_eventname))
    {
      eventDic[_eventname]?.Invoke(_objects,_args);
    }
  }
}
