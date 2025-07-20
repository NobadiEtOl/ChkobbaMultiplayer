using UnityEngine;
using UnityEditor;
using TMPro;
using System.Text;
using System.Linq;

public class FontCharacterDebugger : EditorWindow
{
    private TMP_FontAsset fontAsset;
    private string testText = "The quick brown fox jumps over the lazy dog 1234567890!@#$%^&*()";
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Font Character Debugger")]
    public static void ShowWindow()
    {
        GetWindow<FontCharacterDebugger>("Font Character Debugger");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Font Character Coverage Debugger", EditorStyles.boldLabel);
        
        fontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField("Font Asset", fontAsset, typeof(TMP_FontAsset), false);
        
        GUILayout.Space(10);
        
        GUILayout.Label("Test Text:");
        testText = EditorGUILayout.TextArea(testText, GUILayout.Height(60));
        
        if (fontAsset != null && GUILayout.Button("Check Character Coverage"))
        {
            CheckCharacterCoverage();
        }
        
        if (fontAsset != null && GUILayout.Button("List All Available Characters"))
        {
            ListAvailableCharacters();
        }
        
        if (fontAsset != null && GUILayout.Button("Regenerate Font Asset"))
        {
            RegenerateFontAsset();
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        GUILayout.Label(debugOutput, EditorStyles.textArea);
        EditorGUILayout.EndScrollView();
    }
    
    private string debugOutput = "";
    
    void CheckCharacterCoverage()
    {
        if (fontAsset == null) return;
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== Character Coverage Report for '{fontAsset.name}' ===\n");
        
        var missingChars = new StringBuilder();
        var presentChars = new StringBuilder();
        
        foreach (char c in testText)
        {
            bool hasChar = fontAsset.HasCharacter(c);
            
            if (hasChar)
            {
                presentChars.Append(c);
            }
            else
            {
                missingChars.Append(c);
                sb.AppendLine($"MISSING: '{c}' (Unicode: {(int)c:X4})");
            }
        }
        
        sb.AppendLine($"\nSUMMARY:");
        sb.AppendLine($"Total characters in test: {testText.Length}");
        sb.AppendLine($"Present: {presentChars.Length}");
        sb.AppendLine($"Missing: {missingChars.Length}");
        
        sb.AppendLine($"\nPresent characters: {presentChars}");
        sb.AppendLine($"Missing characters: {missingChars}");
        
        sb.AppendLine($"\nFont Asset Info:");
        sb.AppendLine($"- Character Table Count: {fontAsset.characterTable.Count}");
        sb.AppendLine($"- Source Font: {(fontAsset.sourceFontFile ? fontAsset.sourceFontFile.name : "MISSING")}");
        sb.AppendLine($"- Atlas Texture: {(fontAsset.atlasTexture ? $"{fontAsset.atlasTexture.width}x{fontAsset.atlasTexture.height}" : "MISSING")}");
        
        debugOutput = sb.ToString();
    }
    
    void ListAvailableCharacters()
    {
        if (fontAsset == null) return;
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== All Available Characters in '{fontAsset.name}' ===\n");
        
        var chars = fontAsset.characterTable.OrderBy(c => c.unicode).ToList();
        
        foreach (var character in chars)
        {
            char c = (char)character.unicode;
            sb.AppendLine($"'{c}' (Unicode: {character.unicode:X4})");
        }
        
        debugOutput = sb.ToString();
    }
    
    void RegenerateFontAsset()
    {
        EditorUtility.DisplayDialog("Regenerate Font Asset", 
            "To regenerate the font asset with more characters:\n\n" +
            "1. Go to Window → TextMeshPro → Font Asset Creator\n" +
            "2. Select your source font file\n" +
            "3. Choose a broader Character Set:\n" +
            "   - ASCII Default Extended\n" +
            "   - Unicode Range (Custom)\n" +
            "4. Generate Font Atlas\n" +
            "5. Save as new asset or replace existing one", 
            "OK");
    }
}