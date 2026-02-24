using UnityEngine;
using TMPro;
using PurrNet;
using Steamworks;

public class PlayerNameTag : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Canvas nameCanvas;

    public SyncVar<string> steamName = new SyncVar<string>(ownerAuth: true);
    protected override void OnSpawned()
    {
        if (isOwner)
        {
            try
            {
                steamName.value = SteamFriends.GetPersonaName();
            }
            catch
            {
                steamName.value = "Player " + UnityEngine.Random.Range(100, 999);
            }
        } else {

            nameText.text = steamName.value;
            Debug.Log("Setting name tag for " + steamName.value);
        }

    }

    private void Update()
    {
        if (nameCanvas != null && Camera.main != null)
        {
            nameCanvas.transform.LookAt(Camera.main.transform);
            nameCanvas.transform.Rotate(0, 180, 0);
        }
    }
}
