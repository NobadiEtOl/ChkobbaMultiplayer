using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

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
    private VideoPlayer smokeEffectPlayer;
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

        smokeEffectPlayer = gameObject.GetComponent<VideoPlayer>();
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
    public void ReadyToSpawnSuperPower()
    {
        for (int i = 0; i < numberOfSuperPowersToSpawn; i++)
        {
            SpawnSuperPower(GetRandomSuperPower());
        }
    }

    private void SpawnSuperPower(SuperPower superPower)
    {
        if (spawnedSuperPowers.Count >= maxSuperPowers)
        {
            Debug.LogWarning("Max super powers reached, cannot spawn more.");
            return;
        }

        if (superPowers.TryGetValue(superPower, out GameObject prefab))
        {
            GameObject instance = Instantiate(prefab, playerPowerPoolTransform.position, transform.rotation);
            spawnedSuperPowers.Add(instance);
            Debug.Log($"{superPower.name} spawned.");
            smokeEffectPlayer.Play();
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
    
    [ContextMenu("Play Smoke Effect")]
    private void PlaySmokeEffect()
    {
        if (smokeEffectPlayer != null && !smokeEffectPlayer.isPlaying)
        {
            smokeEffectPlayer.Play();
        }
        else
        {
            Debug.LogWarning("Smoke effect player is null or already playing.");
        }
    }
}
