using UnityEngine;

namespace Mirror.Examples.NetworkRoom
{
    internal class Spawner
    {
        [ServerCallback]
        internal static void InitialSpawn()
        {
            for (var i = 0; i < 10; i++)
                SpawnReward();
        }

        [ServerCallback]
        internal static void SpawnReward()
        {
            var spawnPosition = new Vector3(Random.Range(-19, 20), 1, Random.Range(-19, 20));
            NetworkServer.Spawn(Object.Instantiate(NetworkRoomManagerExt.singleton.rewardPrefab, spawnPosition,
                Quaternion.identity));
        }
    }
}