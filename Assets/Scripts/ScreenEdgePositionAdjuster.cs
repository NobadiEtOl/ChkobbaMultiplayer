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
            
            return;
        }

        
        foreach (var group in groups)
            AdjustGroup(group);
        
    }

    private void AdjustGroup(EdgeGroup group)
    {
        if (group.objectTransform == null)
        {
            
            return;
        }
        if (group.edgeAnchor == null)
        {
            
            return;
        }

        

        // Snapshot of current world positions
        Vector3 originalObjectPos = group.objectTransform.position;
        

        // Anchor offset relative to the object (in world space at rest position)
        Vector3 anchorOffset = group.edgeAnchor.position - originalObjectPos;
        

        // Project object's depth onto camera space so the edge lies on the same plane
        float depth = targetCamera.WorldToViewportPoint(originalObjectPos).z;
        

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

        

        Vector3 edgeWorldPos = targetCamera.ViewportToWorldPoint(new Vector3(vx, vy, depth));
        

        // Shift only the axis that corresponds to the chosen edge (inside position)
        Vector3 newObjectPos = originalObjectPos;
        if (group.edge == ScreenEdge.Left || group.edge == ScreenEdge.Right)
            newObjectPos.x = edgeWorldPos.x - anchorOffset.x;
        else
            newObjectPos.y = edgeWorldPos.y - anchorOffset.y;

        

        // Copy inside position to the reach point
        if (group.reachPoint != null)
        {
            group.reachPoint.position = newObjectPos;
            
        }
        else
        {
            
        }

        // Calculate outside position if outline anchor is assigned
        if (group.outlineAnchor != null && group.outlineReachPoint != null)
        {
            Vector3 outlineAnchorOffset = group.outlineAnchor.position - originalObjectPos;
            
            
            Vector3 outsidePos = CalculateOutsidePosition(group, edgeWorldPos, originalObjectPos, outlineAnchorOffset);
            group.outlineReachPoint.position = outsidePos;
            
        }

        // Return the object to where it started
        group.objectTransform.position = originalObjectPos;
        
    }

    /// <summary>
    /// Calculates the outside position where the object should be positioned so that
    /// its outline anchor aligns with the same assigned screen edge.
    /// </summary>
    private Vector3 CalculateOutsidePosition(EdgeGroup group, Vector3 edgeWorldPos, Vector3 originalObjectPos, Vector3 outlineAnchorOffset)
    {
        
        
        // For outside position, we use the SAME edge but with the outline anchor instead of edge anchor
        float depth = targetCamera.WorldToViewportPoint(originalObjectPos).z;
        
        
        float vx, vy;
        switch (group.edge)
        {
            case ScreenEdge.Left:   vx = 0f;   vy = 0.5f; break;  // Same: Left
            case ScreenEdge.Right:  vx = 1f;   vy = 0.5f; break;  // Same: Right
            case ScreenEdge.Bottom: vx = 0.5f; vy = 0f;   break;  // Same: Bottom
            case ScreenEdge.Top:    vx = 0.5f; vy = 1f;   break;  // Same: Top
            default:                vx = 0.5f; vy = 0.5f; break;
        }

        

        Vector3 sameEdgeWorldPos = targetCamera.ViewportToWorldPoint(new Vector3(vx, vy, depth));
        
        

        Vector3 outsideObjectPos = originalObjectPos;
        if (group.edge == ScreenEdge.Left || group.edge == ScreenEdge.Right)
            outsideObjectPos.x = sameEdgeWorldPos.x - outlineAnchorOffset.x;
        else
            outsideObjectPos.y = sameEdgeWorldPos.y - outlineAnchorOffset.y;

        
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
        
        foreach (var group in groups)
        {
            if (group.groupName == groupName)
            {
                if (group.reachPoint != null)
                {
                    
                    return group.reachPoint;
                }
                else
                {
                    
                    return null;
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Returns the outside reach point Transform for a given group.
    /// Searches by group name instead of transform name to avoid name mismatch issues.
    /// Used by controllers to locate their assigned outside reach point at runtime for positioning objects out of screen.
    /// </summary>
    public Transform GetOutsideReachPointTransform(string groupName)
    {
        
        foreach (var group in groups)
        {
            if (group.groupName == groupName)
            {
                if (group.outlineReachPoint != null)
                {
                    
                    return group.outlineReachPoint;
                }
                else
                {
                    
                    return null;
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Returns the reach point position (where object should be when brought in) as a Vector3.
    /// </summary>
    public Vector3 GetReachPointPosition(string groupName)
    {
        
        Transform reachPoint = GetReachPointTransform(groupName);
        if (reachPoint != null)
        {
            Vector3 pos = reachPoint.position;
            
            return pos;
        }
        
        return Vector3.zero;
    }

    /// <summary>
    /// Returns the outside reach point position (where object should be when moved out) as a Vector3.
    /// This is the "return/starting position" for the object.
    /// </summary>
    public Vector3 GetOutsideReachPointPosition(string groupName)
    {
        
        Transform outlineReachPoint = GetOutsideReachPointTransform(groupName);
        if (outlineReachPoint != null)
        {
            Vector3 pos = outlineReachPoint.position;
            
            return pos;
        }
        
        return Vector3.zero;
    }
}
