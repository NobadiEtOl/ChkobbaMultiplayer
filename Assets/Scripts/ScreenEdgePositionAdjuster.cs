using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// On Start, projects each configured object to a screen edge using the camera,
/// copies that position to the group's reach point transforms, then returns the
/// object to its original position.
///
/// Usage:
/// 1. Add a group for each object (InfoBox, HesapMakinesi, Kese, etc.).
/// 2. Assign the object's Transform, choose which edge it slides from,
///    assign an Edge Anchor (a child transform on the object that should
///    touch the edge), and assign one or more Reach Point transforms
///    (empty GameObjects that will receive the computed position).
/// 3. Controllers retrieve their reach point via GetReachPointTransform("Name").
/// </summary>
public class ScreenEdgePositionAdjuster : MonoBehaviour
{
    public enum ScreenEdge { Left, Right, Top, Bottom }

    [System.Serializable]
    public class EdgeGroup
    {
        public string groupName;
        public Transform objectTransform;
        public ScreenEdge edge;

        [Tooltip("A child transform on the object whose position should align to the assigned edge.")]
        public Transform edgeAnchor;

        [Tooltip("Transform that will receive the computed world position (used as reach point by other scripts).")]
        public Transform reachPoint;

        [Tooltip("The opposite-side anchor on the object that should align to the edge when moving out of screen.")]
        public Transform outlineAnchor;

        [Tooltip("Transform that will receive the computed outside position (used as outside reach point by other scripts).")]
        public Transform outlineReachPoint;
    }

