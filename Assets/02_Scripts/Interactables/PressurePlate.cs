using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace MushOut.Interactables
{
    /// <summary>
    /// 무언가 올라가면 On, 아무것도 없으면 Off가 되는 압력 발판 (물리 트리거 기반)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
    {
        [Header("Pressure Plate Settings")]
        [Tooltip("이 발판을 누를 수 있는 레이어 설정 (예: Player, Box 등)")]
        public LayerMask triggerLayer;
        
        [Header("State")]
        [Tooltip("현재 발판이 눌려져 있는지 여부 (Read Only)")]
        public bool isOn = false;

        [Header("Events (Decoupled)")]
        [Tooltip("발판이 처음 눌릴 때 (On) 실행될 이벤트")]
        public UnityEvent onTurnOn;
        
        [Tooltip("발판에서 모든 물체가 내려갈 때 (Off) 실행될 이벤트")]
        public UnityEvent onTurnOff;

        [Tooltip("발판의 상태가 변경될 때마다 실행될 이벤트 (상태값 전달)")]
        public UnityEvent<bool> onToggle;

        // 발판 위에 있는 물체들을 추적하여 여러 개가 올라가도 정상 작동하도록 함
        private HashSet<Collider> objectsOnPlate = new HashSet<Collider>();

        private void Awake()
        {
            // 발판의 콜라이더는 반드시 트리거여야 합니다.
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"[PressurePlate] {gameObject.name}의 Collider가 isTrigger가 아닙니다. 코드로 자동 활성화합니다.");
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 설정된 레이어인지 확인
            if (((1 << other.gameObject.layer) & triggerLayer) != 0)
            {
                objectsOnPlate.Add(other);
                UpdateState();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (objectsOnPlate.Contains(other))
            {
                objectsOnPlate.Remove(other);
                UpdateState();
            }
        }

        private void UpdateState()
        {
            // 파괴되거나 비활성화된 콜라이더 정리 (안전장치)
            objectsOnPlate.RemoveWhere(col => col == null || !col.gameObject.activeInHierarchy);

            bool shouldBeOn = objectsOnPlate.Count > 0;

            // 상태가 변경되었을 때만 이벤트 발생
            if (isOn != shouldBeOn)
            {
                isOn = shouldBeOn;
                
                if (isOn)
                {
                    onTurnOn?.Invoke();
                }
                else
                {
                    onTurnOff?.Invoke();
                }
                
                onToggle?.Invoke(isOn);
            }
        }
    }
}
