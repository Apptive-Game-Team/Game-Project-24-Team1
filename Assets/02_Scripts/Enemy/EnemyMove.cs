using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 에너미의 이동(순찰, 정지, 추격)을 담당하는 클래스입니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMove : MonoBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("첫 번째 순찰 지점입니다.")]
    [SerializeField] private Transform _mark1;
    [Tooltip("두 번째 순찰 지점입니다.")]
    [SerializeField] private Transform _mark2;
    
    [Header("Stop Settings")]
    [Tooltip("이동 중 멈춰서 두리번거릴 지점들입니다.")]
    [SerializeField] private Transform[] _stopPoints;
    [Tooltip("해당 지점에서 멈춰있는 시간입니다.")]
    [Range(0f, 10f)]
    [SerializeField] private float _stopTime = 2f;
    
    [Header("Chase Settings")]
    [Tooltip("플레이어를 시야에서 놓친 후 복귀할 때까지의 대기 시간입니다.")]
    [Range(0f, 10f)]
    [SerializeField] private float _returnTime = 3f;

    /// <summary> 내비메시 에이전트 캐싱 변수입니다. </summary>
    private NavMeshAgent _agent;
    /// <summary> 현재 mark1을 향해 가고 있는지 여부입니다. </summary>
    private bool _isHeadingToMark1 = true;
    /// <summary> 게임 시작 후 첫 이동인지 여부입니다. </summary>
    private bool _isFirstMove = true;
    
    /// <summary> 현재 두리번거리는 중인지 여부입니다. </summary>
    private bool _isStoppedAndLooking = false;
    /// <summary> 마지막으로 멈췄던 지점입니다. 중복 정지를 방지합니다. </summary>
    private Transform _lastStoppedPoint = null;

    /// <summary> 추격 중인 플레이어의 Transform입니다. </summary>
    private Transform _chaseTarget;
    /// <summary> 기존 순찰 시의 이동 속도입니다. </summary>
    private float _originalSpeed;
    /// <summary> 플레이어를 놓친 후 흐른 시간입니다. </summary>
    private float _lostPlayerTimer = 0f;
    /// <summary> 현재 추격 중인지 여부입니다. </summary>
    private bool _isChasing = false;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _originalSpeed = _agent.speed;

        // 인스펙터에 할당 안 된 경우 이름으로 자동 검색
        // 나중에 없앨 기능
        if (_mark1 == null)
        {
            GameObject obj1 = GameObject.Find("CourseEndMark_1");
            if (obj1 != null)
            {
                _mark1 = obj1.transform;
            }
        }

        if (_mark2 == null)
        {
            GameObject obj2 = GameObject.Find("CourseEndMark_2");
            if (obj2 != null)
            {
                _mark2 = obj2.transform;
            }
        }
    }

    private void Update()
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
        {
            return;
        }

        // 1. 추격 상태 처리 (추격 중일 땐 순찰 무시)
        if (_isChasing)
        {
            HandleChaseState();
            return;
        }

        // 2. 정지 상태 처리 (두리번거리는 중일 땐 순찰 무시)
        if (_isStoppedAndLooking)
        {
            return;
        }

        // 3. 순찰 중 정지 지점 검사
        CheckStopPoints();

        // 4. 최초 이동 처리
        if (_isFirstMove)
        {
            StartInitialPatrol();
            return;
        }

        // 5. 일반 순찰 로직
        HandlePatrolState();
    }

    /// <summary>
    /// 시야 스크립트(EnemySight)에서 플레이어를 발견했을 때 호출하는 함수입니다.
    /// </summary>
    /// <param name="player">발견한 플레이어의 Transform</param>
    public void OnPlayerSpotted(Transform player)
    {
        _chaseTarget = player;
        _lostPlayerTimer = 0f;

        if (!_isChasing)
        {
            _isChasing = true;
            _agent.speed = 6f;
            
            // 두리번거리던 중이었다면 강제 종료
            if (_isStoppedAndLooking)
            {
                StopAllCoroutines();
                _isStoppedAndLooking = false;
                _agent.updateRotation = true;
                _agent.isStopped = false;
            }
        }
    }

    /// <summary>
    /// 추격 상태의 이동 및 시야 상실 시 복귀를 처리합니다.
    /// </summary>
    private void HandleChaseState()
    {
        if (_chaseTarget != null)
        {
            _agent.SetDestination(_chaseTarget.position);
        }

        _lostPlayerTimer += Time.deltaTime;
        
        // 시야를 잃은 지 returnTime이 지났으면 복귀
        if (_lostPlayerTimer >= _returnTime)
        {
            _isChasing = false;
            _agent.speed = _originalSpeed;
            _chaseTarget = null;
            
            if (_mark1 != null && _mark2 != null)
            {
                _agent.SetDestination(_isHeadingToMark1 ? _mark1.position : _mark2.position);
            }
        }
    }

    /// <summary>
    /// 순찰 경로 상의 정지 지점에 도달했는지 검사합니다.
    /// </summary>
    private void CheckStopPoints()
    {
        if (_stopPoints == null || _stopPoints.Length == 0)
        {
            return;
        }

        foreach (Transform sp in _stopPoints)
        {
            if (sp != null && sp != _lastStoppedPoint)
            {
                // 거리가 가까우면 멈춰서 두리번거리기
                if (Vector3.Distance(transform.position, sp.position) < 1.5f)
                {
                    StartCoroutine(StopAndLookAround(sp));
                    return; 
                }
            }
        }
    }

    /// <summary>
    /// 게임 시작 직후 최초 목적지로 이동을 시작합니다.
    /// </summary>
    private void StartInitialPatrol()
    {
        if (_mark1 != null)
        {
            _agent.SetDestination(_mark1.position);
        }
        _isHeadingToMark1 = true;
        _isFirstMove = false;
    }

    /// <summary>
    /// 목적지 도착 시 다음 마크로 방향을 전환하는 순찰 로직입니다.
    /// </summary>
    private void HandlePatrolState()
    {
        // 목적지 도착 판정
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f)
            {
                _lastStoppedPoint = null; // 왕복 시 정지 지점 재사용을 위해 초기화

                // 반대편 마크로 방향 전환
                if (_isHeadingToMark1 && _mark2 != null)
                {
                    _agent.SetDestination(_mark2.position);
                    _isHeadingToMark1 = false;
                }
                else if (!_isHeadingToMark1 && _mark1 != null)
                {
                    _agent.SetDestination(_mark1.position);
                    _isHeadingToMark1 = true;
                }
            }
        }
    }

    /// <summary>
    /// 정지 지점에서 일정 시간 동안 좌우를 두리번거립니다.
    /// </summary>
    /// <param name="stoppedPoint">도달한 정지 지점</param>
    private IEnumerator StopAndLookAround(Transform stoppedPoint)
    {
        _isStoppedAndLooking = true;
        _lastStoppedPoint = stoppedPoint;
        
        _agent.isStopped = true;
        _agent.updateRotation = false;

        Quaternion originalRot = transform.rotation;
        Quaternion leftRot = originalRot * Quaternion.Euler(0, -60, 0);
        Quaternion rightRot = originalRot * Quaternion.Euler(0, 60, 0);

        float phase1Time = _stopTime * 0.25f;
        float phase2Time = _stopTime * 0.5f;
        float phase3Time = _stopTime * 0.25f;
        float timer = 0f;

        // Phase 1: 가운데 -> 왼쪽
        while (timer < phase1Time)
        {
            transform.rotation = Quaternion.Slerp(originalRot, leftRot, timer / phase1Time);
            timer += Time.deltaTime;
            yield return null;
        }

        // Phase 2: 왼쪽 -> 오른쪽
        timer = 0f;
        while (timer < phase2Time)
        {
            transform.rotation = Quaternion.Slerp(leftRot, rightRot, timer / phase2Time);
            timer += Time.deltaTime;
            yield return null;
        }

        // Phase 3: 오른쪽 -> 가운데
        timer = 0f;
        while (timer < phase3Time)
        {
            transform.rotation = Quaternion.Slerp(rightRot, originalRot, timer / phase3Time);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.rotation = originalRot; // 정확히 원위치
        
        _agent.updateRotation = true;
        _agent.isStopped = false;
        
        _isStoppedAndLooking = false;
    }
}