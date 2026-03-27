using UnityEngine;

public class PlayerAnimHandler : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float crossfadeDuration = 0.1f;

    private static readonly int Hash_Fall = Animator.StringToHash("Fall");
    private static readonly int Hash_Jump = Animator.StringToHash("Jump");

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void TriggerFall()
    {
        animator.CrossFade(Hash_Fall, crossfadeDuration);
    }

    public void TriggerJump()
    {
        animator.CrossFade(Hash_Jump, crossfadeDuration);
    }

    public void FlipSprite(float horizontalDirection)
    {
        if (Mathf.Abs(horizontalDirection) > 0.01f)
        {
            spriteRenderer.flipX = horizontalDirection < 0f;
        }
    }

    public void SetFacingDirection(float direction)
    {
        if (Mathf.Abs(direction) > 0.01f)
            spriteRenderer.flipX = direction < 0f;
    }
}
