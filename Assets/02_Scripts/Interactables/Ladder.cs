using UnityEngine;
using Nexush.Player;

namespace Nexush.Interactables
{
    /// <summary>
    /// 사다리 영역을 정의하는 스크립트입니다.
    /// Trigger Collider를 통해 플레이어의 진입을 감지합니다.
    /// </summary>
    public class Ladder : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<PlayerController>(out var player))
            {
                player.SetNearLadder(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<PlayerController>(out var player))
            {
                player.SetNearLadder(false);
            }
        }
    }
}
