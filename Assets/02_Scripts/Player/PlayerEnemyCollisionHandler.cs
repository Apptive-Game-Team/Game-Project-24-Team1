using UnityEngine;

namespace MushOut.Player
{
    /// <summary>
    /// 플레이어와 적(Enemy)의 충돌을 감지하여 GameManager에 게임 오버 이벤트를 전달하는 컴포넌트입니다.
    /// PlayerController로부터 게임 이벤트 관련 책임을 분리하여 단일 책임 원칙을 따릅니다.
    /// </summary>
    public class PlayerEnemyCollisionHandler : MonoBehaviour
    {
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            CheckEnemyCollision(hit.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            CheckEnemyCollision(collision.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckEnemyCollision(other.gameObject);
        }

        /// <summary>
        /// 충돌한 오브젝트가 적인지 확인하고, 적이라면 GameManager에 충돌 사실을 알립니다.
        /// </summary>
        /// <param name="otherObj">충돌한 GameObject</param>
        private void CheckEnemyCollision(GameObject otherObj)
        {
            // 플레이어 자신의 태그와 레이어가 일치하는지 먼저 확인
            if (gameObject.CompareTag("Player") && gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                // 상대방 오브젝트가 enemy 태그인지 확인
                if (otherObj.CompareTag("Enemy"))
                {
                    int layer = otherObj.layer;
                    // 상대방 오브젝트가 enemy 또는 enemyheavy 레이어인지 확인
                    if (layer == LayerMask.NameToLayer("Enemy") || layer == LayerMask.NameToLayer("EnemyHeavy"))
                    {
                        if (MushOut.Core.GameManager.Instance != null &&
                            !MushOut.Core.GameManager.Instance.CrashedByEnemy)
                        {
                            Debug.Log($"[PlayerEnemyCollisionHandler] 적과 충돌! GameManager의 CrashedByEnemy를 true로 설정합니다. (적: {otherObj.name})");
                            MushOut.Core.GameManager.Instance.CrashedByEnemy = true;
                        }
                    }
                }
            }
        }
    }
}
