using UnityEngine;

public class disableOnRunning : MonoBehaviour
{
    private void Start()
    {
        // 해당 오브젝트의 모든 하위 오브젝트를 비활성화합니다.
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }
}
