using Mirror;
using UnityEngine;

public class Move : MonoBehaviour
{
    private NetworkIdentity networkIdentity = null;

    private void Awake()
    {
        this.networkIdentity = this.GetComponent<NetworkIdentity>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!this.networkIdentity.isLocalPlayer) return;

        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            direction += Vector3.up;
        }
        if (Input.GetKey(KeyCode.S))
        {
            direction += Vector3.down;
        }
        if (Input.GetKey(KeyCode.A))
        {
            direction += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            direction += Vector3.right;
        }

        direction.Normalize();
        this.transform.position = this.transform.position + direction * 0.1f;
    }
}
