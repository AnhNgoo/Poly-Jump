using UnityEngine;

namespace PolyJump.Scripts
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float jumpVelocity = 12f;

        [Header("Movement Smoothing")]
        [Tooltip("Do muot input ngang. Gia tri cao hon phan hoi nhanh hon.")]
        public float inputResponse = 18f;
        [Tooltip("Toc do tang van toc ngang (unit/giay^2).")]
        public float horizontalAcceleration = 55f;
        [Tooltip("Toc do giam van toc ngang (unit/giay^2).")]
        public float horizontalDeceleration = 85f;

        [Header("Touch Control")]
        [Tooltip("Toc do keo tay (pixel/giay) de dat input di chuyen toi da.")]
        public float dragPixelsForFullInput = 140f;
        [Tooltip("Nguong toc do keo toi thieu (pixel/giay) de bat dau di chuyen.")]
        public float touchDeadZonePixels = 8f;
        [Tooltip("Do nhay duong cong input keo tay. >1 giam nhay gan tam, <1 tang nhay gan tam.")]
        public float touchResponseExponent = 1f;
        [Tooltip("Nguong de uu tien input touch thay vi input ban phim/joystick.")]
        public float touchOverrideThreshold = 0.01f;

        [Header("References")]
        public Rigidbody2D rb;
        public Animator animator;
        public ParticleSystem bounceParticle;

        [Header("Bounce FX")]
        public bool autoCreateBounceParticle = true;

        [Header("Bounce Animation")]
        public string normalStateName = "Normal";
        public string crouchStateName = "Squash";
        public float animationCrossFade = 0.06f;
        public float crouchDuration = 0.1f;

        private float _horizontalInput;
        private float _targetHorizontalInput;
        private float _smoothedHorizontalInput;
        private bool _inputEnabled = true;
        private bool _gameplayPaused;
        private Vector2 _cachedVelocity;

        private int _activeTouchId = -1;
        private float _crouchUntil;
        private int _currentAnimHash;
        private int _normalAnimHash;
        private int _crouchAnimHash;
        private bool _animReady;
        private bool _warnedMissingAnimatorStates;

        private void Awake()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            _normalAnimHash = Animator.StringToHash(normalStateName);
            _crouchAnimHash = Animator.StringToHash(crouchStateName);

            EnsureBounceParticle();
            RefreshAnimatorStateCache();
        }

        private void Start()
        {
            if (rb != null && rb.velocity.y <= 0f)
            {
                rb.velocity = new Vector2(0f, jumpVelocity * 0.5f);
            }
        }

        private void Update()
        {
            CaptureInput();
            UpdateAnimationState();
        }

        private void FixedUpdate()
        {
            if (rb == null || !_inputEnabled || _gameplayPaused)
            {
                return;
            }

            float targetVelocityX = _horizontalInput * moveSpeed;
            float currentVelocityX = rb.velocity.x;
            float accel = Mathf.Abs(targetVelocityX) > Mathf.Abs(currentVelocityX)
                ? Mathf.Max(0f, horizontalAcceleration)
                : Mathf.Max(0f, horizontalDeceleration);
            float maxDelta = accel * Time.fixedDeltaTime;

            float nextVelocityX = accel <= 0f
                ? targetVelocityX
                : Mathf.MoveTowards(currentVelocityX, targetVelocityX, maxDelta);

            rb.velocity = new Vector2(nextVelocityX, rb.velocity.y);
        }

        private void CaptureInput()
        {
            if (!_inputEnabled || _gameplayPaused)
            {
                _targetHorizontalInput = 0f;
                _smoothedHorizontalInput = 0f;
                _horizontalInput = 0f;
                return;
            }

            float input = Input.GetAxisRaw("Horizontal");
            float touchInput = ReadTouchDragInput();

            // Ưu tiên touch trên mobile; desktop vẫn dùng Horizontal như bình thường.
            if (Mathf.Abs(touchInput) > Mathf.Max(0f, touchOverrideThreshold))
            {
                input = touchInput;
            }

            _targetHorizontalInput = Mathf.Clamp(input, -1f, 1f);

            float response = Mathf.Max(0f, inputResponse);
            if (response <= 0f)
            {
                _smoothedHorizontalInput = _targetHorizontalInput;
            }
            else
            {
                float t = 1f - Mathf.Exp(-response * Time.unscaledDeltaTime);
                _smoothedHorizontalInput = Mathf.Lerp(_smoothedHorizontalInput, _targetHorizontalInput, t);
            }

            if (Mathf.Abs(_targetHorizontalInput) < 0.001f && Mathf.Abs(_smoothedHorizontalInput) < 0.001f)
            {
                _smoothedHorizontalInput = 0f;
            }

            _horizontalInput = Mathf.Clamp(_smoothedHorizontalInput, -1f, 1f);
        }

        private float ReadTouchDragInput()
        {
            if (Input.touchCount <= 0)
            {
                _activeTouchId = -1;
                return 0f;
            }

            if (_activeTouchId < 0)
            {
                Touch firstTouch = Input.GetTouch(0);
                _activeTouchId = firstTouch.fingerId;
            }

            bool foundTouch = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId != _activeTouchId)
                {
                    continue;
                }

                foundTouch = true;

                if (touch.phase == TouchPhase.Began)
                {
                    return 0f;
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    _activeTouchId = -1;
                    return 0f;
                }

                // Use per-frame drag velocity so player stops immediately when finger stops moving,
                // even if finger is still touching the screen.
                float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
                float dragVelocity = touch.deltaPosition.x / dt;
                float deadZone = Mathf.Max(0f, touchDeadZonePixels);
                float absVelocity = Mathf.Abs(dragVelocity);
                if (absVelocity <= deadZone)
                {
                    return 0f;
                }

                float fullInputVelocity = Mathf.Max(1f, dragPixelsForFullInput);
                float effectiveRange = Mathf.Max(1f, fullInputVelocity - deadZone);
                float normalized = Mathf.Clamp((absVelocity - deadZone) / effectiveRange, 0f, 1f);

                float exponent = Mathf.Max(0.01f, touchResponseExponent);
                float curved = Mathf.Pow(normalized, exponent);
                float signed = Mathf.Sign(dragVelocity) * curved;
                return Mathf.Clamp(signed, -1f, 1f);
            }

            if (!foundTouch)
            {
                _activeTouchId = -1;
            }

            return 0f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (rb == null)
            {
                return;
            }

            // Chỉ cho phép nảy khi đang đi xuống để đúng cảm giác Doodle Jump.
            if (rb.velocity.y > 0.05f)
            {
                return;
            }

            bool isPlatform = other.CompareTag("Platform");
            bool isQuizPlatform = other.CompareTag("QuizPlatform");
            if (!isPlatform && !isQuizPlatform)
            {
                return;
            }

            rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
            PlayBounceEffects(other);

            if (isQuizPlatform)
            {
                if (GameManager.Instance != null)
                {
                    other.enabled = false;
                    GameManager.Instance.RegisterPendingQuizPlatform(other);
                    GameManager.Instance.RequestQuiz();
                }
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _targetHorizontalInput = 0f;
                _smoothedHorizontalInput = 0f;
                _horizontalInput = 0f;
                _activeTouchId = -1;
            }
        }

        public void SetGameplayPaused(bool paused)
        {
            bool wasPaused = _gameplayPaused;
            _gameplayPaused = paused;

            if (rb == null)
            {
                return;
            }

            if (paused)
            {
                _cachedVelocity = rb.velocity;
                rb.simulated = false;
            }
            else if (wasPaused)
            {
                rb.simulated = true;
                rb.velocity = _cachedVelocity;
            }

            if (paused)
            {
                _crouchUntil = 0f;
            }
        }

        public void ResetForNewRun()
        {
            _inputEnabled = true;
            _gameplayPaused = false;
            _activeTouchId = -1;
            _targetHorizontalInput = 0f;
            _smoothedHorizontalInput = 0f;
            _horizontalInput = 0f;

            if (rb == null)
            {
                return;
            }

            rb.simulated = true;
            rb.velocity = new Vector2(0f, jumpVelocity * 0.5f);
            _crouchUntil = 0f;
            _currentAnimHash = 0;
        }

        public void ForceNormalAnimation()
        {
            _crouchUntil = 0f;

            if (!_animReady)
            {
                RefreshAnimatorStateCache();
            }

            if (!_animReady || _normalAnimHash == 0)
            {
                return;
            }

            animator.CrossFade(_normalAnimHash, 0.02f, 0);
            _currentAnimHash = _normalAnimHash;
        }

        private void PlayBounceEffects(Collider2D surface)
        {
            EmitBounceParticle(surface);

            _crouchUntil = Time.time + Mathf.Max(0.02f, crouchDuration);
            CrossFadeTo(_crouchAnimHash);
        }

        private void EmitBounceParticle(Collider2D surface)
        {
            if (bounceParticle == null)
            {
                return;
            }

            Vector3 emitPos = transform.position;
            if (surface != null)
            {
                emitPos = surface.ClosestPoint(transform.position);
            }

            bounceParticle.transform.position = emitPos;
            bounceParticle.Play(true);
        }

        private void EnsureBounceParticle()
        {
            if (bounceParticle != null)
            {
                return;
            }

            Transform existing = transform.Find("BounceFx");
            if (existing != null)
            {
                bounceParticle = existing.GetComponent<ParticleSystem>();
                if (bounceParticle != null)
                {
                    return;
                }
            }

            if (!autoCreateBounceParticle)
            {
                return;
            }

            GameObject fx = new GameObject("BounceFx");
            fx.transform.SetParent(transform, false);
            fx.transform.localPosition = Vector3.zero;

            ParticleSystem ps = fx.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.25f;
            main.startLifetime = 0.18f;
            main.startSpeed = 1.8f;
            main.startSize = 0.15f;
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;

            ParticleSystem.ColorOverLifetimeModule colorLifetime = ps.colorOverLifetime;
            colorLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color32(0xF3, 0x70, 0x21, 0xFF), 0f),
                    new GradientColorKey(new Color32(0xFF, 0xCF, 0xB0, 0xFF), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorLifetime.color = gradient;

            bounceParticle = ps;
        }

        private void UpdateAnimationState()
        {
            if (!_animReady)
            {
                RefreshAnimatorStateCache();
                if (!_animReady)
                {
                    return;
                }
            }

            if (_gameplayPaused)
            {
                CrossFadeTo(_normalAnimHash);
                return;
            }

            int targetHash = Time.time < _crouchUntil ? _crouchAnimHash : _normalAnimHash;
            CrossFadeTo(targetHash);
        }

        private void CrossFadeTo(int targetHash)
        {
            if (!_animReady || targetHash == 0 || _currentAnimHash == targetHash)
            {
                return;
            }

            animator.CrossFade(targetHash, Mathf.Max(0f, animationCrossFade), 0);
            _currentAnimHash = targetHash;
        }

        private void RefreshAnimatorStateCache()
        {
            _animReady = false;
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            _normalAnimHash = Animator.StringToHash(normalStateName);
            _crouchAnimHash = Animator.StringToHash(crouchStateName);

            bool hasNormal = animator.HasState(0, _normalAnimHash);
            bool hasCrouch = animator.HasState(0, _crouchAnimHash);
            _animReady = hasNormal && hasCrouch;

            if (!_animReady && !_warnedMissingAnimatorStates)
            {
                _warnedMissingAnimatorStates = true;
                Debug.LogWarning("[PolyJump] Animator dang thieu state '" + normalStateName + "' hoac '" + crouchStateName + "'.");
            }
        }
    }
}
