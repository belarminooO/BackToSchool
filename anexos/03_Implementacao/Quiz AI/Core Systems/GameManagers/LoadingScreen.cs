using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private GameObject loading1;

    public static LoadingScreen Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (loading1 != null)
        {
            loading1.SetActive(true);
        }
    }

    public bool IsLoading => loading1 != null && loading1.activeSelf;

    public void Hide()
    {
        if (loading1 != null)
        {
            loading1.SetActive(false);
        }
    }
}
