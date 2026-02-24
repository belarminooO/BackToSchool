using UnityEngine;
using TMPro;
using PurrNet;
using Steamworks;

public class PlayerNameTagAlt : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Canvas nameCanvas;

    public SyncVar<string> steamName = new SyncVar<string>(ownerAuth: true);

    void Start()
    {
        if (nameCanvas != null && Camera.main != null)
        {
            nameCanvas.worldCamera = Camera.main;
        }

        steamName.onChanged += (newName) => { nameText.text = newName; };

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
        }
        else
        {
            if (steamName.value != null)
            {
                nameText.text = steamName.value;
                Debug.Log("Setting name tag for " + steamName.value);

            }
        }
    }

    private void LateUpdate()
    {
        if (nameCanvas != null && Camera.main != null)
        {
            nameCanvas.transform.rotation = Camera.main.transform.rotation;
        }

        if (!isOwner && nameText.text == "" && steamName.value != "")
        {
            nameText.text = steamName.value;
        }
    }
}
