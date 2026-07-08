using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using TMPro;

public class EmojiKeyboardPrefabCreator : Editor
{
    [MenuItem("Tools/Create Emoji Keyboard Prefab")]
    public static void CreateEmojiKeyboardPrefab()
    {
        // 1. Create EmojiButton Prefab first
        GameObject emojiButton = CreateEmojiButtonPrefab();

        // 2. Create the main hierarchy
        GameObject root = new GameObject("EmojiKeyboard", typeof(RectTransform), typeof(CanvasGroup), typeof(EmojiKeyboardUI));
        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.sizeDelta = Vector2.zero;

        // Raycast Blocker
        GameObject blocker = CreateUIObject("RaycastBlocker", root.transform);
        Image blockerImg = blocker.AddComponent<Image>();
        blockerImg.color = new Color(0, 0, 0, 0.5f);
        Button blockerBtn = blocker.AddComponent<Button>();
        RectTransform blockerRT = blocker.GetComponent<RectTransform>();
        blockerRT.anchorMin = Vector2.zero;
        blockerRT.anchorMax = Vector2.one;
        blockerRT.sizeDelta = Vector2.zero;

        // Keyboard Panel
        GameObject panel = CreateUIObject("KeyboardPanel", root.transform);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark background
        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 10;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0);
        panelRT.anchorMax = new Vector2(0.5f, 0);
        panelRT.pivot = new Vector2(0.5f, 0);
        panelRT.sizeDelta = new Vector2(600, 450);
        panelRT.anchoredPosition = new Vector2(0, 20);

        // Header
        GameObject header = CreateUIObject("Header", panel.transform);
        HorizontalLayoutGroup hlgHeader = header.AddComponent<HorizontalLayoutGroup>();
        hlgHeader.childControlWidth = true;
        hlgHeader.childForceExpandWidth = false;
        
        GameObject title = CreateUIText("Title", header.transform, "İfadeler", 24, TextAlignmentOptions.Left);
        
        GameObject closeBtnObj = CreateUIButton("CloseButton", header.transform, "X");
        RectTransform closeBtnRT = closeBtnObj.GetComponent<RectTransform>();
        LayoutElement closeLE = closeBtnObj.AddComponent<LayoutElement>();
        closeLE.minWidth = 40;
        closeLE.preferredWidth = 40;

        // Middle Section (ScrollRect)
        GameObject middle = CreateUIObject("MiddleSection", panel.transform);
        LayoutElement middleLE = middle.AddComponent<LayoutElement>();
        middleLE.flexibleHeight = 1;

        ScrollRect scrollRect = middle.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject viewport = CreateUIObject("Viewport", middle.transform);
        viewport.AddComponent<RectMask2D>();
        Image viewportImg = viewport.AddComponent<Image>(); // Needed for masking usually
        viewportImg.color = new Color(1, 1, 1, 0.01f);
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewportRT;

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 300);

        GridLayoutGroup glg = content.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(80, 80);
        glg.spacing = new Vector2(10, 10);
        glg.padding = new RectOffset(10, 10, 10, 10);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        // Bottom Section (Tabs)
        GameObject bottom = CreateUIObject("BottomSection", panel.transform);
        HorizontalLayoutGroup hlgBottom = bottom.AddComponent<HorizontalLayoutGroup>();
        hlgBottom.spacing = 5;
        hlgBottom.childControlWidth = true;
        hlgBottom.childForceExpandWidth = true;

        string[] categories = { "Recent", "Smileys", "Gestures", "Nature", "Activities", "Objects" };
        string[] labels = { "Geçmiş", "Yüzler", "Jestler", "Doğa", "Spor", "Eşya" };

        for (int i = 0; i < categories.Length; i++)
        {
            string cat = categories[i];
            GameObject tabBtn = CreateUIButton("Tab_" + cat, bottom.transform, labels[i]);
            Button b = tabBtn.GetComponent<Button>();
            b.onClick.AddListener(() => { /* This will be wired at runtime or via code */ });
            
            // Note: Since we are creating a prefab, persistent listeners are hard to add via code without specific UnityEvents.
            // However, the EmojiKeyboardUI can wire them in Awake/Start if needed, but the requirement is to link fields.
            // Actually, we can use UnityEditorEvents if we really wanted to, but better to let the script handle it.
        }

        // Link fields in EmojiKeyboardUI
        EmojiKeyboardUI ui = root.GetComponent<EmojiKeyboardUI>();
        
        // Use SerializedObject to set private fields
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
        so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
        so.FindProperty("content").objectReferenceValue = contentRT;
        so.FindProperty("emojiButtonPrefab").objectReferenceValue = emojiButton;
        so.ApplyModifiedProperties();

        // Wire Close buttons
        UnityEditor.Events.UnityEventTools.AddPersistentListener(blockerBtn.onClick, ui.Close);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(closeBtnObj.GetComponent<Button>().onClick, ui.Close);

        // Wire Tab buttons (This is tricky to do persistently via script in Editor)
        // We'll use a trick: the EmojiKeyboardUI could find buttons by name or tag,
        // but let's try to add persistent listeners for category switching.
        for (int i = 0; i < categories.Length; i++)
        {
            string cat = categories[i];
            Button b = bottom.transform.GetChild(i).GetComponent<Button>();
            
            // AddPersistentListener requires a target object and a method name
            UnityEditor.Events.UnityEventTools.AddStringPersistentListener(b.onClick, ui.OnCategoryButtonClicked, cat);
        }

        // Save Prefab
        string prefabPath = "Assets/Prefabs/UI/EmojiKeyboard.prefab";
        EnsureDirectory(prefabPath);
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);

        Debug.Log("Emoji Keyboard Prefab created at: " + prefabPath);

        // Cleanup
        GameObject.DestroyImmediate(root);
    }

    private static GameObject CreateEmojiButtonPrefab()
    {
        string path = "Assets/Prefabs/UI/EmojiButton.prefab";
        
        GameObject btnObj = new GameObject("EmojiButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 80);

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        GameObject textObj = CreateUIText("Text", btnObj.transform, "😊", 40, TextAlignmentOptions.Center);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        EnsureDirectory(path);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(btnObj, path);
        GameObject.DestroyImmediate(btnObj);
        return prefab;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreateUIText(string name, Transform parent, string content, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return go;
    }

    private static GameObject CreateUIButton(string name, Transform parent, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        
        Image img = go.GetComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        CreateUIText("Label", go.transform, label, 20, TextAlignmentOptions.Center);
        
        RectTransform labelRT = go.transform.GetChild(0).GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.sizeDelta = Vector2.zero;

        return go;
    }

    private static void EnsureDirectory(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
