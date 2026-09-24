using MetaVoiceChat;
using PurrNet;
using Steamworks;
using TMPro;
using UnityEngine;

public class PlayerNameTag : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Canvas nameCanvas;
    [SerializeField] private MetaVc metaVc;

    public SyncVar<string> steamName = new SyncVar<string>(ownerAuth: true);

    protected override void OnSpawned()
    {
        steamName.onChanged += OnNameChanged;

        if (nameText != null)
        {
            nameText.enableWordWrapping = false;
        }

        if (metaVc != null)
        {
            metaVc.isSpeaking.OnValueChanged += UpdateNameTagColor;
            UpdateNameTagColor(metaVc.isSpeaking.Value);
        }

        if (isOwner)
        {

            if (nameCanvas != null)
            {
                nameCanvas.gameObject.SetActive(false);
            }

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
            if (nameCanvas != null)
            {
                nameCanvas.gameObject.SetActive(true);
            }

            if (!string.IsNullOrEmpty(steamName.value))
                OnNameChanged(steamName.value);
        }

    }

    protected override void OnDespawned()
    {
        steamName.onChanged -= OnNameChanged;

        if (metaVc != null)
        {
            metaVc.isSpeaking.OnValueChanged -= UpdateNameTagColor;
        }
    }

    private void OnNameChanged(string newName)
    {
        if (nameText != null)
        {
            nameText.text = newName;
            Debug.Log("Setting name tag to " + newName);
        }
    }

    private void UpdateNameTagColor(bool isSpeaking)
    {
        if (nameText != null)
        {
            nameText.color = isSpeaking ? Color.green : Color.white;
        }
    }

    private void LateUpdate()
    {
        if (!isOwner && nameText != null && nameText.text == "" && !string.IsNullOrEmpty(steamName.value))
        {
            nameText.text = steamName.value;
        }

        if (nameCanvas != null && Camera.main != null)
        {
            nameCanvas.transform.rotation = Quaternion.LookRotation(nameCanvas.transform.position - Camera.main.transform.position);
        }
    }

}
