using UnityEngine;

namespace Mirror.Examples.AdditiveScenes
{
    // AdditiveNetworkManager, in OnStartServer, instantiates the prefab only on the server.
    // It never exists for clients (other than host client if there is one).
    // The prefab has a Sphere Collider with isTrigger = true.
    // These OnTrigger events only run on the server and will only send a message to the
    // client that entered the Zone to load the subscene assigned to the subscene property.
    public class ZoneHandler : MonoBehaviour
    {
        [Scene] [Tooltip("Assign the sub-scene to load for this zone")]
        public string subScene;

        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            // ignore collisions with non-Player objects
            if (!other.CompareTag("Player")) return;

            if (other.TryGetComponent(out NetworkIdentity networkIdentity))
            {
                var message = new SceneMessage { sceneName = subScene, sceneOperation = SceneOperation.LoadAdditive };
                networkIdentity.connectionToClient.Send(message);
            }
        }

        [ServerCallback]
        private void OnTriggerExit(Collider other)
        {
            // ignore collisions with non-Player objects
            if (!other.CompareTag("Player")) return;

            if (other.TryGetComponent(out NetworkIdentity networkIdentity))
            {
                var message = new SceneMessage { sceneName = subScene, sceneOperation = SceneOperation.UnloadAdditive };
                networkIdentity.connectionToClient.Send(message);
            }
        }
    }
}