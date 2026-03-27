public class JumpState : IPlayerState
{
    public PlayerStateType StateType => PlayerStateType.Jump;

    private PlayerController _controller;
    private PlayerAnimHandler _animHandler;

    public JumpState(PlayerController controller, PlayerAnimHandler animHandler)
    {
        _controller = controller;
        _animHandler = animHandler;
    }

    public void OnEnter()
    {
        _animHandler.TriggerJump();
    }

    public void OnUpdate()
    {
        _animHandler.SetFacingDirection(_controller.MoveDirection.x);
    }

    public void OnExit()
    {
    }
}
