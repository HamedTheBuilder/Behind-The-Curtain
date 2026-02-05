using UnityEngine;
using System.Collections;

/// <summary>
/// أنواع التفاعل
/// </summary>
public enum InteractionType
{
    None,           // لا تفاعل
    Rotate,         // تدوير (مثل مقبض)
    Move,           // تحريك (مثل باب منزلق)
    Scale,          // تكبير/تصغير
    RotateAndMove,  // دوران وحركة معاً
    Custom          // تفاعل مخصص (أنيميشن فقط)
}

/// <summary>
/// بيانات Waypoint - صوت وتفاعل
/// </summary>
[System.Serializable]
public class WaypointData
{
    [Header("Position")]
    public Transform waypointTransform; // موضع الـ Waypoint

    [Header("Sound")]
    public bool playSound = false; // تشغيل صوت عند هذا الـ Waypoint
    public AudioClip soundClip; // الصوت
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("Interaction")]
    public InteractionType interactionType = InteractionType.None; // نوع التفاعل
    public Transform interactionObject; // الأوبجكت (مقبض، باب، إلخ)
    public float interactionDuration = 2f; // مدة التفاعل

    [Header("Rotation Interaction")]
    public Vector3 startRotation = Vector3.zero;
    public Vector3 endRotation = new Vector3(-90, 0, 0);

    [Header("Position Interaction")]
    public Vector3 startPosition = Vector3.zero;
    public Vector3 endPosition = Vector3.up;

    [Header("Scale Interaction")]
    public Vector3 startScale = Vector3.one;
    public Vector3 endScale = Vector3.one * 2f;

    [Header("Animation")]
    public string customAnimationName = ""; // أنيميشن خاص لهذا الـ Waypoint

    [Header("Wait Time")]
    public float waitTimeAfter = 0f; // وقت الانتظار بعد الوصول
}

/// <summary>
/// NPC مساعد - يدخل، يتفاعل، ويطلع
/// </summary>
public class HelperNPC : MonoBehaviour
{
    [Header("Waypoints")]
    [SerializeField] private WaypointData[] waypoints; // نقاط المسار مع البيانات
    [SerializeField] private int currentWaypointIndex = 0;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float waypointReachDistance = 0.2f;

