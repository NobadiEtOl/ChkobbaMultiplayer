using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WebGLCommunication : MonoBehaviour
{
    public static WebGLCommunication LocalInstance;
    private string joinCode;
    public string key;
    public int playerNumber;
    public string playerName;
    [SerializeField] private NetworkManagerUI networkManagerUI;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (LocalInstance == null)
        {
            LocalInstance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.isLoaded)
        {
            Debug.Log("Game fully loaded!");
            Invoke("SceneLoaded",3);
            //SendJoinCodeToHTML(joinCode);
        }
    }

    private void SceneLoaded()
    {
        Application.ExternalCall("sceneLoaded");
        Application.ExternalCall("sendMessageToUnity");
    }
    // Method to be called from the website
    public async Task SendMessageToUnity(string message)
    {
        // Example of handling received message
        Debug.Log("Received message from website: " + message);

        DecodeMessage(message);
    }

    private void DecodeMessage(string message)
    {
        try
        {
            // Split the message by "_"
            string[] parts = message.Split('_');

            if (parts.Length == 3)
            {
                key = parts[0];
                playerNumber = int.Parse(parts[1]);
                playerName = parts[2];


                if (playerNumber == 0)
                {
                    Debug.Log($"Key: {key}, PlayerNumber: {playerNumber}, PlayerName: {playerName}");
                    StartHost();
                    Debug.Log($"Key: {key}, PlayerNumber: {playerNumber}, PlayerName: {playerName}");
                    //SendJoinCodeToHTML(joinCode);
                }

                else
                {
                    Debug.Log($"Key: {key}, PlayerNumber: {playerNumber}, PlayerName: {playerName}");
                    StartClient();
                    Debug.Log($"Key: {key}, PlayerNumber: {playerNumber}, PlayerName: {playerName}");
                }
            }
            else
            {
                Debug.LogError("Invalid message format received.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error decoding message: " + ex.Message);
        }
    }

    private void StartHost()
    {
        Application.ExternalCall("startHost");
    }

    private void StartClient()
    {
        Application.ExternalCall("startClient");
    }
}
