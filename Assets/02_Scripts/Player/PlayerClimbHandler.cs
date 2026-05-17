using UnityEngine;

namespace MushOut.Player
{
    /// <summary>
    /// 플레이어의 사다리 클라이밍 로직을 전담하는 핸들러 클래스입니다.
    /// </summary>
    public class PlayerClimbHandler : MonoBehaviour
    {
        [Header("클라이밍 설정")]
        [Tooltip("사다리 등반 속도입니다.")]
        [SerializeField] private float climbSpeed = 3.0f;

        [Tooltip("꼭대기 판정 거리 (플레이어 Y 기준, 미터 단위)입니다.")]
        [SerializeField] private float climbTopThreshold = 0.6f;

        // Climb Over 내부 설정 (애니메이션 동기화용)
        private float _climbOverStepDist = 0.3f; // 앞으로 전진할 거리
        private float _climbOverYSplit = 0.15f;   // Y축 상승 완료 시점 (15%)

        // 상태 변수
        private bool _isNearLadder = false;
        private Vector3 _ladderForward = Vector3.forward;
        private Vector3 _ladderTopPoint;
        private bool _hasLadderTop = false;

        private Vector3 _climbOverStartPos;
        private Vector3 _climbOverTargetPos;

        // 의존성 캐싱
        private PlayerController _playerController;
        private PlayerInputHandler _input;
        private CharacterController _controller;
        private Animator _animator;

        // 애니메이터 파라미터 해시
        private static readonly int AnimIDSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        private static readonly int AnimIDClimbing = Animator.StringToHash("Climbing");
        private static readonly int AnimIDClimbOver = Animator.StringToHash("ClimbOver");

        public bool IsClimbingOver => _playerController.CurrentState == PlayerState.ClimbOver;
        public bool IsNearLadder => _isNearLadder;
        public Vector3 LadderTopPoint => _ladderTopPoint;
        public bool HasLadderTop => _hasLadderTop;

        /// <summary>
        /// 핸들러 초기화 및 의존성 주입
        /// </summary>
        public void Initialize(PlayerController pc, PlayerInputHandler input, CharacterController cc, Animator anim)
        {
            _playerController = pc;
            _input = input;
            _controller = cc;
            _animator = anim;
        }

        private bool _originalRootMotion;

        /// <summary>
        /// 클라이밍 오버 동작을 시작합니다.
        /// </summary>
        private void StartClimbOver()
        {
            if (IsClimbingOver) return;

            // _isClimbingOver = true; // 삭제: 이제 상태 관리는 Controller가 수행
            _climbOverTimer = 0f;
            _input.DisableInput();

            // [정석] 동작 중에만 충돌 감지를 꺼서 난간 모서리 걸림 방지
            if (_controller) _controller.detectCollisions = false;

            _climbOverStartPos = transform.position;
            Vector3 forwardDir = new Vector3(-_ladderForward.x, 0f, -_ladderForward.z).normalized;
            _climbOverTargetPos = _ladderTopPoint + (forwardDir * _climbOverStepDist) + (Vector3.up * 0.05f);
            _climbOverTargetPos.z = _climbOverStartPos.z;

            // 애니메이터 직접 제어 코드는 Controller.ChangeState로 이동됨
            _playerController.ChangeState(PlayerState.ClimbOver);
        }

        private float _climbOverTimer = 0f;

        /// <summary>
        /// 코드로 위치를 계산하여 이동시키면서 애니메이션 상태를 체크합니다.
        /// </summary>
        public void HandleClimbOver(float deltaTime)
        {
            if (_playerController.CurrentState != PlayerState.ClimbOver) return;

            _climbOverTimer += deltaTime;

            // 1. 회전 처리 (사다리 정면 응시)
            Vector3 lookDirCO = new Vector3(_ladderForward.x, 0f, _ladderForward.z);
            if (lookDirCO.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotCO = Quaternion.LookRotation(-lookDirCO);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotCO, deltaTime * 15f);
            }

            // 2. 진행도 계산 (애니메이션 시간 우선)
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            bool isClimbOverState = stateInfo.IsName("ClimbOver") || stateInfo.IsTag("ClimbOver");
            
            // 전이 중이거나 상태를 못 찾으면 타이머 기반으로 강제 진행
            float progressT = isClimbOverState ? stateInfo.normalizedTime : (_climbOverTimer / 1.1f);
            float clampedT = Mathf.Clamp01(progressT);

            // 3. 좌표 계산 (충돌이 없으므로 높이를 너무 과하게 들 필요 없음)
            Vector3 targetPos;
            float midHeight = _climbOverTargetPos.y + 0.15f; // 약간의 여유만 줌

            if (clampedT < _climbOverYSplit)
            {
                float upT = clampedT / _climbOverYSplit;
                Vector3 upTarget = new Vector3(_climbOverStartPos.x, midHeight, _climbOverStartPos.z);
                targetPos = Vector3.Lerp(_climbOverStartPos, upTarget, upT);
            }
            else
            {
                float forwardT = (clampedT - _climbOverYSplit) / (1f - _climbOverYSplit);
                Vector3 upTarget = new Vector3(_climbOverStartPos.x, midHeight, _climbOverStartPos.z);
                targetPos = Vector3.Lerp(upTarget, _climbOverTargetPos, forwardT);
            }

