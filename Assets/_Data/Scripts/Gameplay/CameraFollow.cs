using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public class CameraFollow : LoadComponents
{
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 3f;
    [SerializeField] private float minY = -10f;
    [SerializeField] private float deathZoneOffset = 2f;
    [SerializeField] private float lookAhead = 0f;
    [SerializeField] private float followUpOffset = 0f;

    private float _targetY;
    private float _currentY;
    private bool _gameOverTriggered;

    protected override void LoadComponent()
    {
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    protected override void LoadComponentRuntime()
    {
        EventManager.Instance.Subscribe(GameEvent.GameOver, OnGameOver);
    }

    private void LateUpdate()
    {
        if (target == null || _gameOverTriggered) return;

        float playerY = target.position.y;
        float cameraBottom = transform.position.y - Camera.main.orthographicSize;
        if (playerY < cameraBottom - deathZoneOffset)
        {
            TriggerGameOver();
            return;
        }

        float desiredY = playerY + lookAhead;
        float followThreshold = transform.position.y + followUpOffset;
        if (desiredY > followThreshold)
            _targetY = Mathf.Max(_targetY, desiredY);
        _targetY = Mathf.Max(_targetY, minY);

        _currentY = Mathf.Lerp(_currentY, _targetY, Time.deltaTime * followSpeed);
        transform.position = new Vector3(transform.position.x, _currentY, transform.position.z);
    }

    private void OnGameOver(object data)
    {
        _gameOverTriggered = true;
        enabled = false;
    }

    private void TriggerGameOver()
    {
        _gameOverTriggered = true;
        EventManager.Instance.Notify(GameEvent.GameOver);
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe(GameEvent.GameOver, OnGameOver);
        }
    }

    public float GetDeathY() => Camera.main.transform.position.y - Camera.main.orthographicSize - deathZoneOffset;

    public void SetTarget(Transform newTarget, bool snap = true)
    {
        target = newTarget;
        if (target == null) return;

        if (snap)
            SnapToTarget();
    }

    [Button("Snap Camera To Target")]
    public void SnapToTarget()
    {
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target != null)
        {
            _targetY = target.position.y;
            _currentY = _targetY;
            transform.position = new Vector3(transform.position.x, _currentY, transform.position.z);
        }
    }
}
