using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SuperPowerSpawner : MonoBehaviour
{
    public static SuperPowerSpawner LocalInstance { get; private set; }
    [SerializeField] private List<GameObject> superPowerTokens = new List<GameObject>();
    private Dictionary<SuperPower, GameObject> superPowers = new Dictionary<SuperPower, GameObject>();
    private List<SuperPower> superPowerList = new List<SuperPower>();
    [SerializeField] private int maxSuperPowers = 5;
    private int numberOfSuperPowersToSpawn = 3;
    private List<GameObject> spawnedSuperPowers = new List<GameObject>();
    private Transform playerPowerPoolTransform;
    [SerializeField] private List<Transform> spawnPositions = new List<Transform>();
    [SerializeField] private GameObject centerGameObject;
    private Vector3 centerPosition;
    public GameObject backgroundPanel;
    public Text nameText;
    public Text descriptionText;
    public Button activateButton;
    public Button closeButton;
    // Start is called before the first frame update
    void Start()
    {
        if (LocalInstance != null && LocalInstance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        LocalInstance = this;
        DontDestroyOnLoad(this.gameObject); // Optional, if you want it to persist

        GetUIElements();

        //centerGameObject = GameObject.Find("Center");
        centerPosition = centerGameObject.transform.position;

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;//Return if pointer is on a UI object

            Vector3 mousePosition = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.LogError("Object touched: " + hit.collider.gameObject.tag);
                if (hit.collider.gameObject.tag == "Token")
                {
                    SuperPowerToken superPowerToken = hit.collider.GetComponent<SuperPowerToken>();
                    if (superPowerToken != null)
                    {
                        OpenInfoBox(superPowerToken);
                        centerGameObject.transform.position = new Vector3(centerPosition.x + 2500, centerPosition.y, centerPosition.z);
                        return; // Exit early if a token was clicked
                    }
                }
                if (SuperPowerToken.ActiveInstance != null && hit.collider.gameObject.tag == "Respawn")
                {
                    CloseInfoBox();
                    centerGameObject.transform.position = centerPosition;
                }
            }
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
        closeButton.onClick.AddListener(() =>
        {
            RemoveSpawnedSuperPower(SuperPowerToken.ActiveInstance.gameObject); // Remove this token from the spawner
            UpdateTokenPositions();
            Destroy(gameObject);
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
        activateButton.gameObject.SetActive(true);
        closeButton.gameObject.SetActive(true);
        nameText.text = SuperPowerToken.ActiveInstance.power.name;
        descriptionText.text = SuperPowerToken.ActiveInstance.power.description;
    }

    public void InitializeSuperPowers()
    {
        DictionaryCreation();
        foreach (var superPower in superPowers)
        {
            int rarity = superPower.Key.rarityMultiplier;
            for (int i = 0; i < rarity; i++)
            {
                superPowerList.Add(superPower.Key);
            }
        }
        playerPowerPoolTransform = GameObject.Find("PlayerPowerPool").transform;
    }

    private void DictionaryCreation()
    {
        foreach (var token in superPowerTokens)
        {
            SuperPower superPower = token.GetComponent<SuperPowerToken>().power;
            if (superPower != null && !superPowers.ContainsKey(superPower))
            {
                superPowers.Add(superPower, token);
            }
            else
            {
                Debug.LogWarning($"Super power {superPower?.name} already exists or is null.");
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
        if (spawnedSuperPowers.Count >= maxSuperPowers)
        {
            Debug.LogWarning("Max super powers reached, cannot spawn more.");
            yield return null;
        }

        // 1. Add a placeholder (empty GameObject)
        GameObject placeholder = new GameObject("TokenPlaceholder");
        spawnedSuperPowers.Add(placeholder);

        // 2. Update positions so existing tokens move as if the new token is present
        UpdateTokenPositions();

        yield return new WaitForSeconds(1.5f);

        // 3. Instantiate the real token at the placeholder's position
        if (superPowers.TryGetValue(superPower, out GameObject prefab))
        {
            Vector3 spawnPos = placeholder.transform.position;
            GameObject instance = Instantiate(prefab, spawnPos, transform.rotation);

            // 4. Replace the placeholder with the real token
            int placeholderIndex = spawnedSuperPowers.IndexOf(placeholder);
            if (placeholderIndex != -1)
            {
                spawnedSuperPowers[placeholderIndex] = instance;
            }
            Destroy(placeholder);

            Debug.Log($"{superPower.name} spawned.");
            // 5. Optionally update positions again to animate the real token (if needed)
            UpdateTokenPositions();
        }
        else
        {
            Debug.LogError($"Super power {superPower.name} not found in the dictionary.");
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


}
