using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Remake
{
    public class PlayerInput : MonoBehaviour
    {
        public PlayerInputAction playerInputAction { get; private set; }
        public PlayerInputAction.PlayerActions playerActions{ get; private set; }

        private void Awake()
        {
            playerInputAction = new PlayerInputAction();
            playerActions = playerInputAction.Player;

        }

        private void OnEnable()
        {
            playerActions.Enable();
            
        }

        private void OnDisable()
        {
            playerActions.Enable();
        }
    }  
}

