using UnityEngine;

public class SimpleRope : MonoBehaviour
{
    [Header("Rope Settings")]
    public Transform ropeStart;
    public int segments = 10;
    public float segmentLength = 0.5f;
    public float ropeWidth = 0.15f;

    [Header("Player Settings")]
    public float grabRadius = 0.3f; // ⭐ تقليل نصف القطر للالتصاق بالحبل
    public float releaseForce = 10f;
    public float swingForce = 5f;
    public float swingTorque = 3f;
    public bool autoGrab = true;
    public float grabCooldown = 1f;

    [Header("Grab Point Settings")]
    public Transform playerGrabPoint; // نقطة الإمساك
    public Vector3 grabPointOffset = new Vector3(0, 0.5f, 0);
    public float grabSnapDistance = 0.1f; // ⭐ مسافة الالتصاق بالحبل

    [Header("Rotation Settings")]
    public float maxXRotation = 45f;
    public float rotationSpeed = 5f;

    private LineRenderer line;
    private Rigidbody[] segmentBodies;
    private Transform[] segmentTransforms;
    private SphereCollider[] segmentColliders; // ⭐ تخزين الكوليدرات
    private Transform player;
    private Rigidbody playerRb;
    private PlayerController playerController;

    [HideInInspector] public bool isGrabbing = false;
    private int currentGrabbedSegment = -1;
    private Vector3 grabPointPosition;
    private Vector3 ropeGrabPosition; // ⭐ موقع الالتصاق على الحبل
    private float currentSwingAngle = 0f;
    private float targetSwingAngle = 0f;

    private SpringJoint playerSpring;
    private Vector3 playerVelocityOnGrab;
    private float lastReleaseTime = -999f;
    private Vector3 playerOriginalPosition;
    private float originalPlayerX;

    void Start()
    {
        CreateRope();

        // إضافة وتخزين Colliders للقطع
        segmentColliders = new SphereCollider[segments];
        for (int i = 0; i < segments; i++)
        {
            SphereCollider collider = segmentTransforms[i].gameObject.AddComponent<SphereCollider>();
            collider.radius = ropeWidth * 1.5f; // ⭐ حجم أكبر قليلاً للحبل
            segmentColliders[i] = collider;
        }

        // البحث عن اللاعب
        FindPlayer();

        // إذا لم يتم تعيين نقطة الإمساك، ابحث عن واحدة أو أنشئها
        if (playerGrabPoint == null)
        {
            FindOrCreateGrabPoint();
        }
    }

    void FindOrCreateGrabPoint()
    {
        if (player == null) return;

        GameObject existingPoint = GameObject.Find("PlayerGrabPoint");
        if (existingPoint != null)
        {
            playerGrabPoint = existingPoint.transform;
            Debug.Log("تم العثور على نقطة إمساك موجودة: " + playerGrabPoint.name);
        }
        else
        {
            CreateGrabPoint();
        }

        if (playerGrabPoint != null)
        {
            RepositionGrabPoint();
        }
    }

    void CreateGrabPoint()
    {
        GameObject grabPointObj = new GameObject("PlayerGrabPoint");

        if (player != null)
        {
            grabPointObj.transform.parent = player;
        }
        else
        {
            grabPointObj.transform.parent = transform;
        }

        playerGrabPoint = grabPointObj.transform;
        Debug.Log("تم إنشاء نقطة إمساك جديدة: " + playerGrabPoint.name);
    }

    void RepositionGrabPoint()
    {
        if (playerGrabPoint == null || player == null) return;

        playerGrabPoint.localPosition = grabPointOffset;
        playerGrabPoint.localRotation = Quaternion.identity;

        Debug.Log("تمت إعادة وضع نقطة الإمساك إلى: " + playerGrabPoint.localPosition);
    }

