using UnityEngine;
using UnityEditor;
using System.Linq;

public class TagAssigner : MonoBehaviour
{
    [MenuItem("Tools/Tag Assigner/Assign Tags to NewCards")]
    public static void AssignTagsToNewCards()
    {
        // Path to the folder containing the GameObjects
        string folderPath = "Assets/Prefabs/Cards/NewCards";

        // Get all prefab asset GUIDs in the folder
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { folderPath });

        if (guids.Length == 0)
        {
            Debug.LogWarning($"No GameObjects found in folder: {folderPath}");
            return;
        }

        int tagIndex = 0;

        foreach (string guid in guids)
        {
            // Load the asset as a GameObject
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (obj == null)
            {
                Debug.LogWarning($"Failed to load GameObject at path: {assetPath}. Skipping...");
                continue;
            }

            string tag = $"{(tagIndex / 10) + 1}_{(tagIndex % 10) + 1}";

            // Check if the tag exists
            if (!UnityEditorInternal.InternalEditorUtility.tags.Contains(tag))
            {
                Debug.LogWarning($"Tag '{tag}' does not exist. Make sure to generate tags first.");
                return;
            }

            // Assign the tag
            obj.tag = tag;
            tagIndex++;

            // Stop if we've exhausted the generated tags
            if (tagIndex >= 40) // 4_10 is the last tag in your range
            {
                Debug.LogWarning("Not enough tags for all specified objects. Some objects were not tagged.");
                break;
            }
        }

        Debug.Log("Tags assigned to GameObjects in the folder successfully!");
    }
}