    [Header("Default Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkAnimationName = "Walk";
    [SerializeField] private string idleAnimationName = "Idle";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Fade Out")]
    [SerializeField] private bool fadeOutAtEnd = true;
    [SerializeField] private float fadeOutDuration = 2f;
    [SerializeField] private Renderer[] renderers; // كل الـ Renderers للـ NPC

    [Header("Trigger")]
    [SerializeField] private bool startOnTrigger = false;
    [SerializeField] private string triggerTag = "Player";

    [Header("Auto Start")]
    [SerializeField] private bool autoStart = false;
    [SerializeField] private float startDelay = 0f;

    private bool isMoving = false;
    private bool hasStarted = false;

    void Start()
    {
        // إيجاد الـ Renderers تلقائياً
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        // إعداد AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound

        // البداية التلقائية
        if (autoStart)
        {
            Invoke(nameof(StartSequence), startDelay);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (startOnTrigger && !hasStarted && other.CompareTag(triggerTag))
        {
            StartSequence();
        }
    }

    // ⭐ بدء السيناريو
    public void StartSequence()
    {
        if (hasStarted) return;

        hasStarted = true;
        StartCoroutine(ExecuteSequence());
    }

    IEnumerator ExecuteSequence()
    {
        isMoving = true;

        // المشي عبر كل الـ Waypoints
        for (int i = 0; i < waypoints.Length; i++)
        {
            currentWaypointIndex = i;
            WaypointData waypointData = waypoints[i];

            if (waypointData.waypointTransform == null)
            {
                Debug.LogWarning($"Waypoint {i} transform is null!");
                continue;
            }

            // المشي للـ Waypoint
            yield return StartCoroutine(MoveToWaypoint(waypointData.waypointTransform));

            // ⭐ تشغيل الصوت عند الوصول
            if (waypointData.playSound && waypointData.soundClip != null)
            {
                audioSource.PlayOneShot(waypointData.soundClip, waypointData.soundVolume);
                Debug.Log($"🔊 Playing sound at waypoint {i}");
            }

            // ⭐ تنفيذ التفاعل
            if (waypointData.interactionType != InteractionType.None)
            {
                yield return StartCoroutine(PerformInteraction(waypointData));
            }

            // انتظار بعد الوصول
            if (waypointData.waitTimeAfter > 0)
            {
                PlayAnimation(idleAnimationName);
                yield return new WaitForSeconds(waypointData.waitTimeAfter);
            }
        }

        isMoving = false;

        // Fade Out والاختفاء
        if (fadeOutAtEnd)
        {
            yield return StartCoroutine(FadeOut());
            Destroy(gameObject);
        }

        Debug.Log("✅ Helper NPC sequence complete!");
    }

    // ⭐ المشي لنقطة معينة
    IEnumerator MoveToWaypoint(Transform target)
    {
        // تشغيل أنيميشن المشي
        PlayAnimation(walkAnimationName);

        while (Vector3.Distance(transform.position, target.position) > waypointReachDistance)
        {
            // الحركة
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            // الدوران باتجاه الحركة
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            yield return null;
        }

        // وصلنا!
        transform.position = target.position;
    }

    // ⭐ تنفيذ التفاعل
    IEnumerator PerformInteraction(WaypointData data)
    {
        if (data.interactionObject == null)
        {
            Debug.LogWarning("Interaction object is null!");
            yield break;
        }

        // الدوران باتجاه الأوبجكت
        Vector3 direction = (data.interactionObject.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            float rotateTime = 0f;
            while (rotateTime < 0.5f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * 2f);
                rotateTime += Time.deltaTime;
                yield return null;
            }
        }

        // تشغيل أنيميشن مخصص إذا موجود
        if (!string.IsNullOrEmpty(data.customAnimationName))
        {
            PlayAnimation(data.customAnimationName);
        }
        else
        {
            PlayAnimation(idleAnimationName);
        }

        // تنفيذ التفاعل حسب النوع
        float elapsed = 0f;

        // حفظ القيم الابتدائية
        Vector3 objStartRot = data.interactionObject.localEulerAngles;
        Vector3 objStartPos = data.interactionObject.localPosition;
        Vector3 objStartScale = data.interactionObject.localScale;

        while (elapsed < data.interactionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / data.interactionDuration;

            switch (data.interactionType)
            {
                case InteractionType.Rotate:
                    data.interactionObject.localRotation = Quaternion.Lerp(
                        Quaternion.Euler(data.startRotation),
                        Quaternion.Euler(data.endRotation),
                        t
                    );
                    break;

                case InteractionType.Move:
                    data.interactionObject.localPosition = Vector3.Lerp(
                        data.startPosition,
                        data.endPosition,
                        t
                    );
                    break;

                case InteractionType.Scale:
                    data.interactionObject.localScale = Vector3.Lerp(
                        data.startScale,
                        data.endScale,
                        t
                    );
                    break;

                case InteractionType.RotateAndMove:
                    data.interactionObject.localRotation = Quaternion.Lerp(
                        Quaternion.Euler(data.startRotation),
                        Quaternion.Euler(data.endRotation),
                        t
                    );
                    data.interactionObject.localPosition = Vector3.Lerp(
                        data.startPosition,
                        data.endPosition,
                        t
                    );
                    break;

                case InteractionType.Custom:
                    // أنيميشن فقط - لا تحريك
                    break;
            }

            yield return null;
        }

        // التأكد من الوصول للقيمة النهائية
        switch (data.interactionType)
        {
            case InteractionType.Rotate:
                data.interactionObject.localRotation = Quaternion.Euler(data.endRotation);
                break;
            case InteractionType.Move:
                data.interactionObject.localPosition = data.endPosition;
                break;
            case InteractionType.Scale:
                data.interactionObject.localScale = data.endScale;
                break;
            case InteractionType.RotateAndMove:
                data.interactionObject.localRotation = Quaternion.Euler(data.endRotation);
                data.interactionObject.localPosition = data.endPosition;
                break;
        }

        Debug.Log($"🎮 Interaction complete: {data.interactionType}");
    }

    // ⭐ Fade Out
    IEnumerator FadeOut()
    {
        // إيجاد كل المواد
        Material[][] allMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            allMaterials[i] = renderers[i].materials;
        }

        // Fade Out تدريجي
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeOutDuration);

            // تطبيق Alpha على كل المواد
            for (int i = 0; i < renderers.Length; i++)
            {
                foreach (Material mat in allMaterials[i])
                {
                    // محاولة تطبيق Alpha بطرق مختلفة حسب الـ Shader

                    // الطريقة 1: _Color (Standard Shader)
                    if (mat.HasProperty("_Color"))
                    {
                        Color color = mat.GetColor("_Color");
                        color.a = alpha;
                        mat.SetColor("_Color", color);
                    }
                    // الطريقة 2: _BaseColor (URP/HDRP)
                    else if (mat.HasProperty("_BaseColor"))
                    {
                        Color color = mat.GetColor("_BaseColor");
                        color.a = alpha;
                        mat.SetColor("_BaseColor", color);
                    }
                    // الطريقة 3: _MainColor
                    else if (mat.HasProperty("_MainColor"))
                    {
                        Color color = mat.GetColor("_MainColor");
                        color.a = alpha;
                        mat.SetColor("_MainColor", color);
                    }
                    // الطريقة 4: Alpha مباشرة
                    else if (mat.HasProperty("_Alpha"))
                    {
                        mat.SetFloat("_Alpha", alpha);
                    }

                    // تغيير Render Queue للشفافية
                    if (mat.renderQueue < 3000)
                    {
                        mat.renderQueue = 3000;
                    }
                }
            }

            yield return null;
        }

