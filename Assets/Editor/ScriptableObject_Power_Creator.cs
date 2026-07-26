using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

public class ScriptableObject_Power_Creator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Create All SuperPower ScriptableObjects")]
    public static void CreateAllSuperPowers()
    {
        string parentFolder = "Assets/Prefabs/SuperPowers/TokenObjects";
        string folderName = "ScriptableObjects";
        string folderPath = $"{parentFolder}/{folderName}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }

        // Find all non-abstract types derived from SuperPower
        var superPowerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(SuperPower).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var type in superPowerTypes)
        {
            string assetPath = $"{folderPath}/{type.Name}.asset";
            if (AssetDatabase.LoadAssetAtPath<SuperPower>(assetPath) != null)
                continue;

            SuperPower asset = ScriptableObject.CreateInstance(type) as SuperPower;
            AssetDatabase.CreateAsset(asset, assetPath);
            
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
    }
#endif
}