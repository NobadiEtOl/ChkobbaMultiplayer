using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class CanvasScreenSync : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Camera _mainCamera;
    
    private float _lastOrthoSize;
    private float _lastAspect;
    private Vector3 _lastCamPos;
    private Quaternion _lastCamRot;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        SyncSize();
    }

    private void LateUpdate()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera == null) return;

        // Check for changes to trigger sync
        if (_mainCamera.orthographicSize != _lastOrthoSize || 
            _mainCamera.aspect != _lastAspect ||
            _mainCamera.transform.position != _lastCamPos ||
            _mainCamera.transform.rotation != _lastCamRot)
        {
            SyncSize();
        }
    }

    public void SyncSize()
    {
        if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null || _rectTransform == null) return;

        _lastOrthoSize = _mainCamera.orthographicSize;
        _lastAspect = _mainCamera.aspect;
        _lastCamPos = _mainCamera.transform.position;
        _lastCamRot = _mainCamera.transform.rotation;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) return;

        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            if (_mainCamera.orthographic)
            {
                float height = 2f * _mainCamera.orthographicSize;
                float width = height * _mainCamera.aspect;

                // Match camera rotation to ensure sizeDelta axes align with viewport axes
                transform.rotation = _mainCamera.transform.rotation;

                // Maintain the current distance from the camera along its forward vector
                Vector3 camPos = _mainCamera.transform.position;
                Vector3 camForward = _mainCamera.transform.forward;
                Vector3 canvasPos = transform.position;
                
                float distance = Vector3.Dot(canvasPos - camPos, camForward);
                
                // If distance is too small or behind, use a default far distance or keep current
                // For a background, we typically want it far away but within far clip plane.
                // If it was at Y = -2000 and camera at Y = 24644, distance is 26644.
                
                // Position the canvas to be centered in the camera's view at that distance
                transform.position = camPos + camForward * distance;

                // Set size and scale. 
                // We set localScale to 1 and sizeDelta to world dimensions.
                _rectTransform.localScale = Vector3.one;
                _rectTransform.sizeDelta = new Vector2(width, height);
            }
        }
        else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvas.worldCamera = _mainCamera;
            canvas.planeDistance = 10f; // Default
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncSize();
    }
#endif
}
