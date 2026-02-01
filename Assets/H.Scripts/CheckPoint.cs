using UnityEngine;

/// <summary>
/// نظام Checkpoint - تذكرة تطفو مع أنيميشن وإضاءة وصوت
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private GameObject ticketModel; // موديل التذكرة
    [SerializeField] private Animator ticketAnimator; // أنيميتور التذكرة
    [SerializeField] private string activationAnimationName = "TicketActivate"; // اسم الأنيميشن

    [Header("Light")]
    [SerializeField] private Light checkpointLight; // الإضاءة
    [SerializeField] private Color inactiveColor = Color.gray; // لون قبل التفعيل
    [SerializeField] private Color activeColor = Color.yellow; // لون بعد التفعيل
    [SerializeField] private float lightIntensity = 2f;

    [Header("Floating")]
    [SerializeField] private bool enableFloating = true;
    [SerializeField] private float floatHeight = 0.5f; // ارتفاع الطفو
    [SerializeField] private float floatSpeed = 1f; // سرعة الطفو
    [SerializeField] private Transform floatingObject; // الأوبجكت اللي يطفو (التذكرة)

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activationSound; // صوت التفعيل
    [SerializeField][Range(0f, 1f)] private float soundVolume = 1f;

    [Header("Collision")]
    [SerializeField] private LayerMask playerLayer;

    private bool isActivated = false;
    private Vector3 startPosition;
    private float floatTimer;

    void Start()
    {
        // إعداد المكونات
        if (floatingObject != null)
        {
            startPosition = floatingObject.localPosition;
        }

        // إعداد الإضاءة
        if (checkpointLight != null)
        {
            checkpointLight.color = inactiveColor;
            checkpointLight.intensity = lightIntensity;
        }

        // إعداد AudioSource
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = soundVolume;

        Debug.Log($"Checkpoint {gameObject.name} initialized");
    }

    void Update()
    {
        // الطفو المستمر
        if (enableFloating && floatingObject != null)
        {
            floatTimer += Time.deltaTime * floatSpeed;
            float newY = startPosition.y + Mathf.Sin(floatTimer) * floatHeight;
            floatingObject.localPosition = new Vector3(
                startPosition.x,
                newY,
                startPosition.z
            );
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // التحقق من أنه اللاعب
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            ActivateCheckpoint(other.gameObject);
        }
    }

    void ActivateCheckpoint(GameObject player)
    {
        // إذا كان مفعّل من قبل، لا نسوي شيء
        if (isActivated)
        {
            Debug.Log($"Checkpoint {gameObject.name} already activated");
            return;
        }

        isActivated = true;

        // ⭐ تشغيل الأنيميشن (مرة واحدة فقط)
        if (ticketAnimator != null)
        {
            ticketAnimator.Play(activationAnimationName);
            Debug.Log($"Playing animation: {activationAnimationName}");
        }

        // ⭐ تشغيل الإضاءة (تبقى مشتغلة)
        if (checkpointLight != null)
        {
            checkpointLight.color = activeColor;
            Debug.Log("Checkpoint light activated");
        }

        // ⭐ تشغيل الصوت
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound, soundVolume);
            Debug.Log("Playing activation sound");
        }

        // ⭐ تسجيل هذا Checkpoint في نظام إدارة اللاعب
        CheckpointManager manager = FindObjectOfType<CheckpointManager>();
        if (manager != null)
        {
            manager.SetCheckpoint(this);
            Debug.Log($"Checkpoint {gameObject.name} registered with manager");
        }
        else
        {
            Debug.LogWarning("CheckpointManager not found in scene!");
        }
    }

    // دالة لإرجاع موضع إعادة الظهور
    public Vector3 GetRespawnPosition()
    {
        return transform.position;
    }

    // دالة للتحقق من التفعيل
    public bool IsActivated()
    {
        return isActivated;
    }

    // دالة لإعادة ضبط Checkpoint (للتجربة)
    public void ResetCheckpoint()
    {
        isActivated = false;

        if (checkpointLight != null)
        {
            checkpointLight.color = inactiveColor;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // رسم موضع إعادة الظهور
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
    }
}