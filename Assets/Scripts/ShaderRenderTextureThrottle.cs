using UnityEngine;

/// <summary>
/// Fix 3: RenderTexture throttle for heavy background/decor shaders.
///
/// Renders the expensive shader to a RenderTexture at a low rate (default 10fps)
/// and displays the frozen RT on the mesh via a plain "Unlit/Texture" shader.
/// The GPU only runs the expensive shader N times per second instead of every frame.
///
/// Best candidates: EbruMarble table, MasaShader table, TreeSmokeShader background.
/// These are large objects that touch many pixels — highest savings per material.
///
/// Setup:
///   1. Attach this component to any GameObject that has a Renderer using
///      a heavy procedural shader.
///   2. (Optional) Assign a "Display Shader" in the Inspector.
///      Defaults to "Unlit/Texture" which is always available in Unity.
///   3. Set renderTextureWidth/Height to match the visual detail you need.
///      256 or 512 is usually sufficient for background decor.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ShaderRenderTextureThrottle : MonoBehaviour
{
    [Header("Throttle Settings")]
    [Tooltip("How many times per second the expensive shader re-renders.")]
    [SerializeField] private int updatesPerSecond = 10;

    [Header("RenderTexture Resolution")]
    [SerializeField] private int renderTextureWidth  = 512;
    [SerializeField] private int renderTextureHeight = 512;

    [Header("Display Shader")]
    [Tooltip("Leave empty to use 'Unlit/Texture' automatically.")]
    [SerializeField] private Shader displayShader;

    private Renderer   _renderer;
    private Material   _sourceMaterial;   // owns the expensive shader
    private Material   _displayMaterial;  // plain texture, shown on the mesh
    private RenderTexture _renderTexture;
    private float      _nextUpdateTime;

    private void Start()
    {
        _renderer = GetComponent<Renderer>();

        // Clone the shared material so we can drive it independently.
        // Using sharedMaterial for the read so we don't accidentally instance it.
        _sourceMaterial = new Material(_renderer.sharedMaterial);

        // Create the RenderTexture
        _renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 0, RenderTextureFormat.ARGB32)
        {
            filterMode  = FilterMode.Bilinear,
            wrapMode    = TextureWrapMode.Clamp,
            anisoLevel  = 0
        };
        _renderTexture.Create();

        // Build the plain display material
        Shader shader = displayShader != null ? displayShader : Shader.Find("Unlit/Texture");
        if (shader == null)
        {
            Debug.LogWarning($"[ShaderRenderTextureThrottle] Could not find display shader on {name}. " +
                             "Assign one manually in the Inspector.");
            return;
        }

        _displayMaterial              = new Material(shader);
        _displayMaterial.mainTexture  = _renderTexture;

        // First blit — ensures the mesh is never shown blank on frame 1
        BakeToRenderTexture();

        // Swap this renderer over to the lightweight display material
        _renderer.sharedMaterial = _displayMaterial;
    }

    private void Update()
    {
        if (Time.time >= _nextUpdateTime)
        {
            _nextUpdateTime = Time.time + 1f / Mathf.Max(1, updatesPerSecond);
            BakeToRenderTexture();
        }
    }

    private void BakeToRenderTexture()
    {
        // Texture2D.whiteTexture is used as a dummy source since these shaders
        // are fully procedural and don't sample an input texture.
        Graphics.Blit(Texture2D.whiteTexture, _renderTexture, _sourceMaterial);
    }

    /// <summary>
    /// Change the bake rate at runtime (e.g. from a quality settings menu).
    /// </summary>
    public void SetUpdatesPerSecond(int fps)
    {
        updatesPerSecond = Mathf.Clamp(fps, 1, 60);
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
        if (_displayMaterial != null)
            Destroy(_displayMaterial);
        if (_sourceMaterial != null)
            Destroy(_sourceMaterial);
    }
}
