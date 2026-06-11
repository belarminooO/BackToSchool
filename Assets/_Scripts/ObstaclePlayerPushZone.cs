using UnityEngine;

[DisallowMultipleComponent]
public class ObstaclePlayerPushZone : MonoBehaviour
{
    [SerializeField] private float pushForce = 12f;
    [SerializeField] private float pushCooldown = 0.15f;

    private float _nextPushTime;
    private Rigidbody _rigidbody;

    private void Awake()
    {
        EnsureRigidBody();
        EnsurePushTriggers();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryPush(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryPush(other);
    }

    private void EnsureRigidBody()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }

        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void EnsurePushTriggers()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider sourceCollider = colliders[i];
            if (sourceCollider == null || sourceCollider.isTrigger)
                continue;

            Transform triggerRoot = transform.Find(sourceCollider.name + "_PushTrigger");
            GameObject triggerObject;
            if (triggerRoot == null)
            {
                triggerObject = new GameObject(sourceCollider.name + "_PushTrigger");
                triggerObject.transform.SetParent(sourceCollider.transform, false);
                triggerObject.transform.localPosition = Vector3.zero;
                triggerObject.transform.localRotation = Quaternion.identity;
                triggerObject.transform.localScale = Vector3.one;
            }
            else
            {
                triggerObject = triggerRoot.gameObject;
            }

            if (triggerObject.GetComponent<Collider>() != null)
                continue;

            CopyColliderAsTrigger(sourceCollider, triggerObject);
        }
    }

    private static void CopyColliderAsTrigger(Collider sourceCollider, GameObject targetObject)
    {
        switch (sourceCollider)
        {
            case BoxCollider box:
            {
                BoxCollider copy = targetObject.AddComponent<BoxCollider>();
                copy.center = box.center;
                copy.size = box.size;
                copy.isTrigger = true;
                break;
            }
            case SphereCollider sphere:
            {
                SphereCollider copy = targetObject.AddComponent<SphereCollider>();
                copy.center = sphere.center;
                copy.radius = sphere.radius;
                copy.isTrigger = true;
                break;
            }
            case CapsuleCollider capsule:
            {
                CapsuleCollider copy = targetObject.AddComponent<CapsuleCollider>();
                copy.center = capsule.center;
                copy.radius = capsule.radius;
                copy.height = capsule.height;
                copy.direction = capsule.direction;
                copy.isTrigger = true;
                break;
            }
            case MeshCollider mesh:
            {
                MeshCollider copy = targetObject.AddComponent<MeshCollider>();
                copy.sharedMesh = mesh.sharedMesh;
                copy.convex = true;
                copy.isTrigger = true;
                break;
            }
            default:
            {
                BoxCollider fallback = targetObject.AddComponent<BoxCollider>();
                fallback.isTrigger = true;
                fallback.center = Vector3.zero;
                fallback.size = Vector3.one;
                break;
            }
        }
    }

    private void TryPush(Collider other)
    {
        if (Time.time < _nextPushTime || other == null)
            return;

        FirstPersonNetworkState playerState = other.GetComponentInParent<FirstPersonNetworkState>();
        if (playerState == null || !playerState.isOwner)
            return;

        Vector3 awayFromObstacle = playerState.transform.position - transform.position;
        awayFromObstacle = Vector3.ProjectOnPlane(awayFromObstacle, Vector3.up);

        if (awayFromObstacle.sqrMagnitude < 0.01f)
            awayFromObstacle = playerState.transform.forward;

        awayFromObstacle.y = 0.2f;
        playerState.PushServerRpc(awayFromObstacle.normalized * pushForce);

        _nextPushTime = Time.time + pushCooldown;
    }
}
