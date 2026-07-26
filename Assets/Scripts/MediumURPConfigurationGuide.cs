using UnityEngine;

public class MediumURPConfigurationGuide : MonoBehaviour
{
    [ContextMenu("Show Complete Medium URP Configuration Guide")]
    public void ShowCompleteGuide()
    {
        var guide = @"
=== COMPLETE MEDIUM URP CONFIGURATION GUIDE ===

STEP 1: CHANGE SCRIPTABLE RENDER PIPELINE SETTINGS
1. Go to: Edit → Project Settings → Graphics
2. In 'Scriptable Render Pipeline Settings' section:
   - Click the dropdown/selector
   - Select: Medium_PipelineAsset (Universal Render Pipeline Asset)
   - This changes the global render pipeline for your entire project

STEP 2: CONFIGURE MEDIUM URP ASSET
1. Select: Assets/Settings/Medium_PipelineAsset.asset
2. In Inspector, configure these settings for mobile optimization:
   - Render Scale: 0.8 (for better performance)
   - MSAA: 1 (disable for better performance)
   - HDR: False (better mobile compatibility)
   - Shadow Distance: 30 (reduce for performance)
   - Shadow Cascade Count: 1 (reduce for performance)
   - Supports Light Cookies: True
   - Supports Dynamic Batching: True
   - Supports GPU Instancing: True
   - Supports SRP Batcher: True

STEP 3: CONFIGURE MEDIUM RENDERER DATA
1. Select: Assets/Settings/Medium_PipelineAsset_ForwardRenderer.asset
2. In Inspector, configure:
   - Opaque Layer Mask: Everything
   - Transparent Layer Mask: Everything
   - Default Stencil State: Default
   - Shadow Transparent Receive: True

STEP 4: ADD CRT FEATURE TO MEDIUM RENDERER
1. In the same Medium_PipelineAsset_ForwardRenderer.asset:
2. Scroll down to 'Renderer Features' section
3. Click 'Add Renderer Feature'
4. Select: CRT Filter (or similar CRT feature)
5. Enable the feature ✅
6. Configure CRT settings if available:
   - Scanline Intensity: 0.5
   - Distortion Strength: 0.1
   - Aberration Offset: 0.01

STEP 5: UPDATE ALL CAMERAS
1. Select your Main Camera
2. In Camera component:
   - Set 'Renderer' to: Medium_PipelineAsset_ForwardRenderer
   - Or set to 'Use Pipeline Default' (recommended)
   - Set 'HDR Rendering' to: Off
   - Enable 'Post Processing' ✅

STEP 6: UPDATE GRAPHICS SETTINGS
1. Go to: Edit → Project Settings → Graphics
2. Verify 'Scriptable Render Pipeline Settings' shows: Medium_PipelineAsset
3. In 'Always Included Shaders' section:
   - Make sure 'Hidden/CRTFilter' is still in the list
   - If not, add it back

STEP 7: UPDATE QUALITY SETTINGS
1. Go to: Edit → Project Settings → Quality
2. For each quality level (especially Android):
   - Set 'Rendering' to: Medium_PipelineAsset
   - This ensures consistent URP usage across quality levels

STEP 8: VERIFY URP GLOBAL SETTINGS
1. Go to: Edit → Project Settings → XR Plug-in Management → URP Global Settings
2. If you have a URP Global Settings asset:
   - Make sure it's configured for Medium URP
   - Verify 'Strip Debug Variants' is disabled ✅

STEP 9: CHECK BUILD SETTINGS
1. Go to: File → Build Settings
2. Select Android platform
3. In Player Settings:
   - Graphics APIs: OpenGLES3 (should be fine)
   - Color Space: Linear (should be fine)
   - Strip Engine Code: True (can keep enabled)

STEP 10: VERIFY CONFIGURATION
1. Run the CRTSetupVerifier script to check everything
2. Look for these confirmations:
   - ✅ URP Asset: Medium_PipelineAsset
   - ✅ Renderer Data: Medium_PipelineAsset_ForwardRenderer
   - ✅ CRT Renderer Feature found and enabled
   - ✅ Camera using correct renderer

STEP 11: TEST IN EDITOR
1. Play the scene in Editor
2. Check Console for any renderer warnings
3. Verify CRT effect is visible
4. Check performance in Profiler

STEP 12: BUILD AND TEST
1. Build for Android
2. Install on device
3. Test CRT effect
4. Check performance

=== TROUBLESHOOTING ===

If you still see renderer warnings:
- Make sure Medium_PipelineAsset is selected in Graphics Settings
- Verify camera is set to 'Use Pipeline Default'
- Check that Medium_PipelineAsset_ForwardRenderer exists

If CRT effect doesn't work:
- Verify CRT feature is added to Medium renderer
- Check that 'Hidden/CRTFilter' is in Always Included Shaders
- Ensure 'Strip Debug Variants' is disabled

If performance is still poor:
- Reduce Render Scale to 0.7 or 0.6
- Disable MSAA completely
- Reduce Shadow Distance further
- Consider disabling some renderer features

=== END GUIDE ===";

        
    }
}
