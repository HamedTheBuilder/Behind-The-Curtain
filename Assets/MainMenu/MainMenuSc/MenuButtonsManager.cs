using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// نظام Main Menu الكامل - مع أزرار 3D
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Video Intro")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private VideoClip introVideo;
    [SerializeField] private float videoDuration = 5f;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Curtains Animation")]
    [SerializeField] private Animator curtainsAnimator;
    [SerializeField] private string curtainCloseAnimationName = "CurtainClose";
    [SerializeField] private string curtainOpenAnimationName = "CurtainOpen";

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuParent; // Parent للأزرار
    [SerializeField] private WoodenButton3D[] menuButtons; // الأزرار الخشبية 3D
    [SerializeField] private float buttonDropDelay = 0.3f;

    [Header("Character")]
    [SerializeField] private GameObject characterObject;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string idleAnimationName = "Idle";

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private WoodenButton3D[] settingsButtons;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Button languageButton;
    [SerializeField] private Text languageText;

    [Header("Credits Panel")]
    [SerializeField] private GameObject creditsPanel;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource characterVoiceSource;

    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip[] characterVoiceLines;
    [SerializeField] private float voiceLineInterval = 10f;

    [Header("Scene Loading")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Camera")]
    [SerializeField] private Camera menuCamera;

    private bool isTransitioning = false;
    private int currentLanguage = 0;
    private string[] languageNames = { "English", "العربية" };

    void Start()
    {
        SetupInitialState();
        StartCoroutine(IntroSequence());
    }

    void Update()
    {
        HandleButtonClicks();
    }

    void SetupInitialState()
    {
        if (mainMenuParent != null)
            mainMenuParent.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        if (characterObject != null)
            characterObject.SetActive(false);

        if (fadeImage != null)
            fadeImage.color = new Color(0, 0, 0, 0);

        if (videoPlayer != null && introVideo != null)
        {
            videoPlayer.clip = introVideo;
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        }

        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.value = RenderSettings.ambientIntensity;
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }

        if (languageButton != null)
            languageButton.onClick.AddListener(OnLanguageClicked);
    }

    IEnumerator IntroSequence()
    {
        // 1. الفيديو
        if (videoPanel != null)
            videoPanel.SetActive(true);

        if (videoPlayer != null && introVideo != null)
        {
            videoPlayer.Play();
            yield return new WaitForSeconds(videoDuration);
            videoPlayer.Stop();
        }

        // 2. Fade + إغلاق الستائر
        StartCoroutine(FadeToBlack());

        if (curtainsAnimator != null)
        {
            curtainsAnimator.Play(curtainCloseAnimationName);
        }

        yield return new WaitForSeconds(fadeDuration);

        if (videoPanel != null)
            videoPanel.SetActive(false);

        // 3. فتح الستائر
        if (curtainsAnimator != null)
        {
            curtainsAnimator.Play(curtainOpenAnimationName);
        }

        // 4. Fade from Black
        yield return StartCoroutine(FadeFromBlack());

        // 5. إظهار Main Menu
        if (mainMenuParent != null)
            mainMenuParent.SetActive(true);

        // 6. إظهار الشخصية + Idle
        if (characterObject != null)
        {
            characterObject.SetActive(true);
        }

        if (characterAnimator != null)
        {
            characterAnimator.Play(idleAnimationName);
        }

        // 7. إسقاط الأزرار
        yield return StartCoroutine(DropButtons());

        // 8. موسيقى الخلفية
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        // 9. أصوات الشخصية
        StartCoroutine(PlayRandomVoiceLines());

        // 10. تأثير الهواء
        StartCoroutine(WindEffect());

        Debug.Log("✅ Main Menu ready!");
    }

    IEnumerator FadeToBlack()
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = elapsed / fadeDuration;
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }

    IEnumerator FadeFromBlack()
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = 1f - (elapsed / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
    }

    IEnumerator DropButtons()
    {
        if (menuButtons == null) yield break;

        foreach (WoodenButton3D btn in menuButtons)
        {
            if (btn != null)
            {
                btn.Drop();
                PlayButtonSound();
                yield return new WaitForSeconds(buttonDropDelay);
            }
        }
    }

    IEnumerator WindEffect()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);

            if (menuButtons != null)
            {
                foreach (WoodenButton3D btn in menuButtons)
                {
                    if (btn != null && btn.rb != null && !btn.rb.isKinematic)
                    {
                        float windForce = Random.Range(-0.5f, 0.5f);
                        btn.rb.AddForce(new Vector3(windForce, 0, 0), ForceMode.Impulse);
                    }
                }
            }
        }
    }

    IEnumerator PlayRandomVoiceLines()
    {
        while (true)
        {
            yield return new WaitForSeconds(voiceLineInterval);

            if (characterVoiceLines != null && characterVoiceLines.Length > 0)
            {
                AudioClip randomVoice = characterVoiceLines[Random.Range(0, characterVoiceLines.Length)];

                if (randomVoice != null && characterVoiceSource != null)
                {
                    characterVoiceSource.PlayOneShot(randomVoice);
                }
            }
        }
    }

    // ⭐ التحقق من النقر على الأزرار 3D
    void HandleButtonClicks()
    {
        if (isTransitioning) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = menuCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                WoodenButton3D clickedButton = hit.collider.GetComponent<WoodenButton3D>();

                if (clickedButton != null && clickedButton.isInteractable)
                {
                    OnButtonClicked(clickedButton);
                }
            }
        }
    }

    void OnButtonClicked(WoodenButton3D button)
    {
        PlayButtonSound();

        switch (button.buttonType)
        {
            case ButtonType.Play:
                OnPlayClicked();
                break;
            case ButtonType.Settings:
                OnSettingsClicked();
                break;
            case ButtonType.Credits:
                OnCreditsClicked();
                break;
            case ButtonType.Quit:
                OnQuitClicked();
                break;
            case ButtonType.Back:
                OnBackFromSettings();
                break;
        }
    }

    void OnPlayClicked()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        StartCoroutine(TransitionToGame());
    }

    IEnumerator TransitionToGame()
    {
        if (curtainsAnimator != null)
        {
            curtainsAnimator.Play(curtainCloseAnimationName);
        }

        yield return StartCoroutine(FadeToBlack());

        SceneManager.LoadScene(gameSceneName);
    }

    void OnSettingsClicked()
    {
        StartCoroutine(ShowSettings());
    }

    IEnumerator ShowSettings()
    {
        // سحب الأزرار الرئيسية
        if (menuButtons != null)
        {
            foreach (WoodenButton3D btn in menuButtons)
            {
                if (btn != null)
                {
                    btn.PullUp();
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        // إظهار Settings Panel
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        // إسقاط أزرار الإعدادات
        if (settingsButtons != null)
        {
            foreach (WoodenButton3D btn in settingsButtons)
            {
                if (btn != null)
                {
                    btn.Drop();
                    yield return new WaitForSeconds(buttonDropDelay);
                }
            }
        }
    }

    void OnCreditsClicked()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
        }
    }

    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnLanguageClicked()
    {
        PlayButtonSound();

        currentLanguage = (currentLanguage + 1) % languageNames.Length;

        if (languageText != null)
        {
            languageText.text = languageNames[currentLanguage];
        }
    }

    void OnBackFromSettings()
    {
        StartCoroutine(HideSettings());
    }

    IEnumerator HideSettings()
    {
        // سحب أزرار الإعدادات
        if (settingsButtons != null)
        {
            foreach (WoodenButton3D btn in settingsButtons)
            {
                if (btn != null)
                {
                    btn.PullUp();
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // إسقاط الأزرار الرئيسية
        if (menuButtons != null)
        {
            foreach (WoodenButton3D btn in menuButtons)
            {
                if (btn != null)
                {
                    btn.Drop();
                    yield return new WaitForSeconds(buttonDropDelay);
                }
            }
        }
    }

    void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    void OnBrightnessChanged(float value)
    {
        RenderSettings.ambientIntensity = value;
    }

    void PlayButtonSound()
    {
        if (buttonClickSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(buttonClickSound);
        }
    }
}

/// <summary>
/// أنواع الأزرار
/// </summary>
public enum ButtonType
{
    Play,
    Settings,
    Credits,
    Quit,
    Volume,
    Brightness,
    Language,
    Back
}

/// <summary>
/// زر خشبي 3D بفيزياء
/// </summary>
[System.Serializable]
public class WoodenButton3D : MonoBehaviour
{
    [Header("Button Settings")]
    public ButtonType buttonType;
    public bool isInteractable = false;

    [Header("Components")]
    public Rigidbody rb;
    public HingeJoint hingeJoint;
    public Collider buttonCollider;

    [Header("Physics")]
    public float gravityScale = 9.8f;
    public float drag = 0.5f;
    public float angularDrag = 2f;
    public float swingForce = 5f;

    [Header("Positions")]
    public Transform ropeAnchor; // نقطة التعليق (الحبل)

    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isDropped = false;

    void Awake()
    {
        // حفظ الموضع الأولي
        startPosition = transform.position;
        startRotation = transform.rotation;

        // إعداد Rigidbody
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
        }

        rb.mass = 1f;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.useGravity = false; // سنطبق الجاذبية يدوياً
        rb.isKinematic = true;

        // إعداد HingeJoint
        if (hingeJoint == null)
        {
            hingeJoint = GetComponent<HingeJoint>();
            if (hingeJoint == null && ropeAnchor != null)
            {
                hingeJoint = gameObject.AddComponent<HingeJoint>();
                hingeJoint.connectedBody = null;
                hingeJoint.anchor = new Vector3(0, 0.5f, 0); // أعلى اللوح
                hingeJoint.axis = new Vector3(0, 0, 1); // دوران على Z
                hingeJoint.autoConfigureConnectedAnchor = false;
                hingeJoint.connectedAnchor = ropeAnchor.position;
            }
        }

        if (hingeJoint != null)
        {
            hingeJoint.enableCollision = false;
        }

        // إعداد Collider
        if (buttonCollider == null)
        {
            buttonCollider = GetComponent<Collider>();
            if (buttonCollider == null)
            {
                buttonCollider = gameObject.AddComponent<BoxCollider>();
            }
        }
    }

    public void Drop()
    {
        if (isDropped) return;

        // رفع للأعلى
        transform.position = startPosition + Vector3.up * 10f;

        // تفعيل الفيزياء
        rb.isKinematic = false;
        rb.useGravity = true;

        // تأرجح أولي
        float randomSwing = Random.Range(-swingForce, swingForce);
        rb.AddTorque(new Vector3(0, 0, randomSwing), ForceMode.Impulse);

        isDropped = true;

        // تفعيل التفاعل بعد ثانية
        StartCoroutine(EnableInteractionAfterDelay(1f));
    }

    public void PullUp()
    {
        StartCoroutine(PullUpCoroutine());
    }

    IEnumerator PullUpCoroutine()
    {
        isInteractable = false;

        rb.isKinematic = true;
        rb.useGravity = false;

        Vector3 targetPos = startPosition + Vector3.up * 10f;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, targetPos, elapsed / duration);
            yield return null;
        }

        isDropped = false;
    }

    IEnumerator EnableInteractionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isInteractable = true;
    }

    void OnMouseEnter()
    {
        if (isInteractable)
        {
            // تغيير اللون أو إضافة Highlight
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.yellow;
            }
        }
    }

    void OnMouseExit()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = Color.white;
        }
    }
}