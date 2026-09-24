using UnityEngine;
using TMPro;
using PurrNet;

[RequireComponent(typeof(AnswerButtonManager))]
public class StationOwnerNameplate : NetworkBehaviour
{
    [Header("Nameplate UI")]
    [SerializeField] private Canvas nameplateCanvas;
    [SerializeField] private TMP_Text ownerNameText;
    [Tooltip("Shown when the table has no owner assigned. Leave empty to hide the plate.")]
    [SerializeField] private string unassignedText = "";

    private AnswerButtonManager _station;
    private PlayerNameTag _trackedNameTag;

    private void Awake()
    {
        _station = GetComponent<AnswerButtonManager>();
    }

    protected override void OnSpawned()
    {
        if (_station != null)
            _station.ownerPlayer.onChanged += OnOwnerChanged;

        RefreshOwner(_station != null ? _station.ownerPlayer.value : null);
    }

    protected override void OnDespawned()
    {
        if (_station != null)
            _station.ownerPlayer.onChanged -= OnOwnerChanged;
        UntrackNameTag();
    }

    private void OnOwnerChanged(NetworkIdentity newOwner)
    {
        RefreshOwner(newOwner);
    }

    private void RefreshOwner(NetworkIdentity owner)
    {
        UntrackNameTag();

        if (owner == null)
        {
            SetText(unassignedText);
            SetVisible(!string.IsNullOrEmpty(unassignedText));
            return;
        }

        _trackedNameTag = owner.GetComponentInChildren<PlayerNameTag>();
        if (_trackedNameTag != null)
        {
            _trackedNameTag.steamName.onChanged += OnNameChanged;
            SetText(_trackedNameTag.steamName.value);
        }
        else
        {
            SetText(owner.gameObject.name);
        }
        SetVisible(true);
    }

    private void OnNameChanged(string newName) => SetText(newName);

    private void UntrackNameTag()
    {
        if (_trackedNameTag != null)
        {
            _trackedNameTag.steamName.onChanged -= OnNameChanged;
            _trackedNameTag = null;
        }
    }

    private void SetText(string value)
    {
        if (ownerNameText != null)
            ownerNameText.text = value;
    }

    private void SetVisible(bool visible)
    {
        if (nameplateCanvas != null)
            nameplateCanvas.gameObject.SetActive(visible);
    }

    private void LateUpdate()
    {

        if (nameplateCanvas != null && Camera.main != null)
        {
            nameplateCanvas.transform.rotation = Quaternion.LookRotation(
                nameplateCanvas.transform.position - Camera.main.transform.position);
        }
    }
}
