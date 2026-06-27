using UnityEngine;
using UnityEngine.UI;

public class AvatarCustomization : MonoBehaviour
{
    public static AvatarCustomization Instance { get; private set; }

    public enum CustomizationCategory
    {
        BaseFace,
        Hair,
        Eyes,
        Eyebrows,
        Mouth
    }

    [Header("Layered Image Components")]
    [SerializeField] private Image baseFaceImage;
    [SerializeField] private Image hairImage;
    [SerializeField] private Image eyesImage;
    [SerializeField] private Image eyebrowsImage;
    [SerializeField] private Image mouthImage;

    [Header("Sprite Arrays")]
    [SerializeField] private Sprite[] baseFaceSprites;
    [SerializeField] private Sprite[] hairSprites;
    [SerializeField] private Sprite[] eyesSprites;
    [SerializeField] private Sprite[] eyebrowsSprites;
    [SerializeField] private Sprite[] mouthSprites;

    [Header("UI Control Buttons (Optional - will auto-bind if assigned)")]
    [SerializeField] private Button hairPrevButton;
    [SerializeField] private Button hairNextButton;
    [SerializeField] private Button eyesPrevButton;
    [SerializeField] private Button eyesNextButton;
    [SerializeField] private Button eyebrowsPrevButton;
    [SerializeField] private Button eyebrowsNextButton;
    [SerializeField] private Button mouthPrevButton;
    [SerializeField] private Button mouthNextButton;

    [Header("Keyboard Selection Options")]
    [SerializeField] private bool enableKeyboardNavigation = true;
    [SerializeField] private Color selectedCategoryColor = Color.yellow;
    [SerializeField] private Color normalCategoryColor = Color.white;
    // Optional: Visual outline/border of category buttons or indicator highlights
    [SerializeField] private Image[] categoryHighlightIndicators;

    // Selection Indices
    private int selectedBaseFaceIndex;
    private int selectedHairIndex;
    private int selectedEyesIndex;
    private int selectedEyebrowsIndex;
    private int selectedMouthIndex;

    // Temporary/Preview Indices (used while browsing before saving)
    private int previewBaseFaceIndex;
    private int previewHairIndex;
    private int previewEyesIndex;
    private int previewEyebrowsIndex;
    private int previewMouthIndex;

