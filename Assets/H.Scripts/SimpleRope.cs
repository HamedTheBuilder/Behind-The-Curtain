using UnityEngine;

/// <summary>
/// أفضل نظام حبل - بسيط، مستقر، وفعّال
/// باستخدام SpringJoint - أقوى وأبسط طريقة
/// </summary>
public class UltimateRope : MonoBehaviour
{
    [Header("Rope Setup")]
    [SerializeField] private Transform anchorPoint; // نقطة التثبيت
    [SerializeField] private int segments = 15; // عدد القطع
    [SerializeField] private float totalLength = 5f; // الطول الكلي

    [Header("Physics")]
    [SerializeField] private float mass = 0.1f; // وزن كل قطعة
    [SerializeField] private float spring = 500f; // قوة السبرنج
    [SerializeField] private float damper = 50f; // التخميد
    [SerializeField] private float drag = 1f; // مقاومة الهواء

    [Header("Visual")]
    [SerializeField] private Material ropeMaterial;
    [SerializeField] private float ropeWidth = 0.1f;
    [SerializeField] private Color ropeColor = new Color(0.4f, 0.3f, 0.2f);

    private GameObject[] ropeSegments;
    private LineRenderer lineRenderer;
    private float segmentLength;

    void Start()
    {
        CreateRope();
        SetupVisuals();
    }

    void CreateRope()
    {
        if (anchorPoint == null)
        {
            Debug.LogError("❌ Anchor Point غير معين!");
            return;
        }

        segmentLength = totalLength / segments;
        ropeSegments = new GameObject[segments];

        for (int i = 0; i < segments; i++)
        {
            // إنشاء قطعة بسيطة (نقطة)
            GameObject segment = new GameObject($"RopePoint_{i}");
            segment.transform.parent = transform;
            segment.transform.position = anchorPoint.position - new Vector3(0, i * segmentLength, 0);

            // Rigidbody بسيط
            Rigidbody rb = segment.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = drag;
            rb.angularDamping = drag;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // ⭐ تجميد الدوران والمحور X
            rb.constraints = RigidbodyConstraints.FreezeRotation |
                            RigidbodyConstraints.FreezePositionX;

            // SpringJoint للربط
            if (i == 0)
            {
                // الربط بنقطة التثبيت
                SpringJoint joint = segment.AddComponent<SpringJoint>();
                joint.connectedBody = null; // ربط بالعالم
                joint.connectedAnchor = anchorPoint.position;
                joint.spring = spring * 2f; // أقوى للنقطة الأولى
                joint.damper = damper;
                joint.minDistance = 0;
                joint.maxDistance = segmentLength * 0.1f;
                joint.autoConfigureConnectedAnchor = false;
            }
            else
            {
                // الربط بالقطعة السابقة
                SpringJoint joint = segment.AddComponent<SpringJoint>();
                joint.connectedBody = ropeSegments[i - 1].GetComponent<Rigidbody>();
                joint.spring = spring;
                joint.damper = damper;
                joint.minDistance = 0;
                joint.maxDistance = segmentLength;
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = Vector3.zero;
                joint.anchor = Vector3.zero;
            }

            ropeSegments[i] = segment;
        }

        Debug.Log("✅ تم إنشاء الحبل بنجاح!");
    }

    void SetupVisuals()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = segments + 1;
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth * 0.8f;
        lineRenderer.useWorldSpace = true;

        // المادة واللون
        if (ropeMaterial != null)
        {
            lineRenderer.material = ropeMaterial;
        }
        else
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        lineRenderer.startColor = ropeColor;
        lineRenderer.endColor = ropeColor;

        // الجودة
        lineRenderer.numCornerVertices = 5;
        lineRenderer.numCapVertices = 5;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void LateUpdate()
    {
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (lineRenderer == null || ropeSegments == null) return;

        // نقطة البداية
        lineRenderer.SetPosition(0, anchorPoint.position);

        // باقي النقاط
        for (int i = 0; i < ropeSegments.Length; i++)
        {
            if (ropeSegments[i] != null)
            {
                lineRenderer.SetPosition(i + 1, ropeSegments[i].transform.position);
            }
        }
    }

    // ⭐ دالة للحصول على نقطة الإمساك
    public Transform GetGrabPoint()
    {
        if (ropeSegments != null && ropeSegments.Length > 0)
        {
            return ropeSegments[ropeSegments.Length - 1].transform;
        }
        return null;
    }

    // دالة للحصول على Rigidbody نقطة الإمساك
    public Rigidbody GetGrabRigidbody()
    {
        Transform grab = GetGrabPoint();
        return grab != null ? grab.GetComponent<Rigidbody>() : null;
    }

    void OnDrawGizmos()
    {
        if (anchorPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(anchorPoint.position, 0.3f);

            if (Application.isPlaying && ropeSegments != null)
            {
                Gizmos.color = Color.green;
                foreach (var segment in ropeSegments)
                {
                    if (segment != null)
                    {
                        Gizmos.DrawSphere(segment.transform.position, 0.05f);
                    }
                }

                // نقطة الإمساك
                if (ropeSegments.Length > 0 && ropeSegments[ropeSegments.Length - 1] != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(ropeSegments[ropeSegments.Length - 1].transform.position, 0.2f);
                }
            }
        }
    }
}