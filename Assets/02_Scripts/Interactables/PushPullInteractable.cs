using UnityEngine;

namespace MushOut.Interactables
{
    public enum PushPullMovementType
    {
        Free,               // 좌우 이동 포함 자유롭게 걷기 가능
        ForwardBackwardOnly // 앞뒤(오브젝트 쪽으로 밀고, 반대쪽으로 당기기)로만 이동 가능
    }

    /// <summary>
    /// 플레이어가 밀거나 당길 수 있는 상호작용 오브젝트입니다.
    /// PlayerController와 연동하여 동작합니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PushPullInteractable : MonoBehaviour
    {
        [Header("Push/Pull Settings")]
        [Tooltip("오브젝트를 밀 수 있는지 여부")]
        public bool Pushable = true;

        [Tooltip("오브젝트를 당길 수 있는지 여부")]
        public bool Pullable = true;

        [Tooltip("밀고 당길 때의 이동 제약 방식")]
        public PushPullMovementType movementType = PushPullMovementType.Free;

        private Rigidbody _rigidbody;
        private Transform _playerTransform;
        private Vector3 _grabOffset;
        
        public bool isGrabbed = false;
        
        [HideInInspector]
        public Collider objectCollider;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            objectCollider = GetComponent<Collider>();
            
            // 물리적인 충돌을 위해 Kinematic이 아니어야 함
            _rigidbody.isKinematic = false;
            // 회전을 방지하려면 Constraints를 설정하는 것이 좋습니다.
            // _rigidbody.constraints = RigidbodyConstraints.FreezeRotation; 
        }

        /// <summary>
        /// 플레이어가 그랩을 시작할 때 호출됩니다.
        /// </summary>
        public void StartGrab(Transform player)
        {
            isGrabbed = true;
            _playerTransform = player;
            // 잡았을 때의 초기 상대 위치(오프셋) 저장
            _grabOffset = transform.position - player.position;
        }

        /// <summary>
        /// 플레이어가 그랩을 해제할 때 호출됩니다.
        /// </summary>
        public void EndGrab()
        {
            isGrabbed = false;
            _playerTransform = null;
            _rigidbody.linearVelocity = Vector3.zero; // 이동 정지
        }

        private void FixedUpdate()
        {
            if (isGrabbed && _playerTransform != null)
            {
                // 목표 위치: 플레이어의 현재 위치 + 잡았을 때의 상대적 거리
                Vector3 targetPos = _playerTransform.position + _grabOffset;
                
                // 목표 위치로 향하는 방향 벡터
                Vector3 diff = targetPos - transform.position;
                
                // 부드럽게(그러나 빠르게) 목표 위치를 따라가도록 스프링 속도 적용
                // 플레이어가 충돌을 무시하더라도, 큐브가 벽에 닿으면 물리 엔진이 이 속도를 상쇄시켜 멈춤
                _rigidbody.linearVelocity = new Vector3(diff.x * 20f, _rigidbody.linearVelocity.y, diff.z * 20f);
            }
        }

        /// <summary>
        /// 특정 방향(밀기/당기기)이 현재 설정(Pushable/Pullable)에 허용되는지 확인합니다.
        /// </summary>
        public bool CanMoveInDirection(Vector3 moveDir, Vector3 toObjectDir)
        {
            // moveDir: 플레이어가 이동하려는 방향
            // toObjectDir: 플레이어에서 오브젝트를 향하는 방향
            
            float dot = Vector3.Dot(moveDir.normalized, toObjectDir.normalized);

            // dot > 0 이면 밀기(Push), dot < 0 이면 당기기(Pull)로 간주
            if (dot > 0.1f && !Pushable) return false;
            if (dot < -0.1f && !Pullable) return false;

            return true;
        }
    }
}
