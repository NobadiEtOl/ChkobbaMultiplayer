using UnityEngine;

/// <summary>
/// Fix 1 + 2: Global shader time throttle with per-material phase staggering.
///
/// This script broadcasts a single stepped time value to ALL shaders via
/// Shader.SetGlobalFloat("_GlobalShaderTime", ...). Shaders only recalculate
/// when this value changes, which happens at <updatesPerSecond> per second
/// instead of the full GPU frame rate.
///
/// Phase staggering ensures materials don't all recalculate on the same frame,
/// spreading the GPU cost evenly across frames.
///
/// Setup:
///   1. Add this component to any persistent GameObject (e.g. a GameManager).
///   2. Assign every animated shader material to the managedMaterials list.
///   3. In each shader, declare "float _GlobalShaderTime;" and "float _TimePhase;"
///      (already added by ShaderTimeManager), and replace _Time.y usages with
///      (_GlobalShaderTime + _TimePhase).
/// </summary>
public class ShaderTimeManager : MonoBehaviour
{
    [Header("Throttle Settings")]
    [Tooltip("How many times per second shaders recalculate. 15 = ~75% GPU savings vs 60fps.")]
    [SerializeField] private int updatesPerSecond = 15;

    [Header("Phase Staggering")]
    [Tooltip("All animated shader materials. Each gets a unique time offset so they don't all update on the same frame.")]
    [SerializeField] private Material[] managedMaterials;

    private static readonly int GlobalShaderTimeID = Shader.PropertyToID("_GlobalShaderTime");
    private static readonly int TimePhaseID        = Shader.PropertyToID("_TimePhase");

    private void Start()
    {
        AssignPhaseOffsets();
    }

    private void Update()
    {
        float interval = 1f / Mathf.Max(1, updatesPerSecond);
        float steppedTime = Mathf.Floor(Time.time / interval) * interval;
        Shader.SetGlobalFloat(GlobalShaderTimeID, steppedTime);
    }

    /// <summary>
    /// Distributes phase offsets evenly across managed materials so their
    /// recalculation frames are spread out rather than all hitting at once.
    /// </summary>
    private void AssignPhaseOffsets()
    {
        if (managedMaterials == null || managedMaterials.Length == 0)
            return;

        float frameInterval = 1f / Mathf.Max(1, updatesPerSecond);
        int count = managedMaterials.Length;

        for (int i = 0; i < count; i++)
        {
            if (managedMaterials[i] == null)
                continue;

            // Spread offsets evenly across one update interval
            float phase = (float)i / count * frameInterval;
            managedMaterials[i].SetFloat(TimePhaseID, phase);
        }
    }

    /// <summary>
    /// Call this at runtime (e.g. from a settings menu) to change the shader
    /// update rate without restarting.
    /// </summary>
    public void SetUpdatesPerSecond(int fps)
    {
        updatesPerSecond = Mathf.Clamp(fps, 1, 60);
        AssignPhaseOffsets();
    }
}