    void CreateRope()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.startWidth = ropeWidth;
        line.endWidth = ropeWidth;
        line.positionCount = segments + 1;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0.4f, 0.2f, 0f);
        line.endColor = new Color(0.3f, 0.15f, 0f);

        segmentBodies = new Rigidbody[segments];
        segmentTransforms = new Transform[segments];

        for (int i = 0; i < segments; i++)
        {
            GameObject segment = new GameObject("RopeSegment_" + i);
            segment.transform.parent = transform;
            segment.transform.position = ropeStart.position + Vector3.down * (segmentLength * i);

            Rigidbody rb = segment.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.linearDamping = 3f;
            rb.angularDamping = 3f;
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation;

            segmentBodies[i] = rb;
            segmentTransforms[i] = segment.transform;

            if (i == 0)
            {
                rb.isKinematic = true;
            }
            else
            {
                SpringJoint spring = segment.AddComponent<SpringJoint>();
                spring.connectedBody = segmentBodies[i - 1];
                spring.autoConfigureConnectedAnchor = false;
                spring.anchor = Vector3.zero;
                spring.connectedAnchor = Vector3.zero;
                spring.spring = 500f;
                spring.damper = 50f;
                spring.minDistance = segmentLength * 0.95f;
                spring.maxDistance = segmentLength * 1.05f;
                spring.enableCollision = false;
            }
        }
    }

    void Update()
    {
        UpdateRopeVisual();
        HandlePlayerInteraction();

        if (isGrabbing)
        {
            HandleSwing();
            UpdatePlayerRotation();
            ConstrainPlayerPosition();

            // ⭐ إذا كنا نمسك الحبل، نبقى ملتصقين به
            if (currentGrabbedSegment >= 0)
            {
                MaintainGrabAttachment();
            }
        }
    }

    void MaintainGrabAttachment()
    {
        if (!isGrabbing || currentGrabbedSegment < 0 || player == null) return;

        // ⭐ جعل نقطة الإمساك تلتصق بمركز قطعة الحبل
        if (segmentTransforms[currentGrabbedSegment] != null)
        {
            // حساب الموضع النسبي بين اللاعب والحبل
            Vector3 ropePosition = segmentTransforms[currentGrabbedSegment].position;

            // ⭐ تحديث موقع نقطة الإمساك لتكون في مركز الكوليدر
            if (playerGrabPoint != null)
            {
                // نقل نقطة الإمساك إلى مركز قطعة الحبل
                playerGrabPoint.position = ropePosition;

                // ⭐ إذا كان الربيع موجوداً، تحديث نقطة الربط
                if (playerSpring != null)
                {
                    // حساب الإزاحة المحلية من مركز اللاعب إلى موقع الحبل
                    Vector3 localOffset = player.InverseTransformPoint(ropePosition);
                    playerSpring.anchor = localOffset;
                    playerSpring.connectedAnchor = Vector3.zero;
                }
            }

            // ⭐ تحديث موقع الالتصاق
            ropeGrabPosition = ropePosition;
        }
    }

    void UpdatePlayerRotation()
    {
        if (!isGrabbing || player == null) return;

        float targetXRotation = Mathf.Clamp(currentSwingAngle * 2f, -maxXRotation, maxXRotation);

        Vector3 currentRotation = player.rotation.eulerAngles;

        Quaternion targetRotation = Quaternion.Euler(
            targetXRotation,
            currentRotation.y,
            0
        );

        player.rotation = Quaternion.Lerp(player.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    void ConstrainPlayerPosition()
    {
        if (!isGrabbing || player == null || playerRb == null) return;

        Vector3 currentVelocity = playerRb.linearVelocity;

        currentVelocity.x = 0;
        currentVelocity.y = 0;

        playerRb.linearVelocity = currentVelocity;

        Vector3 currentPosition = player.position;
        player.position = new Vector3(
            originalPlayerX,
            playerOriginalPosition.y,
            currentPosition.z
        );
    }

    void UpdateRopeVisual()
    {
        if (ropeStart == null || segmentTransforms == null) return;

        line.SetPosition(0, ropeStart.position);
        for (int i = 0; i < segments; i++)
        {
            if (segmentTransforms[i] != null)
            {
                line.SetPosition(i + 1, segmentTransforms[i].position);
            }
        }
    }

    void HandlePlayerInteraction()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        Transform checkPoint = playerGrabPoint != null ? playerGrabPoint : player;
        Vector3 checkPosition = checkPoint.position;

        bool canGrabAgain = (Time.time - lastReleaseTime) >= grabCooldown;
        float closestDist = float.MaxValue;
        int closestSegment = -1;
        Vector3 closestPoint = Vector3.zero;

        // ⭐ البحث عن أقرب نقطة على الحبل (ليس فقط المركز)
        for (int i = 0; i < segments; i++)
        {
            if (segmentTransforms[i] == null) continue;

            Vector3 segmentPos = segmentTransforms[i].position;
            float dist = Vector3.Distance(checkPosition, segmentPos);

            // ⭐ فحص إذا كانت نقطة الإمساك داخل كوليدر الحبل
            if (segmentColliders[i] != null)
            {
                Vector3 closestPointOnCollider = segmentColliders[i].ClosestPoint(checkPosition);
                float distToCollider = Vector3.Distance(checkPosition, closestPointOnCollider);

                if (distToCollider < closestDist)
                {
                    closestDist = distToCollider;
                    closestSegment = i;
                    closestPoint = closestPointOnCollider;
                }
            }
            else if (dist < closestDist)
            {
                closestDist = dist;
                closestSegment = i;
                closestPoint = segmentPos;
            }
        }

        if (!isGrabbing && closestDist < grabRadius && canGrabAgain)
        {
            if (autoGrab || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.G))
            {
                // ⭐ حفظ موقع الالتصاق الدقيق على الحبل
                ropeGrabPosition = closestPoint;
                GrabRope(closestSegment);
            }
        }

        if (isGrabbing && Input.GetKeyDown(KeyCode.Space))
        {
            ReleaseRope();
        }
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerRb = playerObj.GetComponent<Rigidbody>();
            playerController = playerObj.GetComponent<PlayerController>();

            playerOriginalPosition = player.position;
            originalPlayerX = player.position.x;

            Debug.Log("تم العثور على اللاعب. الموضع الأصلي: " + playerOriginalPosition);
        }
    }

    void GrabRope(int segmentIndex)
    {
        isGrabbing = true;
        currentGrabbedSegment = segmentIndex;

        // ⭐ استخدام موقع الالتصاق على الحبل بدلاً من نقطة الإمساك
        if (ropeGrabPosition != Vector3.zero)
        {
            grabPointPosition = ropeGrabPosition;
        }
        else if (playerGrabPoint != null)
        {
            grabPointPosition = playerGrabPoint.position;
        }
        else
        {
            grabPointPosition = player.position + grabPointOffset;
        }

        if (playerRb != null)
        {
            playerVelocityOnGrab = playerRb.linearVelocity;
        }

        if (playerController != null)
        {
            playerController.enabled = false;
            playerController.isMoving = false;
        }

        if (playerRb != null)
        {
            playerRb.freezeRotation = false;
            playerRb.constraints = RigidbodyConstraints.FreezePositionX |
                                  RigidbodyConstraints.FreezePositionY |
                                  RigidbodyConstraints.FreezeRotationY |
                                  RigidbodyConstraints.FreezeRotationZ;

            playerRb.linearDamping = 3f; // ⭐ زيادة التخميد للالتصاق أفضل
            playerRb.angularDamping = 0.8f;
        }

        // ⭐ إنشاء SpringJoint مع إعدادات للالتصاق القوي
        playerSpring = player.gameObject.AddComponent<SpringJoint>();
        playerSpring.connectedBody = segmentBodies[segmentIndex];

        // ⭐ حساب الإزاحة الدقيقة من مركز اللاعب إلى نقطة الالتصاق
        Vector3 localGrabOffset = player.InverseTransformPoint(grabPointPosition);
        playerSpring.anchor = localGrabOffset;
        playerSpring.connectedAnchor = Vector3.zero;

        // ⭐ إعدادات الربيع للالتصاق القوي
        playerSpring.spring = 2000f; // ⭐ زيادة القساوة للالتصاق
        playerSpring.damper = 150f;  // ⭐ زيادة التخميد
        playerSpring.minDistance = 0.05f; // ⭐ تقليل المسافة الدنيا
        playerSpring.maxDistance = 0.1f;  // ⭐ تقليل المسافة القصوى
        playerSpring.enableCollision = true;
        playerSpring.tolerance = 0.01f; // ⭐ تقليل التسامح

        // ⭐ سحب اللاعب إلى نقطة الالتصاق فوراً
        if (playerRb != null)
        {
            Vector3 pullDirection = (ropeGrabPosition - player.position).normalized;
            float pullDistance = Vector3.Distance(player.position, ropeGrabPosition);

            if (pullDistance > 0.1f)
            {
                playerRb.AddForce(pullDirection * Mathf.Min(pullDistance * 50f, 20f), ForceMode.Impulse);
            }
        }

        if (playerRb != null && playerVelocityOnGrab.magnitude > 0.1f)
        {
            segmentBodies[segmentIndex].AddForce(playerVelocityOnGrab * 1.5f, ForceMode.Impulse);
        }

        currentSwingAngle = 0f;
        targetSwingAngle = 0f;

        Debug.Log("أمسكت بالحبل! القطعة: " + segmentIndex + " - موقع الالتصاق: " + ropeGrabPosition);
    }

    void HandleSwing()
    {
        if (playerRb == null) return;

        float horizontal = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(horizontal) > 0.1f)
        {
            float forceAmount = horizontal * swingForce;

            playerRb.AddForce(new Vector3(0, 0, forceAmount), ForceMode.Force);

            targetSwingAngle = Mathf.Clamp(targetSwingAngle + (horizontal * 30f * Time.deltaTime), -maxXRotation / 2f, maxXRotation / 2f);

            for (int i = 0; i < segments; i++)
            {
                if (!segmentBodies[i].isKinematic)
                {
                    segmentBodies[i].AddForce(new Vector3(0, 0, horizontal * swingForce * 0.2f), ForceMode.Force);
                }
            }
        }
        else
        {
            targetSwingAngle = Mathf.Lerp(targetSwingAngle, 0f, Time.deltaTime * 2f);
        }

        currentSwingAngle = Mathf.Lerp(currentSwingAngle, targetSwingAngle, Time.deltaTime * 3f);
    }

    void ReleaseRope()
    {
        isGrabbing = false;
        lastReleaseTime = Time.time;

        Vector3 ropeVel = Vector3.zero;
        if (currentGrabbedSegment >= 0 && segmentBodies[currentGrabbedSegment] != null)
        {
            ropeVel = segmentBodies[currentGrabbedSegment].linearVelocity;
        }

        if (playerSpring != null)
        {
            Destroy(playerSpring);
        }

        if (playerRb != null)
        {
            playerRb.freezeRotation = true;
            playerRb.constraints = RigidbodyConstraints.FreezeRotationX |
                                  RigidbodyConstraints.FreezeRotationY |
                                  RigidbodyConstraints.FreezeRotationZ |
                                  RigidbodyConstraints.FreezePositionX;
            playerRb.linearDamping = 0f;
            playerRb.angularDamping = 0.05f;

            float currentZVelocity = playerRb.linearVelocity.z;

            Vector3 jumpDirection = Vector3.up;
            playerRb.linearVelocity = new Vector3(0, 0, currentZVelocity * 0.5f);
            playerRb.AddForce(jumpDirection * releaseForce, ForceMode.Impulse);

            player.rotation = Quaternion.Euler(0, player.rotation.eulerAngles.y, 0);
        }

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        currentGrabbedSegment = -1;
        currentSwingAngle = 0f;
        targetSwingAngle = 0f;
        ropeGrabPosition = Vector3.zero;

        Debug.Log("تركت الحبل وقفزت!");
    }

    void OnDrawGizmos()
    {
        if (ropeStart != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ropeStart.position, 0.3f);
        }

        if (segmentTransforms != null && segmentTransforms.Length > 0)
        {
            // ⭐ رسم كوليدرات الحبل
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            for (int i = 0; i < segments; i++)
            {
                if (segmentTransforms[i] != null)
                {
                    Gizmos.DrawWireSphere(segmentTransforms[i].position, ropeWidth * 1.5f);
                }
            }

            Gizmos.color = new Color(0, 1, 0, 0.4f);
            for (int i = 0; i < segments; i++)
            {
                if (segmentTransforms[i] != null)
                {
                    Gizmos.DrawWireSphere(segmentTransforms[i].position, grabRadius);
                }
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(segmentTransforms[segments - 1].position, grabRadius * 1.2f);
        }

        // رسم نقطة الإمساك
        if (playerGrabPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerGrabPoint.position, 0.15f);

            if (player != null)
            {
                Gizmos.DrawLine(player.position, playerGrabPoint.position);
            }
        }

        if (isGrabbing && currentGrabbedSegment >= 0 && segmentTransforms[currentGrabbedSegment] != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 ropeSegmentPos = segmentTransforms[currentGrabbedSegment].position;
            Gizmos.DrawLine(player.position, ropeSegmentPos);

            // ⭐ رسم موقع الالتصاق الدقيق
            if (ropeGrabPosition != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(ropeGrabPosition, 0.1f);
                Gizmos.DrawLine(playerGrabPoint != null ? playerGrabPoint.position : player.position, ropeGrabPosition);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(player.position, player.position + Vector3.forward * 2f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(player.position, player.position + Vector3.right * 0.5f);
        }
    }

    [ContextMenu("إنشاء نقطة إمساك جديدة")]
    void CreateGrabPointFromInspector()
    {
        if (player == null)
        {
            Debug.LogWarning("اللاعب غير موجود. تأكد من وجود كائن Player في المشهد.");
            return;
        }

        CreateGrabPoint();
        Debug.Log("تم إنشاء نقطة إمساك جديدة للاعب: " + playerGrabPoint.name);
    }
}