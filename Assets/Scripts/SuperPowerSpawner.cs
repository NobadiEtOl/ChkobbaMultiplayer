using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SuperPowerSpawner : MonoBehaviour
{
    public static SuperPowerSpawner LocalInstance { get; private set; }
    [SerializeField] private List<GameObject> superPowerTokens = new List<GameObject>();
    private Dictionary<SuperPower, GameObject> superPowerPrefabs = new Dictionary<SuperPower, GameObject>();
    private List<SuperPower> superPowerList = new List<SuperPower>();
    [SerializeField] private int maxSuperPowers = 5;
    private int numberOfSuperPowersToSpawn = 3;
    private List<GameObject> spawnedSuperPowers = new List<GameObject>();
    private Transform playerPowerPoolTransform;
    [SerializeField] private List<Transform> spawnPositions = new List<Transform>();
    [SerializeField] private GameObject centerGameObject;
    private Vector3 centerPosition;
    private GameObject backgroundPanel;
    private Text nameText;
    private Text descriptionText;
    private Button activateButton;
    private Button falseActivateButton;
    private Button closeButton;
    // Start is called before the first frame update
    void Awake()
    {
        if (LocalInstance != null && LocalInstance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        LocalInstance = this;
        DontDestroyOnLoad(this.gameObject); // Optional, if you want it to persist

        centerPosition = centerGameObject.transform.position;
        GetUIElements();

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return; // Return if pointer is on a UI object

            Vector3 mousePosition = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                HandleTokenRaycast(hit);
            }
        }
        // Handle touch input
        if (Input.touchCount > 0)
        {
            
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Ended)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    return; // Return if pointer is on a UI object

                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    HandleTokenRaycast(hit);
                }
            }
        }
    }

    private void HandleTokenRaycast(RaycastHit hit)
    {
        Debug.LogWarning("Object touched: " + hit.collider.gameObject.tag);
        if (hit.collider.gameObject.tag == "Token")
        {
            SuperPowerToken superPowerToken = hit.collider.GetComponent<SuperPowerToken>();
            if (superPowerToken != null)
            {
                centerGameObject.transform.position = new Vector3(centerPosition.x + 2500, centerPosition.y, centerPosition.z);
                OpenInfoBox(superPowerToken);
                return; // Exit early if a token was clicked
            }
        }
        if (SuperPowerToken.ActiveInstance != null && hit.collider.gameObject.tag == "Respawn")
        {
            centerGameObject.transform.position = centerPosition;
            Invoke("CloseInfoBox", 0.1f); // Close the info box after a short delay
        }
    }


    private void GetUIElements()
    {
        // Find the InfoBoxCanvas under this token only

        // Now find UI elements under this canvas only
        backgroundPanel = GameObject.Find("BackGroundPanel")?.gameObject;
        nameText = backgroundPanel.transform.Find("NamePanel/NameText")?.GetComponent<Text>();
        descriptionText = backgroundPanel.transform.Find("DescriptionPanel/DescriptionText")?.GetComponent<Text>();
        activateButton = GameObject.Find("ActivateButton")?.GetComponent<Button>();
        falseActivateButton = GameObject.Find("FalseActivateButton")?.GetComponent<Button>();
        closeButton = GameObject.Find("CloseButton")?.GetComponent<Button>();

        if (nameText == null || descriptionText == null || activateButton == null || closeButton == null)
        {
            Debug.LogError("One or more UI elements not found in InfoBoxCanvas for " + gameObject.name);
            if (nameText == null) Debug.LogError("NameText not found");
            if (descriptionText == null) Debug.LogError("DescriptionText not found");
            if (activateButton == null) Debug.LogError("ActivateButton not found");
            if (closeButton == null) Debug.LogError("CloseButton not found");
            return;
        }

        activateButton.onClick.AddListener(OnTokenClicked);
        falseActivateButton.onClick.AddListener(GetActiveButtonErrorMessage);
        closeButton.onClick.AddListener(() =>
        {
            RemoveSpawnedSuperPower(SuperPowerToken.ActiveInstance.gameObject); // Remove this token from the spawner
            UpdateTokenPositions();
            StartCoroutine(SuperPowerToken.ActiveInstance.FadeOutSprite());
            CloseInfoBox();
        });

        CloseInfoBox(); // Ensure the info box is closed initially
    }

    private void OnTokenClicked()
    {
        Debug.Log("Activate button clicked for " + SuperPowerToken.ActiveInstance?.power.name);
        SuperPowerToken.ActiveInstance.OnTokenClicked();
        centerGameObject.transform.position = centerPosition;
    }

    public void CloseInfoBox()
    {
        Debug.Log("Closing InfoBox for " + SuperPowerToken.ActiveInstance?.power.name);
        backgroundPanel.SetActive(false);
        activateButton.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
        SuperPowerToken.ActiveInstance = null; // Clear the active instance
    }

    public void OpenInfoBox(SuperPowerToken superPowerToken)
    {
        Debug.Log("Opening InfoBox for " + superPowerToken.power.name);
        if (SuperPowerToken.ActiveInstance != null && SuperPowerToken.ActiveInstance != this)
        {
            CloseInfoBox(); // Close the currently active instance
        }
        SuperPowerToken.ActiveInstance = superPowerToken; // Set the active instance
        backgroundPanel.SetActive(true);

        bool canActivate = CheckIfCardShouldBeSelected(superPowerToken.power.name);
        activateButton.gameObject.SetActive(canActivate);
        falseActivateButton.gameObject.SetActive(!canActivate);
        closeButton.gameObject.SetActive(true);

        // Always update the current info
        currentNameText = superPowerToken.power.name;
        currentDescriptionText = superPowerToken.power.description;
        nameText.text = currentNameText;
        descriptionText.text = currentDescriptionText;

        // If an error message is showing, stop it and show the correct info
        if (errorMessageCoroutine != null)
        {
            StopCoroutine(errorMessageCoroutine);
            errorMessageCoroutine = null;
        }
    }



    private List<string> restirictedPowersName_CardNeedToBeSelected = new List<string> { "Bu Daha İyi", "Şunu Değiş Tokuş", "Kopyala Yapıştır"};
    private List<string> restirictedPowersName_CenterNotEmpty = new List<string> { "Bomba" };
    private bool CheckIfCardShouldBeSelected(string superPowerTokenName)
    {
        bool flag = CardNeedToBeSelected(superPowerTokenName);
        flag = flag && CenterNotEmpty(superPowerTokenName);

        return flag;
    }

    private bool CardNeedToBeSelected(string superPowerTokenName)
    {
        if (restirictedPowersName_CardNeedToBeSelected.Contains(superPowerTokenName))
        {
            if (CardInteraction.currentlySelectedCard != null && GameManager.LocalInstance.GetCurrentSelectedHandCard() != null)
                return true;
            else
                return false;
        }
        return true;
    }

    private bool CenterNotEmpty(string superPowerTokenName)
    {
        if (restirictedPowersName_CenterNotEmpty.Contains(superPowerTokenName))
        {
            if (GameManager.LocalInstance.centerCards.Count != 0)
                return true;
            else
                return false;
        }
        return true;
    }

    public void SetActiveActivateButtonTrue()
    {
        if (activateButton.gameObject.activeSelf) return;
        else
        {
            activateButton.gameObject.SetActive(true);
            ResetInfoBoxText();
        }
    }

    public void InitializeSuperPowers()
    {
        DictionaryCreation();
        playerPowerPoolTransform = GameObject.Find("PlayerPowerPool")?.transform;
        if (playerPowerPoolTransform == null)
            Debug.LogWarning("PlayerPowerPool transform not found!");
    }



    private void DictionaryCreation()
    {
        superPowerPrefabs.Clear();
        superPowerList.Clear();

        foreach (var token in superPowerTokens)
        {
            var tokenScript = token.GetComponent<SuperPowerToken>();
            if (tokenScript == null)
            {
                Debug.LogWarning($"Token prefab {token.name} does not have a SuperPowerToken component.");
                continue;
            }

            string className = tokenScript.superPowerClassName;
            if (string.IsNullOrEmpty(className))
            {
                Debug.LogWarning($"Token prefab {token.name} does not have a valid superPowerClassName.");
                continue;
            }

            var type = System.Type.GetType(className);
            if (type == null || !typeof(SuperPower).IsAssignableFrom(type))
            {
                Debug.LogWarning($"Could not find SuperPower type for {className}");
                continue;
            }

            SuperPower powerInstance = ScriptableObject.CreateInstance(type) as SuperPower;
            if (powerInstance == null)
            {
                Debug.LogWarning($"Failed to create SuperPower instance for {className}");
                continue;
            }

            if (!superPowerPrefabs.ContainsKey(powerInstance))
            {
                superPowerPrefabs.Add(powerInstance, token);
                // Add to list for rarity
                for (int i = 0; i < powerInstance.rarityMultiplier; i++)
                    superPowerList.Add(powerInstance);
            }
            else
            {
                Debug.LogWarning($"Super power {className} already exists in the dictionary.");
            }
        }
    }

    [ContextMenu("Ready to Spawn Super Powers")]
    public void ReadyToSpawnSuperPowers()
    {
        StartCoroutine(ReadyToSpawnSuperPower());
    }
    private IEnumerator ReadyToSpawnSuperPower()
    {
        yield return new WaitForSeconds(1f); // Wait for 1 second before spawning
        for (int i = 0; i < numberOfSuperPowersToSpawn; i++)
        {
            StartCoroutine(SpawnSuperPower(GetRandomSuperPower()));
            yield return new WaitForSeconds(0.5f); // Wait for 0.5 seconds between spawns
        }
    }

    private IEnumerator SpawnSuperPower(SuperPower superPower)
    {
        if (superPower == null)
        {
            Debug.LogError("SpawnSuperPower called with null SuperPower!");
            yield break;
        }

        if (spawnedSuperPowers.Count >= maxSuperPowers)
        {
            Debug.LogWarning("Max super powers reached, cannot spawn more.");
            yield break;
        }

        GameObject placeholder = new GameObject("TokenPlaceholder");
        spawnedSuperPowers.Add(placeholder);
        UpdateTokenPositions();

        yield return new WaitForSeconds(1.5f);

        if (superPowerPrefabs.TryGetValue(superPower, out GameObject prefab))
        {
            Vector3 spawnPos = placeholder.transform.position;
            GameObject instance = Instantiate(prefab, spawnPos, transform.rotation);

            // Assign the SuperPower instance to the token
            var tokenScript = instance.GetComponent<SuperPowerToken>();
            if (tokenScript != null)
            {
                tokenScript.power = superPower;
            }

            int placeholderIndex = spawnedSuperPowers.IndexOf(placeholder);
            if (placeholderIndex != -1)
            {
                spawnedSuperPowers[placeholderIndex] = instance;
            }
            Destroy(placeholder);

            Debug.Log($"{superPower.name} spawned.");
            UpdateTokenPositions();
        }
        else
        {
            Debug.LogError($"Super power {superPower?.name} not found in the dictionary.");
            spawnedSuperPowers.Remove(placeholder);
            Destroy(placeholder);
        }
    }


    private SuperPower GetRandomSuperPower()
    {
        if (superPowerList.Count == 0)
        {
            Debug.LogWarning("No super powers available to spawn.");
            return null;
        }
        int randomIndex = Random.Range(0, superPowerList.Count);
        return superPowerList[randomIndex];
    }



    public void UpdateTokenPositions(float moveDuration = 0.25f)
    {
        // Remove any nulls (destroyed tokens)
        spawnedSuperPowers.RemoveAll(token => token == null);

        int[] indices = GetSpawnIndices(spawnedSuperPowers.Count);

        for (int i = 0; i < spawnedSuperPowers.Count && i < indices.Length; i++)
        {
            if (spawnedSuperPowers[i] != null)
            {
                StartCoroutine(MoveTokenToPosition(spawnedSuperPowers[i], spawnPositions[indices[i]].position, moveDuration));
            }
        }
    }

    private IEnumerator MoveTokenToPosition(GameObject token, Vector3 targetPosition, float duration)
    {
        Vector3 startPos = token.transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            token.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }
        token.transform.position = targetPosition;
    }



    public void RemoveSpawnedSuperPower(GameObject token)
    {
        if (spawnedSuperPowers.Contains(token))
        {
            spawnedSuperPowers.Remove(token);
            Debug.Log($"Removed {token.name} from spawned super powers.");
        }
        else
        {
            Debug.LogWarning($"{token.name} not found in spawned super powers.");
        }
    }

    private static readonly int[][] spawnIndexPatterns = new int[][]
    {
        new int[] { 4 },                // 1 token: [4]
        new int[] { 3, 5 },             // 2 tokens: [3, 5]
        new int[] { 2, 4, 6 },          // 3 tokens: [2, 4, 6]
        new int[] { 1, 3, 5, 7 },       // 4 tokens: [1, 3, 5, 7]
        new int[] { 0, 2, 4, 6, 8 },    // 5 tokens: [0, 2, 4, 6, 8]
    };

    private int[] GetSpawnIndices(int count)
    {
        if (count <= 0) return new int[0];
        if (count <= spawnIndexPatterns.Length)
            return spawnIndexPatterns[count - 1];
        // For more than 5, just fill from left to right (or expand as needed)
        int[] indices = new int[count];
        for (int i = 0; i < count && i < spawnPositions.Count; i++)
            indices[i] = i;
        return indices;
    }

    private Coroutine errorMessageCoroutine;
    private string currentNameText = "";
    private string currentDescriptionText = "";
    public void GetActiveButtonErrorMessage()
    {
        // If already showing error, reset timer
        if (errorMessageCoroutine != null)
        {
            StopCoroutine(errorMessageCoroutine);
        }
        errorMessageCoroutine = StartCoroutine(ShowErrorMessageCoroutine());
    }

    private IEnumerator ShowErrorMessageCoroutine()
    {
        nameText.text = "Hatan var";
        descriptionText.text = "Bu gücü kullanabilmek için önce bir kart seçmelisin";
        activateButton.gameObject.SetActive(false);
        falseActivateButton.gameObject.SetActive(true);

        float timer = 0f;
        while (timer < 4f)
        {
            // If info box is updated (e.g. new power selected), break early
            if (nameText.text != "Hatan var" || descriptionText.text != "Bu gücü kullanabilmek için önce bir kart seçmelisin")
                yield break;
            timer += Time.deltaTime;
            yield return null;
        }
        ResetInfoBoxText();
    }



    private void ResetInfoBoxText()
    {
        nameText.text = currentNameText;
        descriptionText.text = currentDescriptionText;

        // Update buttons based on current info
        bool canActivate = false;
        if (SuperPowerToken.ActiveInstance != null && SuperPowerToken.ActiveInstance.power != null)
            canActivate = CheckIfCardShouldBeSelected(SuperPowerToken.ActiveInstance.power.name);

        activateButton.gameObject.SetActive(canActivate);
        falseActivateButton.gameObject.SetActive(!canActivate);

        if (errorMessageCoroutine != null)
        {
            StopCoroutine(errorMessageCoroutine);
            errorMessageCoroutine = null;
        }
    }

    public void CheckIfBackgroundPanelOpen()
    {
        if (!backgroundPanel.activeSelf) return;
        else
        {
            OpenInfoBox(SuperPowerToken.ActiveInstance);
        }
    }

    public void ReportZaferPuaniToServer()
    {
        int playerNo = DeckController.LocalInstance.thisPlayerNumber;
        int totalPoints = 0;
        foreach (var tokenObj in spawnedSuperPowers)
        {
            if (tokenObj == null) continue;
            var token = tokenObj.GetComponent<SuperPowerToken>();
            if (token != null && token.power is ZaferPuani zaferPower)
            {
                totalPoints += zaferPower.points;
            }
        }
        // Always report, even if totalPoints is 0
        NetworkRelay.Instance.ReportZaferPuaniServerRPC(playerNo, totalPoints);
    }


}
