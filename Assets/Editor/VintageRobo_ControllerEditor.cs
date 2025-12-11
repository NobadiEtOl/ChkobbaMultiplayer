#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VintageRobo_Controller))]
public class VintageRobo_ControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VintageRobo_Controller controller = (VintageRobo_Controller)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Animation Controls", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Create buttons in a grid layout
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🧍 Idle", GUILayout.Height(35)))
        {
            controller.PlayIdle();
        }
        
        if (GUILayout.Button("👋 Wave", GUILayout.Height(35)))
        {
            controller.PlayWave();
        }
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("📞 Call", GUILayout.Height(35)))
        {
            controller.PlayCall();
        }
        
        if (GUILayout.Button("💃 Dance", GUILayout.Height(35)))
        {
            controller.PlayDance();
        }
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        
        // Show current state
        EditorGUILayout.HelpBox($"Current State: {controller.GetCurrentState()}", MessageType.Info);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("Click buttons above to trigger animations in real-time (works best in Play Mode)", MessageType.Info);
    }
}
#endif
