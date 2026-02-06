using UnityEngine;

/// <summary>
/// نظام Checkpoint - تذكرة تطفو مع أنيميشن وإضاءة وصوت
/// الأنيميشن يشتغل مرة واحدة فقط! ⭐
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

    [Header("Particle Effect")]
    [SerializeField] private GameObject particleEffectPrefab; // ⭐ Particle عند التفعيل
    [SerializeField] private bool spawnParticleOnActivation = true;
    [SerializeField] private float particleLifetime = 3f;

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

        // ⭐ تعطيل Animator في البداية - لا يشتغل لين يتلمس!
        if (ticketAnimator != null)
        {
            ticketAnimator.enabled = false;
            Debug.Log("Animator disabled - will activate on touch");
        }

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

        // ⭐ تفعيل وتشغيل الأنيميشن (مرة واحدة فقط)
        if (ticketAnimator != null)
        {
            // تفعيل Animator أولاً
            ticketAnimator.enabled = true;

            // تشغيل الأنيميشن من البداية
            ticketAnimator.Play(activationAnimationName, 0, 0f);
            Debug.Log($"Playing animation: {activationAnimationName}");

            // ⭐ تعطيل Animator بعد انتهاء الأنيميشن
            float animLength = GetAnimationLength(activationAnimationName);
            if (animLength > 0)
            {
                Invoke(nameof(DisableAnimator), animLength);
            }
            else
            {
                // إذا ما قدرنا نحصل على الطول، نعطله بعد ثانية
                Invoke(nameof(DisableAnimator), 1f);
            }
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

        // ⭐ تشغيل Particle Effect
        if (spawnParticleOnActivation && particleEffectPrefab != null)
        {
            GameObject particle = Instantiate(particleEffectPrefab, transform.position, Quaternion.identity);
            Destroy(particle, particleLifetime);
            Debug.Log("Spawned particle effect");
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

    // ⭐ تعطيل Animator بعد انتهاء الأنيميشن
    void DisableAnimator()
    {
        if (ticketAnimator != null)
        {
            ticketAnimator.enabled = false;
            Debug.Log("✅ Animator disabled - animation will not loop");
        }
    }

    // ⭐ الحصول على طول الأنيميشن
    float GetAnimationLength(string animName)
    {
        if (ticketAnimator == null) return 0f;

        RuntimeAnimatorController ac = ticketAnimator.runtimeAnimatorController;
        if (ac == null) return 0f;

        foreach (AnimationClip clip in ac.animationClips)
        {
            if (clip.name == animName)
            {
                return clip.length;
            }
        }

        return 0f;
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

        // إعادة تفعيل Animator
        if (ticketAnimator != null)
        {
            ticketAnimator.enabled = true;
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