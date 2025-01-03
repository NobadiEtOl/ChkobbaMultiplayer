using UnityEditor;
using UnityEngine;

public class TagCreator : MonoBehaviour
{
    [MenuItem("Tools/Generate Tags")]
    public static void GenerateTags()
    {
        // Loop to generate tags in the format 1_1, 1_2, ..., 4_10
        for (int i = 1; i <= 4; i++) // Outer loop for the first number
        {
            for (int j = 1; j <= 10; j++) // Inner loop for the second number
            {
                string tag = $"{i}_{j}";
                AddTag(tag);
            }
        }

        Debug.Log("Tags generated successfully!");
    }

    private static void AddTag(string tag)
    {
        // Load the TagManager asset
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // Check if the tag already exists
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(tag)) return; // Tag already exists
        }

        // Add the new tag
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
        newTag.stringValue = tag;

        // Apply changes
        tagManager.ApplyModifiedProperties();
    }
}