using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public PlayerController playerController;
    public SimpleRope ropeScript; // ·· Õﬁﬁ „‰ «· ⁄·Ìﬁ ⁄·Ï «·Õ»·

    [Header("Animation Parameters")]
    [Tooltip("«”„ «·»«—«„ — ›Ì Animator ··Õ—ﬂ… «·√›ﬁÌ…")]
    public string horizontalSpeedParam = "HorizontalSpeed";

    [Tooltip("«”„ «·»«—«„ — ··”—⁄… «·⁄„ÊœÌ…")]
    public string verticalSpeedParam = "VerticalSpeed";

    [Tooltip("«”„ «·»«—«„ — ··«‰Õ‰«¡")]
    public string isCrouchingParam = "IsCrouching";

    [Tooltip("«”„ «·»«—«„ — ··Ã—Ì")]
    public string isSprintingParam = "IsSprinting";

    [Tooltip("«”„ «·»«—«„ — ·· √ﬂœ „‰ «·√—÷Ì…")]
    public string isGroundedParam = "IsGrounded";

    [Tooltip("«”„ «·»«—«„ — ··Ãœ«—")]
    public string isTouchingWallParam = "IsTouchingWall";

    [Tooltip("«”„  —ÌÃ— «·ﬁ›“")]
    public string jumpTrigger = "Jump";

    [Tooltip("«”„ «·»«—«„ — ·· ⁄·Ìﬁ ⁄·Ï «·Õ»·")]
    public string isHangingParam = "IsHanging";

    [Header("Animation Smoothing")]
    public float speedDampTime = 0.1f; // Êﬁ  «· ‰⁄Ì„ ··”—⁄…
    public float wallSlideSpeed = -1f; // ”—⁄… «·«‰“·«ﬁ ⁄·Ï «·Ãœ«—

    private bool wasGrounded;
    private bool wasHanging;

    void Start()
    {
        // Auto-find components if not assigned
        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (ropeScript == null)
            ropeScript = FindObjectOfType<SimpleRope>();

        //  Õﬁﬁ „‰ ÊÃÊœ «·„ﬂÊ‰«  «·„ÿ·Ê»…
        if (animator == null)
        {
            Debug.LogError("Animator €Ì— „ÊÃÊœ! √÷› Animator Component ··«⁄»");
            enabled = false;
            return;
        }

        if (playerController == null)
        {
            Debug.LogError("PlayerController €Ì— „ÊÃÊœ!");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (animator == null || playerController == null)
            return;

        UpdateAnimationParameters();
        HandleJumpAnimation();
        HandleRopeHangingAnimation();
    }

    void UpdateAnimationParameters()
    {
        // 1. «·”—⁄… «·√›ﬁÌ… (··„‘Ì Ê«·Ã—Ì)
        float horizontalSpeed = Mathf.Abs(playerController.moveInput);

        // ≈–« ﬂ«‰ ÌÃ—Ì° ‰“Ìœ «·ﬁÌ„…
        if (playerController.IsSprinting())
        {
            horizontalSpeed *= playerController.sprintMultiplier;
        }
        // ≈–« ﬂ«‰ „‰Õ‰Ì° ‰ﬁ·· «·ﬁÌ„…
        else if (playerController.IsCrouching())
        {
            horizontalSpeed *= playerController.crouchSpeedMultiplier;
        }

        animator.SetFloat(horizontalSpeedParam, horizontalSpeed, speedDampTime, Time.deltaTime);

        // 2. «·”—⁄… «·⁄„ÊœÌ… (··ﬁ›“ Ê«·”ﬁÊÿ)
        float verticalSpeed = playerController.GetVerticalVelocity();

        // ≈–« ﬂ«‰ Ì·„” «·Ãœ«—° ‰” Œœ„ ”—⁄… «·«‰“·«ﬁ
        if (playerController.IsTouchingWall() && !playerController.IsGrounded())
        {
            verticalSpeed = Mathf.Max(verticalSpeed, wallSlideSpeed);
        }

        animator.SetFloat(verticalSpeedParam, verticalSpeed);

        // 3. Õ«·… «·«‰Õ‰«¡
        animator.SetBool(isCrouchingParam, playerController.IsCrouching());

        // 4. Õ«·… «·Ã—Ì
        animator.SetBool(isSprintingParam, playerController.IsSprinting());

        // 5. Õ«·… «·√—÷Ì…
        animator.SetBool(isGroundedParam, playerController.IsGrounded());

        // 6. Õ«·… ·„” «·Ãœ«—
        animator.SetBool(isTouchingWallParam, playerController.IsTouchingWall());
    }

    void HandleJumpAnimation()
    {
        //  ‘€Ì· √‰Ì„Ì‘‰ «·ﬁ›“ ⁄‰œ  —ﬂ «·√—÷
        bool isGrounded = playerController.IsGrounded();

        if (wasGrounded && !isGrounded && playerController.GetVerticalVelocity() > 0)
        {
            animator.SetTrigger(jumpTrigger);
        }

        wasGrounded = isGrounded;
    }

    void HandleRopeHangingAnimation()
    {
        if (ropeScript == null)
            return;

        // «· Õﬁﬁ „‰ «· ⁄·Ìﬁ ⁄·Ï «·Õ»·
        // Ì„ﬂ‰ﬂ ≈÷«›… „ €Ì— public ›Ì SimpleRope ÌŒ»—‰« »«·Õ«·…
        bool isHanging = IsPlayerHangingOnRope();

        animator.SetBool(isHangingParam, isHanging);

        // ⁄‰œ »œ«Ì… «· ⁄·Ìﬁ
        if (!wasHanging && isHanging)
        {
            // Ì„ﬂ‰ﬂ  ‘€Ì·  —ÌÃ— Œ«’ ≈–« √—œ 
            // animator.SetTrigger("StartHanging");
        }

        wasHanging = isHanging;
    }

    bool IsPlayerHangingOnRope()
    {
        // Â–Â «·œ«·…   Õﬁﬁ „‰ Õ«·… «· ⁄·Ìﬁ
        // Ì„ﬂ‰ﬂ  ⁄œÌ·Â« Õ”» ÿ—Ìﬁ… ⁄„· ”ﬂ—»  «·Õ»·

        // ÿ—Ìﬁ… 1: ≈–« ﬂ«‰ SimpleRope ÌÕ ÊÌ ⁄·Ï „ €Ì— isGrabbing
        // return ropeScript.isGrabbing;

        // ÿ—Ìﬁ… 2: «· Õﬁﬁ „‰ ÊÃÊœ SpringJoint
        SpringJoint springJoint = playerController.GetComponent<SpringJoint>();
        return springJoint != null;
    }

    // œ«·… «Œ Ì«—Ì… · €ÌÌ— ”—⁄… «·√‰Ì„Ì‘‰
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }

    // œ«·… ··⁄» √‰Ì„Ì‘‰ „⁄Ì‰
    public void PlayAnimation(string animationName)
    {
        if (animator != null)
        {
            animator.Play(animationName);
        }
    }

    void OnDrawGizmosSelected()
    {
        // —”„ „⁄·Ê„«  ··„”«⁄œ… ›Ì «· ÿÊÌ—
        if (!Application.isPlaying) return;

        Vector3 labelPos = transform.position + Vector3.up * 3f;

#if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos,
            $"Grounded: {(playerController?.IsGrounded() ?? false)}\n" +
            $"Wall: {(playerController?.IsTouchingWall() ?? false)}\n" +
            $"Crouch: {(playerController?.IsCrouching() ?? false)}\n" +
            $"Sprint: {(playerController?.IsSprinting() ?? false)}\n" +
            $"Hanging: {IsPlayerHangingOnRope()}");
#endif
    }
}