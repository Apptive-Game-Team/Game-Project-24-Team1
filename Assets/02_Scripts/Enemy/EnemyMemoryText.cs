using UnityEngine;

namespace MushOut.Enemy
{
    [DisallowMultipleComponent]
    public class EnemyMemoryText : MonoBehaviour
    {
        [TextArea(2, 4)]
        [SerializeField] private string memoryText = "기억이 흘러들어온다.";

        public string MemoryText => memoryText;

        public bool HasMemoryText => !string.IsNullOrWhiteSpace(memoryText);
    }
}
