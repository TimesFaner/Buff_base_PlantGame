using UnityEngine;

namespace Mirror.Examples.TanksCoop
{
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkTransformUnreliable))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : NetworkBehaviour
    {
        public enum GroundState : byte
        {
            Jumping,
            Falling,
            Grounded
        }

        [Header("Avatar Components")] public CharacterController characterController;

        [Header("Movement")] [Range(1, 20)] public float moveSpeedMultiplier = 8f;

        [Header("Turning")] [Range(1f, 200f)] public float maxTurnSpeed = 100f;

        [Range(.5f, 5f)] public float turnDelta = 3f;

        [Header("Jumping")] [Range(0.1f, 1f)] public float initialJumpSpeed = 0.2f;

        [Range(1f, 10f)] public float maxJumpSpeed = 5f;

        [Range(0.1f, 1f)] public float jumpDelta = 0.2f;

        [Header("Diagnostics - Do Not Modify")]
        public GroundState groundState = GroundState.Grounded;

        [Range(-1f, 1f)] public float horizontal;

        [Range(-1f, 1f)] public float vertical;

        [Range(-200f, 200f)] public float turnSpeed;

        [Range(-10f, 10f)] public float jumpSpeed;

        [Range(-1.5f, 1.5f)] public float animVelocity;

        [Range(-1.5f, 1.5f)] public float animRotation;

        public Vector3Int velocity;
        public Vector3 direction;

        public TankController tankController;

        // we dont want this object to move once you have control of tank
        public bool canControlPlayer = true;

        private void Update()
        {
            if (!characterController.enabled)
                return;

            // we need to detect player exiting vehichle, so input detection is not blockeed
            HandleInput();

            if (!canControlPlayer)
                return;

            HandleTurning();
            HandleJumping();
            HandleMove();

            // Reset ground state
            if (characterController.isGrounded)
                groundState = GroundState.Grounded;
            else if (groundState != GroundState.Jumping)
                groundState = GroundState.Falling;

            // Diagnostic velocity...FloorToInt for display purposes
            velocity = Vector3Int.FloorToInt(characterController.velocity);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isOwned) return;
            //Debug.Log(name + "- OnTriggerEnter - " + other.name);

            if (other.name == "TankTrigger")
                // dont update tank variable if we're in one
                if (canControlPlayer)
                    tankController = other.transform.root.GetComponent<TankController>();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isOwned) return;
            //Debug.Log(name + "- OnTriggerExit - " + other.name);

            if (other.name == "TankTrigger")
                if (tankController)
                    if (tankController.objectOwner != netIdentity)
                        tankController = null;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            // Override CharacterController default values
            characterController.enabled = false;
            characterController.skinWidth = 0.02f;
            characterController.minMoveDistance = 0f;

            GetComponent<Rigidbody>().isKinematic = true;

            enabled = false;
        }

        public override void OnStartAuthority()
        {
            characterController.enabled = true;
            enabled = true;
        }

        public override void OnStopAuthority()
        {
            enabled = false;
            characterController.enabled = false;
        }

        // TODO: Turning works while airborne...feature?
        private void HandleTurning()
        {
            // Q and E cancel each other out, reducing the turn to zero.
            if (Input.GetKey(KeyCode.Q))
                turnSpeed = Mathf.MoveTowards(turnSpeed, -maxTurnSpeed, turnDelta);
            if (Input.GetKey(KeyCode.E))
                turnSpeed = Mathf.MoveTowards(turnSpeed, maxTurnSpeed, turnDelta);

            // If both pressed, reduce turning speed toward zero.
            if (Input.GetKey(KeyCode.Q) && Input.GetKey(KeyCode.E))
                turnSpeed = Mathf.MoveTowards(turnSpeed, 0, turnDelta);

            // If neither pressed, reduce turning speed toward zero.
            if (!Input.GetKey(KeyCode.Q) && !Input.GetKey(KeyCode.E))
                turnSpeed = Mathf.MoveTowards(turnSpeed, 0, turnDelta);

            transform.Rotate(0f, turnSpeed * Time.deltaTime, 0f);
        }

        private void HandleJumping()
        {
            // Handle variable force jumping.
            // Jump starts with initial power on takeoff, and jumps higher / longer
            // as player holds spacebar. Jump power is increased by a diminishing amout
            // every frame until it reaches maxJumpSpeed, or player releases the spacebar,
            // and then changes to the falling state until it gets grounded.
            if (groundState != GroundState.Falling && Input.GetKey(KeyCode.Space))
            {
                if (groundState != GroundState.Jumping)
                {
                    // Start jump at initial power.
                    groundState = GroundState.Jumping;
                    jumpSpeed = initialJumpSpeed;
                }
                else
                    // Jumping has already started...increase power toward maxJumpSpeed over time.
                {
                    jumpSpeed = Mathf.MoveTowards(jumpSpeed, maxJumpSpeed, jumpDelta);
                }

                // If power has reached maxJumpSpeed, change to falling until grounded.
                // This prevents over-applying jump power while already in the air.
                if (jumpSpeed == maxJumpSpeed)
                    groundState = GroundState.Falling;
            }
            else if (groundState != GroundState.Grounded)
            {
                // handles running off a cliff and/or player released Spacebar.
                groundState = GroundState.Falling;
                jumpSpeed = Mathf.Min(jumpSpeed, maxJumpSpeed);
                jumpSpeed += Physics.gravity.y * Time.deltaTime;
            }
            else
            {
                jumpSpeed = Physics.gravity.y * Time.deltaTime;
            }
        }

        // TODO: Directional input works while airborne...feature?
        private void HandleMove()
        {
            // Capture inputs
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            // Create initial direction vector without jumpSpeed (y-axis).
            direction = new Vector3(horizontal, 0f, vertical);

            // Clamp so diagonal strafing isn't a speed advantage.
            direction = Vector3.ClampMagnitude(direction, 1f);

            // Transforms direction from local space to world space.
            direction = transform.TransformDirection(direction);

            // Multiply for desired ground speed.
            direction *= moveSpeedMultiplier;

            // Add jumpSpeed to direction as last step.
            direction.y = jumpSpeed;

            // Finally move the character.
            characterController.Move(direction * Time.deltaTime);
        }

        private void HandleInput()
        {
            if (tankController)
            {
                // if no one owns trigger object
                if (canControlPlayer && tankController.objectOwner == null)
                {
                    if (Input.GetKeyDown(KeyCode.E)) CmdAssignAuthority(tankController.netIdentity);
                }
                else
                {
                    // if we do own
                    if (Input.GetKeyDown(KeyCode.Q)) CmdRemoveAuthority(tankController.netIdentity);
                }

                // alternatively we could tell everyone to locally do this and disable NetworkTransform
                // it would be more optimal but requires a lil more code
                if (tankController.objectOwner == netIdentity)
                    transform.position = tankController.seatPosition.position;
            }
        }

        [Command]
        public void CmdAssignAuthority(NetworkIdentity _networkIdentity)
        {
            // Debug.Log("Mirror Object owner set to: " + this.netIdentity);

            tankController = _networkIdentity.GetComponent<TankController>();

            // so we dont assign it to same person again
            if (tankController.objectOwner != netIdentity)
            {
                // commands are a good place to do additional validation/cheat checks, but these are left out for simplicity here
                _networkIdentity.RemoveClientAuthority();
                _networkIdentity.AssignClientAuthority(connectionToClient);

                tankController.objectOwner = netIdentity;
            }
        }

        [Command]
        public void CmdRemoveAuthority(NetworkIdentity _networkIdentity)
        {
            //Debug.Log("Mirror Object owner removed from: " + connectionToClient.identity);

            tankController = _networkIdentity.GetComponent<TankController>();

            // double check command is sent to remove auth, from owner of object 
            if (tankController.objectOwner != null && tankController.objectOwner == netIdentity)
            {
                _networkIdentity.RemoveClientAuthority();

                tankController.objectOwner = null;
            }
        }
    }
}