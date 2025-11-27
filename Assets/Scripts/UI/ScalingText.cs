using UnityEngine;
using TMPro;

public class ScalingText : MonoBehaviour
{
    [SerializeField]
    TMP_Text tmpText;

    public float scaleSpeed;
    public float scaleAmount;

    Vector2 baseScale;
    void Start()
    {
        baseScale = tmpText.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        float scaleValue = 1f + Mathf.Sin(Time.time * scaleSpeed) * scaleAmount;

        tmpText.transform.localScale = baseScale * scaleValue;
    }
}
