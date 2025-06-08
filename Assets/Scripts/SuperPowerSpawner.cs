using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public static GameObject backgroundPanel;
    public static Text nameText;
    public static Text descriptionText;
    public static Button activateButton;
    public static Button closeButton;
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
            if(nameText == null) Debug.LogError("NameText not found");
            if(descriptionText == null) Debug.LogError("DescriptionText not found");
            if(activateButton == null) Debug.LogError("ActivateButton not found");
            if(closeButton == null) Debug.LogError("CloseButton not found");
            return;
        }

        activateButton.onClick.AddListener(OnTokenClicked);
        closeButton.onClick.AddListener(() =>
        {
            RemoveSpawnedSuperPower(SuperPowerToken.ActiveInstance.gameObject); // Remove this token from the spawner
            Destroy(gameObject);
        });

        CloseInfoBox(); // Ensure the info box is closed initially
    }

    private void OnTokenClicked()
    {
        Debug.Log("Activate button clicked for " + SuperPowerToken.ActiveInstance?.power.name);
        SuperPowerToken.ActiveInstance.OnTokenClicked();
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

    // Update is called once per frame
    void Update()
    {

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
    private void ReadyToSpawnSuperPowers()
    {
        StartCoroutine(ReadyToSpawnSuperPower());
    }
    public IEnumerator ReadyToSpawnSuperPower()
    {
        yield return new WaitForSeconds(1f); // Wait for 1 second before spawning
        for (int i = 0; i < numberOfSuperPowersToSpawn; i++)
        {
            SpawnSuperPower(GetRandomSuperPower());
            yield return new WaitForSeconds(0.5f); // Wait for 0.5 seconds between spawns
        }
    }

    private void SpawnSuperPower(SuperPower superPower)
    {
        if (spawnedSuperPowers.Count >= maxSuperPowers)
        {
            Debug.LogWarning("Max super powers reached, cannot spawn more.");
            return;
        }

        // Find the first available spawn position
        int spawnIndex = 0;
        for (; spawnIndex < spawnPositions.Count; spawnIndex++)
        {
            bool occupied = false;
            foreach (var token in spawnedSuperPowers)
            {
                if (token != null && Vector3.Distance(token.transform.position, spawnPositions[spawnIndex].position) < 0.01f)
                {
                    occupied = true;
                    break;
                }
            }
            if (!occupied)
                break;
        }

        if (spawnIndex >= spawnPositions.Count)
        {
            Debug.LogWarning("No available spawn positions.");
            return;
        }

        if (superPowers.TryGetValue(superPower, out GameObject prefab))
        {
            GameObject instance = Instantiate(prefab, spawnPositions[spawnIndex].position, transform.rotation);
            spawnedSuperPowers.Add(instance);
            Debug.Log($"{superPower.name} spawned at position {spawnIndex}.");
        }
        else
        {
            Debug.LogError($"Super power {superPower.name} not found in the dictionary.");
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

    public void UpdateTokenPositions()
    {
        // Remove any nulls (destroyed tokens)
        spawnedSuperPowers.RemoveAll(token => token == null);

        for (int i = 0; i < spawnedSuperPowers.Count && i < spawnPositions.Count; i++)
        {
            if (spawnedSuperPowers[i] != null)
            {
                spawnedSuperPowers[i].transform.position = spawnPositions[i].position;
            }
        }
    }

    public void RemoveSpawnedSuperPower(GameObject token)
    {
        CloseInfoBox();
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

}
