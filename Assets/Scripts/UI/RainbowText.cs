using TMPro;
using UnityEngine;

public class RainbowText : MonoBehaviour
{
    [SerializeField]
    TMP_Text tmpText;
    public float colorChangeSpeed = 1f;
    void Update()
    {
        float h = Mathf.Repeat(Time.time * colorChangeSpeed, 1f);
        Color color = Color.HSVToRGB(h, 1f, 1f);

        tmpText.color = color;
    }
}
