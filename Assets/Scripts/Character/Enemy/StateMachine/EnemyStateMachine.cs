using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Remake
{
    public class EnemyStateMachine:StateMachine
    {
        public EnemyCtrl Enemy;
        public EnemyAttackState _enemyAttackState { get; }

        public EnemyStateMachine(EnemyCtrl enemy)
        {
            Enemy = enemy;
            _enemyAttackState = new EnemyAttackState(this);
        }
    }
}