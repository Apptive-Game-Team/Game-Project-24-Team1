using UnityEngine;

namespace MushOut.Player
{
    public class PlayerAnimationDriver : MonoBehaviour
    {
        [Header("시각적 보정")]
        [Tooltip("캐릭터가 공중에 떠 보일 경우 모델을 아래로 내리는 오프셋입니다. (보통 -0.08 권장)")]
        public float modelYOffset = -0.08f;

        private Animator _animator;

        private static readonly int AnimIDSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIDGrounded = Animator.StringToHash("Grounded");
        private static readonly int AnimIDJump = Animator.StringToHash("Jump");
        private static readonly int AnimIDFreeFall = Animator.StringToHash("FreeFall");
        private static readonly int AnimIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        private static readonly int AnimIDClimbing = Animator.StringToHash("Climbing");
        private static readonly int AnimIDClimbOver = Animator.StringToHash("ClimbOver");

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_animator != null)
            {
                _animator.applyRootMotion = false;
                Vector3 currentLocalPos = _animator.transform.localPosition;
                _animator.transform.localPosition = new Vector3(currentLocalPos.x, modelYOffset, currentLocalPos.z);
            }
        }

        public void SetGrounded(bool isGrounded)
        {
            if (_animator) _animator.SetBool(AnimIDGrounded, isGrounded);
        }

        public void SetSpeed(float actualSpeed, float inputMagnitude)
        {
            if (_animator)
            {
                _animator.SetFloat(AnimIDSpeed, actualSpeed);
                _animator.SetFloat(AnimIDMotionSpeed, inputMagnitude);
            }
        }

        public void TriggerJump()
        {
            if (_animator) _animator.SetTrigger(AnimIDJump);
        }

        public void SetState(PlayerState state)
        {
            if (_animator == null) return;

            switch (state)
            {
                case PlayerState.Idle:
                case PlayerState.Move:
                    _animator.SetBool(AnimIDFreeFall, false);
                    _animator.SetBool(AnimIDClimbing, false);
                    _animator.SetBool(AnimIDClimbOver, false);
                    break;
                case PlayerState.Jump:
                    _animator.SetBool(AnimIDClimbing, false);
                    _animator.SetBool(AnimIDClimbOver, false);
                    break;
                case PlayerState.Fall:
                    _animator.SetBool(AnimIDFreeFall, true);
                    _animator.SetBool(AnimIDClimbing, false);
                    _animator.SetBool(AnimIDClimbOver, false);
                    break;
                case PlayerState.Climbing:
                    _animator.SetBool(AnimIDClimbing, true);
                    _animator.SetBool(AnimIDFreeFall, false);
                    _animator.SetBool(AnimIDClimbOver, false);
                    break;
                case PlayerState.ClimbOver:
                    _animator.SetBool(AnimIDClimbOver, true);
                    _animator.SetBool(AnimIDClimbing, false);
                    _animator.SetBool(AnimIDFreeFall, false);
                    break;
            }
        }

        #region Animation Events

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                // TODO: 발소리 사운드 재생 로직 연결
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                // TODO: 착지 사운드 재생 로직 연결
            }
        }

        #endregion
    }
}
