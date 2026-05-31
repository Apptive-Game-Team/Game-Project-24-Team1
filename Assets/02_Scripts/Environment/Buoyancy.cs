using UnityEngine;
using MushOut.Player;

namespace MushOut.Environment
{
    [System.Serializable]
    public struct GrabBuoyancyPenalty
    {
        [Tooltip("비교할 태그 이름입니다. (예: Box, Enemy)")]
        public string targetTag;
        
        [Tooltip("비교할 레이어 이름입니다. 비워두면 태그만 검사합니다. (예: EnemyHeavy)")]
        public string targetLayer;
        
        [Tooltip("해당 오브젝트를 잡았을 때 덮어씌울 부력 값입니다. (0에 가까울수록 쉽게 가라앉음)")]
        public float overrideBuoyancy;
    }

    [RequireComponent(typeof(Collider))]
    public class Buoyancy : MonoBehaviour
    {
        [Header("Buoyancy Settings")]
        [Tooltip("기본 부력 계수 (값이 클수록 물 밖으로 띄워 올리는 힘이 강해집니다)")]
        [SerializeField] private float buoyancyPower = 4.0f;

        [Tooltip("물에 잠기는 비율 (0 = 수면 위로 완전히 뜸, 1 = 물에 완전히 잠김). 기본값은 0.7(70% 잠김)입니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float submergeRatio = 0.7f;

        [Header("Grab Penalties")]
        [Tooltip("플레이어가 물체를 잡고 있을 때 적용할 부력 값 목록입니다. 일치하는 첫 번째 규칙이 적용됩니다.")]
        [SerializeField] private GrabBuoyancyPenalty[] grabPenalties = new GrabBuoyancyPenalty[]
        {
            new GrabBuoyancyPenalty { targetTag = "Box", targetLayer = "", overrideBuoyancy = 0.5f },
            new GrabBuoyancyPenalty { targetTag = "Enemy", targetLayer = "Enemy", overrideBuoyancy = 0.8f },
            new GrabBuoyancyPenalty { targetTag = "Enemy", targetLayer = "EnemyHeavy", overrideBuoyancy = 0.4f }
        };

        private Collider _waterCollider;

        private void Awake()
        {
            _waterCollider = GetComponent<Collider>();
            if (_waterCollider == null || !_waterCollider.isTrigger)
            {
                Debug.LogWarning("[Buoyancy] 물 오브젝트에 Trigger Collider가 필요하며, Is Trigger가 체크되어야 합니다!");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (_waterCollider == null) return;

            // 수면의 Y 좌표 계산 (콜라이더가 회전되었을 경우 bounds.max.y는 AABB의 최상단이므로 오차가 발생함)
            float waterSurfaceY = _waterCollider.bounds.max.y;
            
            if (_waterCollider is BoxCollider box)
            {
                // 플레이어의 현재 위치를 물 오브젝트의 로컬 좌표계로 변환
                Vector3 localPos = box.transform.InverseTransformPoint(other.transform.position);
                // 로컬 좌표계에서 Y값을 박스의 최상단(윗면)으로 고정
                localPos.y = box.center.y + box.size.y * 0.5f;
                // 다시 월드 좌표로 변환하여 정확한 수면의 Y값을 얻음
                waterSurfaceY = box.transform.TransformPoint(localPos).y;
            }
            else
            {
                // BoxCollider가 아닌 경우 (Sphere, Capsule, Mesh 등) Raycast를 사용하여 가장 위쪽 표면을 찾음
                Vector3 rayOrigin = other.transform.position;
                rayOrigin.y = _waterCollider.bounds.max.y + 1.0f;
                Ray ray = new Ray(rayOrigin, Vector3.down);
                
                if (_waterCollider.Raycast(ray, out RaycastHit hit, _waterCollider.bounds.size.y + 2.0f))
                {
                    waterSurfaceY = hit.point.y;
                }
            }
            
            bool isPlayer = other.TryGetComponent<PlayerController>(out var player);
            PlayerMotor playerMotor = isPlayer ? other.GetComponent<PlayerMotor>() : null;
            Rigidbody rb = !isPlayer ? other.GetComponent<Rigidbody>() : null;

            float objHeight = 0f;
            float bottomY = other.transform.position.y; // 기본적으로 피벗 위치를 하단으로 가정

            if (isPlayer)
            {
                var cc = other.GetComponent<CharacterController>();
                if (cc != null)
                {
                    objHeight = cc.height;
                    // 플레이어의 pivot은 항상 발끝(하단)에 위치합니다.
                    bottomY = other.transform.position.y;
                }
            }
            else if (rb != null)
            {
                // 일반 오브젝트는 Bounds를 통해 정확한 높이와 하단 좌표를 구합니다.
                objHeight = other.bounds.size.y;
                bottomY = other.bounds.min.y;
            }

            // --- [추가] 잡고 있는 물체에 따른 부력 페널티 계산 (Inspector 설정 기반) ---
            float currentBuoyancyPower = buoyancyPower;
            GameObject grabbedObj = null;
            
            if (isPlayer)
            {
                var interactor = other.GetComponent<PlayerInteractor>();
                if (interactor != null && interactor.GrabbedObject != null)
                {
                    // 플레이어의 경우, 자신이 잡고 있는 물체를 기준으로 페널티 적용
                    grabbedObj = interactor.GrabbedObject.gameObject;
                }
            }
            else if (rb != null)
            {
                var pushPull = other.GetComponent<MushOut.Interactables.PushPullInteractable>();
                if (pushPull != null && pushPull.isGrabbed)
                {
                    // 일반 오브젝트의 경우, 자신이 누군가에게 잡혀있다면 스스로에게 페널티 적용
                    grabbedObj = other.gameObject;
                }
            }

            if (grabbedObj != null)
            {
                string grabbedTag = grabbedObj.tag;
                string grabbedLayerName = LayerMask.LayerToName(grabbedObj.layer);

                foreach (var penalty in grabPenalties)
                {
                    bool tagMatch = string.IsNullOrEmpty(penalty.targetTag) || grabbedTag == penalty.targetTag;
                    bool layerMatch = string.IsNullOrEmpty(penalty.targetLayer) || grabbedLayerName == penalty.targetLayer;

                    if (tagMatch && layerMatch)
                    {
                        currentBuoyancyPower = penalty.overrideBuoyancy;
                        break; // 조건에 맞는 첫 번째 규칙의 부력을 적용하고 종료
                    }
                }
            }
            // ----------------------------------------------------

            // 목표 하단(바닥) Y 좌표: 전체 높이의 지정된 비율(submergeRatio)만큼 수면 아래에 있도록 설정
            float targetBottomY = waterSurfaceY - (objHeight * submergeRatio);

            // 현재 하단 좌표가 목표 좌표보다 아래에 있는지 확인 (수면 아래로 깊이 들어감)
            if (bottomY < targetBottomY)
            {
                // 얼마나 더 올라가야 하는지 계산
                float diff = targetBottomY - bottomY;

                // 목표 높이까지 올라가는 상승 속도 (최대값은 currentBuoyancyPower로 제한)
                // 거리가 가까워질수록 diff가 작아져서 상승 속도도 자연스럽게 줄어듦
                float targetRiseVelocity = Mathf.Min(diff / Time.fixedDeltaTime, currentBuoyancyPower);
                
                // 물 속에서 위로 밀어올리는 가속도 (부력 파워에 비례)
                float upwardAcceleration = currentBuoyancyPower * 5f;

                if (isPlayer && playerMotor != null)
                {
                    // PlayerMotor 쪽에 목표 높이와 부력 값을 전달하여 Update()에서 부드럽게 처리하도록 위임
                    playerMotor.SetBuoyancyTarget(targetBottomY, currentBuoyancyPower);
                }
                else if (rb != null)
                {
                    float currentVy = rb.linearVelocity.y;
                    
                    if (currentVy < targetRiseVelocity)
                    {
                        currentVy = Mathf.MoveTowards(currentVy, targetRiseVelocity, upwardAcceleration * Time.fixedDeltaTime);
                    }
                    else
                    {
                        currentVy = targetRiseVelocity;
                    }

                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, currentVy, rb.linearVelocity.z);
                    // 중력에 의해 떨어지지 않도록 이번 프레임의 중력을 완전히 상쇄
                    rb.AddForce(-Physics.gravity, ForceMode.Acceleration);
                }
            }
            // 이미 설정한 비율 라인에 도달했거나 그보다 조금 높이 떠 있는 상태 (여유 범위 0.05f)
            else if (bottomY <= targetBottomY + 0.05f)
            {
                if (isPlayer && playerMotor != null)
                {
                    // 수면에 도달했을 때도 목표값을 전달하여 PlayerMotor 내부에서 수직 속도를 부드럽게 0으로 고정하도록 함
                    playerMotor.SetBuoyancyTarget(targetBottomY, currentBuoyancyPower);
                }
                else if (rb != null)
                {
                    // 낙하 중이면 뚝 멈추지 않도록 MoveTowards로 서서히 0으로 감속
                    // 이미 0이거나 상승 중이면 그 속도를 유지하여 부드럽게 수면에 안착
                    float currentVy = rb.linearVelocity.y;
                    float brakeAcceleration = currentBuoyancyPower * 5f;
                    float newVy = Mathf.MoveTowards(currentVy, 0f, brakeAcceleration * Time.fixedDeltaTime);
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, newVy, rb.linearVelocity.z);
                    // 중력 상쇄하여 수면에 고정
                    rb.AddForce(-Physics.gravity, ForceMode.Acceleration);
                }
            }
        }
    }
}
