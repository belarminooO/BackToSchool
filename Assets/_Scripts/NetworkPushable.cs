using PurrNet;
using UnityEngine;
public class NetworkPushable : NetworkBehaviour
{
    [ServerRpc(requireOwnership: false)]
    public void PushServerRpc(Vector3 direction, float power)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.AddForce(direction * power, ForceMode.Impulse);    
    }
}