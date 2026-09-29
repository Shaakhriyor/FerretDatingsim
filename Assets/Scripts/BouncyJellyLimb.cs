using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BouncyJellyLimb : MonoBehaviour
{
    [Header("Hover / Float Settings")]
    [Tooltip("Enable or disable the continuous floating movement.")]
    public bool enableHover = true;
    [Tooltip("How far up and down (or left and right) the sprite moves.")]
    public float hoverAmplitude = 0.5f;
    [Tooltip("How fast the sprite moves up and down.")]
    public float hoverSpeed = 2f;
    [Tooltip("The direction of the hover movement. Default is up and down.")]
    public Vector3 hoverDirection = Vector3.up;

    [Header("Jelly Bounce Settings")]
    [Tooltip("If true, the jelly bounce will loop infinitely. If false, it only happens when triggered.")]
    public bool loopBounce = false;
    [Tooltip("How long to wait between bounces when Loop Bounce is enabled.")]
    public float timeBetweenLoops = 1f;
    [Tooltip("How fast the jelly oscillates.")]
    public float bounceSpeed = 15f;
    [Tooltip("How long it takes for the bounce to settle down.")]
    public float damping = 4f;
    [Tooltip("How drastically it squashes and stretches.")]
    public float elasticity = 0.3f;

    [Header("Shake Settings")]
    [Tooltip("Intensity of the jitter/shake effect.")]
    public float shakeIntensity = 0.1f;
    [Tooltip("How quickly the shake fades away.")]
    public float shakeDamping = 5f;

    [Header("Audio Settings - Loop")]
    [Tooltip("The sound that plays constantly in the background.")]
    public AudioClip loopingSound;
    [Range(0f, 1f)] public float loopVolume = 0.5f;

    [Header("Audio Settings - Random Bounces")]
    [Tooltip("Pool of sounds to pick from when a bounce triggers.")]
    public AudioClip[] randomBounceSounds;
    [Range(0f, 1f)] public float bounceVolume = 0.8f;

    // Internal state variables
    private Vector3 originalScale;
    private Vector3 basePosition;
    private Vector3 currentVelocity = Vector3.zero;
    private float shakeAmount = 0f;
    private float hoverTimer = 0f;
    private float loopTimer = 0f;

    // Audio sources
    private AudioSource loopSource;
    private AudioSource sfxSource;

    void Start()
    {
        originalScale = transform.localScale;
        basePosition = transform.localPosition;

        SetupAudio();

        // Trigger an initial bounce if loop is off, otherwise loop logic handles it
        if (!loopBounce)
        {
            TriggerJellyBounce();
        }
    }

    void Update()
    {
        HandleHoverMovement();
        HandleJellyPhysics();
        HandleShakePhysics();
        HandleLoopingBounce();
    }

    private void SetupAudio()
    {
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = true;
        loopSource.loop = true;
        loopSource.clip = loopingSound;
        loopSource.volume = loopVolume;

        if (loopingSound != null)
        {
            loopSource.Play();
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = bounceVolume;
    }

    private void HandleHoverMovement()
    {
        if (!enableHover) return;
        hoverTimer += Time.deltaTime * hoverSpeed;
    }

    private void HandleLoopingBounce()
    {
        if (!loopBounce) return;

        loopTimer += Time.deltaTime;
        if (loopTimer >= timeBetweenLoops)
        {
            TriggerJellyBounce();
            loopTimer = 0f; // Reset the loop clock
        }
    }

    private void HandleJellyPhysics()
    {
        Vector3 scaleForce = (originalScale - transform.localScale) * (bounceSpeed * bounceSpeed);
        currentVelocity += scaleForce * Time.deltaTime;
        currentVelocity -= currentVelocity * damping * Time.deltaTime;
        transform.localScale += currentVelocity * Time.deltaTime;
    }

    private void HandleShakePhysics()
    {
        float waveOffset = enableHover ? Mathf.Sin(hoverTimer) * hoverAmplitude : 0f;
        Vector3 currentHoverPos = basePosition + (hoverDirection.normalized * waveOffset);

        if (shakeAmount > 0)
        {
            Vector2 randomOffset = Random.insideUnitCircle * shakeAmount;
            transform.localPosition = currentHoverPos + new Vector3(randomOffset.x, randomOffset.y, 0f);
            shakeAmount -= Time.deltaTime * shakeDamping;
        }
        else
        {
            transform.localPosition = currentHoverPos;
        }
    }

    public void TriggerJellyBounce()
    {
        transform.localScale = new Vector3(
            originalScale.x * (1f - elasticity),
            originalScale.y * (1f + elasticity),
            originalScale.z
        );

        shakeAmount = shakeIntensity;
        PlayRandomBounceSound();
    }

    private void PlayRandomBounceSound()
    {
        if (randomBounceSounds == null || randomBounceSounds.Length == 0) return;

        int randomIndex = Random.Range(0, randomBounceSounds.Length);
        AudioClip selectedClip = randomBounceSounds[randomIndex];

        if (selectedClip != null)
        {
            sfxSource.PlayOneShot(selectedClip);
        }
    }
}