            // 4. 물리 이동 (detectCollisions가 꺼져있어 모서리를 부드럽게 통과함)
            if (_controller)
            {
                Vector3 delta = targetPos - transform.position;
                _controller.Move(delta);
            }

            // 5. 종료 판정 (최소 0.3초는 대기하여 애니메이션 씹힘 방지)
            if (_climbOverTimer > 0.3f)
            {
                float distToTarget = Vector3.Distance(transform.position, _climbOverTargetPos);
                if (distToTarget < 0.1f || clampedT >= 0.98f || _climbOverTimer > 1.3f)
                {
                    FinishClimbOver();
                }
            }
        }

        public void FinishClimbOver()
        {
            if (!IsClimbingOver) return;

            // [추가] Raycast로 바닥 착지 보정
            if (Physics.Raycast(_climbOverTargetPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f))
            {
                transform.position = hit.point + (Vector3.up * 0.01f);
            }

            // [정석] 동작 종료 후 충돌 감지 복구
            if (_controller) _controller.detectCollisions = true;

            _playerController.SetVerticalVelocity(0f);
            // _animator.SetBool(AnimIDClimbOver, false); // 삭제: Controller에서 관리

            _isNearLadder = false;
            // _isClimbingOver = false; // 삭제
            _input.EnableInput();

            // 컨트롤러에게 상태 종료 알림
            _playerController.ChangeState(PlayerState.Idle);
        }

        /// <summary>
        /// 사다리 타기 상태의 로직을 처리합니다.
        /// </summary>
        public void HandleClimbingState(float deltaTime, float gravity, float jumpHeight, float moveSpeed, bool isGrounded)
        {
            // 1. 사다리 방향 고정
            Vector3 lookDir = new Vector3(_ladderForward.x, 0f, _ladderForward.z);
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(-lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, deltaTime * 15f);
            }

            // 2. 점프 탈출
            if (_input.IsJumping)
            {
                Vector3 jumpBackDir = lookDir.normalized;
                float horizontalForce = moveSpeed * 1.2f;
                float verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _controller.Move(jumpBackDir * horizontalForce * deltaTime + Vector3.up * verticalVelocity * deltaTime);
                _playerController.SetVerticalVelocity(verticalVelocity); // 수직 속도 동기화
                _playerController.ChangeState(PlayerState.Jump);
                return;
            }

            // 3. 이탈 체크
            if (!IsNearLadder && !IsClimbingOver)
            {
                _playerController.ChangeState(isGrounded ? PlayerState.Idle : PlayerState.Fall);
                return;
            }

            // 4. 수직 이동
            float verticalMove = _input.MoveInput.y * climbSpeed;
            _controller.Move(Vector3.up * verticalMove * deltaTime);

            // 5. 꼭대기 도달 체크
            if (_hasLadderTop && verticalMove > 0.1f)
            {
                float distToTop = _ladderTopPoint.y - transform.position.y;
                
                // [수정] 플레이어가 사다리 꼭대기보다 '아래'에 있을 때만 넘기 동작 발동
                // 이미 올라와서 평지에 있는 상태(distToTop <= 0)라면 다시 발동하지 않음
                if (distToTop <= climbTopThreshold && distToTop > 0f)
                {
                    StartClimbOver();
                    return;
                }
            }

            // 6. 애니메이션
            if (_animator)
            {
                _animator.SetFloat(AnimIDSpeed, Mathf.Abs(verticalMove));
                float motionSpeed = 0f;
                if (_input.MoveInput.y > 0.1f) motionSpeed = 1f;
                else if (_input.MoveInput.y < -0.1f) motionSpeed = -1f;
                _animator.SetFloat(AnimIDMotionSpeed, motionSpeed);
            }

            // 7. 하강 완료
            if (isGrounded && verticalMove < -0.1f)
            {
                _playerController.ChangeState(PlayerState.Idle);
            }
        }

        public void SetNearLadder(bool value, Vector3 ladderForward, Vector3 topPoint, bool hasTop, bool isGrounded)
        {
            _isNearLadder = value;

            if (!value && _playerController.CurrentState == PlayerState.Climbing && !IsClimbingOver)
            {
                if (hasTop && (topPoint.y - transform.position.y) <= (climbTopThreshold + 0.5f) && _input.MoveInput.y > 0.1f)
                {
                    StartClimbOver();
                }
                else
                {
                    _playerController.ChangeState(isGrounded ? PlayerState.Idle : PlayerState.Fall);
                }
            }

            if (value)
            {
                _ladderForward = ladderForward;
                _ladderTopPoint = topPoint;
                _hasLadderTop = hasTop;
            }
            else
            {
                _hasLadderTop = false;
            }
        }
    }
}
