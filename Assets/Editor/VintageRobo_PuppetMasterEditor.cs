#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VintageRobo_PuppetMaster))]
public class VintageRobo_PuppetMasterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VintageRobo_PuppetMaster controller = (VintageRobo_PuppetMaster)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Puppet Master Controls", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (Application.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Enable Control", GUILayout.Height(30)))
            {
                controller.SetControlEnabled(true);
            }
            
            if (GUILayout.Button("Disable Control", GUILayout.Height(30)))
            {
                controller.SetControlEnabled(false);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset to Original Pose", GUILayout.Height(30)))
            {
                controller.ResetToOriginalPose();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Recache Original Transforms", GUILayout.Height(30)))
            {
                controller.SendMessage("EditorRecacheTransforms");
            }
            
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Setup:\n" +
            "1. This script auto-finds VintageRobo_PuppetStrings on the same GameObject\n" +
            "2. Master fingers (reach points) will follow puppet bones (bone targets)\n" +
            "3. Adjust Position/Rotation Influence to control how much following happens\n" +
            "4. Hit Play and watch the puppet master's fingers follow the puppet!", 
            MessageType.Info);
    }
}
#endif
