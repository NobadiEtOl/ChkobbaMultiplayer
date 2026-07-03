using UnityEngine;

public class ScreenEdgePositionAdjuster : MonoBehaviour
{
    public enum AnchorEdge
    {
        Left,
        Right
    }

    [System.Serializable]
    public class OffscreenElement
    {
        public string description;
        public Transform targetTransform;
        public AnchorEdge anchorEdge;
        [Tooltip("The manual padding/offset relative to the screen edge. If using sprite bounds, this is added on top.")]
        public float customPadding = 50f;
        [Tooltip("If true, automatically calculates the object's width using SpriteRenderer or RectTransform bounds to position it perfectly outside.")]
        public bool useSpriteBounds = true;
    }

    [System.Serializable]
    public class OnscreenReachPoint
    {
        public string description;
        public Transform targetTransform;
        public AnchorEdge anchorEdge;
        [Tooltip("How far inside the screen edge this point should remain (measured from design bounds)")]
        public float distanceInsideEdge;
    }

    [Header("Design Reference Resolution")]
    [SerializeField] private float designWidth = 1920f;
    [SerializeField] private float designHeight = 1080f;

    [Header("Offscreen Elements (Adjusted to stay outside)")]
    [SerializeField] private OffscreenElement[] offscreenElements;

    [Header("Onscreen Reach Points (Adjusted to stay inside)")]
    [SerializeField] private OnscreenReachPoint[] reachPoints;

    private void Awake()
    {
        AdjustPositions();
    }

    [ContextMenu("Adjust Positions Now")]
    public void AdjustPositions()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = Object.FindAnyObjectByType<Camera>();
        }

        if (cam == null)
        {
            Debug.LogError("[ScreenEdgePositionAdjuster] Main Camera not found!");
            return;
        }

        // 1. Calculate actual screen bounds of the device/window at runtime
        float actualAspect = cam.aspect;
        float actualWidthHalf = cam.orthographicSize * actualAspect;
        float actualRightEdgeX = cam.transform.position.x + actualWidthHalf;
        float actualLeftEdgeX = cam.transform.position.x - actualWidthHalf;

        Debug.Log($"[ScreenEdgePositionAdjuster] Actual screen edges: Left={actualLeftEdgeX:F2}, Right={actualRightEdgeX:F2}");

        // 2. Adjust Offscreen Elements
        foreach (var element in offscreenElements)
        {
            if (element.targetTransform == null) continue;

            float currentX = element.targetTransform.position.x;
            float newX = currentX;
            float halfWidth = 0f;

            if (element.useSpriteBounds)
            {
                halfWidth = GetSpriteHalfWidth(element.targetTransform);
            }

            if (element.anchorEdge == AnchorEdge.Right)
            {
                // Place the object's left edge perfectly outside the right screen edge
                newX = actualRightEdgeX + halfWidth + element.customPadding;
            }
            else if (element.anchorEdge == AnchorEdge.Left)
            {
                // Place the object's right edge perfectly outside the left screen edge
                newX = actualLeftEdgeX - halfWidth - element.customPadding;
            }

            Vector3 pos = element.targetTransform.position;
            element.targetTransform.position = new Vector3(newX, pos.y, pos.z);
            Debug.Log($"[ScreenEdgePositionAdjuster] Offscreen '{element.targetTransform.name}' set to X: {newX:F2} (halfWidth: {halfWidth:F2}, padding: {element.customPadding:F2})");
        }

        // 3. Adjust Onscreen Reach Points
        foreach (var point in reachPoints)
        {
            if (point.targetTransform == null) continue;

            float newX = point.targetTransform.position.x;

            if (point.anchorEdge == AnchorEdge.Right)
            {
                newX = actualRightEdgeX - point.distanceInsideEdge;
            }
            else if (point.anchorEdge == AnchorEdge.Left)
            {
                newX = actualLeftEdgeX + point.distanceInsideEdge;
            }

            Vector3 pos = point.targetTransform.position;
            point.targetTransform.position = new Vector3(newX, pos.y, pos.z);
            Debug.Log($"[ScreenEdgePositionAdjuster] Reach Point '{point.targetTransform.name}' set to X: {newX:F2}");
        }
    }

    private float GetSpriteHalfWidth(Transform t)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            return (sr.sprite.bounds.size.x * Mathf.Abs(t.lossyScale.x)) / 2f;
        }

        var rectTransform = t.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return (rectTransform.rect.width * Mathf.Abs(t.lossyScale.x)) / 2f;
        }

        return 0f;
    }
}



