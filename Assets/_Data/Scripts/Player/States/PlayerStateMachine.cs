public class PlayerStateMachine
{
    public IPlayerState CurrentState { get; private set; }

    public void Initialize(IPlayerState startState)
    {
        CurrentState = startState;
        CurrentState.OnEnter();
    }

    public void TransitionTo(IPlayerState nextState)
    {
        if (nextState.StateType == CurrentState.StateType)
            return;

        CurrentState.OnExit();
        CurrentState = nextState;
        CurrentState.OnEnter();
    }

    public void Update()
    {
        CurrentState?.OnUpdate();
    }
}
