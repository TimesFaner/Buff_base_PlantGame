using Mirror;
using UnityEngine;

public class MultiPlayer : NetworkBehaviour
{
    private void Update()
    {
        if (isLocalPlayer) HandleMove();
    }

    private void HandleMove()
    {
        var x = Input.GetAxis("Horizontal");
        var y = Input.GetAxis("Vertical");
        var movement = new Vector3(x, y, 0) * 0.3f;
        transform.position = transform.position + movement;
    }
}