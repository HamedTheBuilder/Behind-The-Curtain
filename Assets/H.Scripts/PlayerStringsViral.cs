using UnityEngine;
using System.Collections;

public class PlayerStringsVisual : MonoBehaviour
{
    [Header("String Settings")]
    public Transform topPivot;          // «·Ã”„ «·„ Õ—ﬂ «·⁄·ÊÌ
    public Transform[] attachPoints;    // ‰ﬁ«ÿ «·—»ÿ ⁄·Ï «··«⁄» («·ÌœÌ‰° «·—Ã·Ì‰)
    public float stringThickness = 0.02f;
    public Color stringColor = new Color(0.4f, 0.3f, 0.2f);
    public Material stringMaterial;


    [Header("Pivot Movement")]
    public float pivotHeight = 4f;      // «— ›«⁄ «·Ã”„ «·⁄·ÊÌ ⁄‰ «··«⁄»
    public float floatSpeed = 2f;       // ”—⁄… «·ÿ›Ê
    public float followSpeed = 8f;      // ”—⁄… „ «»⁄… «··«⁄»
    public float floatRadius = 1.5f;    // ‰’› œ«∆—… «·ÿ›Ê

    [Header("String Physics")]
    public int stringResolution = 4;    // ‰ﬁ«ÿ ›Ì «·ŒÌÿ (·· „«Ì·)
    public float swingSpeed = 1.5f;     // ”—⁄…  „«Ì· «·ŒÌÊÿ
    public float swingAmount = 0.2f;    // ﬂ„Ì… «· „«Ì·
    public float gravityEffect = 0.3f;  // «‰Õ‰«¡ «·ŒÌÊÿ ··√”›·

    // «·„ €Ì—«  «·Œ«’…
    private LineRenderer[] stringRenderers;
    private float[] swingTimers;
    private Vector3[] swingOffsets;
    private Vector3 currentPivotPosition;

    void Start()
    {
        // ≈–« ·„ ÌÊÃœ Ã”„ ⁄·ÊÌ° ‰‰‘∆Â
        if (topPivot == null)
        {
            CreateTopPivot();
        }

        // ≈⁄œ«œ «·ŒÌÊÿ
        InitializeStrings();

        // ≈⁄œ«œ«  Õ—ﬂ… «·ÿ›Ê
        SetupSwingEffects();

        // Ê÷⁄ «·»œ«Ì…
        currentPivotPosition = transform.position + Vector3.up * pivotHeight;
    }

    void CreateTopPivot()
    {
        GameObject pivot = new GameObject("StringTopPivot");
        topPivot = pivot.transform;

        // ≈÷«›… ‘ﬂ· „—∆Ì ’€Ì—
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(topPivot);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.4f;
        visual.GetComponent<Renderer>().material.color = new Color(1f, 0.8f, 0f);
        Destroy(visual.GetComponent<Collider>());
    }

    void InitializeStrings()
    {
        stringRenderers = new LineRenderer[attachPoints.Length];

        for (int i = 0; i < attachPoints.Length; i++)
        {
            if (attachPoints[i] == null) continue;

            GameObject stringObj = new GameObject($"PlayerString_{i}");
            stringObj.transform.SetParent(transform);

            stringRenderers[i] = stringObj.AddComponent<LineRenderer>();
            SetupStringRenderer(stringRenderers[i]);
        }
    }

    void SetupStringRenderer(LineRenderer lr)
    {
        lr.positionCount = stringResolution;
        lr.startWidth = stringThickness;
        lr.endWidth = stringThickness * 0.7f;

        // Â–« ÂÊ «·”ÿ— «·„Â„:
        lr.material = stringMaterial; // ? √÷› Â–« «·”ÿ—

        // √Ê ≈–« ﬂ‰   —Ìœ „«œ… «› —«÷Ì… ≈–« ·„  Õœœ Ê«Õœ…:
        if (stringMaterial != null)
        {
            lr.material = stringMaterial;
        }
        else
        {
            // „«œ… «› —«÷Ì…
            Material defaultMat = new Material(Shader.Find("Standard"));
            defaultMat.color = stringColor;
            lr.material = defaultMat;
        }

        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }

