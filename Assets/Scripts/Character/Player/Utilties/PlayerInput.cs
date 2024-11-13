using Timesfaner_work.Action_system;
using Timesfaner_work.BaseManager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Remake
{
    public class PlayerInput : SingletonMonoBase<PlayerInput>
    {
        public PlayerInputAction playerInputAction { get; private set; }
        public PlayerInputAction.PlayerActions playerActions { get; private set; }

        private void Awake()
        {
            playerInputAction = new PlayerInputAction();
            playerActions = playerInputAction.Player;
        }

        private void OnEnable()
        {
            playerInputAction.Enable();
            playerActions.Enable();
        }

        private void OnDisable()
        {
            playerInputAction.Disable();
            playerActions.Disable();
        }
    }
}