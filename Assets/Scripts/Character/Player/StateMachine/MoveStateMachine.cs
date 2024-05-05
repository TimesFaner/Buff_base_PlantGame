using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Remake
{
    public class MoveStateMachine : StateMachine
    {
        public Player Player;
        public PlayerIdleState IdleState { get; }
        public PlayerWalkState WalkState { get; }
        public PlayerSpritState SpritState { get;}



        public MoveStateMachine(Player player)
        {
            Player = player;
            IdleState = new PlayerIdleState(this);
            WalkState = new PlayerWalkState(this);
            SpritState = new PlayerSpritState(this);
        }
    }
    
}

