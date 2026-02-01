using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public PlayerController playerController;
    public UltimateRopeGrabber ropeGrabber; // ⭐ النظام الجديد النهائي

    [Header("Animation Parameters")]
    [Tooltip("اسم البارامتر في Animator للحركة الأفقية")]
    public string horizontalSpeedParam = "HorizontalSpeed";

    [Tooltip("اسم البارامتر للسرعة العمودية")]
    public string verticalSpeedParam = "VerticalSpeed";

    [Tooltip("اسم البارامتر للانحناء")]
    public string isCrouchingParam = "IsCrouching";

    [Tooltip("اسم البارامتر للجري")]
    public string isSprintingParam = "IsSprinting";

    [Tooltip("اسم البارامتر للتأكد من الأرضية")]
    public string isGroundedParam = "IsGrounded";

    [Tooltip("اسم البارامتر للجدار")]
    public string isTouchingWallParam = "IsTouchingWall";

    [Tooltip("اسم تريجر القفز")]
    public string jumpTrigger = "Jump";

    [Tooltip("اسم البارامتر للتعليق على الحبل")]
    public string isHangingParam = "IsHanging";

    [Tooltip("اسم تريجر بداية التعليق على الحبل")]
    public string startHangingTrigger = "StartHanging";

    [Header("Animation Smoothing")]
    public float speedDampTime = 0.1f;
    public float wallSlideSpeed = -1f;

    private bool wasGrounded;
    private bool wasHanging;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (ropeGrabber == null)
            ropeGrabber = GetComponent<UltimateRopeGrabber>();

        if (animator == null)
        {
            Debug.LogWarning("Animator غير موجود!");
        }

        if (playerController == null)
        {
            Debug.LogError("PlayerController غير موجود!");
            enabled = false;
            return;
        }

        if (ropeGrabber == null)
        {
            Debug.LogWarning("UltimateRopeGrabber غير موجود - أنيميشن الحبل لن يعمل");
        }
    }

    void Update()
    {
        if (playerController == null || animator == null)
            return;

        UpdateAnimationParameters();
        HandleJumpAnimation();
        HandleRopeHangingAnimation();
    }

    void UpdateAnimationParameters()
    {
        float horizontalSpeed = Mathf.Abs(playerController.moveInput);

        if (playerController.IsSprinting())
        {
            horizontalSpeed *= playerController.sprintMultiplier;
        }
        else if (playerController.IsCrouching())
        {
            horizontalSpeed *= playerController.crouchSpeedMultiplier;
        }

        animator.SetFloat(horizontalSpeedParam, horizontalSpeed, speedDampTime, Time.deltaTime);

        float verticalSpeed = playerController.GetVerticalVelocity();

        if (playerController.IsTouchingWall() && !playerController.IsGrounded())
        {
            verticalSpeed = Mathf.Max(verticalSpeed, wallSlideSpeed);
        }

        animator.SetFloat(verticalSpeedParam, verticalSpeed);
        animator.SetBool(isCrouchingParam, playerController.IsCrouching());
        animator.SetBool(isSprintingParam, playerController.IsSprinting());
        animator.SetBool(isGroundedParam, playerController.IsGrounded());
        animator.SetBool(isTouchingWallParam, playerController.IsTouchingWall());
    }

    void HandleJumpAnimation()
    {
        bool isGrounded = playerController.IsGrounded();

        if (wasGrounded && !isGrounded && playerController.GetVerticalVelocity() > 0)
        {
            animator.SetTrigger(jumpTrigger);
        }

        wasGrounded = isGrounded;
    }

    void HandleRopeHangingAnimation()
    {
        if (ropeGrabber == null)
            return;

        bool isHanging = ropeGrabber.IsGrabbing();

        animator.SetBool(isHangingParam, isHanging);

        if (!wasHanging && isHanging)
        {
            animator.SetTrigger(startHangingTrigger);
            Debug.Log("Started hanging animation");
        }
        else if (wasHanging && !isHanging)
        {
            Debug.Log("Released from rope animation");
        }

        wasHanging = isHanging;
    }

    public bool IsPlayerHangingOnRope()
    {
        if (ropeGrabber == null)
            return false;

        return ropeGrabber.IsGrabbing();
    }

    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }

    public void PlayAnimation(string animationName)
    {
        if (animator != null)
        {
            animator.Play(animationName);
        }
    }

    public void TriggerAnimation(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }

    public bool IsPlayingAnimation(string stateName, int layer = 0)
    {
        if (animator == null)
            return false;

        return animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName);
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Vector3 labelPos = transform.position + Vector3.up * 3f;

#if UNITY_EDITOR
        string hangingStatus = ropeGrabber != null ?
            ropeGrabber.IsGrabbing().ToString() : "N/A";

        UnityEditor.Handles.Label(labelPos,
            $"Grounded: {(playerController?.IsGrounded() ?? false)}\n" +
            $"Wall: {(playerController?.IsTouchingWall() ?? false)}\n" +
            $"Crouch: {(playerController?.IsCrouching() ?? false)}\n" +
            $"Sprint: {(playerController?.IsSprinting() ?? false)}\n" +
            $"Hanging: {hangingStatus}");
#endif
    }
}