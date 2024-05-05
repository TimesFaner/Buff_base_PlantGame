using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Remake
{
    public interface IState
    {

        public void Enter();
    
        public void Handleinput();
    
        public void Update();
    
        public void PhysicsUpdate();
    
        public void Exit();
    }
}
