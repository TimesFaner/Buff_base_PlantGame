using UnityEngine;

namespace Remake
{
    public class PlayerMoveState : IState
    {
//速度·
        protected float baseSpeed = 1f;
        protected Vector2 movementInput;
        public float speedModifier = All_Data.PlayerWalkSpeedModify;
        public MoveStateMachine stateMachine;

        public PlayerMoveState(MoveStateMachine moveStateMachine)
        {
            stateMachine = moveStateMachine;
        }

        #region Istate Methods

        public virtual void Enter()
        {
            Debug.Log("State" + GetType().Name);
        }

        public virtual void Handleinput()
        {
            ReadMovementInput();
        }


        public virtual void Update()
        {
            Move();
        }


        public virtual void PhysicsUpdate()
        {
        }

        public virtual void Exit()
        {
        }

        #endregion

        #region Main Methods

        public void ReadMovementInput()
        {
            movementInput = stateMachine.Player.Input.playerActions.Movement.ReadValue<Vector2>();
        }

        private void Move()
        {
            if (movementInput == Vector2.zero || speedModifier == 0f) return;

            var movementDirection = GetMoveMentDirection();

            stateMachine.Player.rb.MovePosition(movementInput * movementDirection + stateMachine.Player.rb.position);
            stateMachine.Player.rb.velocity = new Vector3(0f, 0f, 0f);
            Debug.Log("xy:" + movementInput + "v" + stateMachine.Player.rb.velocity);
            //TOLOOK delttime
        }

        #endregion

        #region reusable Methods

        private float GetMovementSpeed()
        {
            return baseSpeed * speedModifier;
        }

        private Vector2 GetMoveMentDirection()
        {
            var movementSpeed = GetMovementSpeed();
            return new Vector2(movementSpeed, movementSpeed);
        }

        #endregion
    }
}