using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace Nexush.Player
{
    /// <summary>
    /// 플레이어 화면에 표시되는 UI 요소(조준점 등)를 관리하는 클래스입니다.
    /// </summary>
    public class PlayerUI : MonoBehaviour
    {
        [Header("조준점 설정")]
        [Tooltip("중앙에 표시될 조준점 이미지입니다.")]
        [FormerlySerializedAs("crosshairImage")]
        [SerializeField] private Image img_crosshair;

        [Tooltip("조준점 표시 여부입니다.")]
        [SerializeField] private bool showCrosshair = true;

        private void Start()
        {
            UpdateCrosshairVisibility();
        }

        /// <summary>
        /// 설정에 따라 조준점의 활성화 상태를 업데이트합니다.
        /// </summary>
        public void UpdateCrosshairVisibility()
        {
            if (img_crosshair != null)
            {
                img_crosshair.enabled = showCrosshair;
            }
        }
    }
}