    // Active category for keyboard navigation
    private CustomizationCategory activeCategory = CustomizationCategory.BaseFace;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("AvatarCustomization: Another instance already exists! Destroying duplicate on " + gameObject.name);
            Destroy(this);
            return;
        }
    }

    private void Start()
    {
        LoadSelections();
        InitializeButtons();
        UpdatePreviewUI();
        UpdateCategoryHighlight();
    }

    private void OnEnable()
    {
        // Whenever the window is opened, sync previews back to the last saved selections
        ResetToSaved();
    }

    private void Update()
    {
        if (enableKeyboardNavigation && gameObject.activeInHierarchy)
        {
            HandleKeyboardInput();
        }
    }

    #region Loading and Saving

    public void LoadSelections()
    {
        selectedBaseFaceIndex = PlayerPrefs.GetInt("AvatarBaseFaceIndex", 0);
        selectedHairIndex = PlayerPrefs.GetInt("AvatarHairIndex", 0);
        selectedEyesIndex = PlayerPrefs.GetInt("AvatarEyesIndex", 0);
        selectedEyebrowsIndex = PlayerPrefs.GetInt("AvatarEyebrowsIndex", 0);
        selectedMouthIndex = PlayerPrefs.GetInt("AvatarMouthIndex", 0);

        ResetToSaved();
    }

    public void SaveCustomization()
    {
        selectedBaseFaceIndex = previewBaseFaceIndex;
        selectedHairIndex = previewHairIndex;
        selectedEyesIndex = previewEyesIndex;
        selectedEyebrowsIndex = previewEyebrowsIndex;
        selectedMouthIndex = previewMouthIndex;

        PlayerPrefs.SetInt("AvatarBaseFaceIndex", selectedBaseFaceIndex);
        PlayerPrefs.SetInt("AvatarHairIndex", selectedHairIndex);
        PlayerPrefs.SetInt("AvatarEyesIndex", selectedEyesIndex);
        PlayerPrefs.SetInt("AvatarEyebrowsIndex", selectedEyebrowsIndex);
        PlayerPrefs.SetInt("AvatarMouthIndex", selectedMouthIndex);
        PlayerPrefs.Save();

        Debug.Log($"[AvatarCustomization] Customization saved successfully! Base: {selectedBaseFaceIndex}, Hair: {selectedHairIndex}, Eyes: {selectedEyesIndex}, Eyebrows: {selectedEyebrowsIndex}, Mouth: {selectedMouthIndex}");
    }

    public void ResetToSaved()
    {
        previewBaseFaceIndex = selectedBaseFaceIndex;
        previewHairIndex = selectedHairIndex;
        previewEyesIndex = selectedEyesIndex;
        previewEyebrowsIndex = selectedEyebrowsIndex;
        previewMouthIndex = selectedMouthIndex;

        UpdatePreviewUI();
    }

    #endregion

    #region Raw Data Accessors

    public int GetBaseFaceIndex() => selectedBaseFaceIndex;
    public int GetHairIndex() => selectedHairIndex;
    public int GetEyesIndex() => selectedEyesIndex;
    public int GetEyebrowsIndex() => selectedEyebrowsIndex;
    public int GetMouthIndex() => selectedMouthIndex;

    public Sprite GetBaseFaceSprite(int index) => GetSpriteFromCategory(baseFaceSprites, index);
    public Sprite GetHairSprite(int index) => GetSpriteFromCategory(hairSprites, index);
    public Sprite GetEyesSprite(int index) => GetSpriteFromCategory(eyesSprites, index);
    public Sprite GetEyebrowsSprite(int index) => GetSpriteFromCategory(eyebrowsSprites, index);
    public Sprite GetMouthSprite(int index) => GetSpriteFromCategory(mouthSprites, index);

    private Sprite GetSpriteFromCategory(Sprite[] array, int index)
    {
        if (array == null || array.Length == 0) return null;
        int safeIndex = Mathf.Clamp(index, 0, array.Length - 1);
        return array[safeIndex];
    }

    #endregion

    #region Element Cycling

    public void CycleElement(CustomizationCategory category, bool forward)
    {
        switch (category)
        {
            case CustomizationCategory.BaseFace:
                previewBaseFaceIndex = CycleIndex(previewBaseFaceIndex, baseFaceSprites.Length, forward);
                break;
            case CustomizationCategory.Hair:
                previewHairIndex = CycleIndex(previewHairIndex, hairSprites.Length, forward);
                break;
            case CustomizationCategory.Eyes:
                previewEyesIndex = CycleIndex(previewEyesIndex, eyesSprites.Length, forward);
                break;
            case CustomizationCategory.Eyebrows:
                previewEyebrowsIndex = CycleIndex(previewEyebrowsIndex, eyebrowsSprites.Length, forward);
                break;
            case CustomizationCategory.Mouth:
                previewMouthIndex = CycleIndex(previewMouthIndex, mouthSprites.Length, forward);
                break;
        }

        UpdatePreviewUI();
    }

    private int CycleIndex(int currentIndex, int arrayLength, bool forward)
    {
        if (arrayLength <= 0) return 0;
        if (forward)
        {
            return (currentIndex + 1) % arrayLength;
        }
        else
        {
            return (currentIndex - 1 + arrayLength) % arrayLength;
        }
    }

    #endregion

    #region UI Presentation

    private void UpdatePreviewUI()
    {
        UpdateImageComponent(baseFaceImage, baseFaceSprites, previewBaseFaceIndex);
        UpdateImageComponent(hairImage, hairSprites, previewHairIndex);
        UpdateImageComponent(eyesImage, eyesSprites, previewEyesIndex);
        UpdateImageComponent(eyebrowsImage, eyebrowsSprites, previewEyebrowsIndex);
        UpdateImageComponent(mouthImage, mouthSprites, previewMouthIndex);
    }

    private void UpdateImageComponent(Image image, Sprite[] sprites, int index)
    {
        if (image == null) return;

        if (sprites == null || sprites.Length == 0)
        {
            image.enabled = false;
            return;
        }

        int safeIndex = Mathf.Clamp(index, 0, sprites.Length - 1);
        Sprite targetSprite = sprites[safeIndex];

        if (targetSprite != null)
        {
            image.sprite = targetSprite;
            image.enabled = true;
        }
        else
        {
            image.enabled = false;
        }
    }

    #endregion

    #region Button Initialization

    private void InitializeButtons()
    {
        if (hairPrevButton != null) hairPrevButton.onClick.AddListener(() => { SetActiveCategory(CustomizationCategory.Hair); CycleElement(CustomizationCategory.Hair, false); });
        if (hairNextButton != null) hairNextButton.onClick.AddListener(() => { SetActiveCategory(CustomizationCategory.Hair); CycleElement(CustomizationCategory.Hair, true); });

        if (eyesPrevButton != null) eyesPrevButton.onClick.AddListener(() => { SetActiveCategory(CustomizationCategory.Eyes); CycleElement(CustomizationCategory.Eyes, false); });
        if (eyesNextButton != null) eyesNextButton.onClick.AddListener(() => { SetActiveCategory(CustomizationCategory.Eyes); CycleElement(CustomizationCategory.Eyes, true); });

        if (eyebrowsPrevButton != null) eyebrowsPrevButton.onClick.AddListener(() => { SetActiveCategory(CustomizationCategory.Eyebrows); CycleElement(CustomizationCategory.Eyebrows, false); });
        if (eyebrowsNextButton != null) eyebrowsNextButton.onClick.AddListener(() => { SetActiveCategory(CustomizationCategory.Eyebrows); CycleElement(CustomizationCategory.Eyebrows, true); });

        if (mouthPrevButton != null) mouthPrevButton.onClick.AddListener(() => { SetActiveCategory(CustomizationCategory.Mouth); CycleElement(CustomizationCategory.Mouth, false); });
        if (mouthNextButton != null) mouthNextButton.onClick.AddListener(() => { SetActiveCategory(CustomizationCategory.Mouth); CycleElement(CustomizationCategory.Mouth, true); });
    }

    #endregion

    #region Keyboard Navigation

    private void HandleKeyboardInput()
    {
        // 1. Category selection using Up and Down arrow keys
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            NavigateCategory(false);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            NavigateCategory(true);
        }

        // 2. Element cycling using Left and Right arrow keys
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            CycleElement(activeCategory, false);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            CycleElement(activeCategory, true);
        }
    }

    private void NavigateCategory(bool forward)
    {
        int totalCategories = System.Enum.GetValues(typeof(CustomizationCategory)).Length;
        int nextCategoryIndex = (int)activeCategory;

        if (forward)
        {
            nextCategoryIndex = (nextCategoryIndex + 1) % totalCategories;
        }
        else
        {
            nextCategoryIndex = (nextCategoryIndex - 1 + totalCategories) % totalCategories;
        }

        SetActiveCategory((CustomizationCategory)nextCategoryIndex);
    }

    public void SetActiveCategory(CustomizationCategory category)
    {
        activeCategory = category;
        UpdateCategoryHighlight();
    }

    private void UpdateCategoryHighlight()
    {
        if (categoryHighlightIndicators == null || categoryHighlightIndicators.Length == 0) return;

        for (int i = 0; i < categoryHighlightIndicators.Length; i++)
        {
            if (categoryHighlightIndicators[i] == null) continue;

            if (i == (int)activeCategory)
            {
                categoryHighlightIndicators[i].color = selectedCategoryColor;
            }
            else
            {
                categoryHighlightIndicators[i].color = normalCategoryColor;
            }
        }
    }

    #endregion
}
