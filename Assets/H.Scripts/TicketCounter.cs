using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// عداد التذاكر في الشاشة
/// </summary>
public class TicketCounter : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI counterText; // Text للعداد
    [SerializeField] private Text counterTextLegacy; // للـ Text العادي (إذا ما عندك TextMeshPro)
    [SerializeField] private Image ticketIcon; // أيقونة التذكرة
    
    [Header("Animation")]
    [SerializeField] private bool animateOnCollect = true;
    [SerializeField] private float punchScale = 1.3f; // حجم الـ Punch
    [SerializeField] private float punchDuration = 0.3f; // مدة الأنيميشن
    
    [Header("Settings")]
    [SerializeField] private string textFormat = "x {0}"; // صيغة النص
    [SerializeField] private int startingTickets = 0; // التذاكر الابتدائية
    
    private int ticketCount = 0;
    private Vector3 originalScale;
    private float punchTimer = 0f;
    private bool isPunching = false;
    
    void Start()
    {
        ticketCount = startingTickets;
        
        // حفظ الحجم الأصلي
        if (counterText != null)
        {
            originalScale = counterText.transform.localScale;
        }
        else if (counterTextLegacy != null)
        {
            originalScale = counterTextLegacy.transform.localScale;
        }
        
        UpdateUI();
    }
    
    void Update()
    {
        // تحديث أنيميشن Punch
        if (isPunching)
        {
            punchTimer += Time.deltaTime;
            float progress = punchTimer / punchDuration;
            
            if (progress >= 1f)
            {
                // انتهى الأنيميشن
                isPunching = false;
                ResetScale();
            }
            else
            {
                // Punch Effect - يكبر ثم يصغر
                float scale = 1f + Mathf.Sin(progress * Mathf.PI) * (punchScale - 1f);
                ApplyScale(originalScale * scale);
            }
        }
    }
    
    // ⭐ دالة إضافة تذكرة
    public void AddTicket()
    {
        ticketCount++;
        UpdateUI();
        
        if (animateOnCollect)
        {
            PlayPunchAnimation();
        }
    }
    
    // دالة إضافة عدة تذاكر
    public void AddTickets(int amount)
    {
        ticketCount += amount;
        UpdateUI();
        
        if (animateOnCollect)
        {
            PlayPunchAnimation();
        }
    }
    
    // دالة حذف تذكرة (للاستخدام المستقبلي)
    public void RemoveTicket()
    {
        if (ticketCount > 0)
        {
            ticketCount--;
            UpdateUI();
        }
    }
    
    // دالة إعادة ضبط العداد
    public void ResetCounter()
    {
        ticketCount = 0;
        UpdateUI();
    }
    
    // تحديث الـ UI
    void UpdateUI()
    {
        string displayText = string.Format(textFormat, ticketCount);
        
        if (counterText != null)
        {
            counterText.text = displayText;
        }
        else if (counterTextLegacy != null)
        {
            counterTextLegacy.text = displayText;
        }
        
        Debug.Log($"Tickets: {ticketCount}");
    }
    
    // تشغيل أنيميشن Punch
    void PlayPunchAnimation()
    {
        isPunching = true;
        punchTimer = 0f;
    }
    
    void ApplyScale(Vector3 scale)
    {
        if (counterText != null)
        {
            counterText.transform.localScale = scale;
        }
        else if (counterTextLegacy != null)
        {
            counterTextLegacy.transform.localScale = scale;
        }
    }
    
    void ResetScale()
    {
        ApplyScale(originalScale);
    }
    
    // دالة للحصول على عدد التذاكر
    public int GetTicketCount()
    {
        return ticketCount;
    }
}
