using System;
using System.Collections;
using System.Collections.Generic;
using Manager;
using UnityEngine;

public class All_Data : SingletonMonoBase<All_Data>
{
   private void Awake() {
  
   }
   //组件
   private void Start()
   {
      
   }
   //状态
   public static bool isGaming = false;
   //property属性
   public static float PlayerHealth = 100f;
   public static float PlayerWalkSpeedModify = 0.3f;
   public static float EnemySpeedModify = 2f;
   public static float EnemyStopDistance = 1f;
   public static float EnemyAttackCd = 1f;
   
}
