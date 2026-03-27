using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float screenLeftBound = -4.5f;
    [SerializeField] private float screenRightBound = 4.5f;
    [SerializeField] private bool useCameraBounds = true;
    [SerializeField] private float cameraEdgePadding = 0.2f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Components")]
    [SerializeField] private PlayerAnimHandler animHandler;

    [Header("Touch Area")]
    [SerializeField] private RectTransform leftTouchArea;
    [SerializeField] private RectTransform rightTouchArea;

    private Rigidbody2D _rb;
    private PlayerStateMachine _stateMachine;
    private FallState _fallState;
    private JumpState _jumpState;

    private Vector2 _moveDirection;
    private bool _isTouchingLeft;
    private bool _isTouchingRight;
    private int _lastBouncePlatformId = -1;

    public Vector2 MoveDirection => _moveDirection;

    private void Awake()
    {
        if (animHandler == null)
            animHandler = GetComponent<PlayerAnimHandler>();
        if (leftTouchArea == null)
            leftTouchArea = GameObject.Find("LeftTouchArea")?.GetComponent<RectTransform>();
        if (rightTouchArea == null)
            rightTouchArea = GameObject.Find("RightTouchArea")?.GetComponent<RectTransform>();

        _rb = GetComponent<Rigidbody2D>();

        _fallState = new FallState(this, animHandler);
        _jumpState = new JumpState(this, animHandler);

        _stateMachine = new PlayerStateMachine();
        _stateMachine.Initialize(_fallState);
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleScreenWrap();
        UpdateState();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void UpdateState()
    {
        float verticalVelocity = _rb.linearVelocity.y;

        if (verticalVelocity > 0.1f)
        {
            if (_stateMachine.CurrentState.StateType != PlayerStateType.Jump)
                _stateMachine.TransitionTo(_jumpState);
        }
        else
        {
            if (_stateMachine.CurrentState.StateType != PlayerStateType.Fall)
                _stateMachine.TransitionTo(_fallState);
        }

        _stateMachine.Update();
    }

    private void HandleKeyboardInput()
    {
        float h = 0f;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

            if (left)
                h = -1f;
            else if (right)
                h = 1f;
        }

        if (h == 0f)
        {
            if (_isTouchingLeft)
                h = -1f;
            else if (_isTouchingRight)
                h = 1f;
        }

        _moveDirection = new Vector2(h, 0f);
    }

    private void ApplyMovement()
    {
        _rb.linearVelocity = new Vector2(_moveDirection.x * moveSpeed, _rb.linearVelocity.y);
    }

    private void HandleScreenWrap()

    {
        Vector3 pos = transform.position;

        if (useCameraBounds && Camera.main != null)
        {
            float halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
            float camX = Camera.main.transform.position.x;
            screenLeftBound = camX - halfWidth - cameraEdgePadding;
            screenRightBound = camX + halfWidth + cameraEdgePadding;
        }

        if (pos.x < screenLeftBound)
        {
            pos.x = screenRightBound;
            transform.position = pos;
        }
        else if (pos.x > screenRightBound)
        {
            pos.x = screenLeftBound;
            transform.position = pos;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        TryBounce(other);
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        TryBounce(other);
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        var platform = other.gameObject.GetComponent<PlatformComponent>();
        if (platform != null && platform.PlatformId == _lastBouncePlatformId)
            _lastBouncePlatformId = -1;
    }

    private void TryBounce(Collision2D other)
    {
        var platform = other.gameObject.GetComponent<PlatformComponent>();
        if (platform == null)
            platform = other.gameObject.AddComponent<PlatformComponent>();

        if (platform.PlatformId == _lastBouncePlatformId) return;
        _lastBouncePlatformId = platform.PlatformId;

        // Logic game: gọi trực tiếp singleton
        ScoreManager.Instance?.AddPlatformPass(platform.PlatformId);

        // Nảy lên
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);

        // UI: dùng event (PlatformSpawner cần biết khi nào player nhảy)
        EventManager.Instance?.Notify(GameEvent.PlayerJump, jumpForce);
    }

    public void OnPointerDownLeft() => _isTouchingLeft = true;
    public void OnPointerUpLeft() => _isTouchingLeft = false;
    public void OnPointerDownRight() => _isTouchingRight = true;
    public void OnPointerUpRight() => _isTouchingRight = false;
}
