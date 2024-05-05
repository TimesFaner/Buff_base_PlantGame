using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MultiPlayer : NetworkBehaviour
{
  void HandleMove()
  {
    float x = Input.GetAxis("Horizontal");
    float y = Input.GetAxis("Vertical");
    Vector3 movement = new Vector3(x, y,0)*0.3f;
    transform.position = transform.position + movement;
  }

  private void Update()
  {
    if (isLocalPlayer)
    {
      HandleMove();
    }
  }
}
