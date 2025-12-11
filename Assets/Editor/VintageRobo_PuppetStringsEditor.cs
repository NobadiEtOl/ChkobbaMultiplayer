#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VintageRobo_PuppetStrings))]
public class VintageRobo_PuppetStringsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VintageRobo_PuppetStrings controller = (VintageRobo_PuppetStrings)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("String Controls", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Initialize Strings", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                System.Reflection.MethodInfo method = controller.GetType().GetMethod("EditorInitializeStrings", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                    method.Invoke(controller, null);
            }
            else
            {
                EditorUtility.DisplayDialog("Initialize Strings", "Please enter Play Mode to initialize strings.", "OK");
            }
        }
        
        if (GUILayout.Button("Clear All Strings", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear Strings", "Remove all puppet strings?", "Yes", "Cancel"))
            {
                System.Reflection.MethodInfo method = controller.GetType().GetMethod("ClearAllStrings", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                    method.Invoke(controller, null);
            }
        }
        
        EditorGUILayout.EndHorizontal();

        if (Application.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Show Strings", GUILayout.Height(25)))
            {
                controller.SetStringVisibility(true);
            }
            
            if (GUILayout.Button("Hide Strings", GUILayout.Height(25)))
            {
                controller.SetStringVisibility(false);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Setup Instructions:\n" +
            "1. Expand 'Strings' list and set size\n" +
            "2. For each string:\n" +
            "   - Drag a bone into 'Bone Target' (bottom end)\n" +
            "   - Drag a Transform into 'Reach Point' (top end)\n" +
            "   - Or leave Reach Point empty to use Anchor Offset\n" +
            "3. Customize color and width\n" +
            "4. Click 'Initialize Strings' or hit Play", 
            MessageType.Info);
    }
}
#endif
