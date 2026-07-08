using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ClickNameLogger : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[ClickNameLogger] No Main Camera found in the scene.");
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Input.mousePosition;
            LogClickedObject(mousePosition);
        }
    }

    private void LogClickedObject(Vector3 screenPosition)
    {
        // 1. Check UI elements first (Canvas-based)
        if (EventSystem.current != null)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                Debug.Log($"[ClickName] {results[0].gameObject.name}");
                return;
            }
        }

        // 2. Check 3D Physics objects
        if (mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log($"[ClickName] {hit.collider.gameObject.name}");
                return;
            }
        }
    }
}
