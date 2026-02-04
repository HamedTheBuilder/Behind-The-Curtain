using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.8f;
    public float crouchSpeedMultiplier = 0.5f;

    [Header("Jump")]
    public float jumpForce = 10f;
    public float wallJumpUpForce = 15f;
    public float wallJumpSideForce = 10f;
    public float wallJumpTime = 0.2f;

    [Header("Crouch")]
    public float crouchHeight = 1.0f; // ارتفاع القرفصاء
    public float standingHeight = 2f; // الارتفاع الطبيعي
    public float crouchTransitionSpeed = 15f;
    public float crouchColliderShrink = 0.3f; // ⭐ مقدار تصغير الكولايدر من الأعلى

    [Header("Collision Detection")]
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public float groundCheckRadius = 0.2f;
    public float groundCheckDistance = 0.1f;
    public float wallCheckRadius = 0.3f;
    public float wallCheckDistance = 0.7f;
    public Vector3 groundCheckOffset = new Vector3(0, -0.8f, 0);
    public float wallCheckHeightOffset = 0.5f;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private bool isGrounded;
    private bool isTouchingWall;
    private int wallSide;
    private float wallJumpTimer;
    private bool isCrouching = false;
    private bool isSprinting = false;
    private float currentHeight;
    private Vector3 originalColliderCenter; // ⭐ مركز الكولايدر الأصلي
    private float feetYPosition; // ⭐ موضع القدمين الثابت

    [HideInInspector] public bool isMoving = false;
    [HideInInspector] public float moveInput = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // تجميد X بدلاً من Z
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationY |
                        RigidbodyConstraints.FreezeRotationZ |
                        RigidbodyConstraints.FreezePositionX;

        if (capsuleCollider != null)
        {
            standingHeight = capsuleCollider.height;
            currentHeight = standingHeight;
            originalColliderCenter = capsuleCollider.center;

            // ⭐ حساب موضع القدمين الثابت (الجزء السفلي من الكولايدر)
            feetYPosition = transform.position.y - (capsuleCollider.height / 2f) + capsuleCollider.radius;
        }
    }

    void Update()
    {
        CheckCollisions();

        // ⭐ تحسين التحكم في القرفصاء
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
        {
            if (!isCrouching)
            {
                StartCrouch();
            }
        }
        else
        {
            if (isCrouching && CanStandUp())
            {
                StopCrouch();
            }
        }

        if (Input.GetKey(KeyCode.LeftShift) && isGrounded && !isCrouching)
        {
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }

        HandleCrouchTransition();

        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded && !isCrouching)
            {
                Jump();
            }
            else if (isTouchingWall && !isGrounded)
            {
                WallJump();
            }
        }
    }

    void StartCrouch()
    {
        isCrouching = true;

        // ⭐ حفظ موضع القدمين قبل القرفصاء
        if (capsuleCollider != null && isGrounded)
        {
            feetYPosition = transform.position.y - (capsuleCollider.height / 2f) + capsuleCollider.radius;
        }
    }

    void StopCrouch()
    {
        isCrouching = false;
    }

    void FixedUpdate()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (wallJumpTimer > 0)
        {
            wallJumpTimer -= Time.fixedDeltaTime;
            return;
        }

        float currentSpeed = moveSpeed;

        if (isCrouching)
        {
            currentSpeed *= crouchSpeedMultiplier;
        }
        else if (isSprinting && Mathf.Abs(moveInput) > 0.1f)
        {
            currentSpeed *= sprintMultiplier;
        }

        // الحركة على محور Z
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, moveInput * currentSpeed);

        isMoving = Mathf.Abs(moveInput) > 0.1f;

        // ✅ الدوران المعدل - الجسم يواجه اتجاه الحركة
        if (moveInput > 0) // يمين = +Z
            transform.rotation = Quaternion.Euler(0, 0, 0); // يواجه للأمام (+Z)
        else if (moveInput < 0) // يسار = -Z
            transform.rotation = Quaternion.Euler(0, 180, 0); // يواجه للخلف (-Z)
    }

    void CheckCollisions()
    {
        // ⭐ تعديل نقطة فحص الأرض بناءً على حالة القرفصاء
        float currentGroundOffset = isCrouching ? -0.5f : groundCheckOffset.y;
        Vector3 groundCheckPos = transform.position + new Vector3(0, currentGroundOffset, 0);

        RaycastHit groundHit;

        bool wasGrounded = isGrounded;
        isGrounded = Physics.SphereCast(
            groundCheckPos + Vector3.up * groundCheckRadius,
            groundCheckRadius,
            Vector3.down,
            out groundHit,
            groundCheckDistance + groundCheckRadius,
            groundLayer
        );

        // ⭐ إذا كنا نقرفص وأصبحنا غير مستقرين على الأرض، نعود للوقوف
        if (isCrouching && !isGrounded && CanStandUp())
        {
            isCrouching = false;
        }
        // إصلاح مشكلة الارتداد عند الهبوط
        if (isGrounded && rb.linearVelocity.y > 0)
        {
            // تحقق من المسافة الفعلية للأرض
            if (groundHit.distance < groundCheckRadius * 0.5f)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            }
        }

        // ✅ Wall Check محسن
        float capsuleHeight = Mathf.Max(currentHeight * 0.8f, 1f);
        Vector3 wallCheckCenter = transform.position + Vector3.up * wallCheckHeightOffset;

        RaycastHit forwardHit, backwardHit;
        bool forwardWall = Physics.SphereCast(
            wallCheckCenter,
            wallCheckRadius,
            Vector3.forward,
            out forwardHit,
            wallCheckDistance,
            wallLayer
        );

        bool backwardWall = Physics.SphereCast(
            wallCheckCenter,
            wallCheckRadius,
            Vector3.back,
            out backwardHit,
            wallCheckDistance,
            wallLayer
        );

        isTouchingWall = (forwardWall || backwardWall) && !isGrounded;

        // تحديد اتجاه الجدار
        if (forwardWall && !backwardWall)
        {
            wallSide = 1;
        }
        else if (backwardWall && !forwardWall)
        {
            wallSide = -1;
        }
        else if (forwardWall && backwardWall)
        {
            wallSide = (moveInput >= 0) ? 1 : -1;
        }
    }

    void HandleCrouchTransition()
    {
        if (capsuleCollider == null) return;

        float targetHeight = isCrouching ? crouchHeight : standingHeight;

        // ⭐ الانتقال السريع عند القرفصاء
        float transitionSpeed = isCrouching ? crouchTransitionSpeed * 2f : crouchTransitionSpeed;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * transitionSpeed);

        // ⭐ تصغير الكولايدر من الأعلى فقط
        capsuleCollider.height = currentHeight;

        // ⭐ حساب المركز الجديد للحفاظ على القدمين ثابتة
        // الجزء السفلي يبقى في نفس الموضع، الأعلى يصغر
        float heightDifference = standingHeight - currentHeight;

        Vector3 center = originalColliderCenter;
        center.y = originalColliderCenter.y - (heightDifference / 2f);
        capsuleCollider.center = center;

        // ⭐ إذا كنا نقرفص وعلى الأرض، ندفع اللاعب للأسفل قليلاً
        if (isCrouching && isGrounded)
        {
            // التحقق من وجود فراغ تحت اللاعب
            RaycastHit groundHit;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;

            if (Physics.Raycast(rayStart, Vector3.down, out groundHit, 1f, groundLayer))
            {
                float distanceToGround = groundHit.distance;

                // إذا كان هناك فراغ، ندفع اللاعب للأسفل
                if (distanceToGround > 0.05f)
                {
                    float pushAmount = Mathf.Min(distanceToGround - 0.05f, 0.1f);
                    rb.MovePosition(new Vector3(
                        transform.position.x,
                        transform.position.y - pushAmount,
                        transform.position.z
                    ));
                }
            }
        }

        // ⭐ عند القرفصاء، تصغير نصف قطر الكولايدر قليلاً ليمر من تحت الجدران
        if (isCrouching)
        {
            capsuleCollider.radius = Mathf.Lerp(capsuleCollider.radius,
                capsuleCollider.radius * (1f - crouchColliderShrink),
                Time.deltaTime * crouchTransitionSpeed);
        }
        else
        {
            capsuleCollider.radius = Mathf.Lerp(capsuleCollider.radius,
                capsuleCollider.radius / (1f - crouchColliderShrink),
                Time.deltaTime * crouchTransitionSpeed);
        }
    }

    bool CanStandUp()
    {
        if (capsuleCollider == null) return true;

        // ⭐ فحص أكثر دقة للمساحة فوق الرأس
        Vector3 checkStart = transform.position + Vector3.up * (currentHeight - capsuleCollider.radius + 0.1f);
        float checkDistance = standingHeight - currentHeight + 0.1f;
        float checkRadius = capsuleCollider.radius * 0.8f;

        // فحص باستخدام CapsuleCast للدقة
        Vector3 point1 = checkStart + Vector3.up * checkRadius;
        Vector3 point2 = checkStart + Vector3.up * (checkDistance - checkRadius);

        return !Physics.CheckCapsule(point1, point2, checkRadius, groundLayer);
    }

    void Jump()
    {
        // ⭐ إذا كنا نقرفص، نتوقف عن القرفصاء أولاً
        if (isCrouching)
        {
            if (CanStandUp())
            {
                isCrouching = false;
                // الانتقال السريع للطول الطبيعي
                currentHeight = standingHeight;
                capsuleCollider.height = currentHeight;
                capsuleCollider.center = originalColliderCenter;
            }
            else
            {
                // لا يمكن القفز إذا كان هناك عائق فوق الرأس
                return;
            }
        }

        rb.linearVelocity = new Vector3(0, 0, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void WallJump()
    {
        rb.linearVelocity = Vector3.zero;

        // القفز على محور Z
        Vector3 jumpDirection = new Vector3(0, wallJumpUpForce, -wallSide * wallJumpSideForce);
        rb.AddForce(jumpDirection, ForceMode.Impulse);

        wallJumpTimer = wallJumpTime;

        // ✅ دوران Wall Jump المعدل
        if (wallSide == 1) // جدار أمامي، اقفز للخلف
            transform.rotation = Quaternion.Euler(0, 180, 0);
        else // جدار خلفي، اقفز للأمام
            transform.rotation = Quaternion.Euler(0, 0, 0);

        Debug.Log("Wall Jump! Direction: " + jumpDirection);
    }

    public bool IsGrounded() => isGrounded;
    public bool IsTouchingWall() => isTouchingWall;
    public bool IsCrouching() => isCrouching;
    public bool IsSprinting() => isSprinting && isMoving;
    public float GetVerticalVelocity() => rb.linearVelocity.y;
    public bool IsMoving() => isMoving;

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // رسم Ground Check
        float currentGroundOffset = isCrouching ? -0.5f : groundCheckOffset.y;
        Vector3 groundCheckPos = transform.position + new Vector3(0, currentGroundOffset, 0);

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheckPos, groundCheckRadius);
        Gizmos.DrawLine(groundCheckPos, groundCheckPos + Vector3.down * (groundCheckDistance + groundCheckRadius));

        // رسم Wall Check
        Gizmos.color = isTouchingWall ? Color.yellow : Color.blue;
        Vector3 wallCheckCenter = transform.position + Vector3.up * wallCheckHeightOffset;

        Gizmos.DrawWireSphere(wallCheckCenter, wallCheckRadius);
        Gizmos.DrawLine(wallCheckCenter, wallCheckCenter + Vector3.forward * wallCheckDistance);
        Gizmos.DrawLine(wallCheckCenter, wallCheckCenter + Vector3.back * wallCheckDistance);

        // رسم CanStandUp Check
        if (isCrouching && capsuleCollider != null)
        {
            Gizmos.color = CanStandUp() ? Color.green : Color.red;

            Vector3 checkStart = transform.position + Vector3.up * (currentHeight - capsuleCollider.radius + 0.1f);
            float checkDistance = standingHeight - currentHeight + 0.1f;
            float checkRadius = capsuleCollider.radius * 0.8f;

            Vector3 point1 = checkStart + Vector3.up * checkRadius;
            Vector3 point2 = checkStart + Vector3.up * (checkDistance - checkRadius);

            // رسم الكبسولة
            Gizmos.DrawWireSphere(point1, checkRadius);
            Gizmos.DrawWireSphere(point2, checkRadius);

            // رسم الخطوط الجانبية
            Gizmos.DrawLine(point1 + Vector3.forward * checkRadius, point2 + Vector3.forward * checkRadius);
            Gizmos.DrawLine(point1 - Vector3.forward * checkRadius, point2 - Vector3.forward * checkRadius);
            Gizmos.DrawLine(point1 + Vector3.right * checkRadius, point2 + Vector3.right * checkRadius);
            Gizmos.DrawLine(point1 - Vector3.right * checkRadius, point2 - Vector3.right * checkRadius);
        }

        // ⭐ رسم حدود الكولايدر بوضوح
        if (capsuleCollider != null)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.5f);

            // حساب نقاط الكبسولة
            Vector3 worldCenter = transform.TransformPoint(capsuleCollider.center);
            float halfHeight = capsuleCollider.height / 2f - capsuleCollider.radius;

            Vector3 topPoint = worldCenter + Vector3.up * halfHeight;
            Vector3 bottomPoint = worldCenter - Vector3.up * halfHeight;

            // رسم الكبسولة
            Gizmos.DrawWireSphere(topPoint, capsuleCollider.radius);
            Gizmos.DrawWireSphere(bottomPoint, capsuleCollider.radius);

            // رسم الخطوط الجانبية
            Gizmos.DrawLine(
                topPoint + Vector3.forward * capsuleCollider.radius,
                bottomPoint + Vector3.forward * capsuleCollider.radius
            );
            Gizmos.DrawLine(
                topPoint - Vector3.forward * capsuleCollider.radius,
                bottomPoint - Vector3.forward * capsuleCollider.radius
            );
            Gizmos.DrawLine(
                topPoint + Vector3.right * capsuleCollider.radius,
                bottomPoint + Vector3.right * capsuleCollider.radius
            );
            Gizmos.DrawLine(
                topPoint - Vector3.right * capsuleCollider.radius,
                bottomPoint - Vector3.right * capsuleCollider.radius
            );

            // ⭐ تسليط الضوء على الجزء السفلي
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(bottomPoint, capsuleCollider.radius * 1.1f);
        }
    }
}