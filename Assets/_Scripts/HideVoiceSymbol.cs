using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HideVoiceSymbol : MonoBehaviour
{

    [Header("Audio Settings")]
    public AudioSource audioSource; 
    public TextMeshProUGUI text;
    public float threshold = 0.1f;

    private float[] samples = new float[256];
    void Update()
    {
        float volume = GetVolume();

        if (volume > threshold)
        {
            ChangeColor(Color.green);
        }
        else
        {
            ChangeColor(Color.white);
        }
    }

    float GetVolume()
    {
        audioSource.GetOutputData(samples, 0);
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        return Mathf.Sqrt(sum / samples.Length);
    }

    void ChangeColor(Color color)
    {
        var mesh = text.mesh;
        var vertices = mesh.vertices;
        var colors = new Color32[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            colors[i] = color;
        }

        mesh.colors32 = colors;
        text.canvasRenderer.SetMesh(mesh);
    }
}
