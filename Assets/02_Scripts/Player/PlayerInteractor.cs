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

            if (_inputHandler.IsInteractingHeld && _environmentDetector.IsGrounded)
            {
                if (_grabbedObject == null)
                {
                    // 전방 1.0 유닛 높이에서 구체(SphereCast)를 쏴서 물체를 감지
                    Vector3 rayOrigin = transform.position + Vector3.up * 1.0f; 
                    if (Physics.SphereCast(rayOrigin, 0.5f, transform.forward, out RaycastHit hit, pushPullDistance))
                    {
                        var interactable = hit.collider.GetComponent<MushOut.Interactables.PushPullInteractable>();
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
                    // 오브젝트가 벽에 막히거나 거리가 벌어지면 그랩 자동 해제
                    float currentDistance = Vector3.Distance(transform.position, _grabbedObject.transform.position);
                    if (Mathf.Abs(currentDistance - _initialGrabDistance) > 0.25f || currentDistance > pushPullDistance + 1.5f)
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

            // 광선에 맞는 모든 물체를 가져옵니다.
            RaycastHit[] hits = Physics.RaycastAll(ray, maxRaycastDistance, interactableLayer);
            
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

                // 2. 플레이어가 아닌 가장 먼저 맞은 물체를 확인합니다.
                foundInteractable = hit.collider.GetComponent<IInteractable>();
                
                if (foundInteractable != null)
                {
                    // 3. 맞춘 물체(hit.point)와 '플레이어 본체(캡슐)' 사이의 실제 거리를 계산합니다.
                    // 이 스크립트가 플레이어 프리팹에 붙어있으므로 transform.root의 위치가 곧 플레이어의 기준점입니다.
                    float distanceToTarget = Vector3.Distance(transform.root.position, hit.point);

                    // 거리가 상호작용 사거리를 벗어났다면 하이라이트하지 않습니다.
                    if (distanceToTarget > interactRange)
                    {
                        foundInteractable = null;
                    }
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
