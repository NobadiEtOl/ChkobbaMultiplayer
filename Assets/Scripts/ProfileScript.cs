using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Unity.VisualScripting;

public class ProfileScript : MonoBehaviour
{
    [SerializeReference] public Sprite[] cardBackSprites;
    [SerializeReference] private Sprite[] profilePicSprites;
    [SerializeField]private GameObject cardBackShowcase;
    [SerializeField] private GameObject profilePicShowcase;
    [SerializeField]private InputField playerNameInputField;
    [SerializeField]private GameObject[] exampleCards = new GameObject[4];
    public int cardBackIndex;
    public int profilePicIndex;
    private int total1v1MatchCount;
    private int total1v1MatchWinCount;
    [SerializeField]private Text text1v1MatchCount;
    [SerializeField]private Text text2v2MatchCount;
    private int total2v2MatchCount;
    private int total2v2MatchWinCount;
    private string playerName;
    public int cardBackCounter;
    public int profilePicCounter;  

    // Start is called before the first frame update
    void Start()
    {

        // Load the saved cardBackIndex or use 0 as the default
        cardBackIndex = PlayerPrefs.GetInt("CardBackIndex", 0);
        playerName = PlayerPrefs.GetString("PlayerName", "Player");
        profilePicIndex = PlayerPrefs.GetInt("ProfilePicIndex", 0);
        LoadMatchCounts();

        playerNameInputField.text = playerName;

        // Update the card backs with the loaded index
        foreach (GameObject card in exampleCards)
        {
            card.GetComponent<Image>().sprite = cardBackSprites[cardBackIndex];
        }

        cardBackShowcase.GetComponent<Image>().sprite = cardBackSprites[cardBackIndex];
        profilePicShowcase.GetComponent<Image>().sprite = profilePicSprites[profilePicIndex];
    }

    private void LoadMatchCounts()
    {
        total1v1MatchCount = PlayerPrefs.GetInt("Total1v1MatchCount", 0);
        total1v1MatchWinCount = PlayerPrefs.GetInt("Total1v1MatchWinCount", 0);
        total2v2MatchCount = PlayerPrefs.GetInt("Total2v2MatchCount", 0);
        total2v2MatchWinCount = PlayerPrefs.GetInt("Total2v2MatchWinCount", 0);
    }

    public void OnCardBackchangeNext()
    {
        // Increment the counter and loop back to 0 if it exceeds the array length
        cardBackCounter = (cardBackCounter + 1) % cardBackSprites.Length;

        // Update the card back showcase with the new sprite
        cardBackShowcase.GetComponent<Image>().sprite = cardBackSprites[(cardBackIndex + cardBackCounter) % cardBackSprites.Length];
    }

    public void OnCardBackchangePrevious()
    {
        // Decrement the counter and loop back to the last index if it goes below 0
        cardBackCounter = (cardBackCounter - 1 + cardBackSprites.Length) % cardBackSprites.Length;

        // Update the card back showcase with the new sprite
        cardBackShowcase.GetComponent<Image>().sprite = cardBackSprites[(cardBackIndex + cardBackCounter + cardBackSprites.Length) % cardBackSprites.Length];
    }

    public void OnProfilePicChangeNext()
    {
        profilePicCounter = (profilePicCounter + 1) % profilePicSprites.Length;
        profilePicShowcase.GetComponent<Image>().sprite = profilePicSprites[(profilePicIndex + profilePicCounter) % profilePicSprites.Length];
    }

    public void OnProfilePicChangePrevious()
    {
        profilePicCounter = (profilePicCounter - 1 + profilePicSprites.Length) % profilePicSprites.Length;
        profilePicShowcase.GetComponent<Image>().sprite = profilePicSprites[(profilePicIndex + profilePicCounter + profilePicSprites.Length) % profilePicSprites.Length];
    }

    public void OnSaveConfirm()
    {
        UpdateCardBack();
        UpdatePlayerName();
        UpdateProfilePic();  
    }

    public void UpdateCardBack()
    {
        cardBackIndex = (cardBackIndex + cardBackCounter) % cardBackSprites.Length;

        foreach (GameObject card in exampleCards)
        {
            card.GetComponent<Image>().sprite = cardBackSprites[cardBackIndex];
        }
        SaveCardBackIndex(); // Save the card back index whenever it is updated
        cardBackCounter = 0;
    }

    public void SaveCardBackIndex()
    {
        PlayerPrefs.SetInt("CardBackIndex", cardBackIndex);
        PlayerPrefs.Save(); // Ensure the data is written to disk
    }

    private void UpdatePlayerName()
    {
        // Update the player name in the PlayerPrefs
        PlayerPrefs.SetString("PlayerName", playerNameInputField.text);
        PlayerPrefs.Save(); // Ensure the data is written to disk
    }

    public void UpdateProfilePic()
    {
        profilePicIndex = (profilePicIndex + profilePicCounter) % profilePicSprites.Length;
        profilePicShowcase.GetComponent<Image>().sprite = profilePicSprites[profilePicIndex];
        SaveProfilePicIndex();
        profilePicCounter = 0;
    }

    public void SaveProfilePicIndex()
    {
        PlayerPrefs.SetInt("ProfilePicIndex", profilePicIndex);
        PlayerPrefs.Save();
    }

    public void ResetCardBackShowcase()
    {
        // Reset the card back showcase to the default sprite
        cardBackCounter = 0;
        cardBackShowcase.GetComponent<Image>().sprite = cardBackSprites[cardBackIndex];
    }

    public void ResetProfilePicShowcase()
    {
        // Reset the profile picture showcase to the default sprite
        profilePicCounter = 0;
        profilePicShowcase.GetComponent<Image>().sprite = profilePicSprites[profilePicIndex];
    }

    [ContextMenu("Reset Increment1v1MatchCount")]
    public void Increment1v1MatchCount()
    {
        total1v1MatchCount++;
        PlayerPrefs.SetInt("Total1v1MatchCount", total1v1MatchCount);
        PlayerPrefs.Save(); // Ensure the data is written to disk
    }

    [ContextMenu("Reset Increment1v1MatchWinCount")]
    public void Increment1v1MatchWinCount()
    {
        total1v1MatchWinCount++;
        PlayerPrefs.SetInt("Total1v1MatchWinCount", total1v1MatchWinCount);
        PlayerPrefs.Save(); // Ensure the data is written to disk
    }
    public void Increment2v2MatchCount()
    {
        total2v2MatchCount++;
        PlayerPrefs.SetInt("Total2v2MatchCount", total2v2MatchCount);
        PlayerPrefs.Save(); // Ensure the data is written to disk
    }
    public void Increment2v2MatchWinCount()
    {
        total2v2MatchWinCount++;
        PlayerPrefs.SetInt("Total2v2MatchWinCount", total2v2MatchWinCount);
        PlayerPrefs.Save(); // Ensure the data is written to disk1937
    }

    public void UpdateMatchCountText()
    {
        text1v1MatchCount.text = "1v1 Score\n" + total1v1MatchWinCount + "/" +total1v1MatchCount;
        text2v2MatchCount.text = "2v2 Score\n" + total2v2MatchWinCount + "/" +total2v2MatchCount;
    }

    //Use only if you need to reset all PlayerPrefs
    [ContextMenu("Reset All PlayerPrefs")]
    public void ResetAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