    [SerializeField] private Camera targetCamera;
    [SerializeField] private List<EdgeGroup> groups = new List<EdgeGroup>();

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogError("[ScreenEdgePositionAdjuster] No camera found. Assign targetCamera or tag your camera as MainCamera.");
            return;
        }

        Debug.Log("[ScreenEdgePositionAdjuster] Awake() - Starting position calculations for all groups");
        foreach (var group in groups)
            AdjustGroup(group);
        Debug.Log("[ScreenEdgePositionAdjuster] Awake() - All position calculations complete");
    }

    private void AdjustGroup(EdgeGroup group)
    {
        if (group.objectTransform == null)
        {
            Debug.LogWarning($"[ScreenEdgePositionAdjuster] Group '{group.groupName}': objectTransform is not assigned.");
            return;
        }
        if (group.edgeAnchor == null)
        {
            Debug.LogWarning($"[ScreenEdgePositionAdjuster] Group '{group.groupName}': edgeAnchor is not assigned.");
            return;
        }

        Debug.Log($"[ScreenEdgePositionAdjuster] AdjustGroup: Starting adjustment for group '{group.groupName}' (Edge: {group.edge})");

        // Snapshot of current world positions
        Vector3 originalObjectPos = group.objectTransform.position;
        Debug.Log($"[ScreenEdgePositionAdjuster] AdjustGroup: Original object position: {originalObjectPos}");

        // Anchor offset relative to the object (in world space at rest position)
        Vector3 anchorOffset = group.edgeAnchor.position - originalObjectPos;
        Debug.Log($"[ScreenEdgePositionAdjuster] AdjustGroup: Edge anchor offset: {anchorOffset}");

        // Project object's depth onto camera space so the edge lies on the same plane
        float depth = targetCamera.WorldToViewportPoint(originalObjectPos).z;
        Debug.Log($"[ScreenEdgePositionAdjuster] AdjustGroup: Camera depth: {depth}");

        // Determine viewport coordinates for the chosen edge
        float vx, vy;
        switch (group.edge)
        {
            case ScreenEdge.Left:   vx = 0f;   vy = 0.5f; break;
            case ScreenEdge.Right:  vx = 1f;   vy = 0.5f; break;
            case ScreenEdge.Bottom: vx = 0.5f; vy = 0f;   break;
            case ScreenEdge.Top:    vx = 0.5f; vy = 1f;   break;
            default:                vx = 0.5f; vy = 0.5f; break;
        }

        Debug.Log($"[ScreenEdgePositionAdjuster] AdjustGroup: Viewport coordinates for edge: vx={vx}, vy={vy}");

        Vector3 edgeWorldPos = targetCamera.ViewportToWorldPoint(new Vector3(vx, vy, depth));
        Debug.Log($"[ScreenEdgePositionAdjuster] AdjustGroup: Edge world position: {edgeWorldPos}");

        // Shift only the axis that corresponds to the chosen edge (inside position)
        Vector3 newObjectPos = originalObjectPos;
        if (group.edge == ScreenEdge.Left || group.edge == ScreenEdge.Right)
            newObjectPos.x = edgeWorldPos.x - anchorOffset.x;
        else
            newObjectPos.y = edgeWorldPos.y - anchorOffset.y;

        Debug.Log($"[ScreenEdgePositionAdjuster] AdjustGroup: Inside reach point calculated: {newObjectPos}");

        // Copy inside position to the reach point
        if (group.reachPoint != null)
        {
            group.reachPoint.position = newObjectPos;
            Debug.Log($"[ScreenEdgePositionAdjuster] AdjustGroup: Set reachPoint '{group.reachPoint.name}' to {newObjectPos}");
        }
        else
        {
            Debug.LogWarning($"[ScreenEdgePositionAdjuster] AdjustGroup: reachPoint is not assigned for group '{group.groupName}'");
        }

        // Calculate outside position if outline anchor is assigned
        if (group.outlineAnchor != null && group.outlineReachPoint != null)
        {
            Vector3 outlineAnchorOffset = group.outlineAnchor.position - originalObjectPos;
            Debug.Log($"[ScreenEdgePositionAdjuster] AdjustGroup: Outline anchor offset: {outlineAnchorOffset}");
            
            Vector3 outsidePos = CalculateOutsidePosition(group, edgeWorldPos, originalObjectPos, outlineAnchorOffset);
            group.outlineReachPoint.position = outsidePos;
            Debug.Log($"[ScreenEdgePositionAdjuster] AdjustGroup: Set outlineReachPoint '{group.outlineReachPoint.name}' to {outsidePos}");
        }
        else
        {
            if (group.outlineAnchor == null)
                Debug.LogWarning($"[ScreenEdgePositionAdjuster] AdjustGroup: outlineAnchor is not assigned for group '{group.groupName}'");
            if (group.outlineReachPoint == null)
                Debug.LogWarning($"[ScreenEdgePositionAdjuster] AdjustGroup: outlineReachPoint is not assigned for group '{group.groupName}'");
        }

        // Return the object to where it started
        group.objectTransform.position = originalObjectPos;
        Debug.Log($"[ScreenEdgePositionAdjuster] AdjustGroup: Completed - object returned to {originalObjectPos}");
    }

    /// <summary>
    /// Calculates the outside position where the object should be positioned so that
    /// its outline anchor aligns with the same assigned screen edge.
    /// </summary>
    private Vector3 CalculateOutsidePosition(EdgeGroup group, Vector3 edgeWorldPos, Vector3 originalObjectPos, Vector3 outlineAnchorOffset)
    {
        Debug.Log($"[ScreenEdgePositionAdjuster] CalculateOutsidePosition: Starting for group '{group.groupName}'");
        
        // For outside position, we use the SAME edge but with the outline anchor instead of edge anchor
        float depth = targetCamera.WorldToViewportPoint(originalObjectPos).z;
        Debug.Log($"[ScreenEdgePositionAdjuster] CalculateOutsidePosition: Depth: {depth}");
        
        float vx, vy;
        switch (group.edge)
        {
            case ScreenEdge.Left:   vx = 0f;   vy = 0.5f; break;  // Same: Left
            case ScreenEdge.Right:  vx = 1f;   vy = 0.5f; break;  // Same: Right
            case ScreenEdge.Bottom: vx = 0.5f; vy = 0f;   break;  // Same: Bottom
            case ScreenEdge.Top:    vx = 0.5f; vy = 1f;   break;  // Same: Top
            default:                vx = 0.5f; vy = 0.5f; break;
        }

        Debug.Log($"[ScreenEdgePositionAdjuster] CalculateOutsidePosition: Using viewport coords for same edge: vx={vx}, vy={vy}");

        Vector3 sameEdgeWorldPos = targetCamera.ViewportToWorldPoint(new Vector3(vx, vy, depth));
        Debug.Log($"[ScreenEdgePositionAdjuster] CalculateOutsidePosition: Same edge world position: {sameEdgeWorldPos}");
        Debug.Log($"[ScreenEdgePositionAdjuster] CalculateOutsidePosition: Outline anchor offset: {outlineAnchorOffset}");

        Vector3 outsideObjectPos = originalObjectPos;
        if (group.edge == ScreenEdge.Left || group.edge == ScreenEdge.Right)
            outsideObjectPos.x = sameEdgeWorldPos.x - outlineAnchorOffset.x;
        else
            outsideObjectPos.y = sameEdgeWorldPos.y - outlineAnchorOffset.y;

        Debug.Log($"[ScreenEdgePositionAdjuster] CalculateOutsidePosition: Final outside position: {outsideObjectPos}");
        return outsideObjectPos;
    }

    /// <summary>
    /// Returns the reach point Transform for a given group.
    /// Searches by group name instead of transform name to avoid name mismatch issues.
    /// Used by controllers (HesapMakinesiController, KeseController, SuperPowerSpawner, etc.)
    /// to locate their assigned reach point at runtime.
    /// </summary>
    public Transform GetReachPointTransform(string groupName)
    {
        Debug.Log($"[ScreenEdgePositionAdjuster] GetReachPointTransform: Searching for group '{groupName}'");
        foreach (var group in groups)
        {
            if (group.groupName == groupName)
            {
                if (group.reachPoint != null)
                {
                    Debug.Log($"[ScreenEdgePositionAdjuster] GetReachPointTransform: FOUND group '{groupName}', returning reachPoint at {group.reachPoint.position}");
                    return group.reachPoint;
                }
                else
                {
                    Debug.LogError($"[ScreenEdgePositionAdjuster] GetReachPointTransform: Found group '{groupName}' but reachPoint is NULL");
                    return null;
                }
            }
        }
        Debug.LogError($"[ScreenEdgePositionAdjuster] GetReachPointTransform: Group '{groupName}' NOT FOUND (searched {groups.Count} groups)");
        return null;
    }

    /// <summary>
    /// Returns the outside reach point Transform for a given group.
    /// Searches by group name instead of transform name to avoid name mismatch issues.
    /// Used by controllers to locate their assigned outside reach point at runtime for positioning objects out of screen.
    /// </summary>
    public Transform GetOutsideReachPointTransform(string groupName)
    {
        Debug.Log($"[ScreenEdgePositionAdjuster] GetOutsideReachPointTransform: Searching for group '{groupName}'");
        foreach (var group in groups)
        {
            if (group.groupName == groupName)
            {
                if (group.outlineReachPoint != null)
                {
                    Debug.Log($"[ScreenEdgePositionAdjuster] GetOutsideReachPointTransform: FOUND group '{groupName}', returning outlineReachPoint at {group.outlineReachPoint.position}");
                    return group.outlineReachPoint;
                }
                else
                {
                    Debug.LogError($"[ScreenEdgePositionAdjuster] GetOutsideReachPointTransform: Found group '{groupName}' but outlineReachPoint is NULL");
                    return null;
                }
            }
        }
        Debug.LogError($"[ScreenEdgePositionAdjuster] GetOutsideReachPointTransform: Group '{groupName}' NOT FOUND (searched {groups.Count} groups)");
        return null;
    }

    /// <summary>
    /// Returns the reach point position (where object should be when brought in) as a Vector3.
    /// </summary>
    public Vector3 GetReachPointPosition(string groupName)
    {
        Debug.Log($"[ScreenEdgePositionAdjuster] GetReachPointPosition: Retrieving position for group '{groupName}'");
        Transform reachPoint = GetReachPointTransform(groupName);
        if (reachPoint != null)
        {
            Vector3 pos = reachPoint.position;
            Debug.Log($"[ScreenEdgePositionAdjuster] GetReachPointPosition: Returning {pos}");
            return pos;
        }
        Debug.LogError($"[ScreenEdgePositionAdjuster] GetReachPointPosition: Could not find reach point for group '{groupName}'");
        return Vector3.zero;
    }

    /// <summary>
    /// Returns the outside reach point position (where object should be when moved out) as a Vector3.
    /// This is the "return/starting position" for the object.
    /// </summary>
    public Vector3 GetOutsideReachPointPosition(string groupName)
    {
        Debug.Log($"[ScreenEdgePositionAdjuster] GetOutsideReachPointPosition: Retrieving position for group '{groupName}'");
        Transform outlineReachPoint = GetOutsideReachPointTransform(groupName);
        if (outlineReachPoint != null)
        {
            Vector3 pos = outlineReachPoint.position;
            Debug.Log($"[ScreenEdgePositionAdjuster] GetOutsideReachPointPosition: Returning {pos}");
            return pos;
        }
        Debug.LogError($"[ScreenEdgePositionAdjuster] GetOutsideReachPointPosition: Could not find outside reach point for group '{groupName}'");
        return Vector3.zero;
    }
}
