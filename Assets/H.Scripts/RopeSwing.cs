using UnityEngine;

public class SimpleRope : MonoBehaviour
{
    [Header("Rope Settings")]
    public Transform ropeStart; // نقطة بداية الحبل
    public int segments = 10; // عدد قطع الحبل
    public float segmentLength = 0.5f; // طول كل قطعة
    public float ropeWidth = 0.15f; // عرض الحبل

    [Header("Player Settings")]
    public float grabRadius = 1.5f; // مسافة الإمساك بالحبل
    public float releaseForce = 10f; // قوة القفز عند ترك الحبل
    public float swingForce = 5f; // قوة التأرجح
    public bool autoGrab = true; // إمساك تلقائي عند اللمس
    public float grabCooldown = 1f; // وقت الانتظار بعد القفز (بالثواني)

    private LineRenderer line;
    private Rigidbody[] segmentBodies;
    private Transform[] segmentTransforms;
    private Transform player;
    private Rigidbody playerRb;
    private bool isGrabbing = false;
    private SpringJoint playerSpring;
    private Vector3 playerVelocityOnGrab; // سرعة اللاعب عند الإمساك
    private float lastReleaseTime = -999f; // آخر وقت ترك فيه اللاعب الحبل

    void Start()
    {
        CreateRope();
    }

    void CreateRope()
    {
        // إنشاء الخط المرئي للحبل
        line = gameObject.AddComponent<LineRenderer>();
        line.startWidth = ropeWidth;
        line.endWidth = ropeWidth;
        line.positionCount = segments + 1;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0.4f, 0.2f, 0f);
        line.endColor = new Color(0.3f, 0.15f, 0f);

        segmentBodies = new Rigidbody[segments];
        segmentTransforms = new Transform[segments];

        // إنشاء قطع الحبل
        for (int i = 0; i < segments; i++)
        {
            GameObject segment = new GameObject("RopeSegment_" + i);
            segment.transform.parent = transform;
            segment.transform.position = ropeStart.position + Vector3.down * (segmentLength * i);

            // إضافة Rigidbody
            Rigidbody rb = segment.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.linearDamping = 3f;
            rb.angularDamping = 3f;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            segmentBodies[i] = rb;
            segmentTransforms[i] = segment.transform;

            // ربط القطع ببعضها
            if (i == 0)
            {
                // أول قطعة ثابتة عند نقطة البداية
                rb.isKinematic = true;
            }
            else
            {
                // ربط بالقطعة السابقة باستخدام Distance Joint
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

        // التحكم بالتأرجح أثناء التعليق
        if (isGrabbing)
        {
            HandleSwing();
        }
    }

    void UpdateRopeVisual()
    {
        // تحديث موقع الخط
        line.SetPosition(0, ropeStart.position);
        for (int i = 0; i < segments; i++)
        {
            line.SetPosition(i + 1, segmentTransforms[i].position);
        }
    }

    void HandlePlayerInteraction()
    {
        // البحث عن اللاعب
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerRb = p.GetComponent<Rigidbody>();
            }
            return;
        }

        // حساب المسافة لآخر قطعة من الحبل
        float dist = Vector3.Distance(player.position, segmentTransforms[segments - 1].position);

        // التحقق من وقت الـ cooldown
        bool canGrabAgain = (Time.time - lastReleaseTime) >= grabCooldown;

        // الإمساك بالحبل
        if (!isGrabbing && dist < grabRadius && canGrabAgain)
        {
            // إمساك تلقائي أو بضغط زر
            if (autoGrab || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.G))
            {
                GrabRope();
            }
        }

        // ترك الحبل
        if (isGrabbing && Input.GetKeyDown(KeyCode.Space))
        {
            ReleaseRope();
        }
    }

    void GrabRope()
    {
        isGrabbing = true;

        // حفظ سرعة اللاعب الحالية
        if (playerRb != null)
        {
            playerVelocityOnGrab = playerRb.linearVelocity;
        }

        // إيقاف تحكم اللاعب
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        // فك تجميد دوران اللاعب بالكامل (X, Y, Z)
        if (playerRb != null)
        {
            playerRb.freezeRotation = false;
            // فقط تجميد Z position
            playerRb.constraints = RigidbodyConstraints.FreezePositionZ;
        }

        // ربط اللاعب بآخر قطعة من الحبل
        playerSpring = player.gameObject.AddComponent<SpringJoint>();
        playerSpring.connectedBody = segmentBodies[segments - 1];
        playerSpring.spring = 800f;
        playerSpring.damper = 50f;
        playerSpring.minDistance = 0.2f;
        playerSpring.maxDistance = 0.6f;

        // إضافة السرعة الأولية للحبل (الزخم)
        if (playerRb != null)
        {
            segmentBodies[segments - 1].AddForce(playerVelocityOnGrab * 2f, ForceMode.Impulse);
        }

        Debug.Log("أمسكت بالحبل!");
    }

    void HandleSwing()
    {
        // التحكم باليمين واليسار للتأرجح
        float horizontal = Input.GetAxisRaw("Horizontal");

        if (horizontal != 0 && playerRb != null)
        {
            // إضافة قوة للتأرجح
            playerRb.AddForce(Vector3.right * horizontal * swingForce, ForceMode.Force);

            // إضافة قوة للحبل أيضاً
            for (int i = 0; i < segments; i++)
            {
                if (!segmentBodies[i].isKinematic)
                {
                    segmentBodies[i].AddForce(Vector3.right * horizontal * swingForce * 0.5f, ForceMode.Force);
                }
            }
        }
    }

    void ReleaseRope()
    {
        isGrabbing = false;

        // حفظ وقت الترك
        lastReleaseTime = Time.time;

        // الحصول على سرعة الحبل
        Vector3 ropeVel = segmentBodies[segments - 1].linearVelocity;

        // حذف الربط
        if (playerSpring != null)
        {
            Destroy(playerSpring);
        }

        // إعادة تجميد دوران اللاعب
        if (playerRb != null)
        {
            playerRb.freezeRotation = true;
            playerRb.constraints = RigidbodyConstraints.FreezeRotationX |
                                  RigidbodyConstraints.FreezeRotationY |
                                  RigidbodyConstraints.FreezeRotationZ |
                                  RigidbodyConstraints.FreezePositionZ;

            // تصحيح الدوران
            player.rotation = Quaternion.Euler(0, player.rotation.eulerAngles.y, 0);

            // إضافة قوة القفز
            playerRb.linearVelocity = ropeVel;
            playerRb.AddForce(Vector3.up * releaseForce, ForceMode.Impulse);
        }

        // إعادة تشغيل تحكم اللاعب
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.enabled = true;

        Debug.Log("تركت الحبل وقفزت!");
    }

    void OnDrawGizmos()
    {
        if (ropeStart != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ropeStart.position, 0.3f);
        }

        if (segmentTransforms != null && segmentTransforms.Length > 0 && segmentTransforms[segments - 1] != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(segmentTransforms[segments - 1].position, grabRadius);
        }
    }
}