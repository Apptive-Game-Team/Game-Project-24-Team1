using UnityEngine;
using MushOut.Interaction;

namespace MushOut.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("캐릭터 기준 상호작용 가능한 실제 반경")]
        [SerializeField] private float interactRange = 3f;
        
        [Tooltip("3인칭 카메라에서 쏘는 레이캐스트의 최대 탐색 거리")]
        [SerializeField] private float maxRaycastDistance = 50f;

        [SerializeField] private LayerMask interactableLayer;

        [Tooltip("상호작용 가능한 밀당 오브젝트와의 최대 인식 거리입니다.")]
        [SerializeField] private float pushPullDistance = 0.5f;

        private IInteractable currentInteractable;
        private PlayerInputHandler _inputHandler;
        private Camera _mainCam;

        private MushOut.Interactables.PushPullInteractable _grabbedObject;
        private float _initialGrabDistance;
        private PlayerEnvironmentDetector _environmentDetector;
        private CharacterController _characterController;

        private void Awake()
        {
            _mainCam = Camera.main;
        }

        private void Start()
        {
            _inputHandler = GetComponent<PlayerInputHandler>();
            if (_inputHandler == null)
            {
                _inputHandler = GetComponentInParent<PlayerInputHandler>();
            }

            _environmentDetector = GetComponent<PlayerEnvironmentDetector>();
            _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // 로직 분리: 입력 처리는 Update에서 수행 (PlayerInputHandler 사용)
            if (_inputHandler != null && _inputHandler.IsInteracting && currentInteractable != null)
            {
                currentInteractable.Interact();
            }

            HandlePushPull();
        }

        private void HandlePushPull()
        {
            if (_inputHandler == null || _environmentDetector == null) return;

            // 밀당(PushPull) 오브젝트를 잡는 입력을 좌클릭(IsFiring)으로 변경
            if (_inputHandler.IsFiring && (_environmentDetector.IsGrounded || _environmentDetector.IsInWater))
            {
                if (_grabbedObject == null)
                {
                    // 전방 1.0 유닛 높이에서 구체(SphereCast)를 쏴서 물체를 감지
                    Vector3 rayOrigin = transform.position + Vector3.up * 1.0f; 
                    if (Physics.SphereCast(rayOrigin, 0.5f, transform.forward, out RaycastHit hit, pushPullDistance))
                    {
                        // 자식 콜라이더(예: 무기, 방패 등)를 맞췄을 때도 부모에 있는 스크립트를 찾도록 변경
                        var interactable = hit.collider.GetComponentInParent<MushOut.Interactables.PushPullInteractable>();
                        if (interactable != null && interactable.enabled)
                        {
                            _grabbedObject = interactable;
                            _grabbedObject.StartGrab(transform);
                            _initialGrabDistance = Vector3.Distance(transform.position, _grabbedObject.transform.position);
                            
                            if (_grabbedObject.objectCollider != null && _characterController != null)
                            {
                                Physics.IgnoreCollision(_characterController, _grabbedObject.objectCollider, true);
                            }
                        }
                    }
                }
                else
                {
                    // 오브젝트가 벽에 막혀서 초기 잡았던 거리보다 5 이상 멀어지면 그랩 자동 해제 (크기가 큰 오브젝트 대응)
                    float currentDistance = Vector3.Distance(transform.position, _grabbedObject.transform.position);
                    if (currentDistance > _initialGrabDistance + 1.0f)
                    {
                        ReleaseGrabbedObject();
                    }
                }
            }
            else
            {
                if (_grabbedObject != null)
                {
                    ReleaseGrabbedObject();
                }
            }
        }

        private void ReleaseGrabbedObject()
        {
            if (_grabbedObject == null) return;

            if (_grabbedObject.objectCollider != null && _characterController != null)
            {
                Physics.IgnoreCollision(_characterController, _grabbedObject.objectCollider, false);
            }
            _grabbedObject.EndGrab();
            _grabbedObject = null;
        }

        public MushOut.Interactables.PushPullInteractable GrabbedObject => _grabbedObject;

        private void FixedUpdate()
        {
            // 물리 연산 분리: 레이캐스트는 FixedUpdate에서 수행
            PerformInteractRaycast();
        }

        private void PerformInteractRaycast()
        {
            if (_mainCam == null) return;

            // 총 쏠 때처럼 화면 정중앙(조준점)을 기준으로 레이캐스트 쏘기
            Ray ray = _mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            // 광선에 맞는 모든 물체를 가져옵니다. (interactableLayer 마스크 적용)
            RaycastHit[] hits = Physics.RaycastAll(ray, maxRaycastDistance, interactableLayer);
            
            // 🚨 최후의 수단: 씬에 있는 Button(1)을 직접 찾아서 레이저가 수학적으로 부딪히는지 강제 검사!
            GameObject testBtn = GameObject.Find("Button(1)");
            if (testBtn != null)
            {
                BoxCollider testCol = testBtn.GetComponent<BoxCollider>();
                if (testCol != null)
                {
                    if (testCol.bounds.IntersectRay(ray, out float testDist))
                    {
                        Debug.Log($"[초정밀 수학 검사] 레이저가 Button(1)에 명중했습니다!! (거리: {testDist}) / 활성화: {testCol.enabled} / 트리거: {testCol.isTrigger} / 레이어: {LayerMask.LayerToName(testBtn.layer)}");
                    }
                }
            }

            // 카메라와 가까운 순서대로 정렬합니다.
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            IInteractable foundInteractable = null;

            foreach (var hit in hits)
            {
                // 1. 카메라 광선이 플레이어(나 자신)를 등 뒤에서 뚫고 나갈 때, 플레이어는 무시합니다.
                if (hit.collider.transform.root == transform.root || hit.collider.gameObject == gameObject)
                {
                    continue;
                }

                Debug.Log($"[디버그] 레이저가 닿은 물체: {hit.collider.name} / 레이어: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

                // 2. 플레이어가 아닌 가장 먼저 맞은 물체를 확인합니다. (자식 콜라이더를 맞췄을 경우를 대비해 부모까지 탐색)
                foundInteractable = hit.collider.GetComponentInParent<IInteractable>();
                
                if (foundInteractable != null)
                {
                    // 3. 맞춘 물체(hit.point)와 '플레이어 본체(캡슐)' 사이의 실제 거리를 계산합니다.
                    float distanceToTarget = Vector3.Distance(transform.root.position, hit.point);

                    if (distanceToTarget > interactRange)
                    {
                        Debug.Log($"[디버그] 상호작용 실패: 거리가 멉니다! (현재거리: {distanceToTarget:F1} / 최대사거리: {interactRange})");
                        foundInteractable = null;
                    }
                    else
                    {
                        Debug.Log($"[디버그] 상호작용 성공! 타겟: {hit.collider.name}");
                    }
                }
                else
                {
                    Debug.Log($"[디버그] 상호작용 실패: {hit.collider.name}에는 IInteractable 스크립트가 없습니다!");
                }
                
                // 플레이어를 제외한 '가장 처음 맞은' 물체가 기준이므로 루프를 종료합니다. (벽 너머 상호작용 방지)
                break;
            }

            // 하이라이트 상태 업데이트
            if (foundInteractable != null)
            {
                if (currentInteractable != foundInteractable)
                {
                    currentInteractable?.OnUnhighlight();
                    currentInteractable = foundInteractable;
                    currentInteractable.OnHighlight();
                }
            }
            else
            {
                ClearInteractable();
            }
        }

        private void ClearInteractable()
        {
            if (currentInteractable != null)
            {
                currentInteractable.OnUnhighlight();
                currentInteractable = null;
            }
        }
    }
}
