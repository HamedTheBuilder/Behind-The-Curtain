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
    
    [Header("Collection")]
    [SerializeField] private float flyToUISpeed = 15f; // سرعة الطيران للشاشة
    [SerializeField] private float shrinkSpeed = 10f; // سرعة التصغير
    [SerializeField] private Vector3 targetUIPosition = new Vector3(-8f, 4f, 0f); // موضع الزاوية (يسار أعلى)
    
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
    private Vector3 targetWorldPosition;
    private Camera mainCamera;
    
    void Start()
    {
        startPosition = transform.position;
        floatTimer = Random.Range(0f, 2f * Mathf.PI); // عشان ما يطفون كلهم بنفس الوقت
        mainCamera = Camera.main;
        
        // حساب الموضع العالمي للزاوية
        CalculateTargetPosition();
    }
    
    void CalculateTargetPosition()
    {
        if (mainCamera != null)
        {
            // تحويل من Screen Space لـ World Space
            Vector3 screenPos = new Vector3(Screen.width * 0.1f, Screen.height * 0.9f, 10f);
            targetWorldPosition = mainCamera.ScreenToWorldPoint(screenPos);
        }
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
            // الطيران للشاشة والتصغير
            FlyToUI();
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
    
    void FlyToUI()
    {
        // ⭐ الطيران باتجاه زاوية الشاشة
        transform.position = Vector3.Lerp(
            transform.position,
            targetWorldPosition,
            Time.deltaTime * flyToUISpeed
        );
        
        // ⭐ التصغير
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            Vector3.zero,
            Time.deltaTime * shrinkSpeed
        );
        
        // ⭐ الحذف عند الوصول
        if (transform.localScale.magnitude < 0.1f)
        {
            Destroy(gameObject);
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
        
        if (isCollected && mainCamera != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetWorldPosition);
        }
    }
}
