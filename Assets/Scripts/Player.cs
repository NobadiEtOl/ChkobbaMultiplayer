//using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    private string playerName;
    private int wins1v1;
    private int wins2v2;
    [SerializeField]private Sprite[] cardBackSprite;
    [SerializeField]private GameObject[] exampleCards = new GameObject[4];
    [SerializeField]public int cardBackIndex = 0;  

    // Handle movement on the client side
    void Start()
    {
        // Load the saved cardBackIndex or use 0 as the default
        cardBackIndex = PlayerPrefs.GetInt("CardBackIndex", 0);

        // Update the card backs with the loaded index
        foreach (GameObject card in exampleCards)
        {
            card.GetComponent<Image>().sprite = cardBackSprite[cardBackIndex];
        }
    }

    public void UpdateCardBack()
    {
        foreach (GameObject card in exampleCards)
        {
            card.GetComponent<Image>().sprite = cardBackSprite[cardBackIndex];
        }
        SaveCardBackIndex(); // Save the card back index whenever it is updated
    }

    public void SaveCardBackIndex()
    {
        PlayerPrefs.SetInt("CardBackIndex", cardBackIndex);
        PlayerPrefs.Save(); // Ensure the data is written to disk
        Debug.Log("CardBackIndex saved: " + cardBackIndex);
    }
}
