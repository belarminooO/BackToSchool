using UnityEngine;
using UnityEngine.UI;

public class ChangeColor : MonoBehaviour
{
    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    public void ChangeToRed()
    {
        if (image != null)
        {
            image.color = Color.red;
        }
    }

    public void ChangeToGreen()
    {
        if (image != null)
        {
            image.color = Color.green;
        }
    }
}
