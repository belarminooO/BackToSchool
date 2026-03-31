using UnityEngine;

public class SkinSelection : MonoBehaviour
{
    public static SkinSelection Instance { get; private set; }
    public int SkinIndex { get; private set; } = 0;

    private void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; DontDestroyOnLoad(gameObject); 
        }
        else Destroy(gameObject);
    }

    public void Set(int index) => SkinIndex = index;
}
