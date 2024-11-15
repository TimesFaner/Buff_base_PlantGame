using UnityEngine;

namespace Mirror.Examples.NetworkRoom
{
    public class PlayerScore : NetworkBehaviour
    {
        [SyncVar] public int index;

        [SyncVar] public uint score;

        private void OnGUI()
        {
            GUI.Box(new Rect(10f + index * 110, 10f, 100f, 25f), $"P{index}: {score:0000000}");
        }
    }
}