    void SetupSwingEffects()
    {
        swingTimers = new float[attachPoints.Length];
        swingOffsets = new Vector3[attachPoints.Length];

        for (int i = 0; i < attachPoints.Length; i++)
        {
            swingTimers[i] = Random.Range(0f, Mathf.PI * 2f);
            swingOffsets[i] = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)
            ).normalized;
        }
    }

    void Update()
    {
        //  ÕœÌÀ Õ—ﬂ… «·Ã”„ «·⁄·ÊÌ
        UpdatePivotMovement();

        //  ÕœÌÀ Ã„Ì⁄ «·ŒÌÊÿ
        UpdateAllStrings();

        //  ÕœÌÀ  „«Ì· «·ŒÌÊÿ
        UpdateSwingTimers();
    }

    void UpdatePivotMovement()
    {
        if (topPivot == null) return;

        // «·Âœ›: ›Êﬁ «··«⁄» „»«‘—…
        Vector3 targetBasePosition = transform.position + Vector3.up * pivotHeight;

        // Õ—ﬂ… ÿ›Ê œ«∆—Ì…
        float time = Time.time * floatSpeed;
        Vector3 floatMovement = new Vector3(
            Mathf.Sin(time) * floatRadius,
            Mathf.Cos(time * 0.7f) * 0.5f, // Õ—ﬂ… ⁄„ÊœÌ… »”Ìÿ…
            Mathf.Cos(time) * floatRadius
        );

        // «·„Êﬁ⁄ «·‰Â«∆Ì „⁄ «·ÿ›Ê
        Vector3 targetPosition = targetBasePosition + floatMovement;

        // Õ—ﬂ… ‰«⁄„…  Ã«Â «·Âœ›
        currentPivotPosition = Vector3.Lerp(
            currentPivotPosition,
            targetPosition,
            Time.deltaTime * followSpeed
        );

        //  ÿ»Ìﬁ «·„Êﬁ⁄
        topPivot.position = currentPivotPosition;
    }

    void UpdateAllStrings()
    {
        for (int i = 0; i < stringRenderers.Length; i++)
        {
            if (stringRenderers[i] == null || attachPoints[i] == null) continue;

            //  ÕœÌÀ ‰ﬁ«ÿ «·ŒÌÿ
            UpdateStringPoints(i);

            //  ÕœÌÀ ·Ê‰ «·ŒÌÿ Õ”» «·‘œ
            UpdateStringTension(i);
        }
    }

    void UpdateStringPoints(int index)
    {
        Vector3[] points = new Vector3[stringResolution];
        Transform endPoint = attachPoints[index];

        // «·‰ﬁÿ… «·⁄·ÊÌ… („‰ «·Ã”„ «·„ Õ—ﬂ)
        points[0] = topPivot.position;

        // «·‰ﬁÿ… «·”›·Ì… (⁄·Ï «··«⁄»)
        points[stringResolution - 1] = endPoint.position;

        // Õ”«» «·‰ﬁ«ÿ «·Ê”ÿÏ „⁄ «·«‰Õ‰«¡
        for (int j = 1; j < stringResolution - 1; j++)
        {
            float t = (float)j / (stringResolution - 1);

            // «·Œÿ «·„” ﬁÌ„
            Vector3 straightPoint = Vector3.Lerp(points[0], points[stringResolution - 1], t);

            //  √ÀÌ— «·Ã«–»Ì… («‰Õ‰«¡ ··√”›·)
            float gravityCurve = Mathf.Sin(t * Mathf.PI) * gravityEffect;

            //  √ÀÌ— «· „«Ì·
            Vector3 swing = swingOffsets[index] *
                Mathf.Sin(swingTimers[index] + j * 0.8f) * swingAmount * (1 - t);

            // «·‰ﬁÿ… «·‰Â«∆Ì…
            points[j] = straightPoint +
                Vector3.down * gravityCurve +
                swing;
        }

        //  ÿ»Ìﬁ «·‰ﬁ«ÿ ⁄·Ï «·ŒÌÿ
        stringRenderers[index].SetPositions(points);
    }

    void UpdateStringTension(int index)
    {
        if (stringRenderers[index] == null) return;

        // Õ”«» «·‘œ »‰«¡ ⁄·Ï ÿÊ· «·ŒÌÿ
        float length = Vector3.Distance(topPivot.position, attachPoints[index].position);
        float maxLength = pivotHeight * 1.8f;
        float tension = Mathf.Clamp01(length / maxLength);

        //  €ÌÌ— «··Ê‰ Õ”» «·‘œ
        Color baseColor = stringColor;
        Color tensionColor = Color.Lerp(baseColor, new Color(0.7f, 0.4f, 0.2f), tension * 0.5f);

        stringRenderers[index].startColor = tensionColor;
        stringRenderers[index].endColor = Color.Lerp(tensionColor, Color.gray, 0.2f);
    }

    void UpdateSwingTimers()
    {
        for (int i = 0; i < swingTimers.Length; i++)
        {
            swingTimers[i] += Time.deltaTime * swingSpeed;
        }
    }

    // œÊ«· ·· Õﬂ„ «·Œ«—ÃÌ
    public void SetPivotHeight(float newHeight)
    {
        pivotHeight = Mathf.Max(1f, newHeight);
    }

    public void SetSwingAmount(float amount)
    {
        swingAmount = Mathf.Clamp(amount, 0f, 1f);
    }

    public void AddAttachPoint(Transform newPoint)
    {
        //  Ê”Ì⁄ «·„’›Ê›« 
        System.Array.Resize(ref attachPoints, attachPoints.Length + 1);
        attachPoints[attachPoints.Length - 1] = newPoint;

        System.Array.Resize(ref stringRenderers, stringRenderers.Length + 1);
        System.Array.Resize(ref swingTimers, swingTimers.Length + 1);
        System.Array.Resize(ref swingOffsets, swingOffsets.Length + 1);

        // ≈‰‘«¡ ŒÌÿ ÃœÌœ
        int index = attachPoints.Length - 1;
        GameObject stringObj = new GameObject($"PlayerString_{index}");
        stringObj.transform.SetParent(transform);

        stringRenderers[index] = stringObj.AddComponent<LineRenderer>();
        SetupStringRenderer(stringRenderers[index]);

        // ≈⁄œ«œ«  «· „«Ì·
        swingTimers[index] = Random.Range(0f, Mathf.PI * 2f);
        swingOffsets[index] = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;
    }

    void OnDrawGizmosSelected()
    {
        if (topPivot != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(topPivot.position, 0.3f);
            Gizmos.DrawLine(topPivot.position, transform.position + Vector3.up * pivotHeight);
        }
    }
}