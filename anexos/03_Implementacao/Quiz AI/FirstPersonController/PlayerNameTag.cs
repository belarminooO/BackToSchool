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

        RegisterVoiceChatEvents();
        SetCanvasVisible(!isOwner);

        if (isOwner)
        {
            steamName.value = GetLocalPlayerName();
        }
        else
        {
            if (!string.IsNullOrEmpty(steamName.value))
            {
                OnNameChanged(steamName.value);
            }
        }
    }

    protected override void OnDespawned()
    {
        steamName.onChanged -= OnNameChanged;
        UnregisterVoiceChatEvents();
    }

    private void RegisterVoiceChatEvents()
    {
        if (metaVc == null)
        {
            return;
        }

        metaVc.isSpeaking.OnValueChanged += UpdateNameTagColor;
        UpdateNameTagColor(metaVc.isSpeaking.Value);
    }

    private void UnregisterVoiceChatEvents()
    {
        if (metaVc != null)
        {
            metaVc.isSpeaking.OnValueChanged -= UpdateNameTagColor;
        }
    }

    private string GetLocalPlayerName()
    {
        try
        {
            return SteamFriends.GetPersonaName();
        }
        catch
        {
            return "Player " + Random.Range(100, 999);
        }
    }

    private void SetCanvasVisible(bool isVisible)
    {
        if (nameCanvas != null)
        {
            nameCanvas.gameObject.SetActive(isVisible);
        }
    }

    private void OnNameChanged(string newName)
    {
        if (nameText != null)
        {
            nameText.text = newName;
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
            OnNameChanged(steamName.value);
        }

        if (nameCanvas != null && Camera.main != null)
        {
            nameCanvas.transform.rotation = Quaternion.LookRotation(nameCanvas.transform.position - Camera.main.transform.position);
        }
    }
}
