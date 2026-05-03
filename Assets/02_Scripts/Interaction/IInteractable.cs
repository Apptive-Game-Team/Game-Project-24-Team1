namespace Nexush.Core
{
    /// <summary>
    /// 플레이어와 상호작용할 수 있는 모든 오브젝트가 구현해야 하는 공통 인터페이스
    /// </summary>
    public interface IInteractable
    {
        void Interact();
        void OnHighlight();
        void OnUnhighlight();
    }
}
