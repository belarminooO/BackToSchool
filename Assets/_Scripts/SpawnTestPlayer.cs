using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SpawnTestPlayer : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] public GameObject playerPrefab;
    [SerializeField] public Transform spawnPoint;
    [SerializeField] public string testNickname = "TestBot";

    [Header("Input")]
    [SerializeField] public InputActionReference spawnAction;

    private GameObject spawnedPlayer;

    private void OnEnable()
    {
        if (spawnAction != null)
        {
            spawnAction.action.Enable();
            spawnAction.action.performed += OnSpawnPerformed;
        }
    }

    private void OnDisable()
    {
        if (spawnAction != null)
        {
            spawnAction.action.performed -= OnSpawnPerformed;
            spawnAction.action.Disable();
        }
    }

    private void OnSpawnPerformed(InputAction.CallbackContext context)
    {
        SpawnFakePlayer();
    }

    public void SpawnFakePlayer()
    {
        if (playerPrefab != null && spawnPoint != null)
        {
        } else {
            Debug.LogError("Player prefab or spawn point is not assigned.");
        }

        if (spawnedPlayer != null)
            Destroy(spawnedPlayer);

        spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        Transform nametagTransform = spawnedPlayer.transform.Find("Root/Canvas/Nametag");
        if (nametagTransform != null)
        {
            TMPro.TMP_Text nameText = nametagTransform.GetComponent<TMPro.TMP_Text>();
            if (nameText != null) {
                nameText.text = testNickname;
            }
        }
        else
        {
            Debug.LogWarning("[SpawnTestPlayer] 'Nametag' not found at Root/Canvas/Nametag.");
        }
    }

}