        // شفاف تماماً
        for (int i = 0; i < renderers.Length; i++)
        {
            foreach (Material mat in allMaterials[i])
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.GetColor("_Color");
                    color.a = 0f;
                    mat.SetColor("_Color", color);
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    Color color = mat.GetColor("_BaseColor");
                    color.a = 0f;
                    mat.SetColor("_BaseColor", color);
                }
                else if (mat.HasProperty("_MainColor"))
                {
                    Color color = mat.GetColor("_MainColor");
                    color.a = 0f;
                    mat.SetColor("_MainColor", color);
                }
                else if (mat.HasProperty("_Alpha"))
                {
                    mat.SetFloat("_Alpha", 0f);
                }
            }
        }
    }

    void PlayAnimation(string animName)
    {
        if (animator != null && !string.IsNullOrEmpty(animName))
        {
            animator.Play(animName);
        }
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // رسم المسار
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i].waypointTransform == null) continue;

            Vector3 pos = waypoints[i].waypointTransform.position;

            // تحديد اللون حسب نوع الـ Waypoint
            if (waypoints[i].interactionType != InteractionType.None)
            {
                Gizmos.color = Color.yellow; // تفاعل
            }
            else if (waypoints[i].playSound)
            {
                Gizmos.color = Color.cyan; // صوت
            }
            else
            {
                Gizmos.color = Color.white; // عادي
            }

            // رسم الـ Waypoint
            Gizmos.DrawWireSphere(pos, 0.3f);

            // رسم الخط للـ Waypoint التالي
            if (i < waypoints.Length - 1 && waypoints[i + 1].waypointTransform != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(pos, waypoints[i + 1].waypointTransform.position);
            }

            // رسم أيقونة التفاعل
#if UNITY_EDITOR
            string label = $"WP {i}";

            if (waypoints[i].playSound)
                label += " 🔊";

            if (waypoints[i].interactionType != InteractionType.None)
                label += $" [{waypoints[i].interactionType}]";

            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, label);

            // رسم الأوبجكت التفاعلي
            if (waypoints[i].interactionObject != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(pos, waypoints[i].interactionObject.position);
                Gizmos.DrawWireSphere(waypoints[i].interactionObject.position, 0.4f);
            }
#endif
        }
    }
}