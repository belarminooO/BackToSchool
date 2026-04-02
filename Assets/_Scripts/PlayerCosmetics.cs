using UnityEngine;
using PurrNet;

public class PlayerCosmetics : NetworkBehaviour
{
    [Header("Cosmetic Models")]
    public GameObject crownModel;
    public GameObject clownNoseModel;

    public SyncVar<bool> hasCrown = new SyncVar<bool>(false);
    public SyncVar<bool> hasClownNose = new SyncVar<bool>(false);

    protected override void OnSpawned()
    {
        hasCrown.onChanged += UpdateCrown;
        hasClownNose.onChanged += UpdateClownNose;

        UpdateCrown(hasCrown.value);
        UpdateClownNose(hasClownNose.value);
    }

    private void UpdateCrown(bool active)
    {
        if (crownModel != null) 
            crownModel.SetActive(active);
    }

    private void UpdateClownNose(bool active)
    {
        if (clownNoseModel != null) 
            clownNoseModel.SetActive(active);
    }

    public void SetCosmeticsServerAuth(bool crown, bool nose)
    {
        if (!isServer) 
            return;

        hasCrown.value = crown;
        hasClownNose.value = nose;
    }
}
