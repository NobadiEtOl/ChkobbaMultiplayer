using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ChangePixelsPerUnit : EditorWindow
{
    public List<Sprite> sprites = new List<Sprite>();
    private float pixelsPerUnit = 10f;

    [MenuItem("Tools/Change Sprite PPU")]
    public static void ShowWindow()
    {
        GetWindow<ChangePixelsPerUnit>("Change Sprite PPU");
    }

    private void OnGUI()
    {
        GUILayout.Label("Change Pixels Per Unit", EditorStyles.boldLabel);

        // Input Fields
        pixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", pixelsPerUnit);

        // Display Sprite List
        SerializedObject serializedObject = new SerializedObject(this);
        SerializedProperty spriteList = serializedObject.FindProperty("sprites");
        EditorGUILayout.PropertyField(spriteList, new GUIContent("Sprites"), true);
        serializedObject.ApplyModifiedProperties();

        // Apply Button
        if (GUILayout.Button("Apply Changes"))
        {
            ApplyPPUChanges();
        }
    }

    private void ApplyPPUChanges()
    {
        foreach (Sprite sprite in sprites)
        {
            if (sprite != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(sprite);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (importer != null && importer.textureType == TextureImporterType.Sprite)
                {
                    importer.spritePixelsPerUnit = pixelsPerUnit;
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    Debug.Log($"Updated {sprite.name} to {pixelsPerUnit} PPU.");
                }
                else
                {
                    Debug.LogWarning($"Failed to update {sprite.name}: Not a valid sprite.");
                }
            }
        }
    }
}
