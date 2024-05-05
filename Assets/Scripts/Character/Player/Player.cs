using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Mirror;
using UnityEngine;

namespace Remake
{
    [RequireComponent(typeof(PlayerInput))]
    public class Player : NetworkBehaviour
    {
        private MoveStateMachine moveStateMachine;
        public PlayerInput Input { get; private set; }
        public Rigidbody2D rb;
        

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().Follow = transform;
            //Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().LookAt = transform;
            
            
            All_Data.isGaming = true;//TODO
        }
        
        
        public void Awake()
        {
            moveStateMachine = new MoveStateMachine(this);
            Input = GetComponent<PlayerInput>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            moveStateMachine.ChangeState(moveStateMachine.IdleState);
            
            
        }

        private void Update()
        {
            moveStateMachine.HandleInput();
            moveStateMachine.Update();
        }

     

        private void FixedUpdate()
        {
            moveStateMachine.PhysicsUpdate();
        }
    }
}

