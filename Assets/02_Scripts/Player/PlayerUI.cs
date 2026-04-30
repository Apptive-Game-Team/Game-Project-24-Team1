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

        // 💡 Update에서 매 프레임 체크하는 불필요한 로직을 제거했습니다.
        // 상태 변경이 필요할 경우 외부에서 UpdateCrosshairVisibility를 호출하거나
        // 프로퍼티를 통해 제어하는 것이 성능상 유리합니다.
    }
}
