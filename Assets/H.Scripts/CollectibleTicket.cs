using UnityEngine;

/// <summary>
/// تذكرة قابلة للجمع - تطفو وتدور وتطير للشاشة
/// </summary>
public class CollectibleTicket : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] private float floatHeight = 0.3f; // ارتفاع الطفو
    [SerializeField] private float floatSpeed = 2f; // سرعة الطفو
    [SerializeField] private float rotationSpeed = 50f; // سرعة الدوران
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // محور الدوران
    
    [Header("Collection Animation")]
    [SerializeField] private float scaleUpAmount = 1.2f; // مقدار التكبير عند الجمع (رقم > 1 للتكبير)
    [SerializeField] private float scaleUpDuration = 0.15f; // مدة التكبير
    [SerializeField] private float fadeDuration = 0.4f; // مدة الاختفاء التدريجي
    
    [Header("Audio")]
    [SerializeField] private AudioClip collectSound; // صوت الجمع
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;
    
    [Header("Particle Effect")]
    [SerializeField] private GameObject particleEffectPrefab; // الـ Prefab
    [SerializeField] private bool spawnParticleOnCollect = true;
    
    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer;
    
    private Vector3 startPosition;
    private float floatTimer;
    private bool isCollected = false;
    
    private Renderer[] ticketRenderers;
    private float collectionTimer = 0f;
    private Vector3 originalScale;
    
    void Start()
    {
        startPosition = transform.position;
        floatTimer = Random.Range(0f, 2f * Mathf.PI); // عشان ما يطفون كلهم بنفس الوقت
        
        ticketRenderers = GetComponentsInChildren<Renderer>();
        originalScale = transform.localScale;
    }
    

    
    void Update()
    {
        if (!isCollected)
        {
            // الطفو والدوران
            FloatAndRotate();
        }
        else
        {
            // التكبير والاختفاء التدريجي
            AnimateCollection();
        }
    }
    
    void FloatAndRotate()
    {
        // ⭐ الطفو لأعلى وأسفل
        floatTimer += Time.deltaTime * floatSpeed;
        float newY = startPosition.y + Mathf.Sin(floatTimer) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        
        // ⭐ الدوران
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
    }
    
    void AnimateCollection()
    {
        collectionTimer += Time.deltaTime;
        
        // ⭐ التكبير أولاً
        if (collectionTimer <= scaleUpDuration)
        {
            float t = collectionTimer / scaleUpDuration;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * scaleUpAmount, t);
        }
        else // ⭐ بعدين الاختفاء التدريجي وتصغيره للصفر
        {
            float fadeProgress = (collectionTimer - scaleUpDuration) / fadeDuration;
            
            if (fadeProgress <= 1f)
            {
                // فقط نغير الشفافية (Fade Out) بدون تصغير الحجم للصفر
                foreach (Renderer r in ticketRenderers)
                {
                    if (r != null && r.material.HasProperty("_Color"))
                    {
                        Color c = r.material.color;
                        c.a = Mathf.Lerp(1f, 0f, fadeProgress);
                        r.material.color = c;
                    }
                }
            }
            else
            {
                // انتهى الأنيميشن، نحذف العنصر
                Destroy(gameObject);
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // التحقق من اللاعب
        if (((1 << other.gameObject.layer) & playerLayer) != 0 && !isCollected)
        {
            CollectTicket();
        }
    }
    
    void CollectTicket()
    {
        isCollected = true;
        
        // ⭐ تشغيل الصوت
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
        }
        
        // ⭐ تشغيل Particle Effect
        if (spawnParticleOnCollect && particleEffectPrefab != null)
        {
            GameObject particles = Instantiate(particleEffectPrefab, transform.position, Quaternion.identity);
            Destroy(particles, 3f); // حذف بعد 3 ثواني
        }
        
        // ⭐ إضافة للعداد
        TicketCounter counter = FindObjectOfType<TicketCounter>();
        if (counter != null)
        {
            counter.AddTicket();
        }
        else
        {
            Debug.LogWarning("TicketCounter not found in scene!");
        }
        
        // ⭐ تعطيل الكولايدر عشان ما ينجمع مرتين
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        Debug.Log("🎫 Ticket collected!");
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = isCollected ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
