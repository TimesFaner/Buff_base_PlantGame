using UnityEngine;

namespace Mirror.Examples.AdditiveScenes
{
    // This script demonstrates the NetworkAnimator and how to leverage
    // the built-in observers system to track players.
    // Note that all ProximityCheckers should be restricted to the Player layer.
    public class ShootingTankBehaviour : NetworkBehaviour
    {
        [SyncVar] public Quaternion rotation;

        [Range(0, 1)] public float turnSpeed = 0.1f;

        private NetworkAnimator networkAnimator;

        [ServerCallback]
        private void Start()
        {
            networkAnimator = GetComponent<NetworkAnimator>();
        }

        private void Update()
        {
            if (isServer && netIdentity.observers.Count > 0)
                ShootNearestPlayer();

            if (isClient)
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, turnSpeed);
        }

        [Server]
        private void ShootNearestPlayer()
        {
            GameObject target = null;
            var distance = 100f;

            foreach (var networkConnection in netIdentity.observers.Values)
            {
                var tempTarget = networkConnection.identity.gameObject;
                var tempDistance = Vector3.Distance(tempTarget.transform.position, transform.position);

                if (target == null || distance > tempDistance)
                {
                    target = tempTarget;
                    distance = tempDistance;
                }
            }

            if (target != null)
            {
                transform.LookAt(target.transform.position + Vector3.down);
                rotation = transform.rotation;
                networkAnimator.SetTrigger("Fire");
            }
        }
    }
}