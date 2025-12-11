#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VintageRobo_ScreenController))]
public class VintageRobo_ScreenControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VintageRobo_ScreenController controller = (VintageRobo_ScreenController)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Emotion Controls", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Create buttons in a grid layout
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("😊 Smile", GUILayout.Height(30)))
        {
            controller.ShowSmile();
        }
        
        if (GUILayout.Button("☹️ Frown", GUILayout.Height(30)))
        {
            controller.ShowFrown();
        }
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("😐 Neutral", GUILayout.Height(30)))
        {
            controller.ShowNeutral();
        }
        
        if (GUILayout.Button("😠 Angry", GUILayout.Height(30)))
        {
            controller.ShowAngry();
        }
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("💬 Talking", GUILayout.Height(30)))
        {
            controller.StartTalking();
        }
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("Click buttons above to preview emotions in real-time (works in Play Mode and Edit Mode)", MessageType.Info);
    }
}
#endif
