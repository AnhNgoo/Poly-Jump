public enum PlayerStateType
{
    Fall,
    Jump
}

public interface IPlayerState
{
    PlayerStateType StateType { get; }
    void OnEnter();
    void OnUpdate();
    void OnExit();
}
