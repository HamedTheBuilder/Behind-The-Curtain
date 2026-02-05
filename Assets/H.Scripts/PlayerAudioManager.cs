using UnityEngine;
using System.Collections;

/// <summary>
/// نظام الصوتيات الكامل للاعب
/// </summary>
public class PlayerAudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource footstepSource; // خطوات
    [SerializeField] private AudioSource actionSource; // قفز، هبوط
    [SerializeField] private AudioSource voiceSource; // صوت اللاعب
    [SerializeField] private AudioSource ropeSource; // صوت الحبل
    
    [Header("Footstep Sounds")]
    [SerializeField] private AudioClip[] walkFootsteps; // أصوات المشي
    [SerializeField] private AudioClip[] runFootsteps; // أصوات الجري
    [SerializeField] [Range(0f, 1f)] private float footstepVolume = 0.5f;
    [SerializeField] private float walkStepInterval = 0.5f; // كل نص ثانية
    [SerializeField] private float runStepInterval = 0.3f; // أسرع
    
    [Header("Jump & Landing")]
    [SerializeField] private AudioClip[] jumpSounds;
    [SerializeField] private AudioClip[] landSounds;
    [SerializeField] [Range(0f, 1f)] private float jumpVolume = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float landVolume = 0.8f;
    
    [Header("Rope Sounds")]
    [SerializeField] private AudioClip ropeGrabSound; // صوت الإمساك بالحبل
    [SerializeField] private AudioClip ropeSwingSound; // صوت التأرجح (loop)
    [SerializeField] [Range(0f, 1f)] private float ropeVolume = 0.6f;
    [SerializeField] private float ropeGrabDelay = 0.15f; // ⭐ تأخير 15 ثانية
    
    [Header("Random Voice Lines")]
    [SerializeField] private AudioClip[] randomVoiceLines; // صوتيات عشوائية
    [SerializeField] [Range(0f, 1f)] private float voiceVolume = 0.7f;
    [SerializeField] private float voiceInterval = 20f; // ⭐ كل 20 ثانية
    [SerializeField] private float voiceIntervalVariation = 5f; // تنويع ±5 ثواني
    
    [Header("Landing Particle")]
    [SerializeField] private GameObject landParticlePrefab; // ⭐ Particle عند الهبوط
    [SerializeField] private Transform groundCheckPoint; // نقطة فحص الأرض
    [SerializeField] private float particleYOffset = 0.1f;
    
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private UltimateRopeGrabber ropeGrabber;
    
    // Private variables
    private float footstepTimer;
    private float nextVoiceTime;
    private bool wasGrounded;
    private bool wasGrabbingRope;
    private bool hasPlayedRopeGrabSound;
    private float ropeGrabTimer;
    
    void Start()
    {
        // Auto-find references
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        
        if (ropeGrabber == null)
            ropeGrabber = GetComponent<UltimateRopeGrabber>();
        
        // Create audio sources if not assigned
        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = 0.5f;
        }
        
        if (actionSource == null)
        {
            actionSource = gameObject.AddComponent<AudioSource>();
            actionSource.playOnAwake = false;
            actionSource.spatialBlend = 0.5f;
        }
        
        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0.3f;
        }
        
        if (ropeSource == null)
        {
            ropeSource = gameObject.AddComponent<AudioSource>();
            ropeSource.playOnAwake = false;
            ropeSource.spatialBlend = 0.5f;
        }
        
        // Setup ground check point
        if (groundCheckPoint == null)
        {
            GameObject checkPoint = new GameObject("GroundCheckPoint");
            checkPoint.transform.SetParent(transform);
            checkPoint.transform.localPosition = new Vector3(0, -1, 0);
            groundCheckPoint = checkPoint.transform;
        }
        
        // Schedule first voice line
        ScheduleNextVoiceLine();
        
        wasGrounded = playerController.IsGrounded();
    }
    
    void Update()
    {
        HandleFootsteps();
        HandleJumpAndLanding();
        HandleRopeSounds();
        HandleRandomVoiceLines();
    }
    
    // ⭐ أصوات الخطوات
    void HandleFootsteps()
    {
        if (playerController == null) return;
        
        bool isMoving = playerController.IsMoving();
        bool isGrounded = playerController.IsGrounded();
        bool isSprinting = playerController.IsSprinting();
        bool isCrouching = playerController.IsCrouching();
        
        // فقط إذا كان يتحرك وعلى الأرض
        if (isMoving && isGrounded && !isCrouching)
        {
            footstepTimer += Time.deltaTime;
            
            float currentInterval = isSprinting ? runStepInterval : walkStepInterval;
            
            if (footstepTimer >= currentInterval)
            {
                PlayFootstepSound(isSprinting);
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }
    
    void PlayFootstepSound(bool isRunning)
    {
        AudioClip[] clips = isRunning ? runFootsteps : walkFootsteps;
        
        if (clips == null || clips.Length == 0)
            return;
        
        AudioClip randomClip = clips[Random.Range(0, clips.Length)];
        
        if (randomClip != null && footstepSource != null)
        {
            footstepSource.PlayOneShot(randomClip, footstepVolume);
        }
    }
    
    // ⭐ القفز والهبوط
    void HandleJumpAndLanding()
    {
        if (playerController == null) return;
        
        bool isGrounded = playerController.IsGrounded();
        float verticalVelocity = playerController.GetVerticalVelocity();
        
        // صوت القفز - عند المغادرة من الأرض
        if (wasGrounded && !isGrounded && verticalVelocity > 0.5f)
        {
            PlayJumpSound();
        }
        
        // صوت الهبوط - عند الوصول للأرض
        if (!wasGrounded && isGrounded)
        {
            PlayLandSound();
            SpawnLandParticle();
        }
        
        wasGrounded = isGrounded;
    }
    
    void PlayJumpSound()
    {
        if (jumpSounds == null || jumpSounds.Length == 0)
            return;
        
        AudioClip randomClip = jumpSounds[Random.Range(0, jumpSounds.Length)];
        
        if (randomClip != null && actionSource != null)
        {
            actionSource.PlayOneShot(randomClip, jumpVolume);
        }
    }
    
    void PlayLandSound()
    {
        if (landSounds == null || landSounds.Length == 0)
            return;
        
        AudioClip randomClip = landSounds[Random.Range(0, landSounds.Length)];
        
        if (randomClip != null && actionSource != null)
        {
            actionSource.PlayOneShot(randomClip, landVolume);
        }
    }
    
    // ⭐ Particle عند الهبوط
    void SpawnLandParticle()
    {
        if (landParticlePrefab == null || groundCheckPoint == null)
            return;
        
        Vector3 spawnPosition = groundCheckPoint.position + Vector3.up * particleYOffset;
        
        GameObject particle = Instantiate(landParticlePrefab, spawnPosition, Quaternion.identity);
        Destroy(particle, 3f);
    }
    
    // ⭐ أصوات الحبل
    void HandleRopeSounds()
    {
        if (ropeGrabber == null) return;
        
        bool isGrabbing = ropeGrabber.IsGrabbing();
        
        // بداية الإمساك بالحبل
        if (!wasGrabbingRope && isGrabbing)
        {
            // بدء العداد
            ropeGrabTimer = 0f;
            hasPlayedRopeGrabSound = false;
        }
        
        // أثناء الإمساك بالحبل
        if (isGrabbing)
        {
            ropeGrabTimer += Time.deltaTime;
            
            // ⭐ تشغيل الصوت بعد 15 ثانية
            if (!hasPlayedRopeGrabSound && ropeGrabTimer >= ropeGrabDelay)
            {
                PlayRopeGrabSound();
                hasPlayedRopeGrabSound = true;
            }
            
            // صوت التأرجح المستمر
            if (ropeSwingSound != null && !ropeSource.isPlaying)
            {
                ropeSource.clip = ropeSwingSound;
                ropeSource.loop = true;
                ropeSource.volume = ropeVolume;
                ropeSource.Play();
            }
        }
        else
        {
            // إيقاف صوت التأرجح
            if (ropeSource.isPlaying)
            {
                ropeSource.Stop();
            }
            
            ropeGrabTimer = 0f;
            hasPlayedRopeGrabSound = false;
        }
        
        wasGrabbingRope = isGrabbing;
    }
    
    void PlayRopeGrabSound()
    {
        if (ropeGrabSound != null && ropeSource != null)
        {
            ropeSource.PlayOneShot(ropeGrabSound, ropeVolume);
            Debug.Log("🎵 Rope grab sound played after delay!");
        }
    }
    
    // ⭐ صوتيات عشوائية كل 20 ثانية
    void HandleRandomVoiceLines()
    {
        if (randomVoiceLines == null || randomVoiceLines.Length == 0)
            return;
        
        if (Time.time >= nextVoiceTime && !voiceSource.isPlaying)
        {
            PlayRandomVoiceLine();
            ScheduleNextVoiceLine();
        }
    }
    
    void PlayRandomVoiceLine()
    {
        AudioClip randomClip = randomVoiceLines[Random.Range(0, randomVoiceLines.Length)];
        
        if (randomClip != null && voiceSource != null)
        {
            voiceSource.PlayOneShot(randomClip, voiceVolume);
            Debug.Log($"🗣️ Playing random voice line: {randomClip.name}");
        }
    }
    
    void ScheduleNextVoiceLine()
    {
        float variation = Random.Range(-voiceIntervalVariation, voiceIntervalVariation);
        nextVoiceTime = Time.time + voiceInterval + variation;
    }
    
    // ⭐ دوال عامة للتحكم
    public void PlayCustomSound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && actionSource != null)
        {
            actionSource.PlayOneShot(clip, volume);
        }
    }
    
    public void SetFootstepVolume(float volume)
    {
        footstepVolume = Mathf.Clamp01(volume);
    }
    
    public void SetMasterVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        
        if (footstepSource != null) footstepSource.volume = clampedVolume;
        if (actionSource != null) actionSource.volume = clampedVolume;
        if (voiceSource != null) voiceSource.volume = clampedVolume;
        if (ropeSource != null) ropeSource.volume = clampedVolume;
    }
}
