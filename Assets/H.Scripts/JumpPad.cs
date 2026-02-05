using UnityEngine;

/// <summary>
/// Jump Pad - منصة قفز تنط اللاعب للأعلى
/// </summary>
public class JumpPad : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 20f; // ⭐ قوة القفز
    [SerializeField] private Vector3 jumpDirection = Vector3.up; // اتجاه القفز (افتراضياً للأعلى)
    [SerializeField] private bool normalizeDirection = true; // جعل الاتجاه بطول 1
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound; // ⭐ صوت القفز
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool enableVisualFeedback = true;
    [SerializeField] private Animator animator; // أنيميتور (اختياري)
    [SerializeField] private string triggerAnimationName = "Bounce"; // اسم الأنيميشن
    [SerializeField] private GameObject visualEffect; // تأثير بصري (اختياري)
    [SerializeField] private float effectDuration = 1f; // مدة التأثير
    
    [Header("Cooldown")]
    [SerializeField] private bool useCooldown = true;
    [SerializeField] private float cooldownTime = 0.5f; // ثانية واحدة بين كل قفزة
    
    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool onlyFromTop = true; // فقط من الأعلى
    
    private float lastJumpTime = -999f;
    private Vector3 normalizedDirection;
    
    void Start()
    {
        // تطبيع اتجاه القفز
        if (normalizeDirection)
        {
            normalizedDirection = jumpDirection.normalized;
        }
        else
        {
            normalizedDirection = jumpDirection;
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
        
        // إخفاء التأثير البصري في البداية
        if (visualEffect != null)
        {
            visualEffect.SetActive(false);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // التحقق من Cooldown
        if (useCooldown && Time.time - lastJumpTime < cooldownTime)
        {
            return;
        }
        
        // التحقق من أنه اللاعب
        if (((1 << collision.gameObject.layer) & playerLayer) == 0)
        {
            return;
        }
        
        // التحقق من الاصطدام من الأعلى
        if (onlyFromTop)
        {
            // حساب اتجاه الاصطدام
            Vector3 contactNormal = Vector3.zero;
            foreach (ContactPoint contact in collision.contacts)
            {
                contactNormal += contact.normal;
            }
            contactNormal.Normalize();
            
            // التحقق من أن الاصطدام من الأعلى
            float dotProduct = Vector3.Dot(contactNormal, Vector3.down);
            if (dotProduct < 0.5f) // زاوية أقل من 60 درجة
            {
                return;
            }
        }
        
        // تطبيق القفزة
        ApplyJump(collision.gameObject);
    }
    
    void OnTriggerEnter(Collider other)
    {
        // التحقق من Cooldown
        if (useCooldown && Time.time - lastJumpTime < cooldownTime)
        {
            return;
        }
        
        // التحقق من أنه اللاعب
        if (((1 << other.gameObject.layer) & playerLayer) == 0)
        {
            return;
        }
        
        // تطبيق القفزة
        ApplyJump(other.gameObject);
    }
    
    void ApplyJump(GameObject player)
    {
        // الحصول على Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("Player has no Rigidbody!");
            return;
        }
        
        // ⭐ إلغاء السرعة الحالية على محور Y (عشان القفزة تكون ثابتة)
        Vector3 currentVelocity = rb.linearVelocity;
        currentVelocity.y = 0;
        rb.linearVelocity = currentVelocity;
        
        // ⭐ تطبيق قوة القفز
        rb.AddForce(normalizedDirection * jumpForce, ForceMode.Impulse);
        
        // ⭐ تشغيل الصوت
        if (audioSource != null && jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound, soundVolume);
        }
        
        // ⭐ تفعيل التأثيرات البصرية
        if (enableVisualFeedback)
        {
            ActivateVisualFeedback();
        }
        
        // تحديث وقت آخر قفزة
        lastJumpTime = Time.time;
        
        Debug.Log($"🚀 Jump Pad activated! Force: {jumpForce}");
    }
    
    void ActivateVisualFeedback()
    {
        // تشغيل الأنيميشن
        if (animator != null && !string.IsNullOrEmpty(triggerAnimationName))
        {
            animator.Play(triggerAnimationName);
        }
        
        // تشغيل التأثير البصري
        if (visualEffect != null)
        {
            visualEffect.SetActive(true);
            Invoke(nameof(DeactivateVisualEffect), effectDuration);
        }
    }
    
    void DeactivateVisualEffect()
    {
        if (visualEffect != null)
        {
            visualEffect.SetActive(false);
        }
    }
    
    // دوال للتحكم من الكود
    public void SetJumpForce(float force)
    {
        jumpForce = force;
    }
    
    public void SetJumpDirection(Vector3 direction)
    {
        jumpDirection = direction;
        normalizedDirection = direction.normalized;
    }
    
    void OnDrawGizmosSelected()
    {
        // رسم اتجاه القفز
        Vector3 direction = normalizeDirection ? jumpDirection.normalized : jumpDirection;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + direction * 2f);
        Gizmos.DrawWireSphere(transform.position + direction * 2f, 0.3f);
        
        // رسم الموضع الحالي
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
