using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class VNDoctorIntro : MonoBehaviour
{
    [Header("Drag these from the doctor scene")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Shader blurShader;

    [Header("Effect")]
    [Tooltip("Keep ringing and blur active until a dialogue line with End Doctor Intro begins. Total Duration is ignored in this mode.")]
    [SerializeField] private bool waitForDialogueLine;
    [Min(0f)] [SerializeField] private float totalDuration = 8f;
    [Min(0f)] [SerializeField] private float fadeOutDuration = 1f;
    [Range(0f, 1f)] [SerializeField] private float ringingVolume = 0.08f;
    [Range(0f, 12f)] [SerializeField] private float blurStrength = 5f;

    private AudioSource ringing;
    private float elapsed;
    private bool initialized;
    private bool waitingForLine;
    private bool redirecting;
    private Material blurMaterial;
    private RenderTexture sceneTexture, horizontalTexture, blurredTexture;
    private GameObject screenObject;
    private UnityEngine.UI.RawImage screenImage;
    private RenderTexture originalTarget;
    private RenderMode originalCanvasMode;
    private Camera originalCanvasCamera;
    private float originalPlaneDistance;
    private int originalMenuOrder, originalMenuLayer;

    public float SecondsRemaining => Mathf.Max(0f, totalDuration - elapsed);
    private float Strength => waitingForLine ? 1f : EvaluateStrength(elapsed, totalDuration, fadeOutDuration);

    public static float EvaluateStrength(float time, float duration, float fade)
    {
        duration = Mathf.Max(0f, duration);
        fade = Mathf.Clamp(fade, 0f, duration);
        if (time >= duration) return 0f;
        if (fade <= 0f) return 1f;
        float t = Mathf.Clamp01((duration - time) / fade);
        return t * t * (3f - 2f * t);
    }

    private void Awake()
    {
        ringing = GetComponent<AudioSource>();
        ringing.playOnAwake = false;
        ringing.loop = false;
        ringing.spatialBlend = 0f;
        ringing.ignoreListenerPause = false;
        ringing.Stop();
    }

    private void OnEnable()
    {
        Camera.onPostRender += AfterBuiltInCamera;
        RenderPipelineManager.endCameraRendering += AfterPipelineCamera;
    }

    private void Start()
    {
        // A cross-scene save can restore this before Start is called.
        if (!initialized) Begin();
    }

    private void Begin()
    {
        initialized = true;
        elapsed = 0f;
        waitingForLine = waitForDialogueLine;
        totalDuration = Mathf.Max(0f, totalDuration);
        fadeOutDuration = waitingForLine ? Mathf.Max(0f, fadeOutDuration) : Mathf.Clamp(fadeOutDuration, 0f, totalDuration);
        ringing.loop = waitingForLine;
        if (!waitingForLine && totalDuration <= 0f) { FinishImmediately(); return; }
        StartBlur();
        PlayRinging(0);
    }

    private void Update()
    {
        if (VNSceneTransition.IsBusy) return;
        if (!initialized || (!waitingForLine && elapsed >= totalDuration)) return;
        if (Time.timeScale == 0f || AudioListener.pause ||
            (VNDialogueManager.Instance != null && VNDialogueManager.Instance.IsPaused)) return;
        if (!waitingForLine) elapsed = Mathf.Min(totalDuration, elapsed + Time.deltaTime);
        ringing.volume = ringingVolume * Strength;
        if (!waitingForLine && elapsed >= totalDuration) FinishImmediately();
    }

    private void PlayRinging(int sample)
    {
        ringing.Stop();
        ringing.volume = ringingVolume * Strength;
        if (ringing.clip == null || (!waitingForLine && elapsed >= totalDuration)) return;
        ringing.Play();
        ringing.timeSamples = Mathf.Clamp(sample, 0, Mathf.Max(0, ringing.clip.samples - 1));
    }

    public void FadeOutNow()
    {
        // The first dialogue line can be displayed before this component's Start.
        if (!initialized) Begin();
        if (!waitingForLine && elapsed >= totalDuration) return;
        if (waitingForLine)
        {
            waitingForLine = false;
            totalDuration = Mathf.Max(0f, fadeOutDuration);
            elapsed = 0f;
        }
        else
        {
            // Repeated triggers never restart or lengthen a fade already in progress.
            elapsed = Mathf.Max(elapsed, totalDuration - fadeOutDuration);
        }
        // Keep looping through this short fade if the clip reaches its end.
        // FinishImmediately stops it and clears loop when the fade completes.
        if (fadeOutDuration <= 0f) FinishImmediately();
    }

    public void FinishImmediately()
    {
        initialized = true;
        waitingForLine = false;
        elapsed = Mathf.Max(0f, totalDuration);
        if (ringing != null) { ringing.Stop(); ringing.loop = false; ringing.volume = 0f; }
        StopBlur();
    }

    public VNDoctorIntroSnapshot CaptureSave(string key)
    {
        return new VNDoctorIntroSnapshot {
            key = key, elapsed = elapsed, duration = totalDuration,
            fade = fadeOutDuration, volume = ringingVolume, blur = blurStrength,
            samples = ringing != null ? ringing.timeSamples : 0,
            playing = ringing != null && ringing.isPlaying,
            waitingForLine = waitingForLine,
            looping = ringing != null && ringing.loop
        };
    }

    public void RestoreSave(VNDoctorIntroSnapshot state)
    {
        initialized = true;
        waitingForLine = state.waitingForLine;
        totalDuration = Mathf.Max(0f, state.duration);
        fadeOutDuration = waitingForLine ? Mathf.Max(0f, state.fade) : Mathf.Clamp(state.fade, 0f, totalDuration);
        elapsed = Mathf.Clamp(state.elapsed, 0f, totalDuration);
        ringingVolume = Mathf.Clamp01(state.volume);
        blurStrength = Mathf.Clamp(state.blur, 0f, 12f);
        ringing.loop = state.looping || waitingForLine;
        if (!waitingForLine && elapsed >= totalDuration) { FinishImmediately(); return; }
        StartBlur();
        ringing.Stop();
        if (state.playing) PlayRinging(state.samples);
    }

    private void StartBlur()
    {
        if (redirecting || blurStrength <= 0f) return;
        if (sceneCamera == null || dialogueCanvas == null || menuCanvas == null || blurShader == null ||
            dialogueCanvas == menuCanvas || !dialogueCanvas.isRootCanvas || !menuCanvas.isRootCanvas ||
            menuCanvas.renderMode != RenderMode.ScreenSpaceOverlay || sceneCamera.targetTexture != null)
        {
            Debug.LogError("Doctor blur: assign Main Camera, the root Dialogue canvas, the separate overlay VNHeartMenuCanvas, and VNDoctorBlur shader. The camera must not already use a Target Texture.", this);
            return;
        }
        if (!blurShader.isSupported)
        {
            Debug.LogError("The doctor blur shader is unsupported. Check the shader's Inspector for errors.", this);
            return;
        }

        originalTarget = sceneCamera.targetTexture;
        originalCanvasMode = dialogueCanvas.renderMode;
        originalCanvasCamera = dialogueCanvas.worldCamera;
        originalPlaneDistance = dialogueCanvas.planeDistance;
        originalMenuOrder = menuCanvas.sortingOrder;
        originalMenuLayer = menuCanvas.sortingLayerID;
        redirecting = true;
        blurMaterial = new Material(blurShader) { hideFlags = HideFlags.HideAndDontSave };

        // Render the scene and its dialogue together, then show that image blurred.
        // Menu controls are on their own overlay canvas and remain sharp.
        dialogueCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        dialogueCanvas.worldCamera = sceneCamera;
        dialogueCanvas.planeDistance = Mathf.Lerp(sceneCamera.nearClipPlane,
            sceneCamera.farClipPlane, 0.01f);

        screenObject = new GameObject("DoctorBlurScreen (Runtime)", typeof(RectTransform), typeof(Canvas));
        screenObject.transform.SetParent(transform, false);
        var displayCanvas = screenObject.GetComponent<Canvas>();
        displayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        displayCanvas.sortingOrder = 30000;
        displayCanvas.sortingLayerID = 0;
        menuCanvas.sortingLayerID = 0;
        menuCanvas.sortingOrder = 30001;

        var imageObject = new GameObject("Blurred Scene", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(UnityEngine.UI.RawImage));
        imageObject.transform.SetParent(screenObject.transform, false);
        screenImage = imageObject.GetComponent<UnityEngine.UI.RawImage>();
        screenImage.raycastTarget = false;
        screenImage.color = Color.white;
        var rect = screenImage.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        AllocateTextures();
        Canvas.ForceUpdateCanvases();
    }

    private void LateUpdate()
    {
        if (redirecting && (sceneTexture.width != Mathf.Max(1, Screen.width) ||
            sceneTexture.height != Mathf.Max(1, Screen.height))) AllocateTextures();
    }

    private void AllocateTextures()
    {
        sceneCamera.targetTexture = originalTarget;
        ReleaseTextures();
        int width = Mathf.Max(1, Screen.width), height = Mathf.Max(1, Screen.height);
        sceneTexture = MakeTexture(width, height, 24, "Doctor scene");
        horizontalTexture = MakeTexture(width, height, 0, "Doctor horizontal blur");
        blurredTexture = MakeTexture(width, height, 0, "Doctor finished blur");
        sceneCamera.targetTexture = sceneTexture;
        screenImage.texture = blurredTexture;
    }

    private static RenderTexture MakeTexture(int width, int height, int depth, string label)
    {
        var texture = new RenderTexture(width, height, depth, RenderTextureFormat.ARGB32) {
            name = label, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave, antiAliasing = 1
        };
        texture.Create();
        var previous = RenderTexture.active;
        RenderTexture.active = texture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = previous;
        return texture;
    }

    private void AfterBuiltInCamera(Camera camera)
    {
        if (GraphicsSettings.currentRenderPipeline == null) RenderBlur(camera);
    }

    private void AfterPipelineCamera(ScriptableRenderContext context, Camera camera)
    {
        RenderBlur(camera);
    }

    private void RenderBlur(Camera camera)
    {
        if (!redirecting || camera != sceneCamera || blurMaterial == null) return;
        var previous = RenderTexture.active;
        try
        {
            float radius = blurStrength * Strength * sceneTexture.height / 1080f;
            blurMaterial.SetVector("_BlurDirection", new Vector4(radius, 0f, 0f, 0f));
            Graphics.Blit(sceneTexture, horizontalTexture, blurMaterial, 0);
            blurMaterial.SetVector("_BlurDirection", new Vector4(0f, radius, 0f, 0f));
            Graphics.Blit(horizontalTexture, blurredTexture, blurMaterial, 0);
        }
        finally { RenderTexture.active = previous; }
    }

    private void StopBlur()
    {
        if (!redirecting) return;
        redirecting = false;
        if (screenObject != null) { screenObject.SetActive(false); Destroy(screenObject); }
        if (sceneCamera != null) sceneCamera.targetTexture = originalTarget;
        if (dialogueCanvas != null)
        {
            dialogueCanvas.renderMode = originalCanvasMode;
            dialogueCanvas.worldCamera = originalCanvasCamera;
            dialogueCanvas.planeDistance = originalPlaneDistance;
        }
        if (menuCanvas != null)
        {
            menuCanvas.sortingOrder = originalMenuOrder;
            menuCanvas.sortingLayerID = originalMenuLayer;
        }
        ReleaseTextures();
        if (blurMaterial != null) Destroy(blurMaterial);
    }

    private void ReleaseTextures()
    {
        if (sceneTexture != null) { sceneTexture.Release(); Destroy(sceneTexture); }
        if (horizontalTexture != null) { horizontalTexture.Release(); Destroy(horizontalTexture); }
        if (blurredTexture != null) { blurredTexture.Release(); Destroy(blurredTexture); }
        sceneTexture = horizontalTexture = blurredTexture = null;
    }

    private void OnDisable()
    {
        Camera.onPostRender -= AfterBuiltInCamera;
        RenderPipelineManager.endCameraRendering -= AfterPipelineCamera;
        if (ringing != null) ringing.Stop();
        StopBlur();
    }
}
