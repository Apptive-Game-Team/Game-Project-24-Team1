using UnityEngine;
using System.Threading.Tasks;

namespace MushOut.Environment
{
    /// <summary>
    /// 게임 오브젝트를 켜고 끄거나, 지정된 시간(t초) 후에 켜고 끄는 기능을 제공하는 스크립트입니다.
    /// Task를 사용하여 오브젝트가 비활성화된 상태에서도 t초 후 켜는 기능이 정상 작동하도록 구현되었습니다.
    /// </summary>
    public class ObjectOnOff : MonoBehaviour
    {
        /// <summary>
        /// 이 스크립트가 붙은 오브젝트를 즉시 활성화합니다.
        /// </summary>
        public void TurnOn()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 이 스크립트가 붙은 오브젝트를 즉시 비활성화합니다.
        /// </summary>
        public void TurnOff()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// t초 후에 이 스크립트가 붙은 오브젝트를 활성화합니다.
        /// </summary>
        /// <param name="t">지연 시간(초)</param>
        public async void TurnOnAfterDelay(float t)
        {
            // t초(밀리초로 변환) 대기
            await Task.Delay(Mathf.RoundToInt(t * 1000));

            // 대기하는 동안 오브젝트가 파괴(Destroy)되지 않았다면 실행
            if (this != null && gameObject != null)
            {
                gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// t초 후에 이 스크립트가 붙은 오브젝트를 비활성화합니다.
        /// </summary>
        /// <param name="t">지연 시간(초)</param>
        public async void TurnOffAfterDelay(float t)
        {
            // t초(밀리초로 변환) 대기
            await Task.Delay(Mathf.RoundToInt(t * 1000));

            // 대기하는 동안 오브젝트가 파괴(Destroy)되지 않았다면 실행
            if (this != null && gameObject != null)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
