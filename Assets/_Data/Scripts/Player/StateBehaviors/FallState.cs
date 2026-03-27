public class FallState : IPlayerState
{
    public PlayerStateType StateType => PlayerStateType.Fall;

    private PlayerController _controller;
    private PlayerAnimHandler _animHandler;

    public FallState(PlayerController controller, PlayerAnimHandler animHandler)
    {
        _controller = controller;
        _animHandler = animHandler;
    }

    public void OnEnter()
    {
        _animHandler.TriggerFall();
    }

    public void OnUpdate()
    {
        _animHandler.SetFacingDirection(_controller.MoveDirection.x);
    }

    public void OnExit()
    {
    }
}
