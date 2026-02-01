using UnityEngine;

/// <summary>
/// نظام إمساك ممتاز للحبل
/// </summary>
public class UltimateRopeGrabber : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float grabRange = 2.5f;
    [SerializeField] private KeyCode grabKey = KeyCode.E;
    [SerializeField] private LayerMask ropeLayer;

    [Header("Connection")]
    [SerializeField] private Vector3 handOffset = new Vector3(0, 1f, 0);
    [SerializeField] private float connectionSpring = 1000f;
    [SerializeField] private float connectionDamper = 50f;
    [SerializeField] private float maxDistance = 0.5f;

    [Header("Swing")]
    [SerializeField] private float swingForce = 150f;
    [SerializeField] private float maxSwingVelocity = 12f;
    [SerializeField] private float jumpForce = 12f;

    private Rigidbody rb;
    private PlayerController playerController;
    private UltimateRope currentRope;
    private SpringJoint playerJoint;
    private bool isGrabbing = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!isGrabbing)
        {
            if (Input.GetKeyDown(grabKey))
            {
                TryGrabRope();
            }
        }
        else
        {
            HandleSwing();

            // الإفلات
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.LeftControl))
            {
                ReleaseRope(true);
            }
            else if (Input.GetKeyDown(grabKey))
            {
                ReleaseRope(false);
            }
        }
    }

    void TryGrabRope()
    {
        UltimateRope[] ropes = FindObjectsOfType<UltimateRope>();

        UltimateRope nearestRope = null;
        float nearestDist = grabRange;

        foreach (var rope in ropes)
        {
            Transform grabPoint = rope.GetGrabPoint();
            if (grabPoint != null)
            {
                float dist = Vector3.Distance(transform.position, grabPoint.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestRope = rope;
                }
            }
        }

        if (nearestRope != null)
        {
            GrabRope(nearestRope);
        }
    }

    void GrabRope(UltimateRope rope)
    {
        currentRope = rope;
        isGrabbing = true;

        // تعطيل التحكم العادي
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // ⭐ إنشاء SpringJoint لربط اللاعب بالحبل
        Rigidbody ropeRb = rope.GetGrabRigidbody();
        if (ropeRb != null)
        {
            playerJoint = gameObject.AddComponent<SpringJoint>();
            playerJoint.connectedBody = ropeRb;
            playerJoint.autoConfigureConnectedAnchor = false;
            playerJoint.anchor = handOffset;
            playerJoint.connectedAnchor = Vector3.zero;

            playerJoint.spring = connectionSpring;
            playerJoint.damper = connectionDamper;
            playerJoint.minDistance = 0;
            playerJoint.maxDistance = maxDistance;

            // تقليل السرعة عند الإمساك
            rb.linearVelocity *= 0.5f;

            Debug.Log("✅ تم الإمساك بالحبل!");
        }
    }

    void HandleSwing()
    {
        if (currentRope == null || rb == null) return;

        float input = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(input) > 0.1f)
        {
            // إضافة قوة على محور Z
            Vector3 force = new Vector3(0, 0, input * swingForce);
            rb.AddForce(force, ForceMode.Force);

            // تحديد السرعة
            Vector3 vel = rb.linearVelocity;
            if (Mathf.Abs(vel.z) > maxSwingVelocity)
            {
                vel.z = Mathf.Sign(vel.z) * maxSwingVelocity;
                rb.linearVelocity = vel;
            }
        }
    }

    void ReleaseRope(bool jump)
    {
        if (!isGrabbing) return;

        // حذف الـ Joint
        if (playerJoint != null)
        {
            Destroy(playerJoint);
        }

        // القفز
        if (jump)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y = jumpForce;
            rb.linearVelocity = vel;
        }

        // إعادة التحكم
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        currentRope = null;
        isGrabbing = false;

        Debug.Log("❌ تم الإفلات من الحبل");
    }

    public bool IsGrabbing() => isGrabbing;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrabbing ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grabRange);

        // موضع اليد
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + handOffset, 0.15f);
    }
}