// 플레이어 위치 복구를 담당하는 파일.
// 게임 시작할 때의 처음 위치를 기억해두고, 다시시도 버튼을 누르면 그 위치에서 다시 시작하게 만든다.

using MushOut.Player;
using MushOut.SavePoint;
using UnityEngine;

namespace MushOut.Core
{
    public partial class GameOverManager
    {
        private void CachePlayerSpawn()
        {
            if (PlayerEnemyCollisionHandler.TryGetInitialSpawn(out Vector3 initialPosition, out Quaternion initialRotation))
            {
                _playerSpawnPosition = initialPosition;
                _playerSpawnRotation = initialRotation;
            }

            _playerObject = GameManager.Instance != null && GameManager.Instance.PlayerTransform != null
                ? GameManager.Instance.PlayerTransform.gameObject
                : GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");

            if (_playerObject != null && !PlayerEnemyCollisionHandler.TryGetInitialSpawn(out _, out _))
            {
                _playerSpawnPosition = _playerObject.transform.position;
                _playerSpawnRotation = _playerObject.transform.rotation;
            }
        }

        private void ResetPlayerToSpawnPosition()
        {
            if (_playerObject == null)
            {
                CachePlayerSpawn();
            }

            if (_playerObject == null)
                return;

            CharacterController controller = _playerObject.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            Vector3 respawnPosition = _playerSpawnPosition;
            Quaternion respawnRotation = _playerSpawnRotation;

            if (SavePointTrigger.TryGetActiveRespawn(out Vector3 savePointPosition, out Quaternion savePointRotation))
            {
                respawnPosition = savePointPosition;
                respawnRotation = savePointRotation;
            }

            _playerObject.transform.SetPositionAndRotation(respawnPosition, respawnRotation);

            // 부활 직후 중력 가속도나 물리 충격이 이어져 이상 작동하는 현상을 방지하기 위해 속도를 초기화합니다.
            PlayerMotor motor = _playerObject.GetComponent<PlayerMotor>();
            if (motor != null)
            {
                motor.VerticalVelocity = 0f;
                motor.ExternalVelocity = Vector3.zero;
            }

            if (controller != null)
            {
                controller.enabled = true;
            }
        }
    }
}
