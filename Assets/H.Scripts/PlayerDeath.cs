using UnityEngine;
using System.Collections;

/// <summary>
/// نظام موت اللاعب - تأثير، صوت، إعادة ظهور
/// </summary>
public class PlayerDeath : MonoBehaviour
{
    [Header("Death Effect")]
    [SerializeField] private Animator curtainAnimator; // ⭐ أنيميتور الستارة
    [SerializeField] private string curtainAnimationName = "CurtainClose"; // اسم أنيميشن الستارة
    [SerializeField] private GameObject curtainObject; // ⭐ أوبجكت الستارة

    [SerializeField] private CanvasGroup blackScreenCanvasGroup; // ⭐ الشاشة السوداء (CanvasGroup للـ Fade)
    [SerializeField] private float fadeInDuration = 2f; // مدة ظهور الشاشة السوداء
    [SerializeField] private float fadeOutDuration = 2f; // مدة اختفاء الشاشة السوداء
    [SerializeField] private GameObject blackScreenCanvas; // Canvas الشاشة السوداء

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip deathSound; // صوت الموت
    [SerializeField][Range(0f, 1f)] private float deathSoundVolume = 1f;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 1f; // ثانية واحدة قبل الإعادة

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody playerRb;

    private bool isDead = false;

    void Start()
    {
        // Auto-find components
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (playerRb == null)
            playerRb = GetComponent<Rigidbody>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // إعداد AudioSource
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
        }

        // ⭐ إخفاء الستارة في البداية
        if (curtainObject != null)
        {
            curtainObject.SetActive(false);
        }

        // ⭐ إعداد الشاشة السوداء
        if (blackScreenCanvas != null)
        {
            blackScreenCanvas.SetActive(true); // نبقيه مفعّل لكن شفاف
        }

        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 0; // شفاف في البداية
        }
    }

    public void Die()
    {
        if (isDead) return; // لا نموت مرتين

        isDead = true;

        Debug.Log("Player died!");

        // ⭐ تعطيل التحكم - البحث على الـ Root
        Transform rootTransform = transform.root;
        PlayerController rootController = rootTransform.GetComponent<PlayerController>();

        if (rootController != null)
        {
            rootController.enabled = false;
        }
        else if (playerController != null) // fallback للـ Child
        {
            playerController.enabled = false;
        }

        // ⭐ تشغيل صوت الموت
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound, deathSoundVolume);
            Debug.Log("Playing death sound");
        }

        // ⭐ تشغيل أنيميشن الستارة
        if (curtainObject != null)
        {
            curtainObject.SetActive(true);
        }

        if (curtainAnimator != null)
        {
            curtainAnimator.Play(curtainAnimationName);
            Debug.Log("Playing curtain animation");
        }

        // ⭐ بدء Fade In للشاشة السوداء
        StartCoroutine(FadeBlackScreen());

        // ⭐ الانتظار ثم إعادة الظهور
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        Respawn();
    }

    // ⭐ Fade In و Fade Out للشاشة السوداء
    IEnumerator FadeBlackScreen()
    {
        if (blackScreenCanvasGroup == null)
        {
            Debug.LogWarning("BlackScreenCanvasGroup not assigned!");
            yield break;
        }

        // ⭐ Fade In - ظهور تدريجي (ثانيتين)
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            blackScreenCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeInDuration);
            yield return null;
        }

        blackScreenCanvasGroup.alpha = 1; // تأكد من الوصول للنهاية

        Debug.Log("Black screen fade in complete");

        // الانتظار حتى Respawn
        yield return new WaitForSeconds(respawnDelay);

        // ⭐ Fade Out - اختفاء تدريجي (ثانيتين)
        elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            blackScreenCanvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / fadeOutDuration);
            yield return null;
        }

        blackScreenCanvasGroup.alpha = 0; // تأكد من الشفافية الكاملة

        Debug.Log("Black screen fade out complete");
    }

    void Respawn()
    {
        Debug.Log("Respawning player...");

        // ⭐ الحصول على الـ Root Transform (Pivot Parent)
        Transform rootTransform = transform.root;

        // ⭐ الحصول على آخر Checkpoint
        CheckpointManager manager = FindObjectOfType<CheckpointManager>();

        if (manager != null && manager.GetCurrentCheckpoint() != null)
        {
            Vector3 respawnPos = manager.GetCurrentCheckpoint().GetRespawnPosition();

            // ⭐ إعادة الـ Root (Pivot) للموضع بدل Child
            rootTransform.position = respawnPos;

            Debug.Log($"Respawned root at checkpoint: {respawnPos}");
        }
        else
        {
            Debug.LogWarning("No checkpoint found! Respawning at current position");
        }

        // ⭐ إعادة ضبط الفيزياء - البحث على الـ Root
        Rigidbody rootRb = rootTransform.GetComponent<Rigidbody>();
        if (rootRb != null)
        {
            rootRb.linearVelocity = Vector3.zero;
            rootRb.angularVelocity = Vector3.zero;
        }
        else if (playerRb != null) // fallback للـ Child
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        // ⭐ إعادة تفعيل التحكم - البحث على الـ Root
        PlayerController rootController = rootTransform.GetComponent<PlayerController>();
        if (rootController != null)
        {
            rootController.enabled = true;
        }
        else if (playerController != null) // fallback للـ Child
        {
            playerController.enabled = true;
        }

        // ⭐ إخفاء الستارة
        if (curtainObject != null)
        {
            curtainObject.SetActive(false);
        }

        // ⭐ التأكد من أن الشاشة السوداء شفافة (يتم بواسطة Coroutine)

        isDead = false;

        Debug.Log("Player respawned successfully");
    }

    public bool IsDead()
    {
        return isDead;
    }
}