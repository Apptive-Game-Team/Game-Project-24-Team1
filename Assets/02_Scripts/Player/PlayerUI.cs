using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Crosshair Settings")]
    public Image crosshairImage;
    public bool showCrosshair = true;

    private void Start()
    {
        if (crosshairImage != null)
        {
            crosshairImage.enabled = showCrosshair;
        }
    }

    private void Update()
    {
        // 씬에서 조준점은 항상 켜져 있도록 설정 (요청사항)
        if (crosshairImage != null && !crosshairImage.enabled && showCrosshair)
        {
            crosshairImage.enabled = true;
        }
    }
}
