using UnityEngine;

/// <summary>
/// طفو متقدم - مع دوران وأنماط مختلفة
/// </summary>
public class AdvancedFloating : MonoBehaviour
{
    [Header("Float Movement")]
    [SerializeField] private Vector3 floatDirection = Vector3.up; // اتجاه الطفو
    [SerializeField] private float floatHeight = 0.5f; // مجال الطفو
    [SerializeField] private float floatSpeed = 1f; // سرعة الطفو
    
    [Header("Movement Pattern")]
    [SerializeField] private MovementPattern pattern = MovementPattern.Sine; // نوع الحركة
    
    [Header("Rotation")]
    [SerializeField] private bool enableRotation = false; // تفعيل الدوران
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // محور الدوران
    [SerializeField] private float rotationSpeed = 50f; // سرعة الدوران
    
    [Header("Bobbing (حركة إضافية)")]
    [SerializeField] private bool enableBobbing = false; // حركة ثانوية
    [SerializeField] private Vector3 bobbingDirection = Vector3.right; // اتجاه الحركة الثانوية
    [SerializeField] private float bobbingHeight = 0.2f; // مجال الحركة الثانوية
    [SerializeField] private float bobbingSpeed = 2f; // سرعة الحركة الثانوية
    
    [Header("Settings")]
    [SerializeField] private bool randomStartOffset = true;
    [SerializeField] private bool useLocalSpace = false; // استخدام Local Space بدل World Space
    
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float timeOffset;
    
    public enum MovementPattern
    {
        Sine,           // موجة سلسة (الافتراضي)
        Triangle,       // موجة مثلثية (خطية)
        Square,         // موجة مربعة (قفزات)
        Bounce,         // ارتداد
        SmoothBounce    // ارتداد سلس
    }
    
    void Start()
    {
        // حفظ الموضع والدوران الابتدائي
        if (useLocalSpace)
        {
            startPosition = transform.localPosition;
            startRotation = transform.localRotation;
        }
        else
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }
        
        // بداية عشوائية
        if (randomStartOffset)
        {
            timeOffset = Random.Range(0f, 2f * Mathf.PI);
        }
    }
    
    void Update()
    {
        // حساب الوقت
        float time = Time.time * floatSpeed + timeOffset;
        
        // حساب الطفو حسب النمط
        float floatAmount = CalculateFloatAmount(time);
        
        // تطبيق الطفو
        Vector3 floatOffset = floatDirection.normalized * floatAmount * floatHeight;
        
        // إضافة حركة ثانوية (Bobbing)
        if (enableBobbing)
        {
            float bobbingTime = Time.time * bobbingSpeed + timeOffset;
            float bobbingAmount = Mathf.Sin(bobbingTime) * bobbingHeight;
            floatOffset += bobbingDirection.normalized * bobbingAmount;
        }
        
        // تطبيق الموضع
        if (useLocalSpace)
        {
            transform.localPosition = startPosition + floatOffset;
        }
        else
        {
            transform.position = startPosition + floatOffset;
        }
        
        // تطبيق الدوران
        if (enableRotation)
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
        }
    }
    
    float CalculateFloatAmount(float time)
    {
        switch (pattern)
        {
            case MovementPattern.Sine:
                return Mathf.Sin(time);
                
            case MovementPattern.Triangle:
                // موجة مثلثية
                float triangleValue = (time % (2f * Mathf.PI)) / (2f * Mathf.PI);
                return (triangleValue < 0.5f) ? (triangleValue * 4f - 1f) : (3f - triangleValue * 4f);
                
            case MovementPattern.Square:
                // موجة مربعة
                return Mathf.Sign(Mathf.Sin(time));
                
            case MovementPattern.Bounce:
                // ارتداد
                float bounceValue = Mathf.Abs(Mathf.Sin(time));
                return bounceValue * bounceValue;
                
            case MovementPattern.SmoothBounce:
                // ارتداد سلس
                float smoothBounce = Mathf.Abs(Mathf.Sin(time));
                return Mathf.SmoothStep(0f, 1f, smoothBounce);
                
            default:
                return Mathf.Sin(time);
        }
    }
    
    // دالة لتغيير الإعدادات من الكود
    public void SetFloatHeight(float height)
    {
        floatHeight = height;
    }
    
    public void SetFloatSpeed(float speed)
    {
        floatSpeed = speed;
    }
    
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
    
    void OnDrawGizmosSelected()
    {
        // رسم مجال الطفو
        if (!Application.isPlaying)
        {
            startPosition = transform.position;
        }
        
        Gizmos.color = Color.cyan;
        
        // رسم الموضع الأعلى
        Vector3 topPosition = startPosition + floatDirection.normalized * floatHeight;
        Gizmos.DrawWireSphere(topPosition, 0.1f);
        
        // رسم الموضع الأسفل
        Vector3 bottomPosition = startPosition - floatDirection.normalized * floatHeight;
        Gizmos.DrawWireSphere(bottomPosition, 0.1f);
        
        // رسم الخط بينهم
        Gizmos.DrawLine(topPosition, bottomPosition);
        
        // رسم الموضع الحالي
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startPosition, 0.15f);
    }
}
