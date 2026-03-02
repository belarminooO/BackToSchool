using UnityEngine;

public class TestPlayerNametag : MonoBehaviour
{
    private void LateUpdate()
    {
        if (Camera.main == null) return;

        transform.rotation = Quaternion.LookRotation(
            transform.position - Camera.main.transform.position
        );
    }
}
